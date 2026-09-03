# Terrain map: compile-time compilation, pipeline and options

Subsystem: `Metalama.Framework/src/Metalama.Framework.Engine/{CompileTime,Pipeline,Options}/**`, plus the four
files in `Utilities/` that the subsystem owns in practice (`SupportedCSharpVersions`, `AllLanguageVersions`,
`LanguageVersionProvider`, `ILanguageVersionProvider`) and the code generator in
`eng/src/GenerateMetaSyntaxRewriter/` that produces `RoslynApiVersion` and `RoslynVersionSyntaxVerifier`.

Repository root used below: `C:/src/Metalama-2027.0/Metalama`. All paths are relative to it unless absolute.
`Metalama.Premium` contains nothing in this subsystem: a search for `SupportedCSharpVersions`,
`ILanguageVersionProvider`, `RoslynApiVersion` and `CompileTimeProjectManifest` over
`C:/src/Metalama-2027.0/Metalama.Premium` matches one test file only
(`src/tests/Metalama.Extensions.Validation.UnitTests/DesignTime/ValidatorTests.cs`).

---

## 1. How the compile-time language version is chosen today

There are **four distinct language versions** in play, and they are chosen by four different mechanisms. Confusing
them is the main hazard in this subsystem.

| # | Version | Meaning | Where it is decided |
| --- | --- | --- | --- |
| 1 | `SupportedCSharpVersions.Latest` | Highest C# the current Metalama **build** admits | `Utilities/SupportedCSharpVersions.cs:31` |
| 2 | The project language version | `LangVersion` of the user project | `Options/MSBuildProjectOptions.cs:167` |
| 3 | The compile-time language version | Version used to **parse and compile the compile-time compilation** | `Utilities/LanguageVersionProvider.cs:29` |
| 4 | The template language version | Version the **template verifier** enforces on template bodies | `Templating/TemplateCompiler.cs:33`, seeded from #3, overridden by `MetalamaTemplateLanguageVersion` |

### 1.1 `SupportedCSharpVersions` (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs`)

```
31:    public static LanguageVersion Latest
32:        => LanguageVersion.CSharp14;
38:    public static ImmutableHashSet<LanguageVersion> All { get; } = ImmutableHashSet.Create(
39:        LanguageVersion.CSharp14,
40:        LanguageVersion.CSharp13,
41:        LanguageVersion.CSharp12,
42:        LanguageVersion.CSharp11,
43:        LanguageVersion.CSharp10 );
```

- `Latest` (line 31-32). Since #1881 removed the `#if ROSLYN_*` ladder, this is a plain literal. It is the value
  every fallback in the subsystem uses when no parse options are available.
- `All` (line 38-43). The accept-list. `CompileTimeAspectPipeline.VerifyLanguageVersion` and
  `TemplateCompiler.TryReadProjectOptions` both reject anything not in this set.
- `FormatSupportedVersions()` (line 45) formats it for diagnostics LAMA0051/LAMA0052.
- `DefaultParseOptions` (line 50) = `CSharpParseOptions.Default.WithLanguageVersion( Latest )`. Still used by
  `CompileTime/RunTimeAssemblyRewriter.cs:88` (the `@@Intrinsics.cs` tree), `CodeModel/LanguageOptions.cs:35` and
  `Utilities/UserCode/UserCodeInvoker.cs:422`.
- `ToLanguageVersion( RoslynApiVersion )` (lines 52-62) maps a Roslyn API variant to the highest C# it can parse.
  Note that both `V5_0_0` and `V5_10_0` map to `AllLanguageVersions.CSharp14` today (lines 59-60).
- `ToNuGetVersionString( RoslynApiVersion )` (lines 77-87). Feeds the `PackageReference` written into the
  reference-assembly project. `V5_10_0 => "5.10.0-1.26365.3"` (line 85) is the only prerelease entry.
- `GetMaxLanguageVersion( Version roslynVersion )` (lines 149-159). Maps an **assembly version read off disk** to a
  C# version. `(>= 5, _) => AllLanguageVersions.CSharp14` (line 152) is a coarse ceiling: every Roslyn 5.x,
  including 5.10 and any future 5.x that supports C# 15, is capped at C# 14.

### 1.2 `AllLanguageVersions` (`Utilities/AllLanguageVersions.cs:14-18`)

```
14:    public const LanguageVersion CSharp10 = (LanguageVersion) 1000;
...
18:    public const LanguageVersion CSharp14 = (LanguageVersion) 1400;
```

Exists so the engine can name a `LanguageVersion` that the *compiling* Roslyn does not declare. C# 15 needs a
`CSharp15 = (LanguageVersion) 1500` here first; every other change depends on it.

### 1.3 `LanguageVersionExtensions.ToDisplayStringSafe` (`Utilities/Roslyn/LanguageVersionExtensions.cs:16-40`)

An exhaustive switch over `LanguageVersion`, with `(LanguageVersion) 1300 => "13.0"` (line 33) and
`(LanguageVersion) 1400 => "14.0"` (line 34), ending in
`_ => throw new ArgumentOutOfRangeException(...)` (line 39). An unmapped version makes **diagnostic formatting
throw**, which is how LAMA0051/LAMA0052/LAMA0232/LAMA0282 report their arguments.

### 1.4 `LanguageVersionProvider` (`Utilities/LanguageVersionProvider.cs`) — the actual decision

`GetCompileTimeLanguageVersion()` (line 29, memoized in `_cachedValue`):

- Line 35: if `IProjectOptions.SdkVersion` (MSBuild `NETCoreSdkVersion`) is null or empty, go to
  `GetLanguageVersionFromMSBuild()`; otherwise `GetLanguageVersionFromDotNetSdk()`.
- `GetLanguageVersionFromDotNetSdk()` (lines 45-72). Parses the SDK version, then:

```
54:        var sdkSupportedVersion = version.Major switch
55:        {
56:            >= 10 => LanguageVersion.CSharp14,
57:            >= 9 => LanguageVersion.CSharp13,
58:            >= 8 => LanguageVersion.CSharp12,
59:            _ => throw new PlatformNotSupportedException( $"Unsupported .NET SDK version: {version}." )
60:        };
```

  then returns `min( sdkSupportedVersion, projectOptions.LanguageVersion )` (lines 62-71).
  **This is the single place where the .NET SDK major version is mapped to a C# version.** Under the .NET 11 SDK
  the `>= 10` arm matches and yields C# 14, silently. A `>= 11 => CSharp15` arm must be inserted **above** line 56.
- `GetLanguageVersionFromMSBuild()` (lines 74-123). Probes `<MSBuildBinPath>\Roslyn\Microsoft.CodeAnalysis.CSharp.dll`
  (line 88), then the parent directory (lines 93-99), reads its `AssemblyName.Version` (line 107) and calls
  `SupportedCSharpVersions.GetMaxLanguageVersion` (line 111), then takes the minimum with the project version
  (lines 113-122). This is the `msbuild.exe` / .NET Framework path added by #1247.

`ILanguageVersionProvider` is an `IProjectService`; the test double is
`Metalama.Testing.UnitTesting/TestLanguageVersionProvider.cs:12`, which returns `SupportedCSharpVersions.Latest`
unconditionally.

### 1.5 The project language version (`Options/MSBuildProjectOptions.cs:167-182`)

```
167:    public override LanguageVersion LanguageVersion
...
170:            var s = this.GetStringOption( MSBuildPropertyNames.LangVersion );
172:            if ( !LanguageVersionFacts.TryParse( s, out var version ) )
...
178:                return SupportedCSharpVersions.Latest;
180:            return version.MapSpecifiedToEffectiveVersion();
```

