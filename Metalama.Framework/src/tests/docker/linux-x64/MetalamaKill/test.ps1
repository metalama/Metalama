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
            break
        }
        $currentDir = $parentDir
    }

    if (-not $nugetConfig) {
        Write-Error "Could not find nuget.wsl.config in any parent directory"
        exit 1
    }

    Write-Host "Restoring..."
    dotnet restore --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }

    # Install the Metalama dotnet tool from local packages
    # Extract the local package source from nuget.wsl.config
    $nugetXml = [xml](Get-Content $nugetConfig)
    $metalamaSource = ($nugetXml.configuration.packageSources.add | Where-Object { $_.key -eq 'Metalama' }).value
    Write-Host "Metalama package source: $metalamaSource"

    # Extract Metalama.Tool from the nupkg to run it directly via 'dotnet exec'.
    # We cannot use 'dotnet tool install' because it has a case-sensitivity bug on Linux
    # when the package version string contains mixed-case characters (e.g. local build suffixes).
    $toolNupkg = Get-ChildItem -Path $metalamaSource -Filter "Metalama.Tool.*.nupkg" | Select-Object -First 1
    if (-not $toolNupkg) {
        throw "Metalama.Tool nupkg not found in $metalamaSource"
    }
    Write-Host "Found tool package: $($toolNupkg.Name)"

    $toolExtractDir = "/tmp/metalama-tool"
    if (Test-Path $toolExtractDir) { Remove-Item -Recurse -Force $toolExtractDir }
    New-Item -ItemType Directory -Force -Path $toolExtractDir | Out-Null

    # nupkg is a zip file - extract it
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($toolNupkg.FullName, $toolExtractDir)

    # Find the metalama DLL inside the extracted package
    $toolDll = Get-ChildItem -Path $toolExtractDir -Recurse -Filter "metalama.dll" | Select-Object -First 1
    if (-not $toolDll) {
        Write-Host "Contents of extracted package:"
        Get-ChildItem -Path $toolExtractDir -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" }
        throw "metalama.dll not found in extracted Metalama.Tool package"
    }
    Write-Host "Metalama tool DLL: $($toolDll.FullName)"

    # Create a wrapper function for 'metalama' command
    function Invoke-Metalama {
        dotnet exec $toolDll.FullName @args
        return $LASTEXITCODE
    }

    # Verify metalama tool is available
    Invoke-Metalama version
    if ($LASTEXITCODE -ne 0) { throw "metalama tool not available" }

    # Build the project - this will start VBCSCompiler
    Write-Host "`nBuilding project (this starts VBCSCompiler)..."
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

    # Check for VBCSCompiler processes before kill
    Write-Host "`nProcesses before 'metalama kill':"
    $beforeProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    $vbcsBeforeCount = 0
    foreach ($proc in $beforeProcesses) {
        try {
            # On Linux, check /proc/<pid>/cmdline to find VBCSCompiler
            $cmdline = Get-Content "/proc/$($proc.Id)/cmdline" -Raw -ErrorAction SilentlyContinue
            if ($cmdline -and $cmdline -match "VBCSCompiler") {
                Write-Host "  VBCSCompiler process found: PID $($proc.Id)"
                Write-Host "    cmdline: $($cmdline -replace '\0', ' ')"
                $vbcsBeforeCount++
            }
        } catch {
            # Process may have exited
        }
    }

    if ($vbcsBeforeCount -eq 0) {
        Write-Host "  No VBCSCompiler processes found before kill."
        Write-Host "  (VBCSCompiler may not have stayed running - trying to keep it alive with nodeReuse)"

        # Retry build with explicit node reuse to keep VBCSCompiler alive
        Write-Host "`nRebuilding with node reuse enabled..."
        dotnet build --no-restore /p:UseSharedCompilation=true
        if ($LASTEXITCODE -ne 0) { throw "dotnet build (retry) failed with exit code $LASTEXITCODE" }

        Start-Sleep -Seconds 2

        $beforeProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        foreach ($proc in $beforeProcesses) {
            try {
                $cmdline = Get-Content "/proc/$($proc.Id)/cmdline" -Raw -ErrorAction SilentlyContinue
                if ($cmdline -and $cmdline -match "VBCSCompiler") {
                    Write-Host "  VBCSCompiler process found: PID $($proc.Id)"
                    $vbcsBeforeCount++
                }
            } catch { }
        }
    }

    if ($vbcsBeforeCount -eq 0) {
        Write-Host "WARNING: No VBCSCompiler processes found. The test may be inconclusive."
        Write-Host "Listing all dotnet processes for diagnostics:"
        foreach ($proc in (Get-Process -Name "dotnet" -ErrorAction SilentlyContinue)) {
            try {
                $cmdline = Get-Content "/proc/$($proc.Id)/cmdline" -Raw -ErrorAction SilentlyContinue
                Write-Host "  PID $($proc.Id): $($cmdline -replace '\0', ' ')"
            } catch { }
        }
        # Even without VBCSCompiler running, still run kill to verify it doesn't crash
    }

    Write-Host "`nRunning 'metalama kill --verbose'..."
    & dotnet exec $toolDll.FullName kill --verbose
    $killExitCode = $LASTEXITCODE
    Write-Host "Exit code: $killExitCode"

    if ($killExitCode -ne 0) { throw "metalama kill failed with exit code $killExitCode" }

    # Wait a moment for processes to terminate
    Start-Sleep -Seconds 3

    # Check for VBCSCompiler processes after kill
    Write-Host "`nProcesses after 'metalama kill':"
    $afterProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    $vbcsAfterCount = 0
    foreach ($proc in $afterProcesses) {
        try {
            $cmdline = Get-Content "/proc/$($proc.Id)/cmdline" -Raw -ErrorAction SilentlyContinue
            if ($cmdline -and $cmdline -match "VBCSCompiler") {
                Write-Host "  VBCSCompiler STILL RUNNING: PID $($proc.Id)"
                Write-Host "    cmdline: $($cmdline -replace '\0', ' ')"
                $vbcsAfterCount++
            }
        } catch { }
    }

    if ($vbcsBeforeCount -gt 0 -and $vbcsAfterCount -gt 0) {
        Write-Error "FAILURE: 'metalama kill' did not kill $vbcsAfterCount VBCSCompiler process(es) on Linux"
        exit 1
    }
    elseif ($vbcsBeforeCount -gt 0 -and $vbcsAfterCount -eq 0) {
        Write-Host "`nSUCCESS: All VBCSCompiler processes were killed by 'metalama kill'"
        exit 0
    }
    else {
        Write-Host "`nFAILURE: No VBCSCompiler processes were running before 'metalama kill'"
        Write-Host "The test could not verify kill behavior. This may indicate VBCSCompiler is not persisting on this configuration."
        exit 1
    }
}
finally {
    Pop-Location
}
