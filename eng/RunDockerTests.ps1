# The original of this file is in <PostSharp.Engineering>/src/PostSharp.Engineering.BuildTools/Resources/RunDockerTests.ps1.
# You can generate this file using `./Build.ps1 generate-scripts`.
# Documentation: https://raw.githubusercontent.com/postsharp/PostSharp.Engineering/HEAD/doc/docker-tests.md

<#
.SYNOPSIS
    Runs the Docker-based tests that apply to one platform, and reports each of them to TeamCity.

.DESCRIPTION
    A Docker-based test is a test that needs a container of its own. Each test is a directory containing a
    manifest (test.psd1) and an entry point (RunTest.ps1). This script discovers those directories, selects
    the tests whose manifest lists the current platform, runs them one at a time, and reports the result of
    each one.

    The only requirements on the host are PowerShell 7.5 and a container engine whose operating system and
    processor architecture match the platform. The tool chain under test lives in the test's own image, so
    this script never needs the .NET SDK, MSBuild or Visual Studio.

    A test reports its own result through its exit code alone. This script owns the TeamCity protocol, so a
    test stays runnable by hand.

.PARAMETER Path
    The directory containing the test directories, relative to this script. Defaults to the value the product
    declares, which is what generate-scripts wrote into this file.

.PARAMETER Platform
    The platform to run: win-x64, win-arm64, linux-x64 or linux-arm64. Defaults to the platform of the
    container engine this host is running.

.PARAMETER Test
    Runs only the test of this name. Without it, every test that applies to the platform is run.

.NOTES
    A test reports its outcome by its exit code: 0 passed, 4 skipped, anything else failed. The skip code is
    for a scenario the test finds it cannot reproduce on this host, which is different from the manifest's Skip:
    that one is known in advance, this one only once the test has looked. A skipping test should write a line
    beginning with 'SKIPPED:', which becomes the reason reported.

.PARAMETER NoTeamCity
    Writes plain text instead of TeamCity service messages. Implied when TEAMCITY_VERSION is not set.

.EXAMPLE
    ./RunDockerTests.ps1 -Platform linux-x64

.EXAMPLE
    ./RunDockerTests.ps1 -Platform win-x64 -Test Issue15-AssetsFileV4
#>

[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64')]
    [string]$Platform,
    [string]$Path, # Defaults to $DockerTestsPath below.
    [string]$Test,
    [switch]$NoTeamCity
)

# Require PowerShell 7.5 or higher (run with pwsh, not powershell)
if ($PSVersionTable.PSVersion -lt [Version]'7.5')
{
    Write-Error "This script requires PowerShell 7.5 or higher (run with 'pwsh', not 'powershell'). Current version: $( $PSVersionTable.PSVersion )"
    exit 1
}

####
# These settings are replaced by the generate-scripts command.
#
# Where the tests are is a fact about this repository, not a choice a caller makes. This script is generated into
# the repository root, so it is fixed relative to it, and a build configuration that had to pass it would be one
# more place for it to drift.
#
# Where what the tests consume is, is not here at all: a test resolves that for itself, from where it lives. It is
# a fact about the product, and this script has no opinion about it.
$DockerTestsPath = 'Metalama.Framework/src/tests/docker'
$EngPath = 'eng'
####

if (-not $Path)
{
    $Path = $DockerTestsPath
}

$DefaultTimeoutSeconds = 900

# The exit code by which a test reports that it decided, at run time, that its scenario cannot occur on this
# host, and that it therefore tested nothing. The manifest's Skip covers what is known before the test runs;
# this covers what is only discoverable once it has looked -- an SDK that ships a pack the scenario needs
# absent, a case-insensitive file system, a kernel without the facility under test. Reporting those as failures
# would train people to ignore red, and reporting them as passes would claim coverage that does not exist.
$SkipExitCode = 4

