# Terrain map: test infrastructure and suites

Subsystem: `Metalama.Framework/src/Metalama.Testing.AspectTesting/**`,
`Metalama.Framework/src/Metalama.Testing.UnitTesting/**`, `Metalama.Backstage/src/Metalama.Testing.Hooks/**`,
`Metalama.Framework/src/tests/**`, `Metalama.Framework/docs/testing.md`.
Branch: `topic/2027.0/26-09-03-net11-impact` (based on `develop/2027.0`).

All paths are relative to `C:/src/Metalama-2027.0/Metalama` unless stated otherwise.

---

## 0. Orientation: how a test declares what it needs

### 0.1 The directive parser

`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs` is the single place where the
`// @Directive(arg)` vocabulary is defined.

- Line 40: the recogniser.
  `private static readonly Regex _optionRegex = new( @"^\s*//\s*@(?<name>\w+)\s*(\((?<arg>[^\)]*)\))?", RegexOptions.Multiline );`
- Line 508: `internal void ApplySourceDirectives( string sourceCode, string? path )`.
- Line 525: every directive must appear after the first `#if` in the file, otherwise
  `InvalidTestOptionException` is thrown: `"The '@{optionName}' option must be in an #if block in '{path}'."`
  This is why every test payload wraps its directives in `#if TEST_OPTIONS … #endif`. `TEST_OPTIONS` is
  **never defined anywhere in the repository** (verified: no `.props`, `.targets`, `.csproj` or `.md`
  defines it); it exists solely to satisfy that rule and to keep the directives out of the compilation.
- Line 528: `switch ( optionName )`, the exhaustive list of directives.
- Line 841: the default arm, `this._invalidSourceOptions.Add( "@" + optionName )`. An unknown directive
  is collected, and `BaseTestRunner.RunAsync` (BaseTestRunner.cs:186-191) throws
  `"Invalid option(s) in source code: …"`. So a **misspelled directive fails loudly**.
- Line 857: `ApplyToTestContextOptions`, which forwards `TargetFrameworks` into
  `TestContextOptions.AllTargetFrameworks`.

Directive cases that matter for a language or platform wave:

| Line | Directive | Property |
|---|---|---|
| 540 | `@Skipped(reason)` | `SkipReason` |
| 555 | `@TestScenario(…)` | `TestScenario` |
| 609 | `@RequiredConstant(c)` | `RequiredConstants` (TestOptions.cs:198) |
| 614 | `@ForbiddenConstant(c)` | `ForbiddenConstants` (TestOptions.cs:204) |
| 619 | `@DefinedConstant(c)` | `DefinedConstants` (TestOptions.cs:212) |
| 624 | `@DependencyDefinedConstant(c)` | `DependencyDefinedConstants` (TestOptions.cs:220) |
| 681 | `@LanguageVersion(v)` | `LanguageVersion` (TestOptions.cs:256) |
| 702 | `@DependencyLanguageVersion(v)` | `DependencyLanguageVersion` (TestOptions.cs:262) |
| 723 | `@LanguageFeature(f[=v])` | `LanguageFeatures` (TestOptions.cs:268) |
| 835 | `@TargetFrameworks(a;b)` | `TargetFrameworks` (TestOptions.cs:289) |

**`@LanguageVersion` has a version-aware escape hatch** (TestOptions.cs:681-700):

```csharp
if ( LanguageVersionFacts.TryParse( optionArg, out var languageVersion ) ) { … }
else
{
    // The version may be a valid number but still not recognized by the current version of Roslyn.
    if ( double.TryParse( optionArg, out var n ) && n >= 10 && Math.Abs( n - Math.Floor( n ) ) <= double.Epsilon )
    {
        this.SkipReason = $"@LanguageVersion '{optionArg}' is not recognized by the current version of Roslyn.";
    }
    else { throw new InvalidTestOptionException( … ); }
}
```

The same block is repeated for `@DependencyLanguageVersion` at lines 702-720. Consequence for C# 15:
`// @LanguageVersion(15)` in a test file will **silently skip** the test on any Roslyn variant whose
`LanguageVersionFacts.TryParse` does not yet know `15`, instead of failing. That is deliberate (it lets the
same payload live in both the Roslyn 5.0 and the latest variant), but it means a whole `CSharp15` suite
written this way could be entirely inert without any red signal. See §5.

`@LanguageFeature(preview)` (line 723) is the only way to switch a Roslyn feature flag on, and there is no
validation of the feature name at all: an unknown feature name is passed straight to
`CSharpParseOptions.WithFeatures` and ignored by Roslyn.

### 0.2 Where the directive values are consumed

`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs`:

- Lines 76-83: `@RequiredConstant` — a constant not in `ProjectProperties.PreprocessorSymbols` sets
  `SkipReason`. **An unknown constant name skips the test.**
- Lines 87-95: `@ForbiddenConstant` — symmetric.
- Lines 97-117: `@TargetFrameworks` — a case-insensitive comparison of the requested list against
  `ProjectProperties.TargetFramework`; a non-match sets `SkipReason`. A typo in a target framework name
  also merely skips.

`Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs`:

- Lines 211-213: the preprocessor symbols of the test compilation are
  `ProjectProperties.PreprocessorSymbols + "TESTRUNNER" + "METALAMA"`.
- Line 218: `var defaultParseOptions = SupportedCSharpVersions.DefaultParseOptions;` — the baseline for
  every test compilation.
- Lines 220-223: `@LanguageVersion` overrides it via `WithLanguageVersion`.
- Lines 225-228: `@LanguageFeature` via `WithFeatures`.
- Line 230: `@DefinedConstant` appended.
- Lines 417-423: the dependency project (`X.Dependency.cs`) starts from `defaultParseOptions` again, adds
  `@DependencyDefinedConstant`, then `@DependencyLanguageVersion`. Note that the dependency **does not**
  inherit `@LanguageVersion`; the two are independent knobs.
- Lines 385-399: `AddPlatformDocumentsAsync` is `#if NETFRAMEWORK`-guarded and injects
  `namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }` on the `net48`
  leg only.

### 0.3 Where `ProjectProperties` come from (the MSBuild → test bridge)

`Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets`, target
`AddMetalamaTestFrameworkAttributes` (line 46), writes `AssemblyMetadataAttribute` pairs that the test
framework reads back:

| targets line | Key | Read by |
|---|---|---|
| 58-61 | `DefineConstants` | `TestAssemblyMetadataReader.GetParserSymbols` (TestAssemblyMetadataReader.cs:74-79) |
| 108-111 | `TargetFramework` | `GetTargetFramework` (line 81) |
| 112-115 | `TargetFrameworks` | `GetTargetFrameworks` (line 83) |
| 62-73 | `ProjectDirectory`, `ProjectPath`, `SourceDirectory` | discovery root |
| 120-123 | `IgnoredWarnings` (from `$(NoWarn)`) | `GetIgnoredWarnings` (line 114) |
| 126-129 | `MetalamaDurableRefKind` | `GetDurableRefKind` (line 123) |

`Metalama.Testing.AspectTesting.targets:52-55` carries a hard-coded Roslyn fallback:

```xml
<!-- When this is referenced as a NuGet package, the latest version of Roslyn is used. -->
<ThisRoslynVersionNoPreview Condition="'$(ThisRoslynVersionNoPreview)'==''">5.0.0</ThisRoslynVersionNoPreview>
```

