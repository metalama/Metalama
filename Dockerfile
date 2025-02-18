FROM mcr.microsoft.com/dotnet/sdk:9.0.102 AS build

WORKDIR /src

COPY . /src/

RUN pwsh ./Build.ps1 build

FROM scratch

COPY --from=build /src/artifacts /artifacts