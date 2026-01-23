param(
    [Parameter(Mandatory=$true)]
    [string]$Directory
)

$ErrorActionPreference = 'Stop'

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
    & $dockerBuildScript -Dockerfile $dockerfile -NoInit -Script $testScript

    if ($LASTEXITCODE -ne 0) {
        throw "Test failed for $($_.Name)"
    }
}
