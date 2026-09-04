# Established facts (verified 2026-09-03) for the .NET 11 / C# 15 / Roslyn 5.10 impact analysis

Repositories:
- Metalama: /home/user/Metalama, branch topic/2027.0/26-09-03-update-eng-7e3j07 (8 commits ahead of origin/develop/2027.0, 0 behind).
- Metalama.Premium: /home/user/metalama.premium, branch develop/2027.0 (shallow clone).
- Metalama.Compiler is NOT cloned. Do not guess its content; state dependencies on it as assumptions.

Platform baseline PB-2027.0 (Metalama.Framework/docs/platform-support.md):
- VS 2026 LTSC (Nov 2026) and VS 2027 (Nov 2026, ships .NET 11 and C# 15) are the design-time hosts. VS 2022 is dropped.
- User target frameworks: net10.0 and net11.0. net8.0/net9.0 dropped. Core embedded flavour = net10.0, Desktop = net472.
- Roslyn variants shipped: Roslyn.5.0.0 (serves Rider 2026.2 at Roslyn 5.0, C# Dev Kit) and Roslyn.5.10.0 (latest; VS 2026 LTSC, VS 2027, .NET 10/11 SDKs, Metalama.Compiler). Roslyn.4.12.0 dropped. RoslynApiMinVersion=5.0.0, RoslynApiMaxVersion=RoslynMaxVersion=5.10.0-1.26365.3 (a July 2026 prerelease from the roslyn-consolidated feed).
- Production source has zero `#if ROSLYN_*` blocks. Only ROSLYN_5_10_0_OR_GREATER exists, used by two aspect tests.
- MetalamaCompilerVersion = 2027.0.0 (eng/AutoUpdatedVersions.props).

C# language version plumbing (all currently stop at C# 14):
- Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs: Latest = CSharp14; All = {10..14}; ToLanguageVersion: V5_0_0 => CSharp14 AND V5_10_0 => CSharp14; GetMaxLanguageVersion: (>=5, _) => CSharp14; ToNuGetVersionString(V5_10_0) = "5.10.0-1.26365.3".
- Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60: SDK major >= 10 => CSharp14 (so the .NET 11 SDK yields C# 14 for the compile-time compilation).
- Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs: ToDisplayStringSafe maps (LanguageVersion)1300 and 1400 by numeric cast; no 1500 case (throws ArgumentOutOfRangeException).
- Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Manifest/CompileTimeProjectManifest.cs:101 ResolvedLanguageVersion defaults to CSharp13.
- Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:425 `if (languageVersion >= CSharp14)` adds EMBED_SYSTEM_TYPES.
- Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-90 VerifyLanguageVersion: Preview allowed only with AllowPreviewLanguageFeatures; any version not in All is reported as CSharpVersionNotSupported.
- Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:115-121: when LangVersionImplicitlySet (set by Metalama.Compiler) and LangVersion is not one of 12.0/13.0/14.0/default/latest/latestMajor/preview, LangVersion is rewritten to 12.0 and MetalamaCheckLangVersion (line 243) emits a warning. A net11.0 project whose SDK-implied LangVersion is 15.0 therefore gets C# 12 plus a warning.
- OperatorData.cs lists C# 14 compound assignment operators with LanguageVersion.CSharp14.

Roslyn 5.10 grammar (eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml) versus 5.0.0:
- New nodes, all marked ExperimentalUrl: UnionDeclarationSyntax (TypeDeclarationSyntax; kinds UnionDeclaration, UnionKeyword), UnsafeExpressionSyntax (kind UnsafeExpression), WithElementSyntax (CollectionElementSyntax; kind WithElement). New optional experimental field `Name` (IdentifierNameSyntax) on BreakStatementSyntax and ContinueStatementSyntax. ParameterSyntax gains HasValidate/RequiredForTest only.
- eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs RemoveExperimentalDeclarations strips every node/field with ExperimentalUrl before generation, so MetaSyntaxRewriter, the template compiler's generated visitors, and the RoslynApiVersion checker currently know nothing of these nodes. When Roslyn 5.10 stable removes the ExperimentalUrl attributes, re-running the generator will start generating code for them.
- The `closed` modifier and extension indexers add no new syntax node (ClosedKeyword = SyntaxKind 8453 already exists in Roslyn main; extension indexers reuse IndexerDeclarationSyntax inside ExtensionBlockDeclarationSyntax).

C# 15 features per Roslyn main MessageID.RequiredVersion (fetched 2026-09-03):
- LanguageVersion.CSharp15 (value 1500): IDS_FeatureCollectionExpressionArguments (`with(...)` element), IDS_FeatureUnions, IDS_FeatureStaticMembersInInterfaces, IDS_FeatureClosedClasses, IDS_FeatureLabeledBreakContinue, IDS_FeatureExtensionIndexers.
- LanguageVersion.Preview: IDS_FeatureUnsafeEvolution (UnsafeExpressionSyntax). Not part of C# 15 for 2027.0.
- MapSpecifiedToEffectiveVersion maps Default/Latest/LatestMajor to CSharp15 on main. The consumed build 5.10.0-1.26365.3 is a July 2026 preview; the stable Roslyn 5.10 ships with VS 2027 / .NET 11 SDK in November 2026 and may differ.
- New SyntaxKind values on main: WithElement = 9081, UnionDeclaration = 9082, UnionKeyword = 8452, ClosedKeyword = 8453, SafeKeyword = 8454.

Metalama.Premium develop/2027.0 (NOT yet aligned with PB-2027.0):
- Directory.Packages.props: RoslynVersion=5.0.0, RoslynMaxVersion=5.0.0; references Metalama.Framework.Implementation.5.0.0 and .4.12.0.
- Variant projects still present: Metalama.Extensions.CodeFixes.DesignTime.4.12.0, Metalama.Extensions.CodeFixes.Engine.4.12.0, Metalama.Extensions.Validation.Engine.4.12.0. Latest variant is 5.0.0. Needs 4.12.0 dropped, 5.0.0 becoming the lower variant with a project suffix, and a 5.10.0 latest.
- Other Premium projects: Metalama.Extensions.Architecture, Metalama.Extensions.CodeFixes(.DesignTime/.Engine/.Package), Metalama.Extensions.Validation(.Engine/.Package), Metalama.Licensing(.BuildTasks), Metalama.Patterns.Caching.Backends.Azure/Redis, tests.

Related closed issues on metalama/Metalama: #1881 (Roslyn 5.10 support, obsolete variants removed), #1896 (template language raised to C# 14), #1039 (C# 14 umbrella), #1159/#1160/#1035 (C# 14 extension members), #1143/#1110 (partial constructors), #1131 (compound assignment operators), #1109 (null-conditional assignment). Open: #985 (template compiler later C# features catch-all), #942 (C# 11 generic aspects), #1217 (Metrics multi-Roslyn).

Docs: Metalama.Framework/docs/{platform-support,updating-roslyn,testing,linker-*,compilation-model,pipeline,design-time-memory,extensibility,compile-time-target-frameworks}.md and Directory.Packages.md.

## Addendum, verified 2026-09-03 (after the first round of reports)

Roslyn release cadence and the target version for 2027.0:
- nuget.org serves Microsoft.CodeAnalysis.CSharp stable versions 5.0.0, 5.3.0 (2026-03-10), 5.6.0 (2026-07-02) and 5.9.0 (2026-08-17). No 5.10, 5.11 or 5.12 package exists on nuget.org. Roslyn ships a stable every third minor, in step with the quarterly Visual Studio 2026 releases 18.0, 18.3, 18.6 and 18.9.
- eng/Versions.props on dotnet/roslyn main reads MajorVersion 5, MinorVersion 12. The only release branches are release/dev18.0 and release/dev18.3. The November 2026 baseline (Visual Studio 2026 long-term servicing channel and Visual Studio 2027, with the .NET 11 SDK) is therefore expected to carry Roslyn 5.12, not 5.10 or 5.11. The consumed 5.10.0-1.26365.3 is a main build of 2026-07-15 with no stable counterpart; the transition to the stable Roslyn is a renumbering of the latest variant to 5.12.0, following updating-roslyn.md step 7.
- The prerequisite is Metalama.Compiler moving to Roslyn 5.12 (updating-roslyn.md step 1). Not verified in this session; Metalama.Compiler is not cloned.

State of the C# 15 API in the stable Roslyn 5.9.0 assemblies (downloaded from nuget.org and inspected):
- Present, but experimental (RSEXPERIMENTAL006): UnionDeclarationSyntax, SyntaxFactory.UnionDeclaration, CSharpSyntaxVisitor.VisitUnionDeclaration, WithElementSyntax, SyntaxFactory.WithElement, CSharpSemanticModel.GetSymbolInfo(WithElementSyntax), SyntaxKind.ClosedKeyword, ITypeSymbol.IsUnion, ITypeSymbol.IsClosed, GetClosedDerivedTypeInfo, ClosedDerivedTypeInfo, UnsafeExpressionSyntax. The public ITypeSymbol.UnionCaseTypes is absent (added to main on 2026-07-31; the UnionCaseTypes strings in the 5.9.0 binaries are internal member names). The 5.9.0 assemblies are built from the same commit lineage as the consumed 5.10.0-1.26365.3 (no public API change between 2026-06-24 and 2026-07-31), so the two expose the same API. Present and experimental (RSEXPERIMENTAL007): IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput.
- Absent: LanguageVersion.CSharp15 (added to main by dotnet/roslyn#84799 on 2026-08-11; the only "CSharp15" string in 5.9.0 is the InternalsVisibleTo name of a test assembly). In 5.9.0 and in the consumed 5.10 preview, the C# 15 features are reachable only under LanguageVersion.Preview.
- On main (5.12), LanguageVersion.CSharp15 = 1500 exists, Default/Latest/LatestMajor map to CSharp15, the six features are gated on CSharp15, and the experimental markers of the union, with-element and labeled-break syntax are removed (per the Roslyn API delta report; the date of that change is being verified).
- Consequence: no Roslyn that Metalama consumes today exposes C# 15 as a non-preview language version. C# 15 support in 2027.0 requires the move to Roslyn 5.12, and every engine reference to the union or closed API must be gated to the latest variant, because the Roslyn 5.0 variant that serves Rider does not have it.

Work already in progress (do not propose it again):
- metalama/Metalama#1913 and metalama/Metalama.Premium#85 (open, green): Premium moves to net10.0, drops the Roslyn 4.12 variant, adds the 5.0.0 lower variant and the 5.10.0 latest variant, adds nuget.base.config with the roslyn-consolidated feed.
- metalama/Metalama.Premium#84 (open): mirrors the re-derived Out-of-band package caps of #1897.
- metalama/Metalama#1879 (open): materializes compiler-synthesized record members so that meta.Proceed() works (#1343). Relevant to the synthesized union members (Value property and per-case constructors).
- metalama/Metalama#1903 (open): re-derives the .NET 8.0 line pins of user-surfacing packages.
