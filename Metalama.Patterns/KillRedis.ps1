# Get a list of all processes whose name starts with "redis-test"
$processes = Get-Process | Where-Object { $_.ProcessName -like "redis-test*" }

# Loop through the list and kill each process
foreach ($process in $processes) {
    Write-Host ("Killing process " + $process.ProcessName + " with ID " + $process.Id)
    Stop-Process -Id $process.Id -Force
}
