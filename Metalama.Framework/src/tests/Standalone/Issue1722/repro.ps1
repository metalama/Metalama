# Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
# Deterministic reproduction driver for issue #1722. Invoked by Issue1722.proj.
#
# Each build is launched as a fully detached top-level process (Start-Process -Wait). This is essential: if the
# builds run as MSBuild <Exec> children they share one warm in-process CompileTimeDomain that keeps
# ml!Primitives @ 1.0.0 loaded, and the bug self-heals. Independent processes reproduce it deterministically.
#
# Exit code 0 => the final consumer build succeeded => issue #1722 is fixed.
# Non-zero    => the final consumer build failed (FileNotFound during eligibility) => bug still present.

param(
    [Parameter(Mandatory = $true)] [string] $MetalamaVersion
)

$ErrorActionPreference = 'Stop'

# When this script is launched from an MSBuild <Exec> task, the environment carries variables that make child
# 'dotnet' processes rejoin the parent MSBuild's SDK/node ecosystem (and thus share one in-process
# CompileTimeDomain, which self-heals the bug). Clear them so every build below is a truly independent top-level
# process, exactly as when the script is run from a plain shell.
foreach ($v in 'MSBUILD_EXE_PATH', 'MSBuildSDKsPath', 'MSBuildExtensionsPath', 'MSBuildExtensionsPath32',
    'MSBuildExtensionsPath64', 'MSBuildLoadMicrosoftTargetsReadOnly', 'MSBUILDNOINPROCNODE',
    'MSBUILDDISABLENODEREUSE', 'MSBUILDTARGETSVERBOSE', 'DOTNET_ROOT_X64', 'DOTNET_ROOT', 'DOTNET_HOST_PATH') {
    Remove-Item "env:$v" -ErrorAction SilentlyContinue
}
# Never route builds through the persistent dotnet MSBuild server: with disable-build-servers the compilation
# runs inside that server process, which persists across the two consumer builds and keeps ml!Primitives @ 1.0.0
# loaded (masking the bug).
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'

$dir = $PSScriptRoot
$feed = Join-Path $dir 'artifacts\feed'
$pkgs = Join-Path $dir 'artifacts\pkgs'
$cacheRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'Metalama\CompileTime'
$common = @('--disable-build-servers', '-nodeReuse:false', '-clp:ErrorsOnly', '-nologo', "-p:MetalamaVersion=$MetalamaVersion")

function Invoke-Dotnet([string[]] $arguments) {
    # Launch dotnet as an independent top-level process so it does not share in-process state with this script
    # or with previous builds.
    $p = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -Wait -NoNewWindow -PassThru
    return $p.ExitCode
}

function Remove-Dir([string] $path) {
    if (Test-Path $path) { Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue }
}

function Stop-BuildWorkers {
    # Kill the Roslyn compiler server and any persistent MSBuild worker nodes (dotnet '/nodemode:' processes),
    # which host the warm in-process CompileTimeDomain. This does NOT touch the orchestrator: worker nodes are
    # identified by their command line, not by killing every dotnet. Essential when running under MSBuild, where
    # such workers survive 'dotnet build-server shutdown' and would otherwise keep ml!Primitives @ 1.0.0 loaded.
    Get-Process VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match '/nodemode:' -or $_.CommandLine -match 'VBCSCompiler' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Milliseconds 500
}

Write-Host 'Issue1722: resetting local feed, package cache and compile-time cache.'
Invoke-Dotnet @('build-server', 'shutdown') | Out-Null
Remove-Dir $feed
Remove-Dir $pkgs
foreach ($p in 'Issue1722.Primitives', 'Issue1722.Aspects', 'Issue1722.PackageConsumer') { Remove-Dir (Join-Path $cacheRoot $p) }

# NuGet package versions are immutable in the global cache, so a stale Issue1722.* package from an earlier run
# (built against a different Metalama version) would be reused and mask the result. Remove them from the global
# packages folder so the consumer always restores the freshly-packed packages.
$globalPackages = ((& dotnet nuget locals global-packages --list) -replace '^[^:]*:\s*', '').Trim()
if ($globalPackages -and (Test-Path $globalPackages)) {
    Get-ChildItem $globalPackages -Filter 'issue1722.*' -Directory -ErrorAction SilentlyContinue |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force -Path $feed | Out-Null

$primitives = Join-Path $dir 'Issue1722.Primitives\Issue1722.Primitives.csproj'
$aspects    = Join-Path $dir 'Issue1722.Aspects\Issue1722.Aspects.csproj'
$consumer   = Join-Path $dir 'PackageConsumer\Issue1722.PackageConsumer.csproj'

Write-Host 'Issue1722: packing Primitives 1.0.0/2.0.0 and Aspects 1.0.0.'
if ((Invoke-Dotnet (@('pack', $primitives) + $common + @('-p:Issue1722PrimitivesVersion=1.0.0', '-o', $feed))) -ne 0) { throw 'pack Primitives 1.0.0 failed' }
if ((Invoke-Dotnet (@('pack', $aspects) + $common + @('-p:Issue1722PrimitivesVersion=1.0.0', '-p:Issue1722AspectsVersion=1.0.0', '-o', $feed))) -ne 0) { throw 'pack Aspects 1.0.0 failed' }
if ((Invoke-Dotnet (@('pack', $primitives) + $common + @('-p:Issue1722PrimitivesVersion=2.0.0', '-o', $feed))) -ne 0) { throw 'pack Primitives 2.0.0 failed' }

# The pack builds populated the compile-time cache; empty it so the consumer builds it fresh.
foreach ($p in 'Issue1722.Primitives', 'Issue1722.Aspects', 'Issue1722.PackageConsumer') { Remove-Dir (Join-Path $cacheRoot $p) }

Write-Host 'Issue1722: step A - consume Aspects 1.0.0 + Primitives 1.0.0 (must succeed).'
if ((Invoke-Dotnet (@('build', $consumer) + $common + @('-p:ConsumerAspectsVersion=1.0.0', '-p:ConsumerPrimitivesVersion=1.0.0'))) -ne 0) {
    throw 'Issue1722: step A failed unexpectedly (consuming matching versions should succeed).'
}

Invoke-Dotnet @('build-server', 'shutdown') | Out-Null
Stop-BuildWorkers

Write-Host 'Issue1722: step B - upgrade Primitives to 2.0.0 and rebuild (reproduces the bug when present).'
$exit = Invoke-Dotnet (@('build', $consumer) + $common + @('-p:ConsumerAspectsVersion=1.0.0', '-p:ConsumerPrimitivesVersion=2.0.0'))
if ($exit -ne 0) {
    Write-Host 'Issue1722: step B FAILED - issue #1722 reproduced (compile-time assembly mismatch during eligibility).'
    exit $exit
}

Write-Host 'Issue1722: step B succeeded - issue #1722 is fixed.'
exit 0
