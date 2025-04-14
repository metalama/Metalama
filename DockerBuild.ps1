$ErrorActionPreference = "Stop"

if (Test-Path -Path "artifacts") {
  rm -Recurse -Force "artifacts"
}

mkdir artifacts | Out-Null
docker build . -t build-metalama
docker run -v ".\artifacts:c:/src/artifacts" build-metalama pwsh ./Build.ps1 build $args
docker rmi build-metalama