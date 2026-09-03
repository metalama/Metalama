# Terrain map: build, packaging and target frameworks

Subsystem scope: `eng/**`, `Directory.Packages.props`, `Directory.Packages.md`, `Directory.Build.props`,
`global.json`, `nuget.base.config`, `Metalama.Framework.CompilerExtensions.Resources`, the Package projects,
`Metalama.Framework/docs/platform-support.md` and `compile-time-target-frameworks.md`.

Repository: `C:\src\Metalama-2027.0\Metalama`, branch `topic/2027.0/26-09-03-net11-impact`.
Companion repository read: `C:\src\Metalama-2027.0\Metalama.Premium`, branch
`topic/2027.0/1829-durable-and-immutable-contracts`.

All paths below are repository-relative unless stated otherwise.

---

## 0. Executive summary

The build subsystem is sensitive to the C# language set in exactly **one** place: the grammar-driven code
generator `eng/src/GenerateMetaSyntaxRewriter`, which reads `Syntax-<roslyn version>.xml` snapshots of Roslyn's own
`src/Compilers/CSharp/Portable/Syntax/Syntax.xml` and emits five generated files per Roslyn variant. Everything
else in the subsystem is sensitive to *versions* (target framework, .NET SDK, Roslyn, host integrated development
environment), not to language shape.

Three facts dominate the C# 15 / .NET 11 work:

1. **The four C# 15 grammar additions are already present in `Syntax-5.10.0.xml` but are deliberately deleted
   before code generation**, by `TreeReader.RemoveExperimentalDeclarations`, because they carry `ExperimentalUrl`.
   Turning C# 15 on is therefore not a grammar-file edit; it is a decision about that filter, plus a refreshed
   grammar snapshot once the features go stable upstream.
2. **`net11.0` is already declared supported to users** in the platform requirement matrix
   (`MaximumNETCoreAppVersion` 11.0, `MaximumSdkVersion` 11.0), but **no shipped asset targets `net11.0`** and
   several derived values still cap the language at C# 14 and the SDK at major 10.
3. **The single most dangerous line in the subsystem is the implicit-`LangVersion` clamp** at
   `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118`, whose whitelist is a
   literal `'12.0' / '13.0' / '14.0'`. A `net11.0` project whose SDK implicitly sets `LangVersion` to `15.0`
   matches the condition and is **silently downgraded to C# 12**.

---

## 1. Files and types sensitive to the shape of the C# language

### 1.1 The grammar-driven generator (the only real language-shape dependency)

`eng/src/GenerateMetaSyntaxRewriter/` is a console library invoked from the `prepare` step
(`eng/src/Program.cs:250`, `GenerateMetaSyntaxRewriter.Generate( srcDirectory )`).

#### `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs`

The version list is a hard-coded literal array.

- L16: `var deprecatedVersionNames = Array.Empty<string>();`
- L17: `string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0"];` — grammar snapshots read for
  *version detection* only; no code is generated for them.
- L18: `string[] versionNames = [.. legacyVersionNames, "5.0.0", "5.10.0"];`
- L37: output directory `Path.Combine( baseDirectory, ".generated", $"{syntax.Version.Name}" )`.
- L39–L48, the five generated artifacts per non-legacy version:
  - `Metalama.Framework.Engine/RoslynApiVersion.g.cs` (`GenerateRoslynApiVersionEnum`)
  - `Metalama.Framework.Engine/MetaSyntaxRewriter.g.cs` (`GenerateTemplateFiles`, which also emits
    `MetaSyntaxFactoryImpl`)
  - `Metalama.Framework.Engine/RoslynVersionSyntaxVerifier.g.cs` (`GenerateVersionChecker`)
  - `Metalama.Framework.DesignTime/RunTimeCodeHasher.g.cs` and `CompileTimeCodeHasher.g.cs` (`GenerateHasher`)
  - `Metalama.Framework.Engine/SyntaxNodePartialUpdateExtensions.g.cs` (`GeneratePartialUpdate`)

`Metalama.Framework/.generated/` is git-ignored (`.gitignore:62`). It currently holds `4.12.0` (stale leftover of
the dropped variant) and `5.0.0`; `5.10.0` appears only after a `prepare` run.

#### `eng/src/GenerateMetaSyntaxRewriter/Generator.cs` (803 lines)

Every generated construct is derived mechanically from the grammar, so a *new node* needs no generator change.
The places that do encode C# knowledge:

| Lines | Member | What it encodes |
| --- | --- | --- |
| L199–L205 | `IsAutoCreatableToken` | `"IdentifierToken"`, `"…LiteralToken"` suffix; decides which factory overload is emitted |
| L212–L215, L262–L264 | `IsAnyList`, `IsNodeList`, `IsSeparatedNodeList` | literal type-name prefixes `SyntaxList<`, `SeparatedSyntaxList<`, `SyntaxNodeOrTokenList` |
| L301–L389 | `IsKeyword` | a hand-written list of 80 C# keywords used by `CamelCase`/`FixKeyword` to `@`-escape a generated parameter name. Contains `unsafe` (L381), `required` (L382), `file` (L383). **Does not contain `union` or `closed`** (both C# 15 contextual keywords, so both are still legal identifiers; no defect today, but this is the list that would have to grow if a future field name collided with a *reserved* word) |
| L603–L609 | `GenerateMetaSyntaxFactory` | special case `node.Name == "LiteralExpressionSyntax"` for a manually-implemented Roslyn factory |
| L714–L723 | `IgnoreFieldContentInRunTimeCode` | literal node type names `BlockSyntax`, `ArrowExpressionClauseSyntax`, `EqualsValueClauseSyntax`, whose bodies are not hashed for run-time code at design time |
| L725–L735 | `IsTrivialToken` | literal token kinds `StringLiteralToken`, `CharacterLiteralToken`, `NumericLiteralToken`, `IdentifierToken`; everything else is hashed by `RawKind` only |
| L774–L781 | `GeneratePartialUpdate` | maps `SyntaxList<SyntaxToken>` to `SyntaxTokenList`, and appends `?` to an optional non-token field |
| L432–L479 | `GenerateMetaSyntaxRewriter` | emits a `switch ( this.TargetApiVersion )` when a node's fields do not all share one `MinimalRoslynVersion`/`MaximalRoslynVersion`; `default:` throws `AssertionFailedException` |
| L160–L173 | local functions `IsVersionSpecificType`, `IsVersionSpecificField`, `GetVersionSpecificKinds` | drive `RoslynVersionSyntaxVerifier.g.cs` |

#### `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs` — the experimental filter

This is the gate that currently hides all four C# 15 grammar additions.

- L48–L58 `ReadTree`: `SyntaxXmlCleaner.Clean` → `XmlSerializer` → `RemoveExperimentalDeclarations( tree )` →
  `TreeFlattening.FlattenChildren( tree )`.
- L70–L78 `RemoveExperimentalDeclarations`: `tree.Types.RemoveAll( t => t.IsExperimental );` then recursion.
- L90–L109 `RemoveExperimentalChildren`: `children.RemoveAll( c => c is Field { IsExperimental: true } );`
  recursing through `Choice` and `Sequence`.
- Documented rationale (L60–L69): "Roslyn annotates the corresponding API with `ExperimentalAttribute`, which turns
  every reference from generated code into an `RSEXPERIMENTAL` error."

Supporting model members:

- `Model/TreeType.cs:31-32` `[XmlAttribute] public string ExperimentalUrl { get; set; }`;
  `TreeType.cs:37` `public bool IsExperimental => !string.IsNullOrEmpty( this.ExperimentalUrl );`
- `Model/Field.cs:97-98` the same attribute on a field; `Field.cs:103` the same predicate.
- `Model/Field.cs:111-113` `IsToken => this.Type == "SyntaxToken"`, `IsOptional => this.Optional == "true"`.

#### `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs`

Computes, per node and per field, the minimal and maximal Roslyn version in which it exists.

- L36–L48: node `MinimalRoslynVersion`.
- L50–L75: per-field `MinimalRoslynVersion`, `MaximalRoslynVersion`, and `KindsMinimalRoslynVersions`.

This is the machinery that makes "a new optional field on an existing statement" work without generator changes:
`BreakStatementSyntax.Name` would be detected as minimal version `5.10.0` and produce a `TargetApiVersion` switch
in `MetaSyntaxRewriter.g.cs` and a `VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 )` in
`RoslynVersionSyntaxVerifier.g.cs`.

#### `eng/src/GenerateMetaSyntaxRewriter/Model/TreeFlattening.cs`

- L292–L324 `FlattenChildren`: `Choice` children are forced optional (L311, `makeOptional: true`), `Sequence` is
  transparent, `default:` throws `InvalidOperationException( "Unknown child type." )` (L321). A new *kind* of
  grammar child element (neither `Field`, `Choice` nor `Sequence`) would fail loudly here — good.

#### The grammar snapshots

