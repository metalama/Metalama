# Metalama.Premium: impact of .NET 11, C# 15 and Roslyn 5.10 for 2027.0

Subject: `/home/user/metalama.premium`, branch `develop/2027.0` (commit 21521e0), read together with the post-change tree of pull request metalama/Metalama.Premium#85 (local branch `pr85`). Reference: `/home/user/Metalama` (core), the commits that dropped Roslyn 4.12.0 and added 5.10.0 (topic/2027.0/1881-roslyn-5.10, merged as 9a26a232) and the build-image commits debc244c to 6f9a59df.

## Summary

1. The variant set and the target framework alignment with PB-2027.0 are tracked by metalama/Metalama#1913 and implemented by Premium#85. I verified its diff file by file: Roslyn 4.12.0 is gone, Roslyn 5.0.0 is the suffixed lower variant, Roslyn 5.10.0 is the latest variant, `nuget.base.config` maps `Microsoft.CodeAnalysis.*` to the roslyn-consolidated feed, every `net8.0` in `src` is `net10.0`, `net471` is `net472`, the `InternalsVisibleTo`, package lists, extension props and `Metalama.Framework.Implementation.*` references follow, and no `ROSLYN_*` constant or test directive remains. PR-1, PR-2 and PR-15 record what #85 leaves for the stable Roslyn 5.10 transition and for testing the 5.0 variant.
2. The build infrastructure of Premium is not aligned: `eng/src/Program.cs` still describes a VS 2022 17.14 container with the .NET 8 SDK and the .NET 6 and 9 runtimes, `BuildMetalamaPremium.csproj` targets `net9.0`, and the generated Docker files follow. The three standalone solutions built by `MSBuild.exe` therefore run on a host that PB-2027.0 excludes and that the core `Metalama.Framework.props` now reports as unsupported (PR-3, PR-4).
3. No Premium source duplicates the core language-version, Roslyn-version or resource-extraction logic. The duplicated logic is in build files: the `net10.0` literal of the extension loader, the `tasks/<tfm>` selection of `Metalama.Licensing.targets`, and the container definition (PR-9, PR-15). Two comments and one property are stale (PR-5, PR-14).
4. C# 15 semantics: the architecture rules need no change for `closed` classes or unions (PR-12); two defects are exposed, one by unions and one by extension indexers: `ChangeVisibilityCodeAction` silently skips union, interface and indexer declarations (PR-10), and `ReferenceValidationContext` throws for a validated extension block (PR-11).
5. No test runs on the .NET 11 SDK, in Premium or in core, and the only Premium asset selected by the SDK is the `tasks/net10.0` licensing task (PR-8).

## Findings

### PR-1. Stable Roslyn 5.10 transition edits that remain after Premium#85

- Where: `/home/user/metalama.premium/Directory.Packages.props:8-9` (on `pr85`), `/home/user/metalama.premium/nuget.base.config:1-18` (on `pr85`), `/home/user/metalama.premium/eng/RoslynVersions/Roslyn.5.10.0.props:3,9-10` (on `pr85`).
- What happens today: `RoslynApiMaxVersion` and `RoslynMaxVersion` are exported by the core build (`/home/user/Metalama/eng/src/Program.cs:151`) and imported by Premium through `eng/Versions.g.props` (`/home/user/metalama.premium/eng/Versions.props:15`), so the `5.10.0-1.26365.3` literals in `Directory.Packages.props` are fallbacks for a build without `Build.ps1 prepare`. `nuget.base.config` is a copy of the core file (`/home/user/Metalama/nuget.base.config`, added by core commit e01ee144). `Roslyn.5.10.0.props` pins `System.Text.Json` and `System.IO.Pipelines` to 10.0.11, which is what the core latest variant requires (`/home/user/Metalama/eng/RoslynVersions/Roslyn.5.10.0.props:11-12`).
- Consequence class: build or restore error, when the core repository leaves the prerelease Roslyn (procedure in `/home/user/Metalama/Metalama.Framework/docs/updating-roslyn.md:38-54`) and Premium is not changed in the same release: either the roslyn-consolidated feed is removed from core while Premium still resolves a preview from it, or Premium removes it while the exported `RoslynApiMaxVersion` still carries a prerelease label.
- Proposed change: when core sets `RoslynApiMaxVersion` to the stable `5.10.0`, change the two fallback literals, keep or remove `nuget.base.config` in the same commit as core removes its own, and re-derive the `SystemTextJsonVersion` and `SystemIOPipelinesVersion` of `Roslyn.5.10.0.props` from the stable `Microsoft.CodeAnalysis.CSharp.Features` 5.10 dependencies. Record this in Premium as a comment on the two literals, because nothing in Premium points at the core procedure.
- Size: S.
- Confidence: verified.
- Covered by #85: the current values, yes; the transition edits, no.
- Open questions: whether the exported `RoslynMaxVersion` is also used by Premium's `Versions.g.props` in continuous integration only or on developer machines too; the file is git-ignored, so I could not read it.

