# 08. The Roslyn public API delta and the semantics of the C# 15 features

This document is the reference that the theme documents of this directory cite. It records which Roslyn versions
exist, which public API members the C# 15 features add and in which version each member first appears, what each
feature means at the level of syntax, lowering, restrictions and diagnostics, and how many switch statements and
syntax visitors of this repository dispatch on the enumerations and node types that the features touch.

This document does not evaluate impact and proposes no work. The consequences for Metalama are the subject of the
theme documents, from [`01-language-version-and-hosts.md`](01-language-version-and-hosts.md) to
[`07-premium.md`](07-premium.md). This document also does not decide the platform baseline:
[`platform-support.md`](../platform-support.md) remains the authority on PB-2027.0,
[`Directory.Packages.md`](../../../Directory.Packages.md) on which package versions the baseline permits, and
[`updating-roslyn.md`](../updating-roslyn.md) on the procedure that a Roslyn transition follows.

The research behind this document is
[`analysis-reports/08-roslyn-api-delta.md`](analysis-reports/08-roslyn-api-delta.md). That report carries the full
per-file lists that are summarized here, and this document names it wherever a list is abridged. The Roslyn facts
were established on 2026-09-03 by downloading the files of the `dotnet/roslyn` repository and by inspecting the
`Microsoft.CodeAnalysis` packages published on nuget.org. The local inventory was taken on the working tree of
`/home/user/Metalama` at branch `topic/2027.0/26-09-03-update-eng-7e3j07` and re-counted on 2026-09-04. No file was
modified, no project was built and no test was run.

## The Roslyn versions

### The snapshots that were read

Each row is a state of the `dotnet/roslyn` repository that was read in full. The version column is the value of
`MajorVersion` and `MinorVersion` in `eng/Versions.props` at that state.

| Label | Git reference | Version in `eng/Versions.props` | What it is |
| --- | --- | --- | --- |
| 5.0 | Branch `release/dev18.0` | 5.0.0 | Roslyn 5.0, the compiler of Visual Studio 18.0. It is the API of the `Roslyn.5.0.0` payload variant. |
| 5.9.0 stable | Commit `35d9211b841e7613c1d2f8f5af6d628ace696c4c` of 2026-07-01, assembly informational version `5.9.0-1.26357.3` | 5.9.0 | The stable package published on nuget.org on 2026-08-17. Its assemblies, its XML documentation and its sources were inspected. |
| 5.10 window | Commit `27d6bed68b458350aca01f91397fa8be9605d47b` of 2026-07-28 | 5.10.0 | The last state of `main` while `main` was versioned 5.10, that is from 2026-06-29 to 2026-07-28. This window contains the consumed prerelease. |
| 5.11 window | Commit `fb723b29b175828bf666ed995a2ccaf0fb41e1ad` of 2026-08-25 | 5.11.0 | The last state of `main` while `main` was versioned 5.11. No package carries this version. |
| main | Branch `main` on 2026-09-03 | 5.12.0 | The state from which the next stable package is expected to be built. |

### The published versions

nuget.org serves four stable versions of `Microsoft.CodeAnalysis.CSharp` in the 5.x line. Roslyn publishes a stable
version every third minor, in step with the quarterly Visual Studio 2026 releases.

| Version | Kind | Date | Visual Studio | Note |
| --- | --- | --- | --- | --- |
| 5.0.0 | Stable | 2025-11-18 | 18.0 | The floor of `RoslynApiMinVersion`. |
| 5.3.0 | Stable | 2026-03-10 | 18.3 | |
| 5.6.0 | Stable | 2026-07-02 | 18.6 | |
| 5.9.0 | Stable | 2026-08-17 | 18.9 | The highest stable version that exists. |
| 5.10.0-1.26365.3 | Prerelease | Build of 2026-07-15 | None | A build of `main`, restored from the `roslyn-consolidated` feed. It is the value of `RoslynApiMaxVersion`. No stable 5.10 package exists. |
| 5.11 | None | | | A version number that `main` carried between 2026-07-28 and 2026-08-25. No package carries it. |
| 5.12 | Expected stable | November 2026, inferred | 18.12 | The version that `main` carries today, and the version expected in the Visual Studio 2026 long-term servicing channel, in Visual Studio 2027 and in the .NET 11 software development kit. |

Two qualifications belong to the last row. First, the November 2026 date and the identification of 5.12 as the next
stable version follow from the published cadence and from the version bumps of `main`, not from an announcement by
Microsoft. Second, the Roslyn version of the .NET 11 software development kit at general availability is not
published; the only data point is the tag `NET-SDK-11.0.100-preview.5.26302.115` of 2026-06-01, whose
`eng/Versions.props` reads 5.8.0.

The public branches of `dotnet/roslyn` are `release/dev18.0` and `release/dev18.3` only. The 5.9.0 package was built
from an internal branch named `release/insiders` at commit `35d9211b`, which is also on `main` and is dated
2026-07-01, that is eight days after `main` was bumped to 5.10 on 2026-06-29.

### The three commits that separate the consumed build from `main`

| Commit | Date | What it changed |
| --- | --- | --- |
| dotnet/roslyn#84707 | 2026-07-31 | Added `ITypeSymbol.UnionCaseTypes`. |
| dotnet/roslyn#84799, "Add C# 15 language version" | 2026-08-11 | Added `LanguageVersion.CSharp15 = 1500`, changed `MessageID.RequiredVersion` of the six features from `Preview` to `CSharp15`, changed the mapping of `Default`, `Latest` and `LatestMajor` in `MapSpecifiedToEffectiveVersion` from `CSharp14` to `CSharp15`, and removed the `ExperimentalUrl` and `[RSEXPERIMENTAL006]` markers from the union, collection-argument and labeled-break API. |
| dotnet/roslyn#84738 | 2026-08-19 | Added `INamedTypeSymbol.TypeLayout` and the `TypeLayout` struct. |

### How C# 15 appears in each snapshot

| Property | 5.9.0 stable and the consumed 5.10 prerelease | `main`, that is 5.12 |
| --- | --- | --- |
| `LanguageVersion.CSharp15` | Does not exist. | Exists, with the value 1500. |
| `MapSpecifiedToEffectiveVersion` for `Default`, `Latest` and `LatestMajor` | Returns `CSharp14`. | Returns `CSharp15`. |
| `MessageID.RequiredVersion` of the six features | Returns `Preview`. | Returns `CSharp15`. |
| The union, closed-hierarchy, collection-argument and labeled-break public API | Present and marked `[RSEXPERIMENTAL006]`. | Present without the marker. |
| `ExperimentalUrl` in `Syntax.xml` | On `UnionDeclarationSyntax`, `WithElementSyntax`, the `Name` field of `BreakStatementSyntax` and `ContinueStatementSyntax`, and `UnsafeExpressionSyntax`. | On `UnsafeExpressionSyntax` only. |
| `RegisterPreCompilationSourceOutput` | Present and marked `[RSEXPERIMENTAL007]`. | Present and still marked `[RSEXPERIMENTAL007]`. |

