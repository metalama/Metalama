# Roslyn 5.x public API changes: Roslyn 5.0 (.NET 10 / C# 14) to Roslyn 5.12 (.NET 11 / C# 15)

Research date: 2026-09-03. Every fact below was read directly from primary sources in
`dotnet/roslyn`, `dotnet/sdk`, `dotnet/docs`, `learn.microsoft.com` and `api.nuget.org`.
Method: I downloaded `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt` from each release branch
and diffed the unions, then diffed `Syntax.xml`, `SyntaxKind.cs`, `SyntaxKindFacts.cs`,
`LanguageVersion.cs` and `TargetFrameworks.props` between branches.

> **The assignment said "Roslyn 5.10 / main". That is now out of date.**
> `main` is **Roslyn 5.12.0-1**. `release/insiders` is 5.11, `release/stable` is 5.10.

---

## 1. Version mapping: Roslyn / Visual Studio / .NET SDK / C#

### 1.1 Established facts

`dotnet/roslyn/docs/wiki/NuGet-packages.md` (main) ends its table at:

- `4.14` = C# 13.0 (Visual Studio 2022 version 17.14, .NET 9)
- `5.0` = C# 14.0 (Visual Studio 2026 version 18.0, .NET 10)

`learn.microsoft.com/visualstudio/extensibility/roslyn-version-support` likewise stops at
`5.0.0 -> Visual Studio 2026 version 18.0`. **Neither document has been updated for 5.1+.**

`eng/Versions.props` (`MajorVersion`/`MinorVersion`) per branch, read 2026-09-03:

| roslyn branch | Roslyn version | notes |
|---|---|---|
| `release/dev18.0` | 5.0.0-2 | VS 2026 18.0, .NET 10 GA, SDK 10.0.1xx |
| `release/dev18.3` | 5.3.0-2 | VS 2026 18.3, SDK 10.0.2xx |
| `release/10.0.3xx` | 5.6.0-2 | SDK 10.0.3xx (= VS 2026 18.6) |
| `release/10.0.4xx` | 5.9.0-1 | SDK 10.0.4xx (= VS 2026 18.9) |
| `release/stable` | 5.10.0-1 | current VS stable channel |
| `release/insiders` | 5.11.0-1 | current VS insiders channel |
| `main` | **5.12.0-1** | .NET 11 / C# 15 |
| `release/dev-next` | 5.0.0-1 | stale |

Also on `main`: `<RazorVsixVersionPrefix>18.12.1</RazorVsixVersionPrefix>` and
`<RazorAddinMajorVersion>18.12</RazorAddinMajorVersion>`, so `main` is aimed at **VS 2026 18.12**.

`dotnet/roslyn/docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` dates its changes by
VS version and once by SDK band: "Introduced in Visual Studio 2026 version 18.3 and .NET 10.0.200".
Combined with the branch table this gives **Roslyn 5.N to Visual Studio 2026 18.N**, and
SDK band to VS: 10.0.1xx/18.0, 10.0.2xx/18.3, 10.0.3xx/18.6, 10.0.4xx/18.9, i.e. every third VS minor.

Published on nuget.org (`api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.csharp/index.json`):
`5.0.0`, `5.3.0`, `5.6.0`, `5.9.0` (plus `5.0.0-2.final`, `5.3.0-2.final`). Exactly the SDK-band cadence.

`dotnet/sdk` `main` (2026-09-03):
- `eng/Versions.props`: `VersionMajor=11`, `VersionMinor=0`, `VersionSDKMinor=1` gives **SDK 11.0.100**,
  with `PreReleaseVersionLabel=rc` and `PreReleaseVersionIteration=1`, i.e. **11.0.100-rc.1**.
- `eng/Version.Details.xml`: `Microsoft.Net.Compilers.Toolset`, `Microsoft.Net.Compilers.Toolset.Framework`,
  `Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.CSharp.Workspaces`,
  `Microsoft.CodeAnalysis.Workspaces.Common`, `Microsoft.CodeAnalysis.Workspaces.MSBuild`,
  `Microsoft.CodeAnalysis.Analyzers`, `Microsoft.CodeAnalysis.PublicApiAnalyzers` are all pinned to
  **`5.12.0-1.26451.109`**.

Visual Studio 2026 release history (`learn.microsoft.com/visualstudio/releases/2026/release-history`,
page updated 2026-08-25): 18.0.0 = 2025-11-11, 18.1.0 = 2025-12-09, 18.2.0 = 2026-01-13,
18.3.0 = 2026-02-10, 18.4.0 = 2026-03-10, 18.5.0 = 2026-04-14, 18.6.0 = 2026-05-12,
18.7.0 = 2026-06-09, 18.8.0 = 2026-07-14, 18.9.0 = 2026-08-11 (latest stable listed: 18.9.2, 2026-08-25).
A monthly cadence from 18.0 / November 2025 puts **18.12 in November 2026**, the .NET 11 GA month.

### 1.2 Conclusion (inference, high confidence)

**.NET 11 GA (SDK 11.0.100, November 2026) ships Roslyn 5.12, which is Visual Studio 2026 version 18.12.**
Evidence: `dotnet/sdk` `main` (11.0.100-rc.1) pins Roslyn 5.12.0-1; Roslyn `main` is 5.12 and its Razor
VSIX version prefix is 18.12; the 18.N / 5.N mapping is exact on every branch that exists.
The residual risk is a slip to 5.13 / 18.13 if another month is inserted before GA.

### 1.3 Assembly versions

`AssemblyVersion` in official builds is `$(MajorVersion).$(MinorVersion).0.0`.
So Roslyn 5.12 assemblies are `5.12.0.0`; 5.0 assemblies were `5.0.0.0`. Every Roslyn minor bump
is a new assembly identity, exactly as in the 4.x line.

---

## 2. Target frameworks and minimum runtime

`eng/targets/TargetFrameworks.props`:

| branch | `NetRoslyn` | `NetRoslynAll` | `NetVS` | `NetVSCode` | `NetRoslynBuildHostNetCoreVersion` |
|---|---|---|---|---|---|
| `release/dev18.0` (5.0) | net9.0 | net8.0;net9.0 | net8.0 | net9.0 | net8.0 |
| `release/dev18.3` (5.3) | net9.0 | net8.0;net9.0;net10.0 | net8.0 | net10.0 | net8.0 |
| `release/10.0.4xx` (5.9) | net10.0 | net10.0 | net10.0 | net10.0 | net8.0 |
| `release/stable` (5.10) | net10.0 | net10.0 | net10.0 | net10.0 | net8.0 |
| `release/insiders` (5.11) | net10.0 | net10.0 | net10.0 | net10.0 | net8.0 |
| `main` (5.12) | net10.0 | net10.0 | net10.0 | net10.0 | net8.0 |

