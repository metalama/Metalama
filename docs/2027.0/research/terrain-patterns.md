# Terrain map: `Metalama.Patterns` under C# 15 and .NET 11

Subsystem: `C:/src/Metalama-2027.0/Metalama/Metalama.Patterns/src/**`.
Branch: `topic/2027.0/26-09-03-net11-impact`, baseline PB-2027.0.
All paths below are relative to `C:/src/Metalama-2027.0/Metalama/Metalama.Patterns/` unless written in full.

---

## 0. Executive summary

`Metalama.Patterns` contains ten shipped packages. Only **one** of them reads C# syntax:
`Metalama.Patterns.Observability`, whose dependency analyser is a `CSharpSyntaxWalker` over property getter
bodies. That single walker, plus its two helper files, is where a new expression form, a new
collection-expression element or a new statement field lands.

Everything else in the subsystem reads the Metalama *code model*, not syntax. Those files are sensitive to
the sets `DeclarationKind`, `TypeKind`, `SpecialType`, `MethodKind`, `RefKind` and `Writeability`, which grow
when the language grows, and they are the places a union declaration or an extension indexer reaches.

The subsystem ships no per-Roslyn-version variant. Two projects
(`Metalama.Patterns.Observability`, `Metalama.Patterns.Immutability`) reference `Metalama.Framework.Sdk`
privately and therefore compile against a single Roslyn, whichever `Directory.Packages.props` resolves.
There is no `#if ROSLYN_*` anywhere in the subsystem.

The most dangerous single line in the subsystem is
`Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:31-36`,
which hard-casts every declaring syntax reference of a property to `PropertyDeclarationSyntax` and then calls
`SingleOrDefault()`.

---

## 1. Shipped target frameworks

`Metalama.Patterns/Directory.Build.props:26` sets `<LangVersion>$(LangMaxVersion)</LangVersion>`.
`LangMaxVersion` is defined once, in `Metalama.Framework/Directory.Build.props:45`, currently `14.0`.
Raising it to `15.0` changes the language version of **every** project in this subsystem at once, including
all the aspect-test target projects. That is the switch that makes C# 15 syntax reach these aspects.

The repository root `Directory.Build.props` also pins `MetalamaTemplateLanguageVersion` to `14.0`, which
bounds what the `[Template]` methods in this subsystem may be written in
(`Metalama.Patterns.Observability/Implementation/ClassicStrategy/Templates.cs`,
`Metalama.Patterns.Caching.Aspects/CacheAttribute.cs`,
`Metalama.Patterns.Wpf/Implementation/DependencyPropertyAspectBuilder.Templates.cs`,
`Metalama.Patterns.Memoization/MemoizeAttribute.cs`).

| Project | `TargetFrameworks` | csproj line |
| --- | --- | --- |
| `Flashtrace` | `net472;net10.0;netstandard2.0` | `src/Flashtrace/Flashtrace.csproj:4` |
| `Flashtrace.Formatters` | `net472;net10.0;netstandard2.0` | `src/Flashtrace.Formatters/Flashtrace.Formatters.csproj:4` |
| `Metalama.Patterns.Caching` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Caching/Metalama.Patterns.Caching.csproj:4` |
| `Metalama.Patterns.Caching.Aspects` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Caching.Aspects/Metalama.Patterns.Caching.Aspects.csproj:4` |
| `Metalama.Patterns.Caching.Backend` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Caching.Backend/Metalama.Patterns.Caching.Backend.csproj:4` |
| `Metalama.Patterns.Contracts` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Contracts/Metalama.Patterns.Contracts.csproj:4` |
| `Metalama.Patterns.Immutability` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Immutability/Metalama.Patterns.Immutability.csproj:4` |
| `Metalama.Patterns.Memoization` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Memoization/Metalama.Patterns.Memoization.csproj:4` |
| `Metalama.Patterns.Observability` | `net472;net10.0;netstandard2.0` | `src/Metalama.Patterns.Observability/Metalama.Patterns.Observability.csproj:4` |
| **`Metalama.Patterns.Wpf`** | **`net472;net10.0-windows`** (no `netstandard2.0`) | `src/Metalama.Patterns.Wpf/Metalama.Patterns.Wpf.csproj:4` |

Test projects: `net472;net10.0` for the unit tests; `net10.0` for the aspect tests;
`net10.0-windows` for `Metalama.Patterns.Wpf.AspectTests`, `.CompileTimeTests`, and one leg of
`.UnitTests` (`src/tests/Metalama.Patterns.Wpf.UnitTests/Metalama.Patterns.Wpf.UnitTests.csproj:4`).

`Metalama.Patterns.Wpf` is the package that platform-support.md calls out as the visible breaking change:
its `net8.0-windows` asset became `net10.0-windows`, so a WPF application on .NET 8 or .NET 9 finds no
compatible asset. If `net11.0` is added anywhere in this subsystem, `Metalama.Patterns.Wpf` is the project
where a `net11.0-windows` leg would have to be decided (or explicitly declined, relying on roll-forward).

Stale `obj/*.nuget.g.props` files under each project still name `net8.0`; they are restore artefacts, not
sources, and must not be read as configuration.

---

## 2. Sensitivity to the set of C# language constructs

### 2.1 The one syntax walker: `Metalama.Patterns.Observability`

`Metalama.Patterns.Observability` is the only package in the subsystem that touches
`Microsoft.CodeAnalysis.CSharp.Syntax`. The complete list of files that `using` it:

- `Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:13-16`
- `Implementation/DependencyAnalysis/RoslynExtensions.cs:11-12`
- `Implementation/RoslynHelper.cs:7-8`

plus these, which use `Microsoft.CodeAnalysis` symbols only (no syntax):
`Implementation/ClassicStrategy/ClassicGraphBuildingContext.cs:11`,
`Implementation/DependencyAnalysis/DependencyGraphBuilder.GatherIdentifiersContext.cs:10`,
`Implementation/DependencyAnalysis/DependencyPathElement.cs:6`,
`Implementation/DependencyAnalysis/GraphBuildingContext.cs:11-12`,
`Implementation/DiagnosticDescriptors.cs:9`.

