# We need .NET Framework because of our use of ILRepack.
FROM mcr.microsoft.com/dotnet/sdk:9.0.303-windowsservercore-ltsc2022@sha256:39d675f110817d12b616fa1bef354661ce8c0819fc863103bcc63fb7555f2782 AS build

# Copy the source code
WORKDIR /src

COPY . /src/

RUN git config --global --add safe.directory C:/src