The consequence for Metalama is a matter of fact rather than of judgement: no Roslyn version that Metalama consumes
today exposes C# 15 as a language version other than `preview`, and none exposes the new syntax API without an
experimental marker.

The state of the consumed prerelease was established indirectly, because the package itself was not read. Its build
number decodes to 2026-07-15; the two `PublicAPI.Unshipped.txt` files are byte-identical at commit `35d9211b` of
2026-07-01 and at commit `27d6bed6` of 2026-07-28; the histories of `PublicAPI.Unshipped.txt`, `LanguageVersion.cs`
and `Syntax.xml` show no commit between 2026-06-24 and 2026-07-31; and the grammar file
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` of this repository carries the same five `ExperimentalUrl`
markers as the 5.10 window.

## The public API delta

The delta between the 5.0 snapshot and each later snapshot was computed as the set difference of the lines of
`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. In the tables below, `Core` is
`src/Compilers/Core/Portable/PublicAPI.*.txt` and `CSharp` is `src/Compilers/CSharp/Portable/PublicAPI.*.txt`. The
relevance column states where Metalama uses the member, or states that it does not use it. It states no conclusion
and proposes no work.

Nothing was removed from the public API between 5.0 and `main`. The only other changes are the addition of the
`params` modifier to several parameters and the attribute changes listed in the first table.

### Members present in the stable 5.9.0 and in the consumed prerelease 5.10.0-1.26365.3

In the version column, "5.10 window" means present at commit `35d9211b`, that is in the 5.9.0 package, and
throughout the 5.10 window. "Marker removed in the 5.11 window" means that commit dotnet/roslyn#84799 of 2026-08-11
removed the `[RSEXPERIMENTAL006]` prefix. Because no package carries the version 5.11, the first stable package
without the prefix is expected to be 5.12.