#### 2.1.1 `DependencyGraphBuilder.Visitor.cs` — the walker

`internal sealed partial class Visitor : CSharpSyntaxWalker` (line 53), constructed with the default
`SyntaxWalkerDepth.Node`.

Entry point, `AddReferencedProperties`, lines 22-50:

```csharp
var body = symbol
    .DeclaringSyntaxReferences
    .Select( r => r.GetSyntax( cancellationToken ) )
    .Cast<PropertyDeclarationSyntax>()      // line 34
    .Select( RoslynExtensions.GetGetterBody )
    .SingleOrDefault();                     // line 36
```

Overrides, all in this file:

| Line | Override | What it does |
| --- | --- | --- |
| 280 | `Visit( SyntaxNode? )` | increments `_depth`, calls `base.Visit`, then `ProcessAndResetIfApplicable` |
| 294 | `VisitInvocationExpression` | **commented out** (lines 292-302) |
| 305 | `VisitArgument( ArgumentSyntax )` | isolates each argument in a new root gather context |
| 313 | `VisitVariableDeclaration` | reports `LAMA5165` for non-immutable local variables |
| 331 | `VisitLocalFunctionStatement` | deliberately empty; local function bodies are not analysed |
| 338 | `VisitConditionalExpression` | forks the gather context for `WhenTrue` / `WhenFalse` |
| 361 | `VisitBinaryExpression` | line 363 `node.IsKind( SyntaxKind.CoalesceExpression )` forks; every other binary operator gets one new root context per operand |
| 395 | `VisitMemberAccessExpression` | `EnsureStarted` |
| 402 | `VisitConditionalAccessExpression` | `EnsureStarted` |
| 409 | `VisitIdentifierName` | resolves the symbol and appends it to the current chain |

Symbol-shape switches in the same file:

- lines 112-117: `pathElement.Symbol switch { IPropertySymbol …, IFieldSymbol …, _ => chainSection == ChainSection.Unsupported ? null : throw new ObservabilityAssertionFailedException() }`
- line 144: `switch ( pathElement.Symbol.Kind )` with cases `SymbolKind.Field` (146), `SymbolKind.Property when …` (163), `SymbolKind.Method` (183). **No default case**: any other symbol kind is silently accepted.
- line 197: `p.RefKind is RefKind.Out`
- line 200: `p.RefKind is RefKind.Out or RefKind.Ref`
- line 227: `symbols[^1].Node.GetAccessKind() is not (AccessKind.Read or AccessKind.ReadWrite)` skips write-only chains.
- lines 237-241: `sr.Symbol.Kind == SymbolKind.Property || (sr.Symbol.Kind == SymbolKind.Field && …)` decides how much of a member chain is "supported".

#### 2.1.2 `Implementation/RoslynHelper.cs` — `GetAccessKind`

The read/write classifier, lines 24-76. Two switches:

- lines 40-50, on `parent.Kind()`: `PostIncrementExpression`, `PostDecrementExpression`,
  `PreIncrementExpression`, `PreDecrementExpression` → `ReadWrite`; `MemberBindingExpression` → recurse.
