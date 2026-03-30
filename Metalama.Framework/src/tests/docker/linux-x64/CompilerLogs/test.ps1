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

    # Determine the Metalama config directory on Linux.
    # On Ubuntu, LocalApplicationData resolves to $HOME/.local/share.
    # Metalama uses LocalApplicationData + "Metalama", or falls back to $HOME/.metalama.
    $localAppData = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
    if ([string]::IsNullOrEmpty($localAppData)) {
        $configDir = Join-Path $HOME ".metalama"
    } else {
        $configDir = Join-Path $localAppData "Metalama"
    }

    Write-Host "Metalama config directory: $configDir"

    # Create the config directory
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null

    # Write diagnostics.json to enable compiler logging
    $diagnosticsJson = @{
        logging = @{
            processes = @{
                Compiler = $true
                BackstageWorker = $true
            }
            trace = @{
                "*" = $true
            }
            stopLoggingAfterHours = 24
        }
    } | ConvertTo-Json -Depth 5

    $diagnosticsFile = Join-Path $configDir "diagnostics.json"
    Write-Host "Writing diagnostics config to: $diagnosticsFile"
    Set-Content -Path $diagnosticsFile -Value $diagnosticsJson
    Write-Host "Diagnostics config content:"
    Get-Content $diagnosticsFile

    # Clear any pre-existing logs
    $logsDir = Join-Path ([System.IO.Path]::GetTempPath()) "Metalama"
    if (Test-Path $logsDir) {
        Remove-Item -Recurse -Force $logsDir
    }

    # Restore and build the project
    Write-Host "`nRestoring..."
    dotnet restore --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }

    Write-Host "`nBuilding..."
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

    # Check for compiler log files
    Write-Host "`nSearching for Metalama log files..."
    $logsDir = Join-Path ([System.IO.Path]::GetTempPath()) "Metalama"
    Write-Host "Looking in: $logsDir"

    if (-not (Test-Path $logsDir)) {
        Write-Error "FAILURE: Metalama temp directory does not exist at $logsDir"
        exit 1
    }

    # List all files under the Metalama temp directory for diagnostics
    Write-Host "`nAll files under Metalama temp directory:"
    Get-ChildItem -Path $logsDir -Recurse -File | ForEach-Object {
        Write-Host "  $($_.FullName) ($($_.Length) bytes)"
    }

    # Look specifically for compiler log files (Metalama-Compiler-*.log)
    $compilerLogFiles = Get-ChildItem -Path $logsDir -Recurse -Filter "Metalama-Compiler-*.log" -File -ErrorAction SilentlyContinue
    $allLogFiles = Get-ChildItem -Path $logsDir -Recurse -Filter "*.log" -File -ErrorAction SilentlyContinue

    Write-Host "`nAll log files found:"
    if ($allLogFiles) {
        $allLogFiles | ForEach-Object { Write-Host "  $($_.FullName) ($($_.Length) bytes)" }
    } else {
        $allFiles = Get-ChildItem -Path $logsDir -Recurse -File -ErrorAction SilentlyContinue
        Write-Host "  No .log files. All files in Metalama temp:"
        $allFiles | ForEach-Object { Write-Host "    $($_.FullName)" }
    }

    if (-not $compilerLogFiles -or $compilerLogFiles.Count -eq 0) {
        Write-Error "FAILURE: No compiler log files (Metalama-Compiler-*.log) were created under $logsDir"
        exit 1
    }

    # Verify compiler log files are non-empty
    $nonEmptyCompilerLogs = 0
    foreach ($logFile in $compilerLogFiles) {
        $content = Get-Content $logFile.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            Write-Host "`nCompiler log file: $($logFile.FullName) ($($logFile.Length) bytes)"
            # Show first few lines for diagnostics
            $lines = Get-Content $logFile.FullName -TotalCount 10
            $lines | ForEach-Object { Write-Host "  $_" }
            $nonEmptyCompilerLogs++
        }
    }

    if ($nonEmptyCompilerLogs -eq 0) {
        Write-Error "FAILURE: Compiler log files exist but are all empty"
        exit 1
    }

    Write-Host "`nSUCCESS: $nonEmptyCompilerLogs compiler log file(s) were created on Ubuntu"
    exit 0
}
finally {
    Pop-Location
}
