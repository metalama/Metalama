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

    # The processes of the compiler server, found from their command line: VBCSCompiler runs under 'dotnet' in some SDKs
    # and as its own application host in others, so the process name alone does not find it.
    function Get-CompilerServerProcessIds {
        $ids = @()
        foreach ($directory in Get-ChildItem /proc -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d+$' }) {
            try {
                $cmdline = Get-Content "/proc/$($directory.Name)/cmdline" -Raw -ErrorAction Stop
                if ($cmdline -and $cmdline -match 'VBCSCompiler(\.dll)?(\x00|$)') {
                    $ids += [int] $directory.Name
                }
            } catch {
                # The process exited meanwhile.
            }
        }
        return $ids
    }

    # Builds the project so that it leaves the compiler server running, and fails when it does not, because the test
    # would then verify nothing.
    function Start-CompilerServer {
        Write-Host "`nBuilding the project with the shared compiler, which leaves VBCSCompiler running..."
        # Out-Host, so that the output of the build is not returned with the identifiers of the processes.
        dotnet build --no-restore --no-incremental /p:UseSharedCompilation=true | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

        $ids = Get-CompilerServerProcessIds
        if ($ids.Count -eq 0) { throw "The build left no VBCSCompiler process running, so there is nothing to shut down." }
        Write-Host "VBCSCompiler running: $($ids -join ', ')"
        return $ids
    }

    # Runs the tool and returns its exit code and output, which is also written to the console.
    function Invoke-MetalamaCommand {
        $output = & dotnet exec $toolDll.FullName @args 2>&1 | ForEach-Object { "$_" }
        $exitCode = $LASTEXITCODE
        $output | ForEach-Object { Write-Host "  $_" }
        return [pscustomobject] @{ ExitCode = $exitCode; Output = ($output -join "`n") }
    }

    # 1. 'metalama shutdown' asks the compiler server to exit, lets it exit on its own, and reports it as exited, not
    #    ended. Without --force, a process that does not exit in time is reported as still running and the exit code is 1.
    $ids = Start-CompilerServer

    Write-Host "`nRunning 'metalama shutdown'..."
    $result = Invoke-MetalamaCommand shutdown --timeout 60
    if ($result.ExitCode -ne 0) { throw "'metalama shutdown' failed with exit code $($result.ExitCode)." }

    foreach ($id in $ids) {
        if ($result.Output -notmatch "Compiler server \(VBCSCompiler\) \(process $id\): exited\.") {
            throw "'metalama shutdown' did not report the compiler server $id as having exited on request."
        }
    }

    $remaining = Get-CompilerServerProcessIds
    if ($remaining.Count -ne 0) { throw "VBCSCompiler is still running after 'metalama shutdown': $($remaining -join ', ')." }

    Write-Host "SUCCESS: 'metalama shutdown' stopped the compiler server gracefully."

    # 2. 'metalama kill --force' is the same command with --force: it stops the compiler server too, and ends the
    #    processes that do not exit.
    $ids = Start-CompilerServer

    Write-Host "`nRunning 'metalama kill --force'..."
    $result = Invoke-MetalamaCommand kill --force --timeout 60
    if ($result.ExitCode -ne 0) { throw "'metalama kill --force' failed with exit code $($result.ExitCode)." }

    $remaining = Get-CompilerServerProcessIds
    if ($remaining.Count -ne 0) { throw "VBCSCompiler is still running after 'metalama kill --force': $($remaining -join ', ')." }

    Write-Host "SUCCESS: 'metalama kill --force' stopped the compiler server."
    exit 0
}

finally {
    Pop-Location
}
