(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot/eng/src/BuildMetalama.csproj" -- $args
exit $LASTEXITCODE

