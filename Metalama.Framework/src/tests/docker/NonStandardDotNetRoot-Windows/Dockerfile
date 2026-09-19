# escape=`

# Use Windows Server Core as base image
FROM mcr.microsoft.com/windows/servercore:ltsc2025

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install PowerShell 7
ARG PWSH_VERSION=7.4.6
RUN $url = \"https://github.com/PowerShell/PowerShell/releases/download/v$env:PWSH_VERSION/PowerShell-$env:PWSH_VERSION-win-x64.msi\"; `
    Write-Host \"Downloading PowerShell $env:PWSH_VERSION from $url\"; `
    Invoke-WebRequest -Uri $url -OutFile PowerShell.msi; `
    Write-Host 'Installing PowerShell 7'; `
    Start-Process msiexec.exe -Wait -ArgumentList '/i', 'PowerShell.msi', '/quiet', '/norestart'; `
    Remove-Item -Force PowerShell.msi

# Define .NET SDK version and custom installation path
ARG DOTNET_VERSION=9.0.101
ARG CUSTOM_DOTNET_PATH=C:\CustomDotNet

# Create custom installation directory
RUN New-Item -ItemType Directory -Force -Path $env:CUSTOM_DOTNET_PATH | Out-Null

# Download and install .NET SDK to custom path
RUN $url = \"https://dotnetcli.azureedge.net/dotnet/Sdk/$env:DOTNET_VERSION/dotnet-sdk-$env:DOTNET_VERSION-win-x64.zip\"; `
    Write-Host \"Downloading .NET SDK $env:DOTNET_VERSION from $url\"; `
    Invoke-WebRequest -Uri $url -OutFile dotnet.zip; `
    Write-Host 'Extracting .NET SDK to custom path'; `
    Expand-Archive dotnet.zip -DestinationPath $env:CUSTOM_DOTNET_PATH; `
    Remove-Item -Force dotnet.zip

# Environment variable to use custom .NET installation
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

WORKDIR /app

CMD ["pwsh"]
