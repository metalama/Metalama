# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.203-windowsservercore-ltsc2022@sha256:56117c03ff717d62c69df3b512af541d2c2e822f88f548e2cdbbc6cb06ec2bd6 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
