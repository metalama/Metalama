# Kind Check Optimization Patterns

This document describes how to optimize type checks on `IDeclaration`, `ISymbol`, and `SyntaxNode` by checking the discriminator property before pattern matching.

## Problem

Pattern matching using `is` and `switch` on these types is expensive because it involves runtime type checks. Performance can be significantly improved by first checking the discriminator property (an enum), then using pattern matching only for the cast.

## Discriminator Properties

| Type | Discriminator | Type |
|------|---------------|------|
| `IDeclaration` | `.DeclarationKind` | `DeclarationKind` enum |
| `ISymbol` | `.Kind` | `SymbolKind` enum |
| `SyntaxNode` | `.Kind()` method | `SyntaxKind` enum |

## Golden Rule

**Always use `when x is Type variable`** - never use explicit casts like `((Type)x)`. This ensures:
- Readability
- No duplicate casts when variable is used multiple times
- Consistent pattern across codebase

---

## Pattern A: Switch Statement

### Before
```csharp
switch (declaration)
{
    case IMethod method:
        DoSomething(method);
        break;
    case IProperty property:
        DoSomethingElse(property);
        break;
    default:
        throw new ArgumentOutOfRangeException();
}
```

### After
```csharp
switch (declaration.DeclarationKind)
{
    case DeclarationKind.Method when declaration is IMethod method:
        DoSomething(method);
        break;
    case DeclarationKind.Property when declaration is IProperty property:
        DoSomethingElse(property);
        break;
    default:
        throw new ArgumentOutOfRangeException();
}
```

### With Property Patterns
```csharp
// Before
switch (declaration)
{
    case IConstructor { IsStatic: true } ctor:
        HandleStaticCtor(ctor);
        break;
    case IConstructor { IsStatic: false } ctor:
        HandleInstanceCtor(ctor);
        break;
    case IField { OverridingProperty: null } field:
        HandleField(field);
        break;
}

// After
switch (declaration.DeclarationKind)
{
    case DeclarationKind.Constructor when declaration is IConstructor { IsStatic: true } ctor:
        HandleStaticCtor(ctor);
        break;
    case DeclarationKind.Constructor when declaration is IConstructor { IsStatic: false } ctor:
        HandleInstanceCtor(ctor);
        break;
    case DeclarationKind.Field when declaration is IField { OverridingProperty: null } field:
        HandleField(field);
        break;
}
```

---

## Pattern B: Switch Expression

### Before
```csharp
return symbol switch
{
    IMethodSymbol method => method.ReturnType,
    IPropertySymbol property => property.Type,
    IFieldSymbol field => field.Type,
    _ => null
};
```

### After
```csharp
return symbol.Kind switch
{
    SymbolKind.Method when symbol is IMethodSymbol method => method.ReturnType,
    SymbolKind.Property when symbol is IPropertySymbol property => property.Type,
    SymbolKind.Field when symbol is IFieldSymbol field => field.Type,
    _ => null
};
```

### With Property Patterns
```csharp
// Before
return method switch
{
    IMethodSymbol { IsStatic: true } m => HandleStatic(m),
    IMethodSymbol { IsStatic: false } m => HandleInstance(m),
    _ => null
};

// After
return method.Kind switch
{
    SymbolKind.Method when method is IMethodSymbol { IsStatic: true } m => HandleStatic(m),
    SymbolKind.Method when method is IMethodSymbol { IsStatic: false } m => HandleInstance(m),
    _ => null
};
```

---

## Pattern C: If Statement

### Before
```csharp
if (symbol is IMethodSymbol method)
{
    DoSomething(method);
}
```

### After
```csharp
if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
{
    DoSomething(method);
}
```

### With Additional Conditions
```csharp
// Before
if (symbol is IMethodSymbol method && method.IsStatic && method.Parameters.Length > 0)
{
    DoSomething(method);
}

// After
if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method && method.IsStatic && method.Parameters.Length > 0)
{
    DoSomething(method);
}
```

