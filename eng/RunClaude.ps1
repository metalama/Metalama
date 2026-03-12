# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

param(
    [string]$Prompt,
    [int]$McpPort
)

$ErrorActionPreference = "Stop"

$Model = "opus"

if ($env:RUNNING_IN_DOCKER -ne "true")
{
    Write-Error "This script must be run inside a Docker container. Set RUNNING_IN_DOCKER=true to override."
    exit 1
}

# --- Output sanitization (matches ClaudeCodeHelper.SanitizeOutput) ---
function Sanitize-ClaudeOutput {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) { return "" }

    # Strip ANSI escape sequences
    $stripped = $Text -replace '\x1b\[[0-9;]*m','' -replace '\[\d+(?:;\d+)*m',''

    $sb = [System.Text.StringBuilder]::new($stripped.Length)
    foreach ($c in $stripped.ToCharArray()) {
        $code = [int]$c
        if (($code -ge 32 -and $code -le 126) -or $c -eq "`n" -or $c -eq "`r" -or $c -eq "`t") {
            [void]$sb.Append($c)
        }
        elseif ([char]::IsWhiteSpace($c)) {
            [void]$sb.Append(' ')
        }
        # Skip all other characters (including extended Unicode)
    }
    return $sb.ToString()
}

# Tools whose output is silenced in the monitoring display
$script:SilentTools = @('Read', 'Glob', 'Grep', 'Edit')
$script:SilentToolIds = @{}

# --- JSON stream line parser (matches ClaudeCodeHelper.TranslateJsonToHumanReadable) ---
function ConvertFrom-ClaudeJsonLine {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) { return }

    try {
        $json = $Line | ConvertFrom-Json
    } catch {
        Write-Host (Sanitize-ClaudeOutput $Line)
        return
    }

    switch ($json.type) {
        'system' {
            if ($json.subtype -eq 'init') {
                $model = if ($json.model) { $json.model } else { "unknown" }
                Write-Host (Sanitize-ClaudeOutput "[Claude Code initialized - model: $model]") -ForegroundColor Green
            }
        }
        'assistant' {
            if ($json.message -and $json.message.content) {
                foreach ($block in $json.message.content) {
                    if ($block.type -eq 'text') {
                        Write-Host ""
                        Write-Host (Sanitize-ClaudeOutput $block.text) -ForegroundColor Cyan
                    }
                    elseif ($block.type -eq 'tool_use') {
                        $toolName = if ($block.name) { $block.name } else { "unknown" }
                        if ($script:SilentTools -contains $toolName) {
                            if ($block.id) { $script:SilentToolIds[$block.id] = $true }
                            continue
                        }
                        # Silence Bash calls for read-only commands (ls, grep)
                        if ($toolName -eq 'Bash' -and $block.input.command -match '^\s*(ls|grep|find)\b') {
                            if ($block.id) { $script:SilentToolIds[$block.id] = $true }
                            continue
                        }
                        Write-Host ""
                        Write-Host (Sanitize-ClaudeOutput "[Tool: $toolName]") -ForegroundColor Yellow
                        if ($block.input) {
                            # Display the most identifying property from the input
                            $displayProps = @(
                                @{ Key = 'file_path';    Label = 'File' }
                                @{ Key = 'command';      Label = '$' }
                                @{ Key = 'pattern';      Label = 'Pattern' }
                                @{ Key = 'query';        Label = 'Query' }
                                @{ Key = 'url';          Label = 'URL' }
                                @{ Key = 'skill';        Label = 'Skill' }
                                @{ Key = 'prompt';       Label = 'Prompt' }
                                @{ Key = 'description';  Label = 'Task' }
                            )
                            $shown = $false
                            foreach ($dp in $displayProps) {
                                $val = $block.input.($dp.Key)
                                if ($val) {
                                    $truncated = if ($val.Length -gt 1024) { $val.Substring(0, 1024) + "..." } else { $val }
                                    Write-Host (Sanitize-ClaudeOutput "  $($dp.Label): $truncated") -ForegroundColor Gray
                                    $shown = $true
                                    break
                                }
                            }
                            if (-not $shown) {
                                # Fallback: show the property names so the user at least sees what was passed
                                $keys = ($block.input.PSObject.Properties | Select-Object -ExpandProperty Name) -join ', '
                                if ($keys) {
                                    Write-Host (Sanitize-ClaudeOutput "  [$keys]") -ForegroundColor Gray
                                }
                            }
                        }
                    }
                }
            }
        }
        'user' {
            if ($json.message -and $json.message.content) {
                foreach ($block in $json.message.content) {
                    if ($block.type -eq 'tool_result') {
                        if ($block.tool_use_id -and $script:SilentToolIds.ContainsKey($block.tool_use_id)) {
                            $script:SilentToolIds.Remove($block.tool_use_id)
                            continue
                        }
                        $content = if ($block.content) { $block.content } else { "" }
                        # Strip system reminders and tool use markup
                        $content = $content -replace '(?s)<system-reminder>.*?</system-reminder>', ''
                        $closingTag = '</function_calls>'
                        $content = $content -replace "(?s)<function_calls>.*?$closingTag", ''
                        $sanitized = Sanitize-ClaudeOutput $content
                        $lines = $sanitized -split "`n"
                        $maxLines = 5
                        $color = if ($block.is_error) { "Red" } else { "DarkGray" }
                        $prefix = if ($block.is_error) { "  [ERROR] " } else { "  ->" }
                        for ($i = 0; $i -lt [Math]::Min($lines.Count, $maxLines); $i++) {
                            Write-Host "$prefix$($lines[$i])" -ForegroundColor $color
                        }
                        if ($lines.Count -gt $maxLines) {
                            Write-Host "  ... ($($lines.Count - $maxLines) more lines)" -ForegroundColor $color
                        }
                    }
                }
            }
        }
        'result' {
            Write-Host (Sanitize-ClaudeOutput "[Session completed]") -ForegroundColor Green
        }
        'error' {
            $msg = if ($json.error.message) { $json.error.message } elseif ($json.error) { $json.error } else { "Unknown error" }
            Write-Host (Sanitize-ClaudeOutput "[ERROR] $msg") -ForegroundColor Red
        }
    }
}