| File | Lines | `<Node ` count |
| --- | --- | --- |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-4.0.1.xml` | 3008 | 237 |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-4.4.0.xml` | 3067 | 240 |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-4.8.0.xml` | 3103 | 243 |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-4.12.0.xml` | 3120 | 245 |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml` | 3199 | 249 |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` | 3245 | 252 |

The five `ExperimentalUrl` sites in `Syntax-5.10.0.xml`, that is, the whole of the C# 15 grammar delta:

- L496 `<Node Name="UnsafeExpressionSyntax" Base="ExpressionSyntax" ExperimentalUrl="…/issues/82789">` — kind
  `UnsafeExpression`, fields `Keyword` (`UnsafeKeyword`), `OpenParenToken`, `Expression`, `CloseParenToken`.
- L816 `<Node Name="WithElementSyntax" Base="CollectionElementSyntax" ExperimentalUrl="…/issues/82210">` — kind
  `WithElement`, fields `WithKeyword`, `ArgumentList` (`ArgumentListSyntax`).
- L1296 `<Field Name="Name" Type="IdentifierNameSyntax" Optional="true" ExperimentalUrl="…/issues/83266" />`
  inside `BreakStatementSyntax`.
- L1307 the same field inside `ContinueStatementSyntax`.
- L1954 `<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" SkipConvenienceFactories="true"
  ExperimentalUrl="…/issues/82567">` — kind `UnionDeclaration`, with `AttributeLists`, `Modifiers`, `Keyword`
  (`UnionKeyword`), `Identifier`, `TypeParameterList`, `ParameterList`, `BaseList`, `ConstraintClauses`,
  `OpenBraceToken`, `Members`, `CloseBraceToken`, `SemicolonToken`, all `Override="true"`.

The two remaining C# 15 features named in the task (`closed` modifier, indexers in an extension block) add no
grammar node and therefore touch nothing in this subsystem: `Modifiers` is already a
`SyntaxList<SyntaxToken>` with no enumeration of allowed tokens anywhere in `eng/`, and an indexer inside an
`ExtensionBlockDeclarationSyntax` is an ordinary `IndexerDeclarationSyntax` member.

### 1.2 Language-version tables outside `eng/` that the build pins feed

These live in `Metalama.Framework.Engine` but are named explicitly by the build procedure in
`Metalama.Framework/docs/updating-roslyn.md:29-34`, so they belong on this map.

- `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs`
  - L31–L32 `Latest => LanguageVersion.CSharp14`
  - L38–L43 `All` = CSharp14, 13, 12, 11, 10
  - L52–L62 `ToLanguageVersion( RoslynApiVersion )`; **L59–L60 map both `V5_0_0` and `V5_10_0` to `CSharp14`**
  - L77–L87 `ToNuGetVersionString`; L85 `RoslynApiVersion.V5_10_0 => "5.10.0-1.26365.3"` — this string alone
    decides whether the `roslyn-consolidated` prerelease feed is written into the generated `nuget.config`
    (L117–L132, `ToPrereleasePackageSourceUrl` / `GetPrereleasePackageSourceUrl`)
  - L134–L144 `ToVersion`
  - L149–L159 `GetMaxLanguageVersion( Version roslynVersion )`; **L152 `(>= 5, _) => AllLanguageVersions.CSharp14`**
- `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18` — the numeric
  `LanguageVersion` shims `CSharp10 = 1000` … `CSharp14 = 1400`. A `CSharp15 = 1500` constant would be added here.
- `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60` — maps the .NET
  SDK major version to a language version; **L56 `>= 10 => LanguageVersion.CSharp14`**, so the .NET 11 SDK yields
  C# 14.
- `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:41-89` — the
  hand-written half of the generated verifier. Note that `VisitVersionSpecificNode` (L41) does **not** call
  `base.Visit…`, so the children of a version-specific node are never walked.

---

## 2. Files and types sensitive to runtime, SDK, Roslyn or host version

### 2.1 The two documents that are the authority

- `Metalama.Framework/docs/platform-support.md` — the PB-2027.0 baseline.
  - L114–L116 the canonical short form:
    `PB-2027.0 = VS 2026 LTSC · VS Code C# Dev Kit / Rider current · .NET 10 SDK · User=net10.0 · .NET Framework
    4.7.2 · Roslyn 5.0–5.x · Core=net10.0 / Desktop=net472`
  - L199–L206 ".NET runtime, for user target frameworks": "The supported user target frameworks are `net10.0` …
    and `net11.0`".
  - L241–L246 the variant-selection table keyed on the measured Rider / C# Dev Kit Roslyn version.
  - L268–L281 the shipped-asset table.
  - L294–L300 names the two files that carry the whole Visual Studio axis:
    `Metalama.Framework.CompilerExtensions.Resources.csproj` and `Metalama.Framework.CompilerExtensions.csproj`.
  - L302–L313 the two drift traps: a target-framework-shaped path segment that belongs to a NuGet package, and the
    string literals in `TargetedAssemblyReference` and `ExtensionLoaderBase`.
  - L344–L364 the verification checklist that must run before general availability.
- `Metalama.Framework/docs/compile-time-target-frameworks.md` — the compile-time compilation always targets
  `netstandard2.0` (L28–L35) and is explicitly out of scope of the baseline (L8–L11). Point 3 (L37–L48) is why one
  compile-time assembly exists per run-time target framework: `ComputeSourceHash` folds the target framework into
  the identity.
- `Directory.Packages.md` — the package-version policy.
  - L61–L69 "TFM constraints", `MicrosoftBuildVersion` derivation.
  - L71–L79 "The Out-of-band family" (`System.Memory`, `System.Buffers`, `System.Numerics.Vectors`,
    `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`) and the `devenv.exe.config`
    binding-redirect ceilings.
  - L161–L172 the Roslyn variant coverage table.
  - L211–L221 the preprocessor symbols the variants define.
  - L381–L391 "Forward-looking constraints to track".
- `Metalama.Framework/docs/updating-roslyn.md` — the 12-step procedure. **This is the pattern the C# 15 work will
  follow.** Steps 3 and 4 (L11–L12) settle the experimental question; steps 7–10 (L15–L34) settle the variant set.

### 2.2 Version pins

`Directory.Packages.props`:

| Line | Property / package | Rationale recorded in the file |
| --- | --- | --- |
| L7 | `RestorePrerelease=true` | "Required while .NET SDK 10 is pre-release" |
| L12 | `PostSharpEngineeringVersion` = `2023.2.412` | must match `global.json` `msbuild-sdks` |
| L16 | `NewtonsoftJsonMinVersion` = `13.0.3` | "must match the version used by the lowest version of Visual Studio supported by the VSX, i.e. VS 17.14" — **stale under PB-2027.0, which excludes VS 2022** |
| L23 | `RoslynApiMinVersion` = `5.0.0` | names the dropped 4.12 variant and the platform baseline |
| L28 | `RoslynApiMaxVersion` = `5.10.0-1.26365.3` | "Roslyn 5.10 is a preview … restored from the roslyn-consolidated feed declared in nuget.base.config" |
| L30 | `RoslynMaxVersion` = `5.10.0-1.26365.3` | version referenced by the Workspaces package |
| L33 | `xUnitApiVersion` = `2.9.3` | |
| L50 | `MicrosoftBuildVersion` = `18.0.2` | derived from the .NET 10 SDK's MSBuild 18.0; long comment L35–L49 explains the `ExcludeAssets="runtime"` requirement |
| L53 | `MicrosoftVisualStudioThreadingLatestVersion` = `17.14.15` | |
| L55 | `MessagePackLatestVersion` = `2.5.198` | pinned to 2.x for StreamJsonRpc compatibility |
| L59–L60 | `SystemTextJsonLatestVersion` / `MicrosoftBclAsyncInterfacesLatestVersion` = `10.0.11` | "Microsoft.CodeAnalysis 5.10 requires the .NET 10 line" |
| L63 | `SystemTextJsonVersion` fallback = `8.0.6` | overridden per Roslyn variant |
| L66 | `SystemTextJsonMinVersion` = `8.0.6` | comment still says "while we support .NET 8 SDK as a build target" — **stale** |
| L70 | `SystemIOPipelinesLatestVersion` = `10.0.11` | |
| L114 | `System.Runtime.CompilerServices.Unsafe` = `6.1.2` | binding-redirect cap AsmVer 6.0.3.0 measured in `devenv.exe.config` of VS 2022 17.14 and VS 2026 18.9 |
| L121 | `System.Memory` = `4.6.3` | binding-redirect cap AsmVer 4.0.5.0, same measurement |
| L132 | `System.Threading.Tasks.Extensions` = `4.5.4` | held for separately-deployed Metalama.Vsx 2026.0.x |
| L168 | `Microsoft.NET.Test.Sdk` = `17.14.1` | "Must be the lowest supported VS, i.e. 17.14" — **stale** |
| L179 | `StreamJsonRpc` = `2.20.17` | flowed-dependency AsmVer freeze for Metalama.Vsx |
| L217–L219 | `Microsoft.CodeAnalysis.Common` / `.CSharp` at `$(RoslynApiMinVersion)`, `.Workspaces.MSBuild` at `$(RoslynApiMaxVersion)` | |
| L229 | `Microsoft.Azure.Functions.Worker.Sdk` = `2.1.0` | "Version 1.18.1 maps a target framework to a tooling suffix up to net8.0 only … 2.1.0 is the first that maps net10.0" — a third-party SDK that will need the same treatment for `net11.0` |