# TeamCity reads a service message up to the first unescaped delimiter, and Docker output is full of brackets,
# so an unescaped value silently truncates or corrupts the report. The vertical bar is replaced first: doing it
# later would double the bars introduced by the other replacements.
function ConvertTo-TeamCityValue([string]$value)
{
    if ($null -eq $value)
    {
        return ''
    }

    $escaped = $value.Replace('|', '||')
    $escaped = $escaped.Replace("'", "|'")
    $escaped = $escaped.Replace('[', '|[')
    $escaped = $escaped.Replace(']', '|]')
    $escaped = $escaped.Replace("`r", '|r')
    $escaped = $escaped.Replace("`n", '|n')

    return $escaped
}

function Write-ServiceMessage([string]$name, [hashtable]$attributes)
{
    if ($NoTeamCity)
    {
        return
    }

    $pairs = $attributes.Keys | Sort-Object | ForEach-Object {
        "$_='$( ConvertTo-TeamCityValue $attributes[$_] )'"
    }

    Write-Host "##teamcity[$name $( $pairs -join ' ' )]"
}

# The operating system a platform identifier asks for, in the spelling DockerBuild.ps1 -OS uses.
function Get-RequestedOs([string]$platform)
{
    return $(if ($platform -like 'linux-*') { 'linux' } else { 'windows' })
}

# Asks the engine that would run the given operating system for its own platform. Which engine that is depends
# on the host: a build agent has one, while a Windows development machine runs Windows containers on Docker
# Desktop and Linux containers on the Docker engine inside WSL. The engine is asked rather than the host
# because only the engine knows what it can actually run.
function Get-EnginePlatform([string]$requestedOs)
{
    $useWsl = $IsWindows -and $requestedOs -eq 'linux' -and -not $env:IS_TEAMCITY_AGENT

    if ($useWsl)
    {
        if (-not (Get-Command wsl.exe -ErrorAction SilentlyContinue))
        {
            Write-Host "Linux containers on a Windows development machine run on the Docker engine inside WSL, and wsl.exe was not found." -ForegroundColor Red
            Write-Host "Install it with 'wsl --install', then install PowerShell 7 and a Docker engine inside the distribution." -ForegroundColor Red
            exit 1
        }

        $engineOs = (& wsl.exe -- docker version --format '{{.Server.Os}}' 2>&1 | Out-String).Trim()
        $engineArchRaw = (& wsl.exe -- docker version --format '{{.Server.Arch}}' 2>&1 | Out-String).Trim()
    }
    else
    {
        $engineOs = (docker version --format '{{.Server.Os}}' 2>&1 | Out-String).Trim()

        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "No container engine is available on this host: $engineOs" -ForegroundColor Red
            Write-Host "A Docker test host needs PowerShell 7.5 and a container engine, and nothing else." -ForegroundColor Red
            exit 1
        }

        $engineArchRaw = (docker version --format '{{.Server.Arch}}' 2>&1 | Out-String).Trim()
    }

    $os = switch ($engineOs)
    {
        'windows' { 'win' }
        'linux' { 'linux' }
        default { $null }
    }

    $arch = switch ($engineArchRaw)
    {
        'amd64' { 'x64' }
        'x86_64' { 'x64' }
        'arm64' { 'arm64' }
        'aarch64' { 'arm64' }
        default { $null }
    }

    if (-not $os -or -not $arch)
    {
        $where = if ($useWsl) { 'inside WSL' } else { 'on this host' }
        Write-Host "The container engine $where reports an unsupported platform: os='$engineOs', arch='$engineArchRaw'." -ForegroundColor Red
        exit 1
    }

    return "$os-$arch"
}

function Read-TestManifest([string]$manifestPath)
{
    try
    {
        $manifest = Import-PowerShellDataFile -LiteralPath $manifestPath -ErrorAction Stop
    }
    catch
    {
        return @{ Error = "The manifest cannot be read: $( $_.Exception.Message )" }
    }

    if (-not $manifest.Platforms)
    {
        return @{ Error = 'The manifest does not declare Platforms.' }
    }

    $timeout = if ($manifest.TimeoutSeconds) { [int]$manifest.TimeoutSeconds } else { $DefaultTimeoutSeconds }

    return @{
        Platforms = @($manifest.Platforms)
        TimeoutSeconds = $timeout
        Skip = $manifest.Skip
    }
}

