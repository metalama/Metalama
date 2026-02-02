<#
.SYNOPSIS
    Finds C# files that may need Kind check optimization.

.DESCRIPTION
    Searches for pattern matching on IDeclaration, ISymbol, and SyntaxNode types
    that could be optimized by checking the discriminator property first.

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

# =============================================================================
# IDeclaration patterns (DeclarationKind)
# =============================================================================
$declarationTypes = "Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter"

$declarationPatterns = @(
    "is I($declarationTypes)\b"
    "is not I($declarationTypes)\b"
    "case I($declarationTypes)\b"
    "I($declarationTypes)\s+\w+\s*=>"
    "I($declarationTypes)\s*\{[^\}]*\}\s*=>"
)

# =============================================================================
# ISymbol patterns (SymbolKind)
# =============================================================================
$symbolPatterns = @(
    "is I\w+Symbol\b"
    "is not I\w+Symbol\b"
    "case I\w+Symbol\b"
    "I\w+Symbol\s+\w+\s*=>"
    "I\w+Symbol\s*\{[^\}]*\}\s*=>"
)

# =============================================================================
# SyntaxNode patterns (SyntaxKind)
# =============================================================================
$syntaxPatterns = @(
    "is \w+Syntax\b"
    "is not \w+Syntax\b"
    "case \w+Syntax\b"
    "\w+Syntax\s+\w+\s*=>"
    "\w+Syntax\s*\{[^\}]*\}\s*=>"
)

# Combine all patterns
$patterns = $declarationPatterns + $symbolPatterns + $syntaxPatterns

# =============================================================================
# Patterns to identify already-optimized code (to exclude)
# =============================================================================
$alreadyOptimizedPatterns = @(
    "DeclarationKind\.\w+\s*(when|&&)"
    "SymbolKind\.\w+\s*(when|&&)"
    "SyntaxKind\.\w+\s*(when|&&)"
    "\.Kind\s*==\s*SymbolKind\."
    "\.Kind\(\)\s*==\s*SyntaxKind\."
    "\.DeclarationKind\s*==\s*DeclarationKind\."
)

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
            $isOptimized = $false
            foreach ($optPattern in $alreadyOptimizedPatterns) {
                if ($lineContent -match $optPattern) {
                    $isOptimized = $true
                    break
                }
            }
            if ($isOptimized) { continue }

            # Skip if this looks like a method parameter declaration
            if ($lineContent -match "^\s*(public|private|protected|internal|static|async|override|virtual|abstract|sealed)?\s*(static\s+)?I\w+\s+\w+\s*[,\)]") {
                continue
            }
            if ($lineContent -match "\(\s*this\s+I\w+\s+\w+") {
                continue
            }

            # Skip property/field declarations (return types)
            if ($lineContent -match "^\s*(public|private|protected|internal|new|static|readonly|override|virtual|abstract|sealed|\s)+I\w+\??\s+\w+\s*(=>|\{|;)") {
                continue
            }

            # Skip generic constraints
            if ($lineContent -match "where\s+\w+\s*:") {
                continue
            }

            # Skip cast expressions like (IMethod)
            if ($lineContent -match "\(I\w+\)\s*\w+") {
                continue
            }

            # Skip variable declarations with explicit type
            if ($lineContent -match "^\s*I\w+\??\s+\w+\s*=") {
                continue
            }

            # Skip as expressions
            if ($lineContent -match "as\s+I\w+") {
                continue
            }

            $fileMatches += [PSCustomObject]@{
                Line = $lineNumber
                Content = $lineContent
            }
        }
    }

    if ($fileMatches.Count -gt 0) {
        $relativePath = $file.FullName.Replace($resolvedPath.Path, "").TrimStart("\", "/")
        $results[$relativePath] = $fileMatches
    }
}

# Output results - simple list of files sorted by path
Write-Host ("=" * 80) -ForegroundColor Yellow
Write-Host "RESULTS: $($results.Count) files with potential optimization opportunities" -ForegroundColor Yellow
Write-Host ("=" * 80) -ForegroundColor Yellow
Write-Host ""

$sortedFiles = $results.Keys | Sort-Object

foreach ($file in $sortedFiles) {
    Write-Host $file -ForegroundColor Green
    foreach ($match in $results[$file]) {
        Write-Host "  L$($match.Line): $($match.Content)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host ""
Write-Host "Total: $($results.Count) files" -ForegroundColor Cyan

# Return the distinct sorted list of files for pipeline use
return $sortedFiles
