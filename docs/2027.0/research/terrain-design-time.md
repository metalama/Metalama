# Terrain map: design time and cross-process

Subsystem: `Metalama.Framework/src/Metalama.Framework.DesignTime/**`,
`Metalama.Framework.DesignTime.Contracts/**`, `Metalama.Framework.DesignTime.Rpc/**`,
`Metalama.Framework.CompilerExtensions/**`, plus the two design documents
`Metalama.Framework/docs/cross-process-communication.md` and `Metalama.Framework/docs/design-time-memory.md`.

Repository root for every relative path below: `C:/src/Metalama-2027.0/Metalama`, unless the path is
explicitly prefixed with `C:/src/Metalama-2027.0/Metalama.Premium`.

Two neighbouring files outside the nominal folder list are covered here because they *are* the design-time
behaviour and because the C# 14 wave landed in one of them:

- `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs`
  (the generator of the partial-class stubs the integrated development environment sees);
- `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs` and
  `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs`
  (the extension loader and its target-framework name literals, named by `docs/platform-support.md`).

---

## 0. Headline conclusions

1. **The design-time assemblies themselves are almost entirely blind to the shape of the C# language.**
   A grep for `SyntaxKind`, `CSharpSyntaxVisitor`, `TypeDeclarationSyntax`, `DeclarationKind`, `TypeKind`
   and `LanguageVersion` across `Metalama.Framework.DesignTime`,
   `Metalama.Framework.DesignTime.Contracts` and `Metalama.Framework.DesignTime.Rpc` returns hits in only
   four files: `Pipeline/Diff/PartialTypesVisitor.cs`, `Pipeline/Diff/PartialTypesHasher.cs`,
   `Refactoring/CSharpAttributeHelper.cs` and `CodeFixes/TheCodeFixProvider.cs`. Everything else works on
   `ISymbol`, `SemanticModel`, file paths and serialized identifiers.
2. **A grep for `ExtensionBlock` / `IsExtension` across the three design-time projects returns nothing.**
   The C# 14 wave produced *no* code in this subsystem. Its design-time work landed entirely in
   `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs` plus design-time
   aspect-test baselines. That is the pattern C# 15 will repeat.
3. **The version sensitivity of this subsystem is concentrated, not diffuse.** Roughly twenty places carry
   the whole of it, and they cluster into five groups: `ResourceExtractor` and `RoslynVariantPolicy`; the
   private-reflection bridges into Roslyn internals; the process-tree and process-name detection; the
   frozen `[Guid]` contract surface; and the generated code hashers.
4. **The dominant failure mode of this subsystem is silence.** `docs/platform-support.md` lines 22 to 28
   states it: a design-time payload that fails to load produces no diagnostic at all, and issue #1710 was
   diagnosed only after finding 8396 silently logged exceptions. Section 5 below lists thirteen concrete
   silent paths.

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 `Pipeline/Diff/PartialTypesVisitor.cs` — closed enumeration of type declarations

`Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesVisitor.cs`

- Line 12: `internal sealed class PartialTypesVisitor : CSharpSyntaxVisitor<ImmutableArray<BaseTypeDeclarationSyntax>>`
- Lines 38, 40, 42: overrides only `VisitClassDeclaration`, `VisitStructDeclaration`, `VisitRecordDeclaration`.
- Lines 44 to 57: overrides `VisitGlobalStatement`, `VisitUsingDirective`, `VisitDelegateDeclaration`,
  `VisitEnumDeclaration`, `VisitMethodDeclaration` to return empty, purely as a recursion cut-off.
- Lines 59 to 67: `VisitBaseTypeDeclaration` tests `type.Modifiers.Any( SyntaxKind.PartialKeyword )`.
- Lines 18 to 36: `DefaultVisit` recurses into children.

