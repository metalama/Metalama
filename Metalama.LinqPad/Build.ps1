(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng\src\BuildMetalamaLinqPad.csproj" -- $args
exit $LASTEXITCODE