### With Property Patterns
```csharp
// Before
if (symbol is IMethodSymbol { IsStatic: true, ReturnsVoid: false } method)
{
    DoSomething(method);
}

// After
if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { IsStatic: true, ReturnsVoid: false } method)
{
    DoSomething(method);
}
```

---

## Pattern D: Tuple with Symbol/Declaration

### Before
```csharp
return (semantic.Symbol, targetKind) switch
{
    (IMethodSymbol method, AspectReferenceTargetKind.Self) => method.ToSemantic(kind),
    (IPropertySymbol property, AspectReferenceTargetKind.PropertyGetAccessor) => property.GetMethod,
    (IPropertySymbol property, AspectReferenceTargetKind.PropertySetAccessor) => property.SetMethod,
    (IEventSymbol @event, AspectReferenceTargetKind.EventAddAccessor) => @event.AddMethod,
    _ => throw new InvalidOperationException()
};
```

### After
```csharp
return (semantic.Symbol.Kind, targetKind) switch
{
    (SymbolKind.Method, AspectReferenceTargetKind.Self)
        when semantic.Symbol is IMethodSymbol method => method.ToSemantic(kind),
    (SymbolKind.Property, AspectReferenceTargetKind.PropertyGetAccessor)
        when semantic.Symbol is IPropertySymbol property => property.GetMethod,
    (SymbolKind.Property, AspectReferenceTargetKind.PropertySetAccessor)
        when semantic.Symbol is IPropertySymbol property => property.SetMethod,
    (SymbolKind.Event, AspectReferenceTargetKind.EventAddAccessor)
        when semantic.Symbol is IEventSymbol @event => @event.AddMethod,
    _ => throw new InvalidOperationException()
};
```

---

## Pattern E: Two Symbols/Declarations Comparison

When comparing two objects of the same base type (e.g., in equality comparers):

### Before
```csharp
// Assume x.Kind == y.Kind is already verified
switch (x, y)
{
    case (IMethodSymbol methodX, IMethodSymbol methodY):
        return CompareMethods(methodX, methodY);
    case (IPropertySymbol propertyX, IPropertySymbol propertyY):
        return CompareProperties(propertyX, propertyY);
    case (IFieldSymbol fieldX, IFieldSymbol fieldY):
        return CompareFields(fieldX, fieldY);
    default:
        return 0;
}
```

### After
```csharp
switch (x.Kind)
{
    case SymbolKind.Method when x is IMethodSymbol methodX && y is IMethodSymbol methodY:
        return CompareMethods(methodX, methodY);
    case SymbolKind.Property when x is IPropertySymbol propertyX && y is IPropertySymbol propertyY:
        return CompareProperties(propertyX, propertyY);
    case SymbolKind.Field when x is IFieldSymbol fieldX && y is IFieldSymbol fieldY:
        return CompareFields(fieldX, fieldY);
    default:
        return 0;
}
```

---

## Pattern F: SyntaxNode Patterns

SyntaxNode uses `.Kind()` method (not property) and `SyntaxKind` enum.

### Before
```csharp
switch (node)
{
    case MethodDeclarationSyntax method:
        return ProcessMethod(method);
    case PropertyDeclarationSyntax property:
        return ProcessProperty(property);
    case ClassDeclarationSyntax classDecl:
        return ProcessClass(classDecl);
}
```

### After
```csharp
switch (node.Kind())
{
    case SyntaxKind.MethodDeclaration when node is MethodDeclarationSyntax method:
        return ProcessMethod(method);
    case SyntaxKind.PropertyDeclaration when node is PropertyDeclarationSyntax property:
        return ProcessProperty(property);
    case SyntaxKind.ClassDeclaration when node is ClassDeclarationSyntax classDecl:
        return ProcessClass(classDecl);
}
```

### If Statement
```csharp
// Before
if (node is BlockSyntax block)
{
    ProcessBlock(block);
}

// After
if (node.Kind() == SyntaxKind.Block && node is BlockSyntax block)
{
    ProcessBlock(block);
}
```

---

## Type Mappings

### DeclarationKind → IDeclaration Types

