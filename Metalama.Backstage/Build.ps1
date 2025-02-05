(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng\src\BuildMetalamaBackstage.csproj" -- $args
exit $LASTEXITCODE

