# Terrain map: `Metalama.Premium`

Scope: the whole of `C:/src/Metalama-2027.0/Metalama.Premium/src/**`, plus that repository's
`Directory.Packages.props`, `Directory.Build.props`, `Directory.Build.targets` and `eng/**`.

Repository state when this map was made: branch `topic/2027.0/1829-durable-and-immutable-contracts`,
head `7d5ce94`. `MainVersion` is `2027.0.0-preview`, `global.json` pins the .NET 10.0.102 SDK, but the
target frameworks, the Roslyn variant set and `RoslynMaxVersion` are still those of 2026.1. Everything
below therefore describes a repository that has *not yet* absorbed PB-2027.0.

---

## 0. Executive summary

1. `Metalama.Premium` contains almost no syntax-level enumeration of C# constructs. The one genuine
   syntax rewriter that enumerates declaration forms is
   `ChangeVisibilityCodeAction.Rewriter`
   (`src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs:52-199`).
   Everything else consumes the core repository's reference index and code model.
2. The subsystem's language sensitivity is therefore concentrated in three *enum* switches over
   `Metalama.Framework.Code` enumerations: `DeclarationKind`, `Accessibility`, `MethodKind`.
   Two of them throw on an unknown value; one silently does nothing.
3. The subsystem's platform sensitivity is concentrated in six MSBuild files that repeat the literal
   strings `net8.0`, `net472`, `4.12.0` and `5.0.0`. These strings are matched by **exact string and
   exact `Version` equality** in the core loader
   (`Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:20,23`).
   A mismatch produces no diagnostic at all, only a trace log.
4. Issue #1913 is therefore mostly a mechanical rename plus one dangerous invariant: the literal
   `net8.0` in Premium's `MetalamaExtensionAssembly` items must become `net10.0` in the *same commit*
   in which the core repository's `TargetedAssemblyReference._targetFramework` is `net10.0`, otherwise
   every premium feature stops working with no error.

---

## 1. Repository shape

### 1.1 Shipped projects and their target frameworks

Source of truth: each `.csproj`; line numbers given.

| Project | Path | `TargetFramework(s)` | Line |
| --- | --- | --- | --- |
| `Metalama.Extensions.Architecture` | `src/Metalama.Extensions.Architecture/Metalama.Extensions.Architecture.csproj` | `netstandard2.0` | 6 |
| `Metalama.Extensions.CodeFixes` (packed as `Metalama.Extensions.CodeFixes.Redist`) | `src/Metalama.Extensions.CodeFixes/Metalama.Extensions.CodeFixes.csproj` | `netstandard2.0;net8.0` | 4 |
| `Metalama.Extensions.CodeFixes.DesignTime` | `src/Metalama.Extensions.CodeFixes.DesignTime/Metalama.Extensions.CodeFixes.DesignTime.csproj` | `net472;net8.0` | 6 |
| `Metalama.Extensions.CodeFixes.DesignTime.4.12.0` | `src/Metalama.Extensions.CodeFixes.DesignTime.4.12.0/…csproj` | inherited | — |
| `Metalama.Extensions.CodeFixes.Engine` | `src/Metalama.Extensions.CodeFixes.Engine/Metalama.Extensions.CodeFixes.Engine.csproj` | `net472;net8.0` | 6 |
| `Metalama.Extensions.CodeFixes.Engine.4.12.0` | `src/Metalama.Extensions.CodeFixes.Engine.4.12.0/…csproj` | inherited | — |
| `Metalama.Extensions.CodeFixes.Package` | `src/Metalama.Extensions.CodeFixes.Package/…csproj` | `netstandard2.0` | 4 |
| `Metalama.Extensions.CodeFixes.Package.Resources` | `src/Metalama.Extensions.CodeFixes.Package.Resources/…csproj` | `net8.0;net472` | 6 |
| `Metalama.Extensions.Validation` (packed as `…Validation.Redist`) | `src/Metalama.Extensions.Validation/Metalama.Extensions.Validation.csproj` | `netstandard2.0;net8.0` | 4 |
| `Metalama.Extensions.Validation.Engine` | `src/Metalama.Extensions.Validation.Engine/…csproj` | `net472;net8.0` | 7 |
| `Metalama.Extensions.Validation.Engine.4.12.0` | `src/Metalama.Extensions.Validation.Engine.4.12.0/…csproj` | inherited | — |
| `Metalama.Extensions.Validation.Package` | `src/Metalama.Extensions.Validation.Package/…csproj` | `netstandard2.0` | 4 |
| `Metalama.Extensions.Validation.Package.Resources` | `src/Metalama.Extensions.Validation.Package.Resources/…csproj` | `net8.0;net472` | 6 |
| `Metalama.Licensing` | `src/Metalama.Licensing/Metalama.Licensing.csproj` | `netstandard2.0` | 5 |
| `Metalama.Licensing.BuildTasks` | `src/Metalama.Licensing.BuildTasks/…csproj` | `net8.0;net472` | 4 |
| `Metalama.Patterns.Caching.Backends.Azure` | `src/Metalama.Patterns.Caching.Backends.Azure/…csproj` | `net471;net8.0;netstandard2.0` | 4 |
| `Metalama.Patterns.Caching.Backends.Redis` | `src/Metalama.Patterns.Caching.Backends.Redis/…csproj` | `net471;net8.0;netstandard2.0` | 4 |

Test projects:

| Project | `TargetFramework(s)` | Line |
| --- | --- | --- |
| `src/tests/Metalama.Extensions.Architecture.AspectTests` | `net8.0` | 6 |
| `src/tests/Metalama.Extensions.CodeFixes.AspectTests` | `netframework4.8;net8.0` | 11 |
| `src/tests/Metalama.Extensions.CodeFixes.UnitTests` | `netframework4.8;net8.0` | 6 |
| `src/tests/Metalama.Extensions.Validation.AspectTests` | `netframework4.8;net8.0` | 11 |
| `src/tests/Metalama.Extensions.Validation.UnitTests` | `netframework4.8;net8.0` | 6 |
| `src/tests/Metalama.Patterns.Caching.Backends.UnitTests` | `net472;net8.0` | 5 |
| `src/tests/Metalama.Patterns.Caching.LoadTests` | `net471;net8.0` | 5 |

Standalone (out-of-solution) test projects, all `net8.0` except where noted:
`src/tests/Standalone/AzureBackendLicenseFailure` (4), `CachingBackends` (4), `CodeFixes` (4),
`CodeFixesLicenseFailure` (4), `MemoryLeaks` (4), `RedisBackendLicenseFailure` (4),
`SerializationBackwardCompatibility/ProjectWithLatestMetalama` (5),
`SerializationBackwardCompatibility/ProjectWithMetalama20251` (5), `Validation` (4),
`ValidationLicenseFailure` (4); `Issue32827/Aspects` is `netstandard2.0` with `LangVersion 11.0` (4-5)
and `Issue32827/Test` is `netstandard2.1` with `LangVersion 11.0` (4-5).

Note that `net471`, not `net472`, is used by the two caching backend packages and by
`Metalama.Patterns.Caching.LoadTests`. PB-2027.0 sets the .NET Framework floor at 4.7.2, so those three
are below the baseline.

### 1.2 Roslyn variant machinery

Files:

- `eng/RoslynVersions/Latest.props` (7 lines). Line 2 imports `Roslyn.5.0.0.props` when
  `ThisRoslynVersion` is empty; line 5 defaults `ThisRoslynVersionNoPreview` to `ThisRoslynVersion`.
- `eng/RoslynVersions/Roslyn.4.12.0.props` (11 lines). Sets `ThisRoslynVersion=4.12.0` (line 3),
  `ThisRoslynVersionNoPreview=4.12.0` (line 5), `ThisRoslynVersionProjectSuffix=.4.12.0` (line 7),
  `DefineConstants` `ROSLYN_4_12_0;ROSLYN_4_4_0_OR_GREATER;ROSLYN_4_12_0_OR_EARLIER` (line 8),
  `SystemTextJsonVersion=8.0.6` (line 9), `SystemIOPipelinesVersion=9.0.0` (line 10).
- `eng/RoslynVersions/Roslyn.5.0.0.props` (17 lines). This is the *latest* variant today:
  `ThisRoslynVersion=$(RoslynApiMaxVersion)` (line 3), `ThisRoslynVersionNoPreview=5.0.0` (line 5),
  empty `ThisRoslynVersionProjectSuffix` (line 7),
  `DefineConstants` `ROSLYN_5_0_0;ROSLYN_4_4_0_OR_GREATER;ROSLYN_5_0_0_OR_GREATER` (line 8),
  `SystemTextJsonVersion=9.0.0` (line 9), `SystemMemoryVersion=4.6.3` (line 13),
  `SystemRuntimeCompilerServicesUnsafeVersion=6.1.2` (line 15).

