# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.301-windowsservercore-ltsc2022@sha256:d208f4104907aeba9722ec08aaded0c92f1d6e2aeff0221b021d8d7a8792cf56 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
