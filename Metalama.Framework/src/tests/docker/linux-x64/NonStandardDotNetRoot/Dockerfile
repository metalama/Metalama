FROM ubuntu:24.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    wget \
    apt-transport-https \
    software-properties-common \
    && rm -rf /var/lib/apt/lists/*

# Install PowerShell 7
RUN wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y powershell && \
    rm -rf /var/lib/apt/lists/*

# Define .NET SDK version and custom installation path
ARG DOTNET_VERSION=9.0.101
ARG CUSTOM_DOTNET_PATH=/opt/custom-dotnet

# Create custom installation directory
RUN mkdir -p $CUSTOM_DOTNET_PATH

# Download and install .NET SDK to custom path
RUN curl -sSL https://dotnetcli.azureedge.net/dotnet/Sdk/$DOTNET_VERSION/dotnet-sdk-$DOTNET_VERSION-linux-x64.tar.gz \
    -o dotnet.tar.gz && \
    tar -xzf dotnet.tar.gz -C $CUSTOM_DOTNET_PATH && \
    rm dotnet.tar.gz

# Verify installations
RUN pwsh -Command 'Write-Host "PowerShell $($PSVersionTable.PSVersion) installed"'
RUN /opt/custom-dotnet/dotnet --version


# Environment variable for .NET SDK
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

WORKDIR /app

CMD ["pwsh"]
