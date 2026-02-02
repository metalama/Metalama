<#
.SYNOPSIS
    Finds C# files that may need Kind check optimization.

.DESCRIPTION
    Searches for pattern matching on IDeclaration types that could be optimized
    by checking the DeclarationKind property first.

.PARAMETER Path
    The root path to search. Defaults to Metalama.Framework.Engine.

.EXAMPLE
    .\Find-KindOptimizationCandidates.ps1

.EXAMPLE
    .\Find-KindOptimizationCandidates.ps1 -Path "..\..\src"
#>

param(
    [string]$Path = "..\src\Metalama.Framework.Engine"
)

$ErrorActionPreference = "Stop"

# Base types for IDeclaration
$declarationTypes = "Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter"

# IDeclaration patterns
$patterns = @(
    # 1. If statement patterns (positive)
    "is I($declarationTypes)\b"

    # 2. If statement patterns (negation)
    "is not I($declarationTypes)\b"

    # 3. Switch statement case patterns
    "case I($declarationTypes)\b"

    # 4. Switch expression arm patterns (with variable)
    "I($declarationTypes)\s+\w+\s*=>"

    # 5. Switch expression arm patterns (with property pattern)
    "I($declarationTypes)\s*\{[^\}]*\}\s*=>"
)

# Pattern to identify already-optimized code (to exclude)
$alreadyOptimizedPattern = "DeclarationKind\.\w+\s*(when|&&)"

# Resolve path
$resolvedPath = Resolve-Path $Path -ErrorAction SilentlyContinue
if (-not $resolvedPath) {
    Write-Error "Path not found: $Path"
    exit 1
}

Write-Host "Searching in: $resolvedPath" -ForegroundColor Cyan
Write-Host "Patterns: $($patterns.Count)" -ForegroundColor Cyan
Write-Host ""

# Find all C# files, excluding tests
$files = Get-ChildItem -Path $resolvedPath -Filter "*.cs" -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\Tests\\" -and $_.FullName -notmatch "\.Tests\." }

Write-Host "Scanning $($files.Count) files..." -ForegroundColor Cyan
Write-Host ""

$results = @{}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    $fileMatches = @()

    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern)
        foreach ($match in $matches) {
            # Get line number
            $beforeMatch = $content.Substring(0, $match.Index)
            $lineNumber = ($beforeMatch -split "`n").Count

            # Get the line content
            $lines = $content -split "`n"
            $lineContent = if ($lineNumber -le $lines.Count) { $lines[$lineNumber - 1].Trim() } else { "" }

            # Skip if this line is already optimized
            if ($lineContent -match $alreadyOptimizedPattern) {
                continue
            }

            # Skip if this looks like a method parameter declaration
            if ($lineContent -match "^\s*(public|private|protected|internal|static|async|override|virtual|abstract|sealed)?\s*(static\s+)?I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\s+\w+\s*[,\)]") {
                continue
            }
            if ($lineContent -match "\(\s*this\s+I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\s+\w+") {
                continue
            }

            # Skip property/field declarations (return types)
            if ($lineContent -match "^\s*(public|private|protected|internal|new|static|readonly|override|virtual|abstract|sealed|\s)+I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter)\??\s+\w+\s*(=>|\{|;)") {
                continue
            }

            # Skip generic constraints
            if ($lineContent -match "where\s+\w+\s*:") {
                continue
            }

            # Skip cast expressions like (IMethod)
            if ($lineContent -match "\(I(Method|Property|Event|Field|Indexer|Constructor|NamedType)\)\s*\w+") {
                continue
            }

            # Skip variable declarations with explicit type
            if ($lineContent -match "^\s*I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\??\s+\w+\s*=") {
                continue
            }

            # Skip as expressions
            if ($lineContent -match "as\s+I(Method|Property|Event|Field|Indexer|Constructor|NamedType)") {
                continue
            }

            $fileMatches += [PSCustomObject]@{
                Line = $lineNumber
                Pattern = $pattern
                Content = $lineContent
            }
        }
    }

    if ($fileMatches.Count -gt 0) {
        $relativePath = $file.FullName.Replace($resolvedPath.Path, "").TrimStart("\", "/")
        $results[$relativePath] = $fileMatches
    }
}

# Output results
Write-Host ("=" * 80) -ForegroundColor Yellow
Write-Host "RESULTS: $($results.Count) files with potential optimization opportunities" -ForegroundColor Yellow
Write-Host ("=" * 80) -ForegroundColor Yellow
Write-Host ""

$sortedKeys = $results.Keys | Sort-Object

foreach ($file in $sortedKeys) {
    Write-Host $file -ForegroundColor Green
    foreach ($match in $results[$file]) {
        Write-Host "  L$($match.Line): $($match.Content)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host ""
Write-Host "Total: $($results.Count) files" -ForegroundColor Cyan

# Return the list for pipeline use
return $sortedKeys