| DeclarationKind | Interface Type | Notes |
|-----------------|----------------|-------|
| `Method` | `IMethod` | Regular methods |
| `Property` | `IProperty` | |
| `Field` | `IField` | |
| `Event` | `IEvent` | |
| `Constructor` | `IConstructor` | |
| `Indexer` | `IIndexer` | |
| `Parameter` | `IParameter` | |
| `TypeParameter` | `ITypeParameter` | |
| `NamedType` | `INamedType` | Classes, structs, interfaces, etc. |
| `Namespace` | `INamespace` | |
| `Compilation` | `ICompilation` | |
| `Attribute` | `IAttribute` | |
| `Finalizer` | `IMethod` | Check `IMethod.MethodKind == MethodKind.Finalizer` |
| `Operator` | `IMethod` | Check `IMethod.MethodKind == MethodKind.Operator` |
| `AssemblyReference` | `IAssembly` | |

### SymbolKind → ISymbol Types

| SymbolKind | Interface Type |
|------------|----------------|
| `Method` | `IMethodSymbol` |
| `Property` | `IPropertySymbol` |
| `Field` | `IFieldSymbol` |
| `Event` | `IEventSymbol` |
| `NamedType` | `INamedTypeSymbol` |
| `Namespace` | `INamespaceSymbol` |
| `Parameter` | `IParameterSymbol` |
| `TypeParameter` | `ITypeParameterSymbol` |
| `ArrayType` | `IArrayTypeSymbol` |
| `PointerType` | `IPointerTypeSymbol` |
| `FunctionPointerType` | `IFunctionPointerTypeSymbol` |
| `DynamicType` | `IDynamicTypeSymbol` |
| `ErrorType` | `IErrorTypeSymbol` |
| `Local` | `ILocalSymbol` |
| `Discard` | `IDiscardSymbol` |
| `RangeVariable` | `IRangeVariableSymbol` |
| `Label` | `ILabelSymbol` |
| `Assembly` | `IAssemblySymbol` |
| `NetModule` | `IModuleSymbol` |
| `Alias` | `IAliasSymbol` |
| `Preprocessing` | `IPreprocessingSymbol` |

### SyntaxKind → Syntax Types (Common)

| SyntaxKind | Syntax Type |
|------------|-------------|
| `MethodDeclaration` | `MethodDeclarationSyntax` |
| `PropertyDeclaration` | `PropertyDeclarationSyntax` |
| `FieldDeclaration` | `FieldDeclarationSyntax` |
| `EventDeclaration` | `EventDeclarationSyntax` |
| `EventFieldDeclaration` | `EventFieldDeclarationSyntax` |
| `IndexerDeclaration` | `IndexerDeclarationSyntax` |
| `ConstructorDeclaration` | `ConstructorDeclarationSyntax` |
| `DestructorDeclaration` | `DestructorDeclarationSyntax` |
| `OperatorDeclaration` | `OperatorDeclarationSyntax` |
| `ConversionOperatorDeclaration` | `ConversionOperatorDeclarationSyntax` |
| `ClassDeclaration` | `ClassDeclarationSyntax` |
| `StructDeclaration` | `StructDeclarationSyntax` |
| `InterfaceDeclaration` | `InterfaceDeclarationSyntax` |
| `RecordDeclaration` | `RecordDeclarationSyntax` |
| `RecordStructDeclaration` | `RecordDeclarationSyntax` |
| `EnumDeclaration` | `EnumDeclarationSyntax` |
| `DelegateDeclaration` | `DelegateDeclarationSyntax` |
| `NamespaceDeclaration` | `NamespaceDeclarationSyntax` |
| `FileScopedNamespaceDeclaration` | `FileScopedNamespaceDeclarationSyntax` |
| `Block` | `BlockSyntax` |
| `IfStatement` | `IfStatementSyntax` |
| `WhileStatement` | `WhileStatementSyntax` |
| `ForStatement` | `ForStatementSyntax` |
| `ForEachStatement` | `ForEachStatementSyntax` |
| `ReturnStatement` | `ReturnStatementSyntax` |
| `ThrowStatement` | `ThrowStatementSyntax` |
| `SwitchStatement` | `SwitchStatementSyntax` |
| `TryStatement` | `TryStatementSyntax` |
| `UsingStatement` | `UsingStatementSyntax` |
| `LocalDeclarationStatement` | `LocalDeclarationStatementSyntax` |
| `ExpressionStatement` | `ExpressionStatementSyntax` |
| `InvocationExpression` | `InvocationExpressionSyntax` |
| `ObjectCreationExpression` | `ObjectCreationExpressionSyntax` |
| `SimpleMemberAccessExpression` | `MemberAccessExpressionSyntax` |
| `IdentifierName` | `IdentifierNameSyntax` |
| `GenericName` | `GenericNameSyntax` |
| `Argument` | `ArgumentSyntax` |
| `Parameter` | `ParameterSyntax` |
| `Attribute` | `AttributeSyntax` |
| `GetAccessorDeclaration` | `AccessorDeclarationSyntax` |
| `SetAccessorDeclaration` | `AccessorDeclarationSyntax` |
| `InitAccessorDeclaration` | `AccessorDeclarationSyntax` |
| `AddAccessorDeclaration` | `AccessorDeclarationSyntax` |
| `RemoveAccessorDeclaration` | `AccessorDeclarationSyntax` |
| `VariableDeclarator` | `VariableDeclaratorSyntax` |
| `EqualsValueClause` | `EqualsValueClauseSyntax` |
| `ArrowExpressionClause` | `ArrowExpressionClauseSyntax` |
| `SimpleLambdaExpression` | `SimpleLambdaExpressionSyntax` |
| `ParenthesizedLambdaExpression` | `ParenthesizedLambdaExpressionSyntax` |

