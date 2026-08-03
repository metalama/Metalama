# escape=`

# Reproduces the cause behind issue #1745: the nested reference-assembly build runs on a .NET installation of a
# different architecture than the one that built the project, so the .NET SDK version that Metalama pins in the
# global.json it writes is not among the ones installed there.
#
# The two installations carry deliberately different .NET SDK versions, which is the ordinary situation: the 32-bit
# SDK is installed by some Visual Studio workloads and is rarely kept in step with the 64-bit one. A 32-bit host such
# as the MSBuild.exe of Visual Studio builds the project with the 64-bit SDK but expands %ProgramFiles% to
# 'C:\Program Files (x86)', which is how the 32-bit installation used to be selected. The selection itself is covered
# by PlatformInfoArchitectureTests in Metalama.Backstage.Tests; this scenario covers the build failure that follows.

FROM mcr.microsoft.com/windows/servercore:ltsc2025

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# The version installed as the 64-bit SDK, i.e. the one that would have built the project, and which Metalama pins.
ARG SDK_X64=10.0.302

# The version installed as the 32-bit SDK. It must differ from SDK_X64, and must not be one that SDK_X64 could roll
# forward to, otherwise the scenario would pass vacuously.
ARG SDK_X86=8.0.423

RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; `
    & .\dotnet-install.ps1 -Version $env:SDK_X64 -Architecture x64 -InstallDir 'C:\Program Files\dotnet'; `
    & .\dotnet-install.ps1 -Version $env:SDK_X86 -Architecture x86 -InstallDir 'C:\Program Files (x86)\dotnet'; `
    Remove-Item -Force dotnet-install.ps1

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV SDK_X64=$SDK_X64
ENV SDK_X86=$SDK_X86

WORKDIR C:/app
COPY test.ps1 C:/app/test.ps1

ENTRYPOINT ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-NoProfile", "-File", "C:\\app\\test.ps1"]