Silent fallback: a `LangVersion` string the **hosting Roslyn cannot parse** (for example `15.0` on a Roslyn that
predates C# 15) silently becomes `Latest`. `Options/DefaultProjectOptions.cs:127` also defaults to
`SupportedCSharpVersions.Latest`.

### 1.6 `MetalamaTemplateLanguageVersion` (`Templating/TemplateCompiler.cs:55-79`)

```
58:            var optionsTemplateLanguageVersion = this._options.TemplateLanguageVersion;
62:                if ( LanguageVersionFacts.TryParse( optionsTemplateLanguageVersion, out var templateLanguageVersion )
63:                     && SupportedCSharpVersions.All.Contains( templateLanguageVersion ) )
65:                    this.TemplateLanguageVersion = templateLanguageVersion;
...
68-73:                  reports GeneralDiagnosticDescriptors.CSharpVersionNotSupported (LAMA0052)
```

- Property declaration: `Templating/TemplateCompiler.cs:33`; seeded at line 51 from
  `ILanguageVersionProvider.GetCompileTimeLanguageVersion()`.
- MSBuild plumbing: `Options/MSBuildPropertyNames.cs:44` and `:93`;
  `Options/IProjectOptions.cs:215`; `Options/MSBuildProjectOptions.cs:153`;
  `Options/DefaultProjectOptions.cs:113`; `Options/ProjectOptionsWrapper.cs:99`;
  `Metalama.Framework.Package/build/Metalama.CompilerVisibleProperties.props:32`.
- This repository sets it in `Directory.Build.props:16` to `14.0`, with a comment that binds it to
  `RoslynApiMinVersion`.
- Consumption: `TemplateCompiler.TryAnnotate` line 106 constructs
  `new RoslynVersionSyntaxVerifier( diagnostics, this.TemplateLanguageVersion )`.
- The value flows out of the compile-time build as
  `CompileTimeCompilationBuilder.cs:360  compileTimeLanguageVersion = templateCompiler.TemplateLanguageVersion;`
  and is written into the manifest (`CompileTimeCompilationBuilder.cs:1282`).

### 1.7 `CompileTimeAspectPipeline.VerifyLanguageVersion`
(`Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`, called at line 177)

```
67:        var languageVersion = ((CSharpParseOptions?) compilation.SyntaxTrees.FirstOrDefault()?.Options)?.LanguageVersion.MapSpecifiedToEffectiveVersion()
68:                              ?? SupportedCSharpVersions.Latest;
70:        if ( languageVersion == LanguageVersion.Preview )
72:            if ( !this.ProjectOptions.AllowPreviewLanguageFeatures )
75:                    GeneralDiagnosticDescriptors.PreviewCSharpVersionNotSupported ...   // LAMA0051
82:        else if ( !SupportedCSharpVersions.All.Contains( languageVersion ) )
85:                    GeneralDiagnosticDescriptors.CSharpVersionNotSupported ...          // LAMA0052
```

Comment at lines 64-65: the check runs **only** in the compile-time pipeline, because "Roslyn does not properly
set the language version at design time". There is no equivalent gate in
`Pipeline/DesignTime/BaseDesignTimeAspectPipeline.cs`, `PreviewAspectPipeline.cs` or
`Pipeline/LiveTemplates/LiveTemplateAspectPipeline.cs`.

### 1.8 Complete list of edits required to raise the compile-time language version to C# 15

1. `Utilities/AllLanguageVersions.cs` — add `CSharp15 = (LanguageVersion) 1500`.
2. `Utilities/Roslyn/LanguageVersionExtensions.cs:34` — add `(LanguageVersion) 1500 => "15.0"` before the throw.
3. `Utilities/SupportedCSharpVersions.cs:31` — `Latest => LanguageVersion.CSharp15`.
4. `Utilities/SupportedCSharpVersions.cs:38-43` — add `CSharp15` to `All` (and decide whether `CSharp10` leaves).
5. `Utilities/SupportedCSharpVersions.cs:52-62` — `RoslynApiVersion.V5_10_0 => AllLanguageVersions.CSharp15`
   (and any new variant enum member).
6. `Utilities/SupportedCSharpVersions.cs:149-159` — split the `(>= 5, _)` arm so that `(5, >= 10)` (or whatever
   the real floor is) yields C# 15 and lower 5.x still yields C# 14.
7. `Utilities/LanguageVersionProvider.cs:54-60` — add `>= 11 => LanguageVersion.CSharp15` above the `>= 10` arm.
8. `Directory.Build.props:16` — `MetalamaTemplateLanguageVersion`, only together with `RoslynApiMinVersion`.
9. `CompileTime/CompileTimeCompilationBuilder.cs:425` — the `languageVersion >= LanguageVersion.CSharp14` guard on
   `EMBED_SYSTEM_TYPES`; decide whether it stays at 14 or becomes a version-independent condition.
10. `CompileTime/Manifest/CompileTimeProjectManifest.cs:101` — `ResolvedLanguageVersion` default (see §5.4).
11. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:17-18` — add the new grammar snapshot to
    `versionNames`, and move `5.0.0` into `legacyVersionNames` if the 5.0 variant is dropped.
12. `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:37,57` — the experimental-node filter must stop
    removing the C# 15 nodes once Roslyn drops `ExperimentalUrl` from them (see §5.1).
13. Standalone scenarios: `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/` (name, csproj
    `LangVersion`, README) and `Standalone/DefaultLanguageVersion/`.

---

## 2. How the prerelease Roslyn package source works

Whole mechanism lives in `Utilities/SupportedCSharpVersions.cs` and
`CompileTime/CompileTimeAssemblyLocator.cs`, and is documented in
`Metalama.Framework/docs/updating-roslyn.md:38-54`. Issue #1885.

- `SupportedCSharpVersions.ToNuGetVersionString` (lines 77-87) is the **single switch**. A version string
  containing a hyphen is a prerelease.
- `GetPrereleasePackageSourceUrl( string )` (lines 131-132):
  `nuGetVersionString.IndexOf( "-", StringComparison.Ordinal ) >= 0 ? RoslynPrereleaseSourceUrl : null`.
- `ToPrereleasePackageSourceUrl( this RoslynApiVersion )` (lines 117-118) derives it from the version string,
  deliberately, so that entering or leaving a prerelease is one edit.
- Constants: `RoslynPrereleaseSourceKey = "roslyn-consolidated"` (line 93),
  `RoslynPrereleaseSourceUrl = "https://proget.postsharp.net/nuget/roslyn-consolidated/v3/index.json"` (line 99),
  `RoslynPackagePattern = "Microsoft.CodeAnalysis.*"` (line 104).
- `CompileTimeAssemblyLocator.cs:234`:
  `this._prereleasePackageSourceUrl = RoslynApiVersion.Current.ToPrereleasePackageSourceUrl();`
  Lines 236-243 then resolve the user-level `nuget.config` (`NuGetHelper.GetUserConfigFile`) only when the source
  is non-null, because it decides whether a package-source mapping can be written.
- Cache-key contribution: lines 268 (`RoslynApiVersion.Current`) and 277-287 (the prerelease URL and the user
  configuration file content), so a cache directory built without the prerelease source is not reused.
- The generated project text is at `CompileTimeAssemblyLocator.cs:740-767`:
  `<TargetFrameworks>{targetFrameworks}</TargetFrameworks>`, `<LangVersion>latest</LangVersion>` (line 751),
  `<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="{RoslynApiVersion.Current.ToNuGetVersionString()}" />`
  (line 756). Note `LangVersion` is the literal string `latest`, resolved by the child SDK, not by us.
- The generated `nuget.config` is written at lines 777-830, through
  `NuGetHelper.AddPackageSource( document, key, url, pattern, userConfigFiles )`. When the user configuration
  already maps `Microsoft.CodeAnalysis.*` elsewhere, the mapping is **not** written and
  `_unmappedPrereleasePackageSourceUrl` (line 130 declaration, line 819 assignment) records that, so that
  `ReferenceAssemblyBuildFailureClassifier` can explain the eventual `NU1101`.
- Failure explanation: `CompileTime/ReferenceAssemblyBuildFailureClassifier.cs:83`
  (`_roslynPackagePrefix` derived from `RoslynPackagePattern`), lines 124-145 (`NU1101` rule) and lines 302-306
  (`ContainsUnresolvedRoslynPackage`).

Variant plumbing outside the subsystem, for reference: `eng/RoslynVersions/Latest.props`,
`eng/RoslynVersions/Roslyn.5.0.0.props`, `eng/RoslynVersions/Roslyn.5.10.0.props` (which defines
`ROSLYN_5_10_0_OR_GREATER`, used only by two aspect tests), and `Directory.Packages.props:20-28`
(`RoslynApiMinVersion` `5.0.0`, `RoslynApiMaxVersion` `5.10.0-1.26365.3`).

---

## 3. Files and types sensitive to the set of C# language constructs

### 3.1 The generated version gate (the pattern that a new construct must join)

- `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:16-18`

```
16:        var deprecatedVersionNames = Array.Empty<string>();
17:        string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0"];
18:        string[] versionNames = [.. legacyVersionNames, "5.0.0", "5.10.0"];
```

- `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:63-96` — `GenerateRoslynApiVersionEnum`, which writes
  `Metalama.Framework/.generated/<version>/Metalama.Framework.Engine/RoslynApiVersion.g.cs`.
- `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:98-170` — `GenerateVersionChecker`, which emits one
  `Visit<Node>` override per version-specific node (`IsVersionSpecificType`, line 156), one per version-specific
  **field** (`IsVersionSpecificField`, line 158), and a `switch` over `Kind()` for version-specific **kinds** of an
  existing field (`GetVersionSpecificKinds`, lines 160-170).
- `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:44` — `GenerateTemplateFiles`, which emits `MetaSyntaxRewriter.g.cs`,
  247 `Transform<Node>` methods in the current 5.0.0 output.
- `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:37` (`tree.Types.RemoveAll( t => t.IsExperimental )`)
  and `:57` (`children.RemoveAll( c => c is Field { IsExperimental: true } )`) — added by commit `e1cbb88a77`
  for #1881. **This is what currently erases all four C# 15 constructs.**
- `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:41-52`
  (`VisitVersionSpecificNode`), `:55-75` (`VisitVersionSpecificField`, with a `TODO` at lines 57-61 about
  generalizing fields), `:78-89` (`VisitVersionSpecificFieldKind`), `:32-38` (`OnForbiddenSyntaxUsed`, reporting
  LAMA0232), `:24` (`MaximalUsedVersion`).
- Generated instance: `Metalama.Framework/.generated/5.0.0/Metalama.Framework.Engine/RoslynVersionSyntaxVerifier.g.cs`,
  e.g. `VisitExtensionBlockDeclaration` → `RoslynApiVersion.V5_0_0`, `VisitFieldExpression` → `V5_0_0`,
  `VisitCollectionExpression`/`VisitExpressionElement`/`VisitSpreadElement` → `V4_8_0`.
- Grammar snapshots: `eng/src/GenerateMetaSyntaxRewriter/Syntax-{4.0.1,4.4.0,4.8.0,4.12.0,5.0.0,5.10.0}.xml`.
  In `Syntax-5.10.0.xml`: `UnsafeExpressionSyntax` at line 496, `WithElementSyntax` at line 816,
  `UnionDeclarationSyntax` at line 1954, all carrying `ExperimentalUrl`.

### 3.2 `ProduceCompileTimeCodeRewriter` — the largest construct-shaped switch in the subsystem
(`CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs`, class declared at line 43,
derives from `RemovePreprocessorDirectivesRewriter`)

Type-declaration dispatch (lines 204-252):

```
204:  public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node ).SingleOrDefault();
206:  public override SyntaxNode? VisitStructDeclaration( ... )
208:  public override SyntaxNode? VisitInterfaceDeclaration( ... )
210:  public override SyntaxNode? VisitRecordDeclaration( ... )
212:  public override SyntaxNode? VisitEnumDeclaration( EnumDeclarationSyntax node )
234:  public override SyntaxNode? VisitDelegateDeclaration( DelegateDeclarationSyntax node )
```

There is **no** `VisitExtensionBlockDeclaration`, although `ExtensionBlockDeclarationSyntax` derives from
`TypeDeclarationSyntax` (`Syntax-5.0.0.xml:2036`), and none for a future `UnionDeclarationSyntax`
(`Syntax-5.10.0.xml:1954`, also `Base="TypeDeclarationSyntax"`).

`PopulateNestedCompileTimeTypes` (lines 254-373), the un-nesting of compile-time types out of run-time types:

```
272:                    switch ( child.Kind() )
274:                        case SyntaxKind.ClassDeclaration when child is ClassDeclarationSyntax childType:
356:                        case SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration or SyntaxKind.RecordDeclaration
357:                            or SyntaxKind.RecordStructDeclaration or SyntaxKind.EnumDeclaration or SyntaxKind.DelegateDeclaration:
369:                        // ReSharper disable once RedundantEmptySwitchSection
370:                        default:
371:                            // Non-type members of a run-time type are always run-time too and should not be copied to the compile-time assembly.
372:                            break;
```

`TransformCompileTimeType` member dispatch (lines 508-563):

```
508:                        switch ( member.Kind() )
510:                            case SyntaxKind.MethodDeclaration when member is MethodDeclarationSyntax method:
515:                            case SyntaxKind.IndexerDeclaration:
516:                                throw new NotImplementedException( "Indexers are not implemented." );
520:                            case SyntaxKind.PropertyDeclaration ...
525:                            case SyntaxKind.EventDeclaration ...
530:                            case SyntaxKind.FieldDeclaration ...
535:                            case SyntaxKind.EventFieldDeclaration ...
540:                            case SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
541:                                or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration when member is TypeDeclarationSyntax nestedType:
546:                            case SyntaxKind.EnumDeclaration:
547:                            case SyntaxKind.DelegateDeclaration:
555:                            default:
556:                                members.Add( (MemberDeclarationSyntax) this.Visit( member ).AssertNotNull() );
```

Other construct-shaped members of the same file:

- `VisitAttributeList` line 147, gated on `SyntaxKind.CompilationUnit` (line 149).
- `VisitTypeDeclaration` (private) lines 417-447.
- Property/accessor handling lines 936-1170 (`GetAccessorDeclaration`, `SetAccessorDeclaration`,
  `InitAccessorDeclaration` at lines 936, 943-944, 1088-1105, 1135-1163) — where a **new accessor kind** or a
  **new modifier** would land.
- `symbol.Kind switch` for explicit implementations, line 783; `variableType = symbol.Kind switch`, line 1327.
- Event handling lines 1379-1380 (`AddAccessorDeclaration`, `RemoveAccessorDeclaration`).
- `member.Modifiers` filtering: line 1248 (`ReadOnlyKeyword`), line 328 (`InternalKeyword` replaces the whole
  modifier list of an un-nested type).
- `VisitConstructorDeclaration` line 1480; `VisitNamespaceDeclaration` line 1496;
  `VisitFileScopedNamespaceDeclaration` line 1511.
- `VisitTypeOrNamespaceMembers` lines 1452-1478, whose only recognized case is
  `case { SyntaxKind.IsTypeDeclaration: true } and TypeDeclarationSyntax type:` (line 1460).
- `VisitCompilationUnit` lines 1526-1535, filtering with
  `m.SyntaxKind.IsBaseTypeDeclaration || m.SyntaxKind.IsNamespaceDeclaration` (line 1530).
- `VisitUsingDirective` line 1562, with an `UnsafeKeyword` check at line 1564 and a `GlobalKeyword` check at 1569.
- `VisitInvocationExpression` line 1584; `VisitTypeOfExpression` line 1602; `VisitQualifiedName` line 1684;
  `VisitMemberAccessExpression` line 1701; `VisitIdentifierName` line 1718; `VisitInterpolation` line 1637;
  `VisitTrivia` lines 1778-1790.

### 3.3 `SyntaxKindExtensions` (`Utilities/Roslyn/SyntaxKindExtensions.cs`) — the shared kind sets

```
33:        public bool IsTypeDeclaration
34:            => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
35:                or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;
41:        public bool IsBaseTypeDeclaration => kind.IsTypeDeclaration || kind is SyntaxKind.EnumDeclaration or SyntaxKind.DelegateDeclaration;
```

Also `IsLambdaExpression` (47), `IsBaseFieldDeclaration` (53), `IsLiteralExpression` (58-63),
`IsAccessorDeclaration` (68-71), `IsBaseMethodDeclaration` (76-…). `IsTypeDeclaration` deliberately excludes
`ExtensionBlockDeclaration`, so a construct that is a `TypeDeclarationSyntax` in Roslyn's hierarchy is not
automatically covered by these predicates. A `UnionDeclaration` will need an explicit decision here, and the
decision propagates to `FindCompileTimeCodeVisitor` line 81 and `ProduceCompileTimeCodeRewriter` lines 1460
and 1530.

### 3.4 `FindCompileTimeCodeVisitor` (`CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs`)

```
58:            private void VisitTypeDeclaration( SyntaxNode node )
77:                if ( node is { SyntaxKind.IsTypeDeclaration: true } and TypeDeclarationSyntax typeWithMembers )
81:                        if ( childType.SyntaxKind.IsBaseTypeDeclaration )
89:            public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
91:            public override void VisitEnumDeclaration( ... )
93:            public override void VisitDelegateDeclaration( ... )
95:            public override void VisitStructDeclaration( ... )
97:            public override void VisitRecordDeclaration( ... )
99:            public override void VisitInterfaceDeclaration( ... )
```

This visitor decides `HasCompileTimeCode` for a whole syntax tree. See §5.2.

### 3.5 Other construct-shaped code in the subsystem

- `CompileTime/CompileTimeCompilationBuilder.CollectSerializableTypesVisitor.cs` — overrides at lines 64, 70, 76
  (class/struct/record) and a block of empty overrides at lines 82-102 used as a **pruning list**
  (`VisitMethodDeclaration`, `VisitFieldDeclaration`, `VisitPropertyDeclaration`, `VisitPropertyPatternClause`,
  `VisitAccessorDeclaration`, `VisitConstructorDeclaration`, `VisitIndexerDeclaration`, `VisitOperatorDeclaration`,
  `VisitConversionOperatorDeclaration`, `VisitEventDeclaration`, `VisitEventFieldDeclaration`). A new member form
  is **not** pruned and is walked.
- `CompileTime/CompileTimeCompilationBuilder.CollectSerializableFieldsVisitor.cs` — lines 47, 69, 101, 111, 121.
- `CompileTime/CompileTimeCompilationBuilder.EmbeddedAttributeDetectorVisitor.cs` — lines 29, 35, 49, 57.
- `CompileTime/CompileTimeCompilationBuilder.EmbeddedAttributeRemover.cs` — lines 57, 68.
- `CompileTime/CompileTimeCompilationBuilder.RemoveInvalidUsingsRewriter.cs:32`.
- `CompileTime/CompileTimeCompilationBuilder.ReplaceDynamicToObjectRewriter.cs:22-26`.
- `CompileTime/RewriterHelper.cs` — the "make this member abstract-bodied" switch: lines 50, 60, 70
  (`MethodDeclaration`), 159, 169 (`PropertyDeclaration`), 188 (`IndexerDeclaration`), 207 (`EventDeclaration`);
  modifier filtering on `ExternKeyword` at 140, 164, 183, 222 and `AbstractKeyword` at 102, 151.
- `CompileTime/RunTimeAssemblyRewriter.cs` — `VisitClassDeclaration` 140, `VisitFieldDeclaration` 208,
  `VisitEventFieldDeclaration` 213, `VisitMethodDeclaration` 308, `VisitPropertyDeclaration` 339,
  `VisitEventDeclaration` 398; `SyntaxKind.EventFieldDeclaration` case at 191; `ExternKeyword` at 255, 273;
  C# 14 `field` detection at 444 and 468-472 (`SyntaxHelpers.ContainsFieldExpression`);
  `SupportedCSharpVersions.DefaultParseOptions` at line 88.
- `CompileTime/CompileTimeCodeFastDetector.cs` — lines 47, 60, 70; a purely syntactic pre-filter over
  `using` directives.
- `CompileTime/SymbolClassifier.cs` — symbol-kind switches at lines 245, 256, 276, 347, 463, 514, 615, 623, 673,
  958, 1093, 1129, 1151, 1210. `TryGetWellKnownScope` at line 1206-…, with
  `case SymbolKind.ErrorType:` (1210 region) and `case SymbolKind.NamedType when …` .
- `CompileTime/CompileTimeTypeResolver.cs` — lines 70, 89, 110, 113, 116, 130 (`ArrayType`, `NamedType`,
  `DynamicType`, `PointerType`, `TypeParameter`). A new *type* form (for example a union type symbol) lands here.
- `CompileTime/FrameworkCompileTimeProjectFactory.cs` — lines 149, 158, 164 (`Method`, `Property`, `Event`).
- `CompileTime/AttributeDeserializer.cs` — lines 320, 389.
- `CompileTime/Manifest/TemplateProjectManifest.cs` — lines 69, 87-88, an explicit list of the symbol kinds whose
  manifest node is looked up by name.
- `CompileTime/TemplatingScope.cs:24-…` — the `TemplatingScope` enum; `TemplatingScopeExtensions.cs` its
  combination rules.

---

## 4. Files and types sensitive to runtime, SDK, Roslyn or host version

### 4.1 Roslyn version

- `Metalama.Framework/.generated/<variant>/Metalama.Framework.Engine/RoslynApiVersion.g.cs` — generated enum;
  the 5.0.0 output currently reads `V4_0_1 = 0 … V5_0_0 = 4, Current = V5_0_0, Lowest = V4_0_1, Highest = V5_0_0`.
  `.generated/` is git-ignored (`.gitignore:62`), so `4.12.0` on disk is a stale local artifact.
- `Utilities/SupportedCSharpVersions.cs:52-62, 77-87, 134-144, 149-159` — every mapping keyed on
  `RoslynApiVersion`.
- `CompileTime/CompileTimeAssemblyLocator.cs:234` (prerelease source), `:268` (cache key), `:756` (package
  reference in the reference project).
- `CompileTime/CompileTimeCompilationBuilder.cs:243-244` — `h.Append( RoslynApiVersion.Current )` in the project
  hash, so a variant change invalidates the compile-time assembly cache.
- `Options/TargetedAssemblyReference.cs:22-24` — `SatisfiesCurrentProcess` compares
  `TargetRoslynVersion` against `RoslynApiVersion.Current.ToVersion()`; consumed by
  `Extensibility/ExtensionLoaderBase.cs:36`.
- `CompileTime/Manifest/TemplateSymbolManifest.cs:31,43,49,59,61,73` and `CompileTime/ITemplateInfo.cs:23` —
  `RoslynApiVersion? UsedApiVersion`, serialized into the compile-time project manifest by System.Text.Json as a
  plain integer (no converter), and read back by
  `Templating/TemplateExpansionContext.cs:863` to produce LAMA0282.
- `Templating/RoslynVersionSyntaxVerifier.cs` and its generated partial.
- `Utilities/LanguageVersionProvider.cs:88-111` — reads the assembly version of
  `Microsoft.CodeAnalysis.CSharp.dll` from the MSBuild bin path.

### 4.2 .NET SDK version

- `Options/IProjectOptions.cs:263` (`SdkVersion`), `Options/MSBuildProjectOptions.cs:186-188`
  (`NETCoreSdkVersion`), `Options/MSBuildPropertyNames.cs:54`,
  `Metalama.Framework.Package/build/Metalama.CompilerVisibleProperties.props:40`.
- `Utilities/LanguageVersionProvider.cs:54-60` — the SDK-major-to-C#-version switch (the primary hotspot).
- `CompileTime/CompileTimeAssemblyLocator.cs:194` (field assignment), `:705`
  (`GlobalJsonHelper.WriteCurrentVersion( this._cacheDirectory, this._sdkVersion )`), `:838-848` (choose
  `msbuild.exe` over `dotnet` when `SdkVersion` is empty).
- `Utilities/GlobalJsonHelper.cs:22-36` — writes `"rollForward": "disable"` pinned to the host SDK version.
- `CompileTime/ReferenceAssemblyBuildFailureClassifier.cs:147-152` (`NETSDK1045`, "SDK too old for the target
  frameworks of the reference-assembly project") and `:184-208` (the `global.json` rule, which names
  `requestedSdkVersion`).

### 4.3 .NET runtime and target frameworks

- `CompileTime/CompileTimeAssemblyLocator.cs:43`
  `private const string _defaultCompileTimeTargetFrameworks = "netstandard2.0;net8.0;net48";`
  Still names `net8.0`, which PB-2027.0 dropped. Only `assemblies-netstandard2.0.txt` is ever read
  (line 664), so the other two entries exist to force the SDK to resolve those reference packs.
- `CompileTime/CompileTimeAssemblyLocator.cs:219-224` — `netstandard2.0` is mandatory; LAMA0084 otherwise.
- `CompileTime/CompileTimeAssemblyLocator.cs:389-430` — `ParseTargetFrameworks` and
  `ReportInvalidTargetFrameworks`; `Options/MSBuildPropertyNames.cs:42` and
  `Options/IProjectOptions.cs:203-205` (`MetalamaCompileTimeTargetFrameworks`, documented as
  `netstandard2.0;net8.0;net48`).
- `Options/DefaultProjectOptions.cs:56` — `TargetFramework => "net8.0"`.
- `Options/IProjectOptions.cs:75-88` — the doc comments still say `net8.0` and `.NETCoreApp,Version=v6.0`.
- `Options/TargetedAssemblyReference.cs:19-20` —
  `RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework" ) ? "net472" : "net10.0"`.
  This literal is one of the two named in `docs/platform-support.md` "What this means in this repository".