| Type | Member | Kind | Version | Source | Relevance to Metalama |
| --- | --- | --- | --- | --- | --- |
| `Microsoft.CodeAnalysis.ITypeSymbol` | `IsUnion { get; }` | New member | 5.10 window, `[RSEXPERIMENTAL006]`; marker removed in the 5.11 window | Core Unshipped | The code model has no union flag. The analogue is `IsRecord` (`Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:173` and `CodeModel/Introductions/Builders/NamedTypeBuilder.cs:56`). The member does not exist in the `Roslyn.5.0.0` variant. |
| `Microsoft.CodeAnalysis.ITypeSymbol` | `IsClosed { get; }` | New member | Same as above | Core Unshipped | Same as above. Documented as restricted from being inherited from outside its containing module (`src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs:166`). |
| `Microsoft.CodeAnalysis.ITypeSymbol` | `GetClosedDerivedTypeInfo(CancellationToken)` returning `ClosedDerivedTypeInfo` | New member | Same as above | Core Unshipped | Throws `InvalidOperationException` when the type is not closed (`ITypeSymbol.cs:171`). Metalama has no consumer of a derived-type list today. |
| `Microsoft.CodeAnalysis.ClosedDerivedTypeInfo` | Struct with `ClosedDerivedTypes` of type `ImmutableArray<INamedTypeSymbol>`, `IsComplete` of type `bool` and a parameterless constructor | New type | Same as above | Core Unshipped; `src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs` | `IsComplete` is false when a generic closed type has an unspeakable derived type. |
| `Microsoft.CodeAnalysis.Operations.CommonConversion` | `IsUnion { get; }` | New member | Same as above | Core Unshipped | Metalama does not inspect `CommonConversion`. |
| `Microsoft.CodeAnalysis.OperationKind` | `CollectionExpressionElementsPlaceholder = 129` | New enumeration value | Same as above | Core Unshipped | No switch over `OperationKind` exists under `Metalama.Framework/src`. `IOperation` is used only in `Metalama.Framework.Analyzers` and in `Metalama.Framework.Engine.Analyzers/MetalamaPerformanceAnalyzer.cs`. |
| `Microsoft.CodeAnalysis.Operations.ICollectionExpressionElementsPlaceholderOperation` | Interface | New type | Same as above | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.Operations.ICollectionExpressionOperation` | `ConstructArguments` of type `ImmutableArray<IOperation>` | New member | Same as above | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.Operations.OperationVisitor` and `OperationVisitor<TArgument, TResult>` | `VisitCollectionExpressionElementsPlaceholder` | New virtual member | Same as above | Core Unshipped | Metalama declares no subclass of `OperationVisitor`. |
| `Microsoft.CodeAnalysis.WellKnownMemberNames` | `HasValuePropertyName` and `TryGetValueMethodName` | New constants | Same as above | Core Unshipped | The names of the optional non-boxing access members of a union. |
| `Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext` | `RegisterPreCompilationSourceOutput<TSource>`, in an `IncrementalValueProvider<TSource>` overload and an `IncrementalValuesProvider<TSource>` overload | New member | 5.10 window, `[RSEXPERIMENTAL007]`, still marked on `main` | Core Unshipped; `docs/features/pre-compilation-source-outputs.md` | Metalama's design-time generator is `Metalama.Framework.DesignTime/SourceGeneration/BaseSourceGenerator.cs:28`, wrapped by `Metalama.Framework.CompilerExtensions/MetalamaSourceGenerator.cs:14`. Both implement `IIncrementalGenerator` and neither registers this phase. |
| `Microsoft.CodeAnalysis.PreCompilationSourceProductionContext` | Struct with `AddSource(string, string)`, `AddSource(string, SourceText)` and `CancellationToken` | New type | 5.10 window, `[RSEXPERIMENTAL007]` | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind` | `PreCompilation = 16` | New enumeration value | 5.10 window, not experimental | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.WellKnownGeneratorOutputs` | `PreCompilationSourceOutput` | New constant | 5.10 window | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.IMethodSymbol` | `ReduceExtensionMember(ITypeSymbol)` returning `IMethodSymbol?` | New member | Shipped between 5.0 and the 5.10 window, by the commit "Extensions: Add ReduceExtensionMember API" of 2025-10-22 | Core Shipped | Metalama uses the 5.0 member `AssociatedExtensionImplementation` (`Metalama.Framework.Engine/CodeModel/Source/SourceMethod.cs:174`) and does not use `ReduceExtensionMember`. The member does not exist in the `Roslyn.5.0.0` variant. |
| `Microsoft.CodeAnalysis.IPropertySymbol` | `ReduceExtensionMember(ITypeSymbol)` returning `IPropertySymbol?` | New member | Same as above | Core Shipped | Same as above. This is the only change to `IPropertySymbol`, and it is not specific to extension indexers. |
| `Microsoft.CodeAnalysis.Emit.EmitDifferenceOptions` | `MethodImplEntriesSupported { get; init; }` | New member | Shipped between 5.0 and the 5.10 window | Core Shipped | Not used by Metalama. It belongs to Edit and Continue. |
| `Microsoft.CodeAnalysis.Text.SourceHashAlgorithm` | `Sha384 = 3` and `Sha512 = 4` | New enumeration values | 5.10 window | Core Unshipped | `Metalama.Framework.Engine/Templating/Mapping/TextMapFile.cs:72-155` writes and compares `SourceHashAlgorithm.None` only, and `CompileTime/CompileTimeCompilationBuilder.cs:520` uses `Sha256`. There is no switch over this enumeration. |
| `Microsoft.CodeAnalysis.Diagnostics.AnalysisContext`, `CompilationStartAnalysisContext`, `CodeBlockStartAnalysisContext<T>`, `OperationBlockStartAnalysisContext` and `SymbolStartAnalysisContext` | The `ImmutableArray` parameter of `RegisterSymbolAction`, `RegisterSyntaxNodeAction<T>` and `RegisterOperationAction` gains `params` | Signature change, source compatible and binary compatible | Between 5.0 and the 5.10 window | Core Shipped | No effect on Metalama. |
| `Microsoft.CodeAnalysis.AssemblyMetadata` | `Create(params ImmutableArray<ModuleMetadata>)` | Signature change | Between 5.0 and the 5.10 window | Core Shipped | No effect on Metalama. |
| `Microsoft.CodeAnalysis.SemanticModel` | `NullableAnalysisIsDisabled` becomes `[RSEXPERIMENTAL001]` | Attribute change | Between 5.0 and the 5.10 window | Core Shipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.GeneratorRunResult` | `HostOutputs` becomes `[RSEXPERIMENTAL004]` | Attribute change | Between 5.0 and the 5.10 window | Core Shipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.CSharp.SyntaxKind` | `UnionKeyword = 8452`, `ClosedKeyword = 8453`, `WithElement = 9081`, `UnionDeclaration = 9082` | New enumeration values | 5.10 window, `[RSEXPERIMENTAL006]`; marker removed in the 5.11 window | CSharp Unshipped | The repository holds 156 switches over `SyntaxKind`, of which 74 have a throwing default arm. `UnionDeclaration` is absent from every explicit list of type-declaration kinds; see the inventory below. |
| `Microsoft.CodeAnalysis.CSharp.SyntaxKind` | `SafeKeyword = 8454`, `UnsafeExpression = 8769` | New enumeration values | 5.10 window, `[RSEXPERIMENTAL006]`, still marked on `main` | CSharp Unshipped | These values belong to unsafe evolution, which stays a preview feature. |
| `Microsoft.CodeAnalysis.CSharp.Syntax.UnionDeclarationSyntax` | Class deriving from `TypeDeclarationSyntax`, overriding `AttributeLists`, `Modifiers`, `Keyword`, `Identifier`, `TypeParameterList`, `ParameterList`, `BaseList`, `ConstraintClauses`, `OpenBraceToken`, `Members`, `CloseBraceToken` and `SemicolonToken`, with a twelve-parameter `Update`, a `With` method per field and an `Add` method per list | New type | 5.10 window, `[RSEXPERIMENTAL006]`; marker removed in the 5.11 window | CSharp Unshipped; `Syntax.xml` line 3513, declared with `SkipConvenienceFactories="true"` | A pattern that matches `TypeDeclarationSyntax` accepts it. The explicit kind lists and the visitors that override `VisitClassDeclaration`, `VisitStructDeclaration` or `VisitRecordDeclaration` do not; see the inventory below. |
| `Microsoft.CodeAnalysis.CSharp.SyntaxFactory` | `UnionDeclaration`, a single overload with twelve parameters and no convenience factory | New member | Same as above | CSharp Unshipped | It belongs to the factory surface that the template compiler and `MetaSyntaxRewriter` use. |
| `Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`, `CSharpSyntaxVisitor<TResult>` and `CSharpSyntaxRewriter` | `VisitUnionDeclaration(UnionDeclarationSyntax)` | New virtual member | Same as above | CSharp Unshipped | Every `SafeSyntaxRewriter`, `SafeSyntaxWalker` and `SafeSyntaxVisitor` of `Metalama.Framework.Sdk/Utilities/Roslyn` inherits it, so a subclass that overrides only the per-kind methods falls through to `DefaultVisit`. |
| `Microsoft.CodeAnalysis.CSharp.Syntax.WithElementSyntax` | Class deriving from `CollectionElementSyntax`, with `WithKeyword`, `ArgumentList`, `Update`, `WithWithKeyword`, `WithArgumentList` and `AddArgumentListArguments` | New type | Same as above | CSharp Unshipped; `Syntax.xml` line 1874 | Collection expressions are handled in `Metalama.Framework.Engine/Templating/TemplateAnnotator.cs`, `SyntaxGeneration/ContextualSyntaxGenerator.cs`, `SyntaxGeneration/ContextualSyntaxGenerator.RemoveReferenceNullableAnnotationsRewriter.cs`, `ReferenceGraph/ReferenceIndexWalker.cs` and `CodeModel/Visitors/TypeRewriter.cs`. |
| `Microsoft.CodeAnalysis.CSharp.SyntaxFactory` | `WithElement(ArgumentListSyntax?)` and `WithElement(SyntaxToken, ArgumentListSyntax)` | New members | Same as above | CSharp Unshipped | Factory surface, as above. |
| `CSharpSyntaxVisitor`, `CSharpSyntaxVisitor<TResult>` and `CSharpSyntaxRewriter` | `VisitWithElement(WithElementSyntax)` | New virtual member | Same as above | CSharp Unshipped | As for `VisitUnionDeclaration`. |
| `Microsoft.CodeAnalysis.CSharp.Syntax.BreakStatementSyntax` | `Name` of type `IdentifierNameSyntax?`, `WithName(IdentifierNameSyntax?)` and a four-parameter `Update`; the three-parameter `Update` remains | New members | Same as above | CSharp Unshipped; `Syntax.xml` line 2503, declared `Optional="true"` | `Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:1375` and `:1378` override `VisitBreakStatement` and `VisitContinueStatement`; `Templating/TemplateCompilerRewriter.cs:2565-2573` and `Templating/TemplateExpansionContext.cs:827-828` treat the kinds `BreakStatement` and `ContinueStatement`; `Linking/Substitution/ReturnStatementSubstitution.cs:86`, `:104`, `:154` and `Templating/Statements/SwitchStatement.cs:93` construct unlabeled break statements. |
| `Microsoft.CodeAnalysis.CSharp.Syntax.ContinueStatementSyntax` | `Name`, `WithName` and a four-parameter `Update` | New members | Same as above | CSharp Unshipped | As above. |
| `Microsoft.CodeAnalysis.CSharp.SyntaxFactory` | `BreakStatement(IdentifierNameSyntax?)`, `BreakStatement(attributeLists, name)` and `BreakStatement(attributeLists, breakKeyword, name, semicolonToken)`, and the three corresponding overloads of `ContinueStatement`; the existing overloads remain | New members | Same as above | CSharp Unshipped | An existing call to `BreakStatement()` binds to the unchanged parameterless overload. |
| `Microsoft.CodeAnalysis.CSharp.Conversion` | `IsUnion { get; }` | New member | Same as above | CSharp Unshipped | Metalama classifies conversions by `TypeKind` in `Metalama.Framework.Engine/CodeModel/Comparers/DeclarationEqualityComparer.Conversions.cs`, not by the flags of `Conversion`. |
| `Microsoft.CodeAnalysis.CSharp.Syntax.UnsafeExpressionSyntax`, the two `SyntaxFactory.UnsafeExpression` overloads and `VisitUnsafeExpression` on `CSharpSyntaxVisitor`, `CSharpSyntaxVisitor<TResult>` and `CSharpSyntaxRewriter` | The node of `unsafe(expression)` | New type and members | 5.10 window, `[RSEXPERIMENTAL006]`, still marked on `main` | CSharp Unshipped | Unsafe evolution stays a preview feature. |
| `Microsoft.CodeAnalysis.CSharp.ForEachStatementInfo` | `DisposeAwaitableInfo` and `MoveNextAwaitableInfo`, both of type `AwaitExpressionInfo` | New members | Shipped between 5.0 and the 5.10 window | CSharp Shipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.CSharp.CSharpExtensions` | `GetAwaitExpressionInfo(SemanticModel?, LocalDeclarationStatementSyntax)` and `GetAwaitExpressionInfo(SemanticModel?, UsingStatementSyntax)` | New members | Shipped between 5.0 and the 5.10 window | CSharp Shipped | Not used by Metalama. |