- lines 52-70, on the parent node type: `AssignmentExpressionSyntax`, `MemberAccessExpressionSyntax`,
  `ConditionalAccessExpressionSyntax`, `ParenthesizedExpressionSyntax` (line 69, the only "transparent
  wrapper" the method knows).
- line 75: `return AccessKind.Read;` for everything else, with an explicit comment saying the result is
  deliberately inaccurate outside the known cases.

#### 2.1.3 `Implementation/DependencyAnalysis/RoslynExtensions.cs`

- `GetEffectiveAccessibility`, lines 19-54. Line 27 `switch ( symbol.Kind )` over
  `Property`, `Method`, `Field`, `Event`, `NamedType`; inner `t.TypeKind switch` at lines 41-46 covers
  `Class`, `Struct`, `Interface`, `Enum` and ends `_ => throw new NotSupportedException()` (line 45).
  The outer switch ends `default: throw new NotSupportedException();` (line 53).
- `GetElementaryType`, lines 84-107: unwraps `System_Nullable_T`, `IArrayTypeSymbol`, `IPointerTypeSymbol`.
- `GetGetterBody( PropertyDeclarationSyntax )`, lines 117-135: prefers `ExpressionBody`, else scans
  `AccessorList.Accessors` for `accessor.Keyword.IsKind( SyntaxKind.GetKeyword )` (line 130).

#### 2.1.4 `Implementation/DependencyAnalysis/GraphBuildingContext.cs`

- lines 37-45: `decl switch { ICompilation, INamespace, INamedType, IMember, _ => DependencyAnalysisOptions.Default }`.
- line 66: `symbol.Kind is SymbolKind.Property or SymbolKind.Field or SymbolKind.Method`.
- line 82: `fieldOrPropertyType.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.TypeParameter`
  as the definition of "deeply immutable" for non-named types.

#### 2.1.5 `Implementation/InpcInstrumentationKindLookup.cs`

`GetCore`, lines 26-84: `switch ( type )` over `INamedType` (28) and `ITypeParameter` (63), with
`default: return InpcInstrumentationKind.None;` at lines 82-83.

### 2.2 `DeclarationKind` switches (code model, not syntax)

| File | Lines | Shape |
| --- | --- | --- |
| `src/Metalama.Patterns.Contracts/ContractContext.cs` | 62-73 | `TargetDisplayName`: `Parameter` (return / named), `Property`, `Field`, `Indexer`, `_ => throw new ArgumentOutOfRangeException` |
| `src/Metalama.Patterns.Contracts/ContractContext.cs` | 78-86 | `TargetParameterName`: `Parameter`, `Property or Field or Indexer`, `_ => throw` |
| `src/Metalama.Patterns.Contracts/ContractExtensions.cs` | 165-172 | `GetContractOptions( IDeclaration )`: `IParameter`, `IFieldOrPropertyOrIndexer`, `INamedType`, `IMethod`, `_ => throw new ArgumentOutOfRangeException()` |
| `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs` | 606, 625 | `fieldOrProperty.DeclarationKind == DeclarationKind.Property` |
| `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/Templates.cs` | 108, 304 | field-or-property discrimination inside a template |
| `src/Metalama.Patterns.Wpf/CommandAttribute.DiagnosticReporter.cs` | 19-21 | `DeclarationKind == Property ? "<property message>" : "<method message>"` (binary, no third branch) |
| `src/Metalama.Patterns.Wpf/Implementation/NamingConvention/DiagnosticReporter.cs` | 40, 57, 69 | `DeclarationKind` passed as a diagnostic argument |
| `src/Metalama.Patterns.Wpf/Implementation/Diagnostics.cs` | 45, 62, 81 | `DeclarationKind` in diagnostic definitions |
| `src/Metalama.Patterns.Immutability/ImmutabilityDiagnostics.cs` | 26 | `DeclarationKind` in `FieldOrPropertyMustBeOfDeeplyImmutableType` |
| `src/Metalama.Patterns.Observability/Implementation/DiagnosticDescriptors.cs` | 49, 61, 72 | `DeclarationKind` in `LAMA5152`, `LAMA5154`, `LAMA5155` |

### 2.3 `TypeKind` switches

| File | Lines | Shape |
| --- | --- | --- |
| `src/Metalama.Patterns.Immutability/ImmutabilityExtensions.cs` | 40-55 | `{ TypeKind: Delegate or Enum or Pointer or FunctionPointer }` ⇒ `ImmutabilityKind.Deep`. Everything else falls through to `IsReadOnly` (line 90) and then `ImmutabilityKind.None` (line 93). |
| `src/Metalama.Patterns.Observability/ObservableAttribute.cs` | 52 | `builder.ExceptForInheritance().MustSatisfy( x => x.TypeKind is TypeKind.Class, x => $"{x} must be a class or a record class" )` |
| `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs` | 41-46 | `Class or Struct` ⇒ `Private`; `Interface or Enum` ⇒ `Public`; `_ => throw` |
| `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/GraphBuildingContext.cs` | 82 | `Pointer or FunctionPointer or TypeParameter` |
| `src/Metalama.Patterns.Contracts/CompileTimeHelpers.cs` | 33 | `type.TypeKind == TypeKind.Interface` |
| `src/Metalama.Patterns.Contracts/CompileTimeHelpers.cs` | 73 | `TypeKind != TypeKind.TypeParameter` |
| `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs` | 105, 126, 129, 163, 175, 187, 203 | `p[n].Type.TypeKind == TypeKind.TypeParameter` (seven occurrences) |

### 2.4 `SpecialType` switches

These enumerate the built-in types. They grow only if the language adds a primitive, which C# 15 does not,
but they are the same shape of hazard.

- `src/Metalama.Patterns.Contracts/Numeric/NumericBound.cs:140-300` (`switch ( valueType.SpecialType )`,
  `Object`, `SByte`, `Int16`, `Int32`, `Int64`, `Byte`, `UInt16`, `UInt32`, `UInt64`, `Decimal`, `Single`,
  `Double`) and `:312-440` (a second switch of the same shape).
- `src/Metalama.Patterns.Contracts/Numeric/NumericRange.cs:513-528`, `IsTypeSupported`.
- `src/Metalama.Patterns.Contracts/RangeAttribute.cs:170-183`.
- `src/Metalama.Patterns.Contracts/EnumDataTypeAttribute.cs:63-74` and `:85`.
- `src/Metalama.Patterns.Contracts/NotEmptyAttribute.cs:56`, `:92`.
- `src/Metalama.Patterns.Contracts/RequiredAttribute.cs:75`.
- `src/Metalama.Patterns.Contracts/InvariantAttribute.cs:32` (`MustEqual( SpecialType.Void )`).
- `src/Metalama.Patterns.Caching.Aspects/CacheAttribute.cs:46`, `:66-67`, `:177-193`
  (`Task_T`, `ValueTask_T`, `IAsyncEnumerable_T`, `IAsyncEnumerator_T`, with `_ => null` at line 193).
- `src/Metalama.Patterns.Caching.Aspects/Helpers/CompileTimeHelpers.cs:63`, `:78`, `:116-117`, `:142`.
- `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:875`, `:914`, `:926` (`ReturnType.SpecialType: SpecialType.Void`, `Type.SpecialType: SpecialType.String`).
- `src/Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs:30`.
- `src/Metalama.Patterns.Wpf/CommandAttribute.cs:128`.
- `src/Metalama.Patterns.Wpf/Implementation/CommandNamingConvention/CommandNamingConventionMatcher.cs:127`, `:134`.
- `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs:79`, `:93`, `:98`, `:110`, `:114`, `:120`, `:152`, `:160`, `:172`, `:184`, `:196`, `:200`.

### 2.5 `MethodKind`, `RefKind`, `Writeability`

- `src/Metalama.Patterns.Memoization/MemoizeAttribute.cs:41`:
  `builder.MustSatisfy( m => m.MethodKind == MethodKind.Default, m => $"{m} must be a normal method" )`.
  This is the only `MethodKind` test in the subsystem, and it is a whitelist of exactly one value.
- `RefKind` whitelists: `src/Metalama.Patterns.Contracts/ContractExtensions.cs:70`
  (`RefKind: RefKind.None`), `src/Metalama.Patterns.Wpf/Implementation/CommandNamingConvention/CommandNamingConventionMatcher.cs:128`
  (`[{ RefKind: RefKind.None or RefKind.In }]`),
  `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs:79`
  and `:152` (`parameter.RefKind is not (RefKind.None or RefKind.In)`),
  `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:197`, `:200`.
- `Writeability`: `src/Metalama.Patterns.Immutability/ImmutableAttribute.cs:61` (`> Writeability.InitOnly`)
  and `:76-90` (`switch ( property.Writeability )` with `All`, `None`, `default`);
  `src/Metalama.Patterns.Memoization/MemoizeAttribute.cs:49`;
  `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:567`, `:583`;
  `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/Templates.cs:58`;
  `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyAspectBuilder.cs:43-45`, `:199`;
  `src/Metalama.Patterns.Contracts/ContractExtensions.cs:100`.

### 2.6 Member enumeration (where a new member kind would be missed)

Every one of these enumerates a fixed list of member collections. A new member kind that the code model
exposes through a new collection is invisible to all of them.

| File | Lines | Collections read |
| --- | --- | --- |
| `src/Metalama.Patterns.Contracts/ContractExtensions.cs` | 93-108 | `t.Properties`, `t.Fields`, `t.Indexers`; then `t.Methods`, `t.Constructors`, `t.Indexers`. **No `t.Events`.** |
| `src/Metalama.Patterns.Contracts/CheckInvariantsAspect.cs` | 27-33 | `Properties` (setters), `Indexers` (setters), `Methods` |
| `src/Metalama.Patterns.Immutability/ImmutableAttribute.cs` | 57, 72 | `Fields`, `Properties`. **No `Indexers`, no `Events`.** |
| `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.cs` | 60 | `type.Properties` only |
| `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs` | 566-575 | `target.Properties` ∪ `target.Fields` |
| `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs` | 822 | `CurrentType.AllMembers()` ∪ `CurrentType.Types` for name reservation |
| `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyAspectBuilder.cs` | 237-239 | same pattern for name reservation |
| `src/Metalama.Patterns.Wpf/Implementation/CommandNamingConvention/CommandNamingConventionMatcher.cs` | 27-28, 40, 52 | `declaringType.AllMembers()`, `declaringType.Types`, `.Methods`, `.Properties` |
| `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs` | 30-31, 44, 50 | same |
| `src/Metalama.Patterns.Caching.Aspects/InvalidateCacheAttribute.cs` | 337 | `invalidatedMethodsDeclaringType.AllMethods` |
| `src/Metalama.Patterns.Caching.Aspects/CacheAttribute.cs` | 108, 118 | `DeclaringType.Fields.OfName(...)` for name uniqueness |

### 2.7 Code that emits C# syntax as text

These build source text with `ExpressionBuilder` / `StatementBuilder`. They are the places where a new
contextual keyword could collide, and where new operator or pattern forms would eventually be adopted.

- `src/Metalama.Patterns.Contracts/Numeric/NumericRange.cs:306-465`, `GeneratePattern`, which emits a
  relational pattern: `builder.AppendVerbatim( " is " )` at line 456 and `" or "` at line 460, and the
  operators `" < "`, `" <= "`, `" > "`, `" >= "` at lines 429, 433, 443, 447. This is the only place in the
  subsystem that generates C# *pattern* syntax.
- `src/Metalama.Patterns.Contracts/Numeric/NumericBound.cs:120-133`, `:310`.
- `src/Metalama.Patterns.Contracts/CompileTimeHelpers.cs:18-23` (`typeof(...)`).
- `src/Metalama.Patterns.Contracts/{Email,Phone,Url,RegularExpression}Attribute.cs` (member-access text).
- `src/Metalama.Patterns.Memoization/MemoizeAttribute.cs:103-111` and `:126-134`, which emit
  `Interlocked.CompareExchange( ref <field>, <value>, null );` as raw text, including the `ref` keyword.
- `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:516-527`:
  `accessChildExprBuilder.AppendVerbatim( node.DottedPropertyPath.Replace( ".", "?." ) )` — a dotted property
  path turned into a null-conditional chain by string replacement.
- `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/Templates.cs:392`, `:645`, `:818`:
  `new SwitchStatementBuilder(...)`, which emits `switch` statements over property-name strings and tuples.
- `src/Flashtrace.Formatters/Implementations/TypeFormatter.cs:14-31`: a `Dictionary<Type, string>` of the
  fifteen C# keyword type names, used by `WriteCore` (lines 47-140) to print C#-shaped type names, including
  `?` for `Nullable<>` (line 76), `[,]` for arrays (lines 78-89) and `<...>` for generics (lines 92-134).

---

## 3. Sensitivity to .NET runtime, .NET SDK, Roslyn and the host IDE

### 3.1 Roslyn coupling

Only two projects reference Roslyn, and only privately:

- `src/Metalama.Patterns.Observability/Metalama.Patterns.Observability.csproj:27`
  `<PackageReference Include="Metalama.Framework.Sdk" PrivateAssets="all" />`
- `src/Metalama.Patterns.Immutability/Metalama.Patterns.Immutability.csproj:27` — same line.

Both carry an identical comment at line 31 that begins
`Microsoft.CodeAnalysis.Features 4.12.0, reached through the private Metalama.Framework.Sdk reference` and
suppresses `https://github.com/advisories/GHSA-8g4q-xg66-9fp4` (`System.Text.Json` 8.0.4). The comment names
Roslyn **4.12**, a version that PB-2027.0 has dropped, and it explains the suppression in terms of the
`net8.0` shared framework pruning the package. Both statements are now stale. This is a documentation
hotspot, and the suppression itself should be re-derived against the Roslyn 5.x graph.

There is no `#if ROSLYN_*` and no per-Roslyn variant directory anywhere under `Metalama.Patterns`. The
subsystem ships one assembly per target framework, compiled against one Roslyn. Every symbol and syntax API
listed in section 2.1 must therefore exist in the *lowest* Roslyn a host presents, which under PB-2027.0 is
5.0 (Rider). Any C# 15 handling written against a Roslyn 5.10 API (for example a `TypeKind.Union` member, or
`BreakStatementSyntax.Name`) would fail to compile here unless it is reached reflectively or unless the
subsystem grows the variant mechanism it does not currently have.

