# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

param(
    [string]$Prompt,
    [int]$McpPort
)

$ErrorActionPreference = "Stop"

if ($env:RUNNING_IN_DOCKER -ne "true")
{
    Write-Error "This script must be run inside a Docker container. Set RUNNING_IN_DOCKER=true to override."
    exit 1
}

# Configure MCP approval server if port is specified
$mcpConfigArg = ""
if ($McpPort -gt 0)
{
    # Get MCP secret from environment variable
    $mcpSecret = $env:MCP_APPROVAL_SERVER_TOKEN
    if ( [string]::IsNullOrEmpty($mcpSecret))
    {
        Write-Error "MCP_APPROVAL_SERVER_TOKEN environment variable is not set. Cannot authenticate to MCP server."
        exit 1
    }

    # URL-encode the secret for path segment
    $encodedSecret = [System.Web.HttpUtility]::UrlEncode($mcpSecret)

    # On Windows containers, host.docker.internal doesn't resolve.
    # Use the default gateway IP which points to the host.
    $hostIp = (Get-NetRoute -DestinationPrefix '0.0.0.0/0' | Select-Object -First 1).NextHop
    if ([string]::IsNullOrEmpty($hostIp))
    {
        Write-Error "Could not determine host IP from default gateway."
        exit 1
    }
    Write-Host "Host IP (gateway): $hostIp" -ForegroundColor Cyan

    $sseUrl = "http://${hostIp}:$McpPort/$encodedSecret/sse"
    Write-Host "Configuring MCP approval server (authenticated)" -ForegroundColor Cyan

    # Create temporary MCP config file
    $mcpConfigPath = "$env:TEMP\mcp-config.json"
    $mcpConfig = @{
        'mcpServers' = @{
            'host-approval' = @{
                'type' = 'sse'
                'url' = $sseUrl
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
    Write-Host "Running Claude with prompt: $Prompt" -ForegroundColor Cyan
    $cmd = "claude --dangerously-skip-permissions $mcpConfigArg -p `"$Prompt`""
    Invoke-Expression $cmd
}
else
{
    Write-Host "Running Claude in interactive mode" -ForegroundColor Cyan
    $cmd = "claude --dangerously-skip-permissions $mcpConfigArg"
    Invoke-Expression $cmd
}

exit $LASTEXITCODE
