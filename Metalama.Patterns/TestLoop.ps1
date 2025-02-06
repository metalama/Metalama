& dotnet test Metalama.Patterns.sln 

if ($LASTEXITCODE -ne 0) {
    Throw "dotnet test failed."
}

for ($i = 2; $i -le 100; $i++) {
    
    $currentTime = Get-Date
    Write-Host "=================================================== Running iteration $i at $currentTime ===================================================================="
    
    & dotnet test Metalama.Patterns.sln --no-build

    if ($LASTEXITCODE -ne 0) {
        Throw "dotnet test failed."
    }

}

Write-Host "Loop finished."
