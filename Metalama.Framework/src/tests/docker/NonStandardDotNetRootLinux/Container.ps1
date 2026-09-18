$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try {
    # Find nuget.wsl.config in parent directories
    $currentDir = $PSScriptRoot
    $nugetConfig = $null

    while ($currentDir) {
        # Two names are tried, in this order. On a development machine the Linux engine is the one inside WSL,
        # so the local feeds are reached by their /mnt paths and Build.ps1 writes those into nuget.wsl.config.
        # On a Linux agent there is no such translation and no such file: the harness copies
        # nuget.restored.config to the repository root as nuget.config, whose paths are already native.
        foreach ($candidateName in @("nuget.wsl.config", "nuget.config")) {
            $candidatePath = Join-Path $currentDir $candidateName
            if (Test-Path $candidatePath) {
                $nugetConfig = $candidatePath
                break
            }
        }

        if ($nugetConfig) {
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
        Write-Error "Could not find nuget.wsl.config or nuget.config in any parent directory. Prepare the repository before running the Docker tests."
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
