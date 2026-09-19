$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try {
    # Find nuget.wsl.config in parent directories
    $currentDir = $PSScriptRoot
    $nugetConfig = $null

    while ($currentDir) {
        $candidatePath = Join-Path $currentDir "nuget.wsl.config"
        if (Test-Path $candidatePath) {
            $nugetConfig = $candidatePath
            break
        }

        $parentDir = Split-Path $currentDir -Parent
        if ($parentDir -eq $currentDir) {
            # Reached root
            break
        }
        $currentDir = $parentDir
    }

    if (-not $nugetConfig) {
        Write-Error "Could not find nuget.wsl.config in any parent directory"
        exit 1
    }

    Write-Host "Restoring using NuGet config: $nugetConfig"
    /opt/custom-dotnet/dotnet restore --configfile $nugetConfig
    Write-Host "Building and running"
    /opt/custom-dotnet/dotnet run --no-restore $nugetConfig
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