- `CompileTime/UnloadableCompileTimeDomain.cs:5` — `#if NET5_0_OR_GREATER`, the only target-framework `#if` in
  the subsystem; `AssemblyLoadContext` at lines 38, 51.
- `CompileTime/CompileTimeDomain.cs:31,135` — assembly-loading behaviour.
- `CompileTime/OutputPathHelper.cs:27-96` — the run-time `FrameworkName` becomes a path segment of the
  compile-time output directory; `docs/compile-time-target-frameworks.md` is the doctrine.
- `CompileTime/CompileTimeCompilationBuilder.cs:123-167` (`ComputeSourceHash`) — hashes
  `targetFramework.FullName` and the preprocessor symbols.
- `CompileTime/CompileTimeCompilationBuilder.cs:420-427` — `NETSTANDARD_2_0` and `EMBED_SYSTEM_TYPES`
  preprocessor symbols for the predefined trees; `CompileTime/ICompileTimePreprocessorSymbolProvider.cs`.

### 4.4 Host integrated development environment

- `Pipeline/CompileTime/CompileTimeAspectPipeline.cs:64-65` — the comment that the language-version check exists
  only in the compile-time pipeline because the design-time host does not set it reliably.
- `Options/MSBuildProjectOptions.cs:174-178` — the comment "the IDE runs a lower Roslyn version than the one
  required by the project", explaining the silent fallback to `Latest`.
