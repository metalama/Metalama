# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.300-windowsservercore-ltsc2022@sha256:7e083f723807e4dace4a867333615833367b709cbe45adb193093cb26653e5e9 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