---

## Edge Cases

### 1. Base Types (IMethodBase, IMember, etc.)

When pattern matching against a base type like `IMethodBase` (which includes both `IMethod` and `IConstructor`), you may need multiple `DeclarationKind` values:

```csharp
// Before
if (declaration is IMethodBase methodBase)
{
    DoSomething(methodBase);
}

// After - need to check multiple kinds
if (declaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Finalizer or DeclarationKind.Operator
    && declaration is IMethodBase methodBase)
{
    DoSomething(methodBase);
}
```

### 2. IType Hierarchy

`IType` includes multiple `DeclarationKind` values:
- `NamedType` → `INamedType`
- `Type` → `IType` (for non-named types)
- `TypeParameter` → also an `IType`

### 3. Null Checks

Always preserve null checks:

```csharp
// Before
if (symbol is IMethodSymbol method)

// After - symbol could be null
if (symbol?.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
// Or if null is not expected:
if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
```

### 4. Negation Patterns

```csharp
// Before
if (declaration is not IMethod)

// After
if (declaration.DeclarationKind != DeclarationKind.Method)
```

### 5. Or Patterns

```csharp
// Before
if (node is MethodDeclarationSyntax or PropertyDeclarationSyntax)

// After
if (node.Kind() is SyntaxKind.MethodDeclaration or SyntaxKind.PropertyDeclaration)
```

### 6. Record Types with Multiple SyntaxKinds

`RecordDeclarationSyntax` can be either `SyntaxKind.RecordDeclaration` or `SyntaxKind.RecordStructDeclaration`:

```csharp
// Before
if (node is RecordDeclarationSyntax record)

// After
if (node.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
    && node is RecordDeclarationSyntax record)
```

---

## What NOT to Optimize

1. **Patterns already using Kind checks** - Don't double-check
2. **Generic type parameters** - `T` cannot use Kind checks
3. **Cases where the type is definitely known** - e.g., after a prior Kind check
4. **Test files with intentional pattern matching** - Some tests specifically test pattern matching behavior
5. **Method parameters with specific types** - e.g., `this IMethod method` extension methods don't need optimization
6. **Patterns checking `object` or other non-declaration types** - e.g., `constant.RawValue is IField`

---

## Search Patterns for Finding Optimization Opportunities

These regular expressions can be used to find patterns that may need optimization in the codebase.

### IDeclaration Patterns