- `CompileTime/DesignTimeCompatibility.cs:33` — `MinimumSupportedVersion = new( 2026, 1 )`; used by
  `CompileTimeProjectRepository.Builder.cs:548-554` to report LAMA0078.
- `Options/IProjectOptions.cs:270` and `MSBuildProjectOptions.cs:191` — `MSBuildBinPath`, the `msbuild.exe`
  (Visual Studio, no .NET SDK) path.
- `Extensibility/ExtensionLoaderBase.cs:36` with `Options/TargetedAssemblyReference.cs` — the string comparison of
  target framework names described in `docs/platform-support.md`.

### 4.5 Metalama version

- `CompileTime/Manifest/CompileTimeProjectManifest.cs:59-62, 88, 90, 92` — `MetalamaVersion`,
  `ManifestVersion`, `CurrentManifestVersion = 1`.
- `CompileTime/CompileTimeProjectRepository.Builder.cs:62` (`_currentMetalamaVersion`), `:526-561`
  (`ReportMixedVersionWarnings`, LAMA0081/LAMA0078), `:581-590` (`ManifestVersion` mismatch → LAMA0061).
- `Pipeline/MetalamaProjectClassifier.cs:16-36`.
- `CompileTime/CompileTimeCompilationBuilder.cs:177-181` — `_buildId` (module MVID) in the project hash.