### 3.2 Preprocessor symbols

Complete list under `src/**` excluding tests:

| File | Line | Symbol | Effect |
| --- | --- | --- | --- |
| `Flashtrace/Utilities/CharSpanExtensions.cs` | 15 | `NET6_0_OR_GREATER` | span helpers |
| `Flashtrace.Formatters/FormatterRepository.Builder.cs` | 62 | `NET6_0_OR_GREATER` | registers `ISpanFormattable` formatter |
| `Flashtrace.Formatters/Implementations/SpanFormattableFormatter.cs` | 5 | `NET6_0_OR_GREATER` | whole file |
| `Flashtrace.Formatters/UnsafeStringBuilder.cs` | 935 | `NET6_0_OR_GREATER` | |
| `Metalama.Patterns.Caching/CachingService.Builder.cs` | 36 | `NETCOREAPP3_0_OR_GREATER` | async-enumerable adapters |
| `Metalama.Patterns.Caching/ValueAdapters/AsyncEnumerableAdapter.cs` | 5 | `NETCOREAPP3_0_OR_GREATER` | whole file |
| `Metalama.Patterns.Caching/ValueAdapters/AsyncEnumeratorAdapter.cs` | 5 | `NETCOREAPP3_0_OR_GREATER` | whole file |
| `Metalama.Patterns.Caching.Aspects/CacheAttribute.cs` | 18 | `NET6_0_OR_GREATER` | `using Metalama.Framework.RunTime` |
| `Metalama.Patterns.Caching.Aspects/CacheAttribute.cs` | 338 | `NETCOREAPP3_0_OR_GREATER` | the `OverrideMethodAsyncEnumerable` / `…Enumerator` templates |
| `Metalama.Patterns.Caching.Aspects/Helpers/AsyncEnumerableHelper.cs` | 5 | `NETCOREAPP3_0_OR_GREATER` | whole file |
| `Metalama.Patterns.Caching.Backend/Backends/CachingBackend.cs` | 1146, 1209 | `NET6_0_OR_GREATER` | |
| `Metalama.Patterns.Caching.Backend/Implementation/CacheSynchronizer.cs` | 89 | `NET6_0_OR_GREATER` | |
| `Metalama.Patterns.Caching.Backend/Implementation/TaskExtensions.cs` | 47, 71 | `NET6_0_OR_GREATER` | |
| **`Metalama.Patterns.Contracts/Numeric/NumericRange.cs`** | **11, 382, 481** | **`NET8_0_OR_GREATER`** | the entire generic-math branch of `GeneratePattern` and `IsZeroBound` |
| `Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs` | 518, 522 | `NETCOREAPP` | `#pragma warning disable CA1307` only |
| `Metalama.Patterns.Wpf/Configuration/CommandNamingConvention.cs` | 17, 141, 161 | `NETFRAMEWORK` / `NETCOREAPP` | |
| `Metalama.Patterns.Wpf/Configuration/DependencyPropertyNamingConvention.cs` | 18, 139, 148 | `NETFRAMEWORK` / `NETCOREAPP` | |
| `Metalama.Patterns.Wpf/Implementation/FormattingExtensions.cs` | 13 | `NETFRAMEWORK` | |

