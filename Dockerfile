# escape=`

FROM mcr.microsoft.com/windows/servercore:ltsc2025

# The initial shell is PowerShell Desktop.
SHELL ["powershell", "-Command"]

##################################################################################################################################
## COMMON REQUIREMENTS: 
## Should be shared by different Dockerfiles as much as possible to promote sharing the image layers.     

# Prepare environment
ENV PSExecutionPolicyPreference=Bypass
ENV POWERSHELL_UPDATECHECK=FALSE
ENV TEMP=C:\Temp
ENV TMP=C:\Temp

# Enable long path support
RUN Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1

# Download and install Git for Windows.
RUN Invoke-WebRequest -Uri https://github.com/git-for-windows/git/releases/download/v2.50.0.windows.1/MinGit-2.50.0-64-bit.zip -OutFile MinGit.zip; `
    Expand-Archive c:\\MinGit.zip -DestinationPath C:\\git; `
    Remove-Item C:\\MinGit.zip

# Install PowerShell Core.
RUN Invoke-WebRequest -Uri https://github.com/PowerShell/PowerShell/releases/download/v7.5.2/PowerShell-7.5.2-win-x64.msi -OutFile PowerShell.msi; `
    $process = Start-Process msiexec.exe -Wait -PassThru -ArgumentList '/I PowerShell.msi /quiet'; `
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }; `
    Remove-Item PowerShell.msi

# Install Azure CLI
RUN Invoke-WebRequest -Uri https://aka.ms/installazurecliwindowsx64 -OutFile AzureCLI.msi; `
    $process = Start-Process msiexec.exe -Wait -PassThru -ArgumentList '/I AzureCLI.msi /quiet'; `
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }; `
    Remove-Item AzureCLI.msi

# Add to PATH  
RUN $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine'); `
    $pathsToAdd = @('C:\Program Files\PowerShell\7', 'C:\git\cmd', 'C:\git\bin', 'C:\git\usr\bin', 'C:\Program Files\dotnet'); `
    $newPath = $currentPath + ';' + ($pathsToAdd -join ';'); `
    [Environment]::SetEnvironmentVariable('PATH', $newPath, 'Machine')


# Download the .NET installer. We will need it several times.
RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; 

##################################################################################################################################
## SPECIFIC REQUIREMENTS:                  
## Components and versions specific for this image should go here, ordered from the most likely 
## to be reused to the less likely to be reused.                                                                             


# Install .NET SDK 9 (must match global.json).
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 9.0.305 -InstallDir 'C:\Program Files\dotnet'; 

# Install .NET Runtime 8 for tests.
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 8.0.20 -Runtime dotnet -InstallDir 'C:\Program Files\dotnet'; 

# Clean up.
RUN Remove-Item C:\\dotnet-install.ps1

# Copy Visual Studio configuration. See eng/docker-context/README.md
COPY vsconfig.json /vsconfig.json
COPY VisualStudio.17.Release.chman /VisualStudio.17.Release.chman

# Install Visual Studio Build Tools. 
RUN Invoke-WebRequest -Uri https://aka.ms/vs/17/release/vs_buildtools.exe -OutFile vs_buildtools.exe; `
    $process = Start-Process .\vs_buildtools.exe -NoNewWindow -Wait -PassThru `
        -ArgumentList  "--quiet", "--wait", "--norestart", "--nocache",  "--installPath", "C:\BuildTools", "--installChannelUri", "c:\VisualStudio.17.Release.chman", "--installCatalogUri", "https://download.visualstudio.microsoft.com/download/pr/eb5f7427-d28f-4e06-95cc-093f6c2070c8/3480d7a528bad877857c92843bb1e9ce8ebd48a2bffcee366a98a7343f4d32fb/VisualStudio.vsman", "--productId", "Microsoft.VisualStudio.Product.BuildTools", "--config", "\vsconfig.json"; `        
    if ($process.ExitCode -ne 0) { `
     Get-ChildItem "$env:TEMP\dd_*.log" -ErrorAction SilentlyContinue | ForEach-Object { `
        Write-Host "=== Contents of $($_.Name) ==="; `
        Get-Content $_.FullName; `
        Write-Host "=== End of $($_.Name) ===" `
        }; `
     exit $process.ExitCode; `
     }; `
    Remove-Item C:\\vs_buildtools.exe;



##################################################################################################################################
## COMMON FOOTER:
## The following is required for integration with DockerBuild.ps1 and PostSharp.Engineering. It should not be modified.         

# Create directories for mountpoints
ARG MOUNTPOINTS
RUN if ($env:MOUNTPOINTS) { `
        $mounts = $env:MOUNTPOINTS -split ';'; `
        foreach ($dir in $mounts) { `
            if ($dir) { `
                Write-Host "Creating directory $dir`."; `
                New-Item -ItemType Directory -Path $dir -Force | Out-Null; `
            } `
        } `
    }

# Import environment variables
COPY ReadSecrets.ps1 c:\ReadSecrets.ps1    
COPY secrets.g.json c:\secrets.g.json
RUN c:\ReadSecrets.ps1 c:\secrets.g.json   

# Configure NuGet
ENV NUGET_PACKAGES=c:\packages

# Configure git
ARG SRC_DIR
RUN echo $env:PATH
RUN git config --global --add safe.directory $env:SRC_DIR/