### Members added to `main` after the 5.10 window

These members are absent from the 5.9.0 package and from the consumed prerelease. Code that names one of them does
not compile against the Roslyn that Metalama consumes today.

| Type | Member | Kind | Version | Source | Relevance to Metalama |
| --- | --- | --- | --- | --- | --- |
| `Microsoft.CodeAnalysis.CSharp.LanguageVersion` | `CSharp15 = 1500` | New enumeration value | 5.11 window, by dotnet/roslyn#84799 of 2026-08-11 | CSharp Unshipped; `LanguageVersion.cs` line 283 | It is the value that `SupportedCSharpVersions`, `LanguageVersionProvider` and `LanguageVersionExtensions.ToDisplayStringSafe` would have to carry. Code compiled against the 5.10 prerelease cannot name it, whereas the numeric cast `(LanguageVersion) 1500` compiles against every version. |
| `Microsoft.CodeAnalysis.ITypeSymbol` | `UnionCaseTypes` of type `ImmutableArray<ITypeSymbol>` | New member | 5.11 window, by dotnet/roslyn#84707 of 2026-07-31 | Core Unshipped | It is the only public way to read the case types of a union. It exists neither in the `Roslyn.5.0.0` variant nor in a 5.10 build. |
| `Microsoft.CodeAnalysis.INamedTypeSymbol` | `TypeLayout` of type `TypeLayout` | New member | 5.11 window, by dotnet/roslyn#84738 of 2026-08-19 | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.TypeLayout` | Struct with `Kind` of type `LayoutKind`, `Size` of type `int`, `PackingSize` of type `ushort`, and equality members and operators | New type | 5.11 window | Core Unshipped | Not used by Metalama. |
| `Microsoft.CodeAnalysis.ISymbol` | `RequiresUnsafeContext { get; }` | New member | 5.11 window, `[RSEXPERIMENTAL006]` | Core Unshipped | It belongs to unsafe evolution, which stays a preview feature. |
| `Microsoft.CodeAnalysis.IModuleSymbol` | `MemorySafetyRulesVersion` of type `MemorySafetyRulesVersion` | New member | `main`, that is 5.12, `[RSEXPERIMENTAL006]` | Core Unshipped | As above. |
| `Microsoft.CodeAnalysis.MemorySafetyRulesVersion` | Enumeration with `Version1 = 1` and `Version2 = 2` | New type | `main`, `[RSEXPERIMENTAL006]` | Core Unshipped | As above. |
| `Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions` | `MemorySafetyRulesVersion` and `WithMemorySafetyRulesVersion(MemorySafetyRulesVersion)` | New members | `main`, `[RSEXPERIMENTAL006]` | CSharp Unshipped | As above. |
| `Microsoft.CodeAnalysis.CSharp.CSharpExtensions` | `GetValueConversion(ICoalesceOperation)` returning `Conversion` | New member | `main`, by dotnet/roslyn#85029 of 2026-09-02 | CSharp Unshipped | Not used by Metalama. |

### The experimental markers and the grammar file

At the 5.10 window, the Core `PublicAPI.Unshipped.txt` file holds 15 lines marked `[RSEXPERIMENTAL006]` and 7 lines
marked `[RSEXPERIMENTAL007]`, and the C# file holds 89 lines marked `[RSEXPERIMENTAL006]`. Both files are
byte-identical at commit `35d9211b`, that is in the 5.9.0 package. At the 5.11 window every `[RSEXPERIMENTAL006]`
prefix on the union, closed-hierarchy, collection-argument and labeled-break API is gone, and the same lines
reappear without a prefix.

