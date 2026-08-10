# Regression test for https://github.com/metalama/Metalama/issues/272.
#
# Metalama acquires machine-wide named mutexes (the "Global\" prefix) while initializing its
# Backstage services. On Unix the .NET runtime implements those mutexes with files under
# /tmp/.dotnet/shm, and when that tree cannot be used the runtime throws
# System.IO.IOException (ERROR_OPEN_FAILED, HRESULT 0x8007006E). Metalama does not handle that
# exception, so the whole compilation fails with LAMA0623 and the user cannot build at all.
#
# The condition is reproduced here by replacing /tmp/.dotnet with a regular file. The .NET
# runtime's SharedMemoryHelpers::EnsureDirectoryExists then finds a path that exists but is not a
# directory and raises SharedMemoryError::IO. This reproduction was chosen because it is
# deterministic, does not depend on the mutex name (Metalama derives its names by hashing
# machine-specific paths) and does not depend on the effective user, unlike reproductions based on
# ownership or permissions, which a root user in a container would bypass.
#
# The reports of this issue all come from macOS, but nothing about it is specific to macOS:
# the runtime code that raises the exception, src/coreclr/pal/src/sharedmemory/sharedmemory.cpp,
# contains no platform conditionals. macOS is affected more often because it does not support
# robust process-shared pthread mutexes, so it maintains a second tree, /tmp/.dotnet/lockfiles,
# that Linux never creates.
#
# The intended behaviour, which this test asserts, is that Metalama degrades instead of failing:
# Metalama.Backstage.Threading.NamedLockService catches the IOException, reports a
# LockEventKind.Degraded event, and returns a lock backed by a monitor of the current process. Mutual
# exclusion between processes is lost, which every caller tolerates, and the build succeeds.
#
# THIS TEST IS EXPECTED TO FAIL UNTIL EVERY CALLER HAS BEEN MOVED TO THAT SERVICE. ResourceExtractor,
# which runs first, has been. Metalama.Backstage.Configuration.ConfigurationManager still opens its
# mutex through MutexHelper, which lets the IOException escape, so Backstage initialization still
# fails after the extraction has succeeded.

$ErrorActionPreference = 'Stop'

# This test has to inspect the exit code and the output of a build that is expected to fail. In
# PowerShell 7.4 and later a native command that returns a non-zero exit code raises a terminating
# error when $ErrorActionPreference is 'Stop', which would abort the test before it could do so.
$PSNativeCommandUseErrorActionPreference = $false

$dotnetSharedMemoryPath = '/tmp/.dotnet'

function Find-FileInParents {
    param( [string] $FileName )

    $currentDir = $PSScriptRoot

    while ( $currentDir ) {
        $candidatePath = Join-Path $currentDir $FileName

        if ( Test-Path $candidatePath ) {
            return $candidatePath
        }

        $parentDir = Split-Path $currentDir -Parent

        if ( $parentDir -eq $currentDir ) {
            break
        }

        $currentDir = $parentDir
    }

    return $null
}

function Reset-SharedMemoryDirectory {
    # Build servers outlive a build and keep their mutexes, and therefore the shared memory files,
    # open. They must be stopped before the tree is replaced, otherwise a later compilation reuses
    # an already initialized process and never reaches the code under test.
    dotnet build-server shutdown | Out-Null

    if ( Test-Path $dotnetSharedMemoryPath ) {
        Remove-Item -Recurse -Force $dotnetSharedMemoryPath
    }
}

function Disable-SharedMemoryDirectory {
    Reset-SharedMemoryDirectory

    # Create a regular file where the runtime expects a directory.
    New-Item -ItemType File -Path $dotnetSharedMemoryPath | Out-Null

    Write-Host "Replaced $dotnetSharedMemoryPath with a regular file."
}

