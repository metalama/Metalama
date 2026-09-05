# 06. The `net11.0` user target framework, runtime dependencies, patterns, tests and documentation

This document covers the platform surface that faces the user of Metalama rather than the compiler internals: the
`net11.0` user target framework and the build container that would have to produce it, the supported-platform checks
of the package build files, the package version pins of the runtime and test dependencies, the pattern and extension
libraries (`Metalama.Patterns`, `Metalama.Extensions`), the test matrix and the test conventions, and the
documentation that states the previous platform baseline. It records what happens today and what has to change for
Metalama 2027.0. The analysis reads the code as it stands on 2026-09-03 on branch
`topic/2027.0/26-09-03-update-eng-7e3j07` of the `Metalama` repository, and its verification passes read external
sources on 2026-09-04. Each finding was re-checked by three passes: a code pass that re-read the cited code and tried
to falsify the claim, a semantics pass that re-checked every external premise against `dotnet/roslyn`,
`dotnet/sdk`, `dotnet/msbuild`, `dotnet/csharplang` and the published package metadata, and a scope pass that
established whether the proposed change is already implemented, in flight or tracked. The platform baseline PB-2027.0
is decided by [`platform-support.md`](../platform-support.md), the permitted package versions by
[`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving to a new Roslyn by
[`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

No project was built and no test was run for this analysis.

## Summary

1. No shipped package needs a `net11.0` asset. Every product project targets `netstandard2.0`, `net472` and
   `net10.0` (or `net10.0-windows`), the embedded Core flavour is `net10.0` by decision, and NuGet selects the
   `net10.0` asset for a `net11.0` consumer. The work of this theme is in the build container, the test matrix, a few
   stale defaults and the documentation.
2. The build container installs exactly one .NET SDK, 10.0.400 (`eng/src/Program.cs:26`), and a .NET 10 SDK rejects a
   `net11.0` project outright with `NETSDK1045`. Every finding of this theme that adds a `net11.0` leg (UT-5, UT-6,
   UT-7, UT-8) is blocked on that one change, and the repository already records the gap in
   `SupportedPlatform.TestedTargetFrameworks.csproj:8-10` and in issue #1884.
3. One defect is live today and contradicts the support matrix that the repository declares. `MaximumSdkVersion` is
   `11.0` while every .NET 11 SDK reports a three-component version such as `11.0.100`, so the comparison at
   `Metalama.Framework.targets:412` reports `LAMA0601` on every build with a .NET 11 SDK, which is a warning that
   becomes an error under `TreatWarningsAsErrors` (UT-2).
4. Two engine defaults still name the previous baseline. The nested compile-time reference project restores a
   `net8.0` leg on every build (UT-3), and every unit, aspect, template and linker test observes
   `Project.TargetFramework == "net8.0"` whatever the test assembly targets (UT-4). Neither produces a failure, which
   is why the change that dropped .NET 8 and .NET 9 did not correct either of them.
5. Runtime dependency pins do not have to move for .NET 11. The per-variant `System.Text.Json` pins are floors
   imposed by the Roslyn packages on their `netstandard2.0` assets, and they stay on the 10.0 line even after the
   renumbering to Roslyn 5.12. Package pruning and the transitive audit default are .NET 10 SDK behaviours that the
   .NET 11 SDK does not change. Finally, `net8.0` is not an end-of-life target framework in either SDK today (UT-11,
   UT-12, UT-13).
6. A C# 15 union declaration is `TypeKind.Struct` in Roslyn, so every pattern library treats it as an ordinary
   non-record struct. `[Observable]` rejects it, Multicast matches it as `MulticastTargets.Struct`, the immutability
   classification calls it mutable and produces a spurious Observability warning, and Caching collapses every value
   of a union to one cache key and loses the value in a JSON round trip (UT-14 and its four sub-findings).
7. Closed classes, labeled `break` and `continue`, `with(...)` elements and extension indexers reach no
   pattern-specific code path that fails. The remaining work for them is test coverage and, for extension indexers,
   the contract advice that theme 04 owns (UT-15, UT-16, UT-17).
8. Three test gates prevent their tests from running, and no build and no test run fails as a result. Two facts of
   the Contracts unit tests are excluded on every leg because their guard names `NET6_0` rather than
   `NET6_0_OR_GREATER`, and two aspect tests never run because their guard names `ROSLYN4_4_OR_GREATER`, which no
   variant defines (UT-9, UT-10). The conventions that a C# 15 test suite needs are recorded in UT-19, and they
   cannot produce an executing test before the move to the stable Roslyn.

## Findings

### UT-1. The build container carries no .NET 11 SDK

- Where:
  - `eng/src/Program.cs:26` (`const string dotNetSdkVersion = "10.0.400"`), `:35-37` (the only `DotNetComponent`),
    `:212-229` (the docker scenario configurations)
  - `eng/docker/build.Dockerfile:43-44`
  - `Metalama.Framework/src/tests/docker/win-x64/NonStandardDotNetRoot/Dockerfile:18`,
    `Metalama.Framework/src/tests/docker/linux-x64/NonStandardDotNetRoot/Dockerfile:21`,
    `Metalama.Framework/src/tests/docker/linux-x64/CompilerLogs/Dockerfile:21`,
    `Metalama.Framework/src/tests/docker/linux-x64/GlobalNamedMutexUnavailable/Dockerfile:24`,
    `Metalama.Framework/src/tests/docker/linux-x64/MetalamaKill/Dockerfile:22`,
    `Metalama.Framework/src/tests/docker/win-x64/ReferenceAssemblyArchitectureMismatch/Dockerfile:18`
  - `Metalama.Framework/src/tests/docker/linux-x64/CompilerLogs/global.json`,
    `Metalama.Framework/src/tests/docker/linux-x64/GlobalNamedMutexUnavailable/global.json`,
    `Metalama.Framework/src/tests/docker/linux-x64/MetalamaKill/global.json`,
    `Metalama.Framework/src/tests/docker/linux-x64/NonStandardDotNetRoot/global.json`,
    `Metalama.Framework/src/tests/docker/win-x64/NonStandardDotNetRoot/global.json`
  - `Metalama.Framework/docs/platform-support.md:195-197`, `:199-201`, `:354-374`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:31,33,38,39`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:115-121`, `:243-248`
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8-13`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/ReferenceAssemblyBuildFailureClassifier.cs:147-153`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CompileTime/ReferenceAssemblyBuildFailureClassifierTests.cs:154-156`
  - `Directory.Packages.props:12`, `.gitignore:65`
  - External: `dotnet/sdk` `src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.TargetFrameworkInference.targets:195-197`,
    `src/Tasks/Common/Resources/Strings.resx` (`UnsupportedTargetFrameworkVersion`, `NETSDK1045`),
    `eng/Versions.props` (11.0.100-rc.1) and `eng/Version.Details.props:80`
    (`Microsoft.Net.Compilers.Toolset` 5.12.0-1.26452.110)
- What happens today: the build container installs one .NET SDK. `eng/src/Program.cs:26` declares the version,
  `eng/src/Program.cs:35-37` registers the single `DotNetComponent`, with a comment stating that it is the only .NET
  SDK required by the product, by the tests and by this project, and `eng/docker/build.Dockerfile:43-44` installs
  that version and no other. `global.json` is generated from the same constant (`platform-support.md:199-201`) and is
  git-ignored (`.gitignore:65`), so the main build and the solution test runs use SDK 10.0.400. The docker scenario
  tests are the exception: six Dockerfiles pin a 10.0.302 SDK and five of those directories carry a checked-in
  `global.json` with the same version. The .NET 11 SDK, which `platform-support.md:195-197` and
  `Metalama.Framework.props:33,39` declare supported, is therefore never installed and never exercised. The
  repository records the same gap independently: `SupportedPlatform.TestedTargetFrameworks.csproj:8-10` excludes
  `net481`, `net11.0` and `net11.0-windows` from the end-to-end matrix project because the build agents do not have
  their targeting packs, citing issue #1884, and the verification checklist of `platform-support.md:354-374` has no
  item that installs the .NET 11 SDK. Adding a `net11.0` target framework before the SDK changes fails in
  `Microsoft.NET.TargetFrameworkInference.targets`, which raises `NETSDK1045` when the target framework version
  exceeds `NETCoreAppMaximumVersion`; that message identifier is already classified by
  `ReferenceAssemblyBuildFailureClassifier.cs:147-153` for the nested reference-assembly build. As of 2026-09-04 the
  .NET 11 SDK is at 11.0.100-rc.1, so a container component added today installs a release candidate.
- The language-version half of the original proposal does not hold and belongs to a different work item. The .NET SDK
  carries no per-target-framework language default; the implied `LangVersion` is computed by the compiler toolset in
  `Microsoft.CSharp.Core.targets`, which derives the maximum supported version from the project target framework,
  caps it at the maximum version the compiler itself knows, and assigns it only when the user has not set one. That
  cap is 14.0 in Roslyn 5.0, 5.3, the 5.9.0 stable commit and the whole 5.10 line, and 15.0 only from the 5.11 line
  onward. With the Metalama.Compiler that the repository consumes today, a `net11.0` project that sets no
  `LangVersion` receives 14.0, which the condition at `Metalama.Framework.targets:118` accepts, so neither the
  rewrite to 12.0 nor the `MetalamaCheckLangVersion` warning occurs. What `global.json` pins does not change that,
  and pinning an 11.0.x SDK is mandatory rather than optional as soon as any project targets `net11.0`.
- Consequence: build or restore error for any project that adds `net11.0` before the SDK is added (`NETSDK1045`).
  Before that there is no build error and no diagnostic, only a coverage gap between the declared support matrix and
  what the agents can exercise.
- Proposed change: add a second `DotNetComponent` for the .NET 11 SDK. `DotNetComponent` takes a free-form version
  string (`eng/src/Program.cs:37`), so this route needs no PostSharp.Engineering release. The alternative route, the
  `Microsoft.NetCore.Component.SDK` of the Visual Studio 2027 Build Tools, is not available today: the
  `PostSharp.Engineering.BuildTools` 2023.2.420 assembly pinned at `Directory.Packages.props:12` exposes only
  `v17_14_15`, `v17_14_23`, `v17_14_39` and `v18_9_2` as `VisualStudioBuildToolsComponentVersion` values. Read the
  `MSBuildExtensionsPath` note of `CLAUDE.md` and the comment at `eng/src/Program.cs:19-25` before adding the second
  SDK: two .NET SDK directories in one image were the cause of an `MSB4062` restore failure, and the mitigation
  already present in `Utilities/MSBuildTool.cs` and in the blocked-environment-variable list of
  PostSharp.Engineering is incomplete by one entry. An `11.0.x` variant of the docker scenarios is optional coverage
  rather than a prerequisite; if it is wanted, six Dockerfiles and five checked-in `global.json` files must move
  together, and none of those scenarios is about .NET 11.
- Size: medium.
- Status: new work. Not implemented, not in progress and not tracked. None of the sixteen sub-issues of the open
  meta-issue #1921 covers the build container, and the local commits ahead of `origin/develop/2027.0` that touch the
  container move it to Visual Studio 2026 18.9.2, pin the SDK to 10.0.400 and stop `MSBuildExtensionsPath` from
  leaking into a nested build. Related: #1884, which widened the declared matrix; #1876, whose comment at
  `eng/src/Program.cs:34-35` this story revises; #1902, which establishes that a Build Tools channel must be added to
  PostSharp.Engineering first; #1745, the precedent for deciding what `global.json` pins.
- Verification: the code pass re-read the container definition, counted six Dockerfiles rather than two, found the
  five checked-in `global.json` files and read the `VisualStudioBuildToolsComponentVersion` members out of the
  consumed PostSharp.Engineering assembly, which answers one open question; the semantics pass confirmed
  `NETSDK1045` and its condition in the .NET SDK sources, refuted the claim that the pinned SDK decides the default
  language version, and dated the .NET 11 SDK at 11.0.100-rc.1 with a 5.12 compiler toolset; the scope pass found no
  issue and no pull request that proposes a second SDK component.
- Open questions: which SDK `global.json` pins once two are installed, and whether a release candidate SDK is
  acceptable in the image until the .NET 11 SDK reaches general availability.

### UT-2. `MaximumSdkVersion=11.0` reports LAMA0601 for every .NET 11 SDK

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:33` (`MaximumSdkVersion` 11.0),
    `:39` (the sentence naming 10.0 and 11.0 as the supported SDK versions)
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:399` (`_MetalamaSdkVersion`),
    `:405-408` (the minimum rule, which shares the property), `:410-413` (the maximum rule), `:321-322` (the target
    framework version, which is not affected), `:392` (the target is hooked before
    `_CheckForInvalidConfigurationAndPlatform`)
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.ContributedRequirements/SupportedPlatform.ContributedRequirements.csproj:39-41`
    (the `Test.LegacySdk` requirement, whose `MaximumSdkVersion` 1.0 is on line 40),
    `Metalama.Framework/src/tests/Standalone/SupportedPlatform.ContributedRequirements/test.json`
  - `eng/src/Program.cs:26` (the agent SDK is 10.0.400, so no existing scenario reaches the boundary)
- What happens today: `Metalama.Framework.props:33` sets `MaximumSdkVersion` to 11.0 and `:39` documents the intent as
  the supported SDK versions being 10.0 and 11.0, that is major and minor lines.
  `Metalama.Framework.targets:399` computes `_MetalamaSdkVersion` as `NETCoreSdkVersion` with the prerelease suffix
  removed, for example `11.0.100`, and `:411-413` reports `LAMA0601` when
  `$([MSBuild]::VersionGreaterThan($(_MetalamaSdkVersion), %(_MetalamaPlatformRequirement.MaximumSdkVersion)))` is
  true. With the .NET 10 SDK the comparison of `10.0.400` against `11.0` is false and nothing is reported. With a
  .NET 11 SDK the comparison is `11.0.100` against `11.0`, and MSBuild compares versions through `SimpleVersion`,
  which treats absent components as zero, so `11.0` is `11.0.0.0` and `11.0.100` is greater. The diagnostic is
  therefore reported for every .NET 11 SDK, including the first one. The target framework rule does not have this
  problem, because `$(TargetFrameworkVersion)` for `net11.0` is `v11.0` and `:322` trims it to `11.0`, which is
  neither greater nor less than the maximum, and the minimum rules are safe for the same reason. The message
  contradicts itself, because the sentence appended at `:413` states that the supported .NET SDK versions are 10.0
  and 11.0 while the first sentence states that the .NET SDK 11.0.100 is not supported.
- Consequence: a diagnostic is reported. Every user building with a .NET 11 SDK, which the baseline places in the
  supported set, gets the warning `LAMA0601`; it is emitted by a `Warning` task, so an ordinary build continues, but
  it becomes an error in any project that sets `TreatWarningsAsErrors`, and the target is hooked before
  `_CheckForInvalidConfigurationAndPlatform` so that it also appears in design-time builds.
- Proposed change: compare the .NET SDK against `MaximumSdkVersion` on the major and minor components only, and do so
  in the maximum rule alone. Truncating `_MetalamaSdkVersion` itself at `:399` would also change the minimum rule at
  `:405-408`, which shares that property, and would remove feature-band precision from `MinimumSdkVersion`, which a
  contributing package may legitimately set to a value such as 10.0.200. Introduce a second property, for example
  `_MetalamaSdkVersionMajorMinor` computed from the first two components of `$(NETCoreSdkVersion)`, and use it in the
  condition at `:412` only, with a comment stating that `MaximumSdkVersion` names the last supported major and minor
  line. Writing `11.0.99999` in the props file is the alternative and is worse, because it leaves the same defect
  available to every requirement contributed by another package. Add a regression scenario that pins the boundary:
  the smallest form is a fifth requirement in `SupportedPlatform.ContributedRequirements`, for example
  `Test.CurrentSdk`, whose `MaximumSdkVersion` is computed from the running SDK so that the scenario stays valid
  when the agent SDK moves, with `warning LAMA0601.*'Test[.]CurrentSdk'` added to the forbidden diagnostics of its
  `test.json`. The existing `Test.LegacySdk` requirement uses 1.0 and cannot detect the boundary.
- Size: small.
- Status: new work. Not implemented, not in progress and not tracked. The finding is a defect against an acceptance
  criterion of the closed issue #1884, whose own rules state that an SDK of 10.0 or 11.0 must produce no warning, and
  a story for it belongs under the meta-issue #1921. The same comparison affects any `MaximumSdkVersion` declared by
  a package under the contract introduced by #1887.
- Verification: the code pass read the two rules end to end, confirmed that no existing scenario reaches the
  boundary, and corrected one citation and the consequence class; the semantics pass confirmed from the MSBuild
  sources and unit test data that `SimpleVersion` pads absent components with zero and that `1.2` is therefore less
  than `1.2.1`, and confirmed that the .NET 11 SDK reports a three-component `NETCoreSdkVersion`; the scope pass
  confirmed that no pull request or issue touches `Metalama.Framework.props` or `Metalama.Framework.targets` for this
  purpose.
- Open questions: none. The original report rated the finding plausible pending a build under an 11.0.100 SDK; the
  semantics pass established the comparison from the MSBuild sources instead, and the conclusion holds under both
  candidate parsers. Report 01 of this series records the opposite conclusion at
  `analysis-reports/01-language-version-and-hosts.md:32` and `:206` from the same premise; that statement is wrong
  and is superseded by this finding.

### UT-3. The nested compile-time reference project still targets `net8.0`

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` (the default
    value), `:209-212` (where it is used), `:219-224` (`netstandard2.0` is mandatory), `:266` (the framework list is
    appended to the cache key), `:415-417` (the selection of the additional compile-time assembly directory),
    `:742-750` (the generated temporary project), `:755-758` (the package item group)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/DotNetTool.cs:93-98,123-133`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:103`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/IProjectOptions.cs:203`
  - `Metalama.Framework/src/tests/Standalone/Issue1789/Issue1789.csproj:13` and its `README.md`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CompileTime/CompileTimeTargetFrameworksTests.cs:23-26,31,52-56`
  - `Metalama.Framework/src/tests/docker/win-x64/ReferenceAssemblyArchitectureMismatch/test.ps1:74-78`
  - `Metalama.Framework/docs/platform-support.md:285`, `:295-297`
- What happens today: `CompileTimeAssemblyLocator.cs:43` sets the default compile-time target frameworks to
  `netstandard2.0;net8.0;net48`, used at `:209-212` when `MetalamaCompileTimeTargetFrameworks` is empty and written
  into the `TargetFrameworks` property of the generated temporary project. The list must contain `netstandard2.0`
  (`:219-224`), and on a .NET host the first entry matching `net1*` or `net6` to `net9` names the directory of the
  additional compile-time assemblies (`:415-417`), which selects `net8.0`. `Issue1789.csproj:13` and its `README.md`
  document that scenario as using the same target frameworks as the default, and the unit test data, the
  documentation comment of `IProjectOptions.CompileTimeTargetFrameworks` and a comment of the
  `ReferenceAssemblyArchitectureMismatch` scenario all repeat the value. Every build therefore restores a temporary
  project with a `net8.0` leg, for a user target framework that PB-2027.0 drops (`platform-support.md:295-297`).
- The consequences are smaller than the original report stated. The .NET 11 SDK still declares `net8.0` as a
  supported target framework and still does not treat it as end of life: the end-of-life list of
  `Microsoft.NET.EolTargetFrameworks.targets` stops at 7.0 and names `net8.0` as the minimum non-end-of-life target
  framework, on `main` and on `release/10.0.1xx` alike, so no `NETSDK1138` is emitted today. The entry may be added
  around 2026-11-10, when .NET 8 support ends, and it would then be a warning only, because the temporary project
  sets no `TreatWarningsAsErrors` and imports no `Directory.Build.*` file (`:742-745`); it would also not be
  displayed, because `DotNetTool.Execute` keeps the child output only to build the exception of a failed run and
  discards it on success (`DotNetTool.cs:93-98,123-133`). No restore error follows from the `net8.0` leg either:
  `netstandard2.0` is mandatory in the same project and is compatible with `net8.0`, so a package that restores for
  the mandatory leg also restores for `net8.0`, and a package that does not already fails on the mandatory leg. The
  .NET 8 reference pack is downloaded from NuGet because the SDK layout bundles only its own version of
  `Microsoft.NETCore.App.Ref`, which is equally true of the .NET 10 SDK and is not a change.
- Consequence: alignment and asset selection, not a build error. For an additional compile-time package that ships
  both a `netstandard2.0` asset and a `net10.0` asset, the `net8.0` leg selects the older one, so the compile-time
  compilation sees the older reference assembly; and an out-of-support target framework keeps being restored on every
  build.
- Proposed change: change the default to `netstandard2.0;net10.0;net48`, which the selection at `:417` already
  accepts, and update `Issue1789.csproj:13`, its `README.md`, the unit test data, the comment at
  `IProjectOptions.cs:203` and the comment at `test.ps1:74-78`. Use `net10.0` and not `net11.0`: PB-2027.0 keeps the
  .NET 10 SDK as a build-time SDK (`platform-support.md:195-197`), and a .NET 10 SDK rejects a `net11.0` target
  framework. The constant is a plain
  string with no Roslyn API surface, so it compiles under both variants, and `DefaultProjectOptions.cs:103` returns
  null, so no second default has to be changed. The framework list is appended to the cache key at `:266`, so
  existing cache directories are invalidated once, which is harmless.
- Size: small.
- Status: new work. The literal remains from the closed issue #1876, whose scope enumerated build files and
  package folders and never reached this C# constant. Related: #1789, the issue behind the standalone scenario and
  the unit test that repeat the value; #1885, the last change to the same temporary project, which declares the
  prerelease package source in the generated `nuget.config` and must not be disturbed; #725, closed as not planned,
  which proposed deriving the frameworks from the current process instead.
- Verification: the code pass confirmed the default and every site that repeats it, and refuted the restore-failure
  consequence by showing that `netstandard2.0` is mandatory and compatible with `net8.0`; the semantics pass read the
  end-of-life list and the bundled-versions data of both SDK branches and refuted the `NETSDK1138` claim, and
  established asset selection as the remaining reason for the change; the scope pass confirmed that no pull request
  or issue covers it and noted that the proposed default raises the floor of the nested restore to the .NET 10
  reference pack.
- Open questions: whether the documented value of `MetalamaCompileTimeTargetFrameworks` in `Metalama.Documentation`
  also names `net8.0`. That repository is not present in this environment.

### UT-4. Tests observe `Project.TargetFramework == "net8.0"` whatever the test assembly targets

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:56`
  - `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestProjectOptions.cs:129`,
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContextOptions.cs:173`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestRunner.cs:55`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Fabrics/DeclarativeAdviceWithTemplateInitialization.cs:15`
    and `DeclarativeAdviceWithTemplateInitialization.t.cs:17`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestProjectProperties.cs:32`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:108-111`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestDiscoverer.cs:74-81`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestExecutor.cs:303`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:565-568`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/ProjectModel.ProjectFeaturesImpl.cs:30`
- What happens today: `DefaultProjectOptions.cs:56` declares `public virtual string? TargetFramework => "net8.0";`
  and no derived type used by a test overrides it. `TestProjectOptions.cs:129` overrides only `AllTargetFrameworks`,
  and the linker test runner uses `DefaultProjectOptions` itself. `meta.Target.Project.TargetFramework` and
  `IProjectOptions.TargetFramework` are therefore `"net8.0"` in every unit, aspect, template and linker test, on the
  `net48` leg as well as on the `net10.0` leg, and the expected output of
  `DeclarativeAdviceWithTemplateInitialization.t.cs:17` records that constant. The test framework knows the real
  value, because `Metalama.Testing.AspectTesting.targets:108-111` publishes `$(TargetFramework)` as assembly
  metadata and `TestDiscoverer.cs:74-81` places it in `TestProjectProperties.TargetFramework`, but
  `TestExecutor.cs:303` copies only `AllTargetFrameworks` into the test context options.
  `ProjectModel.ProjectFeaturesImpl.ComputeSupportsCovariantReturn` reads
  `options.AllTargetFrameworks ?? options.TargetFramework` (`:30`), so it is evaluated on `"net8.0"` only in unit
  tests and linker tests, where `AllTargetFrameworks` is null; in aspect tests it is evaluated on the real list. A
  `net11.0` test leg would keep reporting `net8.0`.
- Consequence: silent wrong output, confined to tests. Production always resolves the property through
  `MSBuildProjectOptions`, so no user compilation is affected, and nothing asserts or reports a diagnostic on the
  stale value.
- Proposed change: the minimal change is to raise the default at `DefaultProjectOptions.cs:56` from `"net8.0"` to
  `"net10.0"` and re-accept `DeclarativeAdviceWithTemplateInitialization.t.cs`, which is the only snapshot that
  prints the value. Routing `TestProjectProperties.TargetFramework` into the test context options and overriding
  `TargetFramework` in `TestProjectOptions` is the more faithful change, and it is not sufficient on its own:
  `BaseTestRunner.cs:565-568` composes a single expected file per aspect test with no target framework in its name,
  so a value that differs between legs cannot be satisfied by re-accepting the baseline. That variant additionally
  requires restricting the test to one leg with `// @TargetFrameworks(net10.0)`, and it leaves unit tests and linker
  tests on the default, because they do not go through `TestProjectProperties`. Note that the default is also the
  fallback of every non-test host, so raising it is a change of that fallback as well.
- Size: small.
- Status: new work. The literal is untouched since April and was not in the scope of #1876, which moved the target
  frameworks of the build files. Related: #1884, the other consumer of the target framework as seen by the pipeline.
- Verification: the code pass confirmed the default, the absence of any test-side override and the snapshot that
  records it, and corrected the covariant-return claim, which holds for unit and linker tests only; the semantics
  pass was not engaged, because the finding carries no external premise; the scope pass confirmed that no pull
  request and no issue covers it.
- Open questions: none.

### UT-5. No test project has a `net11.0` leg

- Where:
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14,23`,
    `Metalama.Framework.Tests.UnitTests.csproj:7`, `Metalama.Framework.Tests.TemplateTests.csproj:14`,
    `Metalama.Framework.Tests.LinkerTests.csproj:14`, `Metalama.Framework.Tests.UnitTestHelpers.csproj:8`
    (all `net48;net10.0`)
  - `Metalama.Framework.Tests.Workspaces.csproj:7`, `Metalama.Framework.Engine.Analyzers.Tests.csproj:7`,
    `Metalama.Framework.Analyzers.Tests.csproj:7`, `Metalama.Framework.Tests.Benchmarks.csproj:6` (`net10.0`)
  - `Metalama.Backstage/src/tests/Metalama.Backstage.Tests/Metalama.Backstage.Tests.csproj:4,34`,
    `Metalama.Backstage.Testing.csproj:4`, `Metalama.Testing.Hooks.Tests.csproj:4` (`netframework4.7.2;net10.0`),
    `Metalama.Backstage.Commands.Tests.csproj:4`, `Metalama.Backstage.Worker.Tests.csproj:4` (`net10.0`)
  - `Metalama.LinqPad/src/tests/Metalama.LinqPad.Tests/Metalama.LinqPad.Tests.csproj:5` (`net10.0-windows`)
  - `Metalama.Patterns/src/tests/Metalama.Patterns.Caching.AspectTests/Metalama.Patterns.Caching.AspectTests.csproj:5`
    and the other Patterns and Extensions test projects
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8-13`
  - `eng/src/Program.cs:26,35-37`
  - `Metalama.Framework/docs/platform-support.md:211-212`, `Metalama.Framework/docs/testing.md:40-52`, `:121`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:46,58-61,103-107,108-115,136,178-179`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:76,97-117`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:367-369`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/AspectTestRunner.cs:239`
  - `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/MetalamaExtensionAssemblies.props:5-11`,
    `Metalama.Framework/src/Metalama.Extensions.DiffEngine/MetalamaExtensionAssemblies.props:5-14`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-23`
  - `Metalama.Framework/Directory.Build.props:31,45-46`, `Metalama.LinqPad/Directory.Build.props:11`
  - `Metalama.Framework/src/tests/Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:38-48`
- What happens today: no file in the repository declares a `net11.0` target framework. A search of the build files
  for the string returns exactly one hit, and it is the comment of
  `SupportedPlatform.TestedTargetFrameworks.csproj:8-10`, which states that `net481`, `net11.0` and
  `net11.0-windows` are in the tested matrix but are not listed because the build agents do not have their targeting
  packs, citing issue #1884. `net11.0` is a supported user target framework (`platform-support.md:211-212`) and
  `Metalama.Framework.props:30` admits it, so a user project on `net11.0` builds without `LAMA0600`; no automated
  test executes it. The absence is a consequence of UT-1: the .NET 10 SDK rejects a `net11.0` project, so the leg
  cannot be built on an agent until the container carries the .NET 11 SDK.
- The mechanisms that a new leg needs already exist. The `.5.0.0` shims inherit the target frameworks of the base
  project because they import its project file (`testing.md:40-52`); the aspect test discovery records
  `TargetFramework` and `TargetFrameworks` per leg; `@TargetFrameworks` skips a test outside its list
  (`TestInput.cs:97-117`); extension assemblies are filtered with `IsTargetFrameworkCompatible`, so the `net10.0`
  assets of `Metalama.Extensions.HtmlWriter` and `Metalama.Extensions.DiffEngine` serve a `net11.0` leg, and they
  pass the run-time filter as well, because `TargetedAssemblyReference` hard-codes `net10.0` for every runtime that
  is not .NET Framework; program execution is gated by `NET5_0_OR_GREATER`; and every `@RequiredConstant` gate that
  names a symbol of the form `NETx_y_OR_GREATER` or `NETCOREAPPx_y_OR_GREATER` is satisfied on `net11.0`, because
  the .NET SDK emits every symbol of that series for a `net11.0` compilation. The two verification passes counted those
  gates slightly differently, 128 of 140 and 131 of 139 occurrences, and neither count changes the conclusion; the
  remaining gates name `NETFRAMEWORK`, a Roslyn variant symbol or `DEBUG` and are unaffected.
- Three details need attention. The expected `.t.cs` and `.t.txt` files are shared by all legs and are compared per
  leg under `obj/transformed/<tfm>`, so any output that depends on the .NET 11 base class library must be split with
  `@TargetFrameworks`; one verified source of such a difference is that the .NET 11 SDK adds `System.Net.Http.Json`
  to the implicit usings for a target framework version of 11.0 or greater, and the aspect test framework passes the
  generated global usings file of the test project into every test compilation, so the difference reaches the test
  compilations of projects that enable implicit usings, such as `Metalama.Patterns.Caching.AspectTests.csproj:5`, and
  not those of `Metalama.Framework.Tests.AspectTests.csproj`, which disables them at line 23. Next,
  `Metalama.Backstage.Tests.csproj:34` conditions the `Microsoft.AspNetCore.App` framework reference on an exact
  comparison with `net10.0` and needs a `net11.0` branch. Finally, `Metalama.Framework/Directory.Build.props:31` and
  `Metalama.LinqPad/Directory.Build.props:11` disable the code quality analyzers on every leg other than `net10.0`,
  which is acceptable for a second Core leg but should be a deliberate choice.
- A `net11.0` leg adds no C# 15 coverage. The language version does not follow the target framework here, because
  `Metalama.Framework/Directory.Build.props:45-46` pins the maximum language version to 14.0 and the other solutions
  inherit that value.
- Consequence: no impact today; a coverage gap for a supported platform, which cannot be closed before the build
  container moves to the .NET 11 SDK.
- Proposed change: after UT-1, add `net11.0` to the five Framework test projects, to the Patterns and Extensions
  aspect and unit tests, and to `Metalama.Backstage.Tests` with the framework reference condition widened; run once
  and split the snapshots that differ with `@TargetFrameworks`. Keep `Metalama.Framework.Tests.Workspaces` and the
  benchmarks on `net10.0` unless UT-13 is settled. Remove the exclusion comment of
  `SupportedPlatform.TestedTargetFrameworks.csproj:8-10` in the same change, which is UT-6. Do not add
  `NET11_0_OR_GREATER` to the hard-coded constant list of `MainViewModel.cs:38-48` on its own: that list is passed
  alongside two `net10.0` strings that stand for the simulated target framework, and `TestInput.cs:97-117` compares
  `@TargetFrameworks` against that same value, so defining the symbol while the workbench still identifies itself as
  `net10.0` would run `net11.0` tests under a `net10.0` identity. Either leave the workbench on `net10.0`, or move
  the list and the two strings together. Expect the Core leg of the continuous integration run to grow
  substantially, and the `Roslyn.5.0.0` variants to grow as well, because the shims inherit the list.
- Size: medium for the project files, large if many snapshots diverge, and blocked until the container carries the
  .NET 11 SDK.
- Status: new work. Not implemented, not in progress and not tracked; none of the sixteen sub-issues of #1921
  concerns a test target framework leg. Related: #1876, which set the current single Core leg; #1884, which made
  `net11.0` a declared supported platform with no executing test; #1894, the precedent for a follow-up story that
  acts once the target frameworks move.
- Verification: the code pass confirmed the complete inventory of target frameworks, confirmed that no `net11.0`
  string exists in any build file, and found the recorded reason and the container prerequisite that the original
  report omitted; the semantics pass confirmed that the .NET SDK defines every `_OR_GREATER` symbol of the series for
  `net11.0`, that `net11.0` is not filtered out of the SDK target framework list, and that
  `IsTargetFrameworkCompatible` selects the `net10.0` extension assets, and it refuted the illustration about a new
  `System.Console.WriteLine` overload, replacing it with the implicit-usings difference above; the scope pass
  confirmed that no pull request and no issue proposes the leg.
- Open questions: whether both Core legs, or only `net11.0`, should run in the `.5.0.0` variants.

### UT-6. `SupportedPlatform.TestedTargetFrameworks` deliberately omits `net11.0`

- Where:
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8-10`
    (the comment), `:13` (the target framework list),
    `Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/test.json`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:31`,
    `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:349-352`
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.ContributedRequirements/SupportedPlatform.ContributedRequirements.csproj:15,25-28`
  - `eng/src/Program.cs:26,35-37,50-55`
- What happens today: the project comment states that `net481`, `net11.0` and `net11.0-windows` are also in the
  tested matrix but are not listed, because the build agents do not have their targeting packs. That reason is
  accurate: `eng/src/Program.cs:26,35-37` install one .NET SDK, and the Visual Studio Build Tools component list at
  `:50-55` carries the 4.7.2 and 4.8 targeting packs only. The scenario builds the six target frameworks listed at
  `:13` and its `test.json` forbids `LAMA0600`, `LAMA0601` and `LAMA0602`, so it proves that none of the six reports
  any of the three diagnostics. The `LAMA0600` rule itself is already exercised, by the contributed requirement
  `Test.NewerFramework` of `SupportedPlatform.ContributedRequirements`, whose `MaximumNETCoreAppVersion` is 9.0
  against a `net10.0` project. What is not exercised is the value declared for `Metalama.Framework` itself at
  `Metalama.Framework.props:31`: no scenario in the repository builds a `net11.0` project, so nothing proves that a
  target framework equal to the declared maximum passes the comparison without a warning.
- Consequence: no impact; a coverage gap.
- Proposed change: add `net11.0;net11.0-windows` to the target framework list at `:13` once UT-1 is done, and rewrite
  the comment at `:8-10` in the same edit so that it names `net481` only; leaving it unchanged would make the file
  state that two target frameworks are not listed while they are listed. The reason for omitting `net481` stands on
  its own, because no 4.8.1 targeting pack is installed, and adding it needs that pack rather than the .NET 11 SDK.
  The `test.json` needs no change for the target frameworks themselves. One caveat belongs to UT-2 rather than to
  this scenario: `test.json` forbids `warning LAMA0601`, so if UT-1 also pins `global.json` to an 11.0.x SDK, this
  scenario fails for the SDK comparison described in UT-2, independently of the target frameworks added here.
- Size: small.
- Status: new work. The omission is untracked, because the issue that the comment links to, #1884, is closed as
  completed, and none of its nine comments defers these rows. Related: #1876, the source of the matrix values;
  #1902, the precedent for changing the build agent image.
- Verification: the code pass confirmed the comment, the list, the `test.json` and the container component list, and
  corrected the claim that the `LAMA0600` rule is unexercised; the semantics pass was not engaged, because the
  finding carries no external premise; the scope pass confirmed that no issue tracks the omission and noted that the
  acceptance criteria of #1884 also name `net481`.
- Open questions: none.

### UT-7. Standalone scenarios run on `net10.0` only, except the three that follow the SDK

- Where:
  - `Metalama.Framework/src/tests/Standalone` and `Metalama.Framework/src/tests/DesignTimeStandalone`: 88 occurrences
    of `<TargetFramework>net10.0</TargetFramework>` (69 and 19) and four of `<TargetFrameworks>net10.0</TargetFrameworks>`
    (`Standalone/Issue30439/Issue30439.csproj:4`,
    `Standalone/IAsyncEnumerableNetStandard/IAsyncEnumerableNetStandard.csproj:4`,
    `Standalone/WorkspaceTest/WorkspaceTest/WorkspaceTest.csproj:5`, `Standalone/Issue30411/Issue30411.csproj:4`)
  - `Metalama.Framework/src/tests/Standalone/BlazorApp/BlazorApp.csproj:8`,
    `Metalama.Framework/src/tests/Standalone/Issue1741/Issue1741.csproj:10` (`net$(NETCoreAppMaximumVersion)`),
    `Metalama.Framework/src/tests/Standalone/SingleFile/Program.cs:1` (a file-based program with no project file)
  - `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DotNet/DotNet.csproj:3` (a stale comment), `:6`
    (`net10.0`), `:10-11` (the `LangVersion` reset),
    `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DotNetFramework/DotNetFramework.csproj:6,11`
  - `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/TemplateLanguageVersion14.csproj:4,10`
  - `Metalama.Framework/Directory.Build.props:45-46`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:117-121`, `:243-249`
  - `Metalama.Framework/docs/testing.md:230,232`
  - External: `dotnet/roslyn` `src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets:23-32`, `dotnet/sdk`
    `src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.TargetFrameworkInference.targets:193-198`
- What happens today: the .NET Core leg of the standalone scenarios is `net10.0`, in 92 target framework
  declarations spread over the 69 scenario directories of `Standalone` and the 7 of `DesignTimeStandalone`. A
  minority of scenarios deliberately target something else, because that is what they test: five projects target
  `net8.0` and one multi-targets `net462;net8.0;net9.0` for the platform checks, one targets `net48`, one `net472`,
  three `net10.0-windows` and 25 `netstandard2.0`. Three scenarios follow the SDK instead of naming a version:
  `BlazorApp` and `Issue1741` through `net$(NETCoreAppMaximumVersion)`, and `SingleFile`, which is a file-based
  program with no project file. Under a build whose `global.json` pins the .NET 11 SDK those three become `net11.0`
  by themselves, which `Metalama.Framework.props:31` accepts, and every other scenario keeps the version written in
  its project file.
- No standalone scenario observes the implicit C# 15 language version, and `DefaultLanguageVersion/DotNet` is not an
  exception. The implicit language version follows the target framework and not the SDK:
  `Microsoft.CSharp.Core.targets` computes it from the project target framework, caps it at the maximum the compiler
  knows, and applies it only when `LangVersion` is empty, and `Metalama.Framework.targets:115` records that the
  property Metalama reads is set by Metalama.Compiler according to the current target framework. `DotNet.csproj` is
  pinned to `net10.0`, so its implicit version stays at 14.0, which the allowed list at `:118` accepts, and neither
  the rewrite to 12.0 nor the warning occurs. The three self-adjusting scenarios cannot observe the rewrite either,
  because they inherit an explicit `LangVersion` of 14.0 from `Metalama.Framework/Directory.Build.props:46`; only the
  two `DefaultLanguageVersion` scenarios clear that inherited value, and both are pinned to a fixed target framework.
- The `Matrix` and `Properties` entries of `test.json` (`testing.md:230,232`) can build a scenario once per entry,
  and no `test.json` in this repository uses either field today. A `net11.0` matrix entry fails with the SDK error
  `UnsupportedTargetFrameworkVersion` on an agent whose resolved SDK is .NET 10, rather than being skipped.
- Consequence: no impact today. The coverage gap is wider than the original report stated, because no standalone
  scenario will observe the C# 15 default under the .NET 11 SDK.
- Proposed change: add `net11.0` coverage to the subset of scenarios that exercise runtime behaviour. A two-entry
  matrix that passes `TargetFramework` as a property works for the single-project scenarios `CodeCoverage` and
  `WriteHtml` only; it is not usable for `CompileTimeContract`, `TestWeaver`, `Issue1749`,
  `Issue1749.PublicKeyVariants` and `Issue1749.SameAssemblyIdentity`, because a global property applies to every
  project of the solution and those solutions deliberately hold `netstandard2.0` projects. For those, give the .NET
  projects a target framework driven by a scenario-level property with a `net10.0` default, or multi-target them.
  `CompiledBindingsWpf` needs `net11.0-windows`, not `net11.0`, and `SingleFile` needs nothing. Guard the `net11.0`
  entry so that it is skipped when the resolved SDK is not .NET 11. Separately, to obtain an early detector of the
  C# 15 default, add a `DefaultLanguageVersion` sibling that targets `net11.0` and clears `LangVersion` in the same
  way, since neither the existing scenario nor the three SDK-following ones can observe it, and extend the allowed
  value list at `Metalama.Framework.targets:118` with 15.0 at the same time as the maximum language version is
  raised, so that a `net11.0` user project is not silently rewritten to C# 12. Update the comment of
  `DotNet.csproj:3`, which still refers to a .NET 8 project and to C# 12.
- Size: medium.
- Status: new work. No `test.json` in the repository uses `Matrix` today, so this would be its first use. Related:
  #1876, which produced the current single-target state; #1884, which declared `net11.0` supported.
- Verification: the code pass corrected the count, refuted the claim that `DefaultLanguageVersion/DotNet` becomes an
  early detector, and showed that the proposed matrix does not work for the multi-project scenarios; the semantics
  pass confirmed the language-version derivation from the compiler toolset targets, confirmed that
  `net$(NETCoreAppMaximumVersion)` resolves to `net11.0` under the .NET 11 SDK, and added the SDK error that a
  premature matrix entry would produce; the scope pass confirmed that no pull request and no issue covers it.
- Open questions: whether the `ManySolutions` scenario passes `Properties` to `dotnet test` as well as to
  `dotnet build`.

### UT-8. The design-time host simulator and the workspace loader select the SDK by the runtime major version

- Where:
  - `eng/src/DesignTimeSolution.cs:42`, `:60-68`, `:122-146`
  - `eng/src/Program.cs:26,35-37,61`
  - `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/Metalama.DesignTime.HostSimulator.csproj:6`,
    `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/MSBuildEnvironment.cs:49-52`,
    `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/SimulateCommand.cs:34-37`,
    `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/SimulateCommandSettings.cs:55-60`
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:83-87`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs:283`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:5-8,18`
  - `Metalama.Framework/src/tests/DesignTimeStandalone/Issue1744/Issue1744.csproj:5`
  - `Metalama.Framework/src/Metalama.Tool/Metalama.Tool.csproj:5,37`,
    `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj:23`
- What happens today: `DesignTimeSolution.cs:42` fixes the simulator target framework to `net10.0`, and the simulator
  project targets `net10.0`. Every project of every design-time scenario targets `net10.0` or `netstandard2.0`, so no
  design-time scenario exercises a `net11.0` project. The reason is not the SDK selection rule of the simulator:
  `MSBuildEnvironment.cs:49-52` does select the highest SDK whose major version is at most the runtime major version,
  but `SimulateCommand.cs:34-37` calls it only under an option that is off by default, because the workspace
  evaluates projects in an out-of-process build host that locates MSBuild itself, and `DesignTimeSolution` never
  passes that option. The operative constraint is the pinned SDK of UT-1: the restore at `DesignTimeSolution.cs:60-68`
  and the preparatory build would fail on a `net11.0` project before the simulator ran.
- The same rule is live in `MSBuildInitializer.cs:83-87`, reached from `Workspace.cs:283`, and
  `Metalama.Framework.Workspaces.csproj:18` is `net10.0` only because
  `Microsoft.CodeAnalysis.Workspaces.MSBuild` ships assets for `net10.0` and `net472` only. That constraint survives
  the renumbering to the stable Roslyn: the workspaces project of `dotnet/roslyn` `main` still targets the .NET
  version that its target framework properties name, which is `net10.0`. The loader therefore selects a .NET 11 SDK
  only in a process that runs on .NET 11, which is the case for a user application targeting `net11.0` that consumes
  the `net10.0` asset. `Metalama.Tool` targets `net10.0` with `RollForward=Major`, and that policy rolls forward to a
  higher major version only when no runtime of the requested major version is installed, so on a machine that has
  both runtimes the tool runs on .NET 10; the LINQPad driver has no roll-forward setting at all and runs inside the
  LINQPad process.
- Consequence: a coverage gap for design-time scenarios on `net11.0`, and a possible functional limitation of
  `Metalama.Tool` and `Metalama.LinqPad` on `net11.0` projects when the tool process runs on the .NET 10 runtime.
- Proposed change: add the .NET 11 SDK to the build container and to the generated `global.json` first; without it no
  `net11.0` scenario can be restored, whatever the simulator targets. Then add a design-time scenario containing a
  `net11.0` project. Whether the simulator itself needs a `net11.0` target has to be decided from the runtime of the
  out-of-process build host rather than from the simulator process, because the SDK registration is not active in the
  default configuration. `RollForward=Major` on a single `net10.0` simulator target is not a sufficient alternative,
  because that policy keeps the process on .NET 10 whenever a .NET 10 runtime is installed. Separately, re-examine
  the selection rule of `MSBuildInitializer.cs:84` for `Metalama.Tool` and `Metalama.LinqPad`, and measure whether
  the process-wide environment variables that the MSBuild locator sets constrain the out-of-process build host,
  before relaxing it. Keeping `Metalama.Framework.Workspaces` at `net10.0` is correct and should not change.
- Size: medium, and blocked on UT-1.
- Status: new work. Not implemented, not in progress and not tracked; the three open pull requests and the merged
  ones do not touch the simulator or the workspace loader for this purpose. Related: #1881, which fixed the
  workspaces project on `net10.0` for the package asset reason recorded in its project file; #1876; #1884.
- Verification: the code pass established that the SDK registration of the simulator is inactive by default and that
  the pinned SDK is the operative constraint, and refuted the statement about the tools reaching .NET 11 through
  roll-forward; the semantics pass confirmed the package asset layout of the stable and the prerelease Workspaces
  package and of `dotnet/roslyn` `main`, confirmed the roll-forward semantics from the .NET documentation, and added
  the SDK error that blocks the scenario before the simulator runs; the scope pass confirmed that no issue covers it.
- Open questions: the MSBuild version that the .NET 11 SDK loads into a process compiled against `Microsoft.Build`
  18.0.2, which is UT-13.

### UT-9. `DoubleTests` in the Contracts unit tests never compiles

- Where:
  - `Metalama.Patterns/src/tests/Metalama.Patterns.Contracts.UnitTests/DoubleTests.cs:5,13,23,38`
  - `Metalama.Patterns/src/tests/Metalama.Patterns.Contracts.UnitTests/Metalama.Patterns.Contracts.UnitTests.csproj:4`
  - `Metalama.Framework/src/Metalama.Framework/Aspects/ContractAspect.cs:71`
  - `Metalama.Patterns/src/tests/Metalama.Patterns.Contracts.UnitTests/Utilities/FloatingPointHelper.cs:15-25`
- What happens today: `DoubleTests.cs:5,13` uses the exact symbol `#if NET6_0` while the project targets
  `net472;net10.0`. The .NET SDK defines `NET6_0` only for an exact `net6.0` target, and no property group defines it
  artificially: no `DefineConstants` property in the repository adds `NET6_0`, in this project or anywhere else. The
  guarded region `DoubleTests.cs:13-43` holds the only two `[Fact]` methods of the class, so on both target
  frameworks the class compiles as an empty class, xunit discovers no test in it, and no diagnostic is emitted.
  Within this repository the guard has never matched: the file entered the repository in a commit where the project
  already targeted `net472;net8.0`, and the leg later moved to `net10.0`. Adding `net11.0` to the target framework
  list would not change this. The title of the finding is a simplification: the class compiles, and what never
  compiles is its body.
- Consequence: silently missing test coverage. The two facts are excluded on every target framework, no diagnostic is
  emitted, and the test run reports no failure and no skipped test.
- Proposed change: replace `NET6_0` with `NET6_0_OR_GREATER` at `DoubleTests.cs:5` and `:13`, then run the tests and
  adopt the result. Removing the guard is not an option: `Math.BitDecrement` and `Math.BitIncrement` at `:23` and
  `:38` were introduced in .NET Core 3.0 and .NET Standard 2.1 and are absent from .NET Framework 4.7.2, which is the
  other target framework. The sibling helper `FloatingPointHelper.cs:15-25` computes its floating-point step by
  multiplication for the same reason. Read the result rather than adopting it without inspection: the two facts have
  never run in this repository, and the closed issue #536 is the precedent for a boundary-precision test of the same
  suite that passed on .NET Framework and failed on .NET.
- Size: small.
- Status: new work. Not implemented, not in progress and not tracked; the file has not been edited since it entered
  the repository, and the change that moved the leg to `net10.0` did not revisit the guard. Related: #1876; #536.
- Verification: the code pass confirmed the guard, the target frameworks, the absence of any artificial define and
  the two methods that the guard hides, and corrected the title and the alternative of removing the guard; the
  semantics pass was not engaged, because the finding carries no external premise beyond the availability of two base
  class library methods; the scope pass confirmed that no issue and no pull request covers it.
- Open questions: none.

### UT-10. Two aspect tests are gated on a preprocessor symbol that no variant defines

- Where:
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Introductions/InterfaceImplementation/Operator.cs:6-7,10,21`
    and `Operator_Explicit.cs:6-7,10,21`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14,53`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:17,21,26,46,58-61`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:508-526,609-610`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:74-83`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestExecutor.cs:309-311`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Introductions/Interfaces/IntroduceMethodStaticAbstract.cs:5-9`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:10`, `eng/RoslynVersions/Roslyn.5.0.0.props:8-10`,
    `Directory.Packages.md:211-221`, `:219`
- What happens today: the symbol `ROSLYN4_4_OR_GREATER` is misspelled relative to the former
  `ROSLYN_4_4_0_OR_GREATER`, which the two variant property files defined until the commit that renumbered the latest
  variant removed it. The misspelled form was never defined anywhere: a search of the whole history finds the string
  only in these two test files. Both tests have therefore been skipped in every variant and on both target framework
  legs, before as well as after the pruning campaign recorded at `Directory.Packages.md:219`, which did not catch
  them because the spelling did not match. The skip is produced by `TestInput.cs:74-83`, which compares the
  `@RequiredConstant` values against the `DefineConstants` of the test project published as assembly metadata, and
  `TestExecutor.cs:309-311` reports the result as a skipped test whose reason names the undefined constant.
- Consequence: lost test coverage. Two tests never run. The condition is reported, because each run lists them as
  skipped with a reason, but no test fails.
- Proposed change: remove the misspelled Roslyn symbol and keep a target framework gate. The test source files are
  compiled into the test project itself, which targets `net48` as well as `net10.0`, and both files declare a static
  abstract operator in an interface, which .NET Framework does not support, so a target framework gate is still
  required. The two files are inconsistent today, because the directive at `:6` names `NET6_0_OR_GREATER` while the
  conditional at `:10` names `NET8_0_OR_GREATER`. Follow the convention of the three sibling static abstract tests:
  keep `// @RequiredConstant(NET6_0_OR_GREATER)` and write the conditional as `#if NET6_0_OR_GREATER`, so that the
  directive and the conditional name the same symbol. Then run the two tests on the `net10.0` leg and accept the
  output; expected files already exist beside them and may need to be re-accepted. A low-cost guard against a
  recurrence is a check that every `@RequiredConstant` value is a symbol that some configuration defines.
- Size: small.
- Status: new work. The gates remain from the closed issue #1881, whose cleanup searched for the `ROSLYN_*`
  naming convention and could not match a spelling without underscores.
- Verification: the code pass confirmed the two files, the skip mechanism, the variant symbol set and the history of
  the misspelling, and corrected the proposal to keep the target framework gate; the semantics pass was not engaged,
  because the finding carries no external premise; the scope pass confirmed that no pull request and no issue covers
  it, and identified UT-9 as the sibling defect of the same class.
- Open questions: none.

### UT-11. Runtime dependency pins for the self-hosted bucket and the design-time cap

- Where:
  - `Directory.Packages.props:52-73` (the `*LatestVersion` values, the `SystemTextJsonVersion` fallback at `:63` and
    `SystemTextJsonMinVersion` at `:66`), `:83` (the central `System.Text.Json` version)
  - `eng/RoslynVersions/Roslyn.5.0.0.props:11-12`, `eng/RoslynVersions/Roslyn.5.10.0.props:11-12`
  - `Directory.Packages.md:44-46`, `:48`
  - `Metalama.Framework/docs/platform-support.md:144-157`
  - `Metalama.Patterns/src/tests/Directory.Build.props:17-19`
  - `Metalama.Framework/src/Metalama.Framework.Package/Metalama.Framework.Package.csproj:29-30`,
    `Metalama.Patterns/src/Metalama.Patterns.Immutability/Metalama.Patterns.Immutability.csproj:31-35`,
    `Metalama.Patterns/src/Metalama.Patterns.Observability/Metalama.Patterns.Observability.csproj:31-35`,
    `Metalama.Framework/src/Metalama.Framework.DesignTime.Contracts/Metalama.Framework.DesignTime.Contracts.csproj:33,36,42-43`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.csproj:7,12,25,28-29`,
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/Metalama.Testing.UnitTesting.csproj:6,9,36-38`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:5,65,68`
- What happens today: the per-variant `System.Text.Json` pins are floors imposed by the Roslyn packages on their
  `netstandard2.0` assets. `Microsoft.CodeAnalysis.CSharp.Features` 5.0.0 declares `System.Text.Json` 9.0.0 in its
  `net8.0` and `.NETStandard2.0` groups and none in its `net9.0` group, and Features 5.9.0, the closest public
  package to the consumed prerelease, declares 10.0.1 in its `.NETStandard2.0` group only. The design-time cap
  therefore stays on the 10.0 line for the latest variant and on the 9.0 line for the Roslyn 5.0 variant, and it
  survives the renumbering of the latest variant to 5.12: `dotnet/roslyn` `main` is version 5.12, its private-runtime
  target framework is still `net10.0`, and it pins `System.Text.Json` 10.0.10. Neither line moves to 11.x for
  2027.0. The value is not confined to the design-time payload, because `Directory.Packages.props:83` declares the
  central `System.Text.Json` version from the same property and `Metalama.Framework.Engine.csproj:5,68` imports a
  variant property file and references the package with no override.
- Three .NET SDK behaviours that the original report attributed to .NET 11 are already present in the .NET 10 SDK
  and unchanged. Package pruning removes the framework-provided packages from the graph of .NETCoreApp 10.0 and later
  target frameworks only, and leaves the `netstandard2.0` and `net472` assets alone; the transitive audit default
  follows the same boundary, with `NuGetAuditMode` defaulting to `all` for .NETCoreApp 10.0 and later and to `direct`
  everywhere else, so the `direct` override at `Metalama.Patterns/src/tests/Directory.Build.props:19` is live for the
  `net10.0` and `net11.0` legs and must not be removed, only its comment rewritten; and `net8.0` is not an end-of-life
  target framework in either SDK today. The user-surfacing 8.0.x line remains unjustified, as
  `Directory.Packages.md:44-46` already records, and .NET 11 adds no new constraint to it.
- The `*LatestVersion` values feed the test projects and also two shipped packages, `Metalama.Testing.AspectTesting`
  and `Metalama.Testing.UnitTesting`, whose dependencies reach a user test project; the `net10.0` assets are pruned
  on a .NET 11 consumer, so the values can stay at 10.0.x.
- Consequence: no impact. The corrections strengthen this, because the two SDK behaviours attributed to .NET 11 are
  .NET 10 behaviours and the third is absent from the .NET 11 SDK sources today.
- Proposed change: no version bump for .NET 11. Once the .NET 11 SDK is in the container, run
  `dotnet list package --vulnerable --include-transitive` on each top-level solution, and remove the three
  `GHSA-8g4q-xg66-9fp4` suppressions if the report confirms that they are no longer needed, which is likely because
  the minimum Roslyn API version of 5.0.0 already forces the transitive `System.Text.Json` to at least 9.0.0. Correct
  the inverted pruning direction and the stale package names in the comments of
  `Metalama.Patterns.Immutability.csproj:31-34` and `Metalama.Patterns.Observability.csproj:31-34`, and name the
  audit-mode default as the actual difference
  between a `net8.0` and a `net10.0` leg. Rewrite the comment of `Metalama.Patterns/src/tests/Directory.Build.props:18`
  without removing the property. Record in `Directory.Packages.md` that the `SystemTextJsonVersion` of a Roslyn
  variant is the floor required by the `Microsoft.CodeAnalysis.CSharp.Features` package of that variant, raised to
  the highest published patch, and that the Roslyn packages declare the dependency only in their `netstandard2.0`
  asset group, so the pin governs the `netstandard2.0` and `net472` assets and is inert for the others. Do not record
  that the value follows the private runtime of the hosts the variant serves: the 5.0.0 variant sits at 9.0.0 while
  the private runtime of the hosts it serves is `net8.0`.
- Size: small.
- Status: new work, except for one item. The audience paragraph of `Directory.Packages.md:44-46` and the
  user-surfacing 8.0 pins are scoped by the open issue #1903, which must be referenced rather than repeated. The
  documentation half of this finding overlaps UT-18 and belongs to the same single documentation pull request.
  Related: #1897 and its pull request #1912, which established the cap-derivation doctrine; #1876, which made several
  of these comments stale; #1881, which raised the minimum Roslyn API version.
- Verification: the code pass confirmed every pin and suppression, corrected the claim that the `*LatestVersion`
  values feed test projects only, and refuted the proposed documentation rule about the private runtime; the
  semantics pass read the package metadata of the Features packages, the NuGet targets of both SDK copies and the
  end-of-life targets of both SDK branches, and refuted the audit-mode and end-of-life claims; the scope pass
  confirmed that only the `Directory.Packages.md` audience paragraph is tracked, by #1903.
- Open questions: none. The advisory content of `GHSA-8g4q-xg66-9fp4` could not be read offline, which is why the
  audit run remains part of the proposed change.

### UT-12. Test infrastructure packages on a `net11.0` leg

- Where:
  - `Directory.Packages.props:161` (`xunit.runner.visualstudio` 2.8.2), `:165-166` (coverlet 6.0.4), `:168`
    (`Microsoft.NET.Test.Sdk` 17.14.1, with the comment naming Visual Studio 17.14), `:169`
    (`Microsoft.AspNetCore.TestHost` 8.0.20), `:203` (BenchmarkDotNet 0.15.8), `:212-213` (the two LINQPad packages)
  - `Metalama.Backstage/src/tests/Metalama.Backstage.Worker.Tests/Metalama.Backstage.Worker.Tests.csproj:4,16`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.Benchmarks/Metalama.Framework.Tests.Benchmarks.csproj:5-6,15`
  - `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj:6,26`,
    `Metalama.LinqPad/src/tests/Metalama.LinqPad.Tests/Metalama.LinqPad.Tests.csproj:5,12`
  - `Metalama.Framework/docs/platform-support.md:126`, `:127`, `:134`, `:139`, `:234-235`
- What happens today: every one of these pins is an unconditional `PackageVersion` item, and the only target
  framework condition in `Directory.Packages.props` applies to a different package, so no pin would fail to resolve
  on a `net11.0` leg. The assets of the packages target `net8.0` (`Microsoft.NET.Test.Sdk`,
  `Microsoft.TestPlatform.TestHost`, `Microsoft.AspNetCore.TestHost` and the highest library folder of
  BenchmarkDotNet), `net6.0` (`xunit.runner.visualstudio`), `netstandard2.0` (`coverlet.collector`) or no target
  framework at all; none carries a `netcoreapp3.1` asset, and none carries a `net10.0` asset. A `net11.0` leg
  resolves all of them, because NuGet selects the highest asset folder compatible with the project target framework
  and .NET 11 is compatible with every one of those folders; the absence of a `net10.0` asset is not what makes the
  leg work, and such a folder would simply be selected in preference to a lower one. `Microsoft.TestPlatform.TestHost`
  17.14.1 ships no runtime configuration of its own, so the test host runs under the runtime configuration of the
  test project, and neither of the two MSBuild files that the test platform packages contribute branches on the
  target framework, so there is no version gate for a `net11.0` leg to trip.
- Only three of the six pins reach a project that could gain such a leg. `Microsoft.AspNetCore.TestHost` is
  referenced only by `Metalama.Backstage.Worker.Tests`, which targets `net10.0` alone; BenchmarkDotNet only by the
  benchmark project, pinned to `net10.0` for a documented Roslyn asset reason; and the two LINQPad packages only by
  the LINQPad driver and its tests, both `net10.0-windows`. The comment on `Microsoft.NET.Test.Sdk` is stale, because
  PB-2027.0 drops Visual Studio 2022 in its entirety.
- Consequence: no impact expected on the `net11.0` leg. One independent action item remains, namely correcting a
  stale comment and re-deriving the pin once the November 2026 Visual Studio baseline ships.
- Proposed change: correct the comment. The rule it states is unchanged, namely the lowest supported Visual Studio,
  and only the value changes: under PB-2027.0 that is the Visual Studio 2026 long-term servicing baseline of
  November 2026, not Visual Studio 2026 in general, which already spans published versions 18.0 through 18.9. The
  `Microsoft.NET.Test.Sdk` version numbers track the Visual Studio version numbers, and nuget.org publishes that
  package only up to 18.9.0 today, so the version matching the November 2026 baseline is not yet available and the
  pin has to be set after that baseline ships, as part of the verification checklist of `platform-support.md`.
  Raising the pin is not required for the `net11.0` leg itself. The other four pins need no attention here.
- Size: small.
- Status: new work, and the comment edit belongs to the single documentation pull request of UT-18, which also names
  `Directory.Packages.props:168`; UT-11 lists the same line. The three should be merged so that the comment is edited
  once. Related: #1876, which made the comment stale; #1897 and its pull request #1912, which rewrote the
  neighbouring comment and left this one; #1903, which does not cover a test-only package.
- Verification: the code pass confirmed that every pin is unconditional, corrected the attribution of
  `Microsoft.AspNetCore.TestHost` and the scope of the six pins, and recorded that the repository holds no lock file
  from which the asset claim could be checked; the semantics pass unpacked the packages from nuget.org, read their
  asset folders and their MSBuild contributions, refuted the reason given for the leg working, and established that
  the test host carries no runtime configuration of its own; the scope pass confirmed that the pin and its comment
  are untouched and untracked.
- Open questions: whether the LINQPad 8 runtime hosts a `net10.0-windows` driver on .NET 11. That question is about
  LINQPad and is not raised by adding a `net11.0` user target framework leg.

### UT-13. `MicrosoftBuildVersion` 18.0.2 against the MSBuild of the .NET 11 SDK

- Where:
  - `Directory.Packages.props:35-50` (the rationale and the value), `:144-151` (the four pins bound to the property
    and the MSBuild locator pin)
  - `Directory.Packages.md:18`, `:20`, `:61-69`, `:309`
  - `Metalama.Framework/docs/platform-support.md:196-197`
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:5-18,61,69,75,76`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:57-95,116`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildLogger.cs:5-51`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs:344-386`,
    `Metalama.Framework/src/Metalama.Framework.Workspaces/WorkspaceProjectOptions.cs:71-85`
  - `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/Metalama.DesignTime.HostSimulator.csproj:31-32`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.Workspaces/Metalama.Framework.Tests.Workspaces.csproj:6-7,26-27`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.Benchmarks/Metalama.Framework.Tests.Benchmarks.csproj:28-29`,
    `Metalama.LinqPad/src/tests/Metalama.LinqPad.Tests/Metalama.LinqPad.Tests.csproj:35-36`
- What happens today: `Directory.Packages.props:35-50` states the rationale, which is that the lowest MSBuild that
  can host Metalama is the one of the .NET 10 SDK, and sets the property to 18.0.2; `Directory.Packages.md:18,20` and
  `:61-69` document the derivation. The property feeds four central pins, and the projects that take a direct
  reference at that version are the workspaces project, the host simulator, the workspaces tests, the benchmarks and
  the LINQPad tests. The engineering project is not one of them: it declares a single package reference, to
  PostSharp.Engineering, and central package management pins only direct references. The API surface is compiled
  against 18.0.2 and the run-time assets of the MSBuild packages are excluded, so the assemblies are loaded from the
  directory that the MSBuild locator registers. `Microsoft.Build` keeps a frozen assembly version of 15.1.0.0 across
  the whole 17.x and 18.x lines, measured on the 17.14.28, 18.0.2 and 18.9.6 packages for both the `net472` and the
  `net10.0` assets, so binding succeeds and only an API removal would break; no public member of the `Microsoft.Build`
  or `Microsoft.Build.Framework` namespace present in 18.0.2 has been removed in any published 18.x version up to
  18.9.6. The .NET 11 SDK carries MSBuild 18.12, which is not published on nuget.org, so the API surface of the exact
  SDK build cannot be verified offline.
- The selection rule of UT-8 bounds the exposure inside this repository. Every project here that consumes MSBuild
  targets `net10.0` or `net10.0-windows`, so the process runs on the .NET 10 runtime, an 11.x SDK is filtered out,
  and the MSBuild of the .NET 10 SDK is selected as before. The 11.x SDK is selected only for a user application that
  references the `net10.0` asset of `Metalama.Framework.Workspaces` and itself runs on the .NET 11 runtime.
- Consequence: no impact expected. Inside this repository there is no exposure at all; a user application on the
  .NET 11 runtime would see an API removal as a missing-method or type-load exception at the first project load.
- Proposed change: none. Keep the pin at 18.0.2, which is the lowest 18.0 published on nuget.org and remains below
  the MSBuild 18.12 of the .NET 11 SDK, per the existing policy of pinning to the lowest supported host. Once the
  .NET 11 SDK is in the container, run the workspaces tests and the LINQPad tests with both SDKs installed, which
  verifies that the selection still resolves the .NET 10 SDK, and separately load a `net11.0` console application
  against the workspaces package, which is the only configuration that loads the MSBuild of the .NET 11 SDK. Do not
  describe the follow-up as adding a `net11.0` leg to the two test projects: both declare a single target framework
  on purpose, and the workspaces project states that only .NET 10 is supported, so a second leg would require
  multi-targeting the product library first. Optionally correct the parenthetical of `Directory.Packages.md:309`,
  which states the frozen assembly version for the 17.x line only, to say 17.x and 18.x.
- Size: small, and it is a verification task rather than a code change.
- Status: new work. Not implemented, not in progress and not tracked. Related: #1876, which produced the current
  unconditional pin; #1881, which moved the affected components to `net10.0`; #1884, which declared the .NET 11 SDK
  inside the matrix while no test exercises it; #1897, whose forward-looking obligation covers a different package
  family.
- Verification: the code pass confirmed the pin, its consumers, the exclusion of the engineering project and the
  runtime-major cap that keeps the exposure outside this repository; the semantics pass established the MSBuild
  version of the .NET 11 SDK from the `dotnet/sdk` dependency manifest, measured the frozen assembly version across
  three package versions and diffed the public API of 18.0.2 against 18.9.6 with no removals; the scope pass
  confirmed that the pin is untouched and untracked, and that the finding is a verification item of the same story as
  UT-1, UT-5 and UT-12.
- Open questions: the API surface of the MSBuild build that ships in the .NET 11 SDK, which is not published on
  nuget.org. Note also that `MSBuildInitializer` constructs its instance by reflecting on a non-public constructor of
  the MSBuild locator, so any future change of that pin has to re-verify the constructor.

### UT-14. A union declaration is a struct for every pattern

This is the shared premise of the four sub-findings below, and it is verified rather than assumed.

Roslyn maps `DeclarationKind.Union` to `TypeKind.Struct` (`dotnet/roslyn`
`src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs:34-38`) and adds no `TypeKind` value, and a union
declaration is a plain struct rather than a record struct. Roslyn forbids the `ref` modifier on a union declaration
and allows `readonly` without implying it; inside a union declaration it forbids instance fields, auto-properties and
field-like events other than the backing field of the synthesized `Value` property (CS9373), public one-parameter
constructors other than the synthesized case constructors (CS9374), a member provider interface (CS9387), and an
explicitly declared constructor that does not chain through a `this(...)` initializer (CS9375); at least one case
type is required (CS9370). Metalama maps the Roslyn kind to `TypeKind.Struct` and reads `IsRecord` from the symbol
(`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79,173`), so, until
theme 03 adds anything, a union declaration is an ordinary non-record struct to every aspect.

There is no union case declaration. The case types are existing types listed in the parameter list of the union
declaration, parsed as parameters that carry a type and, normally, no identifier, so an aspect on a union case is an
aspect on that ordinary type.

Two qualifications apply to every sub-finding. First, a union declaration is not the only union: any class or struct
carrying `System.Runtime.CompilerServices.UnionAttribute` is a union type, so `ITypeSymbol.IsUnion` may be true for a
class or a record, and such a type takes its case types from the signatures of its creation members rather than from
a parameter list; the no-instance-state restrictions apply only to the declaration form. Second, the members that
identify a union are not uniformly available: in the Roslyn build the repository consumes today, `ITypeSymbol.IsUnion`
exists but carries the experimental marker and `ITypeSymbol.UnionCaseTypes` does not exist at all; both become
available without the marker only on the Roslyn line that is expected to ship as the stable 5.12, and neither exists
in the Roslyn 5.0 variant that serves Rider. Any pattern or engine code that names either member must therefore be
gated to the latest variant and waits on the renumbering.

Finally, none of the four sub-findings is reachable before the C# 15 enablement, because no Roslyn that Metalama
consumes exposes C# 15 as a non-preview language version and the supported language versions stop at C# 14.

#### UT-14a. `[Observable]` rejects unions with its existing message

- Where:
  - `Metalama.Patterns/src/Metalama.Patterns.Observability/ObservableAttribute.cs:45` (the attribute usage), `:52`
    (the eligibility rule)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:150`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Aspects/AspectInstance.cs:129-139`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Aspects/CompilationAspectSource.cs:113-128`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:44,69-79`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:69`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:10`
- What happens today: a union declaration is a struct, so the predicate `x.TypeKind is TypeKind.Class` at
  `ObservableAttribute.cs:52` is false and the aspect is ineligible. Applying `[Observable]` to a union produces two
  diagnostics and not one. The C# compiler reports CS0592, because the attribute is declared with
  `AttributeTargets.Class | AttributeTargets.Interface` at `:45` and that set does not cover a struct; Roslyn
  nevertheless keeps the bound attribute in the attribute bag, and Metalama discovers attributes from syntax, so the
  aspect instance is still created and the eligibility error `LAMA0037` is reported with the message
  `must be a class or a record class`. The reasoning that a union carries no observable state is sound, because its
  only synthesized state is the get-only `Value` property. One case is not covered by the rule: a class carrying the
  union attribute has `TypeKind.Class`, so it passes the rule and the aspect runs on it, treating the `Value`
  property as an ordinary property. Whether that form of union survives is an open question of the language
  proposal, whose normative text still permits it.
- Consequence: a diagnostic is reported, which is the intended outcome for the declaration form; two diagnostics
  rather than one, and the attribute-marked class form is not covered by the rule.
- Proposed change: no change in code. Add one aspect test in `Metalama.Patterns.Observability.AspectTests` whose
  expected output lists both CS0592 and `LAMA0037` on a union declaration carrying `[Observable]`, and a second case
  for a class carrying the union attribute, which is eligible and produces transformed code. The test cannot be
  written against the Roslyn version consumed today, so sequence it after the move to the stable Roslyn and the
  raising of the supported language version, and guard it with the Roslyn variant constant, because the Roslyn 5.0
  variant cannot parse a union declaration at all. Decide separately whether the attribute-marked class form deserves
  its own handling; the simplest option is to wait for the language proposal to close that question and to record the
  case as a known limitation meanwhile.
- Size: small for the declaration-form test once C# 15 is enabled. The attribute-marked class form is a separate open
  question and is not sized here.
- Status: new work. Not implemented, not in progress and not tracked; there is no C# 15 umbrella issue. Related:
  #1210 and its pull request #1219, the precedent for a test-only story that pins the behaviour of `[Observable]`
  against a new C# construct; #1039, the precedent umbrella of C# 14.
- Verification: the code pass confirmed the rule, the attribute usage, the eligibility computation and the two
  diagnostics, and found the attribute-marked class case; the semantics pass confirmed the union lowering and the
  restriction on instance state from the language proposal and the compiler sources, and established that the test
  is blocked on the language version; the scope pass confirmed that no issue and no pull request covers it.
- Open questions: none, apart from whether the language proposal retains the attribute-marked class form.

#### UT-14b. Immutability classifies a union as mutable, which produces Observability warnings

- Where:
  - `Metalama.Patterns/src/Metalama.Patterns.Immutability/ImmutabilityExtensions.cs:40-58` (the deep cases),
    `:80-88` (the `System` value types), `:90-93` (the read-only rule), `:95` (the fall-through to `None`)
  - `Metalama.Patterns/src/Metalama.Patterns.Immutability/ImmutabilityKind.cs:30-34`,
    `Metalama.Patterns/src/Metalama.Patterns.Immutability/ImmutableAttribute.cs:55-93`
  - `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/GraphBuildingContext.cs:74-84`,
    `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:112-129`,
    `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DiagnosticDescriptors.cs:93-99`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:169`
  - `Metalama.Patterns/src/tests/Metalama.Patterns.Observability.AspectTests/Diagnostics/ChildPropertyOfNonInpcProperty.cs:11-28`
- What happens today: `GetImmutabilityKind` returns `Deep` for the listed special types and for delegates, enums,
  pointers and function pointers, `Deep` for a non-reference type of the `System` namespace outside an exclusion
  list, `Shallow` when the named type is read-only, and `None` otherwise. `INamedType.IsReadOnly` is the Roslyn
  symbol property, which for a source struct is exactly the declared modifier, and Roslyn allows `readonly` on a
  union without implying it, so a union declared without the modifier is `ImmutabilityKind.None`.
  `GraphBuildingContext.IsDeeplyImmutable` delegates to that method and returns true whenever the kind is not `None`,
  so `Shallow` already counts as immutable there. In an `[Observable]` class, a computed property that reads a member
  of a union-typed property classifies the union-typed property as `ChainSection.Stem`, and
  `DependencyGraphBuilder.Visitor.cs:119-129` reports the warning `LAMA5161`, on the ground that the members of the
  union type are not observable. A union declared `readonly` avoids the warning today, which is the available
  workaround.
- The union declaration form satisfies the documented definition of shallow immutability, because its only instance
  field is the read-only backing field of the synthesized get-only `Value` property and user-declared instance
  fields, auto-properties and field-like events are compiler errors. That definition is about fields and
  auto-property setters and not about the type being declared `readonly`, which is what makes the classification
  accurate rather than merely convenient. Two qualifications: the absence of declared instance state does not by
  itself prevent in-place mutation, because in a struct that is not declared `readonly` an instance method may
  reassign the receiver and no union error code forbids it; and `ITypeSymbol.IsUnion` is also true for a hand-written
  class or struct carrying the union attribute, whose state is unconstrained.
- Consequence: a spurious warning is reported, reachable only once C# 15 and the stable Roslyn are enabled.
- Proposed change: classify only the union declaration form, and only after the move to the stable Roslyn. In
  `GetImmutabilityKind`, before the read-only test, return `ImmutabilityKind.Shallow` for a union declaration, and
  `Deep` when every case type is deeply immutable, treating an interface, a type parameter, a nullable value type or
  a nested union case type conservatively as not deeply immutable. Returning `Shallow` alone is already sufficient to
  remove the warning. Document in the member documentation that this rests on the definition of shallow immutability
  given by `ImmutabilityKind` and not on the type being declared `readonly`. Do not key the rule on
  `ITypeSymbol.IsUnion` alone, because that member is also true for an attribute-marked class or struct; the code
  model surface added by theme 03 must distinguish the declaration form and expose the case types. The branch cannot
  live in `Metalama.Patterns.Immutability`, which is compiled by the Roslyn of the host and is not built per variant,
  so it belongs in the engine. Add tests covering a read-only union, a union declaration without the modifier and a
  hand-written union class with a mutable field, the last of which must remain `None`.
- Size: medium, gated on both theme 03 and the move to the stable Roslyn; not actionable against the currently
  pinned build.
- Status: new work. Not implemented, not in progress and not tracked; the code model exposes no union surface for the
  pattern libraries to consult. Related: #1921, the meta-issue; #1881, which fixed the shipped variant set; #985, the
  open catch-all for later C# features in the template compiler, which does not cover this classification.
- Verification: the code pass confirmed the classification path, the warning path and the descriptor, corrected the
  cited line ranges, and established that the `Deep` half additionally needs a Roslyn member that only the stable
  line exposes; the semantics pass confirmed that `readonly` is neither implied nor forbidden on a union, confirmed
  the instance-state restrictions from the compiler sources and the language proposal, and added the two
  qualifications above; the scope pass confirmed that no issue and no pull request covers it.
- Open questions: none. The original question, whether the compiler implies `readonly` on unions, is answered in the
  negative for the sources read.

#### UT-14c. Caching keys and serializes a union as an opaque struct

- Where:
  - `Metalama.Patterns/src/Metalama.Patterns.Caching/Formatters/CacheKeyBuilder.cs:246-256`
  - `Metalama.Patterns/src/Flashtrace.Formatters/FormatterRepository.cs:107-117,139-153`,
    `Metalama.Patterns/src/Flashtrace.Formatters/Formatter.cs:28-31`,
    `Metalama.Patterns/src/Flashtrace.Formatters/Implementations/DefaultFormatterHelper.cs:14-20`,
    `Metalama.Patterns/src/Flashtrace.Formatters/Implementations/DefaultFormatter.cs:16,42,57-73,107-141`
  - `Metalama.Patterns/src/Metalama.Patterns.Caching.Aspects/ImplementFormattableAspect.cs:53-58`,
    `Metalama.Patterns/src/Metalama.Patterns.Caching.Aspects/CacheKeyAttribute.cs:54`
  - `Metalama.Patterns/src/Metalama.Patterns.Caching.Backend/Serializers/JsonCachingSerializer.cs:39-56,70,73-92`
  - `Metalama.Patterns/src/Flashtrace.Formatters/Flashtrace.Formatters.csproj:4`,
    `Metalama.Patterns/src/Metalama.Patterns.Caching.Backend/Metalama.Patterns.Caching.Backend.csproj:4`
- What happens today: `CacheKeyBuilder.AppendObject` boxes the argument and resolves a formatter by its run-time
  type; a struct without a registered formatter receives the default formatter, which declares itself as the default
  and therefore does not delegate. For every struct, the helper that decides whether a custom `ToString` exists
  compares the declaring type of the resolved method with `System.Object`, and for a struct that declares no
  `ToString` the resolved method is `System.ValueType.ToString`, whose declaring type is `System.ValueType`. The
  branch taken is therefore the one that calls `ToString` and wraps the result in braces, and
  `System.ValueType.ToString` returns the fully qualified type name. The key is consequently the type name for every
  value of the type. The compiler synthesizes, for a union declaration, only the `Value` property with its getter and
  backing field and one constructor per case type, and no `ToString`, `Equals` or `GetHashCode`; the union branch of
  the member synthesis returns before the record synthesis of those members. A `[Cache]` method with a union
  parameter therefore returns the entry of the first value for every other value, and `[InvalidateCache]`
  invalidates by the same collapsed key. The same collapse applies to a union-typed member marked `[CacheKey]`, which
  goes through the same formatter. For serializing backends, `JsonCachingSerializer` writes the runtime type name and
  the System.Text.Json rendering, and deserializes on the resolved type; because `Value` is get-only and a value type
  without a constructor attribute uses the default constructor, deserialization yields the default value of the union
  with a null `Value`.
- The behaviour is not specific to unions: every user-defined struct that does not override `ToString` already
  collapses to a single cache key, and no test asserts that behaviour. C# 15 unions make the case more frequent, because a
  union is a struct whose author expects the cache key to depend on the case value.
- Consequence: silent wrong output. No diagnostic is produced at build time or at run time, and the default formatter
  also catches an exception thrown by `ToString` and writes the name of the exception type in its place
  (`DefaultFormatter.cs:111-118`).
- Proposed change: recognize union types where the default formatter is chosen, that is in the two creation points of
  `FormatterRepository`, and return a formatter that formats `Value` through the repository. A registration in the
  caching repository builder is not sufficient, because the set of union types is not known in advance and the
  repository has no predicate-based registration. Two run-time markers are available and the open question of the
  original report is answered: the compiler synthesizes `System.Runtime.CompilerServices.UnionAttribute` on every
  union declaration, and a union declaration implicitly implements `System.Runtime.CompilerServices.IUnion`, which
  declares `object? Value { get; }`. A test of the form `value is IUnion` is the simpler discriminator and yields the
  case value without reflection. Both types belong to the .NET 11 base class library, while
  `Flashtrace.Formatters` and `Metalama.Patterns.Caching.Backend` target `net472`, `netstandard2.0` and `net10.0`, so
  neither can bind to them at compile time as those projects stand: either add a `net11.0` target framework, or match
  by full name at run time so that the same `net10.0` assembly behaves correctly when loaded into a .NET 11
  application. Add a JSON converter for union types in `Metalama.Patterns.Caching.Backend` that writes the case type
  and the value. The alternative, a build-time warning from the caching aspect when a parameter type is a union
  without `ToString`, rests on `ITypeSymbol.IsUnion`, which does not exist in the Roslyn 5.0 variant and is
  experimental until the stable line, so it would have to be gated and would report nothing under the variant that
  serves Rider. Add tests with the in-memory backend for the key collision and with the JSON serializer for the round
  trip.
- Size: medium.
- Status: new work. Not implemented, not in progress and not tracked; no union-aware code exists in the pattern
  libraries. Related: #1039, the precedent umbrella, since no C# 15 umbrella issue exists.
- Verification: the code pass confirmed the whole key path and the serializer, and corrected the identification of
  the branch taken inside the default formatter, which does not change the resulting key but does change where a fix
  may be placed; the semantics pass confirmed from the compiler sources on the stable line that a union synthesizes
  no object-method override, which removes the caveat of the original report, confirmed the deserialization
  behaviour from the System.Text.Json sources, and identified both run-time markers; the scope pass confirmed that no
  issue and no pull request covers it.
- Open questions: none. The original question, the name of the run-time marker attribute, is answered above.

#### UT-14d. Multicast treats unions as structs

- Where:
  - `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastImplementation.cs:166-178` (the type kind
    match), `:228-235` (the eligibility rule for structs), `:247-252` (the instance constructor rule), `:270,291,304,317`
    (the explicitly-declared rules), `:353,441` (the constructor selection), `:403,412,421,449,459` (the member
    filters)
  - `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs:20-40`,
    `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargets.cs:44,65`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SymbolBasedDeclaration.cs:52-57`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Override/OverrideConstructorAdvice.cs:32`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/ConstructorBuilder.cs:43-56`
- What happens today: a union declaration has `TypeKind.Struct` in the code model, so an assembly-level multicast
  aspect with `MulticastTargets.Struct` or `MulticastTargets.AnyType` selects it. For fields, properties, events,
  methods and parameters the selection filters on members that are not implicitly declared, and the synthesized union
  members are implicitly declared: the `Value` property overrides the flag to true, its getter is implicit because
  its syntax is the union declaration rather than an accessor declaration, and the synthesized case constructors
  inherit the flag from the synthesized method base class. The code model forwards that flag unchanged. Those targets
  therefore behave exactly as for a struct.
- Instance constructors are the exception. Their eligibility rule does not require an explicitly declared member, and
  the selection filter deliberately admits an implicitly declared constructor with no parameters. Roslyn adds such a
  constructor to every struct type that lacks one, and a union always lacks one, because every case constructor takes
  exactly one parameter. An assembly-level multicast aspect with `MulticastTargets.InstanceConstructor` therefore
  selects the implicit parameterless constructor of a union, and if the aspect overrides it, Metalama materializes it
  as an explicitly declared parameterless constructor, which the compiler rejects with CS9375, because a constructor
  declared in a union must chain through a `this(...)` initializer.
- Consequence: correct behaviour for type, method, property, field, event and parameter targets; invalid generated
  code for `MulticastTargets.InstanceConstructor` when the aspect overrides the implicit parameterless constructor of
  a union.
- Proposed change: add an aspect test in `Metalama.Extensions.Multicast.AspectTests` with a union and the type,
  method, property, field and parameter targets, asserting that the synthesized `Value` property, its getter and the
  case constructors are not selected. Add a second case with the instance constructor target, and either restrict the
  constructor selection or the eligibility rule so that the implicit parameterless constructor of a union is not a
  multicast target, or rely on a general rule in the engine that makes constructor advice ineligible on a union. The
  underlying defect, that materializing the implicit parameterless constructor of a union produces CS9375, is not
  specific to multicasting and belongs to the constructor advice work of theme 04; the multicast change is only the
  target filter. Adjusting the wording of the eligibility message at `:235` remains optional, because a union is
  lowered to a struct and the sentence is incomplete rather than false. The test cannot be written before C# 15 is
  enabled.
- Size: small for the target filter; the constructor advice half is sized by theme 04.
- Status: new work. Not implemented, not in progress and not tracked; the word "union" occurs in no C# file of
  `Metalama.Extensions`. Related: #1921; #1881, which fixes the variant set that a union-bearing compilation needs;
  #985.
- Verification: the code pass confirmed the type kind mapping, the member filters and the implicitly-declared status
  of every synthesized union member, which raises the original confidence from plausible to verified for that part;
  the semantics pass confirmed the same from the compiler sources and found the instance-constructor exception and
  its compiler error, which changes the consequence class; the scope pass confirmed that no issue and no pull request
  covers it.
- Open questions: none.

### UT-15. Closed classes reach no pattern-specific code

- Where:
  - `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:89,223,317,347,415`,
    `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicDesignTimeObservabilityStrategyImpl.cs:70,86,105,140`
  - `Metalama.Patterns/src/Metalama.Patterns.Caching.Aspects/ImplementFormattableAspect.cs:77`,
    `Metalama.Patterns/src/Metalama.Patterns.Caching.Aspects/CacheKeyAttribute.cs:54`
  - `Metalama.Patterns/src/Flashtrace.Formatters/FormatterRepository.cs:141`
  - `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributeInfo.cs:141-155,284-288`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMemberOrNamedType.cs:23,36-42`
- What happens today: a closed class is not sealed and is implicitly abstract. Roslyn reports an error for a closed
  class that is also sealed or static, and another for an explicit `abstract` modifier, and it adds the abstract
  modifier to every closed class; Metalama surfaces both flags directly from the symbol. Every read of `IsSealed` in
  the patterns therefore takes the non-sealed branch and introduces protected virtual members exactly as for any
  other unsealed class, which is valid, because a closed class may be derived within the same module of the same
  assembly and a derived class is not itself closed unless declared so. Conversely, every read of `IsAbstract` takes
  the abstract branch: the multicast abstraction filter classifies a closed class as abstract rather than
  non-abstract, and at run time the formatter repository selects the dynamic formatter for it. Both outcomes are the
  correct classification and are identical to the treatment of an ordinary abstract class. Roslyn exposes
  `ITypeSymbol.IsClosed` and the derived-type information, and no Metalama source references either. No pattern
  introduces a derived type, and aspect inheritance is unaffected, because the derived types of a closed class are in
  the same assembly.
- Consequence: no impact.
- Proposed change: no product change. As observation only, add one aspect test per pattern that introduces virtual
  members, that is `[Observable]` and `[CacheKey]`, on a closed class, so that the expected output stays under
  observation when the linker learns the modifier, and one multicast test that applies an attribute restricted to
  non-abstract targets to a closed class, to record that the closed class is filtered out as abstract. The generated
  partial parts of Metalama need not repeat the `closed` modifier, because the compiler merges the modifiers of
  partial parts. The tests cannot be added until C# 15 is available to the pipeline, so they must be sequenced after
  the language version work rather than scheduled on their own.
- Size: small, and blocked on the C# 15 enablement.
- Status: new work. Not implemented, not in progress and not tracked; no source in either repository references the
  closed API. Related: #626, which settled the convention that the `IsSealed` branches implement; #1039, the
  precedent umbrella; #985, the appropriate place to record that the template compiler needs nothing.
- Verification: the code pass confirmed every cited site, the flag forwarding and the absence of any reference to the
  closed API, and added the run-time formatter site that the original report omitted; the semantics pass confirmed
  from the language proposal and the compiler sources that a closed class is not sealed, is implicitly abstract and
  restricts derivation to the module, and that partial modifiers merge; the scope pass confirmed that no issue and no
  pull request covers it.
- Open questions: none.

### UT-16. Extension indexers: contracts and the not-null fabric depend on the code model shape

- Where:
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.Contracts.cs:22,33-78` (the field,
    property and indexer rules), `:87` (the explicitly-declared rule on the declaring member of a parameter),
    `:110-128` (the receiver-parameter predicate and rules), `:175-208` (the dispatch)
  - `Metalama.Patterns/src/Metalama.Patterns.Contracts/ContractExtensions.cs:31-33,49-51` (the type set), `:92-101`,
    `:104-112`
  - `Metalama.Patterns/src/Metalama.Patterns.Contracts/NotNullAttribute.cs:41-58`,
    `Metalama.Patterns/src/Metalama.Patterns.Caching/Fabric.cs:14`
  - `Metalama.Framework/src/Metalama.Framework/Code/IExtensionBlock.cs:11`,
    `Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs:187`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/ExtensionBlockImpl.cs:13-24`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:205-209,318-326`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/References/SymbolRef.Strategy.cs:224-225,298-301,315-316`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMethod.cs:169-180`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/CodeModelTests.CSharp14.cs:111-113,222-223,244-262`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/ExtensionMembers/`,
    in particular `ExtensionMembers_Contract_OnReceiver_Property.cs:17-23`
- What happens today: the contract eligibility rules for a field, a property or an indexer require the member to be
  writable or readable, explicitly declared, declared by a run-time type and not abstract, and the receiver-parameter
  rules apply to a parameter whose containing declaration is an extension block. The Caching fabric calls
  `VerifyNotNullableDeclarations`, which adds `[NotNull]` to the members of the property, field and indexer
  collections of each type and to the parameters of the method, constructor and indexer collections. C# 14 extension
  member tests exist only in the Framework suite, including contracts on an extension property and on the receiver
  parameter; a search of the Patterns and Extensions test trees for an extension block finds nothing.
- The open question of the original report is settled by the code, and the answer is negative. An extension member
  belongs to the extension grouping type, which the code model exposes as a separate `IExtensionBlock` reached only
  through `INamedType.ExtensionBlocks`; the indexer collection of a named type enumerates the members of the
  declaring symbol itself and accepts only property symbols, so an extension indexer will appear in the indexer
  collection of the extension block and never in that of the enclosing static class. Extension blocks are further
  excluded from every named type collection, which an existing unit test pins. The fabric selects its types from the
  compilation and from namespaces, so no extension block, and therefore no extension member of any kind, enters the
  fabric query. The only path by which an extension member reaches the enclosing static class is the
  compiler-generated implementation method, whose parameters do reach the fabric; there the aspect is not applied,
  because the eligibility rule requires an explicitly declared declaring member and the fabric skips an ineligible
  target without reporting anything. This is already true of the C# 14 extension properties and is not specific to
  indexers.
- Consequence: no impact on the pattern libraries. Any diagnostic or failure would come from applying a contract
  directly to an extension indexer or to its parameters, which is behaviour inherited from themes 03 and 04.
- Proposed change: after theme 03 decides and after the move to the stable Roslyn, add `ExtensionIndexer_Contract`
  and `ExtensionIndexer_Contract_OnReceiver` tests to the Framework suite beside the existing C# 14 extension member
  contract tests, with the Roslyn variant gate, because the Roslyn 5.0 variant cannot parse an extension indexer. A
  `[NotNull]` test in `Metalama.Patterns.Contracts.AspectTests` is optional and, if written, must apply the attribute
  explicitly to an extension indexer parameter: a fabric-driven test would assert nothing. Two restrictions of the
  language proposal constrain the tests: an extension indexer cannot declare an `init` accessor, and an extension block
  that declares an indexer must name its receiver parameter, which is what makes a receiver contract meaningful. No
  Patterns source change is expected, which the code confirms rather than leaves open.
- Size: small, and blocked on the advice work of theme 04 and on the language version.
- Status: new work. Not implemented, not in progress and not tracked. Related: #1127, the issue that produced the
  receiver-contract tests and the receiver-parameter rules; #1284, the defect report behind the parameter contracts
  in extension blocks; #1035, #1159 and #1160, the advice-side foundation; #1587, which records that indexers are
  deliberately rejected in extension blocks today; #937, the pre-existing limitation of the non-inlined indexer
  override path.
- Verification: the code pass answered the open question from the collection implementations and the existing unit
  test, and found the implementation-method path and the rule that prevents an aspect from being applied there; the
  semantics pass confirmed the metadata shape of an extension indexer from the language proposal and the symbol API,
  and established that the tests are blocked on the language version; the scope pass confirmed that no issue and no
  pull request covers it and noted the overlap with findings LK-6 and LK-7 of theme 04.
- Open questions: none.

### UT-17. Labeled `break` and `continue` and `with(...)` elements in bodies analysed by patterns

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Metrics/SyntaxMetricProvider.cs:78,96-110`,
    `Metalama.Extensions/src/Metalama.Extensions.Metrics/StatementsCountMetricProvider.Visitor.cs:19-34`,
    `Metalama.Extensions/src/Metalama.Extensions.Metrics/SyntaxNodesCountMetricProvider.Visitor.cs:18-28`,
    `Metalama.Extensions/src/Metalama.Extensions.Metrics/Metalama.Extensions.Metrics.csproj:13-14`,
    `Metalama.Framework/src/Metalama.Framework.Sdk/Metalama.Framework.Sdk.csproj:33-36`, `Directory.Packages.props:23`
  - `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:54`,
    `:97`, `:99-213`, `:215-278`, `:280-291`, `:305-311`, `:409-437`
  - `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs:57-67`,
    `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/RoslynHelper.cs:24-76`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` (the optional `Name` field of the break and continue
    statements, and the `with` element with its argument list)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:30-31,49`,
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContext.CreateRoslynCompilation.cs:155-156`,
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContext.CreateCompilation.cs:96-99`
- What happens today: both constructs are handled by code paths that already exist. A labeled `break` adds one
  identifier node to the break statement, and the identifier binds to a label symbol, which the compiler sources
  document explicitly. In the Observability walker, the identifier is either added to a chain that the classification then
  marks unsupported, because the accepted prefix contains only properties and private fields, or, if the semantic
  model returns no symbol, it resets a context that has already been processed; in both cases no diagnostic and no
  exception result. This is the path that `goto label;` already takes today. The assertion that the walker can throw
  cannot fire for a label, because the prefix stops at the first symbol that is not a property or a private field and
  the accessibility helper is short-circuited. A `with(...)` element contributes argument nodes through its argument
  list, which the walker analyses like method call arguments, so property references inside it are recorded as
  dependencies and the name of a named argument follows the same unsupported path as any named argument. For the
  metrics, the node count grows by the label identifier and by the element and its argument nodes, while the
  statement count is unchanged, because a break statement remains one statement and an identifier is not a statement.
  The metric visitors are compiled against the minimum Roslyn API version and rely on the running Roslyn dispatching
  the new node kinds to the default visit, which is how every generated accept method behaves.
- Neither construct is reachable today. The parser accepts both at any language version, but the binder requires a
  language version that Metalama does not offer, and the supported versions stop at C# 14.
- Consequence: no impact.
- Proposed change: add metric unit tests for both constructs in `Metalama.Extensions.Metrics.UnitTests`. These can be
  written before C# 15 is enabled, because the parser produces the nodes at any language version, but the test must
  construct the compilation itself with preview parse options, since the helper that takes source text builds its
  parse options from the supported versions and exposes no language version parameter. Add one Observability aspect
  test with a labeled loop in a getter. The Roslyn variant constant alone is not a sufficient gate for that test:
  the constant is defined only by the latest variant property file, which the Observability test project does not
  import, so the gate would skip the test permanently, and the test source is also compiled by its own project at the
  pinned language version. Sequence the aspect test after the move to the Roslyn that defines C# 15 and after the
  supported versions are raised.
- Size: small.
- Status: new work. Not implemented, not in progress and not tracked. Related: #1217, the open request that the
  metrics extension support multiple Roslyn versions, which bears on how such a test is gated; #1881, the origin of
  the variant constant; #1896, which pinned the template language version.
- Verification: the code pass confirmed both walkers end to end, established that no assertion on the path can fire
  for a label, and refuted the proposed gate; the semantics pass confirmed the node shapes and the generated
  dispatch from the Roslyn sources, verified from the compiler sources that a labeled break binds to a label symbol,
  which the original report could not check, and established the language version prerequisite; the scope pass
  confirmed that no issue and no pull request covers it and that no test project defines the proposed constant.
- Open questions: none.

### UT-18. Documentation that states the previous baseline

- Where and what to change:
  - `Metalama.Framework/docs/platform-support.md:114-115`: the canonical baseline string lists `User=net10.0` while
    `:211-212` names both `net10.0` and `net11.0`. The denomination rule at `:99-106` says that the short form lists
    the floor of each axis, so the string is consistent with the rule as written; state the set explicitly or make
    the rule unambiguous. `:274-276`: add that the C# 15 language support follows the same schedule as
    `RoslynApiMaxVersion`. `:278-291`: give the shipped assets table a note explaining why no `net11.0` row exists,
    citing the paragraph at `:75-82`, which states that there is one Core flavour rather than one per .NET major
    version; do not repeat the roll-forward sentence of the Metalama.Compiler section at `:325-349`, which is an
    application mechanism and does not apply to a library loaded into a host process.
  - `Metalama.Framework/docs/extensibility.md:21-25,72,150,227-236,573,589,651,692`: the instruction to target
    `net472` and `net10.0` remains correct. Add one paragraph stating that the Core folder name is the literal
    `net10.0` in `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-20` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31`, so an extension
    assembly item declared as `net11.0` is never selected on any host and reports nothing.
  - `Metalama.Framework/docs/testing.md:62`, `:98`, `:133`, `:147`: the target frameworks, the analyzer test note,
    the command example and the target framework directive example all name `net10.0` alone and become stale when
    UT-5 lands; add a subsection on language version tests from UT-19.
  - `Metalama.Framework/docs/compile-time-target-frameworks.md:24`: states that `Metalama.Patterns.Contracts`
    multi-targets `net472;net8.0;netstandard2.0`, while
    `Metalama.Patterns/src/Metalama.Patterns.Contracts/Metalama.Patterns.Contracts.csproj:4` reads
    `net472;net10.0;netstandard2.0`. `:95` uses `net8.0` in the inheritance example.
  - `Directory.Packages.md:44-46`: the .NET 8.0 line paragraph, whose rewrite is scoped by issue #1903. Add the
    per-variant `System.Text.Json` rule of UT-11.
  - `Directory.Packages.props:6-7`: the comment states that the property is required while the .NET 10 SDK is
    prerelease, which is no longer true; determine by a restore whether the property is still needed at all and
    record the actual reason, since no floating package version exists anywhere in the repository. `:15` and `:168`
    name Visual Studio 17.14, which PB-2027.0 drops. `:62-66` names .NET 8 as a build target.
  - `CLAUDE.md:211` and `Metalama.Framework/src/tests/CLAUDE.md:8`: the `-f net10.0` command examples, to be extended
    once UT-5 lands.
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:286-287`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestProjectProperties.cs:17`,
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContextOptions.cs:170`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Options/IProjectOptions.cs:75,85,203`: `net8.0` examples in the
    member documentation. The last of these is also edited by UT-3, and whichever story lands first should take it.
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:66-78`: the list of source
    generator attributes is derived from .NET 9 and should be re-derived for .NET 10 and .NET 11. This item is not
    documentation and must be split out; see the consequence below.
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.UntestedTargetFramework/SupportedPlatform.UntestedTargetFramework.csproj:11-12`:
    the comment states that the root `Directory.Build.props` turns the supported-platform check off while the
    repository targets .NET 8. No such setting exists outside the standalone scenarios, so the comment is false.
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:53-54`: the comment
    says that the latest Roslyn is used for package consumers while the default written is the lower variant, and the
    property is assigned inside a target where no evaluation-time consumer can observe it.
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.props:7` names `net6.0` and
    `net8.0` as examples.
  - `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DotNet/DotNet.csproj:3`: describes the project as
    a .NET 8 project testing C# 12 features, while it targets `net10.0`.
  - `NOTES.md:1-9`: the file holds one breaking-change entry and states no convention beyond one heading per change.
    The entries this release needs and that are absent are the removal of the `net8.0` and `net9.0` user target
    frameworks with the `Metalama.Patterns.Wpf` asset change, the removal of Visual Studio 2022, the template
    language staying at C# 14 while user code may use a later version, and the union cache-key behaviour of UT-14c if
    it ships unchanged.
- Consequence: no impact, that is documentation only, for every item except the source generator attribute list. That
  list is functional: it flows from `Metalama.Framework.props:66-78` through `Metalama.Framework.targets:70` into the
  project options, the source generator detection service and the eligibility rule that excludes a partial member
  produced by a source generator. An attribute that .NET 10 or .NET 11 adds and that is absent from the list makes
  such a member eligible for aspects that should exclude it, with no diagnostic, so that item is a behavioural change
  with its own test and does not belong in a documentation pull request.
- Proposed change: the edits above, in one documentation pull request after the code changes of themes 01 to 05 are
  known, excluding the source generator attribute list. Items that are already false today, that is the platform
  baseline string, the shipped assets table note, the compile-time target frameworks document, the standalone
  scenario comment, the aspect testing comments and the language version scenario comment, do not depend on any code
  theme and can be written immediately.
- Size: small to medium.
- Status: new work, except for one item. The audience paragraph of `Directory.Packages.md:44-46` is already scoped by
  the open issue #1903 and must be referenced rather than repeated. Related: #1921, the meta-issue under which the
  documentation story belongs; #1876, which left the `net8.0` examples behind; #1894, which removed the setting that
  the standalone scenario comment describes; #1884, which created those scenarios; #1896, the source of the template
  language entry that `NOTES.md` needs.
- Verification: the code pass opened every cited location, confirmed the text attributed to it, corrected two
  citations and established that the source generator attribute list is functional rather than prose; the semantics
  pass was not engaged, because the finding carries no external premise; the scope pass confirmed that only the
  `Directory.Packages.md` paragraph is tracked and that this finding overlaps UT-11 and UT-12 on four
  `Directory.Packages.props` comments, which must be edited once.
- Open questions: whether user-facing release notes live in `Metalama.Documentation` rather than in `NOTES.md`; the
  file itself does not say. Whether .NET 10 and .NET 11 added attribute-driven source generators, which decides the
  size of the split-out item.

### UT-19. Test conventions for a new language version

- Where:
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp11` to `CSharp14`, with
    the seven feature subdirectories of `CSharp14`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:452,454,681-699`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestDirectoryOptionsReader.cs:31-53`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:71-82`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:56-60`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/TestFramework/Html/metalamaTests.json:2`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Async/AsyncIterators/metalamaTests.json:2`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32,50`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:218-223`
  - `Metalama.Framework/Directory.Build.props:45-46`, `Directory.Build.props:11-16`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Metalama.Framework.Tests.UnitTests.csproj:12`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14,34,53`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests.5.0.0/Metalama.Framework.Tests.AspectTests.5.0.0.csproj:10`
  - `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14/README.md:15-17`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:10`, `Directory.Packages.md:215`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/LanguageVersion/LanguageVersionPreview.cs`
- What happens today: the aspect test suite is organised by language version directory, with feature subdirectories
  under `CSharp14` only, and by the file conventions for the transformed output, the design-time generated documents
  and the cross-project dependency. The `@LanguageVersion` directive is used 41 times, and the harness sets a skip
  reason when the running Roslyn does not recognise a numeric version of 10 or more, instead of failing. A
  directory-level `metalamaTests.json` can require a preprocessor constant, and the requirement is inherited by
  subdirectories and evaluated against the constants of the test project, published as assembly metadata. Every test
  compilation uses the default parse options, which are the latest supported version, that is C# 14, and the test
  projects themselves are built at the pinned maximum language version of 14.0, with three exceptions and one
  override hook that nothing sets.
- The version numbers of the original proposal do not hold. The parse of the version `15.0` succeeds only on the
  Roslyn line that is expected to ship as 5.12; in the stable 5.9.0 and in the consumed prerelease the highest case
  is `14`, so a test annotated `@LanguageVersion(15.0)` is skipped in both variants and never asserts anything. In
  the same Roslyn, the six C# 15 features require the preview language version, so a preview directive, of which one
  precedent exists, is the only one that reaches them today. Raising the maximum language version to 15.0 before the
  compiler knows the value fails the build with a bad-compatibility-mode error rather than falling back silently.
- Consequence: no impact; conventions to follow. The corrections change the version numbers and the ordering, not
  the shape of the convention.
- Proposed change: create `Tests/Aspects/CSharp15/<Feature>/` directories for the six features that the compiler
  gates on C# 15, that is unions, closed classes, labeled `break` and `continue`, collection expression arguments,
  extension indexers and static members in interfaces; the last of these is omitted from the original list and
  matters here, because the aspect test project has a `net48` leg. Gate the directory on the Roslyn version at which
  C# 15 actually appears, by writing the required constant of the renumbered latest variant in the
  `metalamaTests.json` of the `CSharp15` directory and defining that symbol in the renumbered variant property file,
  as the naming rule of `Directory.Packages.md:215` requires. Keep `@LanguageVersion(15.0)` in each file so that the
  intent is explicit and the skip also works when the directory gate is missing, and write the design-time expected
  documents by reading the generated partial classes as the repository instructions require. Note that neither gate
  removes the source from compilation: the default globbing compiles the C# 15 source into the aspect test assembly,
  and the 5.0.0 variant project compiles the same files, and both are compiled by the Roslyn of the .NET SDK, so the
  new directory builds only under an SDK whose compiler accepts C# 15. If that is not yet possible, place the source
  in companion files that the project excludes from compilation and include them through the test directive.
  Replace the hard-coded language version of `Metalama.Framework.Tests.UnitTests.csproj:12` with the shared property.
  Remove the two misspelled gates of UT-10 first, so that the convention is trustworthy. The `TemplateLanguageVersion14`
  scenario keeps its name and value until the minimum Roslyn API version moves, because templates are compiled by the
  Roslyn of the host.
- Size: small for the conventions; the tests themselves belong to themes 02 to 04.
- Status: new work. Not implemented, not in progress and not tracked; no `CSharp15` directory exists and the
  hard-coded language version of the unit test project is unchanged. Related: #1039, the precedent umbrella whose
  sub-issues produced the `CSharp14` layout; #1881, the origin of the only surviving variant constant; #1896, which
  produced the template language version scenario; #985, the standing catch-all for later C# features.
- Verification: the code pass confirmed the directory layout, the skip mechanism, the inheritance of the directory
  gate and every language version ceiling, and refuted the claim that the aspect test assembly is compiled by
  Metalama.Compiler, which matters because the constraint is the Roslyn of the pinned .NET SDK; the semantics pass
  established from the Roslyn sources that the version `15.0` is unparseable in every consumed build and that the six
  features require the preview version there, and added the sixth feature directory; the scope pass confirmed that no
  issue and no pull request covers it, and identified three other themes that each propose a `CSharp15` directory as
  a side effect, so the convention must be settled once here and cited by them.
- Open questions: whether Patterns aspect tests also need a required-constant gate. They reference the testing
  package and have no Roslyn variant, so they run only against the latest engine and do not.

## Withdrawn findings

No finding of this theme was withdrawn. All twenty-three findings of the original report, counting the four
sub-findings of UT-14, were confirmed by the three verification passes, and none was refuted at its core. Several
statements inside them were refuted and are corrected above; the seven that most change the conclusions are
recorded here so that a reader of the original report knows that they were considered.

The original report proposed to decide what `global.json` pins by weighing the default language version of the two
SDK feature bands against each other (UT-1). The .NET SDK carries no per-target-framework language default; the
implied version is computed by the compiler toolset from the project target framework and capped at the maximum the
compiler knows. There is therefore no trade-off, and the language version rewrite it names is a consequence of the
Metalama.Compiler rebase rather than of the container change.

The original report marked UT-2 as plausible and required a build under an 11.0.100 SDK before anything else. The
MSBuild version comparison was established from the MSBuild sources and unit test data instead, and the conclusion
holds under both candidate parsers. Report 01 of this series records the opposite conclusion from the same premise
and is superseded.

The original report gave a restore failure as the consequence of the stale `net8.0` leg of the nested compile-time
project (UT-3), and an end-of-life warning under the .NET 11 SDK. Neither holds: `netstandard2.0` is mandatory in the
same project and is compatible with `net8.0`, so no package can fail for the `net8.0` leg alone, and the end-of-life
list of both SDK branches stops at 7.0. What remains is asset selection and alignment.

The original report attributed the absence of a `net11.0` design-time scenario to the runtime of the host simulator
(UT-8), and said that the tools reach the .NET 11 runtime through roll-forward. The simulator does not register an
SDK in the configuration the build uses, the operative constraint is the pinned SDK, and roll-forward to a higher
major version applies only when no runtime of the requested major version is installed, so the tools stay on .NET 10
on an ordinary machine.

The original report described three .NET 11 SDK changes bearing on the dependency audit (UT-11): package pruning, an
audit mode default and an end-of-life warning. Pruning and the audit default are .NET 10 SDK behaviours that the
.NET 11 SDK leaves unchanged, and the end-of-life entry does not exist yet. The practical consequence is that the
audit mode override of the Patterns tests must not be removed, only its comment rewritten.

The original report explained the collapsed cache key of a union by the branch of the default formatter that prints
a type name (UT-14c). That branch is unreachable for a struct, because the resolved `ToString` is declared on
`System.ValueType` rather than on `System.Object`; the branch actually taken calls `ToString` and produces the same
text for a different reason, which matters for where a fix may be placed.

The original report proposed `@LanguageVersion(15.0)` and the existing Roslyn 5.10 constant as the gate of a C# 15
test suite (UT-19). Neither works before the move to the stable Roslyn: the version is unparseable in every consumed
build, so such a test is reported as skipped in both variants and asserts nothing, and the six features require the
preview version there.

## Non-findings

The following were checked and found unaffected. The line references are those of the original report and were
re-verified only where a finding above depends on them.

- Packable products need no `net11.0` asset. `Metalama.Framework.csproj:4` (`netstandard2.0;net10.0`),
  `Metalama.Framework.Package.csproj:50`, `Metalama.Framework.Engine.csproj:8`, `Metalama.Framework.DesignTime.csproj:6`,
  `Metalama.Framework.Introspection.csproj:4`, `Metalama.Framework.ConfigurationFiles.csproj:4`,
  `Metalama.Framework.DesignTime.Contracts.csproj:4`, `Metalama.Testing.AspectTesting.csproj:7`,
  `Metalama.Testing.UnitTesting.csproj:6`, `Metalama.Extensions.HtmlWriter.csproj:4` and
  `Metalama.Extensions.DiffEngine.csproj:4` target `net472;net10.0`; `Metalama.Backstage.csproj:4` targets
  `netframework4.7.2;net10.0;netstandard2.0`; `Metalama.Testing.Hooks.csproj:4` and
  `Metalama.Extensions.Multicast.csproj:4` target `netstandard2.0;net10.0`; every `Metalama.Patterns` package and
  Flashtrace target `net472;net10.0;netstandard2.0`; `Metalama.Patterns.Wpf.csproj:4` targets `net472;net10.0-windows`
  and serves `net11.0-windows`; the dependency injection, metrics and migration packages target `netstandard2.0`; and
  `Metalama.LinqPad.csproj:6` targets `net10.0-windows`. NuGet selects the `net10.0` asset for a `net11.0` consumer.
- The embedded Core flavour is `net10.0` by decision (`Metalama.Framework/docs/platform-support.md:144-157`,
  `Metalama.Framework.CompilerExtensions.Resources.csproj:6`, `Metalama.Framework.CompilerExtensions.csproj:54,63-64`),
  and the extension loader literals in
  `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-20` and
  `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31` are constants rather than
  values derived at run time, so a .NET 11 process keeps selecting the `net10.0` extension assets. No `NET10_0`
  conditional compilation symbol exists in production code.
- The self-hosted executables roll forward: `Metalama.Backstage.Worker.csproj:13,25`,
  `Metalama.Backstage.DotNetTool.csproj:17`, `Metalama.Backstage.Desktop.Windows.csproj:6` and
  `Metalama.Tool.csproj:37` set `RollForward=Major`. `DevBackstageToolsLocator.cs:39` uses a `net10.0` path for the
  developer layout only.
- The target framework rule of the supported platform check accepts `net11.0` and `net11.0-windows`:
  `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:30-31` and
  `Metalama.Framework.targets:321-322,344-352`, where `v11.0` compares equal to `11.0`. Only the SDK rule is defective,
  which is UT-2.
- `ProjectModel.ProjectFeaturesImpl` parses `net11.0` as major version 11 and reports covariant return support
  correctly (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/ProjectModel.ProjectFeaturesImpl.cs:55-83`);
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/ProjectFeaturesTests.cs:14-24` could add
  the `net11.0` row.
- The runtime identifier is used only for messages and for SDK architecture matching
  (`Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:50,92,144`,
  `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/MSBuildEnvironment.cs:57,131`) and in one .NET
  Framework standalone project. No target framework version check exists outside the package build files and two
  old-style standalone projects.
- The language version plumbing of `Metalama.Framework.targets:115-121`,
  `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60`,
  `SupportedCSharpVersions.cs:31-43,52-62` and
  `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-34` belongs to
  theme 01 and was not re-derived here. A `net11.0` project is affected through the implicit language version of the
  .NET 11 SDK, not through its target framework.
- Trimming and native ahead-of-time compilation: no trimming or ahead-of-time attribute or property occurs in any
  product project or source. The reflection-based serializer calls of
  `Metalama.Patterns/src/Metalama.Patterns.Caching.Backend/Serializers/JsonCachingSerializer.cs:70` are unchanged by
  .NET 11.
- The `NETCOREAPP` conditional compilation sites in the patterns only toggle analyzer pragmas
  (`Metalama.Patterns.Wpf/Configuration/CommandNamingConvention.cs:141-163`,
  `DependencyPropertyNamingConvention.cs:139-148`, `ClassicObservabilityStrategyImpl.cs:518-522`), and the
  `!NET472` serializer sites are a Desktop versus Core distinction rather than a version-specific one. The one
  Wpf unit test gated on `NETCOREAPP` stays true on `net11.0`.
- Backstage resolves the `dotnet` host without any version literal
  (`Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:40-70,130-200`); the macOS directory rule
  of `StandardDirectories.cs:83` compares the runtime with 8.0 and stays correct on .NET 11; the `net8.0` path in
  `PlatformInfoTests.cs:106` is test data describing Visual Studio 2026 and is unaffected.
- `Metalama.Framework.Workspaces` cannot target `net11.0` and does not need to, because
  `Microsoft.CodeAnalysis.Workspaces.MSBuild` ships `net10.0` and `net472` assets only
  (`Metalama.Framework.Workspaces.csproj:5-8`), the packing target follows the restored layout (`:91-97`), and a
  .NET 11 process receives the `net10.0` asset.
- The tests that generate projects at run time (`Metalama.Framework.Tests.Workspaces/WorkspaceTests.cs:43,76,148,192`,
  `Metalama.LinqPad.Tests/SchemaTests.cs:62`, `Metalama.Extensions.Metrics.UnitTests/AddMetricsTests.cs:38`) generate
  `net10.0` projects, which still build under the .NET 11 SDK once the 10.0 reference pack is restored. The fake
  project properties of `TestExecutorTests.cs:56-57`, `FakeMetadataReader.cs:26-27` and
  `AspectTestRunnerTests.cs:152-153` are harmless.
- Contracts on unions behave as on structs: `NotNullAttribute.cs:45-46,55-56` and `RequiredAttribute.cs:52-53,62-63`
  reject a non-nullable union and accept a nullable one; `ContractExtensions.cs:70` selects reference types only;
  `CompileTimeHelpers.cs:33,73` test interfaces and type parameters only. Memoization takes the value-type branch for
  a union-typed property or return (`MemoizeAttribute.cs:61,82`). `[Dependency]` infers the required flag from
  nullability (`DependencyAttribute.cs:29-34`). The Wpf command and dependency property eligibility rules
  (`CommandAttribute.cs:123-137`, `DependencyPropertyAttribute.cs:100-110`) do not look at the type kind of the
  property type.
- The `[Immutable]` aspect enumerates explicit, non-static fields and auto-properties
  (`ImmutableAttribute.cs:56-72`), so the synthesized backing field of the `Value` property of a union is skipped, and
  the immutability fabric configures only immutable collections (`Immutability/Fabric.cs:13-34`).
- The Roslyn 5.0 variant of the patterns has no test project of its own; the Patterns aspect test projects reference
  the testing package and therefore run only against the latest engine, which is what a C# 15 test needs.
- A stray temporary Windows Presentation Foundation project is tracked in git,
  `Metalama.Framework/src/tests/Metalama.AspectWorkbench/Metalama.AspectWorkbench_3gb1zv23_wpftmp.csproj:13`, which
  targets `net8.0-windows`. It belongs to no solution and can be deleted; it is not a .NET 11 issue.
- `Metalama.Framework/src/tests/RunManually/MultiVersion/Version1|2.csproj:7` and
  `Metalama.Framework/src/tests/Utilities/SyntaxCover/SyntaxCover.csproj:5` target `netcoreapp3.1` and are outside the
  automated run (`Metalama.Framework/docs/testing.md:260`).

## Related themes

- The build container and the `net11.0` test matrix are the cluster that this theme owns. It carries UT-1, UT-4,
  UT-5, UT-6, UT-7 and UT-8 together with finding LV-9 of theme 01 and finding PR-8 of theme 07. The container is a
  separate work item from the legs that consume it, because its feedback loop is one continuous integration cycle per
  attempt while the legs can then be added in parallel.
- The engine defaults and test gates that still name the previous platform baseline are the second cluster of this
  theme, carrying UT-3, UT-9 and UT-10. The three share one property: none of them is detectable in a build that
  succeeds, and none produces a failure.
- The pattern and extension libraries on unions are the third cluster, carrying UT-14 and its four sub-findings
  together with finding PR-12 of theme 07, whose only product change is one override in the reference index walker.
  All six consume the same code model surface and share one test matrix.
- The MSBuild platform checks that misreport under the .NET 11 SDK are owned by theme 01, which carries UT-2 with
  finding LV-1 of that theme. Both are defects in the same pair of package build files, and UT-2 is live today.
- The November 2026 platform measurement is owned by this theme and carries UT-11, UT-12 and UT-13 with finding LV-11
  of theme 01. All four wait on the same measurement of the Visual Studio 2026 long-term servicing feature band after
  2026-11-10, and grouping them avoids four separate reopenings of the same two documents.
- The behaviour of the test harness when a test requests a language version that the running Roslyn does not
  recognise is owned by this theme, which carries UT-19 with finding LV-8 of theme 01 and finding DT-7 of theme 05.
  The harness skips the test with a reason and reports no failure, so a whole C# 15 suite can be committed and assert
  nothing.
- Closed hierarchies are owned by theme 03, which carries UT-15 with findings CM-4 and CM-5 of that theme, LK-5 of
  theme 04 and TP-10 of theme 02. Grouping them allows the closed feature to be deferred as a whole.
- Extension indexers are owned by theme 04, which carries UT-16 with findings LK-6 and LK-7 of that theme. The three
  share one prerequisite, a compilation that accepts an extension indexer.
- Labeled `break` and `continue` are owned by theme 02, which carries UT-17 with finding TP-3 of that theme and LK-9
  of theme 04. Both halves read the label of a break or continue statement, which only the regenerated latest variant
  exposes.
- The documentation that states the previous baseline is owned by this theme, which carries UT-18 with finding LV-10
  of theme 01, DT-9 of theme 05 and PR-9 and PR-14 of theme 07. One pull request avoids five conflicting edits to the
  same two documents.
- The renumbering of the latest Roslyn variant to the stable Roslyn and the regeneration of the syntax model are
  owned by theme 01. Every union finding of this document, every C# 15 test proposed here and the per-variant package
  floors of UT-11 depend on it.
- The C# language version tables that stop at C# 14 are owned by theme 01. Until they are raised, no test proposed in
  this document can request C# 15.
- The Roslyn variant gating strategy, that is how the engine may name an application programming interface member
  that exists only in the latest variant, is finding CM-10 of theme 03. UT-14b, UT-14c and the tests of UT-15, UT-16
  and UT-17 each depend on its outcome.
- The union predicate in the public code model, on which the immutability classification of UT-14b and the formatter
  of UT-14c depend, is finding CM-1 of theme 03.
- The Roslyn public API delta and the semantics of each C# 15 feature are recorded in
  [`analysis-reports/08-roslyn-api-delta.md`](analysis-reports/08-roslyn-api-delta.md), and the original report of
  this theme in
  [`analysis-reports/06-user-tfm-patterns-tests-docs.md`](analysis-reports/06-user-tfm-patterns-tests-docs.md).