Latent defect: `Roslyn.5.0.0.props:3` reads `$(RoslynApiMaxVersion)`, and that property is **not
defined anywhere in this repository** (a full grep for `RoslynApiMaxVersion` over `Metalama.Premium`
returns only that one line). `ThisRoslynVersion` therefore evaluates to the empty string in the latest
variant. Nothing breaks today because every consumer reads `ThisRoslynVersionNoPreview` or
`ThisRoslynVersionProjectSuffix` instead, but the property is dead and misleading. The core repository
defines `RoslynApiMaxVersion` in its own `Directory.Packages.props:28`.

Consumers of `ThisRoslynVersionNoPreview` / `ThisRoslynVersionProjectSuffix`:

- `src/Metalama.Extensions.CodeFixes.Engine/…csproj:9` `AssemblyName`, `:26` package reference
  `Metalama.Framework.Implementation.$(ThisRoslynVersionNoPreview)`.
- `src/Metalama.Extensions.Validation.Engine/…csproj:10` `AssemblyName`, `:22` the same package reference.
- `src/Metalama.Extensions.CodeFixes.DesignTime/…csproj:10` `AssemblyName`, `:21` project reference
  `…Engine$(ThisRoslynVersionProjectSuffix)`.
- `src/tests/Metalama.Extensions.CodeFixes.AspectTests/…csproj:17,51,52`.
- `src/tests/Metalama.Extensions.Validation.AspectTests/…csproj:18,52`.

Variant shim project pattern (this is the template a new variant must follow), from
`src/Metalama.Extensions.Validation.Engine.4.12.0/Metalama.Extensions.Validation.Engine.4.12.0.csproj`:

```xml
<Project ToolsVersion="Current">
  <PropertyGroup><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>
    <Compile Include="../Metalama.Extensions.Validation.Engine/**/*.cs" Exclude="…/bin/**;…/obj/**" />
    <Compile Remove="**/*" Condition="'$(FormattingCode)'=='True'" />
  </ItemGroup>
  <Import Project="../../eng/RoslynVersions/Roslyn.4.12.0.props" />
  <Import Project="../Metalama.Extensions.Validation.Engine/Metalama.Extensions.Validation.Engine.csproj" />
</Project>
```

There are three such shims: `…CodeFixes.Engine.4.12.0`, `…CodeFixes.DesignTime.4.12.0`,
`…Validation.Engine.4.12.0`. `Metalama.Extensions.Validation` has no design-time variant, because
validation exposes no code action.

No production `.cs` file in this repository contains a `#if ROSLYN_*` guard. A grep for `ROSLYN_` over
`**/*.cs` returns only two comment lines, in
`src/tests/Metalama.Extensions.CodeFixes.UnitTests/…csproj:13` and
`src/tests/Metalama.Extensions.Validation.UnitTests/…csproj:13`. The variants differ only by the
Roslyn assemblies they bind to, never by source. That is the same discipline the core repository
adopted for its own 5.0/5.10 pair (see `Metalama/eng/RoslynVersions/Roslyn.5.0.0.props`, whose comment
says "This variant defines no constant. No production source branches on the variant.").

### 1.3 Package versions

`Directory.Packages.props`:

- line 8: `<RoslynVersion Condition="…">5.0.0</RoslynVersion>`
- line 9: `<RoslynMaxVersion Condition="…">5.0.0</RoslynMaxVersion>`
- line 10: `SystemTextJsonLatestVersion = 9.0.10`
- line 11: `MicrosoftBclAsyncInterfacesLatestVersion = 9.0.10`
- line 22: `SystemMemoryVersion = 4.5.5` (default; overridden to 4.6.3 by the 5.0 variant)
- line 27: `SystemRuntimeCompilerServicesUnsafeVersion = 6.1.0` (overridden to 6.1.2 by the 5.0 variant)
- line 30: `Microsoft.CodeAnalysis.CSharp.Workspaces` at `$(RoslynVersion)`
- lines 38-39: `Metalama.Framework.Implementation.5.0.0` and `Metalama.Framework.Implementation.4.12.0`

`RoslynMaxVersion` is read only by tests:
`src/tests/Metalama.Extensions.Architecture.AspectTests/…csproj:35`
(`Microsoft.CodeAnalysis.Common`), `src/tests/Metalama.Extensions.CodeFixes.UnitTests/…csproj:27,28`
and `src/tests/Metalama.Extensions.Validation.UnitTests/…csproj:27,28`
(`Microsoft.CodeAnalysis.CSharp.Workspaces` and `…CSharp.Features`).

The comments at `Directory.Packages.props:18-27` and `:89-93` justify the out-of-band package caps
entirely in terms of the **Visual Studio 2022 17.14 `devenv.exe` binding redirects**. PB-2027.0 removes
Visual Studio 2022 from the supported set, so the whole justification lapses and those two properties
can collapse to a single value.

### 1.4 Build orchestration

`eng/src/Program.cs`:

- line 13: `using MetalamaDependencies = …MetalamaDependencies.V2027_0;` (already on 2027.0)
- line 24: `DotNetComponent( PreferredVersions.DotNetSdk.V_10_0, Sdk )`
- line 27: `DotNetComponent( PreferredVersions.DotNetSdk.V_8_0, Sdk )` — "The runtime is required by all tests"
- line 30: `DotNetComponent( PreferredVersions.DotNet.V_6_0, DotNetRuntime )` — "Required by some tests"
- line 33: `DotNetComponent( PreferredVersions.DotNet.V_9_0, DotNetRuntime )` — "Required by eng"
- line 35: `VisualStudioBuildToolsComponent( v17_14_15, [Microsoft.Component.MSBuild, Microsoft.NetCore.Component.SDK, Microsoft.Net.Component.4.7.2.TargetingPack, Microsoft.Net.Component.4.7.2.SDK] )`
- line 54: `DotNetSdkVersion( PreferredVersions.DotNetSdk.V_10_0 )`
- line 55: `MSBuildVersion = new Version( 17, 14 )`

`eng/docker/build.Dockerfile` installs, at lines 44, 48, 52 and 56: .NET runtime 6.0.36, SDK 8.0.417,
runtime 9.0.12, SDK 10.0.102.
`eng/docker/vs17.Dockerfile:33,36` installs Visual Studio Build Tools 17.14.15 from
`eng/docker-context/VisualStudio.17.14.15.Release.chman`.

`Metalama.Premium.LatestRoslyn.slnf` lists the 21 projects of the latest variant; the three
`*.4.12.0` shims are excluded from it and live under the solution folder "Other Roslyn Versions" in
`Metalama.Premium.sln`.

---

## 2. Question 1 — sensitivity to the set of C# language constructs

Ordered by how much a new construct would break.

### 2.1 `ChangeVisibilityCodeAction.Rewriter` — an explicit enumeration of declaration forms

`src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs`

Class `ChangeVisibilityCodeAction` (line 21), nested `private sealed class Rewriter : SafeSyntaxRewriter`
(line 52). It enumerates the declaration node types it knows about by overriding one `Visit*` method per
form:

| Line | Override | Node type |
| --- | --- | --- |
| 65 | `VisitBlock` | blocks the visit of bodies |
| 67 | `VisitEqualsValueClause` | blocks initialisers |
| 69 | `VisitArrowExpressionClause` | blocks expression bodies |
| 72 | `VisitClassDeclaration` | `ClassDeclarationSyntax` |
| 75 | `VisitRecordDeclaration` | `RecordDeclarationSyntax` |
| 78 | `VisitStructDeclaration` | `StructDeclarationSyntax` |
| 81 | `VisitFieldDeclaration` | `FieldDeclarationSyntax` |
| 84 | `VisitEventDeclaration` | `EventDeclarationSyntax` |
| 87 | `VisitEventFieldDeclaration` | `EventFieldDeclarationSyntax` |
| 90 | `VisitPropertyDeclaration` | `PropertyDeclarationSyntax` |
| 93 | `VisitEnumDeclaration` | `EnumDeclarationSyntax` |
| 96 | `VisitDelegateDeclaration` | `DelegateDeclarationSyntax` |
| 99 | `VisitConstructorDeclaration` | `ConstructorDeclarationSyntax` |
| 102 | `VisitMethodDeclaration` | `MethodDeclarationSyntax` |
| 105 | `VisitDestructorDeclaration` | `DestructorDeclarationSyntax` |
| 108 | `VisitAccessorDeclaration` | `AccessorDeclarationSyntax` |
| 111 | `VisitOperatorDeclaration` | `OperatorDeclarationSyntax` |
| 114 | `VisitConversionOperatorDeclaration` | `ConversionOperatorDeclarationSyntax` |