`NumericRange.cs:382` is the one that matters. It guards *compile-time* behaviour: the generic-math contract
support added by issue #1543 exists only in the `net10.0` build of `Metalama.Patterns.Contracts`. Which build
of an aspect assembly the pipeline loads is decided by the referencing project's target framework, so the
same source can produce different generated code depending on the user's target framework, with no
diagnostic either way.

### 3.3 Host-IDE sensitivity

The subsystem branches on the execution scenario in three places. These are the design-time / compile-time
divergences, and each is a place where the design-time pipeline produces different code from the batch
pipeline:

- `src/Metalama.Patterns.Immutability/ImmutableAttribute.cs:97`
  `if ( this._kind == ImmutabilityKind.Deep && !MetalamaExecutionContext.Current.ExecutionScenario.IsDesignTime )`
- `src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyAspectBuilder.cs:112`
  `if ( !MetalamaExecutionContext.Current.ExecutionScenario.CapturesNonObservableTransformations )`
- `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicDesignTimeObservabilityStrategyImpl.cs`
  — a complete second implementation of the strategy for design time (lines 72, 76, 107, 111, 142, 146 mirror
  `ClassicObservabilityStrategyImpl.cs` lines 225, 229, 349, 353, 417, 421).
- `src/Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs:52-56`:
  `if ( this._targetType.Compilation.IsPartial && !this._targetType.Compilation.Types.Contains( type ) ) return InpcInstrumentationKind.Unknown;`
  — the partial-compilation path that only design time takes.

`Metalama.Patterns.Wpf` is additionally host-sensitive through WPF itself: `<UseWPF>true</UseWPF>` and
`<EnableWindowsTargeting>true</EnableWindowsTargeting>` (csproj lines 5 and 20), plus the `net472`-only
`<Reference Include="WindowsBase" />` at line 29.

---

## 4. How the C# 14 wave was absorbed here

Of the C# 14 issues listed (#1034, #1035, #1036, #1094, #1105, #1108–#1116, #1127, #1131, #1143, #1159,
#1160), **none produced a commit under `Metalama.Patterns/`**. `git log --grep` over those numbers restricted
to that directory returns nothing. The C# 14 work landed in `Metalama.Framework`, and this subsystem
absorbed it downstream, in three moves.

**Move 1 — test the aspects against the new construct, with `.t.cs` baselines.** Six aspect tests were added
to `Metalama.Patterns.Observability.AspectTests`, all named `FieldKeyword_*`, each with a committed expected
output. Commit `32c2984143` "Add [Observable] aspect tests for field-keyword properties".

```
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_BasicProperty.cs        + .t.cs
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_InpcProperty.cs         + .t.cs
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_SetterSideEffect.cs     + .t.cs
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_ValueTypes.cs           + .t.cs
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_WithComputedProperty.cs + .t.cs
src/tests/Metalama.Patterns.Observability.AspectTests/FieldKeyword_WithInitializer.cs      + .t.cs
```