#### 1. If Statement Patterns (positive)

```regex
is I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter)\b
```

**Usage with Grep tool:**
```bash
Grep pattern="is I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter)\b" glob="**/*.cs" path="Metalama.Framework.Engine"
```

#### 2. If Statement Patterns (negation)

```regex
is not I(Method|Property|Event|Field|Indexer|Constructor|NamedType)\b
```

#### 3. Switch Statement Case Patterns

```regex
case I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\b
```

#### 4. Switch Expression Arm Patterns (with variable)

```regex
I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\s+\w+\s*=>
```

#### 5. Switch Expression Arm Patterns (with property pattern)

```regex
I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\s*\{[^}]*\}\s*=>
```

#### 6. Combined Switch Expression Pattern

Matches any switch expression arm with IDeclaration subtypes:
```regex
I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter)\b[^,;]*=>
```

### Extended Patterns (Base Types)

For base types like `IMember`, `IMethodBase`, `IMemberOrNamedType`:
```regex
is I(Method|Property|Event|Field|Indexer|Constructor|NamedType|Parameter|TypeParameter|Member|MethodBase|MemberOrNamedType|Attribute|Namespace|Compilation)\b
```

### ISymbol Patterns

```regex
is I(Method|Property|Event|Field|Named|Type|Parameter|Namespace|Local|Discard|Assembly|Module|Alias)Symbol\b
```

### SyntaxNode Patterns

```regex
is (Method|Property|Field|Event|Constructor|Destructor|Operator|Class|Struct|Interface|Record|Enum|Delegate|Namespace|Block|If|While|For|Return|Switch|Try|Using).*Syntax\b
```

### Pattern to Find Already-Optimized Code

Use this to exclude files that have already been optimized:
```regex
DeclarationKind\.\w+\s*(when|&&).*is I(Method|Property|Event|Field|Indexer|Constructor|NamedType)
```

---

## Filtering Results

After running a search, filter out false positives:

### False Positives to Ignore

1. **Method parameter declarations** - `this IMethod method` or `IMethod method`
   - These are type annotations, not pattern matching

2. **Already optimized patterns** - Look for `DeclarationKind.X when ... is IY`
   - These are already using the optimized pattern

3. **Non-IDeclaration sources** - Check what the variable being matched is:
   - `object` → cannot use DeclarationKind
   - `IType` → different interface hierarchy (use `TypeKind` instead)
   - Generic `T` → cannot use DeclarationKind

4. **Tuple patterns checking two declarations** - e.g., `(x, y) switch { (IMethod a, IMethod b) => ... }`
   - More complex to optimize, may not be worth it

---

## Batch Processing Workflow

1. **Search for patterns in a directory:**
   ```bash
   Grep pattern="is I(Method|Property|Event|Field|Indexer|Constructor)\b" path="Metalama.Framework.Engine" glob="**/*.cs"
   ```

2. **Read each file and identify:**
   - Is the source variable `IDeclaration` or a subtype?
   - Is this a pattern match or a method parameter?
   - Is the pattern already optimized?

3. **Apply transformations:**
   - Switch statements: Change to `switch (x.DeclarationKind)`
   - If statements: Add `x.DeclarationKind == DeclarationKind.Y &&` before the `is` check
   - Switch expressions: Add Kind check as first element of pattern

4. **Build and test after each batch**

5. **Commit successful batches**

---

## Files Processed in Issue #1307

### Optimized Files (22 files)