# Runs the product's own preparation, once, before any test.
#
# What a suite needs before it can run is not the same in every repository: packages may arrive as an archive
# that has to be expanded, a fixture may have to be materialised, a tool may have to be fetched. That belongs to
# the product, not here, so the launcher looks for one script at a known place and runs it if it is there.
#
# Once, not per test. A test's entry point runs for each test, so anything expensive done there is done again
# for every one of them.
function Invoke-ProductPreparation([string]$repositoryRoot)
{
    $script = Join-Path $repositoryRoot ( Join-Path $EngPath 'PrepareDockerTests.ps1' )

    if (-not (Test-Path -LiteralPath $script))
    {
        return
    }

    Write-Host "Preparing the suite with '$script'." -ForegroundColor Green

    $global:LASTEXITCODE = 0

    try
    {
        & $script
    }
    catch
    {
        # Without this the run continued: an exception from the script left $LASTEXITCODE at whatever the last
        # native command had set, which is 0 when there was none, so the check below passed and the suite went on
        # to find no tests and report success.
        throw "'$script' failed: $( $_.Exception.Message )"
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "'$script' failed with exit code $LASTEXITCODE."
    }
}

# Fails unless the repository root has a NuGet configuration, which is what lets a test resolve the product from
# the packages the build produced rather than from nuget.org.
#
# That distinction is the whole point. A Docker test consumes the product through a PackageReference, and the
# version it asks for is often one that has been released, so nuget.org can satisfy it. Without a source for the
# local packages and a packageSourceMapping sending the product's packages there, the restore would quietly
# succeed against the public package and the test would report a pass having verified binaries that nobody just
# built. A test that silently checks the wrong thing is worse than one that fails, so a missing configuration is
# fatal rather than tolerated.
#
# This only checks, it does not write. Putting the file there is the harness's job and it already does it: a
# configuration that has a build snapshot dependency is generated with a CopyNuGetConfig step that copies
# nuget.restored.config -- published beside the packages -- to the root before this script runs. On a prepared
# developer machine the root nuget.config that Build.ps1 writes is already there. Copying it again here would
# only repeat what one of those two has done.
function Assert-NuGetConfiguration([string]$repositoryRoot)
{
    $configuration = Join-Path $repositoryRoot 'nuget.config'

    if (-not (Test-Path -LiteralPath $configuration))
    {
        throw "'$configuration' does not exist, so the packages these tests consume cannot be located and the restore would fall back to nuget.org. Prepare the repository, or copy artifacts/publish/private/nuget.restored.config to the root, before running the Docker tests."
    }
}

# Kills a process and everything it started. Stop-Process alone does not: it terminates the one process, and the
# children it spawned are reparented rather than killed.
function Stop-ProcessTree([int]$processId)
{
    # Win32_Process is the only way to walk the tree on Windows. On Linux and macOS pgrep does the same job, and
    # the container cleanup below is what actually matters there in any case.
    $children = if ($IsWindows)
    {
        @( Get-CimInstance Win32_Process -Filter "ParentProcessId = $processId" -ErrorAction SilentlyContinue |
                ForEach-Object { [int]$_.ProcessId } )
    }
    else
    {
        @( & pgrep -P $processId 2>$null | ForEach-Object { [int]$_ } )
    }

    foreach ($child in $children)
    {
        Stop-ProcessTree $child
    }

    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
}

# Force-removes every container labelled with this test run. A container outlives the process that started it,
# so a timeout that killed only the process would leave it running.
function Remove-TestContainers([string]$runId)
{
    $containers = @(& docker ps --all --quiet --filter "label=postsharp.test-run=$runId" 2>$null)

    if ($containers.Count -eq 0)
    {
        return
    }

    Write-Host "Removing $( $containers.Count ) container(s) left by the timed-out test." -ForegroundColor Yellow
    & docker rm --force @containers 2>&1 | Out-Null
}

