# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.304-windowsservercore-ltsc2022@sha256:f257fad478574269117348f68407b6f0f02b865b51845a0e99baf3bf85aa0023 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