`global.json`:

- L4 `"version": "10.0.102"`, L5 `"rollForward": "patch"`, L6 `"allowPrerelease": true`
- L9 `"PostSharp.Engineering.Sdk": "2023.2.412"`
- L2 says the file is generated by PostSharp.Engineering and must not be edited; the source is
  `eng/src/Program.cs:52`.

`nuget.base.config`:

- L8 the `roslyn-consolidated` source, required only while `RoslynApiMaxVersion` is a prerelease.
- L14–L18 the package-source mapping. L15 records that `Microsoft.CodeAnalysis` must be listed by exact name
  because the wildcard does not match the metapackage.

`Directory.Build.props`:

- L9 `NoWarn` for `IDE0032` and `IDE0031`, "Disabling new features while the SDK is not stable".
- L16 `<MetalamaTemplateLanguageVersion>14.0</MetalamaTemplateLanguageVersion>`, with the comment (L11–L15) that
  the ceiling is `RoslynApiMinVersion`, "which is 5.0.0, which supports C# 14. Raise this value only together with
  that one".

`Metalama.Framework/Directory.Build.props`:

- L31 `Condition="'$(Configuration)'=='Release' OR '$(TargetFramework)'!='net10.0'"` — a literal target framework
  in a code-quality switch.
- L45 `<LangMaxVersion>14.0</LangMaxVersion>`; L46 `<LangVersion>$(LangMaxVersion)</LangVersion>`.
  `LangMaxVersion` is exported to dependent repositories (`eng/src/Program.cs:142`).

`Metalama.Extensions/Directory.Build.props:23`, `Metalama.Patterns/Directory.Build.props:26`,
`Metalama.Migration/Directory.Build.props:18` all read `$(LangMaxVersion)`.

### 2.3 Roslyn variant declarations

`eng/RoslynVersions/`:

- `Latest.props:2` `<Import Project="Roslyn.5.10.0.props" Condition="'$(ThisRoslynVersion)'==''" />`;
  L5 defaults `ThisRoslynVersionNoPreview` to `ThisRoslynVersion`.
- `Roslyn.5.0.0.props`: L3 `ThisRoslynVersion=5.0.0`, L5 `ThisRoslynVersionNoPreview=5.0.0`,
  L7 `ThisRoslynVersionProjectSuffix=.5.0.0`, L12 `SystemTextJsonVersion=9.0.0`. Defines **no** constant (L8–L10).
- `Roslyn.5.10.0.props`: L3 `ThisRoslynVersion=$(RoslynApiMaxVersion)`, L5 `ThisRoslynVersionNoPreview=5.10.0`,
  L7 empty `ThisRoslynVersionProjectSuffix`, L10 `DefineConstants` adds `ROSLYN_5_10_0_OR_GREATER`,
  L12 `SystemTextJsonVersion=10.0.11`.

The variant shim projects, each a two-line `<Import>` pair:

- `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj`
- `Metalama.Framework/src/Metalama.Framework.DesignTime.5.0.0/…csproj`
- `Metalama.Framework/src/Metalama.Framework.Implementation.Package.5.0.0/…csproj` (L5–L6: imports
  `Roslyn.5.0.0.props` then the base package project)
- `Metalama.Framework/src/Metalama.Testing.AspectTesting.5.0.0/…csproj`
- `Metalama.Framework/src/Metalama.Testing.UnitTesting.5.0.0/…csproj`
- and six `Metalama.Framework/src/tests/*.5.0.0/…csproj`

Empty directories left over from the dropped 4.12 variant (only `bin`/`obj` remain, no project file):
`Metalama.Framework/src/Metalama.Framework.DesignTime.4.12.0`, `…Engine.4.12.0`,
`…Implementation.Package.4.12.0`, `Metalama.Testing.AspectTesting.4.12.0`, `Metalama.Testing.UnitTesting.4.12.0`,
and five under `src/tests`. Also `Metalama.Framework/.generated/4.12.0/` is a stale generated tree.

The Roslyn version is threaded into project identity in:

- `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj`
  L5 import of `Latest.props`; L12 `AssemblyName=Metalama.Framework.Engine.$(ThisRoslynVersionNoPreview)`;
  L37–L38 `.generated/$(ThisRoslynVersionNoPreview)` with a `-stubs` fallback; L43–L49 seven `InternalsVisibleTo`
  entries suffixed by the variant; L53–L55 `VersionOverride="$(ThisRoslynVersion)"` on the three Roslyn packages.
- `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj`
  L3, L9, L16–L17, L23, L34–L35 — the same pattern.
- `Metalama.Framework/src/Metalama.Framework.Implementation.Package/Metalama.Framework.Implementation.Package.csproj`
  L3 import, L12 `PackageId=Metalama.Framework.Implementation.$(ThisRoslynVersionNoPreview)`, L33–L34 the
  variant-suffixed project references, L46–L48 the `VersionOverride`s, L84/L87/L108/L111 the packaged assembly
  names.
- `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj`
  L9 import, L21 `AssemblyName`, L59–L63 variant-suffixed references, L66–L70 the `METALAMA_HTML_WRITER` constant
  defined only for the latest variant.
- `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:53-54`
  `<ThisRoslynVersionNoPreview Condition="'$(ThisRoslynVersionNoPreview)'==''">5.0.0</ThisRoslynVersionNoPreview>`
  with the comment "When this is referenced as a NuGet package, the latest version of Roslyn is used." **Stale**:
  the latest variant is `5.10.0`. It happens to be inert, because only `ThisRoslynVersionProjectSuffix` (L77) is
  read afterwards, but it is a trap for the next reader.

The run-time counterpart of the variant table, which must be edited in lockstep with `eng/RoslynVersions`:

- `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs`
  - L22 `MinimumSupportedRoslynVersion = new Version( 5, 0 )` — mirrors `RoslynApiMinVersion`
  - L32–L34 `if ( roslynVersion >= new Version( 5, 10 ) ) variantName = "5.10.0";`
  - L38–L42 `else if ( roslynVersion >= MinimumSupportedRoslynVersion ) variantName = "5.0.0";`
  - L44–L53 below the floor: `variantName = ""`, returns `false`, and the caller behaves "as if Metalama were not
    installed"
- `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs`
  - L35 `_isNetFramework` (the Desktop/Core switch)
  - L54, L77 `HostRoslynVersion` from `GetHostRoslynVersion()` (L633)
  - L79 the `RoslynVariantPolicy.TryGetVariantName` call
  - L83, L468 the `"desktop"` / `"core"` and `Desktop.` / `Core.` resource prefixes
  - L195–L205 the "unsupported Roslyn" report written to a file

### 2.4 Target framework declarations

**Shipping projects.** The complete set in this repository (test, benchmark and standalone projects omitted):

| Target frameworks | Projects |
| --- | --- |
| `netstandard2.0` only | `Metalama.Backstage.Tools`, `Metalama.Extensions.DependencyInjection`, `…DependencyInjection.ServiceLocator`, `Metalama.Extensions.Metrics`, `Metalama.Framework.Analyzers`, `Metalama.Framework.CompileTime`, `Metalama.Framework.CompileTimeContracts`, `Metalama.Framework.CompilerExtensions` (L4), `Metalama.Framework.DesignTime.Rpc`, `Metalama.Framework.EditorExtensions`, `Metalama.Framework.Engine.Analyzers`, `Metalama.Framework.Package` (L4), `Metalama.Framework.Sdk`, `Metalama.SourceTransformer`, `Metalama.Migration`, `Metalama.Migration.Transformer`, `Metalama.Licensing` |
| `netstandard2.0;net10.0` | `Metalama.Testing.Hooks` (L4), `Metalama.Extensions.Multicast` (L4), `Metalama.Framework` (L4) |
| `netframework4.7.2;net10.0;netstandard2.0` | `Metalama.Backstage` (L4) |
| `net472;net10.0` | `Metalama.Extensions.DiffEngine` (L4), `Metalama.Extensions.HtmlWriter` (L4), `Metalama.Framework.ConfigurationFiles` (L4), `Metalama.Framework.DesignTime.Contracts` (L4), `Metalama.Framework.DesignTime` (L6), `Metalama.Framework.Engine` (L8), `Metalama.Framework.Implementation.Package` (L6), `Metalama.Framework.Introspection` (L4), `Metalama.Testing.AspectTesting` (L7), `Metalama.Testing.UnitTesting` (L6) |
| `net10.0;net472` | **`Metalama.Framework.CompilerExtensions.Resources` (L6)** — the two embedded flavours |
| `net472;net10.0;netstandard2.0` | `Flashtrace`, `Flashtrace.Formatters`, `Metalama.Patterns.Caching`, `…Caching.Aspects`, `…Caching.Backend`, `Metalama.Patterns.Contracts`, `Metalama.Patterns.Immutability`, `Metalama.Patterns.Memoization`, `Metalama.Patterns.Observability` (all L4) |
| `net472;net10.0-windows` | `Metalama.Patterns.Wpf` (L4) |
| `net10.0` only | `Metalama.Backstage.Commands` (L4), `Metalama.Backstage.DotNetTool` (L5), `Metalama.Backstage.Worker` (L21), `Metalama.Framework.Workspaces` (L18), `Metalama.Tool` (L5) |
| `net10.0-windows*` | `Metalama.Backstage.Desktop.Windows` (L5, `net10.0-windows10.0.17763.0`), `PostSharp.LicenseKeyGenerator` (L5), `PostSharp.LicenseKeyReader` (L5), `Metalama.LinqPad` (L6) |
| **`net9.0`** | **`eng/src/BuildMetalama.csproj:6`** — the build orchestrator itself |

