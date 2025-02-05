(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng\src\BuildMetalamaExtensions.csproj" -- $args
exit $LASTEXITCODE