Shipping library projects (`Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`,
`Microsoft.CodeAnalysis.Workspaces`, `Microsoft.CodeAnalysis.CSharp.Workspaces`,
`Microsoft.CodeAnalysis.Features`) all declare
`<TargetFrameworks>$(NetRoslynSourceBuild);netstandard2.0</TargetFrameworks>`.
`csc` declares `<TargetFrameworks>$(NetRoslynSourceBuild);net472</TargetFrameworks>`.

### Verified against the published packages (nuspec dependency groups)

| package version | target frameworks in the nupkg |
|---|---|
| Microsoft.CodeAnalysis.CSharp **5.0.0** | `net8.0`, `net9.0`, `.NETStandard2.0` |
| Microsoft.CodeAnalysis.CSharp **5.3.0** | `net8.0`, `net9.0`, `net10.0`, `.NETStandard2.0` |
| Microsoft.CodeAnalysis.CSharp **5.6.0** | `net10.0`, `net8.0`, `.NETStandard2.0` (net9.0 dropped) |
| Microsoft.CodeAnalysis.CSharp **5.9.0** | `net10.0`, `.NETStandard2.0` (net8.0 dropped) |

**This is the most consequential packaging change in the 5.x line.** From Roslyn 5.9 onward a consumer
targeting `net8.0` or `net9.0` no longer gets a matching .NET Core asset and falls back to the
`netstandard2.0` asset, which drags in `System.Memory`, `System.Runtime.CompilerServices.Unsafe`,
`System.Buffers`, `System.Numerics.Vectors`, `System.Text.Encoding.CodePages`,
`System.Threading.Tasks.Extensions`, `System.Collections.Immutable` and `System.Reflection.Metadata`.

Minimum runtime for Roslyn 5.12:
- **.NET Core: net10.0.** Nothing lower is shipped except the `netstandard2.0` asset.
- **.NET Framework: net472** (`csc.exe`/`vbc.exe` desktop, `Microsoft.Net.Compilers.Toolset.Framework`).
- The `MSBuildWorkspace` BuildHost stays on **net8.0** ("until .NET 8 EOL in November 2026",
  per `docs/contributing/target-framework-strategy.md`).
