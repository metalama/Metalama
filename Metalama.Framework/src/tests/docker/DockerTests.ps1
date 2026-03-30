param(
    [Parameter(Mandatory=$true)]
    [string]$Directory,

    [Parameter(Mandatory=$false)]
    [string]$Filter,

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

function ConvertTo-BashEscaped {
    param([string]$Value)

    # Escape single quotes by replacing ' with '\''
    return $Value -replace "'", "'\\''"
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

$testDirs = Get-ChildItem -Path $targetDir -Directory
if ($Filter) {
    $testDirs = $testDirs | Where-Object { $_.Name -like $Filter }
}
$testDirs | ForEach-Object {
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
        $scriptPath = ConvertTo-WslPath $dockerBuildScript
        $dockerfilePath = ConvertTo-WslPath $dockerfile
        $testScriptPath = ConvertTo-WslPath $testScript

        # This makes DockerBuld.ps1 receive the environment variables correctly.
        $command = "\`$env:IS_TEAMCITY_AGENT='$env:IS_TEAMCITY_AGENT'; \`$env:GIT_USER_EMAIL='$env:GIT_USER_EMAIL'; \`$env:GIT_USER_NAME ='$env:GIT_USER_NAME'; & '$scriptPath' -Dockerfile '$dockerfilePath' -NoInit -Script '$testScriptPath'"
        Write-Host "Executing in WSL: pwsh -Command `$'$command`$'"
        wsl pwsh -Command "$command"
    } else {
        & $dockerBuildScript -Dockerfile $dockerfile -NoInit -Script $testScript
    }

    if (-not $? -or $LASTEXITCODE -ne 0) {
        throw "Test failed for $($_.Name)"
    }
}
