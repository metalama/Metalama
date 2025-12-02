# Run-Samples.ps1
# Runs LinqPad samples against a demo solution using lprun.exe
#
# Usage:
#   .\Run-Samples.ps1                                        # Run all samples with default solution
#   .\Run-Samples.ps1 -SolutionPath "C:\path\to\solution.sln" # Run all with custom solution
#   .\Run-Samples.ps1 -Query "Sample.linq"                   # Run single sample
#
# Prerequisites:
#   - LinqPad 9 installed (lprun.exe in PATH or standard location)
#   - Demo solution built

param(
    [string]$SolutionPath = "$PSScriptRoot\..\..\tests\FrogHattery\FrogHattery.sln",
    [string]$Query
)

# Find lprun executable (prefer LINQPad 9, fall back to 8 and 7)
$lprun = Get-Command lprun9.exe -ErrorAction SilentlyContinue
if (-not $lprun) {
    $lprun = Get-Command "$env:ProgramFiles\LINQPad9\lprun9.exe" -ErrorAction SilentlyContinue
}
if (-not $lprun) {
    $lprun = Get-Command lprun8.exe -ErrorAction SilentlyContinue
}
if (-not $lprun) {
    $lprun = Get-Command "$env:ProgramFiles\LINQPad8\lprun8.exe" -ErrorAction SilentlyContinue
}
if (-not $lprun) {
    $lprun = Get-Command lprun.exe -ErrorAction SilentlyContinue
}
if (-not $lprun) {
    $lprun = Get-Command "$env:ProgramFiles\LINQPad7\lprun.exe" -ErrorAction SilentlyContinue
}
if (-not $lprun) {
    Write-Error "lprun.exe not found. Please install LinqPad 9 or add it to PATH."
    exit 1
}

# Resolve solution path
$SolutionPath = Resolve-Path $SolutionPath -ErrorAction SilentlyContinue
if (-not $SolutionPath) {
    Write-Error "Solution not found: $SolutionPath"
    exit 1
}

Write-Host "Using solution: $SolutionPath" -ForegroundColor Cyan
Write-Host "Using lprun: $($lprun.Source)" -ForegroundColor Cyan

# Set environment variable
$env:METALAMA_DEMO_SOLUTION = $SolutionPath

# Get samples to run
if ($Query) {
    # Single file specified
    $samplePath = if ([System.IO.Path]::IsPathRooted($Query)) { $Query } else { Join-Path $PSScriptRoot $Query }
    if (-not (Test-Path $samplePath)) {
        Write-Error "Sample not found: $samplePath"
        exit 1
    }
    $samples = @(Get-Item $samplePath)
} else {
    # Run all .linq files
    $samples = Get-ChildItem -Path $PSScriptRoot -Filter "*.linq"
}

$passed = 0
$failed = 0

foreach ($sample in $samples) {
    Write-Host "`n=== Running: $($sample.Name) ===" -ForegroundColor Yellow

    try {
        & $lprun.Source -format=csv $sample.FullName 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "PASSED" -ForegroundColor Green
            $passed++
        } else {
            Write-Host "FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
            $failed++
        }
    }
    catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

exit $failed