# Configure MCP approval server if port is specified
$mcpConfigArg = ""
if ($McpPort -gt 0)
{
    # On Windows containers, host.docker.internal doesn't resolve.
    # Use the default gateway IP which points to the host.
    $hostIp = (Get-NetRoute -DestinationPrefix '0.0.0.0/0' | Select-Object -First 1).NextHop
    if ([string]::IsNullOrEmpty($hostIp))
    {
        Write-Error "Could not determine host IP from default gateway."
        exit 1
    }
    Write-Host "Host IP (gateway): $hostIp" -ForegroundColor Cyan

    # Use HTTP Streamable transport - no authentication needed (server binds to localhost)
    $mcpUrl = "http://${hostIp}:$McpPort"
    Write-Host "Configuring MCP approval server at $mcpUrl" -ForegroundColor Cyan

    # Create temporary MCP config file (no authentication header - server binds to localhost only)
    $mcpConfigPath = "$env:TEMP\mcp-config.json"
    $mcpConfig = @{
        'mcpServers' = @{
            'host-approval' = @{
                'type' = 'http'
                'url' = $mcpUrl
            }
        }
    }
    $mcpConfig | ConvertTo-Json -Depth 10 | Set-Content $mcpConfigPath -Encoding UTF8
    $mcpConfigArg = "--mcp-config `"$mcpConfigPath`""
    Write-Host "MCP config file created: $mcpConfigPath" -ForegroundColor Green
}

Write-Host "Starting Claude CLI..." -ForegroundColor Green

# Run Claude
if ($Prompt)
{
    # Write prompt to a temporary file to avoid command line length limits
    $promptFile = "$env:TEMP\claude-prompt-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8)).txt"
    $Prompt | Set-Content -Path $promptFile -Encoding UTF8 -NoNewline
    Write-Host "Running Claude with prompt from file: $promptFile" -ForegroundColor Cyan

    # Tag TeamCity build with the prompt
    if ($env:IS_TEAMCITY_AGENT -eq "true" -or $env:IS_TEAMCITY_AGENT -eq "1") {
        # Escape special characters for TeamCity service message format
        $tagValue = $Prompt -replace '\|','||' -replace "'","|'" -replace '\[','|[' -replace '\]','|]' -replace "`n",'|n' -replace "`r",'|r'
        # Truncate to avoid excessively long tags
        if ($tagValue.Length -gt 200) { $tagValue = $tagValue.Substring(0, 200) + "..." }
        Write-Host "##teamcity[addBuildTag '$tagValue']"
    }

    # Stream JSON output for human-readable real-time monitoring
    $processArgs = "-p --output-format stream-json --verbose --model $Model --dangerously-skip-permissions $mcpConfigArg"

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = "claude.cmd"
    $psi.Arguments = $processArgs
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::Start($psi)

    # Send prompt via stdin
    $promptContent = Get-Content -Path $promptFile -Raw
    $process.StandardInput.Write($promptContent)
    $process.StandardInput.Close()

    # Set up log file for raw JSON output
    $logDir = Join-Path (Resolve-Path "$PSScriptRoot\..").Path "artifacts\logs"
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    $timestamp = (Get-Date).ToString("yyyy-MM-dd-HHmmss")
    $logFile = Join-Path $logDir "claude-$timestamp.log.json"
    $logWriter = [System.IO.StreamWriter]::new($logFile, $false, [System.Text.Encoding]::UTF8)
    $logWriter.WriteLine("[")
    $isFirstJsonLine = $true

    # Read and parse stdout line by line (real-time streaming)
    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        # Write to log file in real-time
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            try {
                $obj = $line | ConvertFrom-Json
                $indented = $obj | ConvertTo-Json -Depth 100
                if (-not $isFirstJsonLine) {
                    $logWriter.WriteLine(",")
                }
                $logWriter.Write($indented)
                $isFirstJsonLine = $false
            } catch {
                # Non-JSON line - write as raw string
                if (-not $isFirstJsonLine) {
                    $logWriter.WriteLine(",")
                }
                $logWriter.Write("`"$($line -replace '\\','\\\\' -replace '"','\"')`"")
                $isFirstJsonLine = $false
            }
            $logWriter.Flush()
        }
        ConvertFrom-ClaudeJsonLine -Line $line
    }

    # Also capture stderr
    $stderr = $process.StandardError.ReadToEnd()
    if ($stderr) { Write-Host (Sanitize-ClaudeOutput $stderr) -ForegroundColor Red }

    $process.WaitForExit()
    $exitCode = $process.ExitCode

    # Close JSON log file
    $logWriter.WriteLine()
    $logWriter.WriteLine("]")
    $logWriter.Close()
    Write-Host "Claude output log: $logFile" -ForegroundColor Green

    # Clean up prompt file
    Remove-Item $promptFile -ErrorAction SilentlyContinue

    Write-Host "Claude exited with code $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
    exit $exitCode
}
else
{
    Write-Host "Running Claude in interactive mode" -ForegroundColor Cyan
    $cmd = "claude --model $Model --dangerously-skip-permissions $mcpConfigArg"
    Invoke-Expression $cmd
    exit $LASTEXITCODE
}
