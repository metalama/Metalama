(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng\src\BuildMetalamaPatterns.csproj" -- $args
exit $LASTEXITCODE

