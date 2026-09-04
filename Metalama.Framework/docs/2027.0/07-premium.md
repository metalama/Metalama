# 07. Metalama.Premium

This document covers the `Metalama.Premium` repository: the architecture, validation and code fix extension packages,
the licensing package and its MSBuild task, the caching backends, and the build infrastructure that produces them. It
records what the move to .NET 11, C# 15 and the next stable Roslyn requires of that repository, what the two merged
alignment pull requests already delivered, and what they left behind. The analysis reads the `Metalama.Premium` code
as it stands on `develop/2027.0` at commit 21521e0 and on the local branch `pr85`, which is the merged content of
metalama/Metalama.Premium#85, together with the `Metalama` code on branch
`topic/2027.0/26-09-03-update-eng-7e3j07`. Each finding was then re-checked by up to three verification passes: a
code pass that re-read the cited code and attempted to falsify the claim, a semantics pass that re-checked every
external premise against `dotnet/roslyn`, `dotnet/csharplang`, `dotnet/msbuild` and nuget.org, and a scope pass that
established whether the proposed change is already implemented, in flight, or tracked by an issue. The platform
baseline PB-2027.0 is decided by [`platform-support.md`](../platform-support.md), the permitted package versions by
[`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving to a new Roslyn by
[`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

File references without a repository prefix are relative to the root of the `Metalama.Premium` repository. References
prefixed with `Metalama/` are relative to the root of the core repository. No project was built and no test was run
for this analysis.

## Summary

1. The variant set and the target framework alignment are merged. metalama/Metalama.Premium#85 dropped the Roslyn
   4.12.0 variant, made 5.0.0 the suffixed lower variant, added 5.10.0 as the latest variant with its package source,
   and moved every target framework to `net10.0` (PR-15). What remains is the reverse edit. The latest variant is
   pinned to a prerelease Roslyn that has no stable counterpart, so leaving it is a renumbering to the expected
   stable 5.12, which touches 26 occurrences of the version string in 11 Premium files (PR-1).
2. The Roslyn 5.0.0 variant of the Premium engines is compiled by the solution build and packaged into the shipped
   NuGet packages, and no test executes it. That variant is the payload that serves Rider and the C# Dev Kit, so a
   defect confined to it reaches a user before it reaches the team (PR-2).
3. The build container move to Visual Studio 2026 is merged as metalama/Metalama.Premium#86, together with the
   retargeting of the engineering project to `net10.0` (PR-3, PR-4). Four container residuals remain: the obsolete
   .NET 8 software development kit, the obsolete .NET 6 runtime, the missing prerelease flag, and a stale Visual
   Studio 2022 channel manifest.
4. No test leg of either repository runs on the .NET 11 software development kit. The recorded core decision
   delegates that dimension to a separate matrix repository, so the action in `Metalama.Premium` is a request to that
   repository rather than a second software development kit in the Premium container (PR-8).
5. Two silent defects exist today and are independent of C# 15. The code fix that changes accessibility applies no
   change to an interface or an indexer while reporting success (PR-10), and the reference validation context throws
   for a validated extension block, which is reported as an error diagnostic raised inside the user validator
   (PR-11).
6. The C# 15 semantics require no product change in `Metalama.Premium`. Closed classes keep every architecture rule
   working, because a closed class is implicitly abstract and never sealed. Unions expose one silent false negative,
   and it is located in the core reference index walker rather than in Premium: the reference from a union
   declaration to its case types is never indexed (PR-12).
7. Six build file items remain, each small and none of them blocked by the Roslyn gate: the template language version
   and its Visual Studio 2022 rationale (PR-5), the residual `Microsoft.Build` pins and two dead entries (PR-6), the
   inert `StackExchange.Redis` update (PR-7), and the stale variant constant guidance in the unit test projects
   (PR-13).
8. No Premium source duplicates the core language version, Roslyn version or resource extraction logic. The
   duplication is confined to build files, and two of those duplication points are absent from the drift point list
   of [`platform-support.md`](../platform-support.md), which has no `Metalama.Premium` section at all (PR-9, PR-14).

## Findings

### PR-1. Stable Roslyn transition edits that remain after Premium#85

- Where:
  - `Directory.Packages.props:8-9` (the `RoslynVersion` and `RoslynMaxVersion` literals, both
    `5.10.0-1.26365.3`), `:29` (the central version of `Microsoft.CodeAnalysis.CSharp.Workspaces`) and `:36` (the
    `Metalama.Framework.Implementation.5.10.0` identifier)
  - `eng/RoslynVersions/Roslyn.5.10.0.props:3` (`ThisRoslynVersion` reads `$(RoslynApiMaxVersion)`), `:5`
    (`ThisRoslynVersionNoPreview`, the literal `5.10.0`) and `:9-10` (the `System.Text.Json` and
    `System.IO.Pipelines` pins at 10.0.11)
  - `eng/RoslynVersions/Latest.props:2` (the only consumer of `ThisRoslynVersion`)
  - `eng/Versions.props:15` and `Directory.Build.props:29` (the import of the generated `Versions.g.props`)
  - `nuget.base.config:14-16` (the package source mapping) and `eng/src/Program.cs:53`
    (`GenerateNuGetConfig = true`)
  - `Metalama/eng/src/Program.cs:149-152` (the exported properties) and `Metalama/Directory.Packages.props:28,30`
    (their definitions)
  - `Metalama/nuget.base.config:14-18` and `Metalama/eng/RoslynVersions/Roslyn.5.10.0.props:11-12`
  - `Metalama/Directory.Packages.props:70` (`SystemIOPipelinesLatestVersion`)
  - `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:60,85,142`
  - [`updating-roslyn.md`](../updating-roslyn.md):18-22 (the renumbering of the latest variant) and :38-54 (entering
    and leaving a prerelease Roslyn)
  - The 26 occurrences of the string `5.10.0` in 11 tracked Premium files on `pr85`, which include the
    `InternalsVisibleTo` entries of `src/Metalama.Extensions.CodeFixes/Metalama.Extensions.CodeFixes.csproj:17-20`
    and `src/Metalama.Extensions.Validation/Metalama.Extensions.Validation.csproj:16-17`, the
    `TfmSpecificPackageFile` paths of
    `src/Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj:53-62` and
    `src/Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj:46-51`, and the
    `MetalamaExtensionAssembly` entries with their `TargetRoslynVersion` attributes in
    `src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props:9-18`,
    `src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props:8-11`,
    `src/Metalama.Extensions.CodeFixes/MetalamaExtensionAssemblies.props:11-19` and
    `src/Metalama.Extensions.Validation/MetalamaExtensionAssemblies.props:12`
- What happens today: the core build exports `RoslynApiMaxVersion` and `RoslynMaxVersion`
  (`Metalama/eng/src/Program.cs:149-152`), and Premium imports the generated `Versions.g.props` through
  `eng/Versions.props:15`, which `Directory.Build.props:29` imports before `Directory.Packages.props` is evaluated.
  Of the two literals at `Directory.Packages.props:8-9`, only `RoslynMaxVersion` is a fallback for an exported
  property. The core repository defines no `RoslynVersion` property at all, so line 8 is the effective value in every
  build, whether or not `Build.ps1 prepare` has run, and it is the central version of
  `Microsoft.CodeAnalysis.CSharp.Workspaces`. The property `RoslynApiMaxVersion` itself has no fallback in Premium
  and is read only by `eng/RoslynVersions/Roslyn.5.10.0.props:3`; the resulting `ThisRoslynVersion` is consumed only
  by the import condition of `eng/RoslynVersions/Latest.props:2`, because every assembly name, package identifier and
  `InternalsVisibleTo` entry derives from `ThisRoslynVersionNoPreview`, a literal at
  `eng/RoslynVersions/Roslyn.5.10.0.props:5`. Premium generates its own `nuget.config` from its own
  `nuget.base.config` (`eng/src/Program.cs:53`), so the two feed declarations are independent inputs to two generated
  files. The Premium copy matches the core file as it stood when the roslyn-consolidated feed was restored, and it
  lacks the exact `Microsoft.CodeAnalysis` pattern that the core file later gained
  (`Metalama/nuget.base.config:14-18`); that omission is deliberate, because no Premium project references the
  `Microsoft.CodeAnalysis` metapackage. Finally, `eng/RoslynVersions/Roslyn.5.10.0.props:9-10` pins both
  `System.Text.Json` and `System.IO.Pipelines` to 10.0.11, whereas the core latest variant pins only
  `System.Text.Json` (`Metalama/eng/RoslynVersions/Roslyn.5.10.0.props:11-12`) and carries `System.IO.Pipelines` as a
  global runtime property with a different rationale (`Metalama/Directory.Packages.props:70`). The consumed Roslyn is
  a preview with no stable counterpart: nuget.org publishes 5.0.0, 5.3.0, 5.6.0 and 5.9.0 only, and `dotnet/roslyn`
  `main` is versioned 5.12.
- Consequence: a build or restore error, confined to the Premium build files. Because Premium generates its
  `nuget.config` from its own `nuget.base.config`, the removal of the roslyn-consolidated feed from the core
  repository does not by itself break a Premium restore. The failure that does occur is that Premium removes the feed
  while `Directory.Packages.props:8` still names the prerelease, and the restore of
  `Microsoft.CodeAnalysis.CSharp.Workspaces` then fails. If instead Premium keeps both the literal and the feed after
  the core repository moves to a stable Roslyn, the restore succeeds and Premium builds its latest variant against a
  preview compiler interface while the test projects reference the stable exported `RoslynMaxVersion`, which is a
  silent version split rather than an error.
- Proposed change: perform the mirror renumbering in the same release as the core renumbering. The stable target is
  expected to be Roslyn 5.12, so this is the renumbering of the latest variant described by
  [`updating-roslyn.md`](../updating-roslyn.md):18-22 and not the removal of a prerelease suffix in place. Rename
  `eng/RoslynVersions/Roslyn.5.10.0.props` to the new version, set `ThisRoslynVersionNoPreview` accordingly, point
  `eng/RoslynVersions/Latest.props:2` at the renamed file, and edit both literals at `Directory.Packages.props:8-9`,
  noting that `RoslynVersion` is overridden by nothing and must be edited while `RoslynMaxVersion` is only a
  fallback. Then renumber the remaining occurrences of the version string in the 11 files listed above, because the
  version is part of the identity of the variant assemblies and appears in package identifiers, `InternalsVisibleTo`
  entries, packaged file paths and the `TargetRoslynVersion` metadata that selects an extension assembly at run time.
  Re-derive `SystemTextJsonVersion` and `SystemIOPipelinesVersion` from the dependencies of the stable
  `Microsoft.CodeAnalysis.CSharp.Features` that the renumbered variant binds against, without assuming that the core
  variant file carries either value, and note that a nuspec declares a floor rather than the pin: the stable 5.9.0
  nuspec declares `System.Text.Json` 10.0.1 and `System.IO.Pipelines` 10.0.1, while the pinned value follows the
  stated policy of tracking the latest patch in the line. Decide the feed independently of the core repository,
  because the two `nuget.base.config` files are separate inputs to separate generated files. Record the dependence on
  the core procedure as a comment on the two literals, because nothing in Premium points at
  [`updating-roslyn.md`](../updating-roslyn.md).
- Size: medium.
- Status: new work. metalama/Metalama.Premium#85 created exactly the state described above rather than the transition
  away from it, and no issue covers the reverse edit. The related issues are #1881, which introduced the prerelease
  and the feed, #1885, which declared the package source, #1913, which produced the Premium literals, and the open
  umbrella #1921.
- Verification. Code: the literals, the feed declaration, the generated `nuget.config` and the 26 occurrences of the
  version string were re-read on `pr85`, and three details of the original statement were corrected, namely that
  `RoslynVersion` is not exported, that the core variant file sets no `System.IO.Pipelines` pin, and that the two
  feed declarations are independent. Semantics: nuget.org and `dotnet/roslyn` `main` were re-checked, and the premise
  that a stable 5.10 will be published was refuted, which converts the change from an edit of a label into a
  renumbering. Scope: no pull request and no issue implements or scopes the transition edits.
- Open questions: whether the generated `Versions.g.props` is produced on developer machines as well as in continuous
  integration could not be settled, because the file is ignored by version control. The answer does not change the
  finding, because the governing literal is not exported in either case.

### PR-2. The Roslyn 5.0 variant of the Premium engines is built but never executed by a test

- Where:
  - `Metalama.Premium.sln:65,67,69` (the three variant projects `Metalama.Extensions.CodeFixes.Engine.5.0.0`,
    `Metalama.Extensions.CodeFixes.DesignTime.5.0.0` and `Metalama.Extensions.Validation.Engine.5.0.0`)
  - `src/tests/Metalama.Extensions.Validation.AspectTests/Metalama.Extensions.Validation.AspectTests.csproj:7,42` and
    `src/tests/Metalama.Extensions.CodeFixes.AspectTests/Metalama.Extensions.CodeFixes.AspectTests.csproj:7,41` (the
    import of `Latest.props` and the hard-coded project reference to the unsuffixed engine)
  - `src/tests/Metalama.Extensions.CodeFixes.UnitTests/Metalama.Extensions.CodeFixes.UnitTests.csproj:27-28` and
    `src/tests/Metalama.Extensions.Validation.UnitTests/Metalama.Extensions.Validation.UnitTests.csproj:27-28` (the
    `VersionOverride` that names `RoslynMaxVersion`)
  - `src/Metalama.Extensions.Validation.Package.Resources/Metalama.Extensions.Validation.Package.Resources.csproj:26-27`
    (the project references that cause the variant to be compiled)
  - `eng/src/Program.cs:59-66` (the solution entry, which sets no test method override)
  - `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:3,59-61,91`
    (the core aspect test project, which parameterizes its engine reference and imports the software development kit
    conditionally)
  - `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests.5.0.0/Metalama.Framework.Tests.UnitTests.5.0.0.csproj:1,4-7`
    (the core shim) and
    `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Metalama.Framework.Tests.UnitTests.csproj:36-37`
    (the variant-conditional package override, which is in the shared project and not in the shim)
  - `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTestHelpers.5.0.0/Metalama.Framework.Tests.UnitTestHelpers.5.0.0.csproj:7-9`
    and
    `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTestHelpers/Metalama.Framework.Tests.UnitTestHelpers.csproj:12-13`
    (the helper package is published for the latest variant only)
  - `Metalama/eng/src/Program.cs:73-75` (the core solution is tested as a whole so that the lower variant is
    exercised) and [`Directory.Packages.md`](../../../Directory.Packages.md):197-204 (the Roslyn 5.0.0 variant is the
    payload that serves Rider 2026.2)
- What happens today: the solution declares three Roslyn 5.0.0 variant projects, and no test project carries a
  `.5.0.0` suffix. The two aspect test projects import `Latest.props` and reference the unsuffixed engine by a
  hard-coded path, so they resolve the latest variant. The two unit test projects override the Roslyn package version
  with `RoslynMaxVersion`, which is unconditional. The variant assemblies are nevertheless compiled, because the two
  `Package.Resources` projects reference them, and they are packaged and offered to a Roslyn 5.0 host through the
  `TargetRoslynVersion` metadata of the package build props. The core repository solves the same problem with a shim
  test project per lower variant and by testing the whole solution.
- Consequence: missing test coverage whose run-time effect is a silent design-time degradation. A defect that appears
  only when the Premium engines bind against `Microsoft.CodeAnalysis` 5.0.0.0 is not detected before a Rider user
  reports it. The gap is behavioural and not an interface gap, because the variant projects are compiled by the
  solution build, so a use of an interface member absent from Roslyn 5.0 fails the build. The same gap existed for
  the 4.12.0 variant, but that variant served a host that was also covered by manual Visual Studio 2022 testing,
  whereas the 5.0 variant serves no host that the team tests by hand.
- Proposed change: adding the two shim projects is necessary and not sufficient, because the sibling projects are not
  variant-aware. First make the two aspect test projects shimmable, as the core aspect test project is: replace the
  hard-coded project reference to the unsuffixed engine by one that appends `$(ThisRoslynVersionProjectSuffix)`,
  following
  `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:59-61`.
  Without that change a shim would compile the test sources under the Roslyn 5.0 property set and still load the
  latest engine, and the `MetalamaExtensionAssembly` item that names the engine assembly would point at a file absent
  from the output directory. Decide at the same time how the software development kit is imported, because the two
  Premium projects declare it on the project element, so either convert them to the conditional import form of the
  core project or write the shim in the plain project form of the core unit test shim. Then add the shims to
  `Metalama.Premium.sln`; `Build.ps1 test` runs them, because `eng/src/Program.cs:59-66` sets no test method
  override. The unit test half is a separate and larger change and is blocked on the core repository. Changing the
  `VersionOverride` to `ThisRoslynVersion` mirrors the shared core unit test project rather than the core shim, and
  it does not by itself produce a Roslyn 5.0 unit test run: both Premium unit test projects reference the
  `Metalama.Framework.Tests.UnitTestHelpers` package, of which the core repository publishes only the latest variant,
  so a Premium unit test shim would bind the latest helper and pull the latest implementation package alongside the
  Roslyn 5.0 engine. Such a shim would also need its own assembly name and a matching `InternalsVisibleTo` entry.
  Either restrict the change to the aspect tests, or first publish a Roslyn 5.0 variant of the helper package from
  the core repository.
- Size: medium for the two aspect test shims, once the sibling projects are made variant-aware. Large for the unit
  test shims, and blocked on a core change that publishes a Roslyn 5.0 variant of
  `Metalama.Framework.Tests.UnitTestHelpers`.
- Status: new work. Two searches of the issue tracker returned no issue that scopes it. The related issues are #1913,
  whose scope covers the variant projects and the target frameworks only and which therefore produced this gap, #1881,
  whose acceptance criteria state that both repositories must pass their tests against both Roslyn variants, and
  #1217, which is the other place where an extension package is not covered by the multi-variant pattern.
- Verification. Code: every cited line was re-read, three attempted refutations failed, and three details of the
  proposed change were corrected, namely that the Premium aspect test projects hard-code the engine reference, that
  the package version override lives in the shared core project and not in the shim, and that the unit test half is
  blocked on the helper package. Semantics: not run, because the finding asserts nothing about C# 15, the Roslyn
  public interface or the release timeline. Scope: no pull request and no issue implements or scopes the change.

### PR-3. The build container definition was stale, and four residuals remain

- Where:
  - `eng/src/Program.cs:24-55` before the merge of metalama/Metalama.Premium#86, and the .NET 8 software development
    kit and .NET 6 runtime components that survive it
  - The generated files `eng/docker/build.Dockerfile:43-56`, `eng/docker/vs17.Dockerfile:33-36`,
    `eng/docker-context/VisualStudio.17.14.15.Release.chman`,
    `eng/docker-context/vs17/VisualStudio.17.14.15.Release.chman` and `DockerBuild.ps1:1-3`
  - `Directory.Packages.props:16` (`PostSharpEngineeringVersion`) and `Metalama/Directory.Packages.props:12`
  - `Metalama/eng/src/Program.cs:26,37,41,47-54,61,63` (the core container end state),
    `Metalama/eng/docker/vs18.Dockerfile:36` and `Metalama/eng/docker-context/vs18/VisualStudio.18.9.2.Release.chman`
  - `eng/src/Program.cs:70-72` (the three solutions built with the desktop MSBuild) and
    [`platform-support.md`](../platform-support.md):124-134 (Visual Studio 2022 is outside PB-2027.0)
  - `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:37`
    (`MinimumVisualStudioVersion` 18.0),
    `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:391-418`
    (`MetalamaCheckSupportedToolchain` and the `LAMA0602` warning) and
    `Metalama/Metalama.Framework/src/Metalama.Framework.Package/buildTransitive/Metalama.Framework.props:2`
  - `src/Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj:37`,
    `src/Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj:35` and
    `src/Metalama.Patterns.Caching.Backends.Redis/Metalama.Patterns.Caching.Backends.Redis.csproj:24` (the three
    package chains that carry the requirement into the standalone solutions)
  - `src/tests/Standalone/Validation/test.json:3` and `src/tests/Standalone/CachingBackends/test.json:2`
    (`FailOnUnexpectedDiagnostics`)
  - `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Utilities/DotNetTool.cs:61` and
    `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Utilities/MSBuildTool.cs:53-56` (the
    `MSBuildExtensionsPath` mitigation, which is a separate subject)
- What happens today: before the merge of metalama/Metalama.Premium#86, the container installed the .NET 10 software
  development kit, the .NET 8 software development kit, the .NET 6 runtime, the .NET 9 runtime and the Visual Studio
  2022 Build Tools 17.14.15, and `MSBuildVersion` was 17.14. The three `MsbuildSolution` entries at
  `eng/src/Program.cs:70-72` build the code fixes, validation and caching backend standalone solutions with the
  desktop MSBuild of that installation, which PB-2027.0 excludes. The core package targets seed
  `MinimumVisualStudioVersion` 18.0 and report `LAMA0602` when the MSBuild runtime type is `Full` and the MSBuild
  version is below it, and that requirement reaches all three standalone projects, because the transitive props file
  imports the build props file and none of the three package chains marks `Metalama.Framework` private. No file in the
  Premium repository sets `MetalamaCheckSupportedPlatform` to false or lists `LAMA0602` in `NoWarn`.
  metalama/Metalama.Premium#86 has since raised `PostSharpEngineeringVersion` to 2023.2.423, moved the component to
  Visual Studio Build Tools 18.9.2, set `MSBuildVersion` to 18.9, removed the .NET 9 runtime, taken the software
  development kit version from a single variable, and regenerated the container files, renaming the Visual Studio
  dockerfile and adding the 18.9.2 channel manifest.
- Consequence: a diagnostic reported, namely `LAMA0602` in the three standalone builds driven by the desktop MSBuild,
  and the licensing task exercised under an MSBuild version that no supported host ships. The warning path is
  reachable and unsuppressed. Whether the standalone harness fails on that warning depends on
  `PostSharp.Engineering`, which is not present in this environment. Because the merged pull request moved the image
  to Build Tools 18.9.2, the desktop MSBuild of the image is now 18.9 and the warning should no longer be produced;
  that outcome is not verified here, because this analysis runs no build.
- Proposed change: complete the four residuals. Remove the .NET 8 software development kit and the .NET 6 runtime
  components, because metalama/Metalama.Premium#85 replaced every `net8.0` target framework and no file names
  `net6.0`. Decide whether `DotNetSdkVersion` should carry the prerelease flag that the core repository sets
  (`Metalama/eng/src/Program.cs:61`). Delete the stale `eng/docker-context/VisualStudio.17.14.15.Release.chman`,
  which the generator leaves behind and which the core repository no longer has. Decide whether the two .NET
  Framework 4.7.2 components should be removed to match the core repository: the merged core state keeps only the
  MSBuild component and the .NET Core software development kit component, on the ground that the software
  development kit obtains the reference assemblies from the `Microsoft.NETFramework.ReferenceAssemblies` packages,
  which supersedes the earlier proposal to add the 4.8 pair. Keep `Microsoft.NetCore.Component.SDK`, without which
  the desktop MSBuild of the Build Tools has no software development kit directory and fails to resolve the .NET
  software development kit. Regenerate the container files with `Build.ps1 generate-scripts` and delete by hand
  whatever the generator leaves behind, as the equivalent core commit had to do. Read the `MSBuildExtensionsPath`
  note in the core `CLAUDE.md` before touching the image, because a stale value of that variable, and not the
  presence of a second software development kit feature band, is what caused the earlier core failures.
- Size: medium, and each mistake costs a continuous integration cycle.
- Status: already in progress, and mostly delivered. metalama/Metalama.Premium#86, "Update engineering and move the
  build image to Visual Studio 2026", was merged into `develop/2027.0` on 2026-09-03, and its core counterpart is
  metalama/Metalama#1919, merged the same day. The residuals named above are not implemented and not tracked. The
  related issues are #1902, which was closed as not planned before the decision was reversed for PB-2027.0 and which
  records that `Metalama.Consolidated` is still pinned to the Visual Studio 2022 component, #1913, and the open
  umbrella #1921.
- Verification. Code: the staleness was confirmed line by line against both repositories, the reachability of
  `LAMA0602` through all three package chains was confirmed, and three details were corrected, namely the attribution
  of the container change to a commit that touches no container file, the omission of two of the five core commits,
  and the unstated coupling to the retargeting of the engineering project. Semantics: not run, because the finding
  asserts nothing about C# 15, the Roslyn public interface or the release timeline. Scope: metalama/Metalama.Premium#86
  implements the body of the change, and four residuals remain untracked.
- Open questions: whether `PostSharp.Engineering` 2023.2.423 removes `MSBuildExtensionsPath` in its own blocked
  environment variable list, which is the list that governs the solution restore driven by the engineering project.
  That repository is not present in this environment.

### PR-4. The engineering tool targeted `net9.0`

- Where: `eng/src/BuildMetalamaPremium.csproj:5` before the merge of metalama/Metalama.Premium#86,
  `eng/src/Program.cs:32-33` (the .NET 9 runtime component), `Build.ps1:25,99,157,193` (the tool is executed as a
  framework-dependent application), `Metalama/eng/src/BuildMetalama.csproj:6,11-13` and
  [`platform-support.md`](../platform-support.md):195-197.
- What happens today: the engineering project targeted `net9.0` and `Build.ps1` executes it through the .NET host, so
  the container had to install the .NET 9 shared runtime. Issue #1913 counted this single occurrence and
  metalama/Metalama.Premium#85 did not change it. metalama/Metalama.Premium#86 has since retargeted the project to
  `net10.0`, added a suppression of the two vulnerability warnings, and removed the .NET 9 runtime component.
- Consequence: no impact on the shipped packages. The concern is build infrastructure only, and the failure mode of
  the previous state was that a build agent had to keep an out-of-support runtime installed.
- Proposed change: none remains. Should the change ever need to be reproduced, retargeting alone is not sufficient:
  the `net10.0` assets of `PostSharp.Engineering.BuildTools` depend on a version of `Microsoft.Build` with a known
  vulnerability, so the project must also suppress `NU1903` and `NU1904`, as the core project does at
  `Metalama/eng/src/BuildMetalama.csproj:11-13`. Without that suppression the build fails, because the continuous
  integration build treats warnings as errors.
- Size: small.
- Status: already in progress, and complete. metalama/Metalama.Premium#86 and metalama/Metalama#1919 both merged on
  2026-09-03 and implement both halves. The related issues are #1913, #1876, #1902 and the open umbrella #1921. This
  finding overlaps PR-3 and should be delivered with it or dropped.
- Verification. Code: the local state, the execution path through `Build.ps1` and the absence of any coupling to a
  packable project were confirmed, and the finding was corrected to record that the change has landed upstream and
  that the vulnerability suppression is part of it. Semantics: not run. Scope: the change is merged in both
  repositories and nothing remains open.

### PR-5. `MetalamaTemplateLanguageVersion` is 13.0 with a Visual Studio 2022 rationale

- Where: `Directory.Build.props:18-20`, `Metalama/Directory.Build.props:11-16`,
  `eng/RoslynVersions/Roslyn.5.0.0.props:3` and
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:57-58`.
- What happens today: the compile-time code of `Metalama.Extensions.Architecture`,
  `Metalama.Extensions.Validation` and `Metalama.Extensions.CodeFixes` is parsed as C# 13 by the Metalama pipeline,
  because the property reaches the engine through the MSBuild project options, is accepted because C# 13 is a member
  of the supported set, and becomes the language version of every compile-time syntax tree. The comment names Visual
  Studio 2022 as the reason, which PB-2027.0 no longer supports. The value 13.0 was nevertheless correct while the
  repository still shipped a Roslyn 4.12.0 variant, because that Roslyn version maps to C# 13. What is stale is the
  rationale rather than the number. The core repository is at 14.0 with a comment that derives the value from the
  Roslyn floor.
- Consequence: no impact. The Premium packages contain no template method, only validator aspects, so the property
  affects nothing beyond the parse options of the compile-time compilation of Premium itself, and C# 13 is within the
  supported set, so no diagnostic is reported. Raising the value to 14.0 changes one thing that should be expected
  rather than investigated: the compile-time compilation defines the embedded system types symbol at C# 14 and above,
  so the Premium projects would embed the same set of predefined types that the core repository already embeds.
- Proposed change: set the value to 14.0 and rewrite the comment to name the Roslyn floor instead of a Visual Studio
  version. Do not copy the core comment verbatim: it points at `RoslynApiMinVersion` in `Directory.Packages.props`,
  and the Premium repository defines no such property. The Premium comment must instead name the lowest Roslyn
  variant declared under `eng/RoslynVersions/`, which is 5.0.0, and state that Roslyn 5.0.0 supports C# 14, so that
  the two repositories move together on the next floor change. Check the standalone scenario `Issue32827` before
  merging, because a standalone scenario that references an older released Metalama can reject a raised value with
  `LAMA0052`, as one core scenario did.
- Size: small.
- Status: new work, and the stated blocker is lifted. The core half was delivered by #1896, whose final comment
  deliberately deferred the Premium pin and suggested that a separate issue might be worth opening; that issue was
  never opened. The blocker named by #1896 was the Roslyn 4.12 variant, which #1913 removed through
  metalama/Metalama.Premium#85. The related issues are #1896, #1913, #1881 and the open umbrella #1921.
- Verification. Code: the property, its single occurrence in the repository, its route into the engine and the
  absence of any template method in the Premium packages were confirmed, and four details were corrected, namely the
  incomplete project list, the core line numbers, the dependency on the removal of the 4.12.0 variant, and the fact
  that the core comment cannot be copied literally. Semantics: not run. Scope: nothing implements or tracks the
  change, and the blocker was lifted by a merged pull request.

### PR-6. Residual pins and comments in `Directory.Packages.props` and the test props

- Where: `Directory.Packages.props:5-6` (the `RestorePrerelease` comment), `:69-71` (`Microsoft.Build.Framework`,
  `Microsoft.Build.Core` and `Microsoft.Build.Tasks.Core` at 17.10.46), `:75` (`Microsoft.NET.Test.Sdk` 17.14.1),
  `src/tests/Directory.Build.props:17` (`MicrosoftBclAsyncInterfacesVersion`), `src/tests/Directory.Packages.props:6`,
  `Directory.Packages.props:11`, `THIRD-PARTY-NOTICES.md:38`,
  `src/Metalama.Licensing.BuildTasks/Metalama.Licensing.BuildTasks.csproj:4,23-24`,
  `Metalama/Directory.Packages.props:6-7`, `:35-50` and `:168`.
- What happens today: the core repository derives its MSBuild pin, 18.0.2, from the lowest MSBuild host of the
  baseline, with the doctrine recorded beside it. Premium keeps 17.10.46 for three entries, of which only two are
  referenced by a project: `Metalama.Licensing.BuildTasks` references `Microsoft.Build.Framework` and
  `Microsoft.Build.Tasks.Core` with the runtime assets excluded and the assets marked private, so the pin is a
  compile-time floor only and the task binds to the MSBuild of the host at run time. `Microsoft.Build.Core` is
  referenced by no project and no package of that identifier exists on nuget.org, so that entry is dead rather than
  merely stale. The property `MicrosoftBclAsyncInterfacesVersion` is read by nothing, because the package version
  uses the corresponding latest-version property. `THIRD-PARTY-NOTICES.md:38` lists a `StackExchange.Redis` version
  that matches neither of the two entries in the props file, and several other rows of that file are equally stale.
  `Microsoft.NET.Test.Sdk` 17.14.1 is the same value as the core repository, whose comment still names Visual Studio
  2022 as the reason. The `RestorePrerelease` comment says that the property is required while the .NET software
  development kit 10 is a prerelease, which is no longer true, and the identical comment is also present in the core
  repository.
- Consequence: no impact. The MSBuild pin cannot cause a run-time mismatch, because the two referenced packages are
  consumed with the runtime assets excluded, and the vulnerability diagnostics that a package of that age would
  otherwise raise are suppressed on the same lines.
- Proposed change: raise `Microsoft.Build.Framework` and `Microsoft.Build.Tasks.Core` to the core
  `MicrosoftBuildVersion` property and cite the doctrine comment of `Metalama/Directory.Packages.props:35-49`. Delete
  the `Microsoft.Build.Core` entry rather than aligning it. Sequence the raise after the move of
  `Metalama.Licensing.BuildTasks` to `net10.0`, which metalama/Metalama.Premium#85 performed, because
  `Microsoft.Build.Tasks.Core` 18.0.2 has no `net8.0` compile asset and a `net8.0` target would silently fall back to
  the narrower `netstandard2.0` reference assembly. Note also that the core doctrine requires a consumer of
  `Microsoft.Build` to reference `Microsoft.NET.StringTools` with the runtime assets excluded, and the Premium project
  references neither, so the doctrine citation is not a mechanical copy. Delete the dead property at
  `src/tests/Directory.Build.props:17`, regenerate the bill of materials, and reword the `RestorePrerelease` comment
  in both repositories. State in that rewording which outcome is assumed, because PR-1 may make the property
  unnecessary rather than merely misdescribed.
- Size: small.
- Status: new work. Two searches of the issue tracker returned no issue that proposes any part of it. The related
  issues are #1913, which is the change that last edited this file and whose scope deliberately excluded these
  entries, #1897, which established the rule that a Premium pin follows the core pin and cites the core doctrine,
  #1903, which re-derives the .NET 8.0 line pins in the core repository, #1876, which made the Visual Studio
  rationales inaccurate, and #1881, which is the reason `RestorePrerelease` is still required.
- Verification. Code: every entry was re-read, the dead entries and the dead property were confirmed, and four
  details were corrected, namely two line citations, the treatment of `Microsoft.Build.Core` as alignable rather than
  dead, the answer to the open question, and the duplication of the stale comment in the core repository. Semantics:
  not run. Scope: nothing implements or tracks the change, and it overlaps PR-7 in the same file.
- Open questions: the original question, whether `Microsoft.Build.Tasks.Core` 18.0.2 still ships a `net472` asset, is
  answered. Both packages ship `net472` reference and library assets, so the `net472` build of the task is
  unaffected. The genuine constraint is the absence of a `net8.0` asset, which the sequencing above addresses.

### PR-7. The `StackExchange.Redis` version update is inert

- Where: `Directory.Packages.props:76` (`<PackageVersion Update="StackExchange.Redis" Version="2.10.14" />`) and
  `:99` (`<PackageVersion Include="StackExchange.Redis" Version="2.9.32" />`); the two lines are 75 and 98 on
  `pr85`, which does not change them. The two consumers are
  `src/Metalama.Patterns.Caching.Backends.Redis/Metalama.Patterns.Caching.Backends.Redis.csproj:22` and
  `src/tests/Metalama.Patterns.Caching.LoadTests/Metalama.Patterns.Caching.LoadTests.csproj:17`.
- What happens today: both items are inside the same item group, which opens at `Directory.Packages.props:27` and
  closes at `:105`, so they are evaluated in document order. An MSBuild item update applies only to items that exist
  at that point of the evaluation, and the update precedes the include, so the update creates nothing and the include
  then defines the version as 2.9.32. The effective central version is therefore 2.9.32, and the 2.10.14 line changes
  nothing. Both consumers reference the package with no version and no override, and no other file in either
  repository defines a version for it. Restore is silent, because the duplicate-item diagnostic applies to two
  include items of the same identifier and not to an include paired with an update. The sibling entry for
  `Microsoft.Extensions.Logging.Abstractions` is placed after its include and therefore does apply, which confirms
  that the ordering and not the use of an update is what makes this line inert.
- Consequence: no impact on .NET 11. The Redis backend ships against an older client than the file intends.
- Proposed change: resolve the contradictory pair, and state in the change which of the two outcomes is intended.
  Replacing the pair with one include at 2.10.14 raises the effective version and is a behaviour change for the
  shipped `Metalama.Patterns.Caching.Backends.Redis` package; deleting the inert update and keeping 2.9.32 changes
  nothing that restores today. If the version is raised, review the new transitive dependency: the nuspec of 2.10.14
  adds a dependency on `System.IO.Hashing` to its .NET Framework and .NET Standard groups, which 2.9.32 does not
  have, and `Directory.Packages.props:90-95` pins the out-of-band system package family for the .NET Framework
  payload. Confirm the resulting version in the restored assets file.
- Size: small.
- Status: new work. Two searches of the issue tracker returned nothing relevant, and neither Premium pull request
  changes either line. The related issues are #1913, which is the issue under which the file was last edited, #1903,
  which establishes the method of re-deriving a pin and recording the reason, and the open umbrella #1921.
- Verification. Code: the item group boundaries, the evaluation order, the two consumers and the absence of any other
  definition were confirmed, three attempted refutations failed, and the finding was confirmed without correction.
  Semantics: not run. Scope: nothing implements or tracks the change, and it overlaps PR-6 in the same file. The
  original confidence note said that no restore was run; that remains true, because this analysis runs no build.

### PR-8. No test leg exercises the .NET 11 software development kit

- Where:
  - `src/tests/Metalama.Extensions.CodeFixes.AspectTests/Metalama.Extensions.CodeFixes.AspectTests.csproj:11`,
    `src/tests/Metalama.Extensions.Validation.AspectTests/Metalama.Extensions.Validation.AspectTests.csproj:11`,
    `src/tests/Metalama.Extensions.CodeFixes.UnitTests/Metalama.Extensions.CodeFixes.UnitTests.csproj:6` and
    `src/tests/Metalama.Extensions.Validation.UnitTests/Metalama.Extensions.Validation.UnitTests.csproj:6`
  - `src/tests/Standalone/Issue32827/Test/Test.csproj:4` (the one standalone project that is not `net10.0`)
  - `eng/src/Program.cs:24-33,54,67,70-72` (the container components, the software development kit version and the
    two kinds of solution entry)
  - `src/Metalama.Licensing/build/Metalama.Licensing.targets:12-13` and
    `src/Metalama.Licensing.BuildTasks/Metalama.Licensing.BuildTasks.csproj:4,38-48`
  - `Metalama/eng/src/Program.cs:26,37,61`,
    `Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14`
  - `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:32-33`
    (`MinimumSdkVersion` 10.0 and `MaximumSdkVersion` 11.0)
  - `Metalama/Metalama.Framework/src/tests/Standalone/SupportedPlatform.UntestedTargetFramework/README.md:34-35` and
    `Metalama/Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8-10`
  - [`platform-support.md`](../platform-support.md):195-197, :274 and :338-339
- What happens today: the Premium test projects target the .NET Framework 4.8 and `net10.0`. Every standalone
  scenario targets `net10.0`, except `Issue32827`, whose two projects target `netstandard2.0` and `netstandard2.1`.
  The container carries the .NET 10 and .NET 8 software development kits only, so no leg runs on the .NET 11
  software development kit. The core repository is in the same position, with one software development kit installed.
  PB-2027.0 puts the .NET 11 software development kit in the supported set, and the core package props declare a
  maximum of 11.0, so the platform check reports nothing on it. The only Premium asset that a .NET software
  development kit selects is the `net10.0` licensing task, chosen when the MSBuild runtime type is `Core`, together
  with the assemblies merged into it. Only the entry at `eng/src/Program.cs:67` reaches that asset, because it runs
  the .NET command line build; the three entries at `:70-72` run the desktop MSBuild, whose runtime type is `Full`,
  and therefore load the .NET Framework asset. Nothing loads the `net10.0` task into the MSBuild of the .NET 11
  software development kit before a user does. That software development kit is not yet generally available: only a
  preview exists today, and it carries a compiler toolset from the same Roslyn window that Metalama consumes, so its
  default language version is C# 14.
- Consequence: no impact today. The risk originally stated, an assembly binding failure of the merged `net10.0` task
  on the .NET 11 runtime, is unlikely, because the MSBuild task load context applies no target framework check,
  unifies the MSBuild assemblies to the default load context, and rejects only candidates whose version is below the
  requested one. The repository already relies on the same tolerance for a sibling asset
  ([`platform-support.md`](../platform-support.md):338-339), and it already loads a lower target framework asset into
  a higher runtime in its own build. What the missing leg leaves untested is the .NET 11 host runtime, the `net11.0`
  target framework, and, once the stable software development kit ships with the expected Roslyn 5.12, the command
  line compiler whose default language version is C# 15. A defect in any of those would surface as a user report.
- Proposed change: follow the recorded core decision, which does not place this leg in the product repositories. The
  core repository states that the software development kit dimension is covered only through a contributed
  requirement, because the build agent has one software development kit, and that varying the software development
  kit belongs to the matrix of `metalama/Metalama.Tests.DotNetSdk`; it also excludes `net11.0` from its own scenario
  matrix because the agents have no targeting pack for it. The action in `Metalama.Premium` is therefore to ask that
  matrix repository to add a .NET 11 software development kit case that consumes `Metalama.Extensions.CodeFixes` or
  `Metalama.Extensions.Validation`, so that the licensing task is loaded under the `Core` runtime type on .NET 11. If
  a leg is nevertheless added inside Premium, only the entry at `eng/src/Program.cs:67` is relevant, because the
  three entries at `:70-72` exercise the .NET Framework asset and cannot cover this path. Justify the leg by the
  `net11.0` target framework and the .NET 11 host runtime rather than by the licensing task alone, and schedule it:
  a leg added before the general availability date can install only the preview, whose compiler defaults to C# 14, so
  it proves nothing about C# 15. The container component is not a blocker, because the software development kit
  component takes a version string, as the core repository already demonstrates, and the prerelease flag is set
  there.
- Size: small in this repository, because the software development kit matrix belongs to
  `metalama/Metalama.Tests.DotNetSdk`. Medium only if a second software development kit is added to the Premium
  container.
- Status: new work. No pull request adds a `net11.0` target framework or a .NET 11 software development kit
  component in either repository, and no issue proposes one. The related issues are the open umbrella #1921, #1884,
  which admits `net11.0` into the declared matrix while excluding it from the test matrix for want of targeting
  packs, #1913, which established `net10.0` as the only `Core` flavour of the Premium payload, and #1876, which left
  the container with a single software development kit.
- Verification. Code: the target frameworks, the container components, the asset selection and the two kinds of
  solution entry were re-read, and four items were corrected, namely the claim that every standalone project targets
  `net10.0`, the claim that the desktop MSBuild entries can exercise the `net10.0` task, the assumption that the core
  decision is still open, and the open question about the component catalogue. Semantics: the .NET 11 release
  timeline and the compiler toolset of the preview were checked against `dotnet/core` and `dotnet/sdk`, and the
  consequence class was weakened after reading the MSBuild task load context. Scope: nothing implements or tracks the
  change, and the core counterpart of this finding is LV-9, so a single story should carry both repositories.
- Open questions: whether `metalama/Metalama.Tests.DotNetSdk` already covers the Premium packages could not be
  established, because that repository is not present in this environment.

### PR-9. `Metalama.Licensing.targets` selects the task directory without a runtime guard

- Where: `src/Metalama.Licensing/build/Metalama.Licensing.targets:11-18` (line 11 defines an unused runtime version
  property, lines 12 and 13 select the directory, line 14 composes the path and line 18 declares the task) and
  `:33-38` (the target that runs the task), `src/Metalama.Licensing/buildTransitive/Metalama.Licensing.targets:3`
  (the import, so there is a single selection site), `src/Metalama.Licensing/Metalama.Licensing.csproj:29-30` (the
  two packed directories), `src/Metalama.Licensing.BuildTasks/Metalama.Licensing.BuildTasks.csproj:4` (the two target
  frameworks), `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:32` and
  `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:391-392,405`, and
  [`platform-support.md`](../platform-support.md):338-339 and :343-346.
- What happens today: the tasks directory is chosen from the MSBuild runtime type alone. The file computes a runtime
  version on line 11 and never reads it, so a guard appears to have been intended and left unfinished. Three sites
  carry the target framework name and must move together: this selection, the two packed directories, and the target
  frameworks of the task project. On the .NET 11 software development kit the `net10.0` assembly loads, because the
  task assembly is loaded into an MSBuild process that already runs and its framework references bind against the
  higher versions present in that process. That is not the roll-forward policy described at
  [`platform-support.md`](../platform-support.md):338-339, which is a setting of an application host and applies to
  the compiler executable, although the outcome is the same. The repository already demonstrates the behaviour,
  because before the merged alignment the tasks directory named `net8.0` while the build was pinned to the .NET 10
  software development kit. On an MSBuild below .NET 10 the task fails to load and MSBuild reports a raw `MSB4062`.
  The core document records the identical construct for `Metalama.Compiler.Sdk.props`, and the Premium copy is not
  listed; the document mentions neither `Metalama.Premium` nor `Metalama.Licensing` anywhere.
- Consequence: a build error, only outside the baseline. Restore is unaffected, because MSBuild resolves the task
  declaration only when the task runs, and the target that runs it is incremental, so an up-to-date build skips it
  and reports nothing. Below the floor the core targets first report `LAMA0601` against the minimum software
  development kit version. That diagnostic is a warning rather than an error, and its target is anchored before the
  configuration and platform check rather than before the compilation, which still places it earlier in the build.
  The user therefore sees an actionable warning and then the raw `MSB4062`, because the warning does not stop the
  build. `Metalama.Framework` reaches the consumer on every Premium path, including the caching backends, which
  acquire it through `Metalama.Patterns.Contracts`.
- Proposed change: none in code for 2027.0. In documentation, the Premium selection cannot be appended to the
  existing list, because [`platform-support.md`](../platform-support.md):343 introduces that list as two places in
  `Metalama.Compiler`, under a heading scoped to that repository, and the document has no `Metalama.Premium` section.
  Add a section "What this means in Metalama.Premium" that records the three sites which carry the target framework
  name, states that the selection has no version guard and no equivalent of the diagnostic that `Metalama.Compiler`
  reports, and notes that `Metalama.Licensing` contributes no platform requirement of its own. While editing the
  document, correct the justification at :338-339, which explains the MSBuild tasks with the roll-forward policy of
  the compiler executable; the two are different mechanisms and only the compiler uses roll-forward.
- Size: small.
- Status: new work. The value half is merged: metalama/Metalama.Premium#85 changed the selected directory to
  `net10.0` and nothing else about this file. Neither the runtime guard nor the documentation is implemented or
  scoped anywhere. The related issues are #1913, whose scope and acceptance criteria say nothing about a host runtime
  guard or about the documentation, #1876, which is the floor move that this file mirrors, #1894, which is the
  nearest existing mechanism for reporting an unsupported host, #1884, which establishes the precedent of a
  diagnostic instead of a raw failure, and #1898, which applies the same policy on the Roslyn axis.
- Verification. Code: the selection, the unused property, the single import site, the two companion sites and the
  absence of any guard in the repository were confirmed, and four details were corrected, namely the mechanism, the
  claim that restore is affected, the severity and anchor of `LAMA0601`, and the target of the documentation edit.
  Semantics: the .NET 11 MSBuild, the task load context and the meaning of `MSB4062` were checked against
  `dotnet/msbuild`, which confirmed the outcome and refuted the stated mechanism. Scope: nothing implements or tracks
  either half, and this finding lands on the same documentation edit as PR-14.

### PR-10. `ChangeVisibilityCodeAction` silently skips interface, indexer and union declarations

- Where: `src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs:46-48` (the
  unconditional tree update), `:52` (the base class), `:72-116` (the fifteen per-kind overrides) and `:127` (the
  guard that restricts the change to the declaring syntax references of the target symbol);
  `src/Metalama.Extensions.CodeFixes/CodeFixFactory.cs:101-117` (the public entry point, which accepts any member or
  named type); `src/Metalama.Extensions.CodeFixes.Engine/CodeActionBuilder.cs:44-45` (no validation of the
  declaration kind); `src/Metalama.Extensions.CodeFixes.Engine/CodeActionContext.cs:53-57` and `:82-93` (the file is
  recorded as changed without comparing the new root to the old one);
  `Metalama/Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:44` (the visit method
  is sealed) and `:64-67`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/DesignTime/CodeFixes/CodeActionResult.cs:51-77` (the
  unchanged root is applied and success is reported);
  `Metalama/eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954-1978` (the union node, whose base is the type
  declaration node) and `:2034` (the enum node, whose base is the base type declaration node);
  `eng/RoslynVersions/Roslyn.5.0.0.props:8` and `Metalama/eng/RoslynVersions/Roslyn.5.10.0.props:10`.
- What happens today: the rewriter declares fifteen per-kind overrides and has none for an interface declaration or
  an indexer declaration. The public factory method accepts any member or named type, and nothing between it and the
  rewriter validates the declaration kind. An unhandled node is dispatched to the base rewriter, which copies it and
  visits its children, so the modifier list is never passed to the modifier helper. The code action then updates the
  tree unconditionally, the context records the file as changed without comparing the roots, and the core code action
  result applies the unchanged root and reports success. No diagnostic, exception or assertion is produced. No test
  pins the behaviour: the existing accessibility tests cover a method, a partial declaration and the private
  protected combination only. The interface and indexer cases exist today and are independent of C# 15. C# 15 adds a
  third case, the union declaration, whose node derives from the type declaration node and which accepts accessibility
  modifiers. That case is not reachable on the dependencies consumed today, because the node is marked experimental
  in the consumed Roslyn and unions are gated on the preview language version there; it becomes reachable with the
  move to the stable Roslyn 5.12.
- Consequence: silent wrong output. The user invokes the code fix, sees it succeed and observes no change.
- Proposed change: replace the per-kind overrides for the class, record and struct declarations by one override of
  the core extension point, which is `VisitCore` and not `Visit`, because the visit method is sealed and the base
  class directs derived classes to `VisitCore`. The override should call the base implementation and then, when the
  original node is one of the target declarations and the result is a base type declaration, apply the modifier
  update. Write the generalization against the base type declaration node rather than the type declaration node,
  because the enum node derives from the former and not from the latter, so the enum override can then be removed as
  well. Both modifier update members are shipped public interface members in the Roslyn 5.0 and latest variants, so
  the generalization compiles in both and covers interfaces, extension blocks and unions without naming the union
  node. Do not claim that it covers extension blocks in a meaningful sense: the extension declaration grammar has no
  modifier production, and the rewriter changes modifiers only on the declaring syntax references of the target
  symbol. Add an override for the indexer declaration, because that node derives from the base property declaration
  node and is not reached by the type declaration generalization; it also covers the extension indexers that C# 15
  introduces, because they reuse the same node. Add the aspect tests `ChangeVisibility_Interface` and
  `ChangeVisibility_Indexer`, which need no new language version. A union test is not writable against the currently
  consumed Roslyn at all, because the C# 15 language version does not exist there and the C# 15 features are
  reachable only under the preview language version. It requires the move to the stable Roslyn 5.12, at which point
  it needs a required-constant directive, and `Metalama.Premium` must define the latest-variant constant in its
  variant props file as the core repository does; Premium defines no such constant today.
- Size: small.
- Status: new work. Neither Premium pull request touches the file or adds a test, and two searches of the issue
  tracker returned no issue on the subject, so there are no related issues to cite.
- Verification. Code: the overrides, the entry point, the default dispatch, the unconditional success and the absence
  of any test were confirmed, and four details of the proposed change were corrected, namely the sealed visit method,
  the enum base type, the shipped status of the two modifier update members, and the Roslyn version numbering.
  Semantics: the union grammar, the accessibility of a union declaration, the extension declaration grammar and the
  extension indexer proposal were checked against `dotnet/csharplang` and `dotnet/roslyn`, and the version premises
  were corrected. Scope: nothing implements or tracks the change; the interface and indexer half can be delivered
  immediately and the union half depends on the Roslyn move.

### PR-11. `ReferenceValidationContext.GetInboundGranularity` throws for a validated extension block

- Where: `src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124-134` (the switch and its throwing
  default arm) and `:56-57` (the only caller);
  `src/Metalama.Extensions.Architecture/Validators/ReferencePredicateValidator.cs:30-33` (the read of the
  destination); `src/Metalama.Extensions.Validation.Engine/ReferenceValidatorInstance.cs:52-58`;
  `src/Metalama.Extensions.Validation.Engine/ValidationRunner.cs:170-173,217`;
  `src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:104,111`;
  `src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs:34-39`;
  `Metalama/Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs:115-118`,
  `Metalama/Metalama.Framework/src/Metalama.Framework/Code/IExtensionBlock.cs:10-11`,
  `Metalama/Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs:187`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexerRequirements.cs:28-33,36,73`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexerOptions.cs:72-112,114-122,188-206`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/InboundReferenceIndexBuilder.cs:46-67`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs:781`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Utilities/UserCode/UserCodeInvoker.cs:133-143` and
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:183-190`.
- What happens today: the switch lists thirteen declaration kinds and throws an argument out of range exception for
  every other kind, which includes the extension block. The only caller is the destination property. The core
  repository treats an extension block as a valid validated declaration, exposes the corresponding declaration kind,
  exposes the extension blocks of a named type, and makes the extension block interface derive from the named type
  interface. A fabric can therefore select the extension blocks of a type and apply an architecture rule to them,
  which the query extensions accept, and the validated declaration reaches the validation context unchanged. The
  reference indexer, however, does not reach an extension block on its own. An extension block cannot be named in
  source, so it enters the index only as the containing type of one of its members, which the index builder adds only
  when descent into the referenced declaring type is enabled. The options builder enables that flag only for the
  named type kind and has no extension block case, and the same omission leaves identifier filtering enabled with an
  empty identifier set, so the walker rejects every reference. A project whose only reference validator is on an
  extension block therefore indexes nothing and reports nothing. The failure occurs when the same project also
  registers a reference validator on a named type: that validator enables descent for all references, an invocation
  of an extension member is aggregated onto the extension block, the extension block validator is found for that
  symbol, it runs, reads the destination, and throws. Extension blocks are a C# 14 feature and the Roslyn 5.0 parser
  already accepts them, so this is reachable with the versions consumed today; C# 15 only adds extension indexers to
  the set of members an extension block may declare, which makes the block a more likely validation target.
- Consequence: two outcomes, depending on the rest of the project. When no named type reference validator is
  registered, the extension block rule is a silent no-operation. When one is registered, the exception is raised
  inside the user validator and reported by the user code invoker as the error diagnostic `LAMA0041`, and the rule
  reports nothing else. Because extension blocks are C# 14, the diagnostic can be produced by a customer of the
  current release and not only after the C# 15 work.
- Proposed change: two changes are required, and the Premium one alone is not sufficient. In `Metalama.Premium`, map
  the extension block declaration kind to the type granularity. In the core repository, add the extension block kind
  to the named type case of the switch in `ReferenceIndexerOptions`, so that a validator on an extension block
  enables descent into the referenced declaring type and disables identifier filtering for the member reference
  kinds. Then add a validation aspect test that validates an extension block containing an extension method, and on
  the latest variant one containing an extension indexer. The test fabric must exercise the path that reaches the
  block: without the core change, or without a second validator on a named type, the test would report no diagnostic
  and pass without proving anything. Schedule the extension method half independently of the .NET 11 and C# 15 work,
  because it compiles with the Roslyn 5.0 variant that `Metalama.Premium` consumes today. The extension indexer
  variant requires two prerequisites: the latest Premium variant must move to the stable Roslyn that exposes C# 15 as
  a non-preview language version, and the core language version plumbing must admit C# 15.
- Size: medium, because the change spans two repositories.
- Status: new work. No pull request touches the file and two searches of the issue tracker returned no issue that
  scopes it. The related issues are #1339, which is the same defect class in the core repository and which shows that
  such switches were audited there and not in Premium, #1284, which is the user-visible symptom class for extension
  blocks in validation, and #1159 and #1035, which introduced the extension block into the code model.
- Verification. Code: the switch, the caller, the query path and the diagnostic path were confirmed, and three
  details were corrected, namely the reachability, which requires a second validator, the consequence class, whose
  more common outcome is silence, and the sufficiency of the Premium edit alone. Semantics: the version of the two
  features was checked against `dotnet/roslyn` and `dotnet/csharplang`, which established that extension blocks are
  C# 14 and reachable today and that only extension indexers are new in C# 15. Scope: nothing implements or tracks
  the change.
- Open questions: whether the Roslyn symbol returned for an extension member invocation carries the extension
  grouping type as its containing type after normalization was not verified, because this analysis runs no build. If
  it does not, the extension block is never indexed at all and the Premium switch is unreachable rather than latent.

### PR-12. Architecture rules under closed classes and unions

- Where: `src/Metalama.Extensions.Architecture/Validators/DerivedTypeNamingConventionValidator.cs:79-84`,
  `src/Metalama.Extensions.Architecture/Aspects/InternalOnlyImplementAttribute.cs:44-45`,
  `src/Metalama.Extensions.Architecture/Predicates/TypeEqualityPredicate.cs:82-91`,
  `src/Metalama.Extensions.Architecture/Predicates/HasFamilyAccessPredicate.cs:19-28`,
  `src/Metalama.Extensions.Validation.Engine/TransitiveValidatorInstance.cs`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexerRequirements.cs:35-40,57-65`,
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs:99-102,103,176-183,717,729,761-803,894-903`,
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexBuilder.cs:15-18`,
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMemberOrNamedType.cs:23`,
  `Metalama/Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs`, and
  `Metalama/eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954-1978`.
- What happens today: the core drops the base type reference kind and derived type indexing only when the validated
  named type reports itself as sealed. A closed class is implicitly abstract, cannot carry the sealed modifier and is
  never sealed, so the derived type naming convention rule, the derived type validation and the internal-only
  implementation aspect keep working on it unchanged. The internal-only implementation aspect is in any case outside
  the scope of the closed feature, because its eligibility requires an interface and C# 15 allows the modifier on
  classes only. The compiler restriction on a closed class is narrower than assembly-wide: the direct base type must
  be in the same module, so a class in a referencing assembly may still derive indirectly through a subtype that is
  not closed. The transitive validator that Premium persists therefore remains necessary for a closed class and is
  not redundant. A union declares its case types in the parameter list of the declaration, and a union symbol is a
  sealed struct, because the compiler maps the union declaration to the struct type kind and adds the sealed
  modifier, so the derived type naming convention rule applied to a union is stripped of its only reference kind and
  validates nothing. A union declaration has no primary constructor, so the semantic model returns no declared symbol
  for a case type parameter. The core walker resolves the origin of a reference that way and the index builder
  discards a reference whose referencing symbol is null, and the walker has no override for the union declaration, so
  the union is traversed by the default visit and each case type parameter becomes a current declaration with no
  symbol. A rule such as the accessibility restriction placed on a case type therefore does not see the union
  declaration that names it, and reports nothing. Pattern matching over a union is ordinary type pattern syntax and
  is indexed exactly as it is for a class.
- Consequence: no impact in `Metalama.Premium`. One silent false negative in the core repository: the references from
  a union declaration to its case types are not indexed, so an architecture rule placed on a case type under-reports,
  with no diagnostic. A second, smaller effect follows from the same omission: the base list of a nested union is
  attributed to the containing type rather than to the union.
- Proposed change: none in `Metalama.Premium`. In the core repository, add an override for the union declaration to
  the reference index walker that enters the union as the current type declaration and reports each parameter type of
  the parameter list as a reference whose origin is the union type, so that a case type reference is attributed
  rather than dropped. The override must be gated to the latest Roslyn variant, because the union node does not exist
  in the Roslyn 5.0 variant that serves Rider, and it must wait for the union syntax to leave the experimental state.
  Decide at the same time whether a dedicated reference kind for a union case is wanted rather than reusing the
  parameter type kind. Only then add the Premium aspect test that places an accessibility rule on a case type
  referenced by a union, on the latest variant. The test that applies the derived type naming convention rule to a
  closed class needs no core change and can be added as soon as the language version is available. Add a third test
  for a closed class derived from indirectly in another project, to record that the transitive validator is still
  required.
- Size: small for the Premium tests; small to medium in the core repository for the walker override, its variant
  gating and its own test.
- Status: new work, and the union half belongs to the core theme rather than to this one. No pull request and no
  issue proposes it. The related issue is #1913, because the two proposed aspect tests must run on the latest Premium
  variant, which metalama/Metalama.Premium#85 created.
- Verification. Code: the two validators, the core requirements, the walker and the index builder were re-read, and
  the union half was materially corrected: the case type reference is dropped rather than mislabelled, so the
  consequence class changes from no impact to a silent false negative in the core repository. Semantics: the closed
  hierarchy and union proposals and the Roslyn implementation were checked, which refuted the claim that the
  transitive validator becomes redundant and answered the open question about the type kind. Scope: nothing
  implements or tracks the change, and both halves require a language version that no consumed Roslyn exposes today.
- Open questions: whether a dedicated reference kind for a union case is wanted, so that a user can filter it apart
  from a parameter type reference. That is a decision about the core public interface, and it is now coupled to the
  walker change rather than being cosmetic. The two questions of the original report about the type kind and the
  syntax shape are answered: a union symbol has the struct type kind and reports itself as sealed, and a case type is
  a parameter node whose identifier token is missing.

### PR-13. Stale variant constant guidance in the unit test projects

- Where: `src/tests/Metalama.Extensions.CodeFixes.UnitTests/Metalama.Extensions.CodeFixes.UnitTests.csproj:11-14`
  and `src/tests/Metalama.Extensions.Validation.UnitTests/Metalama.Extensions.Validation.UnitTests.csproj:11-14`,
  `eng/RoslynVersions/Roslyn.5.0.0.props:8` and `eng/RoslynVersions/Roslyn.5.10.0.props:8` on `pr85`, and
  `Metalama/eng/RoslynVersions/Roslyn.5.10.0.props:10`.
- What happens today: the comment tells the author to differentiate tests with a Roslyn version constant. No Premium
  variant defines such a constant, and both variant props files carry an explicit comment saying so. The two unit
  test projects are not variant-built at all: their only import is the coverage props file, no variant props file
  reaches them, and no unit test shim project exists, which is PR-2. The same comment is accurate in the core
  repository, where the latest variant does define the constant and the shim projects exist. The advice was
  therefore already inert before the alignment, which only removed the constants that it names.
- Consequence: no impact. The text is inside an XML comment in a project file, so it produces no property, no
  constant, no diagnostic and no build behaviour.
- Proposed change: reword the comment to say that the unit tests build against the latest Roslyn only, or delete it
  when PR-2 adds variant shims and a constant. Replace the whole comment block rather than its last sentence, because
  the first sentence, which speaks of previous-Roslyn builds of these projects, is stale for the same reason. Correct
  only the Premium copies; the core wording is accurate where it stands.
- Size: small.
- Status: new work. The comment survives metalama/Metalama.Premium#85 verbatim, and three searches of the issue
  tracker returned no issue on the subject. The related issues are #1913, which removed the last constant definition
  and therefore made the comment stale while scoping only target frameworks and variant alignment, #1881, which
  established the doctrine the comment now contradicts, and the open umbrella #1921.
- Verification. Code: the comment, the two variant props comments, the absence of any constant definition in the
  repository and the absence of any variant import in the two projects were confirmed, and the finding was confirmed
  without correction. Semantics: not run. Scope: nothing implements or tracks the change, and it belongs inside the
  story that carries PR-2, because the correct wording depends on that outcome.

### PR-14. The Core flavour literal of the extension loader is repeated in eight Premium build files

- Where: `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-20,24`
  (the literal and the string comparison),
  `Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31,33,35` (the
  second copy of the literal, which feeds only the trace message, and the filter that delegates elsewhere),
  `Metalama/Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:71` (the serialized
  metadata); the four Premium files that carry the compared metadata, namely
  `src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props`,
  `src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props`,
  `src/Metalama.Extensions.CodeFixes/MetalamaExtensionAssemblies.props` and
  `src/Metalama.Extensions.Validation/MetalamaExtensionAssemblies.props`; the four that must agree with it, namely
  `src/Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj` and
  `src/Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj` through their packaged
  file paths and
  `src/Metalama.Extensions.CodeFixes.Package.Resources/Metalama.Extensions.CodeFixes.Package.Resources.csproj:6` and
  `src/Metalama.Extensions.Validation.Package.Resources/Metalama.Extensions.Validation.Package.Resources.csproj:6`
  through their target frameworks; [`platform-support.md`](../platform-support.md):301, :319-323 and :325; and
  [`extensibility.md`](../extensibility.md):215-241.
- What happens today: the Core flavour name of the platform baseline is a literal in the core engine. The reference
  type derives it from a .NET Framework Boolean and compares it for string equality against the target framework
  metadata of the extension assembly item, which reaches the engine through the delimited string built by the package
  targets. The extension loader base holds a second copy of the same literal, but that copy feeds only the trace
  message; the filter delegates to the reference type. `Metalama.Premium` repeats the same name in its own build
  files: four of them carry the compared metadata and four more must agree with it, because they name the directory
  that produces or packages those assemblies. metalama/Metalama.Premium#85 moved all of them to `net10.0`, which is
  what makes the payload load on .NET against the current core engine. The core document records that the two core
  literals move with the Core flavour, and it names only the core files; the document has no `Metalama.Premium`
  section. The extensibility guide shows the pattern with the current value and warns only against omitting the
  metadata; it does not say that the value is compared as a string, and it never references the platform support
  document. The aspect test projects are not part of this set, because their extension assembly items declare no
  target framework metadata, and the reference type accepts an item with no metadata on any framework.
- Consequence: silent wrong output at the next floor move, if a value is left behind. The loader drops the
  non-matching reference without a diagnostic, and the only diagnostic of that file applies to an assembly that was
  selected and then failed to load. A dropped extension removes a pipeline stage, so reference validation stops
  producing its diagnostics with no error reported. No test covers the comparison. The failure has a further trap:
  the aspect testing targets filter extension assemblies by target framework compatibility, which accepts a stale
  lower value, while the run-time comparison is exact, so a stale value passes the build-time filter and fails the
  run-time one.
- Proposed change: add a section "What this means in Metalama.Premium" to
  [`platform-support.md`](../platform-support.md), beside the existing section for `Metalama.Compiler` at :325, naming
  the four Premium files that carry the compared metadata and the four that must agree with it. Adding them under the
  section at :301 would contradict the per-repository organisation of the document. Optionally, state in
  [`extensibility.md`](../extensibility.md):215-241 that the target framework metadata is compared for string equality
  against the Core flavour name of the current platform baseline, so that a value which is merely compatible does not
  match.
- Size: small.
- Status: new work. The value half is merged: metalama/Metalama.Premium#85 moved every Premium literal, and its
  issue scoped no documentation. The related issues are #1913, whose acceptance criteria name only build files,
  #1876, which created the section of the platform support document that a documentation story must amend and which
  scoped the target framework list of the extensibility guide, and the open umbrella #1921.
- Verification. Code: the comparison, the serialization, the two roles of the eight Premium files and the silence of
  the loader were confirmed, and four details were corrected, namely that the second core literal is a trace copy,
  the commit citation, the conflation of the two roles into a single count, and the inclusion of an aspect test
  project that declares no metadata. Semantics: not run. Scope: nothing implements or tracks the documentation half,
  and this finding lands on the same documentation edit as PR-9.

### PR-15. Variant set alignment is implemented by Premium#85

- Where: the difference between `develop/2027.0` and `pr85`, 47 files, of which the principal ones are
  `eng/RoslynVersions/Roslyn.4.12.0.props` (deleted), `eng/RoslynVersions/Roslyn.5.0.0.props:3-7,9-18`,
  `eng/RoslynVersions/Roslyn.5.10.0.props:3-7,9-10`, `eng/RoslynVersions/Latest.props:2`,
  `Directory.Packages.props:8-9,36-37`, `nuget.base.config:14-16`,
  `src/Metalama.Extensions.CodeFixes/Metalama.Extensions.CodeFixes.csproj:17-20`,
  `src/Metalama.Extensions.Validation/Metalama.Extensions.Validation.csproj:16-17`,
  `src/Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj:53-62`,
  `src/Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj:46-51`,
  `src/Metalama.Extensions.CodeFixes.Package.Resources/Metalama.Extensions.CodeFixes.Package.Resources.csproj:6,26-29`,
  `src/Metalama.Extensions.Validation.Package.Resources/Metalama.Extensions.Validation.Package.Resources.csproj:26-27`,
  `src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props:9-18`,
  `src/Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props:8-11` and
  `Metalama.Premium.sln`;
  `Metalama/Metalama.Framework/src/Metalama.Framework.Implementation.Package/Metalama.Framework.Implementation.Package.csproj:6,12`.
- What happens today: the Roslyn 4.12.0 variant props file is deleted and no file on the branch contains that version
  string. The Roslyn 5.0.0 variant carries the project suffix and its four transitive package pins. The Roslyn 5.10.0
  variant reads the exported maximum interface version, carries the literal without the prerelease label and an empty
  project suffix, and pins two transitive packages at the latest patch. The latest variant props file is imported by
  the conditional import file. The three variant project directories are renamed from the 4.12.0 suffix to the 5.0.0
  suffix at 86 per cent similarity, and the solution entries are replaced while keeping the project identifiers. The
  central package management file carries the prerelease version for both properties and declares the two
  implementation packages, with the 4.12.0 entry removed. The package source configuration file is added, declaring
  the roslyn-consolidated feed. The internals visibility entries, the packaged assembly lists, the resources project
  references and the extension assembly metadata with their Roslyn version and target framework attributes all
  follow. Neither variant defines a preprocessor constant, no required-constant or forbidden-constant directive
  exists anywhere in the test tree, and the two test configuration files carry one setting each. The package source
  configuration file is not identical to the core file apart from one comment word: three lines differ, because the
  Premium file omits the exact package name mapping for the metapackage and its explanatory comment. That omission is
  deliberate and correct, as the head commit of the branch states, and no Premium project references that
  metapackage. Without the alignment, `develop/2027.0` cannot restore: it pins an implementation package at a variant
  that the core repository no longer produces, and the implementation packages target the .NET Framework and
  `net10.0` only while many Premium projects still target `net8.0`.
- Consequence: a build or restore error until the alignment lands, which it now has.
- Proposed change: none remains. The residuals are PR-1, the stable Roslyn transition, PR-2, the tests for the 5.0
  variant, and PR-13, the stale comment.
- Size: none beyond the pull request.
- Status: already in progress, and complete. metalama/Metalama.Premium#85 was merged into `develop/2027.0` on
  2026-09-03, and metalama/Metalama#1913 is closed as completed with that pull request recorded as the closing one.
  The related issues are #1913, #1881, #1876 and the open umbrella #1921.
- Verification. Code: the difference was reproduced locally and every item was confirmed at the cited location, with
  one correction, namely the description of the package source configuration file as differing by one comment word.
  Semantics: not run. Scope: the pull request is merged, so the finding records landed work rather than a proposal,
  and the original instruction to merge it is satisfied.

## Withdrawn findings

No finding of the original report was withdrawn. All fifteen findings survived the verification passes and none was
refuted. Eleven of them changed materially and are recorded above rather than withdrawn, because their central claim
held while a supporting statement did not. Two changed status rather than content, because the work landed between
the writing of the report and its verification.

Two proposals inside surviving findings were refuted in detail and are recorded here, so that they are not
reintroduced. PR-10 proposed overriding the visit method of the core safe syntax rewriter; that method is sealed and
the class directs derived classes to a different extension point, so the proposal as written does not compile, and
the same proposal would have dropped the enum case, because the enum declaration node does not derive from the type
declaration node. PR-3 proposed adding the .NET Framework 4.8 targeting pack and developer pack components to the
Premium container image; the merged core decision removed all such components instead, on the ground that the .NET
software development kit obtains the reference assemblies from the reference assembly packages, so a Premium story
should ask whether the two remaining 4.7.2 components are removed rather than whether the 4.8 pair is added.

Three premises that several findings shared were refuted and corrected throughout. PR-1, PR-10 and PR-12 named a
stable Roslyn 5.10 as the release that removes the current constraints; no such release exists or is expected, and
the target is the expected stable 5.12, which changes the version numbers of three proposals without changing their
mechanism. PR-9 and the core document it cites explained the loading of a lower target framework asset by the
roll-forward policy of an application host; the mechanism is instead the ordinary resolution of framework references
inside an MSBuild process that already runs, and the correction is part of the documentation change. PR-12 claimed
that the compiler confines derivation from a closed class to its assembly, which would make the transitive validator
redundant; the restriction applies to the module and to direct derivation only, so the transitive validator remains
necessary.

## Non-findings

The following were checked and found unaffected. They are recorded so that they are not checked again.

- No `Metalama.Premium` production or test source contains a conditional compilation block on a Roslyn variant
  constant, a required-constant or forbidden-constant test directive, or a required-constants entry. The only
  occurrences of the constant prefix anywhere on the branch are the two variant props comments and the two project
  comments of PR-13.
- No Premium source duplicates the supported C# versions table, the language version provider, the Roslyn interface
  version enumeration or the resource extractor. A search for the language version, the Roslyn version, the manifest
  resource stream, the assembly load context and the assembly resolve event over the source tree finds only
  `src/tests/Metalama.Patterns.Caching.Backends.UnitTests/RedisServer/RedisTestInstance.cs:49`, which extracts an
  embedded executable for the tests.
- The Roslyn public interface used by the Premium design-time code is the code action, the apply-changes operation,
  the document, the solution, the comment factory method and the node search method. The tests implement the Metalama
  abstractions for the code fix and code refactoring contexts rather than constructing the Roslyn contexts. No
  interface break against Roslyn 5.10 was found.
- The add-attribute code action special-cases the variable declarator only and delegates to the core syntax
  generator, and the remove-attribute rewriter visits the attribute and attribute list nodes only. Both are
  independent of the declaration kind, so unions and extension indexers add no case to either. The Premium switches
  with a throwing default arm are over the accessibility, the reference granularity, the declaration validation time
  and a menu item kind; none is over a syntax kind, and the only one over a declaration kind is PR-11.
- Extension indexers as referenced members require no Premium change. The validator query sources map accessor
  validators to the declaring member, the validation context maps the indexer kind to the member granularity, the
  reference end resolves the type through the closest named type, and the core walker enters indexer declarations
  wherever they appear and supports the relevant reference kinds for indexers. Nothing in Premium depends on the
  indexer being declared in a class rather than in an extension block.
- The experimental attribute, the internals accessibility rule, the namespace predicate, the assembly name predicate
  and the type name predicate reason about accessibility, namespaces and names only, and are unaffected by closed
  classes, unions or extension indexers.
- The caching backends are unaffected. After the alignment the two backend projects target the .NET Framework 4.7.2,
  `net10.0` and .NET Standard 2.0, and their three principal dependencies carry no .NET 11 constraint, so a
  `net11.0` consumer resolves the `net10.0` asset. The .NET Framework floor violation named in #1913 is fixed.
- The licensing build task is unaffected as an asset. After the alignment it targets `net10.0` and the .NET Framework
  4.7.2, it conditions its .NET Framework-only merge input correctly, and the licensing project packs both
  directories. The `net10.0` task loads on the .NET 11 runtime, although by the mechanism corrected in PR-9 rather
  than by roll-forward.
- The standalone tests are unaffected apart from what PR-3 and PR-8 record. Every project moved to `net10.0` except
  `Issue32827`, which keeps .NET Standard 2.0 and 2.1 with an explicit language version of 11.0, inside the minimum
  and maximum .NET Standard versions that the core package props seed. No test configuration file names a target
  framework or a software development kit version.
- The latest-Roslyn solution filter lists the unsuffixed projects only and is unaffected by the rename. The project
  usage regular expressions in `eng/src/Program.cs:86-90` are unanchored and still match the renamed variant
  projects.
- `Metalama.Premium` has no `CLAUDE.md`. Its top-level readme gives the container build command only, the package
  readme files make no platform claim, and the code style readme is generic. The core
  [`extensibility.md`](../extensibility.md):141 already describes Premium as built for the two Roslyn variants.
- The preview language version of the two unit test projects compiles the test assemblies with the preview language
  of the Metalama.Compiler toolchain. Those projects consume that compiler privately, so the unsupported C# version
  check of the compile-time pipeline does not apply to them.

## Related themes

The findings of this theme cross-reference the following work owned elsewhere. The prefix of a finding identifies its
theme: LV for the language version and the hosts, TP for the syntax generator and the templates, CM for the code
model, LK for the linker and the advice, DT for the design-time pipeline, UT for the user target frameworks, the
tests and the documentation, and PR for this theme.

- Renumbering the latest Roslyn variant to the stable 5.12 (cluster CL-05, owned by the language version theme).
  PR-1 is the `Metalama.Premium` half of one release step that LV-12, LV-13, LV-14, TP-1, TP-9, DT-3 and DT-8 also
  report. It is delivered as a separate pull request, because a pull request cannot span two repositories, and it
  must land in the same release as the core renumbering. PR-10 and PR-12 also depend on that step for their C# 15
  halves, because no Roslyn consumed today exposes C# 15 as a non-preview language version.
- The .NET 11 software development kit and the `net11.0` legs (cluster CL-10, owned by the user target frameworks and
  tests theme). PR-8 is the Premium member of a cluster that also contains LV-9, UT-1 and UT-4 to UT-8. The container
  is the shared prerequisite, and its feedback loop is one continuous integration cycle per attempt, so it is
  separated from the legs that consume it. The recorded core decision delegates the software development kit
  dimension to `metalama/Metalama.Tests.DotNetSdk`, which makes the Premium action a request rather than a container
  change.
- Switches over declaration kinds that fall through (cluster CL-17, owned by the design-time theme). PR-10 and PR-11
  are two of four instances of the same shape, with DT-5 and TP-7. None of the four is caused by C# 15 and all are
  reachable today, and in every case the remedy is to test an abstract syntax base type or to add the missing arm,
  which admits unions later without naming an experimental member.
- Unions and the reference graph (cluster CL-18, owned by the user target frameworks, patterns and documentation
  theme). PR-12 shares its premise with UT-14 and its four sub-findings, namely that a union is an ordinary struct
  with an opaque value property for every consumer of the code model. Its only product change is the override in the
  core reference index walker, without which an architecture rule on a case type under-reports with no diagnostic.
- `Metalama.Premium` residuals after the two merged alignment pull requests (cluster CL-19, owned by this theme).
  PR-2, PR-3, PR-5, PR-6, PR-7 and PR-13 are the remaining items, and PR-4 and PR-15 are fully covered by merged work
  and must not be proposed again. All except PR-2 are small build file items with no dependency on the Roslyn gate.
  PR-2 is the only Premium item of real size, because the variant that serves Rider and the C# Dev Kit is compiled by
  the solution and executed by no test.
- Documentation that states the previous baseline (cluster CL-20, owned by the user target frameworks, tests and
  documentation theme). PR-9 and PR-14 extend the drift point list of [`platform-support.md`](../platform-support.md)
  to the Premium build files, and they are delivered with UT-18, LV-10 and DT-9 in one pull request, because four of
  the five edit the same two documents. Both also require a new section, because the document has no
  `Metalama.Premium` section today.
- The Roslyn variant gating strategy (cluster CL-09, owned by the code model theme as CM-10). PR-10 and PR-12 each
  depend on it for the union half of their proposals, and PR-2 raises the same question for `Metalama.Premium`
  specifically, because Premium defines no variant constant at all while the core repository defines one for the
  latest variant.