Already missing today, with no diagnostic:

- `InterfaceDeclarationSyntax`
- `IndexerDeclarationSyntax`
- the C# 14 extension block declaration

`SafeSyntaxRewriter`
(`Metalama/Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:35`)
derives from `CSharpSyntaxRewriter` and adds only exception wrapping and a recursion guard. An
un-overridden node falls to `CSharpSyntaxRewriter.DefaultVisit`, which recurses into the children and
rebuilds the node unchanged. The result is a silent no-op, covered in section 5.

Lines 124-189, `ChangeModifiers( SyntaxNode node, SyntaxTokenList modifiers )`, contain a second
enumeration: a `switch` over `Metalama.Framework.Code.Accessibility` (line 142) whose `default` throws
`AssertionFailedException` (line 177), and `IsAccessibilityModifier` (lines 191-199), a `switch` over
`SyntaxKind` listing `PrivateKeyword`, `PublicKeyword`, `InternalKeyword`, `ProtectedKeyword` and
returning `false` for everything else. A new *access* modifier keyword would need a case here; a new
*non-access* modifier (`closed`) is correctly preserved by the `_ => false` arm, because line 181 copies
every non-accessibility modifier through.

### 2.2 `ReferenceValidationContext.GetInboundGranularity` — a switch over `DeclarationKind`

`src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124-134`

```csharp
private static ReferenceGranularity GetInboundGranularity( DeclarationKind kind )
    => kind switch
    {
        DeclarationKind.Constructor or DeclarationKind.Event or DeclarationKind.Method or DeclarationKind.Field or DeclarationKind.Property
            or DeclarationKind.Indexer => ReferenceGranularity.Member,
        DeclarationKind.Compilation or DeclarationKind.AssemblyReference => ReferenceGranularity.Compilation,
        DeclarationKind.Namespace => ReferenceGranularity.Namespace,
        DeclarationKind.NamedType => ReferenceGranularity.Type,
        DeclarationKind.Parameter or DeclarationKind.TypeParameter or DeclarationKind.Attribute => ReferenceGranularity.ParameterOrAttribute,
        _ => throw new ArgumentOutOfRangeException( nameof(kind), $"Unexpected kind: '{kind}'" )
    };
```

The switch is not exhaustive over the current `DeclarationKind`
(`Metalama/Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs:18-119`). It omits `None`,
`ManagedResource`, `Type` and, notably, **`ExtensionBlock`**, which the C# 14 wave added at
`DeclarationKind.cs:118`. Validating an inbound reference whose destination is an extension block
therefore throws `ArgumentOutOfRangeException`. Called from `ReferenceValidationContext.Destination`
(line 57), which is reached from every reference validator.

A new `DeclarationKind` for C# 15 (a union type is likely to be a `NamedType`, so this may not fire;
an extension-block indexer would be a `DeclarationKind.Indexer` whose containing declaration is an
extension block) must be added here.

### 2.3 `ReferenceEnd.GetDeclarationOfGranularity` — a switch over `ReferenceGranularity`

`src/Metalama.Extensions.Validation/ReferenceEnd.cs:150-178`. Switch at line 160, `default` throws
`ArgumentOutOfRangeException` (line 176). Not construct-sensitive itself, but its `Namespace`, `Type`,
`TopLevelType` and `Member` arms delegate to
`Metalama.Framework.Code.DeclarationExtensions.GetClosestNamedType`,
`GetTopmostNamedType` and `GetClosestMemberOrNamedType`
(`Metalama/Metalama.Framework/src/Metalama.Framework/Code/DeclarationExtensions.cs:189, 204, 237`), and
each arm throws `InvalidOperationException` when those return `null`. That is the point at which an
unmapped containment shape (a member of an extension block, a member of a union) surfaces.

The `Type` and `TopLevelType` arms also cast to `INamedType` (lines 119, 125). `TypeKind.Extension`
already exists in the core (`…/Metalama.Framework.Engine/CodeModel/Source/ExtensionBlockImpl.cs:30`),
and an `IExtensionBlock` is not an `INamedType`, so any path that reaches an extension block here
throws `InvalidCastException`.

### 2.4 The `MethodKind` switches in the validation query sources

Two near-identical switches translate a validator placed on an accessor into a validator on the
declaring member, and choose which `ReferenceKinds` it should watch:

- `src/Metalama.Extensions.Validation.Engine/Queries/ReferenceValidatorQuerySource.cs:56-73`
  — `MethodKind.PropertyGet` → `ReferenceKinds.Default | ReferenceKinds.Invocation` (58-59);
  `MethodKind.PropertySet` → `ReferenceKinds.Assignment` (63-64);
  `MethodKind.EventAdd`, `MethodKind.EventRemove` → `ReferenceKinds.Assignment` (68-70).
- `src/Metalama.Extensions.Validation.Engine/Queries/DynamicReferenceValidatorQuerySource.cs:53-67`
  — same shape, `PropertyGet` → `ReferenceKinds.Default` (55-56);
  `PropertySet`, `EventAdd`, `EventRemove` → `ReferenceKinds.Assignment` (60-63).

Neither switch has a `default` arm that reports anything, and both are `switch` statements rather than
expressions, so an unlisted `MethodKind` simply falls through to the code after the switch. Neither
handles indexer accessors distinctly, nor `MethodKind.EventRaise`.

### 2.5 `Accessibility` enumerations

- `src/Metalama.Extensions.CodeFixes/CodeFixFactory.cs:103-112` — `ChangeAccessibility` maps
  `Accessibility` to the display string used in the code-fix title; `default` throws
  `ArgumentOutOfRangeException` (line 111).
- `src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs:155, 159, 165, 171, 173` — the
  "which declarations are part of the internal surface" enumeration inside `VerifyInternalsAccess`
  (lines 130-175).
- `src/Metalama.Extensions.Architecture/Aspects/InternalsUsageValidationAttribute.cs:34, 38, 40, 48`.
- `src/Metalama.Extensions.Architecture/Aspects/ExperimentalAttribute.cs:62`.
- `src/Metalama.Extensions.Architecture/Predicates/HasFamilyAccessPredicate.cs:26` — matches
  `IMemberOrNamedType { Accessibility: Accessibility.Protected or Accessibility.ProtectedInternal }`.

### 2.6 The internal-surface enumeration (a member-shape enumeration, not a syntax one)

`src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs:152-174` and, in duplicate,
`src/Metalama.Extensions.Architecture/Aspects/InternalsUsageValidationAttribute.cs:144-152`. Both
enumerate exactly three places an internal API can hide:

1. internal types (`ArchitectureExtensions.cs:158-160`);
2. internal members of public types, via `t.Members()` (`:163-166`);
3. internal accessors of public *properties*, via `t.Properties` then `p.Accessors` (`:169-174`).