Only `Metalama.Patterns.Observability` got such tests. Contracts, Caching, Memoization, Immutability and Wpf
got none, because the `field` keyword only changes the shape of a property getter body, which is exactly what
the Observability dependency analyser reads.

**Move 2 — fix the defect the tests exposed, then re-adopt the baselines.** The semi-automatic property whose
setter has a side effect was mis-handled: the aspect assigned the backing field directly instead of running
the original setter body. Issue #1644, three commits, in this order:

1. `16689158ff` "Add failing regression test for dropped semi-auto setter body (#1644)"
2. `dd2403521d` "Update Observability semi-auto snapshots; setter body now invoked (#1644)"
3. `07a7afffdb` "Make FieldKeyword_SetterSideEffect a snapshot-only test (#1644)"

The comment left in `FieldKeyword_SetterSideEffect.cs:15-18` records what the baseline is asserting:

> `The generated snapshot must route the public setter through 'Value_Source' so that 'SideEffect++' runs;`
> `if the bug regresses, the setter assigns the backing field directly and the side effect is dropped.`

**Move 3 — silence the analyser suggestions the new construct raises.** The repository root
`Directory.Build.props:5-9` disables `IDE0032` ("use auto-property", raised by the `field` keyword) and
`IDE0031` ("simplify null-conditional assignment") for the whole repository, with a comment saying
"Disabling new features while the SDK is not stable". A local `#pragma warning disable IDE0031` also sits at
`src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/Templates.cs:115` and `:120`, and
`ReSharper disable ConvertToAutoPropertyWithInitializer` headers were added to each `FieldKeyword_*` test
(commit `a7494c78c0` "Fix warnings in field-keyword tests").

**A fourth, mechanical move accompanies every Roslyn uptake.** When the Roslyn version moved to 5.10, the
Patterns baselines had to be re-adopted because Roslyn's trivia handling changed the formatted output:
commit `32e6150298` "Update the Patterns baselines for the Roslyn 5.10 trivia handling (#1881)". Expect the
same for C# 15: a Roslyn uptake alone, with no language change, moves `.t.cs` files in this subsystem.

**The pattern to repeat for C# 15**, therefore, is:

1. Raise `LangMaxVersion` in `Metalama.Framework/Directory.Build.props:45` (and, for templates,
   `MetalamaTemplateLanguageVersion` in the root `Directory.Build.props`, which is bounded by
   `RoslynApiMinVersion`, not by the SDK).
2. Add aspect tests under the package whose analysis the construct actually reaches, with committed
   `.t.cs` baselines, one file per construct, named `<Feature>_<Scenario>.cs`.
3. Run them, read the actual output, and only then adopt it.
4. Add the analyser suppressions the new construct raises, at the narrowest scope that works.
5. Re-adopt the baselines that the Roslyn uptake itself moves.

Test-suite sizes, for calibration of step 2:
Caching 34, Contracts 61, Immutability 5, Memoization 6, Observability 68, Wpf 59 test inputs.

---

## 5. Extension points for each new construct

### 5.1 A new kind of type declaration (`union`)

`UnionDeclarationSyntax` derives from `TypeDeclarationSyntax`
(`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954`), so a union is a type declaration with
attribute lists, modifiers, a type parameter list, a parameter list, a base list and members. The symbol's
`TypeKind` is the open question; every site below assumes the four kinds that exist today.

Must change:

1. `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs:41-46` —
   `GetEffectiveAccessibility` throws `NotSupportedException` for any `TypeKind` outside
   `{Class, Struct, Interface, Enum}`. A union containing a private field reached through a property chain
   would crash the aspect. This one fails loudly, which is the correct behaviour, but it fails.
2. `src/Metalama.Patterns.Observability/ObservableAttribute.cs:52` — the eligibility rule
   `x.TypeKind is TypeKind.Class` must decide whether `[Observable]` applies to a union, and the message
   `"must be a class or a record class"` must be extended either way.
3. `src/Metalama.Patterns.Immutability/ImmutabilityExtensions.cs:40-93` — a union is presumably immutable by
   construction, or at least classifiable. Today it falls to line 93, `ImmutabilityKind.None`, silently.
4. `src/Metalama.Patterns.Contracts/CompileTimeHelpers.cs:33` —
   `GetSelfAndAllImplementedInterfaces` yields the type itself only when `TypeKind == Interface`.
5. `src/Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs:26-84` — a union that
   implements `INotifyPropertyChanged` would be handled by the `INamedType` branch if the code model presents
   it as `INamedType`; otherwise it falls to `default: return None` at line 82.
6. `src/Metalama.Patterns.Contracts/ContractExtensions.cs:31-52` — `t.AllTypes` / `t.Types` must include
   unions for `VerifyNotNullableDeclarations` to reach their members.
7. The aspects that *introduce members into the target type* must decide whether a union can receive them:
   `Metalama.Patterns.Wpf/Implementation/DependencyPropertyAspectBuilder.cs:76-107` (two static fields),
   `Metalama.Patterns.Caching.Aspects/CacheAttribute.cs:108-136` (a registration field),
   `Metalama.Patterns.Memoization/MemoizeAttribute.cs:60-95` (a backing field),
   `Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:225-421`
   (several methods and fields).

### 5.2 A new modifier (`closed`)

`closed` adds no syntax node, so nothing in the walker sees it. It reaches the subsystem only through the
code model, and only if `Metalama.Framework` exposes it. The places that would have to consult it:

1. `src/Metalama.Patterns.Immutability/ImmutabilityExtensions.cs:88-91` — a closed hierarchy is exactly the
   condition under which deep immutability can be *proved* rather than assumed, because no unknown derived
   type can add a mutable field. This is the strongest opportunity the feature creates in this subsystem.
2. `src/Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs:47-61` — the
   `!namedType.BelongsToCurrentProject` and `Compilation.IsPartial` fallbacks exist because a type outside
   the current compilation might implement `INotifyPropertyChanged` through an unseen derived type. A closed
   hierarchy narrows that.
