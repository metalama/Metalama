param(
    [Parameter(Mandatory=$true)]
    [string]$Directory,

    [Parameter(Mandatory=$false)]
    [switch]$Wsl
)

$ErrorActionPreference = 'Stop'

function ConvertTo-WslPath {
    param([string]$WindowsPath)

    # Convert backslashes to forward slashes
    $wslPath = $WindowsPath -replace '\\', '/'

    # Convert drive letter (e.g., C: -> /mnt/c)
    if ($wslPath -match '^([A-Z]):') {
        $driveLetter = $matches[1].ToLower()
        $wslPath = $wslPath -replace '^[A-Z]:', "/mnt/$driveLetter"
    }

    return $wslPath
}

$scriptDir = $PSScriptRoot
$targetDir = Join-Path $scriptDir $Directory

if (-not (Test-Path $targetDir -PathType Container)) {
    throw "Directory not found: $targetDir"
}

# Find DockerBuild.ps1 in parent directories
$dockerBuildScript = $null
$currentDir = $scriptDir
while ($currentDir) {
    $candidate = Join-Path $currentDir "DockerBuild.ps1"
    if (Test-Path $candidate) {
        $dockerBuildScript = $candidate
        break
    }
    $parent = Split-Path $currentDir -Parent
    if ($parent -eq $currentDir) { break }
    $currentDir = $parent
}

if (-not $dockerBuildScript) {
    throw "DockerBuild.ps1 not found in any parent directory"
}

Get-ChildItem -Path $targetDir -Directory | ForEach-Object {
    $dir = $_.FullName
    $dockerfile = Join-Path $dir "Dockerfile"
    $testScript = Join-Path $dir "test.ps1"

    if (-not (Test-Path $dockerfile)) {
        throw "Dockerfile not found in $dir"
    }

    if (-not (Test-Path $testScript)) {
        throw "test.ps1 not found in $dir"
    }

    Write-Host "Running test for $($_.Name)..."

    if ($Wsl) {
        # Run DockerBuild.ps1 under WSL
        $wslDockerBuildScript = ConvertTo-WslPath $dockerBuildScript
        $wslDockerfile = ConvertTo-WslPath $dockerfile
        $wslTestScript = ConvertTo-WslPath $testScript

        # Transfer IS_TEAMCITY_AGENT environment variable to WSL
        $tcAgentValue = $env:IS_TEAMCITY_AGENT
        if ($tcAgentValue) {
            wsl pwsh -Command "`$env:IS_TEAMCITY_AGENT='$tcAgentValue'; & '$wslDockerBuildScript' -Dockerfile '$wslDockerfile' -NoInit -Script '$wslTestScript'"
        } else {
            wsl pwsh -File $wslDockerBuildScript -Dockerfile $wslDockerfile -NoInit -Script $wslTestScript
        }
    } else {
        & $dockerBuildScript -Dockerfile $dockerfile -NoInit -Script $testScript
    }

    if (-not $? -or $LASTEXITCODE -ne 0) {
        throw "Test failed for $($_.Name)"
    }
}