**The Desktop/Core embedding pair**, which `platform-support.md:294-300` calls the whole of the Visual Studio axis:

- `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj`
  - L5–L6 `<!-- We target the two frameworks for which csc.exe is built. --> <TargetFrameworks>net10.0;net472</TargetFrameworks>`
  - L25–L26 the two `ProjectReference`s that enumerate the Roslyn variants:
    `Metalama.Framework.DesignTime.5.0.0` and `Metalama.Framework.DesignTime` (the latest). **This is the list
    that must gain an entry whenever a variant is added.**
- `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/Metalama.Framework.CompilerExtensions.csproj`
  - L38–L72 `SelectAssembliesToEmbed`, which repeats `net472` and `net10.0` as **path segments**, nine times:
    L53, L54, L56, L58, L59, L60, L62, L63, L64, L70. Every one is a glob against a build-output directory.
  - L88–L89 `WorkingDirectory="$(MSBuildThisFileDirectory)../../../eng/src/bin/Debug/net9.0"` — the signing step,
    hard-coded to `BuildMetalama.csproj`'s target framework. `BuildMetalama.csproj:5` carries the reciprocal
    comment: "When changing the target framework, update the path to Build.exe in
    Metalama.Framework.CompilerExtensions.csproj."
  - L67–L70 records why `System.Threading.AccessControl` is embedded for Desktop only: it is in the .NET 10 shared
    framework, so NuGet prunes the package asset.

**The extension-loader string literals** named by `platform-support.md:308-313`:

- `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-20`
  `RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", … ) ? "net472" : "net10.0";`
- `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31` — the same
  expression, computed locally and then **not used** (the filtering at L36–L38 goes through
  `a.SatisfiesCurrentProcess`, which uses the field in `TargetedAssemblyReference`); the local is only logged.
- `Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:39` — `"net10.0"` in a development
  path.
- `Metalama.Framework/src/tests/Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:46-47` — `"net10.0"` twice.

**The compile-time reference project**:

- `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43`
  `private const string _defaultCompileTimeTargetFrameworks = "netstandard2.0;net8.0;net48";` — **still names
  `net8.0`**, which PB-2027.0 removed from the supported set. Consumed at L209–L212 and written into the generated
  `TempProject.csproj` at L749. The generated project is at L735–L775; L751 sets `<LangVersion>latest</LangVersion>`
  and L756 references `Microsoft.CodeAnalysis.CSharp` at `RoslynApiVersion.Current.ToNuGetVersionString()`.
  L219–L221 is the only validation: the set must contain `netstandard2.0`.

**Design-time host simulator**:

- `eng/src/DesignTimeSolution.cs:42` `private const string _simulatorTargetFramework = "net10.0";`, used at L83,
  with an explicit, well-worded failure at L104–L107 when the simulator assembly is not found.

**Workspaces package asset selection**:

- `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:91-97` —
  `_WorkspacesMSBuildAssetTargetFramework` defaults to `$(TargetFramework)` and falls back to `net9.0` when
  `Microsoft.CodeAnalysis.Workspaces.MSBuild` has no folder of that name. Guarded by an `<Error>` at L117–L118.

### 2.5 The platform requirement matrix (what the user sees)

`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props`:

- L11–L12 `MetalamaCheckSupportedPlatform` (default `True`).
- L24–L44 the single `MetalamaPlatformRequirement` item, with the maintainer note at L21–L22 that these values are
  the PB-2027.0 baseline:
  - L26 `TargetFrameworkIdentifiers` = `.NETFramework;.NETCoreApp;.NETStandard`
  - L27 `MinimumNETFrameworkVersion` = `4.7.2`
  - L28–L29 `MinimumNETStandardVersion` `2.0`, `MaximumNETStandardVersion` `2.1`
  - L30–L31 `MinimumNETCoreAppVersion` `10.0`, **`MaximumNETCoreAppVersion` `11.0`**
  - L32–L33 `MinimumSdkVersion` `10.0`, **`MaximumSdkVersion` `11.0`**
  - L37 `MinimumVisualStudioVersion` `18.0`
  - L38–L41 the four human-readable sentences, which already say ".NET 10 and .NET 11" and "supported .NET SDK
    versions are 10.0 and 11.0"
- L66–L78 `MetalamaSourceGeneratorAttribute`, a hand-maintained list of attribute-based source generators; the
  comment at L67 says ".Net 9" and L77 says "ASP.NET Core 9 … does not ship any attribute-based source
  generators". A .NET 11 wave has to re-derive this list.

`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets`:

- L286–L302 `_MetalamaComputePlatformRequirements`, which applies `MetalamaSupportedPlatformExclusion`.
- L315–L370 `MetalamaCheckSupportedTargetFramework`, emitting `LAMA0600`. L309–L311 records why the comparison
  uses `TargetFrameworkIdentifier` + `TargetFrameworkVersion` rather than a string compare on `TargetFramework`.
  L322 strips the `v` prefix.
- L378–L380 `_MetalamaFirstTargetFramework`, so the toolchain warnings are reported once per cross-targeting build.
- L392–L421 `MetalamaCheckSupportedToolchain`, emitting `LAMA0601` (.NET SDK, both bounds) and `LAMA0602`
  (MSBuild/Visual Studio, lower bound only). L397 strips a prerelease suffix from `NETCoreSdkVersion`, with
  `'11.0.100-preview.1.26073.1'` as the worked example.

### 2.6 Continuous integration and container definitions

`eng/src/Program.cs`:

- L17 `using MetalamaDependencies = …MetalamaDependencies.V2027_0;`
- L21 `BuildTimeout = 60 minutes`
- L26–L31 the .NET SDK components: `V_10_0` ("Must match global.json"), and `V_9_0` with the comment "Required by
  this project, which targets net9.0. No product project targets net9.0 any more."
- L34–L46 `VisualStudioBuildToolsComponent( VisualStudioBuildToolsComponentVersion.v17_14_15, … )`, including the
  4.7.2 and 4.8 targeting packs
- L52 `DotNetSdkVersion = new DotNetSdkVersion( PreferredVersions.DotNetSdk.V_10_0 ) { AllowPrerelease = true }`
- L54 `MSBuildVersion = new Version( 17, 14 )`
- L55–L100 the solution list, including `SolutionFilterPathForInspectCode =
  "Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf"` (L60) and the note at L64–L65 that the whole solution
  is tested "so that the test projects built against the older supported Roslyn versions are tested as well"
- L140–L143 `ExportedProperties`: `RoslynApiMaxVersion`, `RoslynMaxVersion` from `Directory.Packages.props`, and
  `LangMaxVersion` from `Metalama.Framework/Directory.Build.props`. **This is the contract with the dependent
  repositories** (Metalama.Premium, Metalama.Vsx, Metalama.Documentation, Metalama.Samples).
- L224, L228–L251 `OnPrepareCompleted`, which downloads test licence keys and then calls
  `GenerateMetaSyntaxRewriter.Generate`.

`eng/docker/vs17.Dockerfile`:

- L5 `ARG WINDOWS_VERSION=ltsc2025`
- L33–L36 installs Visual Studio **17.14.15** Build Tools with a pinned channel manifest and catalogue URI, and
  the same six components as `Program.cs`. Marked "auto-generated by PostSharp.Engineering" (L3).

`eng/docker/build.Dockerfile` (also auto-generated):

- L44 installs .NET SDK **8.0.417**, L48 **9.0.310**, L52 **10.0.102**. The .NET 8 line is no longer named by
  `Program.cs`, so this file is out of step with its own generator input.

