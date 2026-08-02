$ErrorActionPreference = 'Stop'

$x64Dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$x86Dotnet = 'C:\Program Files (x86)\dotnet\dotnet.exe'
$sdkX64 = $env:SDK_X64
$failures = 0

function Write-Section($text) { Write-Host ''; Write-Host "===== $text =====" }

# Runs a native command and returns its combined output as one string. PowerShell turns the standard error of a
# native command into ErrorRecord objects, which would terminate the script under $ErrorActionPreference = 'Stop'
# and would not render as text, so the preference is relaxed and the records are converted back to their text.
function Invoke-NativeCommand {
    param([string]$Executable, [string[]]$Arguments)

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try {
        $lines = & $Executable @Arguments 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { $_ }
        }

        return [PSCustomObject]@{ Output = ($lines -join [Environment]::NewLine); ExitCode = $LASTEXITCODE }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

Write-Section 'Precondition: two installations with different SDK sets'
Write-Host "x64 ($x64Dotnet):"
& $x64Dotnet --list-sdks
Write-Host "x86 ($x86Dotnet):"
& $x86Dotnet --list-sdks

$x86Sdks = (& $x86Dotnet --list-sdks | Out-String)

if ($x86Sdks -match [regex]::Escape($sdkX64)) {
    Write-Host "INCONCLUSIVE: the x86 installation also carries $sdkX64, so the scenario would pass vacuously."
    exit 1
}

Write-Section 'Arrange: the files that CompileTimeAssemblyLocator writes into its cache directory'

# GlobalJsonHelper.WriteCurrentVersion pins NETCoreSdkVersion, i.e. the version of the .NET SDK that
# is building the project. That project is built by the x64 SDK here, as it is in a 32-bit MSBuild.exe
# host, which resolves the .NET SDK from the 64-bit installation.
$cacheDirectory = 'C:\app\AssemblyLocator'
New-Item -ItemType Directory -Path $cacheDirectory -Force | Out-Null
Set-Location $cacheDirectory

@"
{
  "sdk": {
    "version": "$sdkX64",
    "rollForward": "disable"
  }
}
"@ | Set-Content -Path 'global.json' -Encoding utf8

# A NuGet configuration with no source at all, so that the control build never reaches the network. The
# container has IP connectivity but no name resolution, and a restore that tries nuget.org would merely
# stall until it times out.
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
'@ | Set-Content -Path 'nuget.config' -Encoding utf8

# This is the project that CompileTimeAssemblyLocator writes, with two deviations that keep the container
# free of network access and that play no part in the failure, because the .NET SDK is resolved before any
# restore would run: its PackageReference on Microsoft.CodeAnalysis.CSharp is omitted, and it targets the
# framework of the 64-bit SDK itself, whose reference pack that SDK carries, rather than
# 'netstandard2.0;net8.0;net48', whose reference packs would have to be downloaded.
@"
<Project>
  <PropertyGroup>
    <ImportDirectoryPackagesProps>false</ImportDirectoryPackagesProps>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
  </PropertyGroup>
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  <PropertyGroup>
    <TargetFrameworks>net$($sdkX64.Split('.')[0]).0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
  </PropertyGroup>
  <Target Name="WriteAssembliesList" AfterTargets="Build" Condition="'`$(TargetFramework)'!=''">
    <WriteLinesToFile File="assemblies-`$(TargetFramework).txt" Overwrite="true" Lines="@(ReferencePathWithRefAssemblies)" />
  </Target>
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
"@ | Set-Content -Path 'TempProject.csproj' -Encoding utf8

'System.Console.WriteLine("Hello, world.");' | Set-Content -Path 'Program.cs' -Encoding utf8

Write-Section "Act 1 (the defect): the nested build runs on the x86 host, as PlatformInfo selects it in a 32-bit process"
$x86Result = Invoke-NativeCommand $x86Dotnet @('build', '-bl:msbuild.binlog')
$x86Output = $x86Result.Output
$x86ExitCode = $x86Result.ExitCode
Write-Host $x86Output
Write-Host "exit code: $x86ExitCode"

if ($x86ExitCode -eq 0) {
    Write-Host 'FAIL: the build on the x86 host succeeded, so the defect was not reproduced.'
    $failures++
}
else {
    # These are the two lines that identify the condition in the crash reports of #1745.
    if ($x86Output -notmatch [regex]::Escape("Requested SDK version: $sdkX64")) {
        Write-Host "FAIL: the output does not request the pinned SDK version $sdkX64."
        $failures++
    }

    if ($x86Output -notmatch 'global\.json file:') {
        Write-Host 'FAIL: the output does not name a global.json file.'
        $failures++
    }
    else {
        # The global.json at fault is the one Metalama wrote, not one belonging to the user.
        if ($x86Output -notmatch [regex]::Escape($cacheDirectory)) {
            Write-Host "FAIL: the global.json named is not the one in the cache directory $cacheDirectory."
            $failures++
        }
        else {
            Write-Host "OK: reproduced, and the global.json at fault is Metalama's own, in $cacheDirectory."
        }
    }
}

Write-Section 'Act 2 (the control): the same directory built on the x64 host, which carries the pinned SDK'
$x64Result = Invoke-NativeCommand $x64Dotnet @('build', '-bl:msbuild-x64.binlog')
$x64Output = $x64Result.Output
$x64ExitCode = $x64Result.ExitCode
Write-Host $x64Output
Write-Host "exit code: $x64ExitCode"

if ($x64ExitCode -ne 0) {
    Write-Host 'INCONCLUSIVE: the control build failed as well, so the failure is not attributable to the architecture.'
    $failures++
}
else {
    Write-Host 'OK: the identical build succeeds on the x64 host. The only difference is which host was selected.'
}

Write-Section 'Result'

if ($failures -eq 0) {
    Write-Host 'REPRODUCED: the nested reference-assembly build fails only because it ran on the installation of the other architecture.'
    exit 0
}

Write-Host "NOT REPRODUCED: $failures check(s) failed."
exit 1