- `docs/contributing/target-framework-strategy.md` on `main` states Visual Studio's private runtime is
  now **net10.0** ("Visual Studio: requires us to ship `net472` for base IDE components and
  `$(NetVisualStudio)` (presently `net10.0`) for private runtime components") and that VS Code DevKit
  is likewise `net10.0`. In Roslyn 5.0 the VS private runtime was net8.0.

`netstandard2.0` dependency versions moved: `System.Collections.Immutable` 9.0.0 to **10.0.1**,
`System.Reflection.Metadata` 9.0.0 to **10.0.1**, `System.Memory` 4.6.0 to 4.6.3,
`System.Runtime.CompilerServices.Unsafe` 6.1.0 to 6.1.2, `System.Buffers` 4.6.0 to 4.6.1,
`System.Numerics.Vectors` 4.6.0 to 4.6.1.
Also, `Microsoft.CodeAnalysis.Common` 5.9.0 depends on `Microsoft.CodeAnalysis.Analyzers`
**`5.9.0-1.26328.17`** (a prerelease, versioned in lockstep with Roslyn) instead of the old stable `3.11.0`.

---

## 3. `LanguageVersion` and `LanguageVersionFacts`

Source: `src/Compilers/CSharp/Portable/LanguageVersion.cs` on `main`.

### New enum member

```
Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp15 = 1500
```

Introduced in **Roslyn 5.11** (absent from 5.10 and everything earlier).
Unchanged: `LatestMajor = int.MaxValue - 2`, `Preview = int.MaxValue - 1`,
`Latest = int.MaxValue`, `Default = 0`. There is **no `CSharp16`**.

XML documentation on `CSharp15` lists its features:
Collection expression arguments; Unions; Non-virtual static members in interfaces;
Closed class hierarchies; Labeled `break` and `continue`; Extension indexers.

### `LanguageVersionFacts` (the public static class is `LanguageVersionExtensions` in source; the public surface is unchanged in shape)

- `ToDisplayString(LanguageVersion.CSharp15)` returns `"15.0"`.
- `TryParse("15")` and `TryParse("15.0")` return `LanguageVersion.CSharp15`.
- `MapSpecifiedToEffectiveVersion(Latest | Default | LatestMajor)` returns **`LanguageVersion.CSharp15`**
  (it returned `CSharp14` in 5.0). So **C# 15 is the default language version of the .NET 11 compiler.**
- `internal static LanguageVersion CurrentVersion => LanguageVersion.CSharp15;`
- `internal const LanguageVersion CSharpNext = LanguageVersion.Preview;` (unchanged).
- `LanguageVersionExtensionsInternal.IsValid` now accepts `CSharp15`.

No `LanguageVersionFacts` member was removed or had its signature changed.

---

## 4. New `SyntaxKind` members

Source: `src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`, diffed `release/dev18.0` to `main`.
The file still carries the comment
`// DO NOT CHANGE NUMBERS ASSIGNED TO EXISTING KINDS OR YOU WILL BREAK BINARY COMPATIBILITY`;
**no existing value was renumbered or removed.** `SyntaxKind` is still declared `: ushort`.

Contextual keywords, appended after `ExtensionKeyword = 8451`:

```csharp
/// <summary>Represents union.</summary>
UnionKeyword = 8452,
/// <summary>Represents closed.</summary>
ClosedKeyword = 8453,
/// <summary>Represents safe.</summary>
[Experimental("RSEXPERIMENTAL006", UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
SafeKeyword = 8454,
```

Expressions:

```csharp
[Experimental("RSEXPERIMENTAL006", UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
UnsafeExpression = 8769,
```

(slot 8768 remains the commented-out `NameOfExpression`.)

Declarations and elements, appended after `IgnoredDirectiveTrivia = 9080`:

```csharp
WithElement = 9081,
UnionDeclaration = 9082,
```

A new comment was added at the top of the file: "When adding new experimental kinds, you will need to
manually specify RSEXPERIMENTAL006, as not all projects that reference this file have RoslynExperiments
available."

That is the complete `SyntaxKind` delta: **5 new members, 0 removed, 0 renumbered.**

---

## 5. New syntax node types and grammar changes (`Syntax.xml`)

The complete `Syntax.xml` diff from `release/dev18.0` to `main` is five items.

### 5.1 `UnionDeclarationSyntax` (new)

```xml
<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" SkipConvenienceFactories="true">
  <Kind Name="UnionDeclaration"/>
  AttributeLists    : SyntaxList<AttributeListSyntax>                    (override)
  Modifiers         : SyntaxTokenList                                    (override)
  Keyword           : SyntaxToken (UnionKeyword)                         (override)
  Identifier        : SyntaxToken (IdentifierToken)                      (override)
  TypeParameterList : TypeParameterListSyntax?                           (override, optional)
  ParameterList     : ParameterListSyntax?                               (override, optional)
  BaseList          : BaseListSyntax?                                    (override, optional)
  ConstraintClauses : SyntaxList<TypeParameterConstraintClauseSyntax>    (override)
  OpenBraceToken    : SyntaxToken?                                       (override, optional)
  Members           : SyntaxList<MemberDeclarationSyntax>                (override)
  CloseBraceToken   : SyntaxToken?                                       (override, optional)
  SemicolonToken    : SyntaxToken?                                       (override, optional)
</Node>
```

It derives from `TypeDeclarationSyntax`, so it is also a `BaseTypeDeclarationSyntax` and a
`MemberDeclarationSyntax`. Structurally it is identical to `InterfaceDeclarationSyntax`.
`SkipConvenienceFactories="true"` means there is **only the full 12-parameter factory**; there is no
`SyntaxFactory.UnionDeclaration("Name")` convenience overload.

The `TypeDeclarationSyntax.Keyword` documentation comment changed from
`("class", "struct", "interface", "record", "extension")` to
`("class", "struct", "interface", "record", "extension", "union")`.

### 5.2 `WithElementSyntax` (new)

```xml
<Node Name="WithElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="WithElement"/>
  WithKeyword  : SyntaxToken (WithKeyword)
  ArgumentList : ArgumentListSyntax
</Node>
```

A third concrete `CollectionElementSyntax` alongside `ExpressionElementSyntax` and `SpreadElementSyntax`.
It is the `with(...)` element of a collection expression, for example `[with(capacity: 10), .. values]`.
`CollectionExpressionSyntax.Elements` is a `SeparatedSyntaxList<CollectionElementSyntax>`, so any
exhaustive switch over collection elements now has a third case.

### 5.3 `UnsafeExpressionSyntax` (new, experimental)

```xml
<Node Name="UnsafeExpressionSyntax" Base="ExpressionSyntax"
      ExperimentalUrl="https://github.com/dotnet/roslyn/issues/82789">
  <Kind Name="UnsafeExpression"/>
  Keyword         : SyntaxToken (UnsafeKeyword)
  OpenParenToken  : SyntaxToken
  Expression      : ExpressionSyntax
  CloseParenToken : SyntaxToken
</Node>
```

The syntax is `unsafe(expr)`. Every public member of this node, its `SyntaxKind`, its factories and its
visitor methods carry `[Experimental("RSEXPERIMENTAL006")]`. It is a **C# 16 / `LangVersion=preview`**
feature, not part of C# 15.

### 5.4 `BreakStatementSyntax` and `ContinueStatementSyntax` gained a child (labeled break/continue)

```xml
<Node Name="BreakStatementSyntax" Base="StatementSyntax">
  AttributeLists, BreakKeyword,
+ Name : IdentifierNameSyntax?   (optional)      <!-- inserted between keyword and semicolon -->
  SemicolonToken
</Node>
```

and identically for `ContinueStatementSyntax`.

**This is the change most likely to break a syntax rewriter generated from the grammar**: two
long-standing nodes changed their child count from 3 to 4, and the new child was inserted in the
middle, not appended. `SyntaxFactory.BreakStatement` now has **six** overloads and
`BreakStatementSyntax.Update` has **three** (the old 2- and 3-parameter forms are retained, so nothing
was removed).

### 5.5 `ParameterSyntax` now validates (behavioural break)

```xml
- <Node Name="ParameterSyntax" Base="BaseParameterSyntax" SkipConvenienceFactories="true">
+ <Node Name="ParameterSyntax" Base="BaseParameterSyntax" SkipConvenienceFactories="true" HasValidate="true">
-   <Field Name="Type" Type="TypeSyntax" Optional="true" Override="true"/>
+   <Field Name="Type" Type="TypeSyntax" Optional="true" RequiredForTest="true" Override="true"/>
```

and `src/Compilers/CSharp/Portable/Syntax/ParameterSyntax.cs` on `main`:

```csharp
private partial void Validate()
{
    if (Type is null && Identifier.IsKind(SyntaxKind.None))
    {
        throw new System.ArgumentException(CSharpResources.ParameterRequiresTypeOrIdentifier);
    }
}
```

Creating a `ParameterSyntax` with **both** `Type` and `Identifier` missing now throws
`ArgumentException` at construction time. Previously it silently produced a degenerate node.
This closes `dotnet/roslyn#78961`.

### 5.6 What did not change

`ExtensionBlockDeclarationSyntax` is **unchanged**. Extension indexers reuse the existing
`IndexerDeclarationSyntax` inside an existing `ExtensionBlockDeclarationSyntax`. There is no new node,
no new `SyntaxKind` and no new factory for them.

---

## 6. New `SyntaxFactory` methods (`Microsoft.CodeAnalysis.CSharp.SyntaxFactory`)

Non-experimental:

```csharp
static BreakStatementSyntax BreakStatement(IdentifierNameSyntax? name = null);
static BreakStatementSyntax BreakStatement(SyntaxList<AttributeListSyntax> attributeLists, IdentifierNameSyntax? name);
static BreakStatementSyntax BreakStatement(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken breakKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken);
static ContinueStatementSyntax ContinueStatement(IdentifierNameSyntax? name = null);
static ContinueStatementSyntax ContinueStatement(SyntaxList<AttributeListSyntax> attributeLists, IdentifierNameSyntax? name);
static ContinueStatementSyntax ContinueStatement(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken continueKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken);

static UnionDeclarationSyntax UnionDeclaration(
    SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers,
    SyntaxToken keyword, SyntaxToken identifier,
    TypeParameterListSyntax? typeParameterList, ParameterListSyntax? parameterList,
    BaseListSyntax? baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
    SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members,
    SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

static WithElementSyntax WithElement(ArgumentListSyntax? argumentList = null);
static WithElementSyntax WithElement(SyntaxToken withKeyword, ArgumentListSyntax argumentList);
```

Experimental (`RSEXPERIMENTAL006`):

```csharp
static UnsafeExpressionSyntax UnsafeExpression(ExpressionSyntax expression);
static UnsafeExpressionSyntax UnsafeExpression(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);
```

Overload-resolution note: `SyntaxFactory.BreakStatement()` remains unambiguous because the
zero-parameter overload is still declared, and a candidate with fewer declared parameters is preferred
over one that omits an optional parameter. The same applies to `ContinueStatement()`.

---

## 7. `CSharpSyntaxVisitor`, `CSharpSyntaxVisitor<TResult>`, `CSharpSyntaxRewriter`

New `virtual` visit methods. A rewriter that must handle every node has to override these:

| method | experimental |
|---|---|
| `CSharpSyntaxVisitor.VisitUnionDeclaration(UnionDeclarationSyntax node)` | no |
| `CSharpSyntaxVisitor.VisitWithElement(WithElementSyntax node)` | no |
| `CSharpSyntaxVisitor.VisitUnsafeExpression(UnsafeExpressionSyntax node)` | **RSEXPERIMENTAL006** |
| `CSharpSyntaxVisitor<TResult>.VisitUnionDeclaration(...)` | no |
| `CSharpSyntaxVisitor<TResult>.VisitWithElement(...)` | no |
| `CSharpSyntaxVisitor<TResult>.VisitUnsafeExpression(...)` | **RSEXPERIMENTAL006** |
| `override CSharpSyntaxRewriter.VisitUnionDeclaration(...)` | no |
| `override CSharpSyntaxRewriter.VisitWithElement(...)` | no |
| `override CSharpSyntaxRewriter.VisitUnsafeExpression(...)` | **RSEXPERIMENTAL006** |

No visit method was removed or renamed. `CSharpSyntaxWalker` inherits the new methods from
`CSharpSyntaxVisitor` and needs no additional override.

`VisitBreakStatement` and `VisitContinueStatement` keep their signatures, but the nodes they receive
now have a fourth child, so a rewriter that reconstructs them by calling an old `Update` overload
silently drops the label.

---

## 8. `SyntaxFacts` behavioural changes (`SyntaxKindFacts.cs`)

```csharp
// IsTypeDeclaration
case SyntaxKind.UnionDeclaration:   // NEW, returns true

// keyword to expression-kind map
case SyntaxKind.UnsafeKeyword: return SyntaxKind.UnsafeExpression;   // NEW

// GetTypeDeclarationKind
case SyntaxKind.UnionKeyword:     return SyntaxKind.UnionDeclaration;          // NEW
case SyntaxKind.ExtensionKeyword: return SyntaxKind.ExtensionBlockDeclaration; // NEW (previously fell through to None)

// GetContextualKeywordKinds()
- for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.ExtensionKeyword; i++)
+ for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.SafeKeyword; i++)

// IsContextualKeyword: now also true for UnionKeyword, ClosedKeyword, SafeKeyword
// GetContextualKeywordKind("union" | "closed" | "safe"): new mappings
// GetText(UnionKeyword | ClosedKeyword | SafeKeyword): "union" | "closed" | "safe"
```

`SyntaxFacts.GetTypeDeclarationKind(SyntaxKind.ExtensionKeyword)` returning
`ExtensionBlockDeclaration` instead of `SyntaxKind.None` is a **silent behavioural change** for any
caller that relied on `None` to mean "not a type-declaration keyword". The tracking comment
"public API, decide what we want for extension declaration" (issue 78957) was removed at the same time.

---

## 9. Symbol API changes (`Microsoft.CodeAnalysis`)

### 9.1 `ITypeSymbol` (interface members added)

```csharp
/// <summary>True if language treats the type as a Union.</summary>
bool IsUnion { get; }

/// <summary>When IsUnion is true, returns the case types of the union. Otherwise, an empty array.</summary>
ImmutableArray<ITypeSymbol> UnionCaseTypes { get; }

/// <summary>Indicates that the type is restricted from being inherited from outside its containing module.</summary>
bool IsClosed { get; }

/// <summary>Gets the direct derived types of a closed type.</summary>
/// <exception cref="InvalidOperationException">If this is not a closed type.</exception>
ClosedDerivedTypeInfo GetClosedDerivedTypeInfo(CancellationToken cancellationToken);
```

**`TypeKind` gained no new member.** A union is `TypeKind.Class` with `IsUnion == true`.
The `TypeKind` enum is byte-for-byte identical between 5.0 and 5.12 (`Extension = 14` is the highest
value and was already present in 5.0).

### 9.2 New public type `Microsoft.CodeAnalysis.ClosedDerivedTypeInfo`

```csharp
public readonly struct ClosedDerivedTypeInfo
{
    /// <summary>Possible direct derived types of the closed type.</summary>
    public ImmutableArray<INamedTypeSymbol> ClosedDerivedTypes { get; }

    /// <summary>Whether ClosedDerivedTypes is a complete set. This is false, for example, when a
    /// generic closed type has an unspeakable derived type.</summary>
    public bool IsComplete { get; }

    public ClosedDerivedTypeInfo();
}
```

### 9.3 `INamedTypeSymbol` plus new public type `Microsoft.CodeAnalysis.TypeLayout`

```csharp
// INamedTypeSymbol
Microsoft.CodeAnalysis.TypeLayout TypeLayout { get; }

// new public struct
public readonly struct TypeLayout : IEquatable<TypeLayout>
{
    public System.Runtime.InteropServices.LayoutKind Kind { get; }   // default(TypeLayout).Kind == LayoutKind.Auto
    public ushort PackingSize { get; }
    public int Size { get; }
    public bool Equals(TypeLayout other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(TypeLayout left, TypeLayout right);
    public static bool operator !=(TypeLayout left, TypeLayout right);
    public TypeLayout();
}
```

This makes public what used to be internal. It exists for
`System.Runtime.InteropServices.ExtendedLayoutAttribute` (`docs/features/ExtendedLayoutAttribute.md`):
a type carrying that attribute reports `LayoutKind` `Extended` (`1`), `Size` 0 and `Pack` 0, and the
compiler emits `TypeAttributes.ExtendedLayout`. `ExtendedLayoutAttribute` may not be combined with
`StructLayoutAttribute`, and in C# it may not be combined with `InlineArrayAttribute` either.
The struct documents itself as reporting metadata or source values, not a computed size.

### 9.4 `IMethodSymbol` and `IPropertySymbol` (extension members)

```csharp
/// <summary>If this is a method of an extension block that can be applied to a receiver of the given
/// type, returns the method symbol in the substituted extension for that receiver type. Otherwise null.</summary>
IMethodSymbol? IMethodSymbol.ReduceExtensionMember(ITypeSymbol receiverType);

IPropertySymbol? IPropertySymbol.ReduceExtensionMember(ITypeSymbol receiverType);
```

`ReduceExtensionMethod(ITypeSymbol)` is unchanged and still present.
`IPropertySymbol.ReduceExtensionMember` is what makes **extension indexers** (a C# 15 feature)
reachable, because an extension indexer is an `IPropertySymbol` with `IsIndexer == true`.
Both members were introduced in **Roslyn 5.3** and were never experimental.

`MethodKind` gained no member; a reduced extension member is still `MethodKind.ReducedExtension = 13`.

### 9.5 `ISymbol` and `IModuleSymbol` (memory-safety preview, experimental)

```csharp
[Experimental("RSEXPERIMENTAL006")] bool ISymbol.RequiresUnsafeContext { get; }
[Experimental("RSEXPERIMENTAL006")] MemorySafetyRulesVersion IModuleSymbol.MemorySafetyRulesVersion { get; }

[Experimental("RSEXPERIMENTAL006")]
public enum MemorySafetyRulesVersion { Version1 = 1, Version2 = 2 }
```

The assembly opt-in is recorded in metadata by
`System.Runtime.CompilerServices.MemorySafetyRulesAttribute`.

### 9.6 `WellKnownMemberNames` (new public constants)

```csharp
public const string HasValuePropertyName  = "HasValue";
public const string TryGetValueMethodName = "TryGetValue";
```

The related union names stay internal: `UnionMembersInterfaceName = "IUnionMembers"` and
`UnionFactoryMethodName = "Create"`. `ValuePropertyName` ("Value") already existed; its documentation
now adds "Also required name for the IUnion.Value property used in Union matching."

### 9.7 `IParameterSymbol`

**No change.** No member was added, removed or altered between 5.0 and 5.12.

---

## 10. `IOperation` and `OperationKind`

### New `OperationKind` member

```csharp
Microsoft.CodeAnalysis.OperationKind.CollectionExpressionElementsPlaceholder = 129
```

That is the **only** new `OperationKind`. No value was removed or renumbered.

### New operation interface

```csharp
public interface ICollectionExpressionElementsPlaceholderOperation : IOperation { }
```

(`HasType="true"`, no extra properties). It represents "the elements of a collection expression as they
are passed to some construction method specified by a `[CollectionBuilder]` attribute". It appears as
the `Value` of an `IArgumentOperation` inside `ICollectionExpressionOperation.ConstructArguments`,
standing for the final `ReadOnlySpan` argument of a collection-builder method.

### Changed operation interface

```csharp
// ICollectionExpressionOperation gained:
ImmutableArray<IOperation> ConstructArguments { get; }
```

`ChildrenOrder` of `ICollectionExpressionOperation` is now `ConstructArguments,Elements`, so
`IOperation.ChildOperations` for a collection expression enumerates the construct arguments **first**.
Arguments are in evaluation order, are never `default`, and are `IArgumentOperation` when binding
succeeded; when binding failed they can be any operation. Params arguments are collected into arrays
in expanded form and defaults are supplied for missing optional arguments.

### `OperationVisitor`

```csharp
virtual void OperationVisitor.VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation operation);
virtual TResult? OperationVisitor<TArgument, TResult>.VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation operation, TArgument argument);
```

### `CommonConversion` (struct, `Microsoft.CodeAnalysis.Operations`)

```csharp
/// <summary>Returns true if the conversion is a union conversion.</summary>
[MemberNotNullWhen(true, nameof(MethodSymbol))]
public bool IsUnion { get; }
```

Implementation: true when `MethodSymbol` is a constructor whose containing type has `IsUnion`, or a
static `Create` method on a nested `IUnionMembers` interface of a union type.
`CommonConversion.MethodSymbol` documentation was updated to cover the union case.

---

## 11. Other `Microsoft.CodeAnalysis` (Core) additions

```csharp
// Emit and Edit-and-Continue
bool Microsoft.CodeAnalysis.Emit.EmitDifferenceOptions.MethodImplEntriesSupported { get; init; }

// Source text hashing
Microsoft.CodeAnalysis.Text.SourceHashAlgorithm.Sha384 = 3
Microsoft.CodeAnalysis.Text.SourceHashAlgorithm.Sha512 = 4

// Incremental source generators: pre-compilation outputs (RSEXPERIMENTAL007)
Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind.PreCompilation = 16
const string Microsoft.CodeAnalysis.WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput";

[Experimental("RSEXPERIMENTAL007", UrlFormat = "https://github.com/dotnet/roslyn/issues/83089")]
void IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValueProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource> action);
void IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValuesProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource> action);

[Experimental("RSEXPERIMENTAL007")]
public readonly struct PreCompilationSourceProductionContext
{
    public void AddSource(string hintName, SourceText sourceText);
    public void AddSource(string hintName, string source);
    public CancellationToken CancellationToken { get; }
    public PreCompilationSourceProductionContext();
}
```

`RegisterPreCompilationSourceOutput` fills the gap between `RegisterPostInitializationOutput` (no inputs)
and `RegisterSourceOutput` (full compilation): the produced source is added to the **initial**
compilation, before any compilation-dependent phase, while the generator may still read
`AdditionalTextsProvider`, `ParseOptionsProvider` and `AnalyzerConfigOptionsProvider`. It was built for
Razor (`docs/features/pre-compilation-source-outputs.md` reports roughly a 50 percent improvement for
Razor generation) and the sources it produces are visible to **all** generators' standard phases, not
only the producing generator's.

`RuntimeCapability.RuntimeAsyncMethods = 9` **already existed in Roslyn 5.0**; it is not new.

---

## 12. `Microsoft.CodeAnalysis.CSharp` additions beyond syntax

```csharp
// Conversions
bool Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion { get; }

// Semantic model helpers
static AwaitExpressionInfo CSharpExtensions.GetAwaitExpressionInfo(this SemanticModel? semanticModel, LocalDeclarationStatementSyntax awaitUsingDeclaration);
static AwaitExpressionInfo CSharpExtensions.GetAwaitExpressionInfo(this SemanticModel? semanticModel, UsingStatementSyntax awaitUsingStatement);
static Conversion CSharpExtensions.GetValueConversion(this ICoalesceOperation coalesceExpression);   // new in 5.12

// ForEachStatementInfo
AwaitExpressionInfo ForEachStatementInfo.MoveNextAwaitableInfo { get; }   // default for a synchronous foreach
AwaitExpressionInfo ForEachStatementInfo.DisposeAwaitableInfo  { get; }   // default for a synchronous foreach

// Compilation options (RSEXPERIMENTAL006, new in 5.12)
MemorySafetyRulesVersion CSharpCompilationOptions.MemorySafetyRulesVersion { get; }
CSharpCompilationOptions CSharpCompilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion version);
```

`ForEachStatementInfo.Equals` and `GetHashCode` now include the two awaitable infos, so two
`ForEachStatementInfo` values that compared equal under 5.0 can compare unequal under 5.12.

The VB equivalent was added as well:
`VisualBasicExtensions.GetValueConversion(ICoalesceOperation) -> VisualBasic.Conversion`.
That is the **only** public API change in `Microsoft.CodeAnalysis.VisualBasic` between 5.0 and 5.12.

---

## 13. Workspaces, Features, Scripting

I diffed `PublicAPI.{Shipped,Unshipped}.txt` for `src/Workspaces/Core/Portable`,
`src/Workspaces/CSharp/Portable`, `src/Workspaces/Core/Desktop`, `src/Workspaces/MSBuild/Core`,
`src/Workspaces/Remote/Core`, `src/Features/Core/Portable`, `src/Features/CSharp/Portable`,
`src/Scripting/Core` and `src/Scripting/CSharp`.

There are only three additions in the whole set, all in `Microsoft.CodeAnalysis.Workspaces`:

```csharp
bool Microsoft.CodeAnalysis.Editing.DeclarationModifiers.IsClosed { get; }
DeclarationModifiers Microsoft.CodeAnalysis.Editing.DeclarationModifiers.WithIsClosed(bool isClosed);
static DeclarationModifiers Microsoft.CodeAnalysis.Editing.DeclarationModifiers.Closed { get; }
```

**Zero removals, zero signature changes.** `Formatter`, `Simplifier`, `SyntaxGenerator`,
`SymbolFinder`, `Solution`, `Project`, `Document`, `AdhocWorkspace` and `MSBuildWorkspace` are
untouched. `SyntaxGenerator` gained no union or extension-indexer factory.

---

## 14. Removed or changed-signature public API (breaking for a Roslyn consumer)

### 14.1 `Microsoft.CodeAnalysis`: eleven members changed to `params ImmutableArray<T>` (Roslyn 5.9)

Removed entry, then re-added with `params`:

| removed | added |
|---|---|
| `abstract AnalysisContext.RegisterSymbolAction(Action<SymbolAnalysisContext>, ImmutableArray<SymbolKind>)` | same with `params` |
| `abstract AnalysisContext.RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext>, ImmutableArray<TLanguageKindEnum>)` | same with `params` |
| `virtual AnalysisContext.RegisterOperationAction(Action<OperationAnalysisContext>, ImmutableArray<OperationKind>)` | same with `params` |
| `abstract CompilationStartAnalysisContext.RegisterSymbolAction(..., ImmutableArray<SymbolKind>)` | same with `params` |
| `abstract CompilationStartAnalysisContext.RegisterSyntaxNodeAction<T>(..., ImmutableArray<T>)` | same with `params` |
| `virtual CompilationStartAnalysisContext.RegisterOperationAction(..., ImmutableArray<OperationKind>)` | same with `params` |
| `abstract CodeBlockStartAnalysisContext<T>.RegisterSyntaxNodeAction(..., ImmutableArray<T>)` | same with `params` |
| `abstract OperationBlockStartAnalysisContext.RegisterOperationAction(..., ImmutableArray<OperationKind>)` | same with `params` |
| `abstract SymbolStartAnalysisContext.RegisterOperationAction(..., ImmutableArray<OperationKind>)` | same with `params` |
| `abstract SymbolStartAnalysisContext.RegisterSyntaxNodeAction<T>(..., ImmutableArray<T>)` | same with `params` |
| `static AssemblyMetadata.Create(ImmutableArray<ModuleMetadata>)` | `static AssemblyMetadata.Create(params ImmutableArray<ModuleMetadata>)` |

Roslyn applied C# 13 params collections to its own surface. The parameter **type** did not change, so
this is not a binary break, and existing call sites that pass an `ImmutableArray<T>` still compile.
Each of these already had a sibling `params T[]` overload, so both are now params collections.
Under the C# 13 params-collections better-function-member rules a `params T[]` candidate should still be
preferred over `params ImmutableArray<T>` for loose element arguments, but I did not verify that by
compiling; treat it as an inference.

### 14.2 `Microsoft.CodeAnalysis.CSharp.Conversion` default value (Roslyn 5.11)

From `docs/Breaking API Changes.md`, section "Version 5.11.0" (PR dotnet/roslyn#84628):

```csharp
var conv = default(Conversion);
conv.Exists;      // previously True, now False
conv.IsExplicit;  // previously True, now False
```

All boolean properties of `default(Conversion)` now return `false`.
This is the **only** entry in `Breaking API Changes.md` for the entire 5.x line.

### 14.3 `SyntaxFactory.Parameter` now throws

See section 5.5: `ArgumentException` when both `Type` and `Identifier` are missing.

### 14.4 `SyntaxFacts.GetTypeDeclarationKind(SyntaxKind.ExtensionKeyword)`

Returns `SyntaxKind.ExtensionBlockDeclaration` instead of `SyntaxKind.None`. See section 8.

### 14.5 Interface members added

`ITypeSymbol` (4), `ISymbol` (1), `IModuleSymbol` (1), `IMethodSymbol` (1), `IPropertySymbol` (1),
`INamedTypeSymbol` (1), `ICollectionExpressionOperation` (1). Roslyn's symbol and operation interfaces
are documented as not implementable outside Roslyn, but any external implementation breaks.

### 14.6 Package target frameworks dropped

`net9.0` assets were removed in Roslyn 5.6 and `net8.0` assets in Roslyn 5.9. See section 2.

### 14.7 Entries that look like changes in the diff but are not

`[RSEXPERIMENTAL001]abstract SemanticModel.NullableAnalysisIsDisabled.get` and
`[RSEXPERIMENTAL004]GeneratorRunResult.HostOutputs.get` appear as "removed, then re-added with a prefix"
because the PublicApiAnalyzer started recording the experimental diagnostic identifier in the entry text
from Roslyn 5.6. I verified against `SemanticModel.cs` on `release/dev18.0` that the
`[Experimental(RoslynExperiments.NullableDisabledSemanticModel, ...)]` attribute was **already present in
5.0**. There is no behavioural change.

---

## 15. `RSEXPERIMENTAL006`: which C# 15 APIs are stable and which are not

`src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs`, identical on `release/stable` and
`main`:

```csharp
internal const string NullableDisabledSemanticModel = "RSEXPERIMENTAL001";
internal const string GeneratorHostOutputs          = "RSEXPERIMENTAL004";
// The UrlFormat property is customized per-api to point at a public API tracking issue for the feature
internal const string PreviewLanguageFeatureApi     = "RSEXPERIMENTAL006";
internal const string PreCompilationSourceOutput    = "RSEXPERIMENTAL007";
// Previously taken: RSEXPERIMENTAL003 (SyntaxTokenParser), RSEXPERIMENTAL005
```

`RSEXPERIMENTAL006` means "API for a language feature that is still in preview". It is reused across
features, so its meaning shifts from release to release.

Between Roslyn 5.10 and 5.11 the union, closed-hierarchy, labeled-break and
collection-expression-arguments APIs had the `[Experimental]` attribute **removed**. They are now plain
public API. Only the memory-safety (unsafe evolution) surface remains marked.

### Introduction and stabilization timeline

Y = plain public API. X = public but `[Experimental("RSEXPERIMENTAL006")]`. Dash = absent.

| API | 5.0 | 5.3 | 5.6 | 5.9 | 5.10 | 5.11 | 5.12 |
|---|---|---|---|---|---|---|---|
| `LanguageVersion.CSharp15` | - | - | - | - | - | Y | Y |
| `SyntaxKind.UnionDeclaration`, `UnionDeclarationSyntax` | - | - | X | X | X | Y | Y |
| `SyntaxKind.WithElement`, `WithElementSyntax` | - | - | X | X | X | Y | Y |
| `ICollectionExpressionOperation.ConstructArguments` | - | - | X | X | X | Y | Y |
| `OperationKind.CollectionExpressionElementsPlaceholder` | - | - | X | X | X | Y | Y |
| `ITypeSymbol.IsUnion` and `UnionCaseTypes` | - | - | - | X | X | Y | Y |
| `ITypeSymbol.IsClosed`, `GetClosedDerivedTypeInfo`, `ClosedDerivedTypeInfo` | - | - | - | X | X | Y | Y |
| `BreakStatementSyntax.Name`, `ContinueStatementSyntax.Name` | - | - | - | X | X | Y | Y |
| `IMethodSymbol`/`IPropertySymbol.ReduceExtensionMember` | - | Y | Y | Y | Y | Y | Y |
| `ForEachStatementInfo.MoveNextAwaitableInfo`/`DisposeAwaitableInfo` | - | Y | Y | Y | Y | Y | Y |
| `EmitDifferenceOptions.MethodImplEntriesSupported` | - | Y | Y | Y | Y | Y | Y |
| `SourceHashAlgorithm.Sha384`/`Sha512` | - | - | Y | Y | Y | Y | Y |
| `IncrementalGeneratorOutputKind.PreCompilation`, `PreCompilationSourceProductionContext` | - | - | - | Y | Y | Y | Y |
| `INamedTypeSymbol.TypeLayout`, `Microsoft.CodeAnalysis.TypeLayout` | - | - | - | - | - | Y | Y |
| `CSharpExtensions.GetValueConversion(ICoalesceOperation)` | - | - | - | - | - | - | Y |
| `SyntaxKind.UnsafeExpression`, `UnsafeExpressionSyntax`, `SafeKeyword` | - | - | - | X | X | X | X |
| `ISymbol.RequiresUnsafeContext`, `IModuleSymbol.MemorySafetyRulesVersion` | - | - | - | - | - | X | X |
| `MemorySafetyRulesVersion`, `CSharpCompilationOptions.WithMemorySafetyRulesVersion` | - | - | - | - | - | - | X |

Reading of this table: everything a C# 15 consumer needs is stable, non-experimental public API in
Roslyn 5.11 and 5.12. The memory-safety (unsafe evolution) surface is the only part still experimental,
and it belongs to C# 16 / `LangVersion=preview`, not to C# 15.

---

## 16. C# 15 language features (`docs/Language Feature Status.md` on `main`)

### Merged, shipping as C# 15

| feature | branch | state |
|---|---|---|
| Collection expression arguments | `collection-expression-arguments` | C# 15 |
| Unions | `Unions` | C# 15 |
| Non-virtual static interface members without DIM runtime support | `main` | C# 15 |
| `ExtendedLayoutAttribute` | `main` | Merged into 18.3 |
| Closed class hierarchies | `closed-class` | C# 15 |
| Extension indexers | `extensions` | C# 15 |
| Labeled `break`/`continue` | `labeled-break-and-continue` | C# 15 |

### Merged as preview, not part of C# 15

| feature | state |
|---|---|
| Unsafe evolution | Merged as preview feature into .NET 11p2 and VS 18.6 |
| Runtime Async | Main feature merged into `main` in preview |

### Still in progress at the top of the working set (not merged)

Dictionary expressions; Null-conditional await; Chained relational comparison;
Target-typed static member access; Relax modifier ordering; Compound assignment in initializers;
Extension members on typeless receivers; Runtime Async Streams; Extension constants;
Type Parameter Inference from Constraints.

### `learn.microsoft.com/dotnet/csharp/whats-new/csharp-15` (ms.date 08/14/2026)

Confirms the six user-facing C# 15 features and gives the syntax:

```csharp
// collection expression arguments
List<string> names = [with(capacity: values.Length * 2), .. values];
HashSet<string> set = [with(StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello"];

// unions
public union Pet(Cat, Dog, Bird);

// closed hierarchies
public closed record class GateState;

// extension indexers
extension(IEnumerable<int> sequence) { public int this[int index] => sequence.ElementAt(index); }

// labeled break and continue
outer: for (...) { for (...) { continue outer; break outer; } }
```

The page states that the runtime types `UnionAttribute` and `IUnion` arrived in **.NET 11 Preview 5**,
and that some of the unions proposal is not yet implemented. In the memory-safety section it states
that the pointer relaxations and `unsafe(expr)` require `LangVersion=preview` plus `AllowUnsafeBlocks`,
and that `safe` and `unsafe` currently have no effect on callers because the requires-unsafe member
model and the assembly opt-in are not available yet.

A closed class is implicitly `abstract`, cannot be combined with `sealed`, `static` or an explicit
`abstract`, and derivation is not transitive.

Caveat: this page still says "C# 15 is the latest C# preview release", which contradicts Roslyn `main`
where `MapSpecifiedToEffectiveVersion(Default) == CSharp15`. Roslyn `main` (read 2026-09-03) is the more
recent source; the docs page is dated 2026-08-14 and was written while Roslyn 5.10 was current.

---

## 17. Compiler behavioural breaking changes affecting parsing and binding

From `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` on `main`, the complete list:

| change | introduced in |
|---|---|
| Safe-context of a `Span`/`ReadOnlySpan` collection expression is now declaration-block, not function-member | VS 18.3 |
| Synthesizing a `ref readonly`-returning delegate now requires `System.Runtime.InteropServices.InAttribute`, else CS0518 | VS 18.3 |
| `ref readonly` local functions now require `InAttribute`, else CS0518 | VS 18.3 |
| `&&` / `||` with an interface-typed left operand and a `dynamic` right operand is now CS7083 | VS 18.3 |
| `nameof(this.X)` in an attribute is now disallowed | VS 18.3 and .NET 10.0.200 |
| Parsing of `with` inside a switch-expression arm: `(X.Y) when` is now a constant pattern followed by a `when` clause, not a cast of the identifier `when` | VS 18.4 |
| `with(...)` as a collection-expression element binds as construction arguments at LangVersion 15 or greater; use `@with` to call a method named `with` | VS 18.4 |
| Pointer types no longer require an unsafe context (C# 16); can introduce new CS0121 overload ambiguities | VS 18.7 |
| `safe` is a contextual keyword on member declarations (C# 16); escape as `@safe` | VS 18.9 |
| `unsafe` required for more members, for example an indexer with a pointer parameter, under langversion 16; mitigate with `unsafe(...)` | VS 18.9 |
| **`closed` is a contextual keyword in type declaration contexts.** A type or alias named `closed` without `@` gives CS9380; in member declaration contexts `closed` is a modifier, so `closed oldField;` now parses as an incomplete declaration and gives CS1519 | VS 18.10 |
| **`union` is a contextual keyword in type declaration contexts.** `union OldField;` now parses as a union declaration and gives CS9370 instead of declaring a field of type `union` | VS 18.10 |

The last two change how existing, otherwise valid source parses.

Diagnostic identifiers seen: CS9380 (`closed` used as an identifier), CS9370 (`union` used as an
identifier), CS9363 (unsafe required for more members), CS7083, CS0518, CS0121, CS1519.

---

## 18. Source URLs used

- https://raw.githubusercontent.com/dotnet/roslyn/main/eng/Versions.props (also `release/dev18.0`, `release/dev18.3`, `release/10.0.3xx`, `release/10.0.4xx`, `release/stable`, `release/insiders`, `release/dev-next`)
- https://raw.githubusercontent.com/dotnet/roslyn/main/eng/targets/TargetFrameworks.props (all branches above)
- https://raw.githubusercontent.com/dotnet/roslyn/main/docs/contributing/target-framework-strategy.md
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt (all branches)
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt (all branches)
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/VisualBasic/Portable/PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt
- PublicAPI files for src/Workspaces/{Core,CSharp}/Portable, src/Workspaces/Core/Desktop, src/Workspaces/MSBuild/Core, src/Workspaces/Remote/Core, src/Features/{Core,CSharp}/Portable, src/Scripting/{Core,CSharp}
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/LanguageVersion.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/Syntax.xml
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/SyntaxKindFacts.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/ParameterSyntax.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Compilation/ForEachStatementInfo.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs, IMethodSymbol.cs, TypeLayout.cs, WellKnownMemberNames.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/Operations/OperationInterfaces.xml and CommonConversion.cs
- https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/Text/SourceHashAlgorithm.cs
- https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md
- https://github.com/dotnet/roslyn/blob/main/docs/Breaking%20API%20Changes.md
- https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md
- https://github.com/dotnet/roslyn/blob/main/docs/features/ExtendedLayoutAttribute.md and pre-compilation-source-outputs.md
- https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md
- https://raw.githubusercontent.com/dotnet/sdk/main/eng/Versions.props and eng/Version.Details.xml
- https://github.com/dotnet/docs/blob/main/docs/csharp/whats-new/csharp-15.md
- https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support
- https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-history
- https://api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.csharp/index.json and the 5.0.0 / 5.3.0 / 5.6.0 / 5.9.0 nuspecs
- https://api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.common/5.0.0 and 5.9.0 nuspecs

## 19. Open questions

1. Whether .NET 11 GA ships Roslyn 5.12 / VS 18.12 exactly, or slips to 5.13 / 18.13. `dotnet/sdk`
   `main` at 11.0.100-rc.1 pins 5.12.0-1, which is strong evidence but not final.
2. Whether `docs/wiki/NuGet-packages.md` and the Learn `roslyn-version-support` page will be extended
   with the 5.1 through 5.12 rows before GA. Today both stop at 5.0.
3. Whether adding `params` to the `ImmutableArray<T>` analyzer-registration overloads can make any
   existing call site ambiguous against the pre-existing `params T[]` overload. I reasoned from the
   C# 13 params-collections better-function-member rules that the array overload still wins, but I did
   not compile a test.
4. How a union type's case types are surfaced structurally beyond `ITypeSymbol.UnionCaseTypes` (the
   nested `IUnionMembers` interface, the generated `Create` factories, and the
   `Value`/`HasValue`/`TryGetValue` members). `WellKnownMemberNames.UnionMembersInterfaceName` and
   `UnionFactoryMethodName` are internal, so the public contract for enumerating a union's members is
   `UnionCaseTypes` plus ordinary member enumeration. I did not find a document that pins down the
   emitted shape.
5. Whether `Microsoft.CodeAnalysis.Analyzers` really ships as a Roslyn-versioned prerelease at GA
   (`Microsoft.CodeAnalysis.Common` 5.9.0 depends on `5.9.0-1.26328.17`), or whether a stable
   `5.12.0` will be published.
6. Whether `Microsoft.Net.Compilers.Toolset` for .NET 11 still ships a `net472` `csc.exe` alongside the
   `net10.0` one. The csproj says yes (`$(NetRoslynSourceBuild);net472`), but I did not inspect a built
   package.
7. The exact release date and version of the Visual Studio build that first ships C# 15 as
   non-preview. Roslyn 5.11 is `release/insiders` as of 2026-09-03.
8. Whether `NetVS` moving from net8.0 to net10.0 means the Visual Studio out-of-process Roslyn
   analyzer host (ServiceHub) now runs on .NET 10 for all VS 18.9+ installations, or only for some
   components. The target-framework-strategy document says "private runtime components", which I read
   as the whole out-of-process host, but I did not confirm against a VS installation.