`eng/docker-context/VisualStudio.17.14.15.Release.chman`, `VisualStudio.17.Release.chman`,
`eng/docker-context/vs17/VisualStudio.17.14.15.Release.chman` — pinned Visual Studio channel manifests.

### 2.7 Generated and local-only files (not to be edited)

- `global.json` (header L2), `eng/Versions.g.props`, `eng/Versions.Debug.g.props`,
  `eng/Versions.Debug.wsl.g.props`, `eng/DockerMounts.g.ps1` — all git-ignored (`.gitignore:36` `*.g.props`).
  `eng/Versions.Debug.g.props:24` contains a machine-local
  `…\eng\src\bin\Debug\net9.0\BuildMetalama.dll` and L26 a VS 2022 `msbuild.exe` path.
- `Metalama.Framework/.generated/**` — git-ignored (`.gitignore:62`).

---

## 3. How the C# 14 wave was absorbed (the pattern to repeat)

Metalama tracked C# 14 as #1034, #1035, #1036, #1094, #1105, #1108–#1116, #1127, #1131, #1143, #1159, #1160. Those
issues produced almost no code in this subsystem: they are code-model, advising and template work. What the build
subsystem contributed was the *platform* half of the wave, done in three distinct commits, and it is that shape
which the C# 15 work will repeat.

### 3.1 The three-part shape

**Part A — take the Roslyn version.** Commit `6e2b07a313` "Adding Roslyn 5.0 and moving net6.0 to net8.0", the
commit that made C# 14 reachable at all. It touched, in one change: every `<TargetFramework(s)>` element in the
repository; `Directory.Packages.props`; `Metalama.Framework/Directory.Build.props`;
`build/RoslynVersion/Roslyn.5.0.0.props` (new) and `Latest.imports`; the two `*.4.12.0.csproj` shim projects (new);
`Metalama.Framework.CompilerExtensions.Resources.csproj`; `Metalama.Framework.CompilerExtensions.csproj`;
`SupportedCSharpVersions.cs`; `CompileTimeAssemblyLocator.cs`; `ExtensionLoaderHelper.cs`;
`DevBackstageToolsLocator.cs`; and `Metalama.Framework.sln` (+370 lines of variant projects).