3. `src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:961-980`
   — `ErrorVirtualMemberIsNotSupported` (`LAMA5154`) and `ErrorNewMemberIsNotSupported` (`LAMA5155`) exist
   because a derived type may override or shadow. In a closed hierarchy the aspect could see every
   derivation.
4. `src/Metalama.Patterns.Contracts/ContractBaseAttribute.cs:81-82` — `IConditionallyInheritableAspect`
   inheritance to derived types; a closed hierarchy bounds the set.
5. `src/Metalama.Patterns.Immutability/ImmutableAttribute.cs:38` — `[Inheritable]`, same reasoning.

Nothing breaks if `closed` is ignored. The risk here is entirely of the "missed opportunity" kind, plus the
possibility that a `closed` type reaches a member-introduction advice that cannot legally add to it.

### 5.3 A new expression form (`unsafe(expr)`)

`UnsafeExpressionSyntax : ExpressionSyntax` with fields `Keyword`, `OpenParenToken`, `Expression`,
`CloseParenToken` (`Syntax-5.10.0.xml:496-508`). It is a transparent single-child wrapper, exactly like
`ParenthesizedExpressionSyntax`.

Must change:

1. `src/Metalama.Patterns.Observability/Implementation/RoslynHelper.cs:52-70` — the list of transparent
   wrappers in `GetAccessKind` contains `ParenthesizedExpressionSyntax` (line 69) and nothing else. An
   identifier whose parent is an `UnsafeExpressionSyntax` falls to line 75 and is classified `Read`. Add a
   `case UnsafeExpressionSyntax: return GetAccessKind( parent );`.
2. `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:280-291`
   — `Visit` maintains `_depth` and the gather contexts are keyed on it (`EnsureStarted( this._depth )`,
   `context.StartDepth == this._depth`). An extra wrapper node adds one depth level around an expression.
   Whether that changes the outcome must be tested, not reasoned about; add a test.
3. `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/GraphBuildingContext.cs:82` and
   `RoslynExtensions.cs:100-106` already treat `TypeKind.Pointer` and `IPointerTypeSymbol` as deeply
   immutable, so pointer-typed results of an `unsafe(...)` expression are already classified.

### 5.4 A new collection-expression element (`with(...)`)

`WithElementSyntax : CollectionElementSyntax` with fields `WithKeyword` and
`ArgumentList` of type `ArgumentListSyntax` (`Syntax-5.10.0.xml:816-822`).

This is the benign case. The arguments are ordinary `ArgumentSyntax` nodes, so
`DependencyGraphBuilder.Visitor.VisitArgument` (line 305) already isolates each of them in its own root
gather context. Nothing must change, **provided** the arguments really do arrive as `ArgumentSyntax`. Verify
that with a test rather than by reading the grammar; the whole correctness of chain isolation rests on it.

Two secondary points:

- `src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:292-302`
  — `VisitInvocationExpression` is commented out, so argument *types* are not validated. A `with(...)`
  argument therefore raises no diagnostic either way.
- `src/Metalama.Patterns.Contracts/NotEmptyAttribute.cs:53-60` and `:171-226` recognise collections
  structurally (`IArrayType`, `ICollection`, `ImmutableArray<>`, `IReadOnlyCollection<>`, `ICollection<>`).
  A collection type constructible only through a `with(...)` element is still one of those at the type level,
  so `[NotEmpty]` is unaffected.

### 5.5 A new optional field on an existing statement (labeled `break` / `continue`)

`BreakStatementSyntax` and `ContinueStatementSyntax` gain an optional `Name` of type `IdentifierNameSyntax`.

This is the case with the highest silent-wrongness risk in the subsystem, and it needs exactly one line of
code to fix.

`DependencyGraphBuilder.Visitor.VisitIdentifierName`
(`Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:409-437`) fires on **every**
`IdentifierNameSyntax` the walker reaches, resolves it through the semantic model, and, if a symbol comes
back, appends it to the current dependency chain:

```csharp
var symbol = this._semanticModel.GetSymbolInfo( node, this._cancellationToken ).Symbol;
if ( symbol != null ) { … ctx.AddSymbol( symbol, node ); }
```

The label of a `break outer;` inside a property getter is an `IdentifierNameSyntax`. If Roslyn's semantic
model returns a non-null symbol for it (a label symbol), the walker appends it to the chain. `AccessKind` for
it is `Read` by the fallback at `RoslynHelper.cs:75`, and by the guard at
`DependencyGraphBuilder.Visitor.cs:421` it therefore starts a chain, and it is not filtered out by the
`AccessKind` test at `DependencyGraphBuilder.Visitor.cs:227`. The chain then contains a label symbol, which
is neither `SymbolKind.Property` nor `SymbolKind.Field`, so `supportedStemAndLeafCount` at lines 237-241
truncates the chain there, and every member after the label is dropped from the dependency graph.

The remedy is to add `public override void VisitBreakStatement` / `VisitContinueStatement` that skip the
`Name` field, or to filter `SymbolKind.Label` in `VisitIdentifierName`. Add both, plus a test.

The same reasoning applies to any other optional identifier field added to a statement in future.

---

## 6. Places that would silently do the wrong thing

Ordered by how quietly they fail.

1. **`DependencyGraphBuilder.Visitor.cs:409-437`, `VisitIdentifierName` has no symbol-kind filter.** Any
   identifier that resolves to a symbol enters the dependency chain. A labeled `break` or `continue`
   truncates the chain silently, so a property stops raising `PropertyChanged` for part of its dependency
   set. No diagnostic. See 5.5.

2. **`DependencyGraphBuilder.Visitor.cs:144-212`, the `SymbolKind` switch has no `default`.** Only `Field`,
   `Property when …` and `Method` are validated. Any other symbol kind that reaches `ValidatePathElement`
   passes validation without a diagnostic, which is the wrong default for a validator whose whole purpose is
   to report unanalysable constructs.

