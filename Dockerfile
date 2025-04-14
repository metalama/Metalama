# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.203-windowsservercore-ltsc2022 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