`Syntax.xml` follows the same schedule. At the 5.10 window it carries `ExperimentalUrl` on `UnionDeclarationSyntax`
(issue 82567), on `WithElementSyntax` (issue 82210), on the `Name` field of `BreakStatementSyntax` and
`ContinueStatementSyntax` (issue 83266) and on `UnsafeExpressionSyntax` (issue 82789). At the 5.11 window and on
`main`, only `UnsafeExpressionSyntax` keeps it. The copy of the grammar in this repository,
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml`, carries the same five markers, and
`TreeReader.RemoveExperimentalDeclarations` in `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs` removes every
node and every field that carries the attribute before generation.

### Verified unchanged between 5.0 and `main`

- `Microsoft.CodeAnalysis.TypeKind` has 16 values, the highest being `Extension = 14`, and has no `Union` value.
  `EnumConversions.ToTypeKind` in `src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs` lines 35 to 38 maps
  `DeclarationKind.Struct`, `DeclarationKind.Union` and `DeclarationKind.RecordStruct` to `TypeKind.Struct`, and
  `SourceMemberContainerSymbol.MakeModifiers` treats a union declaration inside the `TypeKind.Struct` case. A union
  therefore has `TypeKind.Struct` and a closed class has `TypeKind.Class`.
- `Microsoft.CodeAnalysis.SymbolKind`, whose highest value remains `FunctionPointerType = 20`,
  `Microsoft.CodeAnalysis.MethodKind`, whose highest value remains `FunctionPointerSignature = 18`,
  `Microsoft.CodeAnalysis.Accessibility`, `ISymbol.DeclaredAccessibility` and `IMethodSymbol.MethodKind` are
  unchanged.
- `IPropertySymbol` gained only `ReduceExtensionMember`. An extension indexer is an `IPropertySymbol` whose
  `IsIndexer` is true, declared on an `INamedTypeSymbol` whose `IsExtension` is true, that is of `TypeKind.Extension`,
  and it is read through the 5.0 members `ExtensionParameter`, `ExtensionGroupingName`, `ExtensionMarkerName` and
  `AssociatedExtensionImplementation`.
- `INamedTypeSymbol` gained only `TypeLayout`. `IsUnion`, `UnionCaseTypes` and `IsClosed` are declared on
  `ITypeSymbol`, not on `INamedTypeSymbol`.
- No operation kind was added for a labeled `break` or `continue`. `IBranchOperation`, with `BranchKind.Break` or
  `BranchKind.Continue` and a `Target` of type `ILabelSymbol`, and `ILabeledOperation` are unchanged.
- `CSharpSyntaxVisitor`, `CSharpSyntaxRewriter` and `SyntaxFactory` gained only the members listed in the tables
  above. There is no new syntax kind for a labeled break: the target label is an ordinary
  `SyntaxKind.LabeledStatement`, whose value 8799 is unchanged.
- No analyzer registration API was added. `AnalysisContext` gained only the `params` modifier.

## The semantics of the C# 15 features

Each subsection below gives the proposal, the syntax, the lowering, the restrictions and the diagnostics of one
feature. The six features of C# 15 are gated on `LanguageVersion.CSharp15` on `main` and on `LanguageVersion.Preview`
in the consumed prerelease. Unsafe evolution is not part of C# 15 and is described last.

### Union types

The proposal is https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md, with champion issue
dotnet/csharplang#9662. The Roslyn feature branch is `features/Unions`, the tracking issue is dotnet/roslyn#81074,
and the branch was merged into `main` on 2026-03-05.

- Syntax: `attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list? '(' case_types ')'
  struct_interfaces? type_parameter_constraints_clause* ( '{' struct_member_declaration* '}' | ';' )`. In Roslyn the
  node is `UnionDeclarationSyntax`, of kind `UnionDeclaration`, with `DeclarationKind.Union` and `TypeKind.Struct`.
  The case types are carried by the `ParameterList` of the declaration.
- Lowering: the compiler emits a struct that carries the synthesized attribute
  `System.Runtime.CompilerServices.Union`, unless the source already applies it, which
  `SourceNamedTypeSymbol.ShouldApplyUnionAttribute` decides. The struct implements
  `System.Runtime.CompilerServices.IUnion`, receives a synthesized property `public object? Value { get; }`
  (`SynthesizedUnionValuePropertySymbol`) and receives one public constructor per case type (`SynthesizedUnionCtor`).
  A user may declare `HasValue` and `TryGetValue<T>(out T)` members to avoid boxing. A union conversion, which
  `Conversion.IsUnion` reports, is an implicit conversion from a case type that calls the creation member. It is not
  allowed in an expression tree, which is error CS9369.
- Restrictions: the allowed modifiers are those of a struct, including `partial`, `readonly`, `unsafe` and `safe`,
  but not `ref`, as `SourceMemberContainerSymbol.MakeModifiers` decides. Nesting and generics are allowed. A union
  needs at least one case type (CS9370). It may declare no instance field, no automatic property and no field-like
  event (CS9373). It may not declare a public single-parameter constructor explicitly (CS9374), and an explicit
  constructor must chain to another constructor with `this` (CS9375). A `union` declaration may not use a member
  provider interface (CS9387). A type that is a union only because it carries the attribute needs at least one
  creation member (CS9385) and a public `Value` getter of type `object` (CS9386). The proposal requires every union
  member to be public and says nothing about `InternalsVisibleTo`.
- Diagnostics: exhaustiveness reuses the existing codes CS8509, for a switch expression that is not exhaustive, and
  CS8655, for an unhandled null input. A switch that handles every case type is exhaustive without a fallback arm.
  CS9372 reports a pattern that cannot handle the `Value` of the union. The language version is checked through
  `IDS_FeatureUnions`.

### Closed hierarchies

The proposal is https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md, with
champion issue dotnet/csharplang#9499. The Roslyn feature branch is `features/closed-class`, the tracking issue is
dotnet/roslyn#81039, and the public API was merged on 2026-06-17 by dotnet/roslyn#84045.

- Syntax: `closed` is a contextual modifier that applies to classes only. It is a token of kind
  `SyntaxKind.ClosedKeyword`, value 8453, in the `Modifiers` list of a `ClassDeclarationSyntax`. There is no new
  node.
- Lowering: `SourceNamedTypeSymbol`, at lines 1805 to 1829 on `main`, synthesizes the attribute
  `System.Runtime.CompilerServices.IsClosedType` on the closed class, whose `DerivedTypes` property lists the
  candidate closed subtype definitions as unbound generic definitions. `DerivedTypes` must be an instance property
  of type `Type[]` with a public getter and a public setter, which error CS9395 enforces. The public API is
  `ITypeSymbol.IsClosed` and `ITypeSymbol.GetClosedDerivedTypeInfo`.
- Restrictions: a closed class is implicitly abstract, so `sealed` and `static` are errors (CS9381) and an explicit
  `abstract` is an error (CS9384), and a type or an alias may not be named `closed` (CS9380). A closed type of
  another assembly may not be used as a base type (CS9382, `ERR_ClosedBaseTypeBaseFromOtherAssembly`); the proposal
  states the same rule for the module. A generic derived class must reference every type parameter of its base type
  (CS9383, `ERR_UnderspecifiedClosedSubtype`). Nesting is not required. The proposal does not discuss `partial`.
- Diagnostics: `InternalsVisibleTo` does not lift the same-assembly rule, because the rule is stated on the assembly
  and not on accessibility, and the proposal does not mention friend assemblies. For exhaustiveness, a switch that
  handles every direct derived type exhausts the closed class, and an incomplete switch reports the existing CS8509
  family. A case for the closed base type placed after all derived types is an unreachable case, which is an error.
  An empty switch does not exhaust a closed class that has no subtype. A type parameter constrained to a closed
  class is treated like the closed class.
- Closed enumerations are a separate proposal,
  https://github.com/dotnet/csharplang/blob/main/proposals/closed-enums.md, with champion issue
  dotnet/csharplang#9011. It has no version target and no implementation in `main`: neither an error code nor a
  `MessageID` exists for it.

### Labeled break and continue

The proposal is
https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/labeled-break-continue.md, with champion issue
dotnet/csharplang#9875. The Roslyn feature branch is `features/labeled-break-and-continue`, merged through
dotnet/roslyn#84271, with the parser change in dotnet/roslyn#83197 of 2026-04-23.

- Syntax: `break identifier? ;` and `continue identifier? ;`. The target label is an ordinary labeled statement whose
  statement is a switch statement or an iteration statement. In Roslyn the identifier is the optional `Name` field,
  of type `IdentifierNameSyntax`, on `BreakStatementSyntax` and `ContinueStatementSyntax`. There is no new syntax
  kind.
- Lowering: the binder resolves the label to the existing break label or continue label of the enclosing loop and
  emits the ordinary bound break or continue statement, at `Binder_Statements.cs` lines 2960 to 2991. There is no new
  bound node, no new intermediate language pattern and no new operation type.
- Restrictions: only a switch statement or an iteration statement may be targeted. The nearest enclosing statement
  labeled with the identifier is chosen, and only the statement directly nested under the label receives it. A
  labeled `continue` may not target a switch. A `finally` block may not be left.
- Diagnostics: CS9393 (`ERR_NoBreakId`), whose text is "No enclosing loop or switch statement with the label '{0}'
  out of which to break", and CS9394 (`ERR_NoContinueId`). The language version is checked through
  `IDS_FeatureLabeledBreakContinue`, at the location of the name. Nothing in this feature relates to
  `InternalsVisibleTo` or to exhaustiveness.

### Collection expression arguments

The proposal is
https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/collection-expression-arguments.md, with
champion issue dotnet/csharplang#8887. The Roslyn feature branch is `features/collection-expression-arguments` and
the tracking issue is dotnet/roslyn#80613; parsing was merged on 2025-10-03 and the operation model on 2025-12-04.

- Syntax: `collection_element : expression_element | spread_element | with_element` and
  `with_element : 'with' argument_list`. An element that starts with `with (` is always a with-element, and it must
  be the first element. In Roslyn the node is `WithElementSyntax`, of kind `WithElement`, whose `ArgumentList` is an
  `ArgumentListSyntax`.
- Lowering: for a class or struct target the arguments select an accessible instance constructor by overload
  resolution. For a target that uses `CollectionBuilder` the arguments select a create method whose last parameter is
  the `ReadOnlySpan<T>` of elements, and the arguments are prepended. For a mutable interface target the candidate
  signatures are those of `List<T>()` and `List<T>(int)`, and for a dictionary interface also the comparer
  overloads. In the operation model the feature adds `ICollectionExpressionOperation.ConstructArguments` and the
  operation kind `CollectionExpressionElementsPlaceholder`, value 129.
- Restrictions: a with-element that is not first is error CS9354; an unsupported target type, such as an array or a
  span, is CS9355; a dynamic argument is CS9356; a non-empty `with()` for a read-only interface is CS9357; a wrong
  argument count for the create method is CS9359. The presence of a with-element affects whether the collection
  conversion exists; the argument values do not.
- Diagnostics: nothing in this feature relates to `InternalsVisibleTo` or to exhaustiveness.

### Extension indexers

The proposal is https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/extension-indexers.md, with
champion issue dotnet/csharplang#9856 and language design meeting decisions from 2026-02-02 to 2026-04-07. The Roslyn
feature branch is `features/extensions` and the tracking issue is dotnet/roslyn#81505.

- Syntax: an indexer declaration, that is `this[...]`, inside an `extension(T receiver) { }` block. Only instance
  members are allowed, so the block must name its receiver parameter. Roslyn reuses `IndexerDeclarationSyntax` inside
  `ExtensionBlockDeclarationSyntax`. There is no new node, no new syntax kind and no new symbol API.
- Lowering: the compiler emits static implementation methods `get_Item` and `set_Item`, with the receiver prepended
  to the parameter list, in the grouping type, which carries `Extension` and `DefaultMember("Item")`, together with
  the marker type. The indexer appears as an `IPropertySymbol` whose `IsIndexer` is true, declared on the extension
  type, that is of `TypeKind.Extension`, and it is reduced through `IPropertySymbol.ReduceExtensionMember`.
- Restrictions: the modifiers `abstract`, `virtual`, `override`, `new`, `sealed`, `partial` and `protected` are not
  allowed, and there is no `init` accessor. Every type parameter of the extension block must be inferable from the
  parameters. Extension indexers on strings and on arrays are not allowed, per the language design meeting of
  2026-04-07. They are not allowed in an expression tree and are never considered on a `base` receiver. An element
  access binds instance indexers first, then implicit `Index` and `Range` indexers, then extension indexers, then
  implicit extension indexers.
- Diagnostics: an ambiguity between applicable extension indexers is a compile-time error. The language version is
  checked through `IDS_FeatureExtensionIndexers`. Nothing in this feature relates to `InternalsVisibleTo` or to
  exhaustiveness.

### Static members in interfaces without runtime support for default interface implementation

There is no csharplang proposal for this feature. The row of `docs/Language Feature Status.md` is "Non-virtual static
interface members without DIM runtime support", on branch `main`, in state "C# 15", and it links to
https://github.com/dotnet/roslyn/pull/83097, merged on 2026-04-10 into the branch `features/Unions` by AlekseyTs,
with no language design meeting champion. The resource string of `IDS_FeatureStaticMembersInInterfaces` is "static
members in interfaces".

- Syntax: no new syntax.
- Lowering: no new lowering. The members are ordinary static interface members.
- Restrictions: when `ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation` is false, which is the case
  for a `net472` or a `netstandard2.0` target, a non-virtual static method with a body or with an `extern` body
  (`SourceMemberMethodSymbol.cs` lines 1086 to 1091), a static field (`SourceMemberFieldSymbol.cs` lines 404 to 414)
  or a static field-like event (`SourceFieldLikeEventSymbol.cs`) declared in an interface now goes through
  `ReportLackOfRuntimeSupportForStaticMembersInInterfaces`, which reports the language version check instead of
  `ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation`. A protected, protected internal or private protected
  member still reports `ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember`. Instance members with bodies and
  static virtual or abstract members keep their previous errors.
- Diagnostics: only the language version check. The union feature relies on static interface members, which is why
  this feature ships with C# 15.

### Unsafe evolution, which is not part of C# 15

Unsafe evolution introduces the expression form `unsafe(expression)`, whose node is `UnsafeExpressionSyntax`, of kind
`UnsafeExpression`, value 8769, together with the token kind `SafeKeyword`, value 8454. Its language version gate,
`IDS_FeatureUnsafeEvolution`, returns `LanguageVersion.Preview` on `main`, and the comment in `MessageID.cs` states
the intention to keep the feature in preview until C# 16. `UnsafeExpressionSyntax` is the only node that still
carries `ExperimentalUrl` in the grammar of `main`, and the associated symbol API, that is
`ISymbol.RequiresUnsafeContext`, `IModuleSymbol.MemorySafetyRulesVersion`, the `MemorySafetyRulesVersion` enumeration
and the two members of `CSharpCompilationOptions`, is marked `[RSEXPERIMENTAL006]` on `main`. The feature is
therefore outside the scope of the 2027.0 release.

## Pre-compilation source outputs

Pre-compilation source outputs are a source generator phase, not a language feature and not an analyzer API. The
document is `docs/features/pre-compilation-source-outputs.md` on `main`, and the API was merged on 2026-05-20 by
dotnet/roslyn#83088. `IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput` registers a phase
that runs after `RegisterPostInitializationOutput` and before `RegisterSourceOutput`. Its sources are added to the
initial compilation, so the standard phase and the semantic model of every generator observe them. Its input
providers may not depend on the compilation or on syntax nodes; additional files, parse options and analyzer
configuration options are allowed. Reading a value that depends on the compilation throws
`InvalidOperationException` and puts the generator into an error state. `PreCompilationSourceProductionContext`
exposes `AddSource` and `CancellationToken` only, and reports no diagnostic. The stated use cases are Razor, which
would no longer need an intermediate compilation, and dependencies between generators.

## The local inventory of switches and visitors

The inventory covers every `.cs` file under `/home/user/Metalama/Metalama.Framework/src`, outside the `bin` and `obj`
directories, on branch `topic/2027.0/26-09-03-update-eng-7e3j07`. A script located every switch statement and switch
expression, classified it by the enumeration names that appear in its case labels or arm patterns, and recorded
whether the default arm begins with `throw`. A switch over a tuple is counted once per enumeration. Visitor classes
were found by matching class declarations whose base type is one of the visitor types listed below.

The classification is heuristic in one respect that matters when the numbers are read. It attributes a switch to an
enumeration by the names in its labels, so switches over Metalama's own `Metalama.Framework.Code.TypeKind` and
`Metalama.Framework.Code.MethodKind` enumerations, which share member names with the Roslyn enumerations, are counted
together with the Roslyn ones. A switch whose labels are written through a helper property, such as
`SyntaxKindExtensions.IsRecordDeclaration`, is not detected at all.

The per-file lists are in [`analysis-reports/08-roslyn-api-delta.md`](analysis-reports/08-roslyn-api-delta.md), in
the sections "Switches with a throwing default arm", "Switches and expressions with an explicit list of type
declaration kinds" and "Visitor classes per folder and per base class".

### Switch statements and expressions per enumeration

| Enumeration | Switches | Of which the default arm throws | Changed between 5.0 and `main` |
| --- | --- | --- | --- |
| `SyntaxKind` | 156 | 74 | Yes: `UnionKeyword`, `ClosedKeyword`, `SafeKeyword`, `WithElement`, `UnionDeclaration`, `UnsafeExpression` |
| `SymbolKind` | 112 | 38 | No |
| `MethodKind` | 48 | 22 | No |
| `TypeKind` | 17 | 12 | No |
| `OperationKind` | 0 | 0 | Yes: `CollectionExpressionElementsPlaceholder` |
| Total | 326 | 146 | |

Because a union has `TypeKind.Struct` and a closed class has `TypeKind.Class`, no throwing arm of a `TypeKind` switch
is reached by the new features. Because `SymbolKind` and `MethodKind` are unchanged, the same holds for those two.

### Visitor classes per base class

| Base class | Classes | Declared in |
| --- | --- | --- |
| `SafeSyntaxRewriter` | 49 | `Metalama.Framework.Sdk/Utilities/Roslyn`, deriving from `CSharpSyntaxRewriter` |
| `SafeSyntaxWalker` | 25 | `Metalama.Framework.Sdk/Utilities/Roslyn`, deriving from `CSharpSyntaxWalker` |
| `SymbolVisitor` | 12 | Roslyn |
| `CSharpSyntaxWalker` | 11 | Roslyn |
| `SafeSyntaxVisitor` and `SafeSyntaxVisitor<T>` | 8 | `Metalama.Framework.Sdk/Utilities/Roslyn`, deriving from `CSharpSyntaxVisitor` and `CSharpSyntaxVisitor<TResult>` |
| `CSharpSyntaxRewriter` | 7 | Roslyn |
| `CSharpSyntaxVisitor` | 5 | Roslyn |
| `MetaSyntaxRewriter` | 1 | `Metalama.Framework.Engine/Templating`, deriving from `SafeSyntaxRewriter` |
| Total | 118 | |

All 118 classes therefore inherit the new virtual members `VisitUnionDeclaration` and `VisitWithElement`. There is no
subclass of `OperationVisitor` or of `OperationWalker` in the repository.

Of the 12 subclasses of `SymbolVisitor`, 11 have a `DefaultVisit` override that throws and one returns null
(`Metalama.Framework.Sdk/CodeModel/SymbolExtensions.cs:262`). `SymbolVisitor` gained no member, because `SymbolKind`
gained no value, so no throwing `DefaultVisit` is reached by the new features.

### Sites that enumerate type-declaration kinds without `UnionDeclaration`

Every site below lists the kinds of a type declaration explicitly, and none of them lists `UnionDeclaration`. The
"Default arm" column says what the switch does with a kind that it does not list.

| Location | Kinds listed | Default arm |
| --- | --- | --- |
| `Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs:74` | Class, Struct, Interface, Enum, Delegate | Other |
| `Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:272` | Class, Struct, Interface, Record, RecordStruct, Enum, Delegate | Other |
| `Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:508` | Class, Struct, Interface, Record, RecordStruct, Enum, Delegate | Other |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:131` | Record, RecordStruct | Other |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:395` | Record, RecordStruct | Throws |
| `Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:578` | Class, Struct, Interface, Record, RecordStruct, ExtensionBlock | Throws |
| `Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:295` | Record, RecordStruct | Throws |
| `Metalama.Framework.Engine/Linking/LinkerSyntaxHandler.cs:30` | Record, RecordStruct | Throws |
| `Metalama.Framework.Engine/Linking/SymbolExtensions.cs:23` | Class, Struct, Interface, Record, RecordStruct, Enum, Delegate | Throws |
| `Metalama.Framework.Engine/SyntaxGeneration/ContextualSyntaxGenerator.cs:793` | Class, Struct, Interface, Enum, Delegate, each with its own attribute list | Throws |
| `Metalama.Framework.Sdk/AssertionFailedInterpolatedStringHandler.cs:374` | Class, Struct, Interface, Record, RecordStruct, Enum | None |
| `Metalama.Testing.AspectTesting/TestResult.cs:520` | Class, Struct, Interface, Record, RecordStruct, Enum, Delegate | Throws |
| `Metalama.Testing.AspectTesting/TestSyntaxTree.cs:192` | Class, Struct, Interface, Record, RecordStruct, Enum, Delegate | Throws |

The same enumeration appears outside switches, in pattern lists and in boolean helpers. The two helpers that most
other code calls are `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33` (`IsTypeDeclaration`,
listing Class, Struct, Interface, Record and RecordStruct), `:41` (`IsBaseTypeDeclaration`, which adds Enum and
Delegate) and `:100` (`IsRecordDeclaration`). The other sites are
`Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:33`, `:61` and `:116`;
`Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:198-199`;
`Metalama.Framework.Engine/CodeModel/Source/SourceConstructor.cs:124`;
`Metalama.Framework.Engine/Linking/LinkerLateTransformationRegistry.cs:149`, `:152` and `:190`;
`Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs:45` and `:65`;
`Metalama.Framework.Engine/Linking/Inlining/ImplicitLastOverrideReferenceInliner.cs:69`;
`Metalama.Framework.Engine/Linking/LinkerAnalysisStep.AspectReferenceCollector.cs:203`;
`Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:244` and `:418`;
`Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:908`;
`Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:323`;
`Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:641`;
`Metalama.Framework.Engine/Linking/SymbolExtensions.cs:29`;
`Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:510-511` and `:820`;
`Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:400`; and
`Metalama.Framework.Engine/SyntaxGeneration/ContextualSyntaxGenerator.cs:798-802`.

Twenty production visitors and three test runners override `VisitClassDeclaration`, `VisitStructDeclaration` or
`VisitRecordDeclaration`, and therefore treat a union declaration through `DefaultVisit` rather than through the
override. The production files are `Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesHasher.cs` and
`PartialTypesVisitor.cs`; `Metalama.Framework.Engine.Analyzers/MetalamaInternalsAnalyzer.PublicApiVisitor.cs`;
`Metalama.Framework.Engine/CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredAndAttributeTypesVisitor.cs` and
`DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs`;
`Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.CollectSerializableFieldsVisitor.cs`,
`CompileTimeCompilationBuilder.CollectSerializableTypesVisitor.cs`,
`CompileTimeCompilationBuilder.EmbeddedAttributeDetectorVisitor.cs`,
`CompileTimeCompilationBuilder.EmbeddedAttributeRemover.cs`,
`CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs`,
`CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs` and `CompileTime/RunTimeAssemblyRewriter.cs`;
`Metalama.Framework.Engine/Formatting/TextSpanClassifier.cs`;
`Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs` and
`Linking/LinkerLinkingStep.LinkingRewriter.cs`, both of which also override `VisitExtensionBlockDeclaration`;
`Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs`;
`Metalama.Framework.Engine/Templating/TemplateAnnotator.cs` and `Templating/TemplatingCodeValidator.Visitor.cs`;
`Metalama.Framework.Sdk/AssertionFailedInterpolatedStringHandler.cs`; and
`Metalama.SourceTransformer/SourceTransformer.cs`. The test runners are
`tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestRewriter.cs` and
`LinkerTestInputBuilder.TestTypeRewriter.cs` and `tests/Metalama.Framework.Tests.UnitTests/DesignTime/FallbackTests.cs`.
The count of twenty and three was re-established for this document on 2026-09-04; the report states seventeen in one
of its table cells, which is superseded by the list that the same report gives.

One further join point is the modifier list. A `closed` modifier is present only as a token in the `Modifiers` list
of a class declaration, and `ISymbol` exposes it only through `ITypeSymbol.IsClosed`. The files that rebuild modifier
lists are `Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs`,
`Metalama.Framework.Engine/Utilities/Roslyn/SymbolModifiersHelper.cs` and
`Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs`, that is the files that name
`SyntaxKind.SealedKeyword`.

## What could not be verified

- The consumed package `Microsoft.CodeAnalysis 5.10.0-1.26365.3` was not read. It is not in the local NuGet cache and
  no `NuGet.config` naming its feed was found under `/home/user/Metalama` at a depth of three directories. Its API is
  pinned indirectly, by the four coincidences listed above. A change to an experimental attribute that touched none
  of `PublicAPI.Unshipped.txt`, `LanguageVersion.cs` and `Syntax.xml` between 2026-06-24 and 2026-07-31 cannot be
  excluded by this method.
- The date 2026-07-01 of commit `35d9211b` is the date that GitHub shows for the identical commit on `main`. The
  branch `release/insiders` that the package manifest names is not on GitHub, and the build date 2026-07-07 was
  decoded from the Arcade build number 26357. The publication dates of the stable packages come from nuget.org.
- The Roslyn version of the .NET 11 software development kit at general availability is not published. The statements
  that the next stable package is 5.12 and that it ships in November 2026 follow from the nuget.org cadence and from
  the version bumps of `main`, not from an announcement.
- The GitHub REST API and the patch view of a commit returned the status 403 for this session, both through the web
  fetch tool and through `curl`, and the GitHub tools other than code search refuse the `dotnet/roslyn` repository.
  Branches and tags were therefore enumerated with `git ls-remote` and the commit identifiers of the version bumps
  were read from the web page of the history of `eng/Versions.props`.
- The attribute `CompilerFeatureRequired("ClosedClasses")` on the constructors of a closed class is stated by the
  proposal only. The corresponding synthesis was not located in `SourceNamedTypeSymbol.cs`, where only the synthesis
  of `IsClosedTypeAttribute` was read.
- Whether a partial declaration of a closed class must repeat the `closed` modifier is addressed neither by the
  proposal nor by the compiler sources that were read.
- The representation of the case types inside `UnionDeclarationSyntax.ParameterList`, in particular whether each
  `ParameterSyntax` carries an identifier, was not verified. Only the shape of the public API was read.
- The body of dotnet/roslyn#83097 could not be rendered. The description of the static members in interfaces feature
  comes from `SourceMemberMethodSymbol.cs`, `SourceMemberFieldSymbol.cs` and a code search hit in
  `SourceFieldLikeEventSymbol.cs`.
- The GitHub code search index was at commit `f5098787d20f8016bd1abe729ca8b76d9f9ac694`, while the raw files were read
  from `main` on 2026-09-03. The two may differ by a few hours of commits.
- `Metalama.Compiler` is not cloned. Its Roslyn base version, and whether it ships from the 5.10 window or from a
  later state, are not known to this document.
- The local switch classification is heuristic, in the two respects stated at the beginning of the inventory section.