The set `{class, struct, record}` is a hard-coded enumeration of the type declarations that can be partial.
`InterfaceDeclarationSyntax` is already absent, and so is `ExtensionBlockDeclarationSyntax` (C# 14).
`UnionDeclarationSyntax` derives from `TypeDeclarationSyntax` (see
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` line 1954,
`<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" …>`), so a `partial union` falls into
`DefaultVisit` and is silently not recognised as a partial type.

### 1.2 `Pipeline/Diff/PartialTypesHasher.cs` — the same enumeration, duplicated

`Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesHasher.cs`

- Line 15: `internal sealed class PartialTypesHasher : CSharpSyntaxVisitor<int?>`
- Lines 43, 45, 47: the same three overrides.
- Lines 49 to 57: the same five empty overrides.
- Lines 59 to 67: `type.Modifiers.Any( SyntaxKind.PartialKeyword )`, hashing `type.Identifier`.

The two visitors must agree: `DiffStrategy.FindPartialTypes` uses the hasher as a fast path and the visitor
as the slow path (`Pipeline/Diff/DiffStrategy.cs` lines 112 and 129). Adding a type declaration kind to one
and not the other produces a stale partial-type key set rather than an error.

### 1.3 `Refactoring/CSharpAttributeHelper.cs` — exhaustive `SyntaxKind` switch with a silent default

`Metalama.Framework/src/Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs`

- Line 28: `oldNode.IsKind( SyntaxKind.VariableDeclarator )` special case for fields.
- Line 71: `forAssembly: oldNode.IsKind( SyntaxKind.CompilationUnit )`.
- Lines 74 to 191: `switch ( oldNode.Kind() )` over exactly nineteen kinds:
  `MethodDeclaration` (76), `DestructorDeclaration` (81), `ConstructorDeclaration` (86),
  `InterfaceDeclaration` (91), `DelegateDeclaration` (96), `EnumDeclaration` (101),
  `ClassDeclaration` (106), `StructDeclaration` (111), `Parameter` (116), `PropertyDeclaration` (121),
  `EventDeclaration` (126), `AddAccessorDeclaration`/`RemoveAccessorDeclaration`/`GetAccessorDeclaration`/
  `SetAccessorDeclaration` (131 to 134), `OperatorDeclaration` (139), `ConversionOperatorDeclaration` (144),
  `IndexerDeclaration` (149), `FieldDeclaration` (154), `EventFieldDeclaration` (159),
  `CompilationUnit` (164).
- Lines 189 to 190: `default: return null;`

Missing already: `RecordDeclaration`, `RecordStructDeclaration`, `ExtensionBlockDeclaration` (C# 14),
`InitAccessorDeclaration`, `FileScopedNamespaceDeclaration`. `UnionDeclaration` would join that list.
The switch is by `SyntaxKind` and not by base type, so a new type declaration kind falls to `default`.

- Lines 266 and 272: `SyntaxFactory.ParseExpression( a )` / `ParseExpression( property.Value )` parse the
  attribute arguments with the *default* parse options of the loaded Roslyn, not the project's
  `CSharpParseOptions`. An argument expression that uses a construct the project's `LangVersion` forbids is
  accepted here and rejected by the compiler afterwards.

### 1.4 `CodeFixes/TheCodeFixProvider.cs` — the one place that is written against the base type

`Metalama.Framework/src/Metalama.Framework.DesignTime/CodeFixes/TheCodeFixProvider.cs`

- Lines 187 to 193:

  ```csharp
  private static BaseTypeDeclarationSyntax? GetTypeDeclaration( SyntaxNode node )
      => node switch
      {
          BaseTypeDeclarationSyntax typeDeclaration => typeDeclaration,
          { Parent: { } parent } => GetTypeDeclaration( parent ),
          _ => null
      };
  ```

- Line 173: `typeDeclaration.AddModifiers( SyntaxFactory.Token( SyntaxKind.PartialKeyword ) )`.

This is the model to follow: it matches on `BaseTypeDeclarationSyntax`, so `UnionDeclarationSyntax` and
`ExtensionBlockDeclarationSyntax` are handled with no edit. The `partial` modifier it inserts is placed by
`AddModifiers`, which appends after the existing modifiers; a new modifier such as `closed` that must
precede `partial` would be ordered wrongly but would still compile.

### 1.5 `DiagnosticAnalysis/TheDiagnosticAnalyzer.cs` — token-level location remapping

`Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticAnalysis/TheDiagnosticAnalyzer.cs`

- Lines 420 to 486: `TryMapLocation` maps a diagnostic reported on an older syntax tree onto the current
  one. Line 446 `FindNode`, line 448 `NodeFinder.TryFindOldNodeInNewTree`
  (`Metalama.Framework.Engine/Utilities/Roslyn/NodeFinder.cs`), line 459 `oldNode.FindToken`, line 460
  `newNode.ChildTokens().SingleOrDefault( t => t.Text == oldToken.Text )`, line 462
  `newToken.IsKind( SyntaxKind.None )`.

Matching a token *by its text* among the direct child tokens of a node is shape-sensitive: a node that
gains a second token with the same text (for example a `BreakStatementSyntax` whose new `Name` field
carries an identifier equal to some other child token's text) makes `SingleOrDefault` return `default`, and
the diagnostic is dropped with a warning in the log only (line 451, line 465).

### 1.6 The generated code hashers — the real language-shape machine of this subsystem

Generated into `Metalama.Framework/.generated/<roslyn version>/Metalama.Framework.DesignTime/`:
`RunTimeCodeHasher.g.cs` and `CompileTimeCodeHasher.g.cs`. The directory is git-ignored
(`.gitignore` line 62 `.generated`); the copies on disk today are `4.12.0` and `5.0.0` and are stale local
artifacts.

- Generator entry point: `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs`
  - Line 17: `string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0"];`
  - Line 18: `string[] versionNames = [.. legacyVersionNames, "5.0.0", "5.10.0"];`
  - Lines 46 and 47: `generator.GenerateHasher( … "RunTimeCodeHasher.g.cs", "RunTimeCodeHasher", false )`
    and `… "CompileTimeCodeHasher.g.cs", "CompileTimeCodeHasher", true`.
- Generator body: `eng/src/GenerateMetaSyntaxRewriter/Generator.cs` lines 615 to 712.
  - Line 635: `var nodes = this._syntax.Types.OfType<Node>();` — one `Visit<Node>` override per node in the
    grammar snapshot, and nothing else.
  - Lines 645 to 685 `ProcessField`: `SyntaxToken` fields become `VisitTrivialToken` or
    `VisitNonTrivialToken`; every other field becomes `this.Visit( node.<Field> )`.
  - Lines 714 to 723 `IgnoreFieldContentInRunTimeCode`: `BlockSyntax`, `ArrowExpressionClauseSyntax` and
    `EqualsValueClauseSyntax` are hashed only for null-ness in the run-time hasher (line 660
    `this.HashValue( node.<Field> is null );`). This is what makes editing a method body not invalidate the
    design-time pipeline.
  - Lines 725 to 735 `IsTrivialToken`: only `StringLiteralToken`, `CharacterLiteralToken`,
    `NumericLiteralToken` and `IdentifierToken` hash their text; every other token hashes its `RawKind`
    only.
- Base class: `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/BaseCodeHasher.cs`
  lines 19 to 80, deriving from `SafeSyntaxWalker`
  (`Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs` lines 35 to 73),
  whose `VisitCore` calls `base.Visit( node )`, that is `CSharpSyntaxWalker.DefaultVisit`, which recurses
  into children but hashes nothing.

Consequences for the four Roslyn 5.10 grammar additions:

| Grammar addition | Effect on the generated hashers |
| --- | --- |
| `UnionDeclarationSyntax` (`Syntax-5.10.0.xml` line 1954) | The latest variant gains `VisitUnionDeclaration`; the Roslyn 5.0 variant does not. |
| `UnsafeExpressionSyntax` (line 496) | Same. |
| `WithElementSyntax` (line 816) | Same. |
| `Name` on `BreakStatementSyntax` (line 1290) and `ContinueStatementSyntax` (line 1301) | The field is `SyntaxToken`-typed identifier data inside the generated `VisitBreakStatement` body. The 5.0 variant's generated method does not read it. |

The grammar snapshots are `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml` and
`Syntax-5.10.0.xml`; the node-name delta is exactly the three new node types above.

### 1.7 `Metalama.Framework.Engine/CompileTime/CompileTimeCodeFastDetector.cs` — used only by the design-time differ

- Lines 41 to 84, `DetectCompileTimeVisitor : SafeSyntaxVisitor<bool>`.
- Line 45 `VisitUsingDirective`, line 77 `VisitNamespaceDeclaration`, line 79
  `VisitFileScopedNamespaceDeclaration`, line 81 `VisitCompilationUnit`, line 83
  `public override bool DefaultVisit( SyntaxNode node ) => false;`

This closed enumeration of the three containers that can hold a using directive decides, in
`DiffStrategy.IsDifferent` (line 74) and `DiffStrategy.GetSyntaxTreeVersion` (line 155), whether a file is
hashed with `CompileTimeCodeHasher` (which hashes method bodies) or `RunTimeCodeHasher` (which does not).

### 1.8 `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs` — what the editor sees

This is where C# 14 landed and where C# 15 will land.

- Lines 55 to 57: the generated tree inherits `CSharpParseOptions` from the first existing syntax tree,
  falling back to `CSharpParseOptions.Default`, because Roslyn rejects a compilation with inconsistent
  language versions.
- Lines 65 to 78: the transformation bucket switch, `_ => throw new AssertionFailedException(…)` at line 75.
- Lines 113 to 127: `switch ( target.DeclarationKind )`, with
  `case DeclarationKind.NamedType or DeclarationKind.ExtensionBlock when target is INamedType namedType:`
  at line 115. **This is the C# 14 edit**: a new `DeclarationKind` enum member added to the code model and
  admitted here. `default:` at line 125 throws `AssertionFailedException`.
- Lines 247 to 280: the extension-block indentation and wrapping path
  (`hasExtensionBlock`, `CreateExtensionBlock`). Also a C# 14 addition.
- Lines 381 to 388: `if ( current.TypeKind == TypeKind.Extension )` — the containing-type walk special-cases
  the extension `TypeKind`.
- Lines 456 to 504 `IndentMember`: `switch ( member.Kind() )` over `MethodDeclaration`,
  `ConstructorDeclaration`, `DestructorDeclaration`, `OperatorDeclaration`,
  `ConversionOperatorDeclaration`. A member kind absent from the list is simply not brace-indented.
- Lines 506 to 523 `AddPartialModifierToTypes`:
  `member.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or
  SyntaxKind.InterfaceDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration`
  (lines 510 to 511). A new type declaration kind introduced into the generated file does not get its
  `partial` modifier.
- Lines 662 to 695: `CreateExtensionBlock`, `CreateExtensionBlockParameterList`. C# 14 additions.
- Lines 697 to 790 `CreatePartialType`: `switch` on `type.TypeKind` with six arms
  (`Class` non-record 722, `Class` record 734, `Struct` non-record 749, `Struct` record 761,
  `Interface` 776) and `_ => throw new AssertionFailedException( $"Unknown type kind: {type.TypeKind}." )`
  at line 788. **This is the single most important extension point for a new kind of type declaration.**
- Lines 792 to 815 `CreateTypeParameters`, with a `VarianceKind` switch that throws on an unknown value
  (line 808).
- Lines 817 to 823 `AddHeader`:
  `NamespaceDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or
  RecordDeclarationSyntax or InterfaceDeclarationSyntax => node.WithLeadingTrivia( GetHeader() ), _ => node`.
  A new declaration kind silently loses the "Generated by Metalama" header.

### 1.9 Language-version gating: absent at design time by design

- `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs`
  lines 62 to 93, `VerifyLanguageVersion`, reporting `LAMA0051`
  (`PreviewCSharpVersionNotSupported`, `Diagnostics/GeneralDiagnosticDescriptors.cs` lines 235 to 242) and
  `LAMA0052` (`CSharpVersionNotSupported`, lines 244 to 251).
- Lines 64 to 65 of that file state the rule verbatim:

  > Note that Roslyn does not properly set the language version at design time, so we don't check the
  > language version in other pipelines.

  There is no counterpart in `Metalama.Framework.DesignTime` or in
  `Metalama.Framework.Engine/Pipeline/DesignTime/`. A grep for `LanguageVersion` across the design-time
  pipeline returns nothing.
- `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs`
  - Lines 31 to 32: `public static LanguageVersion Latest => LanguageVersion.CSharp14;`
  - Lines 38 to 43: `All` = CSharp14, 13, 12, 11, 10.
  - Lines 52 to 62 `ToLanguageVersion`: `V5_0_0 => CSharp14`, `V5_10_0 => CSharp14`,
    `_ => throw new AssertionFailedException(…)`.
  - Lines 77 to 87 `ToNuGetVersionString`: `V5_10_0 => "5.10.0-1.26365.3"`.
  - Lines 134 to 144 `ToVersion`.
  - Lines 149 to 159 `GetMaxLanguageVersion`: `(>= 5, _) => AllLanguageVersions.CSharp14`.

---

## 2. Files and types sensitive to the .NET runtime, the .NET SDK, Roslyn, or the host

### 2.1 `ResourceExtractor` — the Desktop/Core selection and the Roslyn-version detection

`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs`

| Lines | What |
| --- | --- |
| 31 | `_designTimeContractsAssemblyName = "Metalama.Framework.DesignTime.Contracts.v2"` — the physical assembly name that encodes the contract generation. |
| 35 to 36 | `_isNetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.OrdinalIgnoreCase )`. The whole Desktop/Core selection, with no version fallback. |
| 54, 77 | `HostRoslynVersion { get; }` and its initialisation. |
| 79 | `_variantName = RoslynVariantPolicy.TryGetVariantName( HostRoslynVersion, out var variantName ) ? variantName : null;` |
| 83 | `GetTempDirectory` appends `_isNetFramework ? "desktop" : "core"`, so the two flavours never share an extraction directory. |
| 89 to 108 | `GetTempBaseDirectory`, `RuntimeInformation.IsOSPlatform( OSPlatform.Windows )` and the Unix per-user directory (issue #1650). |
| 157 to 172 | `TryCreateInstance<T>`: returns `false` and calls `ReportUnsupportedHost` when `_variantName == null`. |
| 180 to 211 | `ReportUnsupportedHost`: writes `unsupported-roslyn-<version>.txt` into the crash-reports directory. Records `RuntimeInformation.FrameworkDescription`, `ProcessKindHelper.CurrentProcessKind`, `Environment.CommandLine`. |
| 244 | `assemblyName = assemblyName + "." + _variantName;` — the variant name is a *suffix of the assembly simple name*, not a directory. |
| 466 to 485 | `GetEmbeddedAssemblies`: resource prefix `Metalama.Framework.CompilerExtensions.Resources.{(_isNetFramework ? "Desktop" : "Core")}.` |
| 539 to 603 | `GetAssemblyCore`. Lines 579 to 583: the contracts assembly is loaded with `Assembly.LoadFile` (outside any assembly load context) when `ProcessKindHelper.CurrentProcessKind is ProcessKind.DevEnv or ProcessKind.Rider`, so that COM type equivalence works (issue #1626); every other process uses the assembly load context, because `Assembly.LoadFile` broke DevHub (issue #1461). |
| 605 to 631 | `GetAlreadyLoadedAssembly`, delegating the rules to `AssemblyResolutionPolicy`. |
| 633 to 656 | `GetHostRoslynVersion`: reads `typeof(SyntaxNode).Assembly.GetName().Version`, and when it equals `new Version( 42, 42, 42, 42 )` (the JetBrains build marker) parses `AssemblyInformationalVersionAttribute` up to the first `-`. |

### 2.2 `RoslynVariantPolicy` — the variant table

`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs`

- Line 22: `MinimumSupportedRoslynVersion { get; } = new Version( 5, 0 );` — must mirror
  `RoslynApiMinVersion` in `Directory.Packages.props`.
- Lines 30 to 54 `TryGetVariantName`:
  - Line 32: `if ( roslynVersion >= new Version( 5, 10 ) ) { variantName = "5.10.0"; return true; }`
  - Line 38: `else if ( roslynVersion >= MinimumSupportedRoslynVersion ) { variantName = "5.0.0"; … }`
  - Lines 46 to 52: below the floor, `variantName = ""` and `false`, with the comment naming issue #1881.
- Tests: `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/RoslynVariantPolicyTests.cs`
  lines 21 to 79. Note lines 36 to 45, `LatestVersionSelectsThe5100Variant`, which asserts that Roslyn
  `5.11.0` and `6.0.0` are served by the `5.10.0` variant: the latest variant is the catch-all for any
  future Roslyn.

### 2.3 `AssemblyResolutionPolicy` — exact-version binding for embedded assemblies

`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/AssemblyResolutionPolicy.cs`

- Lines 24 to 25 `MatchesExactVersion`; lines 31 to 35 `MatchesSameOrHigherVersion` (with the .NET Framework
  null-`Version` caveat); lines 61 to 89 `SelectAlreadyLoadedAssembly`.
- Lines 51 to 60, remarks: an assembly embedded in the current build binds to the exact embedded version,
  because several builds of Metalama can be active in one process (issue #1833). Host-provided assemblies,
  Roslyn included, may roll forward.
- Compiled into both `Metalama.Framework.CompilerExtensions` and the unit-test project
  (`Metalama.Framework.Tests.UnitTests.csproj` lines 26 and 27); tests in
  `Metalama.Framework.Tests.UnitTests/Utilities/AssemblyResolutionPolicyTests.cs`.

### 2.4 `ProcessKindHelper` — host process names

`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs`

- Lines 19 to 58, `switch ( Process.GetCurrentProcess().ProcessName.ToLowerInvariant() )`:
  `"devenv"` (21), `"servicehub.roslyncodeanalysisservice"` / `"servicehub.roslyncodeanalysisservices"` /
  `"devhub"` (24 to 26), `"csc"` / `"vbcscompiler"` (29 to 30), `"dotnet"` (33) with command-line probes
  for `jetbrains.resharper.roslyn.worker`, `jetbrains.roslyn.worker` (37 to 38), `vbcscompiler.dll` /
  `csc.dll` (42), `dotnet-format.dll` (46), and `default: return ProcessKind.Other;` (56 to 57).
- Line 16 to 17 warns that the same logic is duplicated in
  `Metalama.Backstage.Utilities.ProcessUtilities` and cannot be shared.
- Enum at lines 62 to 70.

The Visual Studio Code C# Dev Kit language server (`Microsoft.CodeAnalysis.LanguageServer`) is not in the
list and falls into `ProcessKind.Other`.

### 2.5 The entry-point shims — per-process-kind dispatch

All in `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/`, plus two in
`Metalama.Framework/src/Metalama.Framework.EditorExtensions/`.

| File | Lines | Notes |
| --- | --- | --- |
| `MetalamaDiagnosticAnalyzer.cs` | 22 to 58 | `switch ( ProcessKindHelper.CurrentProcessKind )`, four arms plus `default`. |
| `MetalamaSourceGenerator.cs` | 18 to 60 | Same, plus an early return when `MetalamaCompilerInfo.IsActive` (line 20). |
| `MetalamaDiagnosticSuppressor.cs` | 19 to 51 | `DevEnv` deliberately gets no implementation (line 34). |
| `MetalamaGeneratedCodeAnalyzer.cs` | 20 to 35 | Type name hard-coded: `"Metalama.Framework.Engine.GeneratedCodeAnalyzer"`. |
| `AdditionalDiagnosticAnalyzer.cs` | 22 to 28 | Type name hard-coded: `"Metalama.Framework.Engine.Analyzers.AdditionalDiagnosticAnalyzer"`. |
| `MetalamaSourceTransformer.cs` | 23 to 63 | Holds `LAMA0087` (lines 23 to 31) and reports it at line 52 when the implementation is null; the only entry point that fails loudly. |
| `EditorExtensions/MetalamaCodeFixProvider.cs` | 22 to 58 | Has a `ProcessKind.Rider` arm (line 42). |
| `EditorExtensions/MetalamaCodeRefactoringProvider.cs` | 87 to 123 | Same, but with the type names spelled inline (lines 101 to 118) instead of via `RoslynEntryPointTypeNames`. |

Entry-point type names: `Metalama.Framework/src/Metalama.Framework.DesignTime/RoslynEntryPointTypeNames.cs`
lines 17 to 32, compiled into `Metalama.Framework.CompilerExtensions.csproj` (line 35) and
`Metalama.Framework.EditorExtensions.csproj` (line 22), tested by
`Metalama.Framework.Tests.UnitTests/DesignTime/TestRoslynEntryPointTypeNames.cs` lines 26 to 41.

### 2.6 Target-framework literals

| File | Line | Literal |
| --- | --- | --- |
| `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj` | 6 | `<TargetFrameworks>net10.0;net472</TargetFrameworks>` |
| same | 25, 26 | `ProjectReference` to `Metalama.Framework.DesignTime.5.0.0` and `Metalama.Framework.DesignTime` — one project reference per shipped Roslyn variant. |
| `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/Metalama.Framework.CompilerExtensions.csproj` | 53, 56, 58 to 62, 70 | `…/bin/$(Configuration)/net472/…` |
| same | 54, 63, 64 | `…/bin/$(Configuration)/net10.0/…` |
| same | 65 to 69 | Comment recording that `System.Text.Json` is provided by the parent load context on .NET and that `System.Threading.AccessControl` is in the .NET 10 shared framework. |
| same | 4 | The shim itself is `netstandard2.0`. |
| `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj` | 6 | `<TargetFrameworks>net472;net10.0</TargetFrameworks>` |
| `Metalama.Framework/src/Metalama.Framework.DesignTime.Contracts/Metalama.Framework.DesignTime.Contracts.csproj` | 4 | `<TargetFrameworks>net472;net10.0</TargetFrameworks>` |
| `Metalama.Framework/src/Metalama.Framework.DesignTime.Rpc/Metalama.Framework.DesignTime.Rpc.csproj` | 4 | `netstandard2.0` — host-agnostic. |
| `Metalama.Framework/src/Metalama.Framework.EditorExtensions/Metalama.Framework.EditorExtensions.csproj` | 4 | `netstandard2.0`. |
| `Metalama.Framework/src/Metalama.Framework.Package/Metalama.Framework.Package.csproj` | 50 | `lib/net10.0/_._` |
| `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs` | 31 | `var targetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net10.0";` |
| `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs` | 19 to 20 | the same expression, as a static field `_targetFramework`. |
| `Metalama.Framework/docs/extensibility.md` | 19, 21, 25, 72, 131 to 136, 150, 225 to 236 | the same names, in the extension-author instructions. |
| `Metalama.Framework/src/Metalama.Extensions.DiffEngine/**/*.props`, `Metalama.Extensions.HtmlWriter/**/*.props` | throughout | `TargetFramework="net472"` / `TargetFramework="net10.0"` metadata on `MetalamaExtensionAssembly`. |

Note on `ExtensionLoaderBase.cs` line 31: the local `targetFramework` it computes is used only in the trace
message at line 33. The actual filter is `a.SatisfiesCurrentProcess` (line 36), which reads the static in
`TargetedAssemblyReference`. Two copies of the same expression must therefore be kept in step, and only one
of them has an effect.

`TargetedAssemblyReference.SatisfiesCurrentProcess` (lines 22 to 24):

```csharp
=> (this.TargetRoslynVersion == null || this.TargetRoslynVersion.Equals( RoslynApiVersion.Current.ToVersion() ))
   && (this.TargetFramework == null || this.TargetFramework == _targetFramework);
```

Both comparisons are *equality*, not "at least". An extension declaring `TargetRoslynVersion="5.0.0"` is
never loaded by the 5.10 variant, and one declaring `TargetFramework="net8.0"` is never loaded at all.

### 2.7 Private reflection into Roslyn internals

Three bridges, all with a null-returning fallback.

`Metalama.Framework/src/Metalama.Framework.DesignTime/Services/RemoteWorkspaceProvider.cs`

- Lines 26 to 28: finds the highest-versioned loaded assembly whose full name starts with
  `"Microsoft.CodeAnalysis.Remote.ServiceHub,"`.
- Line 39: `serviceHubAssembly.GetType( "Microsoft.CodeAnalysis.Remote.RemoteWorkspaceManager" )`.
- Lines 50 to 58: `Default` is looked up **both as a field (Roslyn 4) and as a property (Roslyn 5)**, with
  the two cases named in the comments. This is the shape of a Roslyn-version accommodation in this
  subsystem.
- Lines 69 to 74: `GetMethod( "GetWorkspace", … Type.EmptyTypes, null )`.
- Every failure path logs a warning and returns `false` (lines 32, 43, 62, 78, 89).

`Metalama.Framework/src/Metalama.Framework.DesignTime/Services/AnalysisProcessInvalidationService.cs`

- Line 44: `"Microsoft.CodeAnalysis.Diagnostics.IDiagnosticsRefresher"` from the assembly of
  `CompletionProvider`.
- Line 45: `GetMethod( "RequestWorkspaceRefresh", Type.EmptyTypes )`.
- Line 53: `"Microsoft.CodeAnalysis.Host.Mef.IMefHostExportProvider"` from the assembly of `Workspace`.
- Lines 55 to 58: the single-generic-argument, zero-parameter `GetExports` overload.
- Lines 69 to 84: an expression tree compiled to `Action<HostServices>`.
- Lines 47 to 50 and 60 to 63: on any miss, `return null`, and `_diagnosticsRefreshAction?.Invoke` at
  line 36 becomes a no-op.
- Line 67 records that the export is absent in Visual Studio and Rider and present only under the language
  server, so "no export" is a normal state and cannot be used as an error signal.

`Metalama.Framework/src/Metalama.Framework.DesignTime/Services/UserProcessInvalidationService.cs`

- Lines 58 to 63: `typeof(Workspace).GetMethod( "EnqueueUpdateSourceGeneratorVersion",
  BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(ProjectId), typeof(bool)], null )`.
- Lines 65 to 68: `return null` on a miss; line 50 `_updateSourceGeneratorsAction?.Invoke(…)`.

### 2.8 Host process-tree detection

`Metalama.Framework/src/Metalama.Framework.DesignTime/VisualStudio/ServiceHub/ServiceHubClientEndpoint.cs`

- Lines 50 to 83 `TryGetPipeName`:
  - Lines 59 to 66: `parentProcesses[0] == "Microsoft.ServiceHub.Controller"` and
    `parentProcesses[1] == "devenv"` — commented `// VS 2022.`
  - Lines 67 to 71: `parentProcesses[0] == "devenv"` — commented `// VS 2026.`
  - Lines 72 to 78: otherwise `Logger.Remoting.Error?.Log( "The process 'devenv' could not be found. " )`
    and `return false`.
- Introduced by commit `5146c0a252 FIX #1096 Incompatibilities with VS 2026.`, which is the precedent for
  how a new Visual Studio generation is absorbed here.

`Metalama.Framework/src/Metalama.Framework.DesignTime/VisualStudio/Rpc/PipeNameProvider.cs` lines 13 to 19:
the pipe name is `Metalama_{role}_{processId}_{hash of the package version}`, so two Metalama versions in
one Visual Studio session never share a pipe. `EndpointRole` in `VisualStudio/Rpc/EndpointRole.cs`.

### 2.9 The Roslyn variant projects

- `Metalama.Framework/src/Metalama.Framework.DesignTime.5.0.0/Metalama.Framework.DesignTime.5.0.0.csproj`:
  - Line 6: globs every `.cs` of `../Metalama.Framework.DesignTime`.
  - Line 8: `<Import Project="../../../eng/RoslynVersions/Roslyn.5.0.0.props" />`
  - Line 9: imports the base `.csproj` itself.
- `Metalama.Framework/src/Metalama.Framework.DesignTime.4.12.0/` still exists on disk but holds only
  `bin` and `obj`: the project file was removed by issue #1881 and the output directories are stale.
- `eng/RoslynVersions/Roslyn.5.0.0.props`: `ThisRoslynVersion` `5.0.0`,
  `ThisRoslynVersionProjectSuffix` `.5.0.0`, `SystemTextJsonVersion` `9.0.0`, no `DefineConstants`.
- `eng/RoslynVersions/Roslyn.5.10.0.props`: `ThisRoslynVersion` `$(RoslynApiMaxVersion)`,
  `ThisRoslynVersionNoPreview` `5.10.0`, empty suffix,
  `<DefineConstants>$(DefineConstants);ROSLYN_5_10_0_OR_GREATER</DefineConstants>`,
  `SystemTextJsonVersion` `10.0.11`.
- `eng/RoslynVersions/Latest.props`: imports `Roslyn.5.10.0.props` when `ThisRoslynVersion` is empty.

`Metalama.Framework.DesignTime.csproj` lines 33 to 36 wire the generated hashers:

```xml
<Compile Include="../../.generated/$(ThisRoslynVersionNoPreview)/Metalama.Framework.DesignTime/*.cs"
         Condition="Exists('../../.generated/$(ThisRoslynVersionNoPreview)')" />
<Compile Include="../../.generated/$(ThisRoslynVersionNoPreview)-stubs/Metalama.Framework.DesignTime/*.cs"
         Condition="!Exists('../../.generated/$(ThisRoslynVersionNoPreview)')" />
```

A missing generated directory falls back to a `-stubs` directory that does not exist in the tree either, so
the variant compiles with **no hasher at all**, and the abstract `BaseCodeHasher` derivations referenced by
`DiffStrategy` lines 76 and 157 fail to resolve. The condition is a build-time trap rather than a runtime
one, but it is version-name-driven: renaming `ThisRoslynVersionNoPreview` without regenerating breaks it.

Line 9: `<AssemblyName>Metalama.Framework.DesignTime.$(ThisRoslynVersionNoPreview)</AssemblyName>` — this is
where the `"Metalama.Framework.DesignTime" + "." + _variantName` convention of
`ResourceExtractor.CreateInstance` line 244 is honoured.

### 2.10 .NET SDK version sensitivity

`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs`

- Lines 31 to 43: `NETCoreSdkVersion` empty means "built by `msbuild.exe`", and the language version is
  then derived from the Roslyn beside MSBuild.
- Lines 45 to 72 `GetLanguageVersionFromDotNetSdk`:
  ```csharp
  var sdkSupportedVersion = version.Major switch
  {
      >= 10 => LanguageVersion.CSharp14,
      >= 9 => LanguageVersion.CSharp13,
      >= 8 => LanguageVersion.CSharp12,
      _ => throw new PlatformNotSupportedException( $"Unsupported .NET SDK version: {version}." )
  };
  ```
- Lines 74 to 123 `GetLanguageVersionFromMSBuild`: probes `<MSBuildBinPath>/Roslyn/Microsoft.CodeAnalysis.CSharp.dll`
  and its parent (the `amd64` case), then `SupportedCSharpVersions.GetMaxLanguageVersion( roslynVersion )`
  at line 111.
- MSBuild property names: `Metalama.Framework.Engine/Options/MSBuildPropertyNames.cs` lines 54
  (`NETCoreSdkVersion`), 55 (`MSBuildBinPath`), 24 (`TargetFramework`), 42
  (`MetalamaCompileTimeTargetFrameworks`), 58 (`TargetFrameworks`), and the corresponding entries of the
  "all properties" array at lines 74, 91, 101, 102, 105.

### 2.11 Contracts pinned against an old Roslyn

`Metalama.Framework/src/Metalama.Framework.DesignTime.Contracts/Metalama.Framework.DesignTime.Contracts.csproj`

- Lines 30 to 33:
  ```xml
  <!-- It is essential that the package version remains constant.-->
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" VersionOverride="4.0.1" PrivateAssets="all" />
  <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.Common" VersionOverride="4.0.1" PrivateAssets="all" />
  ```
- Line 35: `<!-- We must match the version used by the lowest version of Visual Studion supported by the VSX. -->`
  This comment is stale under PB-2027.0, which drops Visual Studio 2022 and sets `RoslynApiMinVersion` to
  `5.0.0`. The pin itself is nevertheless load-bearing: the contracts assembly must stay binary-frozen
  across Metalama versions, so the pin is about the *contract*, not about the host.
- Lines 38 to 43: `NuGetAuditSuppress` entries for the transitive advisories that the 4.0.1 pin drags in
  (issue #1876).
- Lines 16 to 27: the named-lock sources are compiled in rather than referenced, because the assembly may
  reference nothing.

`Metalama.Framework/src/Metalama.Framework.EditorExtensions/Metalama.Framework.EditorExtensions.csproj`
lines 14 to 17 reference Roslyn at `$(RoslynApiMinVersion)`, so this assembly binds against the *floor*,
not the latest.

### 2.12 The Premium repository is not yet on PB-2027.0

`C:/src/Metalama-2027.0/Metalama.Premium`, branch `topic/2027.0/1829-durable-and-immutable-contracts`.

- `src/Metalama.Extensions.CodeFixes.DesignTime/Metalama.Extensions.CodeFixes.DesignTime.csproj`
  line 6: `<TargetFrameworks>net472;net8.0</TargetFrameworks>` — still `net8.0`.
- `src/Metalama.Extensions.CodeFixes.DesignTime.4.12.0/` still present with its `.csproj`.
- `eng/RoslynVersions/` holds `Latest.props`, `Roslyn.4.12.0.props`, `Roslyn.5.0.0.props` — no `5.10.0`.

Under the framework's current `ExtensionLoaderBase` / `TargetedAssemblyReference` literals, a
`net8.0`-targeted design-time extension is never selected on .NET, and a `TargetRoslynVersion` of `4.12.0`
never equals `RoslynApiVersion.Current.ToVersion()`. Both mismatches are silent.

---

## 3. How the previous language wave (C# 14) was absorbed here

### 3.1 The evidence

`git log --grep` over issues #1034, #1035, #1036, #1094, #1105, #1108 to #1116, #1127, #1131, #1143, #1159,
#1160, intersected with paths under `Metalama.Framework.DesignTime*` and `Metalama.Framework.CompilerExtensions`,
returns **no commit touching those projects**. The intersection with the design-time *pipeline* returns:

| Commit | Issue | File |
| --- | --- | --- |
| `5a1ac3e5c4`, `6c9ffc219d`, `f374fce480`, `707522939d`, `1099dfba86`, `f776fd9af9` | #1159 introduce extension blocks | `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs` |
| `0bc242649a`, `c36340bbf9`, `6c41855702` | #1143 partial constructor parameter introduction | same file |
| `22697b6ba5` | #1036 extension member invokers | engine only |
| `30e21aea98` | #1127 contracts on extension-block receiver parameters | engine only |
| `aea7b2e5a2` and the `#1114` series | field keyword in property templates | engine only |

A separate commit, `836bc53035 #1119 Test suites are broken with legacy Roslyn versions`, is the precedent
for the *variant* dimension of a language wave: a test whose behaviour differs between Roslyn variants.

### 3.2 The pattern, stated

1. **The language change is absorbed in the code model and the engine, not in the design-time assemblies.**
   The design-time assemblies consume `DeclarationKind`, `ISymbol` and `SerializableDeclarationId`; as long
   as those grow to cover the new construct, the design-time assemblies need no edit.
2. **The one design-time file that changes is `DesignTimeSyntaxTreeGenerator.cs`**, because it *emits* C#.
   For C# 14 the edits were: admitting `DeclarationKind.ExtensionBlock` in the target switch (line 115),
   adding `CreateExtensionBlock` and `CreateExtensionBlockParameterList` (lines 662 to 695), adding the
   extension-block indentation depth (lines 247 to 280), and special-casing `TypeKind.Extension` in the
   containing-type walk (lines 381 to 388).
3. **A `@TestScenario(DesignTime)` aspect test is added with its generated-partial baselines.** For
   example
   `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/ExtensionMembers/ExtensionMembers_Introduce_DesignTime.cs`
   with `// @TestScenario(DesignTime)` at line 6, `// @RequiredConstant(NET8_0_OR_GREATER)` at line 7, and
   the baselines `ExtensionMembers_Introduce_DesignTime.0.i.cs` and `.1.i.cs` (the `.0.i.cs` shows the
   emitted `extension(global::System.Int32 test) { … }` block inside a `static partial class C`).
   Also `Tests/Aspects/CSharp14/PartialConstructor/PartialConstructor_IntroduceParameter_DesignTime.cs`
   with `.0.i.cs` and `.t.cs`, and
   `Tests/Aspects/DesignTime/IntroduceExtensionBlock.cs` / `IntroduceExtensionBlockIntoIntroducedClass.cs`
   with `.0.i.cs` and `.1.i.cs`.
4. **The tests are gated by a preprocessor constant, not by a Roslyn variant.** The C# 14 tests use
   `@RequiredConstant`. Where a *variant* difference is genuinely needed, the mechanism is
   `ROSLYN_5_10_0_OR_GREATER`, defined by `eng/RoslynVersions/Roslyn.5.10.0.props` line 24, and the props
   file itself states that "No production source branches on it": it exists for the two aspect tests whose
   expected output differs between variants.
5. **`SupportedCSharpVersions` moves last, and moves in three places at once.** `Latest` (line 31), `All`
   (line 38), `ToLanguageVersion` (lines 52 to 62). The design-time pipeline does *not* consult them.
6. **The contracts assembly does not move.** No C# 14 commit touched
   `Metalama.Framework.DesignTime.Contracts`, and `CurrentContractVersions.ContractVersion_1_0` is still 3.

### 3.3 The most recent wave in this subsystem is the platform wave, not a language wave

`git log` restricted to the four design-time projects, most recent first:

- `335d6ff1a6`, `f41a609696`, `fcc028d43e` — issue #1898: degrade to no implementation on a Roslyn below
  the floor. This produced `RoslynVariantPolicy`, `ResourceExtractor.TryCreateInstance`,
  `ResourceExtractor.ReportUnsupportedHost` and `LAMA0087`.
- `08d065a9f8`, `e413ad96f9` — issue #1881: replace the Roslyn 4.12 variant with a Roslyn 5.0 variant, and
  renumber the latest variant to 5.10.
- `575be8b88a`, `cf2874353f`, `22d9d31779`, `751ef4c7f8` — issue #1876: replace `net8.0` with `net10.0`.

This is the template for the *platform* half of the .NET 11 work, and its shape is: one policy type with
unit tests, one loud diagnostic for the compile-time path, one written report for the design-time path.

---

## 4. Extension points for each shape of language change

### 4.1 A new kind of type declaration (for example `union`)

| Must change | Where | If not changed |
| --- | --- | --- |
| The partial-type detector | `Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesVisitor.cs` lines 38 to 42 | Silent: a `partial union` is never registered as a partial type. |
| The partial-type hasher | `Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesHasher.cs` lines 43 to 47 | Silent: the fast path never reports a change in a `partial union`. |
| The attribute-insertion switch | `Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs` lines 74 to 191 | Silent: "Add aspect" produces no edit. |
| The partial-stub factory | `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs` lines 720 to 789 | Loud: `AssertionFailedException` at line 788, contained by the per-group `catch` at lines 90 to 105 and surfaced as `LAMA0049`. |
| The `partial` modifier injection | same file, lines 510 to 511 | The generated nested type lacks `partial`. |
| The generated-file header | same file, lines 817 to 823 | Cosmetic: no header comment. |
| The generated hashers | regenerated from `eng/src/GenerateMetaSyntaxRewriter/Syntax-<version>.xml` | Silent: see 5.1. |
| The code model `DeclarationKind` admission | same file, line 115 | Loud: `AssertionFailedException` at line 126. |

`TheCodeFixProvider.GetTypeDeclaration` (lines 187 to 193) needs no change: it matches
`BaseTypeDeclarationSyntax`.

### 4.2 A new modifier (for example `closed`)

- No design-time file enumerates modifiers, except the three `SyntaxKind.PartialKeyword` tests listed in
  1.1, 1.2 and 1.8 (line 513), which look for `partial` specifically and are unaffected.
- `DesignTimeSyntaxTreeGenerator.CreatePartialType` lines 710 to 714 build the modifier list of the
  generated stub from `type.IsStatic` alone:
  `static partial` or `partial`. A modifier that the C# compiler requires to be repeated on every partial
  declaration would have to be added here, and its absence produces a compiler error in the generated
  file, which the user sees in the editor and cannot fix.
- `TheCodeFixProvider` line 173 `AddModifiers` appends `partial` after the existing modifiers; modifier
  ordering rules for a new modifier would need attention there.
- The generated hashers hash a `SyntaxTokenList` of modifiers via `Visit( node.Modifiers )`, so a new
  modifier token is hashed by its `RawKind` without any edit, provided the node type itself is in the
  grammar snapshot.

### 4.3 A new expression form (for example `unsafe(expr)`)

- Nothing in `Metalama.Framework.DesignTime` matches on expressions.
- The only impact in this subsystem is the generated hashers: `UnsafeExpressionSyntax` must appear in the
  grammar snapshot of the variant, otherwise its tokens are not hashed. See 5.1.
- `CSharpAttributeHelper.CreateAttributeSyntax` lines 266 to 272 parse attribute-argument expressions with
  `SyntaxFactory.ParseExpression`, which uses the loaded Roslyn's default language version.

### 4.4 A new collection-expression element (for example `with(...)`)

- Same as 4.3: the generated hashers are the only consumer in this subsystem.
- `WithElementSyntax` derives from `CollectionElementSyntax` (`Syntax-5.10.0.xml` line 816). The generated
  hasher of the 5.10 variant will contain `VisitWithElement`; the 5.0 variant will not.

### 4.5 A new optional field on an existing statement (labelled `break` / `continue`)

This is the shape with the highest silent-failure risk in this subsystem, for two reasons.

1. **The generated hashers are per-variant and per-field.** `Generator.GenerateHasher`
   (`eng/src/GenerateMetaSyntaxRewriter/Generator.cs` lines 637 to 708) emits, for each node, one
   `this.Visit( node.<Field> )` or `VisitTrivialToken`/`VisitNonTrivialToken` call per field *present in
   that variant's snapshot*. The Roslyn 5.0 variant's `VisitBreakStatement` will not read the new `Name`
   field. Two syntax trees differing only in a `break` label therefore hash equal under the 5.0 variant.
2. **`MetaSyntaxRewriter` has a per-version field switch and the hasher does not.** `Generator.cs` lines
   432 to 479 emit `switch ( this.TargetApiVersion )` with a per-version field list, and `default: throw
   new AssertionFailedException();` at line 477, for the template rewriter. `GenerateHasher` has no such
   switch: it generates one method per node from one snapshot, so it cannot express "this field exists
   only above version X" and cannot fail when it meets one.

Also: `TheDiagnosticAnalyzer.TryMapLocation` lines 459 to 460 match tokens by text among the direct
children of a node, and a new identifier-valued token on `BreakStatementSyntax` adds a candidate there.

---

## 5. Where the subsystem would silently do the wrong thing

Ordered by consequence.

### 5.1 A syntax node or field absent from the variant's grammar snapshot is not hashed

`BaseCodeHasher` derives from `SafeSyntaxWalker`
(`Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs` lines 69 to 72, `VisitCore` calls
`base.Visit`), whose `DefaultVisit` recurses into children but appends nothing to the hash. Every
`Visit<Node>` the generator emits (`Generator.cs` line 642) *overrides* the base and hashes only the
declared fields; it never calls `base`.

Therefore, for a node type that the host's Roslyn produces but the variant's snapshot does not contain:
its own tokens contribute nothing to the hash, and only its child nodes do. An edit confined to those
tokens produces an identical `DeclarationHash`, `DiffStrategy.IsDifferent` returns `false`
(`Pipeline/Diff/DiffStrategy.cs` lines 80 to 85), the syntax tree version is reused, the design-time
pipeline does not re-run for that file, and the integrated development environment keeps showing the
previous generated code and the previous diagnostics. Nothing is logged.

This is reachable in two ways under PB-2027.0:
- the Roslyn 5.0 variant meeting C# 15 syntax (only if a host below Roslyn 5.10 ever parses it, which the
  language-version gate normally prevents);
- the latest variant meeting a Roslyn newer than `5.10.0-1.26365.3`, which
  `RoslynVariantPolicyTests.LatestVersionSelectsThe5100Variant` explicitly permits for `5.11.0` and
  `6.0.0`.

### 5.2 `CompileTimeCodeFastDetector` misclassifies a file, and the wrong hasher runs

`CompileTimeCodeFastDetector.DetectCompileTimeVisitor.DefaultVisit` returns `false`
(`Metalama.Framework.Engine/CompileTime/CompileTimeCodeFastDetector.cs` line 83), and only
`CompilationUnitSyntax`, `NamespaceDeclarationSyntax` and `FileScopedNamespaceDeclarationSyntax` recurse
(lines 77 to 81). A using directive reachable only through a container not in that list is not seen, the
file is classified as run-time-only, and `RunTimeCodeHasher` is chosen
(`DiffStrategy.cs` lines 76 and 157). That hasher deliberately ignores the *content* of `BlockSyntax`,
`ArrowExpressionClauseSyntax` and `EqualsValueClauseSyntax` (`Generator.cs` lines 656 to 663), so edits
inside a template body stop invalidating the pipeline. The user sees stale generated code with no error.

### 5.3 A design-time host with no loadable payload variant reports nothing to the editor

`ResourceExtractor.TryCreateInstance` returns `false` (lines 160 to 167), every entry-point shim holds a
null `_impl`, and each of them degrades to a no-op: `MetalamaDiagnosticAnalyzer.SupportedDiagnostics`
returns `ImmutableArray<DiagnosticDescriptor>.Empty` (line 61), `Initialize` does nothing (line 63);
`MetalamaSourceGenerator.Initialize` does nothing (line 62); and so on. The only trace is the file written
by `ReportUnsupportedHost` (lines 187 to 210) into the crash-reports directory. The compile-time path is
the exception: `MetalamaSourceTransformer.Execute` reports `LAMA0087` as an *error* (lines 50 to 60), and
its own doc comment (lines 18 to 22) states why the two paths differ.

This is deliberate and documented, but it means that every mistake in the variant table, the target
framework of the Resources project, or the `CoreAssemblyToEmbed` glob is invisible in the editor.

### 5.4 A target-framework literal that stops matching drops every extension, silently

`TargetedAssemblyReference.SatisfiesCurrentProcess` (lines 22 to 24) compares
`this.TargetFramework == _targetFramework` by string equality, and
`ExtensionLoaderBase.GetExtensionAssemblyPaths` (lines 35 to 37) simply filters the sequence. An extension
whose props file declares `net8.0` yields an empty path list, `DesignTimeExtensionManager.OnProjectDiscovered`
(lines 67 to 71) discovers no extension type, and the loop at line 73 does nothing. No diagnostic is
reported: `NullDiagnosticAdder.Instance` is passed at line 71. `docs/platform-support.md` lines 305 to 313
names this exact hazard. The Premium repository is in this state today (section 2.12).

### 5.5 An empty embedded resource set instead of a build error

`docs/platform-support.md` line 300: "The two files must move together, and a mismatch produces an empty
resource set rather than a build error." The `CoreAssemblyToEmbed` and `DesktopAssemblyToEmbed` items of
`Metalama.Framework.CompilerExtensions.csproj` (lines 53 to 70) are MSBuild globs over
`../Metalama.Framework.CompilerExtensions.Resources/bin/$(Configuration)/<tfm>/`; a `<tfm>` that names no
directory matches nothing. Lines 305 to 309 of the same document add the second half of the trap: a path
segment that names a target framework is not always ours (the removed
`runtimes/win/lib/net8.0/System.Threading.AccessControl.dll` was a package asset folder).

### 5.6 The "Add aspect" refactoring produces no edit for an unknown declaration kind

`CSharpAttributeHelper.AddAttribute` returns `null` at line 190, `AddAttributeAsync` returns `null` at
lines 35 to 38, and in the Premium consumer
`C:/src/Metalama-2027.0/Metalama.Premium/src/Metalama.Extensions.CodeFixes.DesignTime/AddAspectAttributeCodeActionModel.cs`
lines 96 to 99 turn that into `CodeActionResult.Empty`. The code action appears in the menu, the user
invokes it, and nothing happens. Already true for `record`, `record struct` and `extension` blocks.

### 5.7 A Roslyn internal that moves turns a refresh into a no-op

- `AnalysisProcessInvalidationService`: `_diagnosticsRefreshAction` becomes `null` (lines 47 to 50, 60 to
  63) and line 36 `?.Invoke` does nothing. Diagnostics are never refreshed after a pipeline run in the
  language-server host.
- `UserProcessInvalidationService`: `_updateSourceGeneratorsAction` becomes `null` (lines 65 to 68) and
  line 50 `?.Invoke` does nothing. Generated source is never regenerated in the Visual Studio user process.
- `RemoteWorkspaceProvider.TryCreate` returns `false` and the workspace is unavailable, so
  `AnalysisProcessInvalidationService.OnCompilationResultChanged` returns early at lines 30 to 34.

All three log at `Warning` or below and none reports a diagnostic. `AnalysisProcessInvalidationService`
line 67 explicitly records that "no export" is the normal state in Visual Studio and Rider, so absence
cannot be used as a failure signal.

### 5.8 A host process tree that changes shape disables the whole cross-process layer

`ServiceHubClientEndpoint.TryGetPipeName` returns `false` at lines 74 to 77 with a log line only.
`TryStart` then returns `false` (lines 35 to 40), and every service that flows through the service hub, the
CodeLens, the preview, the aspect explorer and the compile-time editing status, is simply absent from the
editor.

### 5.9 Contract-version validation accepts a missing contract

`DesignTimeEntryPointManager.Consumer.ValidateContractVersions`
(`Metalama.Framework.DesignTime.Contracts/EntryPoint/DesignTimeEntryPointManager.Consumer.cs` lines 30 to
43):

```csharp
var candidateVersion = candidates.SingleOrDefault( c => c.Version == supportedVersion.Key ).Revision;

if ( candidateVersion != 0 && candidateVersion != supportedVersion.Value )
{
    return false;
}
```

A candidate that does not declare the contract at all yields `Revision == 0` and passes. A future
`ContractVersion_2_0` added on one side only is therefore accepted rather than rejected, and the mismatch
surfaces later as an `InvalidCastException` or a `MissingMethodException` rather than as the
`ContractVersionMismatchDetected` event (line 107) that exists for the purpose.

### 5.10 A contract service resolved by simple type name returns null for anything unknown

`CompilerServiceProvider.GetService` (`Metalama.Framework.DesignTime/VersionNeutral/CompilerServiceProvider.cs`
line 34) resolves by `serviceType.Name`, and `GetServiceCore` ends in `_ => null` (line 42).
`VsUserProcessCompilerServiceProvider.GetServiceCore`
(`VisualStudio/Services/VsUserProcessCompilerServiceProvider.cs` lines 28 to 44) adds seven names and
delegates the rest. A Visual Studio extension asking for a contract interface this build does not
implement receives `null` and must decide for itself what that means.

### 5.11 An unresolvable declaration identifier drops a row from the Aspect Explorer

`VisualStudio/AspectExplorer/AspectDatabaseService.cs`:

- Lines 140 to 143 and 155 to 158: `if ( targetDeclaration is null ) { continue; }`.
- Lines 172 to 186 `ResolveToSymbol`: `new SerializableDeclarationId( id ).ResolveToSymbolOrNull( … )`.
- Line 161: `Invariant.Assert( transformedDeclarationKind == default );` — the only loud check here, and it
  asserts that a *transformed* declaration is never a return parameter.

A declaration kind that `SerializableDeclarationId` cannot name, or can name but cannot resolve, is
silently omitted from the aspect explorer. The declaration-kind enum on the wire,
`AspectExplorerDeclarationKind` (`Contracts/AspectExplorer/AspectExplorerAspectInstance.cs` lines 60 to 66),
has exactly two members, `Default` and `ReturnParameter`, and carries
`[Guid( "96F4689F-0FBA-4732-B7C3-069F608F79C2" )]`, so it is frozen: a third kind requires a new type with
a new GUID, per `docs/cross-process-communication.md` lines 56 to 59.

### 5.12 An unparseable `LangVersion` silently becomes the latest supported version

`Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs` lines 167 to 183:

```csharp
if ( !LanguageVersionFacts.TryParse( s, out var version ) )
{
    // This can happen if the property is set to an invalid value, but also if the IDE runs
    // a lower Roslyn version than the one required by the project. In this case, we return
    // the latest supported version of the current Metalama build, for the current Roslyn version.
    return SupportedCSharpVersions.Latest;
}
```

A project on `<LangVersion>15</LangVersion>` edited in a host whose Roslyn cannot parse `15` is analysed as
C# 14. The comment names the design-time case explicitly. Combined with the absence of any language-version
check in the design-time pipeline (section 1.9), this means the design-time experience never tells the user
that the language version is out of range; only the build does, through `LAMA0052`.

### 5.13 The design-time pipeline contains a failure per generated file rather than reporting it

`DesignTimeSyntaxTreeGenerator` lines 84 to 106: a failure while processing one transformation group is
caught and routed to `ICompileTimeExceptionHandler` with `canIgnoreException: true`, producing a warning
(`LAMA0049`) rather than an error, "and when the service is not registered, the failure is contained but
not reported" (line 100). The rationale, in the comment at lines 92 to 96, is issue #1767: letting the
exception escape makes the generated source of the whole project disappear. The consequence for a new
language construct is that an `AssertionFailedException` from `CreatePartialType` (line 788) costs the user
one generated file, quietly, rather than failing anything.

---

## 6. Cross-references and secondary observations

- `docs/cross-process-communication.md` is the authority on which of the two mechanisms a change belongs
  to. Rule 3, at line 20, forbids cross-process *and* cross-version traffic outright. The frozen-GUID
  checklist is at lines 102 to 111; the same-version RPC checklist at lines 113 to 121. The symptom table
  at lines 93 to 100 maps `ConnectionLostException`, `FileLoadException`, `InvalidCastException` and the
  "added a method to a `[Guid]` interface" case to their causes.
- `Metalama.Framework.DesignTime.Rpc/CLAUDE.md` documents the endpoint architecture; note lines 113 to 119
  on `JsonSerializationBinder`, which uses the *full* assembly name for Metalama assemblies and the simple
  name for others, so that several Metalama versions coexist.
- `docs/design-time-memory.md` bears on a language wave in one place: a new declaration kind must be
  nameable by `SerializableDeclarationId` or `SerializableTypeId`, because that is the representation a
  durable reference takes at design time (lines 74 to 95). Lines 130 to 137 record the precedent in which a
  durable reference silently widened `Generic<int>` to `Generic<T>` with no diagnostic (issue #1797), which
  is the exact failure shape to expect from an identifier grammar that does not cover a new construct.
- `Metalama.Framework.DesignTime.Contracts/EntryPoint/CurrentContractVersions.cs` line 22:
  `public const int ContractVersion_1_0 = 3;`. Line 24 exposes `All`. The assembly GUID is
  `234D9C3E-29CA-4ACC-8DB5-3F0D5C931D41` (`AssemblyInfo.cs` line 7) and the assembly name is
  `Metalama.Framework.DesignTime.Contracts.v2` (csproj line 5), matched by `ResourceExtractor` line 31 and
  by the AppDomain data slot name at `DesignTimeEntryPointManager.cs` line 23,
  `"Metalama.Framework.DesignTime.Contracts.v2.DesignTimeEntryPointManager"`, whose comment at lines 38 to
  39 says the name "is used verbatim and must never change".
- `DesignTimeTextSpanClassificationHelper.ToDesignTime`
  (`VisualStudio/Classification/DesignTimeTextSpanClassificationHelper.cs` lines 12 to 27) maps the engine
  classification enum to the frozen contract enum and throws `ArgumentOutOfRangeException` at line 26 for
  an unknown value. This is the one contract-crossing enum mapping in the subsystem that fails loudly.
- `Metalama.Framework.DesignTime/SourceGeneration/TouchFileRenderer.cs` line 44 carries the only `net472`
  reference in the design-time C# sources, and it is a `#pragma warning disable CA1307` comment, not a
  behavioural branch.
- `DesignTimeAspectPipelineFactory`, `BaseSourceGenerator` (`SourceGeneration/BaseSourceGenerator.cs`
  lines 28 to 228) and `ProjectSourceGenerator` (`SourceGeneration/ProjectSourceGenerator.cs` lines 18 and
  49) are the pipeline that serves the integrated development environment. They are keyed by `ProjectKey`
  and by touch-file GUID and are entirely language-shape-agnostic. `ProjectKeyFactory`
  (`ProjectKeyFactory.cs` lines 30 to 48, 54, 74 to 96) hashes only the `METALAMA_PROJECT_*` preprocessor
  symbols when any is present (issue #1749), so a change in `LangVersion` alone does not change the project
  key and does not invalidate the design-time cache.
