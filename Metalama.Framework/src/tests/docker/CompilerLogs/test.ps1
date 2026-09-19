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

    # Enable Metalama compiler logging via environment variable.
    # This avoids having to resolve the platform-specific config directory.
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

    $env:METALAMA_DIAGNOSTICS = $diagnosticsJson
    Write-Host "METALAMA_DIAGNOSTICS set to: $env:METALAMA_DIAGNOSTICS"

    # Where Backstage keeps its temporary files, which is where the compiler writes its logs. The two
    # branches mirror StandardDirectories.TempDirectory: Windows keeps them under the system temporary
    # directory, while Unix moved to the local application data directory in #1650. Asking for the former
    # on Unix finds the pre-#1650 location, which nothing writes to any more.
    $logsDir = if ($IsWindows) {
        Join-Path ([System.IO.Path]::GetTempPath()) "Metalama"
    } else {
        Join-Path ([System.Environment]::GetFolderPath("LocalApplicationData")) "Metalama" "Temp"
    }

    # Clear any pre-existing logs
    if (Test-Path $logsDir) {
        Remove-Item -Recurse -Force $logsDir
    }

    # Restore and rebuild the project (force rebuild to ensure compilation happens)
    Write-Host "`nRestoring..."
    dotnet restore --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }

    Write-Host "`nBuilding..."
    dotnet build /t:Rebuild --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

    # Check for compiler log files
    Write-Host "`nSearching for Metalama log files..."
    Write-Host "Looking in: $logsDir"

    if (-not (Test-Path $logsDir)) {
        Write-Error "FAILURE: Metalama temp directory does not exist at $logsDir"
        exit 1
    }

    # Look specifically for compiler log files (Metalama-Compiler-*.log)
    $compilerLogFiles = Get-ChildItem -Path $logsDir -Recurse -Filter "Metalama-Compiler-*.log" -File -ErrorAction SilentlyContinue
    $allLogFiles = Get-ChildItem -Path $logsDir -Recurse -Filter "*.log" -File -ErrorAction SilentlyContinue

    Write-Host "`nAll log files found:"
    if ($allLogFiles) {
        $allLogFiles | ForEach-Object { Write-Host "  $($_.FullName) ($($_.Length) bytes)" }
    } else {
        Write-Host "  No .log files found."
        # Dump full directory listing only on failure to help diagnose
        Write-Host "`nAll files under Metalama temp directory:"
        Get-ChildItem -Path $logsDir -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "    $($_.FullName)"
        }
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
