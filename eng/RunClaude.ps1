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

    # Use stdin redirection to pass the prompt, avoiding command line length issues
    $cmd = "Get-Content -Path '$promptFile' -Raw | claude --model $Model --dangerously-skip-permissions $mcpConfigArg"
    $exitCode = Invoke-Expression $cmd

    # Clean up prompt file
    Remove-Item $promptFile -ErrorAction SilentlyContinue
    exit $exitCode
}
else
{
    Write-Host "Running Claude in interactive mode" -ForegroundColor Cyan
    $cmd = "claude --model $Model --dangerously-skip-permissions $mcpConfigArg"
    Invoke-Expression $cmd
    exit $LASTEXITCODE
}
