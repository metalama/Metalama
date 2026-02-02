# Optimize Kind Checks in Current Branch

Automatically find and apply Kind optimization patterns to files changed in the current branch.

## Overview

This command optimizes type pattern matching by checking discriminator properties first:
- **IDeclaration** → `DeclarationKind` enum
- **ISymbol** → `SymbolKind` enum
- **SyntaxNode** → `SyntaxKind` enum (via `.Kind()` method)

## Execution Steps

### 1. Determine Base Branch and Changed Files

```bash
# Get current branch name
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)

# Extract version (e.g., topic/2026.1/xxx -> develop/2026.1)
BASE_BRANCH=$(echo "$CURRENT_BRANCH" | sed -E 's/topic\/([0-9]+\.[0-9]+)\/.*/develop\/\1/')

# Get changed .cs files in Engine (excluding tests)
git diff --name-only "$BASE_BRANCH"...HEAD -- "Metalama.Framework/src/Metalama.Framework.Engine/**/*.cs" | grep -v -E "(Tests|\.Tests\.)"
```

### 2. Run the Search Script

```powershell
cd Metalama.Framework/docs
pwsh -File Find-KindOptimizationCandidates.ps1
```

The script finds patterns in three categories:
- **IDeclaration**: `is IMethod`, `case IProperty`, `IField field =>`
- **ISymbol**: `is IMethodSymbol`, `case IPropertySymbol`, `IFieldSymbol field =>`
- **SyntaxNode**: `is MethodDeclarationSyntax`, `case PropertyDeclarationSyntax`

### 3. Launch Parallel Subagents by Category

For each file with candidates, launch a `kind-optimization-transformer` subagent.

**Group files by category** and process in batches of 5-10:

#### IDeclaration Optimization Prompt:
```
Optimize DeclarationKind checks in file: {filepath}

Transformations:
- `if (x is IMethod m)` → `if (x.DeclarationKind == DeclarationKind.Method && x is IMethod m)`
- `case IProperty p:` → `case { DeclarationKind: DeclarationKind.Property } and IProperty p:`
- `IMethod m =>` → `{ DeclarationKind: DeclarationKind.Method } and IMethod m =>`

Skip: IType/INamedType/ITypeParameter (use TypeKind), IRef<T>, already-optimized, parameters/properties.
```

#### ISymbol Optimization Prompt:
```
Optimize SymbolKind checks in file: {filepath}

Transformations:
- `if (symbol is IMethodSymbol m)` → `if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol m)`
- `case IPropertySymbol p:` → `case { Kind: SymbolKind.Property } and IPropertySymbol p:`
- `IMethodSymbol m =>` → `{ Kind: SymbolKind.Method } and IMethodSymbol m =>`

Skip: already-optimized, parameters/properties.
```

#### SyntaxNode Optimization Prompt:
```
Optimize SyntaxKind checks in file: {filepath}

Transformations:
- `if (node is MethodDeclarationSyntax m)` → `if (node.Kind() == SyntaxKind.MethodDeclaration && node is MethodDeclarationSyntax m)`
- `case PropertyDeclarationSyntax p:` → `case { } when node.Kind() == SyntaxKind.PropertyDeclaration && node is PropertyDeclarationSyntax p:`
- `MethodDeclarationSyntax m =>` → Pattern with Kind() check

Note: SyntaxNode uses `.Kind()` method, not `.Kind` property.
Skip: already-optimized, parameters/properties.
```

### 4. Build and Fix

After subagents complete:

```bash
dotnet build Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf --no-restore -v q
```

Common fixes needed:
- Add `?.` for nullable access (e.g., `ContainingDeclaration?.DeclarationKind`)
- Add missing enum cases in switch statements
- Handle `RecordDeclarationSyntax` which has two SyntaxKinds

Use **Opus model** for fixing complex build errors.

### 5. Test and Validate

```bash
dotnet test Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests -f net8.0 --no-build -v q
```

If tests fail, analyze and fix. Common issues:
- Missing enum cases
- Semantic changes in pattern matching order
- Null reference after adding Kind checks

### 6. Summary Report

Report by category:
```
## Summary

### IDeclaration
- Files analyzed: N
- Patterns transformed: N
- Patterns skipped: N

### ISymbol
- Files analyzed: N
- Patterns transformed: N
- Patterns skipped: N

### SyntaxNode
- Files analyzed: N
- Patterns transformed: N
- Patterns skipped: N

### Status
- Build: Pass/Fail
- Tests: N passed, N failed
```

## Type Mappings

### DeclarationKind → IDeclaration
| Kind | Type |
|------|------|
| Method | IMethod |
| Property | IProperty |
| Field | IField |
| Event | IEvent |
| Constructor | IConstructor |
| Indexer | IIndexer |
| Parameter | IParameter |
| TypeParameter | ITypeParameter |
| NamedType | INamedType |
| Namespace | INamespace |
| Compilation | ICompilation |
| AssemblyReference | IAssembly |

### SymbolKind → ISymbol
| Kind | Type |
|------|------|
| Method | IMethodSymbol |
| Property | IPropertySymbol |
| Field | IFieldSymbol |
| Event | IEventSymbol |
| NamedType | INamedTypeSymbol |
| Namespace | INamespaceSymbol |
| Parameter | IParameterSymbol |
| TypeParameter | ITypeParameterSymbol |
| Local | ILocalSymbol |
| Assembly | IAssemblySymbol |

### SyntaxKind → SyntaxNode (common)
| Kind | Type |
|------|------|
| MethodDeclaration | MethodDeclarationSyntax |
| PropertyDeclaration | PropertyDeclarationSyntax |
| FieldDeclaration | FieldDeclarationSyntax |
| ClassDeclaration | ClassDeclarationSyntax |
| StructDeclaration | StructDeclarationSyntax |
| InterfaceDeclaration | InterfaceDeclarationSyntax |
| Block | BlockSyntax |
| IfStatement | IfStatementSyntax |
| ReturnStatement | ReturnStatementSyntax |

## Reference

- Documentation: `Metalama.Framework/docs/kind-check-optimization.md`
- Search script: `Metalama.Framework/docs/Find-KindOptimizationCandidates.ps1`
- Agent: `.claude/agents/kind-optimization-transformer.md`