### PR-2. The Roslyn 5.0 variant of the Premium engines is built but never executed by a test

- Where: `/home/user/metalama.premium/Metalama.Premium.sln` (on `pr85`, lines 65-69 declare `Metalama.Extensions.CodeFixes.Engine.5.0.0`, `Metalama.Extensions.CodeFixes.DesignTime.5.0.0` and `Metalama.Extensions.Validation.Engine.5.0.0`; no `*.AspectTests.5.0.0` or `*.UnitTests.5.0.0` project exists); `/home/user/metalama.premium/src/tests/Metalama.Extensions.Validation.AspectTests/Metalama.Extensions.Validation.AspectTests.csproj:7,42` and `/home/user/metalama.premium/src/tests/Metalama.Extensions.CodeFixes.AspectTests/Metalama.Extensions.CodeFixes.AspectTests.csproj:7,41` import `Latest.props` and reference the unsuffixed engine only.
- What happens today: the core repository ships a shim test project per lower variant (`/home/user/Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests.5.0.0`, `...UnitTests.5.0.0`, and others) and runs the whole solution so that the older variant is tested (`/home/user/Metalama/eng/src/Program.cs:73-75`, issue #1811). Premium has no counterpart. The 5.0 variant is the payload that serves Rider 2026.2 (`/home/user/Metalama/Directory.Packages.md:197-204`).
- Consequence class: silent wrong output. A defect that appears only when the Premium engines bind against `Microsoft.CodeAnalysis` 5.0.0.0 is not detected before a Rider user reports it. This was already true of the 4.12.0 variant, but that variant served a host that was also covered by manual Visual Studio 2022 testing; the 5.0 variant serves no host that the team tests by hand.
- Proposed change: add `Metalama.Extensions.Validation.AspectTests.5.0.0` and `Metalama.Extensions.CodeFixes.AspectTests.5.0.0` shim projects following the core pattern (`Compile Include` of the sibling sources, `Import` of `Roslyn.5.0.0.props`, `Import` of the sibling project), add them to the solution, and let `Build.ps1 test` run them. The unit test projects reference `Microsoft.CodeAnalysis.CSharp.Workspaces` and `.Features` at `RoslynMaxVersion` (`Metalama.Extensions.CodeFixes.UnitTests.csproj:27-28`, `Metalama.Extensions.Validation.UnitTests.csproj:27-28`) and need a variant-conditional override, as the core `Metalama.Framework.Tests.UnitTests.5.0.0` project does.
- Size: M.
- Confidence: verified.
- Covered by #85: no.
- Open questions: none.

### PR-3. The build container definition is stale relative to PB-2027.0 and to the core repository

- Where: `/home/user/metalama.premium/eng/src/Program.cs:20-55` (unchanged on `pr85`); generated files `/home/user/metalama.premium/eng/docker/build.Dockerfile:43-56`, `/home/user/metalama.premium/eng/docker/vs17.Dockerfile:33-36`, `/home/user/metalama.premium/eng/docker-context/VisualStudio.17.14.15.Release.chman`, `/home/user/metalama.premium/DockerBuild.ps1`; `/home/user/metalama.premium/Directory.Packages.props:16` (`PostSharpEngineeringVersion` 2023.2.412; core has 2023.2.420 at `/home/user/Metalama/Directory.Packages.props:12`).
- What happens today: the container installs the .NET SDK 10.0.102 (`PreferredVersions.DotNetSdk.V_10_0`) and the .NET SDK 8.0.417 ("The runtime is required by all tests", which is no longer true after #85), the .NET 6.0.36 runtime ("Required by some tests"; no project targets `net6.0`), the .NET 9.0.12 runtime (for PR-4), and Visual Studio 2022 Build Tools 17.14.15. `MSBuildVersion` is 17.14. The core repository moved to the .NET SDK 10.0.400 as the only SDK, Visual Studio 2026 Build Tools 18.9.2 with the 4.7.2 and 4.8 components, and `MSBuildVersion` 18.9 (`/home/user/Metalama/eng/src/Program.cs:19-63`, commits debc244c, 37df9404, bb0bf0e0, 6f9a59df). The three `MsbuildSolution` entries (`Program.cs:69-72`) build `CodeFixes.sln`, `Validation.sln` and `CachingBackends.sln` with the `MSBuild.exe` of Visual Studio 2022 17.14, which PB-2027.0 excludes (`/home/user/Metalama/Metalama.Framework/docs/platform-support.md:124-134`). The core `Metalama.Framework.props` now seeds `MinimumVisualStudioVersion` 18.0 (`/home/user/Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:37`) and `MetalamaCheckSupportedToolchain` reports `LAMA0602` when `MSBuildRuntimeType` is `Full` and `MSBuildVersion` is below it (`/home/user/Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:391-417`). `Validation/test.json:3` and `CachingBackends/test.json:2` set `FailOnUnexpectedDiagnostics`.
- Consequence class: diagnostic reported (`LAMA0602` in the three MSBuild-based standalone builds), and the licensing task is exercised under an MSBuild that no supported host ships. Whether the standalone harness fails on that warning depends on PostSharp.Engineering, which is not in the clone; #85 is reported green, so either the harness tolerates warnings or the warning is not reached.
- Proposed change: mirror the four core commits: raise `PostSharpEngineeringVersion` to 2023.2.420, pin `dotNetSdkVersion` to `10.0.400` with `AllowPrerelease`, replace the Visual Studio component with `VisualStudioBuildToolsComponentVersion.v18_9_2` and add the 4.7.2 and 4.8 targeting pack and SDK components, remove the .NET 8 SDK and the .NET 6 and 9 runtimes, set `MSBuildVersion` to 18.9, then run `Build.ps1 generate-scripts` to regenerate `DockerBuild.ps1`, `eng/docker`, `eng/docker-context` and `.teamcity`. Read the `MSBuildExtensionsPath` note in the core `CLAUDE.md` first; the second .NET 10 feature band that Visual Studio 2026 installs is what caused the core failures.
- Size: M. Each mistake costs a continuous integration cycle.
- Confidence: verified for the staleness; plausible for the `LAMA0602` effect. Check the build log of #85 for `LAMA0602` before deciding.
- Covered by #85: no.
- Open questions: whether the `Microsoft.NetCore.Component.SDK` component of 18.9.2 (SDK 10.0.400) and a pinned `global.json` reproduce the `MSB4062` conflict that core fixed in 6f9a59df, which is in PostSharp.Engineering and therefore shared.

### PR-4. The engineering tool targets `net9.0`

- Where: `/home/user/metalama.premium/eng/src/BuildMetalamaPremium.csproj:5` (unchanged on `pr85`); `/home/user/metalama.premium/eng/src/Program.cs:32-33` (".NET 9 runtime, required by eng"); core counterpart `/home/user/Metalama/eng/src/BuildMetalama.csproj:6` is `net10.0`.
- What happens today: `Build.ps1` runs `BuildMetalamaPremium.dll` on the .NET 9 runtime, which leaves support on 2026-11-10 (`platform-support.md:195-197`). Issue #1913 counts this occurrence ("net9.0: 1") and #85 does not change it.
- Consequence class: no impact on the shipped packages; build infrastructure only.
- Proposed change: `net10.0`, and remove the .NET 9 runtime component together with PR-3.
- Size: S.
- Confidence: verified.
- Covered by #85: no.
- Open questions: none.

### PR-5. `MetalamaTemplateLanguageVersion` is 13.0 with a Visual Studio 2022 rationale

- Where: `/home/user/metalama.premium/Directory.Build.props:18-20` (unchanged on `pr85`); core `/home/user/Metalama/Directory.Build.props:12-17` is 14.0, with a comment that derives the value from `RoslynApiMinVersion` (core issue #1896, commit 980027f7).
- What happens today: the compile-time code of `Metalama.Extensions.Architecture` and `Metalama.Extensions.Validation` is compiled as C# 13 by the Metalama pipeline, and the comment names Visual Studio 2022 as the reason, which PB-2027.0 no longer supports.
- Consequence class: no impact. Premium has no templates, and C# 13 is within the supported set.
- Proposed change: set 14.0 and copy the core comment, so that the two repositories move together on the next floor change.
- Size: S.
- Confidence: verified.
- Covered by #85: no.
- Open questions: none.

### PR-6. Residual pins and comments in `Directory.Packages.props` and the test props

- Where: `/home/user/metalama.premium/Directory.Packages.props:5-6` (`RestorePrerelease` comment), `:68-70` (`Microsoft.Build.Framework`, `.Core`, `.Tasks.Core` 17.10.46), `:74` (`Microsoft.NET.Test.Sdk` 17.14.1); `/home/user/metalama.premium/src/tests/Directory.Build.props:17` (`MicrosoftBclAsyncInterfacesVersion` 9.0.0); `/home/user/metalama.premium/THIRD-PARTY-NOTICES.md:38`.
- What happens today: the core repository derives `MicrosoftBuildVersion` 18.0.2 from the lowest MSBuild host of the baseline (`/home/user/Metalama/Directory.Packages.props:35-50`); Premium keeps 17.10.46, which `Metalama.Licensing.BuildTasks` references with `ExcludeAssets="Runtime"` (`Metalama.Licensing.BuildTasks.csproj:23-24`), so it is an API floor only and binds to the host MSBuild at run time. `MicrosoftBclAsyncInterfacesVersion` is read by nothing: the package version uses `MicrosoftBclAsyncInterfacesLatestVersion` (`src/tests/Directory.Packages.props:6`), which #85 raises to 10.0.11. `THIRD-PARTY-NOTICES.md` lists StackExchange.Redis 2.8.31, which is neither of the two versions in the props file (see PR-7). `Microsoft.NET.Test.Sdk` 17.14.1 is the same value as core (`/home/user/Metalama/Directory.Packages.props:168`).
- Consequence class: no impact.
- Proposed change: align `Microsoft.Build.*` with the core `MicrosoftBuildVersion` and cite the doctrine, delete the dead property, regenerate the bill of materials, and reword the `RestorePrerelease` comment (the .NET SDK 10 is no longer a prerelease; the property is now needed for the Roslyn 5.10 preview).
- Size: S.
- Confidence: verified.
- Covered by #85: no.
- Open questions: whether `Microsoft.Build.Tasks.Core` 18.0.2 still ships a `net472` asset for the `net472` build of the task; verify before raising.

### PR-7. The `StackExchange.Redis` version update is inert

- Where: `/home/user/metalama.premium/Directory.Packages.props:76` (`<PackageVersion Update="StackExchange.Redis" Version="2.10.14" />`) and `:99` (`<PackageVersion Include="StackExchange.Redis" Version="2.9.32" />`); the same two lines are 75 and 98 on `pr85`, which does not change them.
- What happens today: an MSBuild item `Update` applies only to items that exist at that point of evaluation. The `Update` precedes the `Include` in the same item group, so the effective central version is 2.9.32 and the 2.10.14 line changes nothing. `Metalama.Patterns.Caching.Backends.Redis` and the load tests reference the package without a version override.
- Consequence class: no impact on .NET 11; the Redis backend ships against an older client than the file intends. Found while checking the caching backend dependencies for theme 2.
- Proposed change: replace the pair with one `Include` at 2.10.14 and confirm the version in the restored `project.assets.json`.
- Size: S.
- Confidence: plausible. I did not run a restore in this container.
- Covered by #85: no.
- Open questions: none.

### PR-8. No test leg exercises the .NET 11 SDK

- Where: test projects on `pr85` target `netframework4.8;net10.0` (`Metalama.Extensions.CodeFixes.AspectTests.csproj:11`, `Metalama.Extensions.Validation.AspectTests.csproj:11`, both `UnitTests.csproj:6`) or `net472;net10.0`; every standalone project targets `net10.0`; the container (PR-3) carries no .NET 11 SDK. The core test projects are `net48;net10.0` as well (`/home/user/Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14`).
- What happens today: PB-2027.0 puts the .NET 11 SDK in the supported set (`platform-support.md:195-197`). The Premium assets that the SDK selects are `tasks/net10.0/Metalama.Licensing.BuildTasks.dll`, chosen by `/home/user/metalama.premium/src/Metalama.Licensing/build/Metalama.Licensing.targets:12` for any `MSBuildRuntimeType` of `Core`, and the ILRepacked `Metalama.Backstage` inside it (`Metalama.Licensing.BuildTasks.csproj:38-48`). Nothing loads that assembly into the MSBuild of the .NET 11 SDK before a user does.
- Consequence class: no impact today; an assembly-binding failure of the merged task on the .NET 11 runtime would surface as a user report.
- Proposed change: follow the core decision on a .NET 11 leg for the standalone tests (a `global.json` per scenario and an SDK component in the container). If core adds one, the `ManyDotNetSolutions` and `MsbuildSolution` entries of Premium should run on it; the licensing task is the asset that justifies it.
- Size: M, most of it in the container.
- Confidence: plausible.
- Covered by #85: no.
- Open questions: whether the .NET 11 SDK is available in the PostSharp.Engineering component catalogue at the time of writing.

### PR-9. `Metalama.Licensing.targets` selects `tasks/net10.0` without a runtime guard

- Where: `/home/user/metalama.premium/src/Metalama.Licensing/build/Metalama.Licensing.targets:10-15` (on `pr85`).
- What happens today: the directory is chosen from `MSBuildRuntimeType` alone. On the .NET 11 SDK the `net10.0` assembly loads by roll-forward, which is the same mechanism the toolset relies on (`platform-support.md:338-339`). On an MSBuild below .NET 10, the `UsingTask` fails with a raw `MSB4062`. `platform-support.md:345-346` records the identical drift point for `Metalama.Compiler.Sdk.props`; the Premium copy is not listed.
- Consequence class: build or restore error, only outside the baseline. Inside it, `Metalama.Framework.targets` reports `LAMA0601` for an SDK below 10.0 before `CoreCompile`, so the user gets a diagnostic first.
- Proposed change: none for 2027.0. Add the file to the list of drift points in `platform-support.md`, so that the next floor move edits it.
- Size: S.
- Confidence: verified.
- Covered by #85: the `net10.0` value, yes; the guard and the documentation, no.
- Open questions: none.

### PR-10. `ChangeVisibilityCodeAction` silently skips union, interface and indexer declarations

- Where: `/home/user/metalama.premium/src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs:72-116` (overrides for class, record, struct, field, event, event field, property, enum, delegate, constructor, method, destructor, accessor, operator and conversion operator only); `/home/user/metalama.premium/src/Metalama.Extensions.CodeFixes/CodeFixFactory.cs:101-117` (the public `ChangeAccessibility` accepts any `IMemberOrNamedType`).
- What happens today: the rewriter derives from the core `SafeSyntaxRewriter`, which visits an unhandled node through the default visit. A `UnionDeclarationSyntax` (a `TypeDeclarationSyntax`, `/home/user/Metalama/eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954-1978`), an `InterfaceDeclarationSyntax` and an `IndexerDeclarationSyntax` are visited but `ChangeModifiers` is never applied to them. `ExecuteAsync` then calls `context.UpdateTree` with an unchanged root (`ChangeVisibilityCodeAction.cs:46-48`), and `CodeActionContext.ToCodeActionResult` reports the file as changed (`CodeActionContext.cs:82-93`). The user sees the code fix succeed and nothing change. The interface and indexer cases exist today; C# 15 adds the union case.
- Consequence class: silent wrong output.
- Proposed change: replace the per-kind overrides on type declarations with one override of `Visit(SyntaxNode?)` that applies `TypeDeclarationSyntax.WithModifiers` to any `TypeDeclarationSyntax` (this covers interfaces, extension blocks, and unions without naming the union node, which is experimental in 5.10.0-1.26365.3 and absent from the 5.0 variant), add `VisitIndexerDeclaration`, and add aspect tests `ChangeVisibility_Interface` and `ChangeVisibility_Indexer`. A union test needs the stable Roslyn 5.10 grammar and `LangVersion` 15, and Premium defines no `ROSLYN_5_10_0_OR_GREATER`; if PR-2 adds the 5.0 test shims, the union test needs a `@RequiredConstant` and Premium must define the constant in `Roslyn.5.10.0.props` as core does.
- Size: S.
- Confidence: verified for the missing overrides; plausible for the union syntax shape, which the FACTS file records as experimental in the consumed build.
- Covered by #85: no.
- Open questions: none.

### PR-11. `ReferenceValidationContext.GetInboundGranularity` throws for a validated extension block

- Where: `/home/user/metalama.premium/src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124-134` (the switch lists Constructor, Event, Method, Field, Property, Indexer, Compilation, AssemblyReference, Namespace, NamedType, Parameter, TypeParameter and Attribute, and throws `ArgumentOutOfRangeException` otherwise); called from the `Destination` property at line 56-57.
- What happens today: the core repository treats an extension block as a valid validated declaration (`/home/user/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexerRequirements.cs:36,73`), exposes `DeclarationKind.ExtensionBlock` (`/home/user/Metalama/Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs:115-118`) and `INamedType.ExtensionBlocks` (`INamedType.cs:187`), and `IExtensionBlock` is an `INamedType` (`IExtensionBlock.cs:11`). A fabric can therefore write `.SelectMany(t => t.ExtensionBlocks).CanOnlyBeUsedFrom(...)`, which accepts `IQuery<IDeclaration>` (`ArchitectureExtensions.cs:34-39`). `ReferenceValidatorRunner` aggregates the references to the members of the validated symbol onto it (`ReferenceValidatorRunner.cs:111`, `ChildKinds.All`), so the validator runs, reads `context.Destination`, and throws. C# 15 extension indexers are declared inside extension blocks, which makes the block a more likely validation target than in C# 14.
- Consequence class: assertion or crash. The exception is raised inside the user validator and reported by `UserCodeInvoker` as an error diagnostic; the rule reports nothing else.
- Proposed change: map `DeclarationKind.ExtensionBlock` to `ReferenceGranularity.Type` and add a Validation aspect test that validates an extension block with an extension method, and on the latest variant an extension indexer.
- Size: S.
- Confidence: verified for the switch; plausible for the reachability, which I traced through the query API without running it.
- Covered by #85: no.
- Open questions: whether the core `DeclarationFactory` maps the indexer symbol of an extension type to an `IIndexer` whose `DeclaringType` is the `IExtensionBlock`; if it does not, the failure is in core before Premium is reached.

### PR-12. Architecture rules under `closed` classes and unions

- Where: `/home/user/metalama.premium/src/Metalama.Extensions.Architecture/Validators/DerivedTypeNamingConventionValidator.cs:80-84` (`IncludeDerivedTypes`, `ReferenceKinds.BaseType`); `/home/user/metalama.premium/src/Metalama.Extensions.Architecture/Aspects/InternalOnlyImplementAttribute.cs:25,46`; `/home/user/metalama.premium/src/Metalama.Extensions.Architecture/Predicates/TypeEqualityPredicate.cs:82-91`; `/home/user/metalama.premium/src/Metalama.Extensions.Architecture/Predicates/HasFamilyAccessPredicate.cs:19-28`; core `ReferenceIndexerRequirements.cs:35-40,57-65`; core `ReferenceIndexWalker.cs:176-180` (parameter types), `:715-732` (`is` expressions and recursive patterns).
- What happens today: the core drops derived-type indexing only for `INamedType.IsSealed`. A `closed` class is not sealed, so `DerivedTypesMustRespectNamingConvention`, `ValidateDerivedTypes` and `InternalOnlyImplement` keep working; the compiler already confines derivation of a closed class to its assembly, so the transitive validator that Premium persists for other projects (`TransitiveValidatorInstance.cs`) becomes redundant for it but harmless. A union declares its case types in a `ParameterList` (`Syntax-5.10.0.xml:1965`), so a `CanOnlyBeUsedFrom` rule on a case type reports the union declaration as a `ParameterType` reference whose origin is the union type, and `pet is Cat c` in a switch is reported as `IsType`, as for any class. `DerivedTypesMustRespectNamingConvention` applied to a union validates nothing, because case types do not derive from it.
- Consequence class: no impact, subject to the open questions.
- Proposed change: none in Premium. Add two aspect tests on the latest variant once the union syntax is stable: `CanOnlyBeUsedFrom` on a case type referenced by a union, and `DerivedTypesMustRespectNamingConvention` on a `closed` class.
- Size: S for the tests.
- Confidence: plausible. The union symbol shape depends on the stable Roslyn 5.10, which I could not read.
- Covered by #85: not applicable.
- Open questions: the `TypeKind` that the core code model assigns to a union symbol; `TypeKind.cs` has no union member and the FACTS file states that the core knows nothing of `UnionDeclarationSyntax`. Whether a `ReferenceKinds` member for a union case is wanted, so that users can filter it apart from `ParameterType`; that is a core API question.

### PR-13. Stale `ROSLYN_X_Y_OR_GREATER` guidance in the unit test projects

- Where: `/home/user/metalama.premium/src/tests/Metalama.Extensions.CodeFixes.UnitTests/Metalama.Extensions.CodeFixes.UnitTests.csproj:11-14` and `/home/user/metalama.premium/src/tests/Metalama.Extensions.Validation.UnitTests/Metalama.Extensions.Validation.UnitTests.csproj:11-14` (unchanged on `pr85`).
- What happens today: the comment tells the author to differentiate tests with `ROSLYN_X_Y_OR_GREATER`; after #85 no Premium variant defines a constant (`eng/RoslynVersions/Roslyn.5.0.0.props:8`, `Roslyn.5.10.0.props:8` on `pr85`), and these projects are not variant-built at all (PR-2).
- Consequence class: no impact.
- Proposed change: reword to say that the unit tests build against the latest Roslyn only, or delete the comment when PR-2 adds variant shims and a constant.
- Size: S.
- Confidence: verified.
- Covered by #85: no.
- Open questions: none.

### PR-14. The `net10.0` literal of the core extension loader is copied into eight Premium build files

- Where: `/home/user/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-20,24` and `/home/user/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31` compare the `TargetFramework` metadata of `MetalamaExtensionAssembly` with a literal; the Premium copies are `src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props`, `src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props`, both `MetalamaExtensionAssemblies.props`, both `*.Package.csproj` (`_AddAssembliesToOutput`), both `*.Package.Resources.csproj` and `Metalama.Extensions.CodeFixes.AspectTests.csproj:51` (all on `pr85`).
- What happens today: #85 moves every copy to `net10.0`, which is what makes the payload load on .NET after core commit 28c036a9. `platform-support.md:319-323` records that the two literals move with the Core flavour, but names only the core files; `extensibility.md:215-241` shows the pattern with `net10.0` but does not say that the value is compared as a string.
- Consequence class: silent wrong output at the next floor move, when a value is left behind: the extension is not loaded and no diagnostic is reported (the doctrine's own description of that failure).
- Proposed change: list the Premium files in `platform-support.md` under "What this means in this repository", or state in `extensibility.md` that the metadata must equal the Core flavour name of the baseline.
- Size: S.
- Confidence: verified.
- Covered by #85: the values, yes; the documentation, no.
- Open questions: none.

### PR-15. Variant set alignment (theme 1) is implemented by Premium#85

- Where: the `pr85` diff against `develop/2027.0`, 47 files. The items requested by the coordinator, verified in the post-change tree: `eng/RoslynVersions/Roslyn.4.12.0.props` deleted; `Roslyn.5.0.0.props` with `ThisRoslynVersionProjectSuffix` `.5.0.0`, `System.Text.Json` and `System.IO.Pipelines` 9.0.0, `System.Memory` 4.6.3, `Unsafe` 6.1.2; `Roslyn.5.10.0.props` with `ThisRoslynVersion=$(RoslynApiMaxVersion)`, `NoPreview` 5.10.0, empty suffix, 10.0.11 pins; `Latest.props` importing 5.10.0; the three shim directories renamed to `.5.0.0` and the solution updated; `Directory.Packages.props` with `RoslynVersion`/`RoslynMaxVersion` 5.10.0-1.26365.3 and `Metalama.Framework.Implementation.5.10.0` and `.5.0.0` (4.12.0 removed); `nuget.base.config` with the roslyn-consolidated feed and the `Microsoft.CodeAnalysis.*` mapping (identical to core `nuget.base.config` apart from one comment word); `InternalsVisibleTo` for `Engine.5.10.0`, `DesignTime.5.10.0`, `Engine.5.0.0`, `DesignTime.5.0.0` in `Metalama.Extensions.CodeFixes.csproj:17-20` and `Engine.5.10.0`/`Engine.5.0.0` in `Metalama.Extensions.Validation.csproj:16-17`; the `Package.Resources` project references; the `_AddAssembliesToOutput` lists (`CodeFixes.Package.csproj:53-62`, `Validation.Package.csproj:46-51`) and the build props with `TargetRoslynVersion` 5.10.0 and 5.0.0 and `TargetFramework` `net472`/`net10.0`; no `DefineConstants` in either variant; no `@RequiredConstant`, `@ForbiddenConstant` or `RequiredConstants` anywhere in `src/tests` (grep), and the two `metalamaTests.json` files carry only `FormatOutput` and `RemoveOutputCode`.
- What happens today: `develop/2027.0` without #85 fails restore with `NU1101` on `Metalama.Framework.Implementation.4.12.0` and `NU1701` on the `net8.0` projects, as #1913 records.
- Consequence class: build or restore error until #85 merges.
- Proposed change: merge #85. The residuals are PR-1 (stable transition), PR-2 (5.0 variant tests) and PR-13 (comment).
- Size: none beyond the pull request.
- Confidence: verified.
- Covered by #85: yes.
- Open questions: none.

## Non-findings

- Premium production and test source contains no `#if ROSLYN_*` block, no `@RequiredConstant` or `@ForbiddenConstant` directive and no `RequiredConstants` entry (grep over `/home/user/metalama.premium/src` and `eng`; the only `ROSLYN_` hits are the two props files and the two csproj comments of PR-13).
- No Premium source duplicates `SupportedCSharpVersions`, `LanguageVersionProvider`, `RoslynApiVersion` or `ResourceExtractor`: a grep for `LanguageVersion`, `RoslynVersion`, `GetManifestResourceStream`, `AssemblyLoadContext` and `AssemblyResolve` over `src` finds only `src/tests/Metalama.Patterns.Caching.Backends.UnitTests/RedisServer/RedisTestInstance.cs:49`, which extracts the embedded `redis-server.exe` for the tests.
- The Roslyn API surface used by the Premium design-time code is `CodeAction`, `ApplyChangesOperation`, `Document`, `Solution`, `SyntaxFactory.Comment` and `SyntaxNode.FindNode` (`LamaCodeAction.cs`, `CodeActionDescriptorExtensions.cs`, `PremiumCodeFixProviderExtension.cs:73-91`, `CodeFixService.cs:191-231`). The tests implement the Metalama abstractions `ICodeFixContext` and `ICodeRefactoringContext` (`TestCodeFixContext.cs`, `TestCodeRefactoringContext.cs`), not the Roslyn `CodeFixContext` constructors. No Roslyn 5.10 API break was found.
- `AddAttributeCodeAction` (`AddAttributeCodeAction.cs:44-56`) special-cases only `VariableDeclarator` and delegates to the core `SyntaxGenerator.AddAttribute`; `RemoveAttributeRewriter` (`RemoveAttributeCodeAction.RemoveAttributeRewriter.cs:27-54`) visits `AttributeSyntax` and `AttributeListSyntax` only. Both are independent of the declaration kind, so unions and extension indexers add no case there. The `switch` statements with a throwing default in Premium are over `Accessibility` (`ChangeVisibilityCodeAction.cs:142-178`, `CodeFixFactory.cs:103-112`), `ReferenceGranularity` (`ReferenceEnd.cs:91-108`), `DeclarationValidationTime` (`ValidationReferenceValidationQueryService.cs:16-23`) and a menu item kind (`CodeActionMenuBuilder.cs:62`); none of them is over `SyntaxKind`, and the only one over `DeclarationKind` is PR-11.
- Extension indexers as referenced members: `ReferenceValidatorQuerySource.cs:54-74` and `DynamicReferenceValidatorQuerySource.cs:51-67` map accessor validators to `DeclaringMember`, `ReferenceValidationContext.cs:127-128` maps `Indexer` to `Member`, `ReferenceEnd.cs:97-99` resolves the type through `GetClosestNamedType`, and the core walker enters indexer declarations wherever they appear (`ReferenceIndexWalker.cs:590-600`) and supports `Default`, `Assignment`, `InterfaceMemberImplementation` and `OverrideMember` for indexers (`ReferenceIndexerRequirements.cs:82-83`). Nothing in Premium depends on the indexer being declared in a class rather than in an extension block.
- `ExperimentalAttribute` (`ExperimentalAttribute.cs:37-56`), `InternalsCanOnlyBeUsedFrom` (`ArchitectureExtensions.cs:130-175`), `NamespacePredicate`, `AssemblyNamePredicate` and `TypeNamePredicate` reason about accessibility, namespaces and names only and are unaffected by `closed`, unions or extension indexers.
- The caching backends: after #85 `Metalama.Patterns.Caching.Backends.Azure.csproj:4` and `.Redis.csproj:4` target `net472;net10.0;netstandard2.0`; `Azure.Messaging.ServiceBus` 7.20.1, `Azure.Identity` 1.21.0 and `Microsoft.Extensions.Hosting.Abstractions` 8.0.1 have no .NET 11 constraint, and a `net11.0` consumer resolves the `net10.0` asset. The `net471` floor violation named in #1913 is fixed by #85.
- `Metalama.Licensing.BuildTasks` after #85 targets `net10.0;net472` (`Metalama.Licensing.BuildTasks.csproj:4`), conditions the `System.Threading.AccessControl` merge input on `net472` (`:44-46`), and `Metalama.Licensing.csproj:29-30` packs `tasks/net472` and `tasks/net10.0`. The `net10.0` task loads on the .NET 11 runtime of the .NET 11 SDK by roll-forward, the same mechanism `platform-support.md:338-339` relies on for the compiler toolset.
- The standalone tests: every `net8.0` project is `net10.0` on `pr85`; `Issue32827` keeps `netstandard2.0` and `netstandard2.1` with `LangVersion` 11.0, which is inside the `MinimumNETStandardVersion` 2.0 and `MaximumNETStandardVersion` 2.1 that core seeds (`Metalama.Framework.props:28-29`). No `test.json` names a target framework or an SDK version.
- `Metalama.Premium.LatestRoslyn.slnf` lists the unsuffixed projects only and is unaffected by the rename (unchanged on `pr85`). The `ProjectUsageInfo` regular expressions in `eng/src/Program.cs:86-90` are unanchored and still match the renamed `.5.0.0` projects.
- Premium has no `CLAUDE.md` (find over the repository). `README.md` gives the Docker build command only, the package `README.md` files make no platform claim (grep for Visual Studio, .NET 8, .NET 9, C# 1x, Roslyn 4), and `eng/style/README.md` is the generic code-style document. The core `extensibility.md:141` already describes Premium as built for Roslyn 5.0.0 and 5.10.0.
- `LangVersion` `preview` in the two unit test projects (`Metalama.Extensions.CodeFixes.UnitTests.csproj:10`, `Metalama.Extensions.Validation.UnitTests.csproj:10`) compiles the test assemblies with the preview language of the Metalama.Compiler 2027.0 toolchain; those projects set `UseMetalamaCompiler` to `Private`, so the `CSharpVersionNotSupported` check of the compile-time pipeline (FACTS) does not apply to them.