# Copies whatever has been appended to the redirected output files since the last call, so that a running test
# is visible while it runs. FileShare.ReadWrite is required: the child process still holds these files open,
# and opening them any other way would fail.
function Copy-NewOutput([hashtable]$positions)
{
    foreach ($file in @($positions.Keys))
    {
        if (-not (Test-Path -LiteralPath $file))
        {
            continue
        }

        $stream = $null

        try
        {
            $stream = [System.IO.File]::Open($file, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)

            if ($stream.Length -gt $positions[$file])
            {
                $stream.Position = $positions[$file]
                $reader = New-Object System.IO.StreamReader($stream)
                $text = $reader.ReadToEnd()
                $positions[$file] = $stream.Position

                if ($text)
                {
                    Write-Host $text -NoNewline
                }
            }
        }
        catch
        {
            # A transient sharing failure only delays the output to the next pass, and must never fail the test.
        }
        finally
        {
            if ($stream)
            {
                $stream.Dispose()
            }
        }
    }
}

# Runs one test to completion and returns its outcome. The output is redirected to files so that the whole of
# it can be attached to the test even when the test is killed on its timeout, and is echoed as it arrives so
# that a long test is not indistinguishable from a hung one. Pulling a Windows base image takes tens of
# minutes, and a silent log for that long is a log nobody can act on.
function Invoke-OneTest([string]$testDirectory, [int]$timeoutSeconds, [string]$platform)
{
    $rootDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "dockertest-$( [System.Guid]::NewGuid().ToString('n') )"

    # The captured output is kept outside the directory handed to the test, so that a test writing into its own
    # scratch directory cannot collide with it.
    # Only the captured output. A test is given no scratch directory: it runs against the repository, which
    # DockerBuild.ps1 mounts into the container, rather than against anything staged for it here.
    $logDirectory = Join-Path $rootDirectory 'log'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

    $stdOutFile = Join-Path $logDirectory 'stdout.log'
    $stdErrFile = Join-Path $logDirectory 'stderr.log'

    # Every container this test starts is labelled with this, so that a timeout can remove them. Killing the
    # test process is not enough: it is waiting on `docker run`, and the container is a child of the engine, not
    # of the process tree. Left behind, it goes on holding CPU, memory and the image, and a later test that
    # expects an idle machine -- or the same port, or the same mount -- fails for a reason that has nothing to
    # do with it.
    $runId = [System.Guid]::NewGuid().ToString('n')
    $env:POSTSHARP_DOCKER_TEST_RUN_ID = $runId

    try
    {
        $arguments = @(
            '-NonInteractive'
            '-NoProfile'
            '-File'
            (Join-Path $testDirectory 'RunTest.ps1')
            '-Platform'
            $platform
        )

        # [Environment]::ProcessPath is this pwsh, so the test runs under the same PowerShell that discovered it.
        $process = Start-Process -FilePath ([Environment]::ProcessPath) `
            -ArgumentList $arguments `
            -PassThru `
            -NoNewWindow `
            -RedirectStandardOutput $stdOutFile `
            -RedirectStandardError $stdErrFile

        $timedOut = $false
        $positions = @{ $stdOutFile = [long]0; $stdErrFile = [long]0 }
        $deadline = [DateTime]::UtcNow.AddSeconds($timeoutSeconds)

        while (-not $process.HasExited)
        {
            if ([DateTime]::UtcNow -gt $deadline)
            {
                $timedOut = $true
                break
            }

            Copy-NewOutput $positions
            Start-Sleep -Milliseconds 500
        }

        Copy-NewOutput $positions

        if ($timedOut)
        {
            Write-Host "The test exceeded its timeout of $timeoutSeconds seconds and is being killed." -ForegroundColor Red

            # The whole tree: the test process is a pwsh that started another to run DockerBuild.ps1, and killing
            # only the one that was waited on leaves the rest running.
            Stop-ProcessTree $process.Id

            $process.WaitForExit(30 * 1000) | Out-Null

            Remove-TestContainers $runId
        }
        else
        {
            # The parameterless overload also waits for the redirected streams to be flushed. Without it the
            # captured output can be read back short of its last lines.
            $process.WaitForExit()
        }

        $output = @()

        foreach ($file in @($stdOutFile, $stdErrFile))
        {
            if (Test-Path -LiteralPath $file)
            {
                $content = Get-Content -LiteralPath $file -Raw -ErrorAction SilentlyContinue

                if ($content)
                {
                    $output += $content
                }
            }
        }

        return @{
            TimedOut = $timedOut
            ExitCode = if ($timedOut) { -1 } else { $process.ExitCode }
            Output = ($output -join "`n")
        }
    }
    finally
    {
        Remove-Item -LiteralPath $rootDirectory -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item Env:\POSTSHARP_DOCKER_TEST_RUN_ID -ErrorAction SilentlyContinue
    }
}