and line 75-78 uses it to emit `[assembly: Xunit.TestFramework("Metalama.Testing.AspectTesting.AspectTestFramework",
"Metalama.Testing.AspectTesting$(ThisRoslynVersionProjectSuffix)")]`. Every **external** consumer of the
`Metalama.Testing.AspectTesting` package (including `Metalama.Premium`'s three `*.AspectTests` projects)
therefore binds the Roslyn-5.0-suffixed assembly name by default.

Reading path: `TestAssemblyMetadataReader.GetMetadataCore` (TestAssemblyMetadataReader.cs:28-128) →
`TestAssemblyMetadata` (TestAssemblyMetadata.cs:164-177) → `TestDiscoverer.GetTestProjectProperties`
(`XunitFramework/TestDiscoverer.cs:69-80`) → `TestProjectProperties` (TestProjectProperties.cs:18-74).

`TestDiscoverer` also reads `TargetFrameworkAttribute` off the test assembly
(`XunitFramework/TestDiscoverer.cs:56-65`) into `TestDiscoverer.TargetFramework`.

### 0.4 `metalamaTests.json`

`TestDirectoryOptions` (TestDirectoryOptions.cs:14) extends `TestOptions` with `Exclude` and `IsRoot`.
`TestDirectoryOptionsReader` merges directory options up the tree; `TestOptions.ApplyBaseOptions`
(TestOptions.cs:407-501) does the field-by-field merge. Note lines 452-458: `RequiredConstants`,
`ForbiddenConstants`, `DefinedConstants` and `DependencyDefinedConstants` are **added**, never replaced,
so a directory-wide `RequiredConstants` is inherited by every test below it. `TargetFrameworks` (line 500)
is `??=`, so a file-level directive wins.

Every `metalamaTests.json` in the repository (25 of them; none under `Tests/Aspects/CSharp1x`):

```
Metalama.Framework.Tests.AspectTests/Tests/metalamaTests.json          (root: IgnoredDiagnostics, MainMethod "TestMain", CheckMemoryLeaks, LicenseKeyProviderType)
Metalama.Framework.Tests.AspectTests/Runners/metalamaTests.json        ({ "Exclude": true })
Metalama.Framework.Tests.AspectTests/Tests/Aspects/{AppendParameter,Async/AsyncIterators,DesignTimeInvalidCode,Formatting,Formatting/EndOfLines,Initialization,Introductions/InterfaceImplementation,InvalidCode,Misc/Include,Samples}/metalamaTests.json
Metalama.Framework.Tests.AspectTests/Tests/Fabrics/Include/metalamaTests.json
Metalama.Framework.Tests.AspectTests/Tests/Formatting{,/Diff,/Output}/metalamaTests.json
Metalama.Framework.Tests.AspectTests/Tests/TestFramework/{Formatting,Html,Imported}/metalamaTests.json
Metalama.Framework.Tests.TemplateTests/{Runner,Tests}/metalamaTests.json   (Tests: TestRunnerFactoryType = …TemplatingTestRunnerFactory)
Metalama.Framework.Tests.LinkerTests/{Runner,Tests,Tests/_Helpers}/metalamaTests.json (Tests: TestRunnerFactoryType = …LinkerTestRunnerFactory)
Standalone/HtmlWriterAspectTest/metalamaTests.json
```

`TestRunnerFactoryType` is resolved by `TestRunnerFactory.ResolveTestRunnerFactoryType`
(TestRunnerFactory.cs:93-115). Its doc comment (lines 84-92) records the exact Roslyn-variant hazard:
the assembly name in a `metalamaTests.json` type name is only a hint, because the assembly is renamed per
Roslyn variant (`Metalama.Testing.AspectTesting.4.12.0`, `.5.0.0`, …).

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 The two `MemberDeclarationSyntax` kind switches (the primary hotspot)

Both enumerate the member-declaration kinds by hand and **throw** on anything else.

**`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestSyntaxTree.cs:187-231`**,
`TestSyntaxTree.SetRunTimeCodeAsync`:

```
192  switch ( syntaxNode.Kind() )
194      case SyntaxKind.CompilationUnit:
199      // All MemberDeclarationSyntax kinds
200-221  GlobalStatement, NamespaceDeclaration, FileScopedNamespaceDeclaration, ClassDeclaration,
         StructDeclaration, InterfaceDeclaration, RecordDeclaration, RecordStructDeclaration,
         EnumDeclaration, DelegateDeclaration, EnumMemberDeclaration, FieldDeclaration,
         EventFieldDeclaration, MethodDeclaration, OperatorDeclaration, ConversionOperatorDeclaration,
         ConstructorDeclaration, DestructorDeclaration, PropertyDeclaration, EventDeclaration,
         IndexerDeclaration, IncompleteMember
225      default:
226          throw new ArgumentOutOfRangeException( nameof(syntaxNode),
                 $"The root of the document must be a CompilationUnitSyntax or a MemberDeclarationSyntax but it is a {syntaxNode.Kind()}." );
```

**`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestResult.cs:518-585`**, in
`BuildSyntaxTreesForComparison`, the `// <target>` consolidation:

```
520  switch ( outputMembers[i].Kind() )
522      case SyntaxKind.CompilationUnit:
531      case SyntaxKind.ExpressionStatement:   // an empty statement
547      // All MemberDeclarationSyntax kinds
548-569  (the same 22 kinds)
583      default:
584          throw new InvalidOperationException( $"Don't know how to add a {outputMembers[i].Kind()} to the compilation unit." );
```

Facts:

- `SyntaxKind.ExtensionBlockDeclaration` (C# 14; `ExtensionBlockDeclarationSyntax : TypeDeclarationSyntax`,
  declared at `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:2083-2088`) is **absent from both
  lists**. The C# 14 suite got away with it because the `// <target>` marker in every
  `Tests/Aspects/CSharp14/ExtensionMembers/*.cs` sits on the enclosing `static class`, not on the
  `extension(...)` block (see `ExtensionMembers_IntroduceMethod.cs:60-67`).
- `SyntaxKind.UnionDeclaration` (C# 15; `UnionDeclarationSyntax : TypeDeclarationSyntax`,
  `Syntax-5.10.0.xml:1954-1959`) will hit the same `default` arms. A `CSharp15/Union/*.cs` test that
  marks the `union` with `// <target>` fails with `InvalidOperationException: Don't know how to add a
  UnionDeclaration to the compilation unit.` until both switches are extended.
- The target selection itself (TestResult.cs:490-499) is kind-agnostic: it takes every
  `MemberDeclarationSyntax` whose leading trivia, first child token's leading trivia or attribute-list
  leading trivia contains `<target>`. So a new declaration kind is *found* and then *rejected*.

### 1.2 Output normalisation reparses without parse options

**`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOutputNormalizer.cs:21-22`**:

```csharp
public static string? NormalizeTestOutput( string? s, bool preserveFormatting, bool forComparison )
    => s == null ? null : NormalizeTestOutput( CSharpSyntaxTree.ParseText( s ).GetRoot(), preserveFormatting, forComparison );
```

`CSharpSyntaxTree.ParseText(s)` uses `CSharpParseOptions.Default`, that is `LanguageVersion.Default`
(the latest *stable major* the running Roslyn supports), and the returned diagnostics are never inspected.
This function is on the whole golden-file comparison path:

- `BaseTestRunner.cs:575` (actual, for comparison), `:578` (actual, for storage), `:592` (expected,
  for comparison) — the `.t.cs` / `.i.cs` path.
- `BaseTestRunner.cs:698`, `:701`, `:713` — the `.ct.cs` compiled-template path.
- `Metalama.Framework/src/tests/Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:332-333`.

See §5.1 for the consequence.

### 1.3 Meta-syntax round trip

**`Metalama.Framework/src/Metalama.Testing.AspectTesting/SyntaxTreeStructureVerifier.cs`**:

- `VerifyMetaSyntax` (line 26): compares `syntaxTree.GetRoot().ToSyntaxFactoryDebug( compilation )` with
  the same rendering of a reparse. The reparse at line 32-35 uses
  `SupportedCSharpVersions.DefaultParseOptions`, **not** the tree's own options, so it is pinned to
  `SupportedCSharpVersions.Latest`. `ToSyntaxFactoryDebug` comes from
  `Metalama.Framework.Engine/SyntaxGeneration/SyntaxFactoryDebugHelper.cs:19`, which is generated by
  `eng/src/GenerateMetaSyntaxRewriter` from the `Syntax-<version>.xml` grammar; a new node type therefore
  has to be present in that generator before this verifier can render it.
  Only caller: `Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:259`.
- `Verify` (line 52): reparses each tree with `(CSharpParseOptions) syntaxTree.Options` (correct) and
  reports any error diagnostic. Callers: `AspectTestRunner.cs:211`,
  `Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestRunner.cs:99`. This is the guard that would
  catch a construct the syntax generator emits incorrectly, so it is the single most valuable check in
  the subsystem for a new expression form or a new statement field.

### 1.4 The linker test input builder

`Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs`
is written against a closed set of declarations.

Type declarations handled (each pushes onto `_currentTypeStack`):

- `VisitClassDeclaration` (line 49), `VisitRecordDeclaration` (line 65), `VisitStructDeclaration` (line 81).
- **Not handled**: `InterfaceDeclaration`, `ExtensionBlockDeclaration`, and any future `UnionDeclaration`.

Member declarations handled, each doing `this._currentTypeStack.Peek().Members.AddRange( … )` and
returning `null` (removing the node from its original parent):

- `VisitMethodDeclaration` (119), `VisitPropertyDeclaration` (126), `VisitEventDeclaration` (133),
  `VisitEventFieldDeclaration` (140), `VisitFieldDeclaration` (147). No indexer, no operator, no
  constructor.

Closed switches that throw:

- Line 341-350, `introducedElementName`: `Method/Property/Event/EventField/Field`, else
  `throw new NotSupportedException()`.
- Line 555-568, `GetFinalIntroductionSyntax`: `PropertyDeclarationSyntax`, `FieldDeclarationSyntax`, else
  `throw new AssertionFailedException( $"Unexpected syntax kind {introductionSyntax.GetLocation()}." )`.
- Line 641-696, `ProcessPseudoOverride`: `MethodDeclarationSyntax { Body }`,
  `MethodDeclarationSyntax { ExpressionBody }`, `PropertyDeclarationSyntax { AccessorList }`,
  `EventDeclarationSyntax`, `EventFieldDeclarationSyntax`, else `throw new NotSupportedException()`.

`LinkerTestInputBuilder.TestRewriter.cs` mirrors the three type overrides at lines 112, 126, 140, and
`HasLayerOrderAttribute` / `ProcessLayerOrderAttributeNode` (lines 154, 183) are typed on
`TypeDeclarationSyntax`, so they would accept a union but never be reached for one.

`LinkerInlineAssertionWalker.cs:34-35` reparses both the asserted and the observed syntax with
`SupportedCSharpVersions.DefaultParseOptions` — the same pinning as §1.3.

### 1.5 The template test runner

`Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Runner/`:

- `TestTemplateCompiler.cs:85`, `Visitor.VisitMethodDeclaration` — **only methods can be templates in this
  suite**. A template expressed as any other member is silently not compiled and not snapshotted.
- `TemplatingTestRunner.cs:328-340` — the runner requires a type literally named `TargetCode` with a
  method literally named `Method`, resolved by `Single(...)`; anything else throws.
- `TemplatingTestRunner.cs:393-402` — `p.RefKind switch { None, In, Out, Ref, _ => throw new
  AssertionFailedException( $"Unexpected value for RefKind in {p}: {p.RefKind}." ) }`.
  `RefKind.RefReadOnlyParameter` (C# 12 `ref readonly`) is **not** handled; a new `RefKind` from a future
  parameter modifier would throw here.
- `TemplatingTestRunner.cs:184` reparses the transformed template with the *old* tree's options
  (`(CSharpParseOptions?) oldTransformedTemplateSyntaxTree.Options`), which is correct.
- `TemplatingTestRunner.cs:194` has `// SyntaxTreeStructureVerifier.Verify( compileTimeCompilation );`
  commented out, so the template suite has no structural verification at all.

### 1.6 Construct-agnostic parts (no change needed for a new node)

- `BaseTestRunner.ValidateAttributesVisitor` (`BaseTestRunner.ValidateAttributesVisitor.cs:15-36`) —
  a `SafeSyntaxWalker` overriding `VisitAttribute` only.
- `LiveTemplateTestRunner.TargetAttributeWalker` (`LiveTemplateTestRunner.cs:107-140`) — a
  `CSharpSyntaxWalker` overriding `VisitAttribute` only; it takes `node.Parent?.Parent` as the target and
  calls `GetDeclaredSymbol`, so it works for any attributable declaration.
- `DesignTimeTestRunner.cs:47-61` and `PreviewTestRunner.cs:62-87` — they move whole syntax trees around
  and never inspect kinds.
- `Metalama.AspectWorkbench/ViewModels/AnnotationRenderingRewriter.cs` — a `CSharpSyntaxRewriter` that
  only manipulates `SyntaxKind.MultiLineCommentTrivia`.
- `BaseTestRunner.cs:255` uses `RemovePreprocessorDirectivesRewriter( SyntaxKind.PragmaWarningDirectiveTrivia,
  SyntaxKind.NullableDirectiveTrivia )`, and line 259 checks `IsKind( SyntaxKind.CompilationUnit )` with a
  `CompilationUnitSyntax { Members.Count: 0, AttributeLists.Count: 0 }` pattern — a file with only
  top-level statements would be a `CompilationUnit` with `Members` containing a `GlobalStatement`, so this
  is fine.

### 1.7 The grammar-coverage tool (dead)

`Metalama.Framework/src/tests/Utilities/SyntaxCover/Program.cs` is the only artefact in the subsystem that
enumerates the whole C# grammar:

```
21  Dictionary<string, int> syntaxKindCounts = GetAllSyntaxKinds().ToDictionary( s => s.ToString(), s => 0 );
51  static IEnumerable<SyntaxKind> GetAllSyntaxKinds()
53      var excludedNamesRegexes = new[] { "Trivia$", "^Xml", "Cref", "List$" };
55      ((SyntaxKind[]) Enum.GetValues( typeof( SyntaxKind ) )).Where( kind => !SyntaxFacts.IsAnyToken( kind ) && … )
```

It reads `artifacts/tests/SyntaxCover/**/*.txt`, and **nothing in the repository writes those files any
more** (the only remaining mention is `Metalama.Framework/docs/testing.md:252`, which lists it under
"not part of the automated run"). Its project targets `netcoreapp3.1`
(`Utilities/SyntaxCover/SyntaxCover.csproj:5`), a runtime no longer in the baseline. There is therefore
**no automated signal at all** telling us which C# syntax kinds the aspect-test corpus exercises.

---

## 2. Files and types sensitive to runtime, SDK, Roslyn or host versions

### 2.1 The language-version floor and ceiling that the test suites inherit

| File:line | Value | Effect on the test suites |
|---|---|---|
| `Metalama.Framework/Directory.Build.props:45` | `<LangMaxVersion>14.0</LangMaxVersion>`, then `<LangVersion>$(LangMaxVersion)</LangVersion>` (line 46) | The language the aspect-, template- and linker-test **payload files themselves** are compiled with. A `CSharp15` payload does not compile until this moves. Exported to other repositories via `eng/src/Program.cs:142`. |
| `Directory.Build.props:16` | `<MetalamaTemplateLanguageVersion>14.0</MetalamaTemplateLanguageVersion>` | The language a **template** may use. Comment (lines 12-16) ties it to `RoslynApiMinVersion` and to `platform-support.md`. |
| `Directory.Build.props:9` | `<NoWarn>$(NoWarn);IDE0032;IDE0031</NoWarn>` "Disabling new features while the SDK is not stable" | Suppresses the C# 14 `field`-keyword and null-conditional-assignment IDE suggestions repository-wide. |
| `Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32` | `Latest => LanguageVersion.CSharp14` | Every default test parse. |
| `SupportedCSharpVersions.cs:38-43` | `All = { CSharp14, CSharp13, CSharp12, CSharp11, CSharp10 }` | Rendered verbatim into two checked-in expected files (§3.2). |
| `SupportedCSharpVersions.cs:50` | `DefaultParseOptions = CSharpParseOptions.Default.WithLanguageVersion( Latest )` | Used by 18 call sites in the test tree. |
| `SupportedCSharpVersions.cs:149-159` | `GetMaxLanguageVersion(Version roslynVersion)`: `(>= 5, _) => CSharp14` | Would have to gain a `(5, >= 10)` or `(>= 6, _)` arm for C# 15. |
| `SupportedCSharpVersions.cs:52-62` | `RoslynApiVersion.V5_10_0 => AllLanguageVersions.CSharp14` | Same. |

Per-project language pins inside the subsystem:

- `Metalama.Framework.Tests.UnitTests/…csproj:13` — `<LangVersion>14.0</LangVersion>`, with the comment
  (lines 14-17) explaining that the Roslyn variants deliberately do **not** override it "so we can use
  asserts in the latest language. To differentiate tests, use ROSLYN_X_Y_OR_GREATER".
- `Metalama.Framework.Tests.AspectTests/…csproj:22`,
  `Metalama.Framework.Tests.LinkerTests/…csproj:22`,
  `Metalama.Framework.Tests.TemplateTests/…csproj:22` —
  `<LangVersion Condition="'$(LangVersionOverride)'!=''">$(LangVersionOverride)</LangVersion>`.
  `LangVersionOverride` is **set nowhere in the repository**; it is a manual developer hook to rebuild the
  three payload suites under a different language version.
- `Metalama.Framework.Tests.Benchmarks/…csproj:9` — `<LangVersion>12.0</LangVersion>`.
- `Standalone/CSharp10/CSharp10.csproj:5` — `10.0`.
- `Standalone/TemplateLanguageVersion14/TemplateLanguageVersion14.csproj:10` — `14.0`.
- `Standalone/Issue1585b/Repro.csproj:13` — `12.0` (deliberately non-default).
- `Standalone/Issue1757/OldAspects/OldAspects.csproj:7` — `12.0`, with
  `<MetalamaTemplateLanguageVersion>12.0</MetalamaTemplateLanguageVersion>` at line 11.
- `Standalone/Issue31024/NetFrameworkBuildApp/ConsoleApp1.csproj:16` — `12.0`.
- `DesignTimeStandalone/Issue1749.FrameworkVersions/OldAspects/OldAspects.csproj:8` — `12.0`
  (README explains: the old `Metalama.Compiler` predates the SDK default).
- `Standalone/DefaultLanguageVersion/{DotNet,DotNetFramework}/*.csproj:11` — `<LangVersion></LangVersion>`,
  deliberately cleared so the scenario measures the **SDK default**.

### 2.2 Roslyn variant plumbing

- `eng/RoslynVersions/Latest.props` imports `Roslyn.5.10.0.props` when `ThisRoslynVersion` is empty.
- `eng/RoslynVersions/Roslyn.5.10.0.props:3` `ThisRoslynVersion = $(RoslynApiMaxVersion)`;
  line 5 `ThisRoslynVersionNoPreview = 5.10.0`; line 10
  `<DefineConstants>$(DefineConstants);ROSLYN_5_10_0_OR_GREATER</DefineConstants>`.
- `eng/RoslynVersions/Roslyn.5.0.0.props` defines no constant; line 13 pins
  `SystemTextJsonVersion 9.0.0` for that variant.
- `Roslyn.4.12.0.props` is gone. Leftover **empty** directories remain on disk and contain only stale
  `obj/`: `Metalama.Framework/src/tests/Metalama.Framework.Tests.{AspectTests,LinkerTests,TemplateTests,UnitTestHelpers,UnitTests}.4.12.0`.

Variant shim projects (each is a three-line glob over the base project's sources):

```
Metalama.Framework.Tests.AspectTests.5.0.0/…csproj      (also sets MetalamaDurableRefKind=SerializedWithoutCache, see #1811)
Metalama.Framework.Tests.LinkerTests.5.0.0/…csproj
Metalama.Framework.Tests.TemplateTests.5.0.0/…csproj
Metalama.Framework.Tests.UnitTestHelpers.5.0.0/…csproj
Metalama.Framework.Tests.UnitTests.5.0.0/…csproj
Metalama.Framework.Tests.Benchmarks.5.0.0/…csproj       (sets BenchmarkRoslynVersion=4.14.0; NOT in the solution, never built)
```

Solution membership (`Metalama.Framework/Metalama.Framework.sln`, lines 68-90) confirms
`Metalama.Framework.Tests.Benchmarks.5.0.0` is absent.

Constant-based test splitting, the whole of it:

```
Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7          // @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate_Roslyn5_0.cs:7 // @ForbiddenConstant(ROSLYN_5_10_0_OR_GREATER)
```

Their doc comments state the reason precisely: "Roslyn 5.0 reports `CS1014` on an empty span, and Roslyn
5.10 reports it on the `setx` token. The test framework compares a single expected file per test, so the
scenario needs one [test per variant]." **This is the canonical pattern for a per-Roslyn-variant expected
output.**

Other Roslyn-version pins in the subsystem:

- `Metalama.Testing.AspectTesting.csproj:17-18` and `Metalama.Testing.UnitTesting.csproj` —
  `Microsoft.CodeAnalysis.CSharp.{Workspaces,Features}` `VersionOverride="$(ThisRoslynVersion)"`.
- `Metalama.Framework.Tests.UnitTests/…csproj:37-38` — same.
- `Metalama.DesignTime.HostSimulator/…csproj:19-22` — four Roslyn packages at `$(RoslynMaxVersion)`.
- `Metalama.Framework.Tests.Workspaces/…csproj:20` — `Microsoft.CodeAnalysis.Workspaces.MSBuild` at
  `$(RoslynMaxVersion)` (public packages, not the Metalama.Compiler fork).
- `Metalama.Framework.Tests.Benchmarks/…csproj:10` —
  `BenchmarkRoslynVersion` defaults to `$(RoslynApiMaxVersion)`, overridden to `4.14.0` by the `.5.0.0` shim.
- `Metalama.Testing.AspectTesting.csproj:41` and `Metalama.Testing.UnitTesting.csproj` —
  `Condition="$(ThisRoslynVersion) == $(RoslynApiMaxVersion)"` on the
  `Metalama.Framework.Implementation.Package` reference.

### 2.3 Target frameworks of the test matrix (where they are declared)

Core test projects:

| Project | TFMs |
|---|---|
| `Metalama.Testing.AspectTesting` | `net472;net10.0` (csproj line 7) |
| `Metalama.Testing.UnitTesting` | `net472;net10.0` (csproj line 6) |
| `Metalama.Testing.Hooks` (`Metalama.Backstage/src/…`) | `netstandard2.0;net10.0` (csproj line 4) |
| `Metalama.Framework.Tests.AspectTests` | `net48;net10.0` (csproj line 14) |
| `Metalama.Framework.Tests.LinkerTests` | `net48;net10.0` (line 14) |
| `Metalama.Framework.Tests.TemplateTests` | `net48;net10.0` (line 14) |
| `Metalama.Framework.Tests.UnitTests` | `net48;net10.0` (line 7) |
| `Metalama.Framework.Tests.UnitTestHelpers` | `net48;net10.0` (line 8) |
| `Metalama.Framework.Tests.Workspaces` | `net10.0` ("to match Metalama.Framework.Workspaces") |
| `Metalama.Framework.Tests.Benchmarks` | `net10.0` ("Microsoft.CodeAnalysis.Workspaces.MSBuild 5.10 ships assets for net10.0 and net472 only") |
| `Metalama.DesignTime.HostSimulator` | `net10.0` (comment: must match the SDK that builds the simulated solution) |
| `Metalama.Framework.Engine.Analyzers.Tests`, `Metalama.Framework.Analyzers.Tests` | `net10.0` |
| `Metalama.AspectWorkbench` | `net10.0-windows` |
| `Metalama.Framework.TestApp{,.TestRunner}` | `net10.0`; `.Aspects`, `.Library` `netstandard2.0` |
| `Deprecated/Metalama.Reactive.UnitTests` | `net10.0` |
| `Utilities/SyntaxCover` | `netcoreapp3.1` (stale) |

The `net48` leg is the only place where `NETFRAMEWORK` is defined, which is what the `@RequiredConstant`
idiom below actually keys on.

Standalone scenarios: 60+ `.csproj` files, almost all `net10.0` or `netstandard2.0`. Non-uniform ones:

```
Standalone/BlazorApp/BlazorApp.csproj:8          net$(NETCoreAppMaximumVersion)   ← follows the installed SDK
Standalone/Issue1741/Issue1741.csproj:10         net$(NETCoreAppMaximumVersion)   ← same
Standalone/HtmlWriterAspectTest/…csproj:4        net472;net10.0
Standalone/Issue30200/Issue30200.csproj:4        net4.8;net10.0-windows
Standalone/Issue1710/Issue1710.App/…csproj:19    net472
Standalone/DefaultLanguageVersion/DotNetFramework/…csproj:6  net48
Standalone/Issue1789/Issue1789.csproj:13         MetalamaCompileTimeTargetFrameworks = netstandard2.0;net8.0;net48
Standalone/{Issue1585,Issue1585b,CompiledBindingsWpf}                 net10.0-windows
```

The `SupportedPlatform.*` family is the explicit .NET-target-framework matrix, and it is the part most
directly affected by .NET 11:

```
SupportedPlatform.TestedTargetFrameworks/…csproj:13   net472;net48;netstandard2.0;netstandard2.1;net10.0;net10.0-windows
      comment lines 8-10: "'net481', 'net11.0' and 'net11.0-windows' are also in the tested matrix but are
      not listed here, because the build agents do not have their targeting packs."
      test.json: ForbiddenDiagnosticsRegexes [ LAMA0600, LAMA0601, LAMA0602 ]
SupportedPlatform.MultiTargeting/…csproj:13           net462;net8.0;net9.0
      test.json expects LAMA0600 for net462, net8.0, net9.0 and LAMA0601 for Test.FutureSdk
SupportedPlatform.UntestedTargetFramework/…csproj:8   net8.0   → expects "warning LAMA0600.*'net8[.]0'"
SupportedPlatform.Exclusion/…csproj:9                 net8.0
SupportedPlatform.NoWarn/…csproj:12                   net8.0
SupportedPlatform.CheckDisabled/…csproj:9             net8.0
SupportedPlatform.MetalamaDisabled/…csproj:9          net8.0
SupportedPlatform.ContributedRequirements/…csproj:15  net10.0
```

The product-side declaration these assert against lives outside the subsystem, in
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:26-41`
(`MinimumNETCoreAppVersion 10.0`, `MaximumNETCoreAppVersion 11.0`, `MinimumSdkVersion 10.0`,
`MaximumSdkVersion 11.0`, `MinimumVisualStudioVersion 18.0`). It is already .NET 11 aware; the test
scenarios are not.

### 2.4 Runtime-conditional compilation inside the test framework

```
Metalama.Testing.AspectTesting/BaseTestRunner.cs:389            #if NETFRAMEWORK   → injects IsExternalInit
Metalama.Testing.AspectTesting/AspectTestRunner.cs:22, 42       #if NET5_0_OR_GREATER  (usings, _consoleLock)
Metalama.Testing.AspectTesting/AspectTestRunner.cs:239-241      #if NET5_0_OR_GREATER  → await ExecuteTestProgramAsync(...)
Metalama.Testing.AspectTesting/AspectTestRunner.cs:294-455      #if NET5_0_OR_GREATER  → ExecuteTestProgramAsync + FindProgramMain
Metalama.Testing.AspectTesting/AspectTestRunner.cs:546-566      #if DEBUG
Metalama.Testing.UnitTesting/TestContext.CreateRoslynCompilation.cs:88   #if NET5_0_OR_GREATER (standard library names)
Metalama.Testing.UnitTesting/TestContext.CreateRoslynCompilation.cs:119  #if NETFRAMEWORK (force-load System.Reflection/System.Linq)
Metalama.Testing.UnitTesting/TestCompileTimeDomainFactory.cs:29  #if NET5_0_OR_GREATER (unloadable CompileTimeDomain)
Metalama.Testing.UnitTesting/MemoryDumpHelper.cs:37              #if NET6_0_OR_GREATER || NETFRAMEWORK
```

Consequence: **the `.t.txt` program-output snapshot is only produced and compared on the `net10.0` leg.**
On `net48` the transformed program is never executed.

### 2.5 Hard-coded `net10.0` and SDK versions

```
eng/src/DesignTimeSolution.cs:42          private const string _simulatorTargetFramework = "net10.0";
                                          (lines 101-107 report a clear error if the simulator dll is missing)
eng/src/Program.cs:52                     DotNetSdkVersion = new DotNetSdkVersion( PreferredVersions.DotNetSdk.V_10_0 ) { AllowPrerelease = true }
eng/src/Program.cs:54                     MSBuildVersion = new Version( 17, 14 )
Metalama.Framework.Tests.UnitTests/TestFramework/FakeMetadataReader.cs:26-27      "net10.0", "net10.0"
Metalama.Framework.Tests.UnitTests/TestFramework/TestExecutorTests.cs:56-57       "net10.0", "net10.0"
Metalama.Framework.Tests.UnitTests/TestFramework/AspectTestRunnerTests.cs:152-153 "net10.0", "net10.0"
docker/{linux-x64,win-x64}/*/Dockerfile   ARG DOTNET_VERSION=10.0.302
docker/*/*/global.json                     { "sdk": { "version": "10.0.302" } }
docker/win-x64/ReferenceAssemblyArchitectureMismatch/Dockerfile:18,22
                                           ARG SDK_X64=10.0.302 / ARG SDK_X86=8.0.423
docker/linux-x64/*/Dockerfile:1            FROM ubuntu:24.04
docker/win-x64/*/Dockerfile                FROM mcr.microsoft.com/windows/servercore:ltsc2025
Metalama.Testing.AspectTesting.targets:54  ThisRoslynVersionNoPreview fallback "5.0.0"
```

`docker/Directory.Build.props` imports `eng/Versions.props`, so the docker scenarios inherit repository
versions but pin their SDK independently through `global.json` and `ARG DOTNET_VERSION`.

The docker suite is CI-only, driven by `Metalama.Framework/src/tests/docker/DockerTests.ps1` (which locates
a `DockerBuild.ps1` by walking parent directories, lines 42-56) and registered as two extra CI
configurations in `eng/src/Program.cs:203-220` (`DockerTestsWinX64`, `DockerTestsWslX64`).

### 2.6 Host-IDE sensitivity

The only design-time *host* simulation in the subsystem is
`Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator`:

- `MSBuildEnvironment.cs:27-99` — registers the SDK found by parsing `dotnet --list-sdks`
  (lines 76-99), not `MSBuildLocator.QueryVisualStudioInstances()`. It mirrors
  `Metalama.Framework.Workspaces.MSBuildInitializer`.
- `DesignTimeHost.cs:34-47` — `properties.TryAdd( "DesignTimeBuild", "true" )` (and
  `BuildingInsideVisualStudio`), then `MSBuildWorkspace.Create( properties )`.
- `IsolatedAnalyzerAssemblyLoader.cs:15-111` — one `AssemblyLoadContext` per analyzer directory,
  reproducing Roslyn's `DirectoryLoadContext`; the host wins every resolution so Roslyn types are unified
  (line 111 and following).
- `ProjectDesignTimeSession.cs:130-136` — `CSharpGeneratorDriver.Create(...)` with
  `(CSharpParseOptions?) this._project.ParseOptions`, that is, the language version MSBuild computed.
  This is the **only** place in the subsystem where the language version comes from a real project
  evaluation rather than from `SupportedCSharpVersions`.
- The csproj comment (lines 12-15) records that the project deliberately does not reference Metalama, so
  it exercises whatever payload each scenario restored — including two Metalama versions in one solution.

The simulator does **not** simulate Visual Studio's two-process split, incremental editing, or Roslyn's
concurrent scheduling (documented in `Metalama.Framework/docs/testing.md:244`).

---

## 3. How the C# 14 wave (#1034 … #1160) was absorbed

### 3.1 The test framework itself barely moved

`git log --grep` over the nineteen issue numbers, restricted to
`Metalama.Testing.AspectTesting`, `Metalama.Testing.UnitTesting`, `Metalama.Testing.Hooks` and
`docs/testing.md`, returns exactly **one** commit:

`b4da95860584ae6463439ec679a09f5a58a3f608` "Null-conditional assignments including #1108 (invokers)",
whose only change in the framework was `SyntaxTreeStructureVerifier.cs`:

```diff
-        foreach ( var syntaxTree in compilation.SyntaxTrees )
+        foreach ( var syntaxTree in compilation.SyntaxTrees.Where( t => !CompileTimeConstants.IsPredefinedSyntaxTree( t.FilePath ) ) )
```

That is the whole of the framework change for the C# 14 wave. **The pattern is: the framework is not
touched; the suites are.**

### 3.2 The "enable the language version" commit

`2c8c1c818935651ae73ac08294c9975c4af7c11b` "Fixing build issues and tests." (2025-09-12) is the commit that
raised `SupportedCSharpVersions.Latest` to C# 14, and it shows exactly what a language bump costs inside
this subsystem:

- Two checked-in expected files that render `SupportedCSharpVersions.All` verbatim:
  - `Tests/Aspects/Misc/LanguageVersion.t.cs` (LAMA0052) —
    `'10.0', '11.0', '12.0', '13.0'` → `'10.0', '11.0', '12.0', '13.0', '14.0'`.
  - `Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs` (LAMA0051) — the same list.
- One expected file changed because the *code model* changed shape:
  `Tests/Aspects/Overrides/Fields/BackingFieldAdvice_Error.t.cs` —
  `'TargetClass.<AutoProperty>k__BackingField'` → `'TargetClass.AutoProperty.field'`.
- One test split per Roslyn variant, using the constant idiom:
  `Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs` gained
  `// @RequiredConstant(ROSLYN_4_12_0_OR_EARLIER)` (later reworked into the
  `ROSLYN_5_10_0_OR_GREATER` / `_Roslyn5_0` pair that exists today).
- Two analyzer suppressions added to `eng/style/AspectTests.editorconfig`:
  `dotnet_diagnostic.SA1402.severity = none` and `dotnet_diagnostic.IDE0025.severity = none`
  (IDE0025 is "use expression body for property", which the `field` keyword makes fire).
- `Metalama.Framework/src/Metalama.Framework.Package/build/RoslynVersion/Roslyn.{4.8.0,4.12.0,5.0.0}.props`
  were adjusted in the same commit.

### 3.3 The `Tests/Aspects/CSharp14/**` suite

Structure today, 61 input files in six per-feature subdirectories:

```
Tests/Aspects/CSharp14/
  CompoundAssignmentOperator/   3 tests   (#1116)
  ExtensionMembers/            17 tests   (#1034, #1035, #1036, #1127, #1159)
  FieldKeyword/                20 tests   (#1094, #1105, #1114)
  NullConditionalAssignment/    5 tests   (#1108, #1109)
  PartialConstructor/           9 tests   (#1110, #1111, #1143)
  PartialEvent/                 3 tests   (#1112, #1113)
  SimpleLambdaModifier/         1 test
```

History (`git log --diff-filter=A`): the directory started **flat** in January 2026 (`929d055d85`,
`aa5e62dbb0`, `06116b398b`, all "(#1114)"), then was reorganised into per-feature subdirectories
(`b789173193` "Refactor code and add target markers to tests (#1114)"), and grew regression tests later
(`6b0c0eeda2` #780, `70b606eaaa`/`6ca2af83d0` #1644).

**How the tests declare their requirements.** Every directive in the whole suite:

- 20 files carry only `// @RequiredConstant(NET8_0_OR_GREATER)`.
- 3 files carry `// @TestScenario(DesignTime)`
  (`ExtensionMembers_Introduce_DesignTime`, `PartialConstructor_IntroduceParameter_DesignTime`,
  `PartialConstructor_IntroduceParameter_DefinitionOnly_DesignTime`).
- 1 file carries `// @IncludeAllSeverities`, 1 carries `// @FormatOutput`.
- **No file in the suite carries `@LanguageVersion`.** The language version comes from
  `SupportedCSharpVersions.DefaultParseOptions`, that is, from `SupportedCSharpVersions.Latest`.

This matches the older suites:

```
CSharp11/  12 files, 4 with @RequiredConstant(NET6_0_OR_GREATER | NET7_0_OR_GREATER)
CSharp12/  18 files, 5 with @RequiredConstant(NETCOREAPP3_0_OR_GREATER | NET8_0_OR_GREATER)
CSharp13/  17 files, 5 with @RequiredConstant(NET9_0_OR_GREATER | NET5_0_OR_GREATER)
CSharp14/  61 files, 20 with @RequiredConstant(NET8_0_OR_GREATER)
```

`@RequiredConstant(NETx_0_OR_GREATER)` is used for a **runtime** requirement (`System.Threading.Lock`,
`[OverloadResolutionPriority]`, `InlineArray`, `RequiredMemberAttribute`, ref fields) or, in the C# 14
suite, simply as an idiom meaning "skip the `net48` leg". Since the matrix is now `net48;net10.0`, every
`NET5_0_OR_GREATER` … `NET9_0_OR_GREATER` constant means exactly the same thing: "not `net48`".

Snapshot files that accompany a C# 14 test:

- `X.t.cs` for every test (mandatory; `BaseTestRunner.ExecuteAssertions`, BaseTestRunner.cs:843-861,
  asserts that every checked-in `X.*.{t,i,ct}.cs` was actually written).
- `X.0.i.cs`, `X.1.i.cs` for a `@TestScenario(DesignTime)` test. See
  `Tests/Aspects/CSharp14/ExtensionMembers/ExtensionMembers_Introduce_DesignTime.{0,1}.i.cs`, which show
  the design-time pipeline emitting `static partial class C { extension(global::System.Int32 test) { … } }`
  in a separate document.
- `X.t.txt` where the transformed program is executed
  (`FieldKeyword_Override_SemiAutoTarget.t.txt`, `…_AutoGetter.t.txt`).
- `X.Dependency.cs` for a cross-project test (`FieldKeyword_Override_CrossProject.Dependency.cs`).

### 3.4 The standalone scenario the wave produced

`Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/` (issue #1896) is the one standalone
scenario dedicated to the wave. Its `README.md` is the model to copy:

> Asserts that a template may be written in C# 14. See issue #1896. … The `field` keyword is represented by
> a syntax node that Roslyn 5.0 added, so the verification reports `LAMA0232`, "Template code must be
> written in C# 13.0", while that property is `13.0`. There is no `test.json`: the assertion is that the
> scenario builds and runs cleanly … The project sets `LangVersion` to `14.0` of its own, so that the
> source language version is not what the scenario measures … The value that this scenario guards is
> bounded by the lowest Roslyn version that a supported host presents, because a template is compiled by
> the Roslyn of the host. That version is `RoslynApiMinVersion` in `Directory.Packages.props`, and the
> platform baseline that decides it is `Metalama.Framework/docs/platform-support.md`.

### 3.5 What a `CSharp15` suite would look like

Direct extrapolation of the above:

```
Tests/Aspects/CSharp15/
  Union/                     union declarations: override, introduce, contracts, design time
  UnsafeExpression/          unsafe(expr) in templates and in target code
  WithElement/               with(...) in a collection expression
  LabeledBreakContinue/      break/continue with an optional Name
  ClosedModifier/            the 'closed' contextual modifier
  ExtensionIndexer/          an indexer declared inside an extension block
```

Each test: one `X.cs` with `#if TEST_OPTIONS … #endif` carrying at most
`// @RequiredConstant(NET10_0_OR_GREATER)` (only if a BCL type is needed) and
`// @TestScenario(DesignTime)` where applicable, a `// <target>` marker, and a committed `X.t.cs` produced
by running the test and accepting `obj/transformed/<tfm>/…`. **No `@LanguageVersion(15)`**, because
(a) that directive silently skips on a Roslyn that does not parse `15` and (b) the suite is meant to run
at the project default.

Prerequisites before a single such file can even compile:

1. `Metalama.Framework/Directory.Build.props:45` `LangMaxVersion` → `15.0`, otherwise the payload file
   fails to compile in the test project itself.
2. `Metalama.Framework.Tests.UnitTests/…csproj:13` `<LangVersion>` → `15.0` for a C# 15 unit test.
3. `SupportedCSharpVersions.Latest` / `.All` / `GetMaxLanguageVersion` / `RoslynApiVersion.V5_10_0 =>` →
   `CSharp15`, otherwise `SupportedCSharpVersions.DefaultParseOptions` cannot parse the payload.
4. `Directory.Build.props:16` `MetalamaTemplateLanguageVersion` → `15.0` only if `RoslynApiMinVersion`
   moves to a Roslyn that parses C# 15, per the reasoning in
   `Standalone/TemplateLanguageVersion14/README.md`. If it stays at `5.0.0`, templates stay at 14.0 and a
   `Standalone/TemplateLanguageVersion15` scenario must *expect* `LAMA0232` rather than success.
5. The four grammar additions are all marked `ExperimentalUrl` in
   `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` (lines 496, 816, 1954, and the
   `Break`/`Continue` `Name` field), so a `// @LanguageFeature(...)` flag may be required per feature until
   they ship stable. `@LanguageFeature` is the directive for that (TestOptions.cs:723).

Files that must be edited for the wave, at minimum:

```
Tests/Aspects/Misc/LanguageVersion.t.cs             the LAMA0052 supported-version list
Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs   the LAMA0051 supported-version list
TestSyntaxTree.cs:200-221                            add SyntaxKind.UnionDeclaration (and ExtensionBlockDeclaration)
TestResult.cs:548-569                                the same two kinds
eng/style/AspectTests.editorconfig                   plus its three identical copies (§4.5)
Standalone/TemplateLanguageVersion15/                new scenario, modelled on …14
Standalone/SupportedPlatform.TestedTargetFrameworks  add net11.0 / net11.0-windows once agents have the packs
```

---

## 4. Extension points, by kind of language change

### 4.1 A NEW kind of type declaration (`union`)

| Must change | Why |
|---|---|
| `Metalama.Testing.AspectTesting/TestSyntaxTree.cs:200-221` | else `ArgumentOutOfRangeException` when the union is the document root |
| `Metalama.Testing.AspectTesting/TestResult.cs:548-569` | else `InvalidOperationException` when the union carries `// <target>` |
| `LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs:49-89` | add a `VisitUnionDeclaration` that calls `RewriteTypeDeclaration`; without it the union never pushes onto `_currentTypeStack` |
| `LinkerTests/Runner/LinkerTestInputBuilder.TestRewriter.cs:112-152` | the matching override for the outer rewriter |
| `LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs:341-350, 555-568, 641-696` | the three closed member switches, if a union member may be introduced or overridden |
| `Tests/Aspects/CSharp15/Union/**` | the new payloads plus `.t.cs` (and `.i.cs` for design time) |

No change needed in: the discoverer, the directive parser, `TestOptions`, the runners, the HTML writer.

### 4.2 A NEW modifier (`closed`)

No syntax node is added, so nothing in the subsystem switches on it. What matters:

- `Metalama.Framework/Directory.Build.props:45` `LangMaxVersion` must accept the modifier in payloads.
- `LinkerTestInputBuilder.TestTypeRewriter.cs:501-502` reads
  `node.Modifiers.Any( m => m.IsKind( SyntaxKind.NewKeyword ) )` — the only modifier inspection in the
  subsystem, and it is a positive test, so a new modifier passes through.
- The expected `.t.cs` files change wherever the modifier survives the transformation. That is a baseline
  refresh, not code.
- `eng/style/AspectTests.editorconfig` may need a new suppression if an IDE analyzer starts suggesting
  the modifier (this is exactly what happened with `IDE0025` for C# 14).

### 4.3 A NEW expression form (`unsafe(expr)`)

- Nothing enumerates expression kinds in the subsystem. The relevant guard is
  `SyntaxTreeStructureVerifier.Verify` (`SyntaxTreeStructureVerifier.cs:52-85`), which reparses the output
  with the tree's own options and reports error diagnostics — that is what catches a syntax generator
  emitting the new form incorrectly.
- `TestOutputNormalizer.NormalizeTestOutput` (line 22) will reparse it with default options; see §5.1.
- `AspectTests.csproj:16` already sets `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`, so an `unsafe`
  payload compiles in the test project.
- `LinkerTestInputBuilder.TestMethodBodyRewriter.cs` overrides only `VisitInvocationExpression` (32),
  `VisitElementAccessExpression` (51), `VisitMemberAccessExpression` (175), `VisitIdentifierName` (188);
  a new expression form containing one of those is visited transitively and needs no change.

### 4.4 A NEW collection-expression element (`with(...)`)

`WithElementSyntax : CollectionElementSyntax` (`Syntax-5.10.0.xml:816-817`). `CollectionElementSyntax` is
not a `MemberDeclarationSyntax`, so neither of the two big switches is involved. Nothing in the subsystem
enumerates collection elements. The whole cost is:

- a `Tests/Aspects/CSharp15/WithElement/**` payload plus baselines, modelled on
  `Tests/Aspects/CSharp12/CollectionExpressions.cs` and `CollectionExpressions_Error.cs`;
- possibly a `@LanguageFeature` flag while the feature is experimental.

### 4.5 A NEW optional field on an existing statement (labeled `break` / `continue`)

`BreakStatementSyntax` and `ContinueStatementSyntax` gain an optional `Name` of type
`IdentifierNameSyntax`. Nothing in the subsystem constructs or destructures those statements, so the only
exposure is:

- the generated meta-syntax rewriter behind `ToSyntaxFactoryDebug`
  (`SyntaxTreeStructureVerifier.VerifyMetaSyntax`, line 30 and 37) — a node whose new field the generator
  does not know is rendered without it, and the round trip then compares equal against an equally
  truncated reparse. That is a **false pass**, and it is the reason the generator's
  `Syntax-<version>.xml` must be regenerated before the suite is trusted;
- the `.t.cs` baselines of any test whose transformed output contains a labeled `break`.

### 4.6 Adding a whole new directive to the framework

If the wave needs one (for example `@TemplateLanguageVersion`, which does not exist today although
`TestContextOptions.TemplateLanguageVersion` at `Metalama.Testing.UnitTesting/TestContextOptions.cs:167`
and `TestProjectOptions.cs:127` do), the four edits are:

1. a property on `TestOptions` (with the `/// To set this option in a test, add this comment…` doc form);
2. a `case` in the `switch` at `TestOptions.cs:528`;
3. a `??=` line in `ApplyBaseOptions` (TestOptions.cs:407-501) so `metalamaTests.json` can carry it;
4. a forward in `ApplyToTestContextOptions` (TestOptions.cs:857-872) if it belongs to `TestContextOptions`.

Note the asymmetry that already exists: `TestContextOptions.TemplateLanguageVersion` is reachable from a
**unit** test (`Metalama.Framework.Tests.UnitTests/CompileTime/CompileTimeCompilationBuilderTests.cs:1915-1966`
sets `new TestContextOptions() { TemplateLanguageVersion = … }`) but from **no aspect test**. A C# 15
template-language-version scenario therefore has to be a standalone test, as `TemplateLanguageVersion14`
is, or the directive has to be added.

---

## 5. Where the subsystem silently does the wrong thing

### 5.1 The golden-file comparison reparses with the wrong language version

`TestOutputNormalizer.NormalizeTestOutput` (TestOutputNormalizer.cs:22) calls
`CSharpSyntaxTree.ParseText( s )` with no `CSharpParseOptions` and never looks at the diagnostics. Both the
**actual** transformed text (BaseTestRunner.cs:575, 578) and the **expected** `.t.cs` text
(BaseTestRunner.cs:592) go through it, and the result is then whitespace-normalised by
`SyntaxNode.NormalizeWhitespace` (TestOutputNormalizer.cs:33).

If the transformed output contains a construct the default parse options do not accept, both sides are
mangled the same way and the comparison **passes**. The test then asserts nothing about the construct it
was written for. `@LanguageVersion(preview)` and `@LanguageFeature(preview)` do not reach this function at
all, so a preview-gated construct is exactly the case that hits it.

The `.ct.cs` compiled-template path (BaseTestRunner.cs:698, 701, 713) has the same defect.

### 5.2 A `@RequiredConstant` naming an undefined constant skips the test forever

`TestInput.cs:76-83` only checks membership; there is no validation that the constant is one the build ever
defines. Two tests are currently in this state:

```
Tests/Aspects/Introductions/InterfaceImplementation/Operator.cs:7           // @RequiredConstant(ROSLYN4_4_OR_GREATER)
Tests/Aspects/Introductions/InterfaceImplementation/Operator_Explicit.cs:7  // @RequiredConstant(ROSLYN4_4_OR_GREATER)
```

`ROSLYN4_4_OR_GREATER` is defined **nowhere** in the repository (the constants the build defines are
`ROSLYN_5_10_0_OR_GREATER` and the standard `NETx_0_OR_GREATER` family). Both files also guard their body
with `#if NET8_0_OR_GREATER && ROSLYN4_4_OR_GREATER`, so they compile to an empty file and are reported as
skipped on every leg of every variant. A skip is not a failure, and the CI does not gate on skip counts.

The same hazard applies to `@ForbiddenConstant` (a wrong name never matches, so the test always runs) and
to `@TargetFrameworks` (a wrong name never matches the current TFM, so the test always skips).

### 5.3 `@LanguageVersion` on an unrecognised version silently skips

`TestOptions.cs:687-694` (and 708-715 for the dependency): any integral value ≥ 10 that
`LanguageVersionFacts.TryParse` rejects becomes `SkipReason`, not an error. `// @LanguageVersion(15)` on a
Roslyn that does not know C# 15 skips the test with a message nobody reads. A whole `CSharp15` suite
written with that directive can be entirely inert while the run is green.

### 5.4 The `.t.txt` program-output snapshot is discarded when the program produces nothing

`AspectTestRunner.SaveResultsAsync` (AspectTestRunner.cs:479-519):

```csharp
if ( !string.IsNullOrWhiteSpace( actualProgramOutput ) ) { …read the expected file… }
else
{
    expectedProgramOutput = "";          // ← the checked-in .t.txt is never read
    if ( File.Exists( expectedProgramOutputPath ) && string.IsNullOrWhiteSpace( File.ReadAllText( … ) ) ) { File.Delete( … ); }
    …
}
```

`ExecuteAssertions` (line 525-540) then compares `""` with `""` and passes. So a test whose program stops
running still passes and its committed `.t.txt` is silently ignored. Three ways to land there:

- `FindProgramMain` (AspectTestRunner.cs:385-453) returns `null` when there is no type named `Program` or
  no method of the configured name (`MainMethod`, `"TestMain"` at the suite root). A renamed class or
  method silently disables execution.
- the `net48` leg, where `ExecuteTestProgramAsync` is not compiled at all (§2.4);
- `@DisableExecuteProgram`.

The `.t.cs` / `.i.cs` / `.ct.cs` files are protected against this by the
"Verify that all expected files have been written" loop (BaseTestRunner.cs:843-861), which globs
`testInput.TestName + ".*.cs"` and asserts each was written. **`.t.txt` is not covered by that loop.**

### 5.5 Orphaned test payload directories that are no longer in any project

`Metalama.Framework/docs/testing.md:12` and :179-185 describe
`Metalama.Framework.Tests.AspectTests.Internals` and `Metalama.Framework.Tests.PublicPipeline` as test
projects. Neither has a `.csproj` any more, and neither appears in `Metalama.Framework.sln` or in
`Metalama.Framework.LatestRoslyn.slnf`. Seven payload files survive and are discovered by nothing:

```
Metalama.Framework.Tests.AspectTests.Internals/Tests/Templating/Dynamic/DynamicForbiddenCases.cs (+ .t.cs)
Metalama.Framework.Tests.AspectTests.Internals/Tests/Templating/Dynamic/Invoke.cs (+ .t.cs)
Metalama.Framework.Tests.AspectTests.Internals/Tests/Templating/Syntax/Misc/ElementAccessError.cs (+ .t.cs)
Metalama.Framework.Tests.PublicPipeline/Tests/Aspects/Initialize/ServicePlugIn.cs
```

Discovery walks from `MetalamaTestSourceDirectory`
(`Metalama.Framework.Tests.AspectTests.csproj:6`, the project directory), so sibling directories are
outside its reach. Nothing reports these as missing.

Related dead weight: the five `*.4.12.0` test directories (obj-only), the never-built
`Metalama.Framework.Tests.Benchmarks.5.0.0` (with `BenchmarkRoslynVersion 4.14.0`), and a committed
WPF temporary project `Metalama.AspectWorkbench/Metalama.AspectWorkbench_3gb1zv23_wpftmp.csproj`
that pins `net8.0-windows` and absolute machine paths.

### 5.6 `VerifyMetaSyntax` reparses with `SupportedCSharpVersions.DefaultParseOptions`

`SyntaxTreeStructureVerifier.cs:32-37` ignores the tree's own parse options. For a test running under
`@LanguageVersion(preview)` or an older `@LanguageVersion`, the comparison is against a differently-parsed
tree. It is only reachable from the AspectWorkbench (MainViewModel.cs:259), so it does not affect CI, but
the same pinning in `LinkerInlineAssertionWalker.cs:34-35` **is** on the linker-test path.

### 5.7 The linker test rewriter relocates members of an unhandled type declaration

`LinkerTestInputBuilder.TestTypeRewriter` handles class, record and struct only. A member declared inside
an interface, an extension block, or a future union is still visited by `VisitMethodDeclaration` (line 119)
and friends, each of which does `this._currentTypeStack.Peek().Members.AddRange( … ); return null;` — it is
removed from its real parent and appended to the **enclosing** class's member list, or, with an empty
stack, throws `InvalidOperationException` from `Stack<T>.Peek`. Neither outcome names the cause.

### 5.8 `SupportedPlatform` scenarios encode the matrix by hand

`SupportedPlatform.TestedTargetFrameworks/…csproj:8-10` states in a comment that `net481`, `net11.0` and
`net11.0-windows` are in the tested matrix but omitted "because the build agents do not have their
targeting packs". Nothing verifies that the `TargetFrameworks` list stays in step with
`Metalama.Framework.props:26-41`. When `MaximumNETCoreAppVersion` moves, the scenario keeps passing while
covering less. Conversely, `SupportedPlatform.MultiTargeting` and four other scenarios *assert* `LAMA0600`
for `net8.0` / `net9.0`; if those floors move again, the assertions become vacuous rather than red.

### 5.9 Documentation drift in `docs/testing.md`

Beyond §5.5: line 41 says the `.5.0.0` siblings exist for `Benchmarks` (it does, but it is not in the
solution and is never built); line 147 documents `@TargetFrameworks(net10.0;net472)` while
`TestOptions.cs:287` still documents `net8.0;net472`; line 62 says the unit-test TFMs are `net48;net10.0`
and the framework's are `net472;net10.0`, which is correct.

---

## 6. Quick index of the files to touch for the C# 15 / .NET 11 wave

Framework code (few, and all in `Metalama.Testing.AspectTesting`):

```
TestSyntaxTree.cs:192-231      add the new type-declaration kinds
TestResult.cs:520-585          add the same kinds
TestOutputNormalizer.cs:22     pass CSharpParseOptions (fix §5.1)
TestOptions.cs:681-720         decide whether an unparsable @LanguageVersion should still skip (§5.3)
AspectTestRunner.cs:479-519    stop discarding a committed .t.txt (§5.4)
```

Configuration:

```
Metalama.Framework/Directory.Build.props:45         LangMaxVersion
Directory.Build.props:16                            MetalamaTemplateLanguageVersion
Directory.Build.props:9                             the IDE0032/IDE0031 NoWarn
Metalama.Framework.Tests.UnitTests/…csproj:13       LangVersion
eng/style/AspectTests.editorconfig                  + the three identical copies under
                                                    {AspectTests,TemplateTests,LinkerTests}/Tests/.editorconfig
eng/RoslynVersions/*.props                          if a variant is added or removed
eng/src/Program.cs:52,54                            DotNetSdkVersion, MSBuildVersion
eng/src/DesignTimeSolution.cs:42                    _simulatorTargetFramework
Metalama.Testing.AspectTesting.targets:54           the ThisRoslynVersionNoPreview package fallback
docker/*/*/Dockerfile (ARG DOTNET_VERSION), docker/*/*/global.json
```

Suites and baselines:

```
Tests/Aspects/CSharp15/**                                   new
Tests/Aspects/Misc/LanguageVersion.t.cs                     LAMA0052 version list
Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs   LAMA0051 version list
Standalone/TemplateLanguageVersion15/                       new, modelled on …14
Standalone/SupportedPlatform.TestedTargetFrameworks/        net11.0, net11.0-windows
Standalone/SupportedPlatform.{MultiTargeting,UntestedTargetFramework,Exclusion,NoWarn,CheckDisabled,MetalamaDisabled}
                                                            re-pick the "unsupported" TFMs once net8.0/net9.0 stop being illustrative
Metalama.Framework/docs/testing.md                          §5.5 and §5.9 corrections, plus the CSharp15 conventions
```

Downstream (outside this repository, same framework): `Metalama.Premium/src/tests/*.AspectTests` and
`*.UnitTests` still target `net8.0` / `netframework4.8` and consume `Metalama.Testing.AspectTesting` as a
package, so they take the hard-coded `ThisRoslynVersionNoPreview = 5.0.0` path.
