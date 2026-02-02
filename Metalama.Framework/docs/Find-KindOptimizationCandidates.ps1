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
    # If statement patterns (positive)
    "is I($declarationTypes)\b"

    # If statement patterns (negation)
    "is not I($declarationTypes)\b"

    # Switch statement case patterns
    "case I($declarationTypes)\b"

    # Switch expression arm patterns (with variable)
    "I($declarationTypes)\s+\w+\s*=>"

    # Switch expression arm patterns (with property pattern)
    "I($declarationTypes)\s*\{[^\}]*\}\s*=>"
)

# =============================================================================
# ISymbol patterns (SymbolKind)
# =============================================================================
# Match any I*Symbol pattern (IMethodSymbol, IPropertySymbol, etc.)
$symbolPatterns = @(
    # If statement patterns (positive)
    "is I\w+Symbol\b"

    # If statement patterns (negation)
    "is not I\w+Symbol\b"

    # Switch statement case patterns
    "case I\w+Symbol\b"

    # Switch expression arm patterns (with variable)
    "I\w+Symbol\s+\w+\s*=>"

    # Switch expression arm patterns (with property pattern)
    "I\w+Symbol\s*\{[^\}]*\}\s*=>"
)

# =============================================================================
# SyntaxNode patterns (SyntaxKind)
# =============================================================================
# Match any *Syntax pattern (MethodDeclarationSyntax, BlockSyntax, etc.)
$syntaxPatterns = @(
    # If statement patterns (positive)
    "is \w+Syntax\b"

    # If statement patterns (negation)
    "is not \w+Syntax\b"

    # Switch statement case patterns
    "case \w+Syntax\b"

    # Switch expression arm patterns (with variable)
    "\w+Syntax\s+\w+\s*=>"

    # Switch expression arm patterns (with property pattern)
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
Write-Host "Patterns: $($patterns.Count) (IDeclaration: $($declarationPatterns.Count), ISymbol: $($symbolPatterns.Count), SyntaxNode: $($syntaxPatterns.Count))" -ForegroundColor Cyan
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

            # Determine category
            $category = if ($pattern -match "Symbol") { "ISymbol" } elseif ($pattern -match "Syntax") { "SyntaxNode" } else { "IDeclaration" }

            $fileMatches += [PSCustomObject]@{
                Line = $lineNumber
                Pattern = $pattern
                Content = $lineContent
                Category = $category
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

# Group by category
$declarationFiles = @{}
$symbolFiles = @{}
$syntaxFiles = @{}

foreach ($file in $results.Keys) {
    $matches = $results[$file]
    $hasDeclaration = $matches | Where-Object { $_.Category -eq "IDeclaration" }
    $hasSymbol = $matches | Where-Object { $_.Category -eq "ISymbol" }
    $hasSyntax = $matches | Where-Object { $_.Category -eq "SyntaxNode" }

    if ($hasDeclaration) { $declarationFiles[$file] = $hasDeclaration }
    if ($hasSymbol) { $symbolFiles[$file] = $hasSymbol }
    if ($hasSyntax) { $syntaxFiles[$file] = $hasSyntax }
}

# Output by category
if ($declarationFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "=== IDeclaration patterns ($($declarationFiles.Count) files) ===" -ForegroundColor Magenta
    foreach ($file in ($declarationFiles.Keys | Sort-Object)) {
        Write-Host $file -ForegroundColor Green
        foreach ($match in $declarationFiles[$file]) {
            Write-Host "  L$($match.Line): $($match.Content)" -ForegroundColor Gray
        }
    }
}

if ($symbolFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "=== ISymbol patterns ($($symbolFiles.Count) files) ===" -ForegroundColor Magenta
    foreach ($file in ($symbolFiles.Keys | Sort-Object)) {
        Write-Host $file -ForegroundColor Green
        foreach ($match in $symbolFiles[$file]) {
            Write-Host "  L$($match.Line): $($match.Content)" -ForegroundColor Gray
        }
    }
}

if ($syntaxFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "=== SyntaxNode patterns ($($syntaxFiles.Count) files) ===" -ForegroundColor Magenta
    foreach ($file in ($syntaxFiles.Keys | Sort-Object)) {
        Write-Host $file -ForegroundColor Green
        foreach ($match in $syntaxFiles[$file]) {
            Write-Host "  L$($match.Line): $($match.Content)" -ForegroundColor Gray
        }
    }
}

Write-Host ""
Write-Host ("=" * 80) -ForegroundColor Yellow
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total files: $($results.Count)" -ForegroundColor Cyan
Write-Host "  IDeclaration: $($declarationFiles.Count) files" -ForegroundColor Cyan
Write-Host "  ISymbol: $($symbolFiles.Count) files" -ForegroundColor Cyan
Write-Host "  SyntaxNode: $($syntaxFiles.Count) files" -ForegroundColor Cyan

# Return the list for pipeline use
return $results.Keys | Sort-Object