Push-Location
try
{
    # This script lives in the engineering directory, not at the root, so everything it addresses relative to
    # the repository is resolved from the parent.
    $repositoryRoot = ( Resolve-Path ( Join-Path $PSScriptRoot '..' ) ).Path

    Set-Location $repositoryRoot

    if (-not $env:TEAMCITY_VERSION)
    {
        $NoTeamCity = $true
    }

    if (-not $Platform)
    {
        # Without a platform, the host's own engine is the one that answers, which is what a developer running
        # the suite with no argument means. No requested operating system means no hop into WSL.
        $Platform = Get-EnginePlatform $null
        Write-Host "Platform not specified; using the container engine's own platform: $Platform" -ForegroundColor Cyan
    }
    else
    {
        $enginePlatform = Get-EnginePlatform ( Get-RequestedOs $Platform )

        if ($Platform -ne $enginePlatform)
        {
            # This is the whole point of one configuration per platform. A mismatch on an agent means the build
            # was routed to the wrong one, and every test would otherwise fail for the same uninformative
            # reason.
            Write-Host "The engine that would run the '$Platform' tests reports '$enginePlatform' instead." -ForegroundColor Red

            if ($env:IS_TEAMCITY_AGENT)
            {
                Write-Host "Check the agent requirements of this build configuration." -ForegroundColor Red
            }

            exit 1
        }
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Container))
    {
        Write-Host "The test directory '$Path' does not exist." -ForegroundColor Red
        exit 1
    }

    Invoke-ProductPreparation $repositoryRoot
    Assert-NuGetConfiguration $repositoryRoot