**Part B — take the grammar.** Commits `b46f9218a8` "Use the actual Roslyn 5.10 grammar for the syntax rewriter"
and `e1cbb88a77` "Skip experimental Roslyn nodes in the syntax rewriter generator" (both #1881). The first added
52 lines to `Syntax-5.10.0.xml` and removed three lines from `GenerateMetaSyntaxRewriter.cs`; the second added the
`ExperimentalUrl` attribute to the model and the `RemoveExperimentalDeclarations` pass to `TreeReader`. The
principle, written into `updating-roslyn.md:11-12`, is:

> "Study the new C# syntax features. **We IGNORE any experimental feature. They are not supported.** If the new
> Roslyn only has new experimental features, there is nothing to do in this repo."
> "Keep the experimental nodes that the file declares … the grammar file has to keep describing the Roslyn version
> it is named after."

So the grammar snapshot stays faithful to upstream and the *filter* decides what is generated. That separation is
the single most important thing to preserve for C# 15.

**Part C — re-derive the variant set.** Commits `e413ad96f9` (renumber the latest variant to 5.10, prune dead
symbols), `08d065a9f8` (replace the 4.12 variant with a 5.0 variant), `58e4141956` (reinstate one variant
constant), `d92bbbb664` and `d69c66e568` (record the decision in `Directory.Packages.md`,
`platform-support.md`, `extensibility.md`, `testing.md`, `updating-roslyn.md`). The rule that came out of it, now
in `platform-support.md:53-54` as rule 8: "An axis enters the matrix only if some shipped asset depends on it.
Before adding a target framework, a Roslyn variant or a version cap for a platform, name the asset whose selection
actually changes."

### 3.2 The preprocessor-symbol discipline

`Directory.Packages.md:211-221` records the outcome, and it is the rule the C# 15 wave inherits:

- No production source branches on a variant symbol. The only symbol either variant defines is
  `ROSLYN_5_10_0_OR_GREATER`, used by exactly two aspect tests (`UnknownAccessorInTemplate` and its `_Roslyn5_0`
  counterpart, which differ only in where Roslyn reports `CS1014`).
- `ROSLYN_5_0_0_OR_GREATER` was removed together with **177 conditional blocks**, **69 `@RequiredConstant` and
  `@ForbiddenConstant` test directives**, and the `RequiredConstants` entries of three `metalamaTests.json` files,
  once every variant sat on the same side of it.
- `updating-roslyn.md:36`: "Do not add a `DefineConstants` entry to a variant props file unless the source has to
  branch on a distinction that no existing constant expresses."
- Name a symbol after the Roslyn version at which the distinction appears, never after a variant number, so that
  renumbering a variant does not rewrite the `#if` sites.

### 3.3 The template-language-version half

C# 14 in *templates* was a separate, later change: #1896, commits `778edd5dd6` and `a5a1035bab` on
`topic/2027.0/1896-template-language-version-14`. Its build-subsystem footprint:

- `Directory.Build.props:16` `<MetalamaTemplateLanguageVersion>14.0</MetalamaTemplateLanguageVersion>`, with the
  comment tying it to `RoslynApiMinVersion`.
- A new standalone test, `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/`, whose csproj pins
  `<LangVersion>14.0</LangVersion>` (L10) precisely so that the scenario asserts the template language version
  alone (L7–L9).
- `Metalama.Framework/src/tests/Standalone/Issue1757/OldAspects/OldAspects.csproj:7,11` pins both `LangVersion` and
  `MetalamaTemplateLanguageVersion` to `12.0`, so that a raised repository default does not silently change what
  that scenario tests.

The C# 15 equivalent would be `MetalamaTemplateLanguageVersion` `15.0` plus a `TemplateLanguageVersion15`
standalone test, and it can only follow a `RoslynApiMinVersion` that supports C# 15.

### 3.4 What was *not* needed

No change to `Generator.cs`, to the model classes, to `VersionDetector`, or to `TreeFlattening`. The generator
absorbed C# 14's new nodes (`FieldExpressionSyntax`, `ExtensionBlockDeclarationSyntax` and the rest) purely from
the grammar diff. That is the strongest single signal for the C# 15 estimate.

---

## 4. Extension points, per kind of language addition

The question "what has to change for a new X" has, in this subsystem, a short answer for four of the five kinds and
a longer one for the fifth.

### 4.1 A new kind of type declaration (`union`)

Grammar shape: a `<Node>` whose `Base` is `TypeDeclarationSyntax`, with `SkipConvenienceFactories="true"` and
`Override="true"` on every inherited field. Exactly what `UnionDeclarationSyntax` is
(`Syntax-5.10.0.xml:1954-1979`).

Build-subsystem changes required: **none in the generator**, provided the node reaches it. Concretely:

1. `TreeReader.RemoveExperimentalDeclarations` (`Model/TreeReader.cs:72`) must stop deleting it — either because
   the refreshed grammar snapshot no longer carries `ExperimentalUrl`, or by a deliberate change to the filter.
2. Re-run `Build.ps1 prepare` (`eng/src/Program.cs:250`).
3. `Generator.cs` then emits, per variant: `VisitUnionDeclaration` + `TransformUnionDeclaration`
   (`GenerateMetaSyntaxRewriter`, L403–L524), a `UnionDeclaration(…)` factory in `MetaSyntaxFactoryImpl`
   (L535–L610), `VisitUnionDeclaration` in both hashers (L637–L708), a `PartialUpdate` overload (L761–L800), and
   a `VisitUnionDeclaration` in `RoslynVersionSyntaxVerifier.g.cs` guarded by `RoslynApiVersion.V5_10_0`
   (L118–L124, because `MinimalRoslynVersion.Index > 0`).

The change that is *not* automatic: `SupportedCSharpVersions.ToLanguageVersion` (L52–L62) must map the Roslyn
version at which the node appears to `CSharp15`, otherwise the verifier's guard is a no-op (see §5.1).

### 4.2 A new modifier (`closed`)

Grammar shape: none. Modifiers are a `SyntaxList<SyntaxToken>` and no file under `eng/` enumerates the allowed
tokens. Verified: `grep` for modifier names across `eng/` returns only `Generator.IsKeyword` (L343–L383), which is
an identifier-escaping list, not a modifier list.

Build-subsystem changes required: **none**. The impact is entirely in the code model and the syntax generation of
`Metalama.Framework.Engine`, which is another subsystem's terrain.

The one build-side consequence: `Generator.IsTrivialToken` (L725–L735) classifies every token that is not one of
four literal/identifier kinds as "trivial", meaning the hashers record only its `RawKind`. A new modifier token is
correctly treated as trivial, and `RawKind` distinguishes it. No change.

### 4.3 A new expression form (`unsafe(expr)`)

Grammar shape: a `<Node>` with `Base="ExpressionSyntax"` (`Syntax-5.10.0.xml:496-508`).

Build-subsystem changes required: **none in the generator**; same three steps as §4.1. Note that the field name
`Keyword` camel-cases to `keyword`, which is not in `IsKeyword`, so no escaping is involved; had the field been
named `Unsafe`, `Generator.IsKeyword:381` already covers it.

### 4.4 A new collection-expression element (`with(...)`)

Grammar shape: a `<Node>` with `Base="CollectionElementSyntax"` (`Syntax-5.10.0.xml:816-822`).

`CollectionElementSyntax` is itself an `AbstractNode` introduced in Roslyn 4.8 alongside `CollectionExpressionSyntax`;
`RoslynVersionSyntaxVerifier.g.cs` already carries `VisitCollectionExpression` and `VisitExpressionElement` guarded
by `RoslynApiVersion.V4_8_0` (generated file, lines around 30–40). A new derived element follows the identical
path. Build-subsystem changes: **none in the generator**; same three steps.

### 4.5 A new optional field on an existing statement (labeled `break` / `continue`)

This is the only one of the five that exercises the multi-version code path, and it is the one with the sharpest
failure mode.

Grammar shape: `<Field Name="Name" Type="IdentifierNameSyntax" Optional="true" />` inserted between
`BreakKeyword` and `SemicolonToken` (`Syntax-5.10.0.xml:1296` and `:1307`).

What the generator does once the field is visible:

- `VersionDetector.DetectVersions` (`Model/VersionDetector.cs:50-75`) gives the field
  `MinimalRoslynVersion = 5.10.0` while the node's other fields keep `4.0.1`.
- `Generator.GenerateMetaSyntaxRewriter` (L432–L479) then sees `roslynApiMinimalVersions.Count > 1` and emits a
  `switch ( this.TargetApiVersion )` with one arm per relevant version, each calling the factory with the field
  list of *that* version; `default:` throws `AssertionFailedException` (L476–L477).
- `Generator.GenerateVersionChecker` (L127–L156) emits
  `this.VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 );` inside `VisitBreakStatement`.
- `Generator.GenerateHasher` (L645–L705) adds `this.Visit( node.Name );`.
- `Generator.GeneratePartialUpdate` (L768–L799) adds an `Option<IdentifierNameSyntax?> name = default` parameter.

Nothing in the generator needs to change. What needs attention is the *current* generated output while the field
is filtered out — see §5.2.

### 4.6 Summary table

| Addition | Grammar element | Generator change | Other build change |
| --- | --- | --- | --- |
| New type declaration | `<Node Base="TypeDeclarationSyntax">` | none | remove the experimental filter for it; re-run `prepare` |
| New modifier | none | none | none |
| New expression form | `<Node Base="ExpressionSyntax">` | none | as above |
| New collection element | `<Node Base="CollectionElementSyntax">` | none | as above |
| New optional field | `<Field Optional="true">` on an existing node | none | as above; the `TargetApiVersion` switch appears automatically |

In every case the *decision* points are the same three: the grammar snapshot
(`eng/src/GenerateMetaSyntaxRewriter/Syntax-<version>.xml`), the version list
(`GenerateMetaSyntaxRewriter.cs:17-18`), and the experimental filter (`Model/TreeReader.cs:70-109`).

---

## 5. Where the subsystem would silently do the wrong thing

Ordered by how quietly the failure happens.

### 5.1 The implicit-`LangVersion` clamp downgrades a `net11.0` project to C# 12

`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118-121`:

```xml
<PropertyGroup Condition="'$(LangVersionImplicitlySet)'=='True' AND '$(LangVersion)'!='12.0' AND '$(LangVersion)'!='13.0' AND '$(LangVersion)'!='14.0' AND '$(LangVersion)'!='default' AND '$(LangVersion)'!='latest' AND '$(LangVersion)'!='latestMajor' AND '$(LangVersion)'!='preview'">
    <_LangVersionBeforeMetalamaFix>$(LangVersion)</_LangVersionBeforeMetalamaFix>
    <LangVersion>12.0</LangVersion>
</PropertyGroup>
```

The whitelist is an inclusion test against three literal versions. The .NET 11 SDK will implicitly set
`LangVersion` to `15.0` for a `net11.0` project. `'15.0'` is not in the list, so the condition is true and the
project is compiled as **C# 12**, ten language versions below what the user asked for and two below what Metalama
already supports. A `MetalamaCheckLangVersion` warning is raised (L243–L247), but its text says the version was
raised "to … the lowest version supported by Metalama Framework", which reads as a *floor* message and describes a
*ceiling* action. A user reading it will not conclude that C# 14 features have just stopped compiling.

The same shape would misfire for C# 15 on `net10.0` when `LangVersion` is set explicitly to `15.0` — that path is
not implicit, so it escapes, but any future implicit `15.0` does not.

### 5.2 The experimental filter silently drops a field from an existing node

`Model/TreeReader.cs:92` removes `BreakStatementSyntax.Name` and `ContinueStatementSyntax.Name`. Verified against
the generated output for the 5.0.0 variant:

`Metalama.Framework/.generated/5.0.0/Metalama.Framework.DesignTime/RunTimeCodeHasher.g.cs:865-870`

```csharp
public override void VisitBreakStatement( BreakStatementSyntax node )
{
    this.Visit( node.AttributeLists );
    this.VisitTrivialToken( node.BreakKeyword );
    this.VisitTrivialToken( node.SemicolonToken );
}
```

`Metalama.Framework/.generated/5.0.0/Metalama.Framework.Engine/MetaSyntaxRewriter.g.cs:3170-3183` — the
three-argument `SyntaxFactory.BreakStatement( attributeLists, breakKeyword, semicolonToken )` call.

Two consequences, both silent:

1. **Design-time incremental staleness.** The generated override does not call `base.VisitBreakStatement`, so the
   `Name` child is never walked. `break loop1;` and `break loop2;` produce the *same* hash. Once C# 15 ships and a
   user edits the label, the design-time pipeline sees an unchanged file and serves a stale result. There is no
   diagnostic, and the failure is exactly the shape described at `platform-support.md:22-28` — Visual Studio logs
   nothing the user sees.
2. **Silent label loss in a template.** `TransformBreakStatement` reconstructs the statement through the
   three-argument factory overload, which Roslyn keeps for binary compatibility. A labeled `break` inside a
   template body compiles, transforms, and comes out unlabeled.

The same argument applies to a *node* that is filtered out, with a different mechanism: `MetaSyntaxRewriter` has no
generated `VisitUnionDeclaration`, so `CSharpSyntaxRewriter`'s default rewrites children instead of transforming
the node into syntax-factory calls, and `RoslynVersionSyntaxVerifier` has no override, so the "unsupported language
version" check never fires.

This is not a defect today: no supported `LanguageVersion` can produce these nodes. It becomes one the moment
`SupportedCSharpVersions.Latest` moves past C# 14 without the grammar filter being revisited in the same change.
The two edits are in different repositories' worth of distance from one another — `Utilities/SupportedCSharpVersions.cs`
and `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs` — and nothing links them mechanically.

### 5.3 `RoslynApiVersion.V5_10_0` maps to C# 14, so the template version guard passes silently

`SupportedCSharpVersions.ToLanguageVersion` (`…/Utilities/SupportedCSharpVersions.cs:52-62`) maps both `V5_0_0`
and `V5_10_0` to `CSharp14`. `RoslynVersionSyntaxVerifier.VisitVersionSpecificNode`
(`…/Templating/RoslynVersionSyntaxVerifier.cs:41-52`) compares `version.ToLanguageVersion()` against
`MaximalAcceptableLanguageVersion`. Once a C# 15 node is generated with the guard `RoslynApiVersion.V5_10_0`, the
comparison is `CSharp14 > CSharp14`, which is false, so `OnForbiddenSyntaxUsed` never fires and
`TemplateUsesUnsupportedLanguageVersion` is never reported. A template using a C# 15 construct against a
C# 14 ceiling would be accepted and then fail, or worse succeed wrongly, further down.

Also note that `VisitVersionSpecificNode` does not call `base.Visit…`, so nothing inside a version-specific node is
verified either.

### 5.4 `GetLanguageVersionFromDotNetSdk` caps the .NET 11 SDK at C# 14

`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60`:

```csharp
var sdkSupportedVersion = version.Major switch
{
    >= 10 => LanguageVersion.CSharp14,
    ...
```

The `>=` makes this silent rather than an exception: the .NET 11 SDK returns C# 14, and L64–L71 then takes the
minimum of that and the project's own version. A project on `net11.0` with `LangVersion=15.0` would have its
*compile-time* (template) language version silently reduced to 14. The `>= 8` / `>= 9` arms show the intended
pattern; a `>= 11 => CSharp15` arm is the mechanical fix.

`GetMaxLanguageVersion` (`SupportedCSharpVersions.cs:149-159`) has the same shape at L152, `(>= 5, _) =>
CSharp14`, for the `msbuild.exe` path.

### 5.5 `_defaultCompileTimeTargetFrameworks` still names `net8.0`

`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43`:

```csharp
private const string _defaultCompileTimeTargetFrameworks = "netstandard2.0;net8.0;net48";
```

This is written verbatim into the generated `TempProject.csproj` (L749) that is restored and built on the user's
machine to produce `assemblies-netstandard2.0.txt`. `net8.0` is out of PB-2027.0. The only validation (L219–L221)
checks that `netstandard2.0` is present. On a machine that carries only the .NET 10 and .NET 11 targeting packs,
the `net8.0` inner build is what fails, inside a nested build whose output goes to a binary log the user never
looks at. The `net8.0` entry serves no asset that anything reads, since only the `netstandard2.0` list is
consumed (L664).

### 5.6 Path segments that name a target framework and are not ours

`platform-support.md:302-307` records this trap and it survives in
`Metalama.Framework.CompilerExtensions.csproj:53-70`: ten `Include` globs whose paths contain `net472` or
`net10.0`. Each is an MSBuild item glob. A glob that matches nothing produces **no error and no item**; the
assembly simply does not get embedded, `ResourceExtractor` finds no resource at run time, and the design-time
payload fails to load with the silent-in-Visual-Studio failure mode. There is no `<Error>` guard on any of them.

Contrast `Metalama.Framework.Workspaces.csproj:117-118`, which does guard the equivalent situation, and
`eng/src/DesignTimeSolution.cs:104-107`, which also does. The embedding project is the one place that does not.

### 5.7 The `MetalamaPlatformRequirement` matrix already claims `net11.0` that nothing ships

`build/Metalama.Framework.props:31` `MaximumNETCoreAppVersion` `11.0` and L38's sentence "The supported target
frameworks are .NET Framework 4.7.2 and later, .NET Standard 2.0 and 2.1, .NET 10 and .NET 11." A `net11.0`
project therefore gets **no** `LAMA0600`, while:

- no package in this repository ships a `net11.0` asset (§2.4), so a `net11.0` project resolves the `net10.0`
  asset — which is correct and intended for the framework itself, but not verified anywhere;
- `Metalama.Patterns.Wpf` ships `net472;net10.0-windows` only, so a `net11.0-windows` WPF application resolves
  `net10.0-windows`, again unverified;
- the SDK-version rule (`MaximumSdkVersion` 11.0) also passes, so the .NET 11 SDK produces no warning while
  `LanguageVersionProvider` silently caps the language at C# 14 (§5.4).

The matrix is written as policy, ahead of the assets. That is deliberate, but it means the platform check cannot be
relied on as a tripwire for the C# 15 / .NET 11 work: it is already green.

### 5.8 Stale rationales that will mislead the next change

None of these is a defect on its own; each is a comment that no longer describes the code, in a place where the
next person will read it as authority.

- `Directory.Packages.props:15` "We must match the version used by the lowest version of Visual Studio supported
  by the VSX, i.e. VS 17.14" — VS 2022 is out of PB-2027.0.
- `Directory.Packages.props:65` "…that need to remain on the .NET 8 line while we support .NET 8 SDK as a build
  target" — the .NET 8 SDK is out.
- `Directory.Packages.props:168` "Must be the lowest supported VS, i.e. 17.14."
- `Directory.Packages.props:84-85` on `K4os.Hash.xxHash`, same VSX wording.
- `Metalama.Testing.AspectTesting.targets:53-54` defaults `ThisRoslynVersionNoPreview` to `5.0.0` and calls it
  "the latest version of Roslyn"; the latest variant is `5.10.0`.
- `eng/docker/build.Dockerfile:44` installs .NET SDK 8.0.417, which `eng/src/Program.cs:26-31` no longer requests.
- `eng/docker/vs17.Dockerfile:33-36` and `eng/src/Program.cs:34-46` install Visual Studio 17.14.15 Build Tools, and
  `Program.cs:54` sets `MSBuildVersion = new Version( 17, 14 )`, while `build/Metalama.Framework.props:37` declares
  `MinimumVisualStudioVersion` `18.0`. Continuous integration therefore tests on an MSBuild that the product warns
  about with `LAMA0602`.
- `Metalama.Framework/.generated/4.12.0/` and the ten empty `*.4.12.0` project directories are leftovers of the
  dropped variant.

### 5.9 Metalama.Premium is a whole wave behind

`C:\src\Metalama-2027.0\Metalama.Premium`, on `topic/2027.0/1829-durable-and-immutable-contracts`, still carries:

- `eng/RoslynVersions/Roslyn.4.12.0.props` and `Roslyn.5.0.0.props`, with `Latest.props:2` pointing at **5.0.0**;
- `ROSLYN_4_12_0`, `ROSLYN_4_4_0_OR_GREATER`, `ROSLYN_4_12_0_OR_EARLIER`, `ROSLYN_5_0_0`,
  `ROSLYN_5_0_0_OR_GREATER` — every one of which the main repository has removed;
- `Directory.Packages.props:8-9` `RoslynVersion` / `RoslynMaxVersion` = `5.0.0`, and package references to
  `Metalama.Framework.Implementation.4.12.0` and `.5.0.0` (L37–L38), neither of which the main repository will
  publish for 2027.0 — the published identities are `.5.0.0` and `.5.10.0`;
- `net8.0` as the Core target framework in every project, including
  `Metalama.Extensions.Validation.Package.Resources.csproj:6` and `Metalama.Extensions.CodeFixes.Package.Resources.csproj:6`,
  which are Premium's counterparts of `Metalama.Framework.CompilerExtensions.Resources`;
- `eng/src/BuildMetalamaPremium.csproj:5` on `net9.0`.

Premium consumes the exported properties from `eng/src/Program.cs:140-143` (`RoslynApiMaxVersion`,
`RoslynMaxVersion`, `LangMaxVersion`), so a raise in this repository reaches it, but the variant *identities* and
the target frameworks do not travel and have to be edited by hand. Because the payload resources project there
targets `net8.0`, a Premium extension built today cannot be loaded by a PB-2027.0 design-time host at all — and by
`ExtensionLoaderBase`/`TargetedAssemblyReference` (§2.4) that is a *string equality* test on the target framework
name, so it fails by producing an empty extension list rather than by reporting anything.

---

## 6. What exactly must change to add `net11.0` as a supported user target framework

The platform *policy* already admits `net11.0` (§2.5). What is missing is the assets and the derived values.

### 6.1 Already done

- `build/Metalama.Framework.props:31` `MaximumNETCoreAppVersion` `11.0`
- `build/Metalama.Framework.props:33` `MaximumSdkVersion` `11.0`
- `build/Metalama.Framework.props:38-39` the two user-facing sentences
- `platform-support.md:199-206` records the decision
- `Directory.Packages.md:189` "The .NET 10 and .NET 11 SDKs, and `Metalama.Compiler`" already appear in the variant
  table

### 6.2 Decide first, per rule 8 of the doctrine

`platform-support.md:53-54` requires naming the asset whose selection changes before adding an axis. For `net11.0`
the honest answer is likely **no shipped asset needs to change**: a `net11.0` project resolves the `net10.0` asset,
and the `net10.0` embedded Core flavour runs on the .NET 11 runtime by roll-forward. If that is the conclusion, the
work reduces to §6.4 and §6.5 and no `<TargetFrameworks>` element moves at all. Record that conclusion in
`platform-support.md`, in the "Shipped assets under PB-2027.0" table (L268–L281), so that the next reader does not
re-derive it.

### 6.3 If an asset genuinely needs `net11.0`

The complete list of places a target framework is declared, in the order they must move together:

1. `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj:6`
   — the embedded flavours. **Only if the Core flavour itself moves**, which requires a measurement that no host in
   the baseline runs a .NET runtime below 11. Per `platform-support.md:76-82` there is exactly one Core flavour and
   no fallback, so this is the highest-risk edit in the repository.
2. `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/Metalama.Framework.CompilerExtensions.csproj:53,54,56,58,59,60,62,63,64,70`
   — the ten glob path segments, which must move in the same commit (§5.6).
3. `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:20` and
   `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31` — the two `"net10.0"`
   literals, which are compared for string equality against `MetalamaExtensionAssembly` metadata.
4. The per-package `<TargetFrameworks>` elements listed in §2.4, for whichever packages genuinely need a `net11.0`
   asset. `Metalama.Patterns.Wpf:4` (`net472;net10.0-windows`) is the one with a real user-visible consequence,
   because a Windows Presentation Foundation application gets no compatible asset when the floor moves.
5. `Metalama.Framework/docs/extensibility.md` L19–L25, L72, L110, L120–L121, L131–L136, L150, L224–L236, L533–L536,
   L573, L589, L632–L635 — the extension-author instructions, which `platform-support.md:280-281` says are derived
   from the asset table and have no independent authority.
6. `eng/src/DesignTimeSolution.cs:42` if the host simulator moves.
7. `Metalama.Framework/Directory.Build.props:31` — the literal `net10.0` in the code-quality condition.

### 6.4 Version-derived values that must move regardless

- `eng/src/Program.cs:26-31` — add `PreferredVersions.DotNetSdk.V_11_0` to the container requirements; drop the
  `V_9_0` entry once `eng/src/BuildMetalama.csproj:6` moves off `net9.0`.
- `global.json:4` — regenerated by PostSharp.Engineering from `eng/src/Program.cs:52`, not edited.
- `eng/docker/build.Dockerfile` — regenerated; the .NET 8 line at L44 should disappear at the same time.
- `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` — replace
  `net8.0` with a supported framework (§5.5).
- `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:56` — add a
  `>= 11 => …` arm (§5.4).
- `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:97` — the `net9.0`
  fallback, which is guarded and will fail loudly, so it can wait.
- `Directory.Packages.props` — re-derive `MicrosoftBuildVersion` (L50) if the SDK floor moves, since it is defined
  as the MSBuild of the lowest host; and the `*LatestVersion` properties (L53–L73) against the .NET 11 line.

### 6.5 Verification, which the doctrine makes mandatory

`platform-support.md:344-364` lists three checks that are performed against machines rather than calendars. For a
`net11.0` addition the relevant ones are item 1 (the Visual Studio 2026 long-term servicing channel private runtime
and Roslyn version, after 2026-11-10) and item 3 (a design-time smoke test on the floor, reading the
`ServiceHub.RoslynCodeAnalysisService` log rather than the editor).

---

## 7. What exactly must change to move the Roslyn floor

The procedure is `Metalama.Framework/docs/updating-roslyn.md`, and it is complete and current. Restated against the
files, with the additions this subsystem's survey suggests.

### 7.1 Raising `RoslynApiMaxVersion` (taking a newer Roslyn)

1. `Directory.Packages.props:28` `RoslynApiMaxVersion` and `:30` `RoslynMaxVersion`.
2. `eng/RoslynVersions/Roslyn.5.10.0.props:5` `ThisRoslynVersionNoPreview` if the identity changes; L3 already
   follows `RoslynApiMaxVersion`.
3. `SupportedCSharpVersions.ToNuGetVersionString` (`…/Utilities/SupportedCSharpVersions.cs:85`) — the exact package
   version, prerelease label included. This one string also decides whether `nuget.base.config`'s
   `roslyn-consolidated` source is written into the user-side generated `nuget.config`
   (`SupportedCSharpVersions.cs:117-132`; `updating-roslyn.md:38-54`).
4. `nuget.base.config:8` — the feed itself, removed when leaving prerelease.
5. Add `eng/src/GenerateMetaSyntaxRewriter/Syntax-<new>.xml`, copied unchanged from
   `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` of the matching `Metalama.Compiler` branch, keeping the
   experimental nodes (`updating-roslyn.md:12`).
6. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:18` — add the version to `versionNames`, and
   move the superseded one into `legacyVersionNames` (L17) if it no longer needs generated code.
7. `Build.ps1 prepare`.

### 7.2 Raising `RoslynApiMinVersion` (dropping a variant)

`updating-roslyn.md:35` is the checklist; the files are:

1. `Directory.Packages.props:23` `RoslynApiMinVersion`.
2. Delete `eng/RoslynVersions/Roslyn.<old>.props`.
3. Delete the shim projects listed in §2.3 and remove them from `Metalama.Framework/Metalama.Framework.sln`.
4. `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj:25-26`
   — remove the variant's `ProjectReference`.
5. `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:22,32-53` — the floor and
   the branch table.
6. `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` — the three switches at
   L52–L62, L77–L87 and L134–L144, plus `GetMaxLanguageVersion` at L149–L159.
7. `Directory.Build.props:16` `MetalamaTemplateLanguageVersion` — the ceiling is `RoslynApiMinVersion`, so raising
   the floor is what unlocks a higher template language version.
8. Every preprocessor symbol that is now defined by all remaining variants or by none: remove it together with its
   `#if` sites, its `@RequiredConstant` / `@ForbiddenConstant` test directives, and the `RequiredConstants` /
   `ForbiddenConstants` entries of the `metalamaTests.json` files. The 2027.0 precedent removed 177 blocks and 69
   directives for one symbol.
9. Re-derive the tables in `Directory.Packages.md:161-172` and `:193-209`, and the Roslyn API section of
   `platform-support.md:216-266`.
10. Mirror steps 1–5 in `Metalama.Premium` (§5.9), which is currently a whole wave behind.

### 7.3 Enabling C# 15, which is the actual C# 15 wave

Beyond §7.1 and §7.2, the C# 15 work is:

1. Refresh `eng/src/GenerateMetaSyntaxRewriter/Syntax-<latest>.xml` from the Roslyn branch on which the four
   features are no longer `ExperimentalUrl`-annotated. This is a snapshot replacement, not an edit.
2. Confirm that `Model/TreeReader.cs:70-109` now retains all four, and re-run `prepare`.
3. `AllLanguageVersions.cs` — add `CSharp15 = 1500`.
4. `SupportedCSharpVersions.cs` — `Latest` (L31), `All` (L38–L43), `ToLanguageVersion` (L52–L62; the arm for the
   Roslyn version at which C# 15 becomes reachable must yield `CSharp15`, see §5.3), `GetMaxLanguageVersion`
   (L149–L159).
5. `LanguageVersionProvider.cs:54-60` — a `>= 11 => CSharp15` arm.
6. `Metalama.Framework/Directory.Build.props:45` `LangMaxVersion`, which is exported to dependent repositories.
7. `Directory.Build.props:16` `MetalamaTemplateLanguageVersion`, only after `RoslynApiMinVersion` supports it.
8. `build/Metalama.Framework.targets:118` — the literal whitelist (§5.1). This should be rewritten as a numeric
   comparison rather than extended by one more literal, because the current form fails closed in the wrong
   direction.
9. A `TemplateLanguageVersion15` standalone test mirroring
   `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/`, and a pinned `MetalamaTemplateLanguageVersion`
   in any scenario whose meaning depends on the repository default (the `Issue1757/OldAspects` precedent).
10. Aspect tests under `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp15/`,
    following the `CSharp14` directory layout introduced by commit `3626bba6d2`.

---

## 8. Quick index of the files that matter

| File | Why |
| --- | --- |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` | the C# 15 grammar delta, lines 496, 816, 1296, 1307, 1954 |
| `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs` | L70–L109, the experimental filter that hides it |
| `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs` | L17–L18, the version list |
| `eng/src/GenerateMetaSyntaxRewriter/Generator.cs` | L160–L173, L432–L479, L714–L735, the language-shape logic |
| `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs` | L50–L75, per-field version detection |
| `eng/RoslynVersions/{Latest,Roslyn.5.0.0,Roslyn.5.10.0}.props` | the variant declarations |
| `Directory.Packages.props` | L23, L28, L30, L50 — the four version pins that define the Roslyn and MSBuild axes |
| `Directory.Build.props` | L16, `MetalamaTemplateLanguageVersion` |
| `Metalama.Framework/Directory.Build.props` | L45, `LangMaxVersion` |
| `global.json` | L4, the .NET SDK pin (generated) |
| `nuget.base.config` | L8, the prerelease Roslyn feed |
| `.../Metalama.Framework.CompilerExtensions.Resources.csproj` | L6 the flavour pair, L25–L26 the variant list |
| `.../Metalama.Framework.CompilerExtensions.csproj` | L53–L70 the unguarded embedding globs, L88–L89 the `net9.0` signing path |
| `.../Metalama.Framework.Package/build/Metalama.Framework.props` | L24–L44, the platform requirement matrix |
| `.../Metalama.Framework.Package/build/Metalama.Framework.targets` | L118 the `LangVersion` clamp, L315–L421 the platform checks |
| `.../Metalama.Framework.Implementation.Package.csproj` | the per-variant payload package |
| `eng/src/Program.cs` | L26–L54 the toolchain requirements, L140–L143 the exported properties, L250 the generator call |
| `Metalama.Framework/docs/platform-support.md` | the baseline doctrine |
| `Metalama.Framework/docs/updating-roslyn.md` | the 12-step procedure that is the C# 15 template |
| `Directory.Packages.md` | L161–L221, the variant coverage and symbol discipline |
| `.../Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs` | L22, L32–L53, the run-time variant table |
| `.../Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` | the four version switches |
| `.../Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs` | L54–L60, the SDK-to-language map |
| `.../Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs` | L43 the stale `net8.0`, L735–L775 the generated reference project |
