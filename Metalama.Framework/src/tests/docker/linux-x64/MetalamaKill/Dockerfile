FROM ubuntu:24.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    wget \
    apt-transport-https \
    software-properties-common \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Install PowerShell 7
RUN wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y powershell && \
    rm -rf /var/lib/apt/lists/*

# Install .NET SDK to default path
ARG DOTNET_VERSION=9.0.101
RUN curl -sSL https://dotnetcli.azureedge.net/dotnet/Sdk/$DOTNET_VERSION/dotnet-sdk-$DOTNET_VERSION-linux-x64.tar.gz \
    -o dotnet.tar.gz && \
    mkdir -p /usr/share/dotnet && \
    tar -xzf dotnet.tar.gz -C /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm dotnet.tar.gz

# Verify installations
RUN pwsh -Command 'Write-Host "PowerShell $($PSVersionTable.PSVersion) installed"'
RUN dotnet --version

# Environment variable for .NET SDK
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

# Create directories for mountpoints
ARG MOUNTPOINTS
RUN if [ -n "$MOUNTPOINTS" ]; then \
        OLD_IFS="$IFS"; \
        IFS=':'; \
        set -- $MOUNTPOINTS; \
        IFS="$OLD_IFS"; \
        for dir in "$@"; do \
            if [ -n "$dir" ]; then \
                echo "Creating directory $dir."; \
                mkdir -p "$dir"; \
            fi; \
        done; \
    fi

WORKDIR /app

CMD ["pwsh"]