$testDirectories = Get-ChildItem -LiteralPath $Path -Directory |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'test.psd1') } |
            Sort-Object Name

    if ($Test)
    {
        $testDirectories = $testDirectories | Where-Object { $_.Name -eq $Test }

        if (-not $testDirectories)
        {
            Write-Host "There is no test named '$Test' in '$Path'." -ForegroundColor Red
            exit 1
        }
    }

    if (-not $testDirectories)
    {
        Write-Host "No test was found in '$Path'. A test is a directory containing test.psd1." -ForegroundColor Yellow
        exit 0
    }

    $suiteName = "DockerTests.$Platform"
    Write-ServiceMessage 'testSuiteStarted' @{ name = $suiteName }

    $failed = @()
    $passed = 0
    $ignored = 0

    foreach ($testDirectory in $testDirectories)
    {
        $testName = $testDirectory.Name
        $manifest = Read-TestManifest (Join-Path $testDirectory.FullName 'test.psd1')

        Write-ServiceMessage 'testStarted' @{ name = $testName; captureStandardOutput = 'false' }

        $durationMilliseconds = 0

        # Every test's outcome is decided inside this try, so that one test's failure never ends the run. The
        # launcher's own exit code is the aggregate, reported at the end. testFinished is written once, in the
        # finally, and therefore always after the testFailed or testIgnored that explains the outcome.
        try
        {
            if ($manifest.Error)
            {
                Write-Host "$testName : $( $manifest.Error )" -ForegroundColor Red
                Write-ServiceMessage 'testFailed' @{ name = $testName; message = $manifest.Error }
                $failed += $testName
            }
            elseif ($manifest.Skip)
            {
                Write-Host "$testName : skipped -- $( $manifest.Skip )" -ForegroundColor Yellow
                Write-ServiceMessage 'testIgnored' @{ name = $testName; message = $manifest.Skip }
                $ignored++
            }
            elseif ($manifest.Platforms -notcontains $Platform)
            {
                $reason = "This test declares $( $manifest.Platforms -join ', ' ) and not $Platform."
                Write-Host "$testName : not applicable -- $reason" -ForegroundColor DarkGray
                Write-ServiceMessage 'testIgnored' @{ name = $testName; message = $reason }
                $ignored++
            }
            elseif (-not (Test-Path -LiteralPath (Join-Path $testDirectory.FullName 'RunTest.ps1')))
            {
                $reason = 'The test directory has no RunTest.ps1.'
                Write-Host "$testName : $reason" -ForegroundColor Red
                Write-ServiceMessage 'testFailed' @{ name = $testName; message = $reason }
                $failed += $testName
            }
            else
            {
                Write-Host "Running $testName on $Platform." -ForegroundColor Green
                $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                $result = Invoke-OneTest $testDirectory.FullName $manifest.TimeoutSeconds $Platform
                $stopwatch.Stop()
                $durationMilliseconds = [int]$stopwatch.Elapsed.TotalMilliseconds

                if ($result.Output)
                {
                    # Not written to the console again: it was echoed as the test produced it. Only the service
                    # message is sent, so that the whole of the output is attached to the test in the report.
                    Write-ServiceMessage 'testStdOut' @{ name = $testName; out = $result.Output }
                }

                if ($result.TimedOut)
                {
                    $message = "The test exceeded its timeout of $( $manifest.TimeoutSeconds ) seconds."
                    Write-ServiceMessage 'testFailed' @{ name = $testName; message = $message }
                    $failed += $testName
                }
                elseif ($result.ExitCode -eq $SkipExitCode)
                {
                    # The reason is taken from the last line the test wrote beginning with SKIPPED:, which is how
                    # these tests already explain themselves. Without one the exit code still counts, because the
                    # decision belongs to the test; only the explanation is missing.
                    $skipLine = @( $result.Output -split "`n" | Where-Object { $_ -match '^\s*SKIPPED:' } ) |
                            Select-Object -Last 1

                    $message = if ($skipLine -match '^\s*SKIPPED:\s*(.+?)\s*$')
                    {
                        $Matches[1]
                    }
                    else
                    {
                        "The test reported exit code $SkipExitCode, meaning its scenario cannot occur on this host."
                    }

                    Write-Host "$testName : skipped -- $message" -ForegroundColor Yellow
                    Write-ServiceMessage 'testIgnored' @{ name = $testName; message = $message }
                    $ignored++
                }
                elseif ($result.ExitCode -ne 0)
                {
                    $message = "The test failed with exit code $( $result.ExitCode )."
                    Write-ServiceMessage 'testFailed' @{ name = $testName; message = $message }
                    $failed += $testName
                }
                else
                {
                    $passed++
                }
            }
        }
        catch
        {
            # The harness itself failed, which is a failure of this test and not of the run.
            $message = "The test harness failed: $( $_.Exception.Message )"
            Write-Host $message -ForegroundColor Red
            Write-ServiceMessage 'testFailed' @{ name = $testName; message = $message }
            $failed += $testName
        }
        finally
        {
            Write-ServiceMessage 'testFinished' @{ name = $testName; duration = [string]$durationMilliseconds }
        }
    }

    Write-ServiceMessage 'testSuiteFinished' @{ name = $suiteName }

    Write-Host ""
    Write-Host "$Platform : $passed passed, $( $failed.Count ) failed, $ignored ignored." -ForegroundColor Cyan

    # Nothing ran at all. That is not a pass: a suite reporting success without executing a test is worse than one
    # that fails, because nobody looks at it again. It happens when the discovery found tests and every one of them
    # was skipped by a fault rather than by a manifest, or when preparation left the suite unable to start.
    if ($passed -eq 0 -and $failed.Count -eq 0 -and $ignored -eq 0 -and $testDirectories.Count -gt 0)
    {
        Write-Host "$( $testDirectories.Count ) test(s) were found and none of them ran." -ForegroundColor Red
        Write-ServiceMessage 'buildProblem' @{ description = "$( $testDirectories.Count ) Docker test(s) were found and none of them ran." }

        exit 1
    }

    if ($failed.Count -gt 0)
    {
        Write-Host "Failed: $( $failed -join ', ' )" -ForegroundColor Red
        exit 1
    }

    exit 0
}
finally
{
    Pop-Location
}