3. **`RoslynHelper.cs:75`, `GetAccessKind` returns `Read` for everything it does not recognise**, and the
   comment at lines 72-73 says so deliberately. A new expression form that makes its operand a write target
   would be classified as a read, so the aspect would treat an assignment as a dependency. The comment says
   "In current use cases there's no benefit to having accurate Undefined returns" — that reasoning is what
   must be revisited when the set of expression forms grows.

4. **`DependencyGraphBuilder.Visitor.cs:36`, `SingleOrDefault()` over declaring syntax references.** A
   property with two declaring syntax references — a **partial property**, which C# 13 already allows —
   makes `SingleOrDefault()` throw `InvalidOperationException` from inside the aspect. This is a crash rather
   than a silent wrong answer, but it is an unhandled one, and the neighbouring `Cast<PropertyDeclarationSyntax>()`
   at line 34 has the same character: it is an `InvalidCastException` for any property whose declaring syntax
   is not a `PropertyDeclarationSyntax`. Both need to become tolerant selections.

5. **`DependencyGraphBuilder.Visitor.cs:331-335`, `VisitLocalFunctionStatement` is empty by design.** A
   dependency expressed only through a local function is invisible, and the comment says so. A new form of
   nested callable would inherit the same blindness without even the comment.

6. **`InpcInstrumentationKindLookup.cs:82-83`, `default: return InpcInstrumentationKind.None`.** An `IType`
   shape the lookup does not recognise is reported as "does not implement `INotifyPropertyChanged`", so
   `[Observable]` generates no subscription for it. `IsImplemented()`
   (`Implementation/InpcInstrumentationKindExtensions.cs:13-19`) then maps `None` to `false`, and the third
   state `Unknown` → `null` exists precisely because the author knew a silent `false` was dangerous. The
   `default` case does not use it.

7. **`ImmutabilityExtensions.cs:93`, `return ImmutabilityKind.None`.** Any unrecognised type is "not
   immutable". This is conservative and therefore safe for correctness, but it is silent, and it is what a
   `union` or a `closed` hierarchy will hit.

8. **`GraphBuildingContext.cs:37-45`, `decl switch { …, _ => DependencyAnalysisOptions.Default }`.** A
   declaration kind that is not `ICompilation`, `INamespace`, `INamedType` or `IMember` gets default options,
   so any fabric-configured observability contract on it is ignored without warning.

9. **`CommandAttribute.DiagnosticReporter.cs:19-21`.** `DeclarationKind == Property ? … : …` produces the
   *method* explanation for every declaration kind that is not a property. A user given the wrong reason for
   a rejected candidate is a silent error in the diagnostic itself.

10. **`Metalama.Patterns.Contracts/Numeric/NumericRange.cs:382-419`, the `#if NET8_0_OR_GREATER` generic-math
    branch.** In the `netstandard2.0` and `net472` builds of the aspect assembly, a `[Range]` contract on a
    generic-math type generates **no check at all**: `GeneratePattern` falls to the final `else` (line 419),
    which calls `AppendConvertedValueToExpression` on a type it cannot convert to. The guard makes the
    aspect's generated code depend on which asset of `Metalama.Patterns.Contracts` the pipeline loaded, and
    nothing reports the difference. Re-derive this guard when the target-framework set changes.

11. **`Metalama.Patterns.{Observability,Immutability}.csproj:31`, the stale `NuGetAuditSuppress` comment.**
    It names "Microsoft.CodeAnalysis.Features 4.12.0" and reasons about the `net8.0` shared framework
    pruning `System.Text.Json`. Roslyn 4.12 is out of PB-2027.0 and `net8.0` is gone, so the suppression is
    now justified by a premise that is false. The suppression keeps working and hides whatever the current
    graph actually contains.

12. **Name-reservation via `AllMembers()`**
    (`ClassicObservabilityStrategyImpl.cs:822`, `DependencyPropertyAspectBuilder.cs:237-239`,
    `CommandNamingConventionMatcher.cs:27-28`, `DependencyPropertyNamingConventionMatcher.cs:30-31`).
    These build a `HashSet<string>` of existing names and then introduce members. If a future member kind is
    not enumerated by `AllMembers()`, the aspect introduces a colliding member and the collision surfaces as
    a raw C# error in generated code, pointing at the aspect rather than at the cause.

13. **`Metalama.Patterns.Contracts/ContractExtensions.cs:93-108` omits events.** The fabric
    `VerifyNotNullableDeclarations` documents itself as covering "all public, reference typed, non-nullable
    fields, properties and parameters", which is accurate, but a user who reads it as "all declarations" gets
    silently incomplete coverage. Any new member kind is added to that silent gap by default.

14. **`Metalama.Patterns.Immutability/ImmutableAttribute.cs:57-93` checks only `Fields` and `Properties`.**
    A mutable indexer or a mutable member of a new kind on an `[Immutable]` type is not reported, so the type
    is declared immutable and other aspects — notably the Observability dependency analyser through
    `GraphBuildingContext.IsDeeplyImmutable` (line 74) — trust that declaration and skip change tracking.
    This is the one silent gap whose consequence propagates into another package's generated code.

---

## 7. Quick index of the files a C# 15 change is most likely to touch

```
src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs
src/Metalama.Patterns.Observability/Implementation/RoslynHelper.cs
src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs
src/Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs
src/Metalama.Patterns.Observability/ObservableAttribute.cs
src/Metalama.Patterns.Immutability/ImmutabilityExtensions.cs
src/Metalama.Patterns.Contracts/ContractContext.cs
src/Metalama.Patterns.Contracts/ContractExtensions.cs
src/Metalama.Patterns.Contracts/Numeric/NumericRange.cs
src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs
src/Metalama.Patterns.Wpf/CommandAttribute.DiagnosticReporter.cs
src/Metalama.Patterns.Memoization/MemoizeAttribute.cs
src/tests/Metalama.Patterns.Observability.AspectTests/            (new <Feature>_*.cs + .t.cs)
Metalama.Patterns/Directory.Build.props                            (LangVersion, inherited)
Metalama.Framework/Directory.Build.props:45                        (LangMaxVersion, the actual switch)
```