| File | Patterns Optimized |
|------|-------------------|
| DocumentationIdHelper.GeneratorOfDeclarationIdFromDeclaration.cs | Multiple switch cases |
| DeclarationExtensions.cs (Engine Helpers) | GetExplicitInterfaceImplementation, TryGetHiddenDeclaration |
| AdviceSyntaxGenerator.cs | Switch on declaration type |
| LinkerInjectionStep.cs | Member type checks |
| ModifierHelper.cs | Declaration type checks |
| AdviceFactory.cs | 3 switch statements |
| SerializableDeclarationIdProvider.FromDeclaration.cs | Switch with property patterns |
| OverrideHelper.cs | Declaration type checks |
| LinkerInjectionStep.LinkerInjectedMemberComparer.cs | IMethod check |
| LexicalScopeFactory.cs | IMethod check |
| DocumentationIdHelper.GeneratorOfReferenceIdFromDeclaration.cs | IMethod check |
| TemplateExpansionContext.cs | 2 switch statements |
| ExecuteAspectLayerPipelineStep.cs | IMethod comparison |
| ReferenceIndexerRequirements.cs | MethodKind-based logic |
| MetalamaMethodBaseSerializer.cs | IConstructor check |
| DocumentationIdHelper.Parser.cs | 7 patterns (IMethod, INamedType) |
| TemplateMemberGenericContext.cs | IMethod check |
| ContractIndexerTransformation.cs | 3 patterns (IIndexer, IParameter) |
| AddAttributeAdvice.cs | IConstructor check |
| EligibilityHelper.LocalFunctionEligibilityRule.cs | 2 patterns (IMethod, IParameter) |
| TemplateSyntaxFactoryImpl.cs | IMethod check |
| FieldOrPropertyInvoker.cs | 2 IProperty checks |

### Skipped Files (no optimization needed)

| File | Reason |
|------|--------|
| RefExtensions.cs | Uses method parameters, not pattern matching |
| AsyncHelper.cs | Uses method parameters, not pattern matching |
| IteratorHelper.cs | Uses method parameters, not pattern matching |
| TypedConstantExtensions.cs | Checks `object`, not `IDeclaration` |
| CodeModelExtensions.cs | Uses method parameters, not pattern matching |
| AccessorHelper.cs | Uses method parameters, not pattern matching |
| ConstructorExtensions.cs | Uses method parameters, not pattern matching |
| ContextualSyntaxGenerator.cs | Patterns check `IType`, not `IDeclaration` |
| Advising/DeclarationExtensions.cs | Uses method parameters in tuple pattern |

---

## Pitfalls and Common Mistakes

This section documents common mistakes that were discovered during the Kind optimization work. These pitfalls can cause subtle bugs that are difficult to diagnose.

### Pitfall 1: Multiple DeclarationKinds for the Same Interface

**Problem:** Some interfaces can have multiple `DeclarationKind` values. Checking only one Kind excludes valid cases.

**Example Bug (Query.cs):**
```csharp
// WRONG - CompilationModel has DeclarationKind.Compilation, not AssemblyReference
if (declaration.DeclarationKind == DeclarationKind.AssemblyReference && declaration is IAssembly assembly)

// CORRECT - IAssembly includes both Compilation and AssemblyReference
if (declaration.DeclarationKind is DeclarationKind.Compilation or DeclarationKind.AssemblyReference && declaration is IAssembly assembly)
```

**Common multi-Kind interfaces:**
| Interface | Possible DeclarationKinds |
|-----------|--------------------------|
| `IAssembly` | `Compilation`, `AssemblyReference` |
| `INamedType` | `NamedType`, `ExtensionBlock` |
| `IMethodBase` | `Method`, `Constructor`, `Finalizer`, `Operator` |
| `IMember` | `Method`, `Property`, `Field`, `Event`, `Indexer`, `Constructor` |
| `IType` | `NamedType`, `TypeParameter`, `Type` |

### Pitfall 2: ExtensionBlock is Not NamedType

**Problem:** `IExtensionBlock` is processed as `INamedType` but has `DeclarationKind.ExtensionBlock`, not `NamedType`.

**Example Bug (DesignTimeSyntaxTreeGenerator.cs):**
```csharp
// WRONG - Extension blocks won't match this
case DeclarationKind.NamedType when target is INamedType namedType:
    ProcessTransformationsOnType(namedType, transformations);

// CORRECT - Include ExtensionBlock
case DeclarationKind.NamedType or DeclarationKind.ExtensionBlock when target is INamedType namedType:
    ProcessTransformationsOnType(namedType, transformations);
```

### Pitfall 3: Switch Already Using Kind as Discriminator