---

## 5. What would have to change for each new kind of construct

### 5.1 A new kind of type declaration (`union`)

`UnionDeclarationSyntax` derives from `TypeDeclarationSyntax`. Ordered list of extension points:

1. `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:37` — while the grammar marks the node
   `ExperimentalUrl`, it is deleted and **nothing downstream sees it**. Nothing to do while it stays experimental;
   everything below applies the moment it stops being experimental.
2. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:18` — the grammar snapshot that first
   declares it must be in `versionNames`.
3. Generated `RoslynVersionSyntaxVerifier.g.cs` gains `VisitUnionDeclaration`; no manual edit, but the
   `RoslynApiVersion → LanguageVersion` map (`SupportedCSharpVersions.cs:52-62`) must be correct or the gate
   reports the wrong version in LAMA0232.
4. `Utilities/Roslyn/SyntaxKindExtensions.cs:33-41` — decide whether `IsTypeDeclaration` and
   `IsBaseTypeDeclaration` include `SyntaxKind.UnionDeclaration`. Both `ProduceCompileTimeCodeRewriter:1460`
   and `:1530` and `FindCompileTimeCodeVisitor:77,81` follow from this one decision.
5. `CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs` — add
   `VisitUnionDeclaration` alongside lines 89-99, otherwise a file whose only compile-time type is a union is
   classified as containing no compile-time code.
6. `CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs`
   - add `public override SyntaxNode? VisitUnionDeclaration(...) => this.VisitTypeDeclaration( node ).SingleOrDefault();`
     beside lines 204-210;
   - add `SyntaxKind.UnionDeclaration` to the nested-type case at lines 540-541;
   - add it to the "run-time type may not contain this" case at lines 356-357, or to the un-nesting case at
     line 274, depending on whether a compile-time union nested in a run-time type must be un-nested;
   - review `TransformCompileTimeType` (line 449) for the union's own member forms.
7. `CompileTime/CompileTimeCompilationBuilder.CollectSerializableTypesVisitor.cs:64-76` and
   `CollectSerializableFieldsVisitor.cs:101-121` — a serializable compile-time union needs its own override.
8. `CompileTime/SymbolClassifier.cs` — `TypeKind`-shaped reasoning (lines 1129, 1210 region) if Roslyn introduces
   a new `TypeKind`.
9. `CompileTime/CompileTimeTypeResolver.cs:70-130` — if a union produces a new `SymbolKind` or a new type-symbol
   shape.

### 5.2 A new modifier (`closed`)

No new syntax node; it appears in `SyntaxTokenList Modifiers`. The subsystem never enumerates the whole modifier
list against an accept-list, but it does **rebuild** modifier lists, which drops modifiers it does not name:

- `ProduceCompileTimeCodeRewriter.cs:328` —
  `.WithModifiers( TokenList( Token( SyntaxKind.InternalKeyword )… ) )`, which replaces the **entire** modifier
  list of an un-nested compile-time type. Any `closed` modifier on such a type is discarded.
- `ProduceCompileTimeCodeRewriter.cs:1248` — `node.Modifiers.Where( m => !m.IsKind( SyntaxKind.ReadOnlyKeyword ) )`.
- `RewriterHelper.cs:140, 164, 183, 222` and `RunTimeAssemblyRewriter.cs:273` —
  `Modifiers.Where( m => !m.IsKind( SyntaxKind.ExternKeyword ) )`.
- `RewriterHelper.cs:102, 151` and `RunTimeAssemblyRewriter.cs:255` — `AbstractKeyword` / `ExternKeyword` probes.
- `ProduceCompileTimeCodeRewriter.cs:642` — `TokenList( Token( SyntaxKind.PublicKeyword )… )` when synthesizing
  an interface implementation.

The generator's version gate does **not** cover a modifier: `Generator.GenerateVersionChecker` only emits checks
for version-specific nodes, fields, and field kinds. A new contextual modifier is therefore invisible to
`RoslynVersionSyntaxVerifier` and must be added by hand if templates are to be gated on it.

### 5.3 A new expression form (`unsafe(expr)`), a new collection-expression element (`with(...)`), a new optional field (`break label;`)

- Expression form: generated `Transform<Node>` in `MetaSyntaxRewriter.g.cs` plus generated
  `Visit<Node>` in `RoslynVersionSyntaxVerifier.g.cs`. Both come from the grammar automatically.
  In this subsystem, `ProduceCompileTimeCodeRewriter` handles expressions generically through
  `VisitCore` (line 1628) and the base rewriter, so no per-node edit is expected; but see §6.3.
- Collection-expression element: `WithElementSyntax` derives from `CollectionElementSyntax`. The 5.0 grammar
  already gates `CollectionExpression`, `ExpressionElement` and `SpreadElement` at `V4_8_0`
  (`.generated/5.0.0/…/RoslynVersionSyntaxVerifier.g.cs`). A new element type joins them automatically once it
  loses `ExperimentalUrl`.
- Optional field on an existing statement: this is the `IsVersionSpecificField` path,
  `Generator.cs:158` and `RoslynVersionSyntaxVerifier.cs:55-75`. The generator would emit
  `VisitBreakStatement`/`VisitContinueStatement` calling
  `VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 )`. The `TODO` at
  `RoslynVersionSyntaxVerifier.cs:57-61` is directly on point: a field added in a new Roslyn that returns a
  concrete value for old code produces a false positive. `BreakStatementSyntax.Name` is optional and is
  `SyntaxKind.None` for an unlabeled `break`, so the guard `!nodeOrToken.IsKind( SyntaxKind.None )` at line 63
  handles it correctly. This is the case the mechanism was designed for.

---

## 6. Places that would silently do the wrong thing

Ordered by how hard the failure is to notice.

### 6.1 Experimental nodes are erased from the version gate

`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:37,57` removes every node and field carrying
`ExperimentalUrl`. All four C# 15 constructs carry it in `Syntax-5.10.0.xml`. Consequence today: a template that
uses `union`, `unsafe(expr)`, `with(...)` in a collection expression, or a labeled `break`/`continue` is
**not reported** by `RoslynVersionSyntaxVerifier`, and `MaximalUsedVersion` is not raised, so
`TemplateSymbolManifest.UsedApiVersion` under-reports and LAMA0282 (`AspectUsesHigherCSharpVersion`) is never
produced for a consumer on an older language version. The transformation then either produces wrong code (§6.3)
or throws deep in the template compiler. `docs/updating-roslyn.md:11` records the policy ("We IGNORE any
experimental feature"), but the policy has no enforcement point: nothing rejects experimental syntax, it is
merely unmodelled.

### 6.2 A top-level compile-time `union` would not be found at all

`FindCompileTimeCodeVisitor` (lines 89-99) enumerates six declaration forms. It has no `DefaultVisit` fallback
that classifies an unknown type declaration. A syntax tree whose only compile-time type is a construct with no
override sets `HasCompileTimeCode = false`, and
`CompileTimeCompilationBuilder` then excludes the whole file from the compile-time compilation. The user sees no
diagnostic; the aspect simply does not exist. The same shape applies today to a hypothetical aspect declared
inside an `extension` block, since there is no `VisitExtensionBlockDeclaration` either.

### 6.3 `MetaSyntaxRewriter` silently passes through an unknown expression

`Templating/MetaSyntaxRewriter.cs:106-138`. When a node has no generated `Transform<Node>`,
`this.Visit( node )` returns the node unchanged and the result is cast at line 136
(`return (ExpressionSyntax) this.Visit( node )!;`). For an unknown **expression**, the cast succeeds and the raw
syntax is emitted into the compiled template instead of the `SyntaxFactory` call that should build it, so a
run-time expression is evaluated at compile time or emitted verbatim. For an unknown **member declaration** the
`default:` arm at line 132 throws `AssertionFailedException`, which is loud. The dangerous half is the expression
half, which is exactly the shape of `UnsafeExpressionSyntax` and `WithElementSyntax`.

### 6.4 The compile-time language version is not part of any cache key

`CompileTimeCompilationBuilder.ComputeProjectHash` (lines 169-247) appends `_buildId`, the assembly identity, the
referenced compile-time identities, `sourceHash`, `FormatCompileTimeCode`, `AllowPreviewLanguageFeatures`,
`RequireOrderedAspects`, `RoslynIsCompileTimeOnly`, `CompileTimeTargetFrameworks`, `TemplateLanguageVersion`
(line 239, the raw MSBuild string) and `RoslynApiVersion.Current` (line 243).
`ComputeSourceHash` (lines 123-167) appends the target framework and the preprocessor symbols.
Neither appends `IProjectOptions.SdkVersion` nor the value returned by
`ILanguageVersionProvider.GetCompileTimeLanguageVersion()`. Upgrading the .NET SDK from 10 to 11 changes the
compile-time language version once `LanguageVersionProvider.cs:54-60` gains an arm, but the project hash is
unchanged, so the previously emitted compile-time assembly and its manifest are served from the disk cache with
the old language version. The failure is a stale compile-time assembly, not an error.

### 6.5 `manifest.LanguageVersion ?? SupportedCSharpVersions.Latest` versus `ResolvedLanguageVersion`

`CompileTimeProjectManifest.cs:99-101` defines

```
 99:        // Prior versions of Metalama did not write LanguageVersion, but the maximum version was 13.