`t.Indexers` is absent from both copies, so an internal accessor of a public indexer is not validated.
Any new member-carrying construct (an extension block; a union's cases) has to be added to this list
explicitly, and the failure mode is a rule that quietly stops firing.

### 2.7 `TypeKind`

`src/Metalama.Extensions.Architecture/Aspects/InternalOnlyImplementAttribute.cs:110` —
`builder.MustSatisfy( type => type.TypeKind == TypeKind.Interface, … )`. This is the only `TypeKind`
comparison in the repository. It is a positive test, so a new `TypeKind` makes the aspect ineligible
rather than misbehaving, which is the safe direction.

### 2.8 `SymbolKind`

`src/Metalama.Extensions.CodeFixes.DesignTime/CodeFixService.cs:207-231` — a `switch` on the symbol
returned by `semanticModel.GetDeclaredSymbol( node )` (line 205):
`null` plus `CompilationUnitSyntax` → the assembly (210-216); `null` otherwise → no refactoring (219);
`{ Kind: SymbolKind.Alias }` → none (222-224); `{ Kind: SymbolKind.Namespace }` → the assembly
(226-230). Everything else is passed to `pipeline.GetEligibleAspects` (line 236). This is generic over
declaration kinds and needs no change for a new declaration form, provided Roslyn returns a symbol.

### 2.9 `SyntaxKind` outside the visibility rewriter

`src/Metalama.Extensions.CodeFixes.Engine/Implementations/AddAttributeCodeAction.cs:52`:

```csharp
case { SyntaxKind: SyntaxKind.VariableDeclarator } and VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variableDeclaration }:
    originalNode = variableDeclaration.Parent!;
```

This is the single node-shape special case in `AddAttributeCodeAction`; everything else is delegated to
`generationContext.SyntaxGenerator.AddAttribute` in the core (line 62).

`src/Metalama.Extensions.CodeFixes.Engine/Implementations/RemoveAttributeCodeAction.RemoveAttributeRewriter.cs`
overrides only `VisitAttribute` (line 94) and `VisitAttributeList` (line 108), so it is
construct-agnostic.

`src/Metalama.Extensions.Validation.Engine/ReferenceValidationContextImpl.cs:66-71` switches on
`SyntaxNode` versus `SyntaxToken` to compute a diagnostic location, delegating to
`Metalama/Metalama.Framework/src/Metalama.Framework.Sdk/Diagnostics/DiagnosticLocationHelper.cs:54-109`,
whose own switch is a fifteen-case enumeration of declaration node types ending in
`default: return node.GetLocation()`. `UnionDeclarationSyntax` derives from `TypeDeclarationSyntax`,
hence from `BaseTypeDeclarationSyntax` (see
`Metalama/eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954`), so it is already served correctly
by the `BaseTypeDeclarationSyntax` case at line 79.

### 2.10 Where the construct enumeration actually lives: the core reference index

`src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:43-48, 68-79` constructs an
`InboundReferenceIndexBuilder` and calls `IndexSemanticModel` / `IndexSyntaxTree`. Both types belong to
`Metalama.Framework.Engine.ReferenceGraph` in the core repository. Every mapping from a C# construct to
a `ReferenceKinds` flag is made there, not here.

The consequence for the C# 15 work is a division of labour:

- the core repository teaches the index about `union`, `unsafe(expr)`, `with(...)` elements and labeled
  `break`/`continue`;
- this repository changes nothing in the engine, but its expected test outputs change (section 3.3),
  and any *new* `ReferenceKinds` flag must be surfaced in the documentation of
  `src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs` and
  `src/Metalama.Extensions.Validation/ReferenceValidationQueryExtensions.cs`.

`ReferenceKinds` (`Metalama/Metalama.Framework/src/Metalama.Framework/Code/ReferenceKinds.cs:16-166`)
declares `All = -1` (line 23), so a newly added flag is automatically included in every
`ReferenceKinds.All` default. That is additive-safe, and it is the reason the C# 14 wave needed no
change in the twenty-odd `ReferenceKinds referenceKinds = ReferenceKinds.All` parameter defaults across
`ArchitectureExtensions.cs` (lines 38, 49, 61, 69, 89, 102, 116, 124, 134, 185, 197, 210, 218, 230, 242,
255, 263).

---

## 3. Question 2 — sensitivity to runtime, SDK, Roslyn and host

### 3.1 The extension-assembly manifests: exact string and exact version matching

Four files repeat the same table. They are the most consequential platform-sensitive artefacts in this
repository.

`src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props` (21 lines):

| Line | Item | `TargetFramework` | `TargetRoslynVersion` |
| --- | --- | --- | --- |
| 5 | `MetalamaExtensionAssembly` `…CodeFixes.dll` | `net472` | — |
| 6 | `MetalamaExtensionAssembly` `…CodeFixes.dll` | `net8.0` | — |
| 9 | `…CodeFixes.Engine.5.0.0.dll` | `net472` | `5.0.0` |
| 10 | `…CodeFixes.Engine.5.0.0.dll` | `net8.0` | `5.0.0` |
| 11 | `…CodeFixes.Engine.4.12.0.dll` | `net472` | `4.12.0` |
| 12 | `…CodeFixes.Engine.4.12.0.dll` | `net8.0` | `4.12.0` |
| 15 | `MetalamaDesignTimeExtensionAssembly` `…DesignTime.5.0.0.dll` | `net472` | `5.0.0` |
| 16 | `…DesignTime.5.0.0.dll` | `net8.0` | `5.0.0` |
| 17 | `…DesignTime.4.12.0.dll` | `net472` | `4.12.0` |
| 18 | `…DesignTime.4.12.0.dll` | `net8.0` | `4.12.0` |
| 20 | `MetalamaPremiumComponent` `Metalama.Extensions.CodeFixes` | — | — |

`src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props` (16 lines) is
the same without the design-time rows: lines 5, 6, 9, 10, 11, 12, and `MetalamaPremiumComponent` at 14.

`src/Metalama.Extensions.CodeFixes/MetalamaExtensionAssemblies.props` (23 lines) and
`src/Metalama.Extensions.Validation/MetalamaExtensionAssemblies.props` (18 lines) mirror those tables
for in-repository `ProjectReference` consumption; the comment on line 8 of each says so explicitly
("These mirror Metalama.Extensions.*.props"). `MetalamaExtensionAssemblies.props` is imported by
`src/Metalama.Extensions.Architecture/Metalama.Extensions.Architecture.csproj:3` and
`src/tests/Metalama.Extensions.Architecture.AspectTests/…csproj:3`.

How the values are consumed, in the core repository:

`Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:71-72`
flattens the items to `%(FullPath)|%(TargetFramework)|%(TargetRoslynVersion)`, and
`Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs`
decides:

```csharp
private static readonly string _targetFramework =
    RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net10.0";   // line 20

public bool SatisfiesCurrentProcess
    => (this.TargetRoslynVersion == null || this.TargetRoslynVersion.Equals( RoslynApiVersion.Current.ToVersion() ))         // line 23
       && (this.TargetFramework == null || this.TargetFramework == _targetFramework);                                        // line 24
```

Two exact comparisons. The core repository's `develop/2027.0` already carries the literal `net10.0` at
line 20, and `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31`
repeats it. Premium still emits `net8.0`. As of today, in that combination, **no premium extension
assembly satisfies the current process on .NET**, and `ExtensionLoaderBase.GetExtensionAssemblyPaths`
(lines 29-38) returns an empty sequence with only a trace log at line 33.

`TargetRoslynVersion` is compared with `Version.Equals`, that is, exact equality, not a floor. Under
PB-2027.0 `RoslynApiVersion.Current` is `5.0` in the Roslyn 5.0 payload and `5.10` in the latest
payload, so a `TargetRoslynVersion` of `4.12.0` matches nothing that ships.

### 3.2 The packaging copy lists

`src/Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj:47-64`, target
`_AddAssembliesToOutput`. Ten `TfmSpecificPackageFile` items at lines 53-62, each naming both a build
output directory (`bin/$(Configuration)/net472` or `…/net8.0`) and a package path
(`metalama/net472`, `metalama/net8.0`), for `…CodeFixes.dll`, `…Engine.5.0.0.dll`,
`…Engine.4.12.0.dll`, `…DesignTime.5.0.0.dll`, `…DesignTime.4.12.0.dll`.

`src/Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj:40-53`, six
items at lines 46-51.

These are `Include` globs with no existence check. A path that names a directory the build no longer
produces contributes nothing and raises no error, exactly the hazard
`Metalama/Metalama.Framework/docs/platform-support.md` describes for `CoreAssemblyToEmbed`.

`src/Metalama.Extensions.CodeFixes.Package.Resources/…csproj:26-30` and
`src/Metalama.Extensions.Validation.Package.Resources/…csproj:26-27` list the per-variant project
references that produce those files. Both carry the comment "We target the two frameworks for which
csc.exe is built" at line 5.

### 3.3 The MSBuild-task runtime selection

`src/Metalama.Licensing/build/Metalama.Licensing.targets`:

```
11:  <_MetalamaLicensingBuildRuntimeVersion>$([System.Environment]::Version)</_MetalamaLicensingBuildRuntimeVersion>
12:  <_MetalamaLicensingTasksDirectoryName Condition="'$(MSBuildRuntimeType)' == 'Core'">net8.0</_MetalamaLicensingTasksDirectoryName>
13:  <_MetalamaLicensingTasksDirectoryName Condition="'$(MSBuildRuntimeType)' != 'Core'">net472</_MetalamaLicensingTasksDirectoryName>
14:  <MetalamaLicensingTasksAssembly>$(MSBuildThisFileDirectory)..\tasks\$(_MetalamaLicensingTasksDirectoryName)\Metalama.Licensing.BuildTasks.dll</MetalamaLicensingTasksAssembly>
18:  <UsingTask TaskName="Metalama.Licensing.BuildTasks.VerifyMetalamaLicense" AssemblyFile="$(MetalamaLicensingTasksAssembly)"/>
```

Line 11 computes the host runtime version and **never uses it**. The selection at line 12 has no version
guard and no equivalent of the `LAMA0622` diagnostic that `Metalama.Compiler` uses for the same
decision. This is the same defect that `platform-support.md` records for
`buildTransitive/Metalama.Compiler.Sdk.props` in the `Metalama.Compiler` repository: below the SDK
floor, the build fails with a raw assembly-load error rather than a diagnostic.

The task assembly is produced by `src/Metalama.Licensing.BuildTasks/…csproj` (`net8.0;net472`, line 4)
and packed by `src/Metalama.Licensing/Metalama.Licensing.csproj:29-30` into `tasks/net472` and
`tasks/net8.0`. Those three literals must move together.

`src/Metalama.Licensing.BuildTasks/…csproj` also carries .NET-Framework-only references:
`Microsoft.IO.Redist` under `'$(TargetFramework)'=='net472'` (line 27) and the ILRepack input
`System.IO.Hashing.dll` under the same condition (line 42). The ILMerge target (lines 74-100) merges
`Metalama.Backstage.dll`, `Metalama.Testing.Hooks.dll`, `Jetbrains.*.dll` and
`System.Threading.AccessControl.dll`, and resolves `netstandard.library/$(NETStandardLibraryPackageVersion)`
at line 87.

### 3.4 Runtime-version conditional compilation in the caching backends

Seven `#if` blocks, all in code that ships on `net471`, `net8.0` and `netstandard2.0`:

| File | Line | Guard | What differs |
| --- | --- | --- | --- |
| `src/Metalama.Patterns.Caching.Backends.Azure/AzureCacheSynchronizer.cs` | 161 | `NET8_0_OR_GREATER` | `CancellationTokenSource.CancelAsync()` versus `Cancel()` |
| `src/Metalama.Patterns.Caching.Backends.Redis/LimitedConcurrencyTaskQueue.cs` | 136 | `NET6_0_OR_GREATER` | `Task.WhenAll(...).WaitAsync( cancellationToken )` |
| `src/Metalama.Patterns.Caching.Backends.Redis/RedisCacheDependencyGarbageCollector.cs` | 255 | `NET8_0_OR_GREATER` | `CancelAsync()` versus `Cancel()` |
| `src/Metalama.Patterns.Caching.Backends.Redis/RedisCachingBackend.cs` | 236 | `NET6_0_OR_GREATER` | `Guid.TryParse` on a span versus on a string |
| `src/Metalama.Patterns.Caching.Backends.Redis/RedisCachingBackendConfiguration.cs` | 126 | `NETFRAMEWORK \|\| NETSTANDARD` | `IndexOf` versus `Contains( …, StringComparison )` |
| `src/Metalama.Patterns.Caching.Backends.Redis/RedisNotificationQueueProcessor.cs` | 610 | `NETCOREAPP` | `await using` versus `using` |
| `src/Metalama.Patterns.Caching.Backends.Redis/ShortKey.cs` | 39, 57 | `NET6_0_OR_GREATER` | `string.Replace` with `StringComparison` |
| `src/Metalama.Patterns.Caching.Backends.Redis/ShortKeyAndVersion.cs` | 51 | `NET6_0_OR_GREATER` | `string.GetHashCode( StringComparison )` |

None of these becomes removable when `net8.0` is replaced by `net10.0`, because the `#else` arm still
serves `netstandard2.0` and `net471`. That is a departure from the "remove always-true guards" rule the
Roslyn 4.8 drop applied (see section 4.2), and it should be stated in the #1913 pull request so the
reviewer does not ask for their removal.

Two test-side guards: `src/tests/Metalama.Patterns.Caching.LoadTests/StringExtensions.cs:14`
(`NET5_0_OR_GREATER`) and
`src/tests/Metalama.Patterns.Caching.Backends.UnitTests/Backends/Single/RedisCacheBackendWithGarbageCollectorTests.cs:5`
(`!(RELEASE && NETFRAMEWORK)`).

### 3.5 Host (integrated development environment) sensitivity

`src/Metalama.Extensions.CodeFixes.DesignTime/CodeFixesDesignTimeExtension.cs:328-357` branches on
`DesignTimeProcessKind`:

- `!= VsUserProcess` (line 331) registers the in-process `CodeFixService` (336);
- `== VsUserProcess` registers `UserProcessCodeFixService`, an RPC client (343);
- `== VsAnalysisProcess` (line 346) adds the RPC service (348);
- otherwise it adds `PremiumCodeFixProviderExtension` and `PremiumCodeRefactoringProviderExtension`
  (352-353).

`DesignTimeProcessKind` has exactly three members —
`Default`, `VsUserProcess`, `VsAnalysisProcess` —
(`Metalama/Metalama.Framework/src/Metalama.Framework.DesignTime/Services/DesignTimeServiceProviderFactory.cs:26-44`).
Rider and the Visual Studio Code C# Dev Kit are `Default`: single process, so both the service and the
providers are registered locally. A new host that split its processes would need a fourth member and a
change here.

The cross-process code-fix plumbing lives in `src/Metalama.Extensions.CodeFixes.DesignTime/Rpc/`
(`CodeFixRpcClient.cs`, `CodeFixRpcService.cs`, `CodeFixRpcService.Api.cs`, `CodeFixRpcServiceFactory.cs`,
`ICodeFixRpcApi.cs`, `ICodeFixService.cs`, `UserProcessCodeFixService.cs`), and only Visual Studio uses
it. `src/Metalama.Extensions.CodeFixes.DesignTime/CodeActionPayloadTypes.cs` is the name-to-type
registry used to materialise a wire payload; `CodeFixService.ExecuteCodeActionAsync`
(`CodeFixService.cs:38-…`) soft-fails to `CodeActionResult.Empty` on an unknown payload type, with the
comment at lines 44-47 explaining that throwing would disable the provider for the whole session.

`src/Metalama.Extensions.CodeFixes.DesignTime/LamaCodeAction.cs:252` derives from Roslyn's
`Microsoft.CodeAnalysis.CodeActions.CodeAction`, overriding `ComputeOperationsAsync` (292),
`ComputePreviewOperationsAsync` (297) and `GetChangedDocumentAsync` (302). Any change to Roslyn's
`CodeAction` contract lands here.

There is a host-specific regression test:
`src/tests/Metalama.Extensions.CodeFixes.UnitTests/CodeFixTests.cs:96` `RiderHiddenDiagnosticCodeFixTest`.
`VsCodeFixProviderTests.cs` and `VsCodeRefactoringProviderTests.cs` in the same directory exercise the
Visual Studio split-process path.

### 3.6 Assembly-name coupling that fails loudly

`src/Metalama.Extensions.Validation/Metalama.Extensions.Validation.csproj:16-17` and
`src/Metalama.Extensions.CodeFixes/Metalama.Extensions.CodeFixes.csproj:44-47` hard-code
`InternalsVisibleTo` for `…Engine.5.0.0`, `…Engine.4.12.0`, `…DesignTime.5.0.0`, `…DesignTime.4.12.0`.
Renaming a variant without updating these produces a compile error, which is the desired behaviour.

### 3.7 The template language version

`Directory.Build.props:19-20`:

```xml
<!-- Metalama.Extensions and Metalama.Patterns must be compatible with VS 2022,
     so we can't use C# 14 in templates and build-time code. -->
<MetalamaTemplateLanguageVersion>13.0</MetalamaTemplateLanguageVersion>
```

The core repository's own `Directory.Build.props:16` sets the same property to `14.0`. Under PB-2027.0,
Visual Studio 2022 leaves the supported set, so the stated reason no longer holds and this value can
follow the core. The property is read by
`Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:153`, is
declared compiler-visible at
`Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.CompilerVisibleProperties.props:32`,
and drives the `LAMA0282` diagnostic in
`Metalama/…/Templating/TemplatingDiagnosticDescriptors.cs:623`.

`Directory.Build.props:16` also sets `MetalamaAvoidLockingExtensionAssemblies` to `True`.

### 3.8 Test-side language and Roslyn pivots

- `src/tests/Metalama.Extensions.Validation.UnitTests/DesignTime/ValidatorTests.cs:141, 156` parse test
  code with `SupportedCSharpVersions.DefaultParseOptions`. That constant follows
  `SupportedCSharpVersions.Latest` in the core, so enabling C# 15 changes what these two tests parse
  without any edit here.
- `src/tests/Metalama.Extensions.CodeFixes.UnitTests/…csproj:10` and
  `src/tests/Metalama.Extensions.Validation.UnitTests/…csproj:10` set `<LangVersion>preview</LangVersion>`,
  with the comment at line 12: "There is intentionally no override of LangVersion for previous Roslyn
  tests as compilations created by test helpers have correct language version set."
- `src/tests/Metalama.Extensions.CodeFixes.AspectTests/…csproj:18` and
  `src/tests/Metalama.Extensions.Validation.AspectTests/…csproj:19` honour
  `<LangVersion Condition="'$(LangVersionOverride)'!=''">$(LangVersionOverride)</LangVersion>`, which is
  how the build harness runs the same aspect tests at a pinned language version.

---

## 4. Question 3 — how the previous wave was absorbed

### 4.1 The C# 14 language wave (#1034 … #1160) left three traces here

A search of the whole `Metalama.Premium` history for those issue numbers, for "C# 14", "extension
block", "extension member", "field keyword", "partial event" and "partial constructor" returns nothing.
The wave was absorbed in the core repository and reached Premium only as consequences:

1. **A deliberate opt-out for compile-time code.** `Directory.Build.props:19-20` pins
   `MetalamaTemplateLanguageVersion` to `13.0` with an explicit rationale. Premium's templates and
   build-time code stayed on C# 13 for the whole C# 14 wave. This is the *pattern* for a wave: the core
   moves, and Premium declares a lower ceiling until the platform baseline allows it to follow.
2. **Adoption of `field` in run-time library code only.** The `field` contextual keyword (C# 14) is used
   at
   `src/Metalama.Patterns.Caching.Backends.Redis/DependenciesRedisCachingBackend.CleanUp.cs:24`,
   `src/Metalama.Patterns.Caching.Backends.Redis/RedisCacheSynchronizer.cs:28`,
   `src/Metalama.Patterns.Caching.Backends.Redis/RedisCachingBackend.cs:47, 52` and
   `src/Metalama.Patterns.Caching.Backends.Redis/RedisCachingBackendConfiguration.cs:136`.
   All five are in the caching backends, which are ordinary libraries compiled by the .NET SDK at its
   default language version, never by the Metalama template compiler. The template ceiling of point 1
   does not apply to them, so the new feature was adopted there and nowhere else.
3. **Enum members consumed without a corresponding switch update.** `DeclarationKind.ExtensionBlock`
   and `TypeKind.Extension` were added in the core. No switch in Premium was extended to cover them.
   `ReferenceValidationContext.GetInboundGranularity` (section 2.2) still throws on
   `DeclarationKind.ExtensionBlock`. This is the wave's unfinished business, and it is a warning for
   C# 15: additions to core enumerations do not announce themselves at the Premium build.

### 4.2 The Roslyn-variant wave is the pattern that #1913 will follow

Two commits define it.

`c9244ce` (merge of pull request #39, `topic/2026.0/1194-multi-roslyn-version-extensions`,
2025-11-18, "Code fixes and design-time validation do not work in VS 2022") **introduced** the whole
variant mechanism, in 38 files: it created `eng/RoslynVersions/Latest.props`, `Roslyn.4.8.0.props`,
`Roslyn.4.12.0.props`, `Roslyn.5.0.0.props`; created the six shim projects; created
`Metalama.Premium.LatestRoslyn.slnf`; created both `MetalamaExtensionAssemblies.props` files (deleting
`ProjectReferenceSupplements.props`); and rewrote the two `build/*.props` and both `Package.csproj`
copy lists.

`77e53e9` ("Sync dependency versions with Metalama #1603; drop Roslyn 4.8 variant", 2026-04-27,
23 files) **removed** a variant. Its own summary states the recipe:

> - Drop the Roslyn 4.8 build variant and Metalama.Framework.Implementation.4.8.0 reference (no
>   in-MS-support host below Roslyn 4.12 remains; Metalama no longer produces the 4.8.0 implementation
>   package)
> - Remove always-true ROSLYN_4_8_0_OR_GREATER and ROSLYN_4_12_0_OR_GREATER guards from DefineConstants
>   and the AllReferences.cs test file

and it touched, in order: `Directory.Packages.props`, `Metalama.Premium.sln` (63 lines removed),
`eng/RoslynVersions/Roslyn.4.12.0.props`, deleted `eng/RoslynVersions/Roslyn.4.8.0.props`,
`eng/RoslynVersions/Roslyn.5.0.0.props`, deleted the two `*.4.8.0.csproj` shims, both `Engine.csproj`
files, both `Package.Resources.csproj` files, both `Package.csproj` files, both `build/*.props` files,
both `Redist` `.csproj` files, both `MetalamaExtensionAssemblies.props` files,
`Metalama.Licensing.BuildTasks.csproj`, and the test file
`src/tests/Metalama.Extensions.Validation.AspectTests/AllReferences.cs`.

The commit message also records that the acceptance criterion was "dotnet restore + Build.ps1 build are
warning-free across all top-level configurations", and that the `System.Memory` /
`System.Runtime.CompilerServices.Unsafe` pinning exists only to resolve `MSB3277` against the Roslyn
4.12 dependency closure.

`Metalama.Premium.LatestRoslyn.slnf` was **not** touched by `77e53e9`, because the removed variant was
never in it. Adding a Roslyn 5.10 variant likewise leaves it alone; renaming the latest one does not.

### 4.3 The shape of the test evidence for a language wave

`src/tests/Metalama.Extensions.Validation.AspectTests/AllReferences.cs` (248 lines) is the single file
that enumerates C# constructs for their reference-detection behaviour. Its aspect reports, for every
inbound reference, the tuple
`(ReferenceKinds, referencing DeclarationKind, referencing declaration, referenced DeclarationKind,
referenced declaration, SyntaxKind)` (lines 23-41), and the expected output
`AllReferences.t.cs` (62 lines) is a list of warnings such as

```
// Warning MY001 on `new ValidatedClass()`: `Reference constraint of type 'ObjectCreation' to type
// 'ValidatedClass' from method 'DerivedClass.Method(…)' (SyntaxKind=ObjectCreationExpression).`
```

The constructs currently covered, by line in `AllReferences.cs`: explicit interface implementation
(83-94), attribute on a field (103-104), field type (107), `typeof` in an initialiser (110),
constructors and `base(...)` (113-115), `override` and `base.` call (118-122), parameters and return
types (125), target-typed `new()` and `new T()` (128-129), field read (132), assignment and compound
assignment (135-137), `typeof` argument (140), `nameof` (143-144), event invocation (147-148),
event `+=` / `-=` (151-154), array creation and collection expressions (157-160), casts and `as`
(163-164), `is` and `is` with a property pattern (167-168), automatic properties (174), overridden
property accessors (177-184), field-like and explicit events (187-199), local variables (207), generic
type and method type arguments (213-216), derived generic types (220-222), attributes on every target
(224-242), a positional record (244), a primary-constructor class (246) and a primary-constructor
struct (248).

Absent, and therefore the shopping list for C# 15: an extension block, an extension-block indexer, a
`union` declaration, an `unsafe(expr)` expression, a `with(...)` collection element, a labeled
`break`/`continue`, and a `closed` type.

`AllReferences_Derived.cs` (72 lines) and `AllReferences_NotDerived.cs` (68 lines) are the
`IncludeDerivedTypes` variants. `src/tests/Metalama.Extensions.Validation.UnitTests/SideBySideVersionTests.cs`
covers the cross-project (transitive) path.

---

## 5. Question 4 — the extension points a new construct would touch

### A new kind of type declaration (`union`)

1. `ChangeVisibilityCodeAction.Rewriter` needs `public override SyntaxNode VisitUnionDeclaration(
   UnionDeclarationSyntax node )` at
   `src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs`, beside the
   class/record/struct trio at lines 72-79. Because the variant assemblies compile against two Roslyn
   versions and only the latest defines `UnionDeclarationSyntax`, the override must be guarded by a
   preprocessor symbol — and this repository defines none today (section 1.2). Adding one is itself a
   design decision: the core repository chose `ROSLYN_5_10_0_OR_GREATER`, defined only by the latest
   variant, and used it only in tests.
2. `ReferenceValidationContext.GetInboundGranularity`
   (`src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124`) if the core gives a union a
   `DeclarationKind` other than `NamedType`.
3. `ReferenceEnd.Type` and `ReferenceEnd.TopLevelType`
   (`src/Metalama.Extensions.Validation/ReferenceEnd.cs:119, 125`) cast to `INamedType`; a union that is
   not an `INamedType` breaks them.
4. `InternalOnlyImplementAttribute.BuildEligibility`
   (`src/Metalama.Extensions.Architecture/Aspects/InternalOnlyImplementAttribute.cs:110`) if a union may
   be implemented.
5. `VerifyInternalsAccess` (`src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs:152-174`)
   and `InternalsUsageValidationAttribute.BuildAspect`
   (`…/Aspects/InternalsUsageValidationAttribute.cs:144-152`), if a union carries members reachable by
   `Members()`.
6. `src/tests/Metalama.Extensions.Validation.AspectTests/AllReferences.cs` plus its `.t.cs`.

### A new modifier (`closed`)

1. `ChangeVisibilityCodeAction.IsAccessibilityModifier`
   (`…/ChangeVisibilityCodeAction.cs:191-199`) returns `false` for it, and line 181 copies it through
   unchanged; that is the correct behaviour and needs no edit.
2. `ChangeVisibilityCodeAction.ChangeModifiers` (line 124) rebuilds the token list by placing the new
   accessibility keywords first and then appending the rest. If `closed` has an ordering constraint
   relative to the accessibility keywords, this produces syntactically wrong output.
3. Nothing else. `closed` adds no `Accessibility` value, so
   `src/Metalama.Extensions.CodeFixes/CodeFixFactory.cs:103` and the `Accessibility` switch at
   `ChangeVisibilityCodeAction.cs:142` are unaffected.

### A new expression form (`unsafe(expr)`)

Nothing in this repository walks expressions. The whole burden is on the core's
`InboundReferenceIndexBuilder`, invoked at
`src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:48` and `:75`. What changes here
is only test evidence: `AllReferences.cs` and `AllReferences.t.cs`. Note that both aspect test projects
already set `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
(`…CodeFixes.AspectTests/…csproj:13`, `…Validation.AspectTests/…csproj:13`), so an `unsafe(expr)` test
compiles without a project change.

### A new collection-expression element (`with(...)`)

Same as above: no walker here. `ReferenceKinds.ObjectCreation`, whose documentation at
`Metalama/Metalama.Framework/src/Metalama.Framework/Code/ReferenceKinds.cs:56-58` already says "In case
of collection expression, the reference points to the type", is the flag most likely to gain a
sibling. If the core adds a flag, `ReferenceKinds.All = -1` picks it up everywhere in Premium with no
edit, and `TransitiveValidatorInstance` serialises the value as an integer
(`src/Metalama.Extensions.Validation.Engine/TransitiveValidatorInstance.cs:107, 117`), so the wire
format is additive-safe.

### A new optional field on an existing statement (labeled `break`/`continue`)

No effect on this repository at all. Nothing here reads `BreakStatementSyntax` or
`ContinueStatementSyntax`, and the code model exposes no statement-level abstraction that Premium
consumes. The only observable consequence is a possible additional `ReferenceKinds` entry in an aspect
test baseline if the label ever produces a reference.

### A new `ReferenceGranularity` (this repository's own extension axis)

`src/Metalama.Extensions.Validation/ReferenceGranularity.cs:15-53` is Premium's own enum. Adding a
member requires: `ReferenceEnd.GetDeclarationOfGranularity`
(`ReferenceEnd.cs:160`), `ReferenceValidationContext.GetInboundGranularity`
(`ReferenceValidationContext.cs:125`), the grouping-key switch at
`src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:135-143`, and
`ReferenceGranularityExtension.CombineWith`
(`src/Metalama.Extensions.Validation/ReferenceGranularityExtension.cs:69`), which assumes the members
are ordered coarsest to finest.

### The pipeline extension point itself

`src/Metalama.Extensions.Validation.Engine/ValidationPipelineExtension.cs:26` derives from
`Metalama.Framework.Engine.Pipeline.PipelineExtension` and overrides `Initialize` (28),
`ExecuteContributorsAsync` (42), `ExecutePipelineContributorsAsync` (65),
`ExecuteDesignTimePipelineContributorsAsync` (84), `GetTransitiveManifestExtensions` (113),
`GetPipelineContributorsFromTransitiveManifest` (116) and `AnalyzeSemanticModel` (133). A new abstract
member on `PipelineExtension` in the core lands here and in
`src/Metalama.Extensions.CodeFixes.Engine/CodeFixesPipelineExtension.cs`.

`src/Metalama.Extensions.CodeFixes/ICodeActionBuilder.cs:21` is marked `[InternalImplement]`, so new
code-action kinds are added by adding members there (lines 31, 36, 41, 46, 52) and implementing them in
`src/Metalama.Extensions.CodeFixes.Engine/CodeActionBuilder.cs:29-45`.

---

## 6. Question 5 — where the subsystem would silently do the wrong thing

Ordered by consequence.

### 6.1 A target-framework or Roslyn-version string that matches nothing

`TargetedAssemblyReference.SatisfiesCurrentProcess`
(`Metalama/…/Options/TargetedAssemblyReference.cs:22-24`) compares by exact equality;
`ExtensionLoaderBase.GetExtensionAssemblyPaths` (`Metalama/…/Extensibility/ExtensionLoaderBase.cs:29-38`)
filters by it and, when nothing matches, returns an empty sequence after a single `Trace` log at line 33.
`LoadExtensionAssemblies` (lines 56-83) reports `CannotLoadExtensionAssembly` only for an assembly that
*was* selected and then failed to load. There is no diagnostic for an empty selection.

The observable result is that code fixes, refactorings, architecture rules and validation rules all
stop working, with a green build and no message. The literals at risk are `net8.0` in
`src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props:6,10,12,16,18`,
`src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props:6,10,12`,
`src/Metalama.Extensions.CodeFixes/MetalamaExtensionAssemblies.props:10,13,15,18,20` and
`src/Metalama.Extensions.Validation/MetalamaExtensionAssemblies.props:10,13,15`; and the
`TargetRoslynVersion` values `4.12.0` and `5.0.0` in the same lines.

This is the single most important risk in #1913, and it is invisible to `Build.ps1 build`. The only
guards are the standalone tests under `src/tests/Standalone/` (`CodeFixes`, `Validation`,
`CachingBackends`, and their `*LicenseFailure` counterparts), which are run twice, once by
`ManyDotNetSolutions` and once through MSBuild (`eng/src/Program.cs:67-72`). Those projects are all
`net8.0`, so they must move too or they will test the wrong thing.

### 6.2 `ChangeVisibilityCodeAction` on an unknown declaration form

`ChangeVisibilityCodeAction.ExecuteAsync` (lines 33-50) visits every declaring syntax reference and
calls `context.UpdateTree( newRoot, syntaxTree )` unconditionally at line 48, whether or not the
rewriter changed anything. For a declaration form without a `Visit*` override — an interface, an
indexer, an extension block today, a union tomorrow — `CSharpSyntaxRewriter.DefaultVisit` rebuilds the
node unchanged, the code fix reports success, and the user sees a light bulb that does nothing.

The fix is a post-condition: compare the rewritten root against the original and report a diagnostic
when they are equal, or make `Rewriter` track whether it matched any node in `_nodes` (line 126).

### 6.3 The internal-surface enumeration misses member containers

`ArchitectureExtensions.VerifyInternalsAccess` (lines 152-174) and its duplicate in
`InternalsUsageValidationAttribute.BuildAspect` (lines 144-152) enumerate internal types, `t.Members()`
of public types, and internal accessors of public `t.Properties`. `t.Indexers` is missing, so
`InternalsCanOnlyBeUsedFrom` and `InternalsCannotBeUsedFrom` already fail to protect an internal
accessor of a public indexer. Nothing reports this: the rule simply never fires, and a false negative in
an architecture rule is silent by construction.

C# 15 widens the gap: an indexer declared in an extension block is a new place for the same omission.

### 6.4 `TransitiveValidatorInstance` does not serialise `Granularity` or `IncludeDerivedTypes`

`src/Metalama.Extensions.Validation.Engine/TransitiveValidatorInstance.cs`:
`Serializer.SerializeObject` (lines 103-111) writes `ValidatedDeclaration`, `ReferenceKinds`, `Object`,
`State` and `MethodName`. It does not write `Granularity` (declared at line 77) or `IncludeDerivedTypes`
(line 66). `DeserializeFields` (lines 113-121) reads the same five. A validator that crosses a project
boundary therefore comes back with `Granularity = ReferenceGranularity.SyntaxNode` — the field
initialiser at line 78, whose comment says "Default value for backward compatibility with serialized
values" — and `IncludeDerivedTypes = false`.

The consequences are both silent: the validator runs on the obsolete per-syntax-node path
(`ReferenceValidatorRunner.cs:158-191`), which costs one user-code call per node instead of one per
group, and a rule declared with `ReferenceValidationOptions.IncludeDerivedTypes` stops seeing derived
types in a downstream project. `SideBySideVersionTests.TransitiveValidator`
(`src/tests/Metalama.Extensions.Validation.UnitTests/SideBySideVersionTests.cs:19-60`) exercises exactly
this path with `ReferenceGranularity.Member` and `IncludeDerivedTypes`, so its baseline encodes the
current behaviour rather than the intended one.

### 6.5 The `MethodKind` switches fall through

`ReferenceValidatorQuerySource.cs:56-73` and `DynamicReferenceValidatorQuerySource.cs:53-67` list four
`MethodKind` values each and have no `default`. A validator attached to any other accessor-like method
is dropped without a message. Adding a new accessor form to the language, or reaching these switches
with an indexer accessor, produces a validator that is registered and never runs.

### 6.6 The grouping-key switch defaults silently

`src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:135-143` ends with
`_ => GetDeclaration`. A `ReferenceGranularity` value the switch does not name silently degrades to
per-declaration grouping. Combined with 6.4, a deserialised transitive validator lands on this arm.

### 6.7 The `net471` floor

`Metalama.Patterns.Caching.Backends.Azure` (line 4), `Metalama.Patterns.Caching.Backends.Redis`
(line 4) and `Metalama.Patterns.Caching.LoadTests` (line 5) target `net471`, below PB-2027.0's .NET
Framework floor of 4.7.2. A `net472` consumer resolves the `net471` asset happily, so this produces no
error; it produces an asset that is tested against a runtime we no longer claim to support.

### 6.8 The licensing task selection has no version guard

`src/Metalama.Licensing/build/Metalama.Licensing.targets:12` chooses `tasks/net8.0` from
`$(MSBuildRuntimeType) == 'Core'` alone, and line 11 computes but discards the host runtime version.
Below the SDK floor this fails with a raw assembly-load error from `UsingTask` at line 18, with no
`LAMA`-numbered diagnostic. `Metalama.Compiler` solves the same problem with `LAMA0622`; this file
should do the same.

### 6.9 The packaging `Include` globs

`…CodeFixes.Package.csproj:53-62` and `…Validation.Package.csproj:46-51` are `Include` patterns over
build outputs. A path that no longer exists contributes nothing and raises no error, so a half-renamed
variant produces a package that is missing an assembly and a `build/*.props` that references it. The
loader then finds no matching assembly and, by 6.1, says nothing.

---

## 7. What issue #1913 involves, concretely

Ordered so that each step leaves the repository buildable.

### 7.1 Target frameworks

Replace `net8.0` with `net10.0` in, at minimum:

- `src/Metalama.Extensions.CodeFixes/…csproj:4`, `src/Metalama.Extensions.Validation/…csproj:4`
- `src/Metalama.Extensions.CodeFixes.DesignTime/…csproj:6`
- `src/Metalama.Extensions.CodeFixes.Engine/…csproj:6`, `src/Metalama.Extensions.Validation.Engine/…csproj:7`
- `src/Metalama.Extensions.CodeFixes.Package.Resources/…csproj:6`,
  `src/Metalama.Extensions.Validation.Package.Resources/…csproj:6`
- `src/Metalama.Licensing.BuildTasks/…csproj:4`, and the pack paths at
  `src/Metalama.Licensing/Metalama.Licensing.csproj:29-30`
- `src/Metalama.Licensing/build/Metalama.Licensing.targets:12`
- `src/Metalama.Patterns.Caching.Backends.Azure/…csproj:4` and
  `src/Metalama.Patterns.Caching.Backends.Redis/…csproj:4`, where `net471` should also become `net472`
- every test project of section 1.1, and every `src/tests/Standalone/**` project
- the ten and six `TfmSpecificPackageFile` items in the two `Package.csproj` files
- the `net8.0` occurrences in the four extension-assembly manifest files of section 3.1

The manifest files (section 3.1) and the core's
`TargetedAssemblyReference._targetFramework` / `ExtensionLoaderBase` literals must agree; see 6.1.

### 7.2 Roslyn variants

Mirror the core's `develop/2027.0` layout exactly:

- rename `eng/RoslynVersions/Roslyn.5.0.0.props` semantics: it becomes the *suffixed* variant, with
  `ThisRoslynVersion=5.0.0`, `ThisRoslynVersionNoPreview=5.0.0`,
  `ThisRoslynVersionProjectSuffix=.5.0.0`, no `DefineConstants`;
- add `eng/RoslynVersions/Roslyn.5.10.0.props` as the *latest* variant, with
  `ThisRoslynVersion=$(RoslynApiMaxVersion)`, `ThisRoslynVersionNoPreview=5.10.0`, empty suffix, and —
  if Premium ever needs one — a `ROSLYN_5_10_0_OR_GREATER` constant;
- point `eng/RoslynVersions/Latest.props:2` at `Roslyn.5.10.0.props`;
- delete `eng/RoslynVersions/Roslyn.4.12.0.props` and the three `*.4.12.0` shim projects, and create
  three `*.5.0.0` shim projects in their place, following the template in section 1.2;
- define `RoslynApiMaxVersion` in `Directory.Packages.props` (it is currently undefined; see 1.2) and
  set `RoslynVersion` / `RoslynMaxVersion` to the core's `5.10.0-1.26365.3`;
- replace the `Metalama.Framework.Implementation.4.12.0` package version at
  `Directory.Packages.props:39` with `Metalama.Framework.Implementation.5.10.0`;
- update the four `InternalsVisibleTo` blocks of section 3.6;
- update the `Metalama.Premium.sln` solution-folder entries and, because the *latest* variant's assembly
  name changes from `…5.0.0` to `…5.10.0`, every literal assembly name in the four manifest files and
  the two packaging copy lists;
- add the three new shim projects to `Metalama.Premium.sln` and leave
  `Metalama.Premium.LatestRoslyn.slnf` unchanged (it lists only the primary projects).

Note the naming inversion this causes. Today `…Engine.5.0.0.dll` *is* the latest variant. After the
change, `…Engine.5.0.0.dll` is the Rider variant and `…Engine.5.10.0.dll` is the latest. Any file that
mentions `5.0.0` must be read to decide which of the two it now means; a blind rename is wrong.

### 7.3 Package versions

- Collapse the `SystemMemoryVersion` / `SystemRuntimeCompilerServicesUnsafeVersion` split
  (`Directory.Packages.props:18-27`, `:89-95`, `eng/RoslynVersions/Roslyn.5.0.0.props:11-15`): its whole
  justification is the Visual Studio 2022 17.14 `devenv.exe` binding-redirect ceiling, and Visual Studio
  2022 leaves the supported set under PB-2027.0.
- Revisit `SystemTextJsonVersion` per variant (`Roslyn.4.12.0.props:9` = 8.0.6,
  `Roslyn.5.0.0.props:9` = 9.0.0). The core sets 9.0.0 for the 5.0 variant and 10.0.11 for 5.10.
- `SystemTextJsonLatestVersion` and `MicrosoftBclAsyncInterfacesLatestVersion`
  (`Directory.Packages.props:10-11`) are both 9.0.10 and should follow the .NET 10 line.

### 7.4 Build agent and container

- `eng/src/Program.cs:27` requests the .NET 8 SDK "required by all tests"; with no `net8.0` test project
  left, it can go. Line 30 requests the .NET 6 runtime "required by some tests" — find which, or drop it.
  Line 33 requests the .NET 9 runtime "required by eng"; that is
  `eng/src/bin/…/net9.0/BuildMetalamaPremium.dll`, so it survives until PostSharp.Engineering moves.
- `eng/src/Program.cs:35` and `eng/docker/vs17.Dockerfile:33,36` pin Visual Studio Build Tools 17.14.15;
  PB-2027.0 makes Visual Studio 2026 the floor.
- `eng/docker/build.Dockerfile:44,48,52,56` installs runtimes and SDKs 6.0.36, 8.0.417, 9.0.12 and
  10.0.102.

### 7.5 The two changes that are not target frameworks

- Raise `MetalamaTemplateLanguageVersion` from `13.0` to `14.0` in `Directory.Build.props:20` and delete
  the "must be compatible with VS 2022" comment on line 19, which PB-2027.0 falsifies.
- Do **not** remove the `NET6_0_OR_GREATER` / `NET8_0_OR_GREATER` guards in the caching backends
  (section 3.4): unlike the `ROSLYN_*_OR_GREATER` guards that `77e53e9` removed, their `#else` arms
  still serve `netstandard2.0` and `net472`.

### 7.6 Verification

`Build.ps1 build` proves nothing about 6.1. The design-time and extension-loading paths are covered only
by `src/tests/Standalone/**` and by the two `*.AspectTests` projects, whose
`MetalamaExtensionAssembly` items at
`src/tests/Metalama.Extensions.CodeFixes.AspectTests/…csproj:51` and
`src/tests/Metalama.Extensions.Validation.AspectTests/…csproj:52` use
`$(ThisRoslynVersionNoPreview)` rather than a literal, so they follow a rename automatically and will
*not* catch a stale literal in the four manifest files. A deliberate check is required: build a
standalone project and confirm that a premium diagnostic is actually reported.
