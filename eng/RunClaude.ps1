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

# --- Resume-loop helpers ------------------------------------------------------------------
# In headless `-p` mode the process exits the instant the turn ends, with no way to wake it.
# The model occasionally ends its turn mid-build (e.g. "I'll wait for the background
# notification rather than poll"), which abandons all work. We cannot distinguish that from a
# genuine finish via the exit code (both are exit 0 / result.subtype == "success"), so we invert
# the logic: the run is considered DONE only when the model emits an explicit sentinel; any other
# ending is auto-resumed via `claude --resume <session_id>`, bounded by the guards below. Each
# resume waits a short delay first (the exit may signal a transient condition), and the "give up"
# guards (no-progress / max-iterations) are held off until a minimum runtime floor so a burst of
# fast exits cannot abandon the run within the first few minutes.

# Spawn one `claude` process, stream/log its stdout, and return what we need to decide whether
# to resume: the exit code, the (stable) session id, and any completion sentinel in the final text.
function Invoke-ClaudeOnce {
    param(
        [string]$Arguments,
        [string]$StdinContent,
        [string]$LogFile
    )

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $script:ClaudeExe
    $psi.Arguments = $Arguments
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::Start($psi)

    # Send the prompt (initial issue prompt, or the resume nudge) via stdin.
    if ($null -ne $StdinContent) { $process.StandardInput.Write($StdinContent) }
    $process.StandardInput.Close()

    $logWriter = [System.IO.StreamWriter]::new($LogFile, $false, [System.Text.Encoding]::UTF8)
    $logWriter.WriteLine("[")
    $isFirstJsonLine = $true

    $sessionId = $null
    $resultText = $null
    $resultSubtype = $null
    $resultIsError = $false
    $lastAssistantText = $null

    # Read and parse stdout line by line (real-time streaming).
    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            try {
                $obj = $line | ConvertFrom-Json
                $indented = $obj | ConvertTo-Json -Depth 100
                if (-not $isFirstJsonLine) { $logWriter.WriteLine(",") }
                $logWriter.Write($indented)
                $isFirstJsonLine = $false

                # session_id rides on most events and is stable across resumes; capture the latest.
                if ($obj.session_id) { $sessionId = $obj.session_id }

                # Final result event carries the model's final text + status.
                if ($obj.type -eq 'result') {
                    if ($null -ne $obj.result) { $resultText = [string]$obj.result }
                    if ($obj.subtype) { $resultSubtype = [string]$obj.subtype }
                    if ($null -ne $obj.is_error) { $resultIsError = [bool]$obj.is_error }
                }

                # Fallback for stream-json schema uncertainty: remember the last assistant text block.
                if ($obj.type -eq 'assistant' -and $obj.message -and $obj.message.content) {
                    foreach ($block in $obj.message.content) {
                        if ($block.type -eq 'text' -and $block.text) { $lastAssistantText = [string]$block.text }
                    }
                }
            } catch {
                # Non-JSON line - write as raw string
                if (-not $isFirstJsonLine) { $logWriter.WriteLine(",") }
                $logWriter.Write("`"$($line -replace '\\','\\\\' -replace '"','\"')`"")
                $isFirstJsonLine = $false
            }
            $logWriter.Flush()
        }
        ConvertFrom-ClaudeJsonLine -Line $line
    }

    $stderr = $process.StandardError.ReadToEnd()
    if ($stderr) { Write-Host (Sanitize-ClaudeOutput $stderr) -ForegroundColor Red }

    $process.WaitForExit()
    $exitCode = $process.ExitCode

    $logWriter.WriteLine()
    $logWriter.WriteLine("]")
    $logWriter.Close()

    # Detect the completion sentinel in the model's final message (result text, else last assistant text).
    $scanText = if ($resultText) { $resultText } else { $lastAssistantText }
    $sentinel = $null
    if ($scanText) {
        if ($scanText -match '<promptly-done/>') { $sentinel = 'done' }
        elseif ($scanText -match '<promptly-blocked/>') { $sentinel = 'blocked' }
    }

    return @{
        ExitCode      = $exitCode
        SessionId     = $sessionId
        Sentinel      = $sentinel
        ResultIsError = $resultIsError
        ResultSubtype = $resultSubtype
    }
}

# Discover git repos to watch for progress (handles both the source-dependencies and sibling layouts).
function Get-GitRepos {
    param([string]$RepoRoot)

    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add($RepoRoot)

    $srcDeps = Join-Path $RepoRoot "source-dependencies"
    if (Test-Path $srcDeps) {
        Get-ChildItem -Path $srcDeps -Directory -ErrorAction SilentlyContinue | ForEach-Object { $candidates.Add($_.FullName) }
    }

    $parent = Split-Path $RepoRoot -Parent
    if ($parent -and (Test-Path $parent)) {
        Get-ChildItem -Path $parent -Directory -ErrorAction SilentlyContinue | ForEach-Object { $candidates.Add($_.FullName) }
    }

    $repos = @{}
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c ".git")) { $repos[$c] = $true }
    }
    return $repos.Keys
}

# Snapshot HEAD of each watched repo. A HEAD that advances == the model committed == progress.
function Get-RepoHeads {
    param([string[]]$Repos)

    $heads = @{}
    foreach ($r in $Repos) {
        try {
            $sha = (& git -C $r rev-parse HEAD 2>$null)
            if ($LASTEXITCODE -eq 0 -and $sha) { $heads[$r] = $sha.Trim() }
        } catch { }
    }
    return $heads
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

    # Stream JSON output for human-readable real-time monitoring.
    # In headless `-p` mode the process exits at the `result` event, so a scheduled wakeup /
    # cron / remote trigger can never fire and any work deferred to it is abandoned. We disallow
    # those, AND -- because the model can still simply END its turn mid-build -- the resume loop
    # below re-invokes `claude --resume` whenever a turn ends without a completion sentinel.
    # Do NOT disallow Monitor or run_in_background -- those are the in-turn wait mechanisms long
    # builds depend on.
    $disallowedTools = "ScheduleWakeup CronCreate CronDelete CronList RemoteTrigger"
    $commonArgs = "--output-format stream-json --verbose --model $Model --dangerously-skip-permissions --disallowedTools `"$disallowedTools`" $mcpConfigArg"

    # Resolve the Claude CLI launcher: npm ships claude.cmd on Windows, the native installer ships
    # claude.exe, and on Linux/macOS the binary is plain "claude". Get-Command honors PATHEXT so
    # asking for "claude" without an extension finds whichever variant is on PATH.
    $claudeCommand = Get-Command claude.cmd -ErrorAction SilentlyContinue
    if (-not $claudeCommand) { $claudeCommand = Get-Command claude.exe -ErrorAction SilentlyContinue }
    if (-not $claudeCommand) { $claudeCommand = Get-Command claude -ErrorAction SilentlyContinue }
    if (-not $claudeCommand) {
        Write-Error "Claude CLI not found on PATH. Install it via 'npm i -g @anthropic-ai/claude-code' or the native installer."
        exit 1
    }
    $script:ClaudeExe = $claudeCommand.Source
    Write-Host "Using Claude executable: $script:ClaudeExe" -ForegroundColor Cyan

    $promptContent = Get-Content -Path $promptFile -Raw

    $repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
    $logDir = Join-Path $repoRoot "artifacts\logs"
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null

    # Resume-loop guards (env-overridable) so a stuck model cannot loop forever.
    $maxIterations = if ($env:CLAUDE_MAX_ITERATIONS) { [int]$env:CLAUDE_MAX_ITERATIONS } else { 8 }
    $maxMinutes    = if ($env:CLAUDE_MAX_MINUTES)    { [int]$env:CLAUDE_MAX_MINUTES }    else { 120 }
    $maxNoProgress = 2
    # Floor before any "give up" guard (no-progress / max-iterations) may fire: a fast-exiting or
    # crashing claude must be retried for at least this long before we conclude it is truly stuck.
    $minMinutes        = if ($env:CLAUDE_MIN_MINUTES)         { [int]$env:CLAUDE_MIN_MINUTES }         else { 10 }
    # Pause before each resume -- claude exited for a reason (rate limit, transient error, etc.),
    # so give that condition a moment to clear instead of hammering an immediate retry.
    $retryDelaySeconds = if ($env:CLAUDE_RETRY_DELAY_SECONDS) { [int]$env:CLAUDE_RETRY_DELAY_SECONDS } else { 30 }

    $gitRepos = @(Get-GitRepos -RepoRoot $repoRoot)
    Write-Host "Monitoring $($gitRepos.Count) git repo(s) for progress between iterations." -ForegroundColor Cyan

    $startTime = Get-Date
    $sessionId = $null
    $iteration = 0
    $noProgressStreak = 0
    $finalExitCode = 1
    $stopReason = "unknown"

    while ($true) {
        $iteration++
        $elapsedMin = [int]((Get-Date) - $startTime).TotalMinutes
        $remainingMin = [Math]::Max(0, $maxMinutes - $elapsedMin)

        Write-Host ""
        Write-Host "=== Claude iteration $iteration (elapsed ${elapsedMin}m, ~${remainingMin}m budget left) ===" -ForegroundColor Magenta

        # Snapshot HEADs before the turn so we can detect whether it made any progress.
        $headsBefore = Get-RepoHeads -Repos $gitRepos

        $timestamp = (Get-Date).ToString("yyyy-MM-dd-HHmmss")
        $logFile = Join-Path $logDir "claude-$timestamp.log.json"

        if ($iteration -eq 1) {
            # First turn: the original issue prompt, fresh session.
            $claudeArgs = "-p $commonArgs"
            $stdinContent = $promptContent
        }
        else {
            # Resume turn: re-enter the SAME session and nudge the model to continue to completion.
            $stdinContent = @"
Your previous turn ended without a completion sentinel, so your work is presumed incomplete. Resume exactly where you left off, following CLAUDE.md instructions and phases strictly. You are running headless: never wait for a notification, schedule a wakeup, or defer work across turns -- keep working WITHIN this turn and poll background builds until they finish. About $remainingMin minutes of budget remain. End your run ONLY by emitting the literal token <promptly-done/> (all CLAUDE.md phases complete and PRs ready) or <promptly-blocked/> (genuinely blocked after at least 5 distinct attempts and a blocker comment posted to the issue).
"@
            $claudeArgs = "--resume $sessionId -p $commonArgs"
        }

        $result = Invoke-ClaudeOnce -Arguments $claudeArgs -StdinContent $stdinContent -LogFile $logFile
        Write-Host "Claude output log: $logFile" -ForegroundColor Green
        Write-Host "Iteration $iteration exited with code $($result.ExitCode); sentinel='$($result.Sentinel)'; session=$($result.SessionId)" -ForegroundColor Cyan

        if ($result.SessionId) { $sessionId = $result.SessionId }

        # Terminal: model emitted an explicit completion sentinel.
        if ($result.Sentinel -eq 'done')    { $finalExitCode = 0; $stopReason = "done"; break }
        if ($result.Sentinel -eq 'blocked') { $finalExitCode = 0; $stopReason = "blocked"; break }

        # Cannot resume without a session id (e.g. claude crashed before emitting one).
        if (-not $sessionId) {
            Write-Host "No session id captured; cannot resume." -ForegroundColor Red
            $finalExitCode = $result.ExitCode; $stopReason = "no-session-id"; break
        }

        # Guard: wall-clock budget.
        $elapsedMin = [int]((Get-Date) - $startTime).TotalMinutes
        if ($elapsedMin -ge $maxMinutes) {
            Write-Host "Wall-clock budget of ${maxMinutes}m exhausted." -ForegroundColor Red
            $finalExitCode = 1; $stopReason = "budget-exhausted"; break
        }

        # Guard: max iterations. Held off until the minimum runtime floor so a burst of fast
        # exits cannot exhaust the iteration budget within the first few minutes.
        if ($iteration -ge $maxIterations -and $elapsedMin -ge $minMinutes) {
            Write-Host "Reached max iterations ($maxIterations) after ${elapsedMin}m." -ForegroundColor Red
            $finalExitCode = 1; $stopReason = "max-iterations"; break
        }

        # Guard: no-progress. If no watched repo's HEAD advanced, the turn committed nothing.
        $headsAfter = Get-RepoHeads -Repos $gitRepos
        $madeProgress = $false
        foreach ($repo in $headsAfter.Keys) {
            if (-not $headsBefore.ContainsKey($repo) -or $headsBefore[$repo] -ne $headsAfter[$repo]) { $madeProgress = $true; break }
        }
        if ($madeProgress) { $noProgressStreak = 0 } else { $noProgressStreak++ }
        Write-Host "Progress this iteration: $madeProgress (no-progress streak: $noProgressStreak/$maxNoProgress)" -ForegroundColor Cyan
        # Held off until the minimum runtime floor: a model that exits fast without committing
        # (e.g. repeated crashes) still gets at least $minMinutes of retries before we give up.
        if ($noProgressStreak -ge $maxNoProgress -and $elapsedMin -ge $minMinutes) {
            Write-Host "No progress for $maxNoProgress consecutive iterations after ${elapsedMin}m; stopping to avoid a stuck loop." -ForegroundColor Red
            $finalExitCode = 1; $stopReason = "stuck-no-progress"; break
        }

        # Pause before resuming -- claude exited for a reason, so let any transient condition clear.
        if ($retryDelaySeconds -gt 0) {
            Write-Host "Waiting ${retryDelaySeconds}s before resuming..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $retryDelaySeconds
        }
        Write-Host "No completion sentinel -- resuming Claude session $sessionId." -ForegroundColor Yellow
    }

    # Clean up prompt file
    Remove-Item $promptFile -ErrorAction SilentlyContinue

    $color = if ($finalExitCode -eq 0) { "Green" } else { "Red" }
    Write-Host "Claude run finished: reason=$stopReason, iterations=$iteration, exitCode=$finalExitCode" -ForegroundColor $color
    exit $finalExitCode
}
else
{
    Write-Host "Running Claude in interactive mode" -ForegroundColor Cyan
    $cmd = "claude --model $Model --dangerously-skip-permissions $mcpConfigArg"
    Invoke-Expression $cmd
    exit $LASTEXITCODE
}
