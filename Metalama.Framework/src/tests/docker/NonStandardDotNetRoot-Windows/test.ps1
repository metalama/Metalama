$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try {
    C:\CustomDotNet\dotnet.exe run --configfile
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