**Problem:** If a switch already uses `Kind()` or `SyntaxKind`, adding pattern matching for extraction doesn't need Kind optimization.

**Example - Do NOT optimize:**
```csharp
// This switch ALREADY uses expression.Kind() - don't add redundant Kind checks
switch (expression.Kind())
{
    case SyntaxKind.CharacterLiteralExpression:
    case SyntaxKind.StringLiteralExpression:
    case SyntaxKind.NumericLiteralExpression:
        var literal = (LiteralExpressionSyntax)expression;  // Just cast
        return literal.Token.Value;
}
```

### Pitfall 4: Fall-Through Cases with Pattern Variables

**Problem:** When multiple switch cases fall through to shared code, a pattern variable defined in only one case is undefined for others.

**Example Bug:**
```csharp
// WRONG - literal is only defined for DefaultLiteralExpression
case SyntaxKind.CharacterLiteralExpression:
case SyntaxKind.StringLiteralExpression:
case SyntaxKind.DefaultLiteralExpression when expression is LiteralExpressionSyntax literal:
    var value = literal.Token.Value;  // literal undefined for other cases!

// CORRECT - Cast inside the case body
case SyntaxKind.CharacterLiteralExpression:
case SyntaxKind.StringLiteralExpression:
case SyntaxKind.DefaultLiteralExpression:
    var literal = (LiteralExpressionSyntax)expression;
    var value = literal.Token.Value;
```

### Pitfall 5: Missing Using Directives

**Problem:** Adding `SyntaxKind` checks requires `using Microsoft.CodeAnalysis.CSharp;`

**Symptoms:**
- `CS0103: The name 'SyntaxKind' does not exist in the current context`
- `.Kind()` method not found on `SyntaxNode`

**Fix:** Add `using Microsoft.CodeAnalysis.CSharp;` to the file's using directives.

### Pitfall 6: Unassigned Variable After Pattern Match

**Problem:** When a `when` clause pattern defines a variable but the switch/if doesn't guarantee that branch, the variable may be unassigned.

**Example Bug:**
```csharp
// WRONG - typeSymbol only assigned if Kind == TypeParameter
case SymbolKind.NamedType:
case SymbolKind.ArrayType:
case SymbolKind.TypeParameter when symbol is ITypeSymbol typeSymbol:
    return this.TypeSyntax(typeSymbol);  // typeSymbol undefined for NamedType/ArrayType!

// CORRECT - Cast directly
case SymbolKind.NamedType:
case SymbolKind.ArrayType:
case SymbolKind.TypeParameter:
    return this.TypeSyntax((ITypeSymbol)symbol);
```

### Pitfall 7: RS1034 Analyzer Warning

**Problem:** Roslyn analyzer RS1034 prefers `.IsKind()` over `.Kind() ==` for SyntaxKind checks.

**Example:**
```csharp
// Triggers RS1034 warning
if (node.Kind() == SyntaxKind.MethodDeclaration)

// Preferred - no warning
if (node.IsKind(SyntaxKind.MethodDeclaration))
```

### Pitfall 8: Null Reference After Null-Conditional

**Problem:** After `symbol?.Kind == ...`, accessing `symbol.X` without null check can throw.

**Example Bug:**
```csharp
// WRONG - symbol could be null
if (parameterTypeX?.ContainingSymbol?.Kind == SymbolKind.Method
    && parameterTypeX.ContainingSymbol is IMethodSymbol)  // NullRef if parameterTypeX is null!

// CORRECT - Keep null-conditional or check separately
if (parameterTypeX?.ContainingSymbol?.Kind == SymbolKind.Method
    && parameterTypeX?.ContainingSymbol is IMethodSymbol)
```

---

## Testing Strategy

After applying Kind optimizations to a batch of files:

1. **Build first:** `dotnet build Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf`
2. **Run tests:** `dotnet test Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf`
3. **If tests fail:**
   - Check for multi-Kind interfaces (Pitfall 1, 2)
   - Check for pattern variable scope issues (Pitfall 4, 6)
   - Check for missing using directives (Pitfall 5)
4. **Commit after each successful batch** - Don't batch too many changes together