100:        [JsonIgnore]
101:        public LanguageVersion ResolvedLanguageVersion => this.LanguageVersion ?? Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp13;
```

`ResolvedLanguageVersion` has **no callers anywhere in the repository**. The two places that actually resolve the
absent value use a different default:

- `CompileTimeProjectRepository.Builder.cs:596` — `new CSharpParseOptions( manifest.LanguageVersion ?? SupportedCSharpVersions.Latest )`
- `CompileTimeCompilationBuilder.cs:1355` — `manifest.LanguageVersion ?? SupportedCSharpVersions.Latest`

So a reference compiled by a Metalama that predates the manifest field is re-parsed as C# 14 today and would be
re-parsed as C# 15 after the bump, rather than as C# 13. Parsing older source with a newer language version is
usually harmless, but the two defaults disagree and one of them is dead code, so the intent is not recoverable
from the code.

### 6.6 `GetMaxLanguageVersion` caps every Roslyn 5.x at C# 14

`SupportedCSharpVersions.cs:150-152` — `(>= 5, _) => AllLanguageVersions.CSharp14`. On the `msbuild.exe` path
(`LanguageVersionProvider.GetLanguageVersionFromMSBuild`), a Visual Studio carrying a Roslyn that supports C# 15
is silently limited to C# 14, and the project's `LangVersion` is silently lowered at line 115-122 without any
diagnostic. There is no equivalent of LAMA0052 on this path.

### 6.7 `MSBuildProjectOptions.LanguageVersion` swallows an unparseable `LangVersion`

`Options/MSBuildProjectOptions.cs:172-178`. A `LangVersion` of `15.0` on a host Roslyn that does not know C# 15
returns `SupportedCSharpVersions.Latest`. The comment says this is deliberate, but the effect is that
`VerifyLanguageVersion` then sees a supported version and reports nothing, while the compile-time compilation is
built for a lower language version than the run-time compilation. The user gets template compilation errors whose
cause is not named.

### 6.8 `PopulateNestedCompileTimeTypes` default arm

`ProduceCompileTimeCodeRewriter.cs:369-372`. Any member kind of a run-time type that is not in the two explicit
cases is skipped with the comment "Non-type members of a run-time type are always run-time too". A **new type
form** nested in a run-time type therefore falls into this arm, and a compile-time type declared with it is
neither un-nested nor reported. The comment's premise (the default arm holds only non-type members) stops being
true the moment a new type declaration kind exists.

### 6.9 `TransformCompileTimeType` default arm

`ProduceCompileTimeCodeRewriter.cs:555-558` — `members.Add( (MemberDeclarationSyntax) this.Visit( member ).AssertNotNull() )`.
An unknown member of a compile-time type is copied through without the template compilation, the manifest entry,
or the scope classification that every named case performs. The member survives into the compile-time assembly
uninterpreted. Compare line 515-516, where an indexer is at least a loud `NotImplementedException` — and note
that "indexers declared inside an extension block" is one of the two no-new-syntax C# 15 features.

### 6.10 `_defaultCompileTimeTargetFrameworks` still names `net8.0`

`CompileTimeAssemblyLocator.cs:43`. The reference-assembly project is built for `netstandard2.0;net8.0;net48`
although only `assemblies-netstandard2.0.txt` is read (line 664). On a machine with only a .NET 11 SDK this either
downloads a `net8.0` reference pack for nothing or fails with `NETSDK1045`, which is at least classified
(`ReferenceAssemblyBuildFailureClassifier.cs:147-152`). `Options/DefaultProjectOptions.cs:56` similarly still
returns `"net8.0"` as the default `TargetFramework`, which becomes a directory segment through
`OutputPathHelper.GetOutputPaths`.

### 6.11 `RoslynApiVersion` is serialized as a bare integer

`TemplateSymbolManifest.UsedApiVersion` (`CompileTime/Manifest/TemplateSymbolManifest.cs:31`) is a
`RoslynApiVersion?` serialized by System.Text.Json with no converter, so the wire value is the enum ordinal, and
the ordinals are assigned by position in `GenerateMetaSyntaxRewriter.cs:17-18`
(`version.Version.Index + deprecatedVersionNames.Length`, `Generator.cs:88`). Appending a version is safe;
**removing** one from the head of `legacyVersionNames` without moving it into `deprecatedVersionNames` shifts
every ordinal and silently reinterprets the manifests of already-compiled references. `CompileTimeProjectManifest`
guards its own shape with `ManifestVersion` (line 92, currently `1`) but that guard does not cover an ordinal
change inside an unchanged manifest version. Contrast `CompileTimeProjectManifest.LanguageVersion`, which is
explicitly given `LanguageVersionJsonConverter` (line 96) with the comment at lines 94-95 about cross-Roslyn
compatibility.

---

## 7. How C# 14 was absorbed here (the pattern to repeat)

Two commits carry almost the whole of the C# 14 wave in this subsystem.

**`6e2b07a313` "Adding Roslyn 5.0 and moving net6.0 to net8.0"** — the Roslyn side. In
`SupportedCSharpVersions.cs` it added a `#if ROSLYN_5_0_0_OR_GREATER => LanguageVersion.CSharp14` arm to
`Default`, added `CSharp14` to `All` under the same guard, added
`RoslynApiVersion.V5_0_0 => (LanguageVersion) 1400` to `ToLanguageVersion`, and added
`V5_0_0 => "5.0.0-2.25460.106"` to `ToNuGetVersionString`. It also turned the fall-through arm into
`#error Invalid Roslyn version`. The `#if` ladder was later flattened by `e247425d69` (#1603) and `08d065a9f8`
(#1881) once the lower variants were dropped, which is why the file is now plain literals.

**`afbab4eae8` "Compile-time compilation uses lower lang version"** — the language-version side, and the template
for the C# 15 work. It:

- renamed `SupportedCSharpVersions.Default` to `Latest` and documented that it "might not be supported by the
  .NET SDK";
- created `Utilities/ILanguageVersionProvider.cs` and `Utilities/LanguageVersionProvider.cs`, that is, separated
  "what this Metalama build can do" from "what the .NET SDK in front of us can do";
- registered the provider in `Services/ServiceProviderFactory.cs`;
- added `LanguageVersion? languageVersion = null` to the `CompileTimeProjectManifest` constructor and the
  `LanguageVersion` property, so a compile-time project records the version it was built with;
- replaced `SupportedCSharpVersions.DefaultParseOptions` in `CompileTimeProjectRepository.Builder.cs` with parse
  options derived from the manifest;
- added `MetalamaTemplateLanguageVersion` end to end: `MSBuildPropertyNames`, `IProjectOptions`,
  `MSBuildProjectOptions`, `DefaultProjectOptions`, `ProjectOptionsWrapper`,
  `Metalama.CompilerVisibleProperties.props`, `TemplateCompiler.TryReadProjectOptions`;
- pointed `CompileTimeAspectPipeline.VerifyLanguageVersion` at `Latest`;
- added `SdkVersion` to `IProjectOptions` and read `NETCoreSdkVersion` in `MSBuildProjectOptions`;
- added `TestContextOptions.TemplateLanguageVersion` and `TestProjectOptions` so the testing layer can pin it.

Later commits in the same wave, for the feature work rather than the version plumbing:

- `aea7b2e5a2` (#1114, `field` keyword) touched `CompileTime/RunTimeAssemblyRewriter.cs` only: it added
  `SyntaxHelpers.ContainsFieldExpression` probes at lines 444 and 468-472 and a new
  `CompiledTemplateAttribute.IntroducesBackingField` argument. Nothing in `CompileTime/**` had to learn a new
  syntax node, because `FieldExpressionSyntax` was gated automatically by the generated verifier
  (`VisitFieldExpression → RoslynApiVersion.V5_0_0`).
- `a9698fa1e8`, `f374fce480`, `5a1ac3e5c4` and the rest of #1159 (extension blocks) added
  `AdviceImpl/Introduction/IntroduceExtensionBlock*`, `CodeModel` and `Linking` support. **They added nothing to
  `CompileTime/**`**: an extension block can be introduced into run-time code, but no compile-time (aspect) type
  may itself be an extension block, so the rewriters were never taught the node. This is why
  `SyntaxKindExtensions.IsTypeDeclaration` still excludes `ExtensionBlockDeclaration`.
- `a67fac8277` (#1247) added the whole `GetLanguageVersionFromMSBuild` path and
  `SupportedCSharpVersions.GetMaxLanguageVersion`.
- `#1896` added the `Standalone/TemplateLanguageVersion14` scenario, whose README states the invariant that binds
  `MetalamaTemplateLanguageVersion` to `RoslynApiMinVersion`.

The shape of the pattern, restated for C# 15:

1. Add the grammar snapshot, list it in `GenerateMetaSyntaxRewriter.cs`, run `build.ps1 prepare`. The verifier and
   the meta rewriter follow automatically **for non-experimental nodes only**.
2. Add the `LanguageVersion` constant to `AllLanguageVersions`, its display string to `ToDisplayStringSafe`, and
   raise `Latest` and `All`.
3. Map the new `RoslynApiVersion` member to it in `ToLanguageVersion`, and give it its package version string in
   `ToNuGetVersionString` (which also decides the prerelease feed).
4. Extend the SDK-major switch in `LanguageVersionProvider` and the Roslyn-assembly-version switch in
   `GetMaxLanguageVersion`.
5. Raise `MetalamaTemplateLanguageVersion` in `Directory.Build.props` only together with `RoslynApiMinVersion`.
6. Add the per-feature support in `AdviceImpl`, `CodeModel`, `Linking` and `Templating`. `CompileTime/**` only
   changes when the construct can appear **inside compile-time code**, which for C# 15 means `union` and `closed`
   on an aspect class, and an indexer inside an extension block.

---

## 8. Quick index of the load-bearing lines

| File | Lines | What |
| --- | --- | --- |
| `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` | 31, 38-43, 50, 52-62, 77-87, 93, 99, 104, 117-118, 131-132, 134-144, 149-159 | Every C#/Roslyn version constant and mapping |
| `.../Utilities/AllLanguageVersions.cs` | 14-18 | Roslyn-independent `LanguageVersion` constants |
| `.../Utilities/Roslyn/LanguageVersionExtensions.cs` | 16-40 | Exhaustive display-string switch, throws on unknown |
| `.../Utilities/LanguageVersionProvider.cs` | 29, 35, 45-72 (54-60), 74-123 (88, 107, 111) | The compile-time language version decision |
| `.../Utilities/ILanguageVersionProvider.cs` | 10-17 | Service contract |
| `.../Pipeline/CompileTime/CompileTimeAspectPipeline.cs` | 62-93, 177 | `VerifyLanguageVersion`, LAMA0051/LAMA0052 |
| `.../Templating/TemplateCompiler.cs` | 33, 51, 55-79, 106 | `MetalamaTemplateLanguageVersion`, verifier construction |
| `.../Templating/RoslynVersionSyntaxVerifier.cs` | 24, 32-38, 41-52, 55-75, 78-89 | The version gate for template syntax |
| `.../Templating/TemplateExpansionContext.cs` | 861-878 | LAMA0282, consumer-side version check |
| `.../CompileTime/CompileTimeCompilationBuilder.cs` | 64, 120, 123-167, 169-247 (239, 243), 251-395 (279, 351, 360), 411-454 (425-427, 430), 1142, 1282, 1355 | Hashing, parse options, manifest writing |
| `.../CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs` | 204-252, 254-373 (272, 356, 370), 417-447, 508-563, 1452-1478, 1526-1535, 1562-1580 | Construct-shaped dispatch |
| `.../CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs` | 58-99 | Whether a file has compile-time code |
| `.../CompileTime/CompileTimeAssemblyLocator.cs` | 43, 194, 209-224, 234-243, 261-296, 389-430, 664, 705, 740-767 (751, 756), 777-830, 838-848 | Reference-assembly project, prerelease feed, cache key |
| `.../CompileTime/ReferenceAssemblyBuildFailureClassifier.cs` | 83, 115-152, 177-208, 302-306 | Failure explanations, including the prerelease-feed one |
| `.../CompileTime/Manifest/CompileTimeProjectManifest.cs` | 38, 63, 88, 90, 92, 96-97, 99-101 | The `LanguageVersion` field and its converter |
| `.../CompileTime/Manifest/TemplateSymbolManifest.cs` | 31, 43, 49 | `RoslynApiVersion? UsedApiVersion`, serialized as an ordinal |
| `.../CompileTime/CompileTimeProjectRepository.Builder.cs` | 62, 526-561, 581-590, 596-604 | Version guards, manifest parse options |
| `.../CompileTime/DesignTimeCompatibility.cs` | 33, 42-48 | Design-time generation floor |
| `.../Serialization/LanguageVersionJsonConverter.cs` | 16-43 | Integer serialization of `LanguageVersion` |
| `.../Options/IProjectOptions.cs` | 75-88, 123, 197-205, 215, 261-270 | The option surface |
| `.../Options/MSBuildProjectOptions.cs` | 87-93, 108, 144-153, 167-182, 186-191 | MSBuild binding |
| `.../Options/DefaultProjectOptions.cs` | 56, 77, 101-113, 127-131 | Defaults, including `net8.0` and `Latest` |
| `.../Options/MSBuildPropertyNames.cs` | 24, 31, 41-44, 54-58 | Property names |
| `.../Options/TargetedAssemblyReference.cs` | 19-24 | `net472`/`net10.0` literal, Roslyn version match |
| `.../Utilities/Roslyn/SyntaxKindExtensions.cs` | 33-41 | `IsTypeDeclaration`, `IsBaseTypeDeclaration` |
| `.../Utilities/GlobalJsonHelper.cs` | 22-36 | SDK pin for the nested build |
| `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs` | 16-18 | The version list |
| `eng/src/GenerateMetaSyntaxRewriter/Generator.cs` | 63-96, 98-170 | Enum and verifier generation |
| `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs` | 37, 57 | Experimental-node removal |
| `Directory.Build.props` | 16 | `MetalamaTemplateLanguageVersion` = `14.0` |
| `Directory.Packages.props` | 20-28 | `RoslynApiMinVersion`, `RoslynApiMaxVersion` |
| `Metalama.Framework/docs/updating-roslyn.md` | 10-12, 29-36, 38-54 | The procedure, including the prerelease switch |
| `Metalama.Framework/docs/compile-time-target-frameworks.md` | 1-50 | Why the compile-time compilation is always `netstandard2.0` |