function Invoke-Build {
    param( [string] $Message )

    Write-Host "`n$Message"

    # The compilation must run in the build process itself. A shared compiler server would have
    # initialized the Backstage services before the shared memory tree was replaced, and reused
    # MSBuild nodes would do the same.
    $output = & dotnet build --no-restore -t:Rebuild `
        --disable-build-servers `
        -nodeReuse:false `
        -p:UseSharedCompilation=false 2>&1 | Out-String

    Write-Host $output

    return @{ ExitCode = $LASTEXITCODE; Output = $output }
}

Push-Location $PSScriptRoot

try {
    $nugetConfig = Find-FileInParents 'nuget.wsl.config'

    if ( -not $nugetConfig ) {
        throw 'Could not find nuget.wsl.config in any parent directory.'
    }

    Write-Host 'Restoring...'
    dotnet restore --configfile $nugetConfig
    if ( $LASTEXITCODE -ne 0 ) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

    dotnet restore MutexProbe/MutexProbe.csproj --configfile $nugetConfig
    if ( $LASTEXITCODE -ne 0 ) { throw "dotnet restore of MutexProbe failed with exit code $LASTEXITCODE." }

    dotnet build MutexProbe/MutexProbe.csproj --no-restore
    if ( $LASTEXITCODE -ne 0 ) { throw "dotnet build of MutexProbe failed with exit code $LASTEXITCODE." }

    $probeDll = Get-ChildItem -Path MutexProbe/bin -Recurse -Filter 'MutexProbe.dll' | Select-Object -First 1
    if ( -not $probeDll ) { throw 'MutexProbe.dll was not found.' }

    # Step 1. The baseline. A build must succeed when the shared memory tree is healthy, otherwise
    # any failure observed later would not be attributable to the condition under test.
    Reset-SharedMemoryDirectory
    $baseline = Invoke-Build 'Building with a healthy /tmp/.dotnet (baseline)...'

    if ( $baseline.ExitCode -ne 0 ) {
        Write-Error "INCONCLUSIVE: the baseline build failed with exit code $($baseline.ExitCode). The test environment is broken, independently of issue #272."
        exit 1
    }

    Write-Host 'Baseline build succeeded.'

    # Step 2. Verify the premise, namely that the prepared environment really does prevent the
    # runtime from creating a global named mutex. Should the runtime stop failing here, this test
    # would otherwise start passing for the wrong reason.
    Disable-SharedMemoryDirectory

    Write-Host "`nProbing global named mutex creation..."
    $probeOutput = & dotnet exec $probeDll.FullName 2>&1 | Out-String
    $probeExitCode = $LASTEXITCODE
    Write-Host $probeOutput

    if ( $probeExitCode -eq 0 ) {
        Write-Error 'INCONCLUSIVE: the .NET runtime created a global named mutex even though /tmp/.dotnet is not a directory. The premise of this test no longer holds and the test must be revised.'
        exit 1
    }

    if ( $probeOutput -notmatch '0x8007006E' ) {
        Write-Error "INCONCLUSIVE: the probe failed, but not with ERROR_OPEN_FAILED (0x8007006E). This test targets that specific failure. Actual output above."
        exit 1
    }

    Write-Host 'Premise confirmed: the runtime cannot create a global named mutex.'

    # Step 3. The assertion. Metalama must still build.
    Disable-SharedMemoryDirectory
    $actual = Invoke-Build 'Building with an unusable /tmp/.dotnet...'

    if ( $actual.ExitCode -eq 0 ) {
        Write-Host "`nSUCCESS: Metalama built successfully even though the global named mutex could not be created."
        exit 0
    }

    if ( $actual.Output -match 'LAMA0623' -or $actual.Output -match '0x8007006E' -or $actual.Output -match 'cannot open the device or file specified' ) {
        Write-Error "FAILURE: the build failed because the global named mutex could not be created. This is issue #272: Metalama must not depend on a machine-wide named mutex being available. See the build output above."
        exit 1
    }

    Write-Error "FAILURE: the build failed with exit code $($actual.ExitCode), but not with the diagnostic expected for issue #272. See the build output above."
    exit 1
}
finally {
    Reset-SharedMemoryDirectory
    Pop-Location
}
