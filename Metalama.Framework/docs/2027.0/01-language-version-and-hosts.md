# 01. The C# 15 language version, the .NET 11 SDK as build host, and the move to the stable Roslyn 5.12

This document is the impact analysis of the C# 15 language version, of the .NET 11 SDK used as a build host, and of
the move from the consumed Roslyn 5.10 prerelease to the stable Roslyn 5.12, on the parts of Metalama that decide,
cap, display, serialize or test a C# language version, and on the parts of the build definition that pin a .NET SDK,
a Visual Studio Build Tools component or a Roslyn variant identity. It does not decide the platform baseline:
[`platform-support.md`](../platform-support.md) remains the authority on PB-2027.0, and
[`Directory.Packages.md`](../../../Directory.Packages.md) on which package versions the baseline permits. The
procedure that a Roslyn transition follows is [`updating-roslyn.md`](../updating-roslyn.md).

The analysis was made on `/home/user/Metalama`, branch `topic/2027.0/26-09-03-update-eng-7e3j07`, on 2026-09-03, and
every finding was re-verified on 2026-09-04 through a code lens, a semantics lens and a scope lens. Line numbers
refer to the working tree at that commit. No file was modified, no project was built and no test was run.
`Metalama.Compiler` is not cloned, so every dependency on it is stated as an assumption. Where a claim rests on an
inference rather than on a file that was read, the finding says so.

## Summary

1. The move to the stable Roslyn 5.12 is the prerequisite of the whole theme. The consumed `5.10.0-1.26365.3` is a
   build of the `main` branch that will never have a stable counterpart, and the November 2026 baseline carries
   Roslyn 5.12, so the transition is a renumbering of the latest variant from `5.10.0` to `5.12.0` (LV-12), across
   eight edit sites with two silent failure modes (LV-13), following a procedure that names two members that no
   longer exist (LV-14).
2. Two language version defects are latent today and both become real on the day of that rebase. The MSBuild clamp
   rewrites an implied `15.0` down to `12.0` and warns with a text that is false in that direction (LV-1), and
   `VerifyLanguageVersion` throws inside the formatting of its own diagnostic instead of reporting `LAMA0052`
   (LV-2). Neither can be reproduced with the Roslyn that Metalama consumes today.
3. Accepting C# 15 is one coordinated edit of the language version tables, with one design constraint:
   `SupportedCSharpVersions.Latest` is a constant shared by the Roslyn 5.0 variant and the latest variant, and C# 15
   is valid only in the latter, so `Latest` has to become variant aware while `All` may stay shared (LV-3).
4. The compile-time project manifest carries three disagreeing defaults for an absent language version and no clamp
   for a value that the reading host cannot parse, which is the failure that issue #1185 already reported once
   (LV-4, LV-5).
5. One defect of this theme is live today and independent of C# 15: the .NET SDK ceiling is expressed as `11.0`,
   which the MSBuild version comparison pads to `11.0.0.0`, so `LAMA0601` is reported for every project built with
   any shipped .NET 11 SDK, contradicting the supported set of PB-2027.0 (LV-9).
6. Nothing in the repository builds or tests under the .NET 11 SDK or against `net11.0`, which is what hides the two
   defects above (LV-9), and the desktop MSBuild surface is pinned to Visual Studio 18.9, one release short of the
   November 2026 baseline of 18.12 (LV-11).
7. Four checks close with no code change and are recorded so that they are not repeated: the operator table is
   complete for C# 15 (LV-6), the language ceiling of this repository stays at C# 14 and is correct there (LV-7),
   the aspect test harness already tolerates a language version that the running Roslyn does not know (LV-8), and
   the remaining `net10.0` literals name our own outputs rather than the SDK (LV-10).
8. Only one item can be delivered before the rebase. The non-throwing version formatter of LV-2 compiles against
   both Roslyn variants today and should be delivered first, because every other change of this theme either assigns
   the value 1500 or renames the latest variant, and both wait on Roslyn 5.12.

## Findings

### LV-1. The `LangVersion` clamp rewrites an implied 15.0 to 12.0 and warns with the wrong explanation

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:115-121` (the rewrite)
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:243-247` (the warning)
  - `Metalama.Framework/src/Metalama.Framework.Package/buildTransitive/Metalama.Framework.targets:2` (the import
    that gives transitive consumers the same evaluation)
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:329-366`, `:405-410`,
    `:415` (`LAMA0600`, `LAMA0601` and `LAMA0602`, the codes already allocated in the same file)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:167-179`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-58`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:38-43`
  - `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DotNet/DotNet.csproj:3`, `:6`, `:11` and
    `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DotNet/Test.cs:26`
  - `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DefaultLanguageVersion.sln:6-9`
  - `eng/src/Program.cs:100`
- What happens today: the condition at line 118 accepts only `12.0`, `13.0`, `14.0`, `default`, `latest`,
  `latestMajor` and `preview`, saves the previous value into `_LangVersionBeforeMetalamaFix` at line 119 and assigns
  `12.0` at line 120. The value that reaches that condition is not produced by the .NET SDK. The .NET SDK sets no
  `LangVersion` in any of its five relevant targets files; the mapping from a target framework to a language version
  is defined in Roslyn's `Microsoft.CSharp.Core.targets`, and `Metalama.Compiler` ships a patched copy of that file and
  redirects `CSharpCoreTargetsPath` to it. That copy computes, for a `.NETCoreApp` target framework, nine plus the
  major version minus five, which is `15.0` for `net11.0`, caps the result at `_MaxAvailableLangVersion`, sets
  `LangVersionImplicitlySet` to `True` when `LangVersion` was empty, and then assigns `LangVersion`. The cap decides
  when the clamp fires. In the `Metalama.Compiler` package that was inspected, and in the Roslyn 5.10 prerelease
  that this repository consumes, that cap is `14.0`, so a `net11.0` project receives `14.0` today, `14.0` is in the
  accepted list, and neither the rewrite nor the warning occurs. The cap is `15.0` on the `main` branch of
  `dotnet/roslyn`, which is the Roslyn 5.12 that this theme adopts. Two consequences follow. Until the rebase, a
  `net11.0` project silently compiles as C# 14 rather than C# 15, independently of Metalama. From the rebase
  onwards, `15.0` is a legal language version for the new compiler, it matches none of the seven accepted strings,
  and the clamp rewrites it to `12.0`. The `PropertyGroup` carries no `MetalamaEnabled` guard, unlike the
  neighbouring blocks, so the clamp also applies to a project in which Metalama is disabled.
- Consequence: build or restore error. When the clamp fires, the project drops from C# 15 to C# 12 and therefore
  loses the features of C# 13 and C# 14 as well as those of C# 15, so the compiler reports the family of errors that
  name a feature as unavailable in C# 12.0; when the code happens to fit C# 12, the user instead receives a warning
  whose text is false in that direction and which carries no code, so it cannot be suppressed.
- Proposed change: replace the enumeration by a comparison. Keep the rewrite only when the value is numeric and
  lower than `12.0`, for example a condition that first establishes that `$(LangVersion)` begins with a digit and
  only then calls `$([MSBuild]::VersionLessThan('$(LangVersion)', '12.0'))`. The numeric guard is required rather
  than optional, because that function fails on the named values `default`, `latest`, `latestMajor` and `preview`,
  all of which reach this condition today. Add the missing `MetalamaEnabled` guard in the same edit. Give the
  `Warning` task a `Code` attribute in the `LAMA06xx` range that the platform and toolchain checks already use; the
  task does expose a `Code` parameter, and suppression of an MSBuild task warning is done through
  `MSBuildWarningsAsMessages` rather than through `NoWarn`, which is a C# compiler property. Rewrite the text so
  that it states the direction of the change that actually happened, and so that it no longer says that the new
  value is the lowest version supported by Metalama Framework: `SupportedCSharpVersions.All` contains `CSharp10` and
  `CSharp11`, so `12.0` is the lowest version that this build integration imposes, not the lowest version that the
  product supports. Add a standalone scenario `Standalone/DefaultLanguageVersion/DotNet11` that targets `net11.0`,
  leaves `LangVersion` empty and uses a C# 14 feature, and register it in
  `Metalama.Framework/src/tests/Standalone/DefaultLanguageVersion/DefaultLanguageVersion.sln`, because the suite is
  declared as `ManyDotNetSolutions` over the `Standalone` directory at `eng/src/Program.cs:100` and therefore builds
  solutions rather than project folders. A C# 14 feature is the right choice: the scenario passes today and starts
  failing on the day the cap becomes `15.0`. A C# 15 feature would not serve, because no Roslyn that Metalama
  consumes today accepts C# 15 outside the preview language version. Assert the effective language version rather
  than only compiling, so that the silent downgrade is visible in the test output. Correct the stale comment at
  `DotNet.csproj:3`, which still speaks of .NET 8 and C# 12, in the same change.
- Size: S for the targets, M including the container change of LV-9. The correction of the trigger adds no work.
- Status: new work. The meta-issue #1921 has sixteen sub-issues and none of them covers the clamp. Issue #1884 added
  the neighbouring platform checks and their codes to the same file and declared `net11.0` and the .NET 11 SDK
  inside the tested matrix, which is why the clamp is the remaining `net11.0` defect there.
- Verification: the code lens confirmed the enumeration, the rewrite, the missing warning code and the fact that the
  existing `DefaultLanguageVersion` scenarios cannot observe the defect, and corrected the proposal by requiring the
  new project to be registered in the solution. The semantics lens refuted the attribution of the implied value to
  the .NET SDK, read the patched `Microsoft.CSharp.Core.targets` shipped by `Metalama.Compiler` and the
  `_MaxAvailableLangVersion` cap of the Roslyn sources, and established that the defect is latent until the rebase
  onto Roslyn 5.12. The scope lens confirmed that the defect is present verbatim on the working branch,
  that no pull request touches those lines and that no issue tracks it.
- Open questions: the `Metalama.Compiler` package that was inspected is 2026.1.17 from the `release/2026.1` branch,
  while the repository consumes 2027.0.0, which is not in the local cache. Confirming the cap in 2027.0.0 would
  settle whether the defect is already live on the current branch.

### LV-2. `VerifyLanguageVersion` throws instead of reporting `LAMA0052` for C# 15

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`
    (`:67` the effective version, `:70-81` the `LAMA0051` branch, `:82-89` the `LAMA0052` branch)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:16-40`
    (arms up to `(LanguageVersion) 1400` at `:34`, throwing default arm at `:39`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-45`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/SourceTransformer.cs:145-159`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeExceptionHandler.cs:184-198`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:25-32`,
    `:235-250`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:33-38`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateExpansionContext.cs:867-876`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:45`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Misc/LanguageVersion.t.cs:2`
- What happens today: nothing, because the value 1500 is unreachable. `LanguageVersion.CSharp15` does not exist in
  the consumed Roslyn build, `LanguageVersionFacts.TryParse` has no case for `15` or `15.0`, an out-of-range value
  is reported by the compiler as an invalid language version before Metalama runs, and
  `MapSpecifiedToEffectiveVersion` returns `CSharp14` for `Default`, `Latest` and `LatestMajor`, so the membership
  test at `:82` passes. After the move to Roslyn 5.12, an explicit `15.0` as well as `default`, `latest` and
  `latestMajor` all yield 1500 at `:67`, 1500 is not in `SupportedCSharpVersions.All`, and line 87 calls
  `ToDisplayStringSafe` in order to build the `LAMA0052` argument tuple. That call throws
  `ArgumentOutOfRangeException` before the diagnostic is created, `SourceTransformer` catches it,
  `CompileTimeExceptionHandler` writes a crash report, reports `LAMA0001` and sets `isHandled`, so the build fails
  with an error that asks the user to open a support ticket. The same helper is applied to every member of `All` by
  `SupportedCSharpVersions.FormatSupportedVersions` at `:45`, which both the `LAMA0051` and the `LAMA0052` paths
  use, so adding C# 15 to `All` without the corresponding arm makes the preview diagnostic crash in the same way.
- Consequence: assertion or crash, surfaced as a `LAMA0001` error. The build does fail, so no wrong output is
  produced, but the diagnostic designed for the situation is replaced by a request to open a support ticket.
- Proposed change: extend `ToDisplayStringSafe` so that it can never turn a diagnostic into a crash again. Keep the
  discard arm and return `$"{(int) version / 100}.{(int) version % 100}"` for any value of at least 700, retaining
  the throw for smaller unknown values. The discard arm must be kept rather than replaced by a relational pattern
  arm, because a switch expression without a discard arm produces `CS0509` family exhaustiveness warnings that
  `CodeQuality.targets` promotes to errors under `ContinuousIntegrationBuild`. The formatted fallback reproduces the
  display strings of the compiler for 700, 703 and 800 through 1500, and the four named members `Default`, `Latest`,
  `LatestMajor` and `Preview` are matched by arms that precede it. If an explicit arm is preferred, declare
  `CSharp15` in `AllLanguageVersions.cs` following the existing convention rather than writing a numeric cast
  inline. Add a unit test for 1300, 1400, 1500 and 1600; the class is internal, and the unit test project has access
  to it through the `InternalsVisibleTo` entry at `Metalama.Framework.Engine.csproj:45`.
- Size: S.
- Status: new work, and the only item of this theme that can be delivered before the Roslyn 5.12 rebase, because the
  numeric cast compiles against the Roslyn 5.0 reference assembly exactly like the existing arms for 1300 and 1400.
- Verification: the code lens confirmed the throwing formatter, the `LAMA0052` argument evaluation and the exception
  handling path, and corrected the finding from present-tense behaviour to a latent failure. The semantics lens
  confirmed against the compiler sources that `CSharp15` is 1500, that its display string is `15.0` and that the
  value is unreachable on the consumed build, and corrected the trigger from a non-existent stable 5.10 to Roslyn
  5.12. The scope lens confirmed that the file is unchanged on the working branch, that no test covers a version
  above the supported range and that no issue scopes the change.
- Open questions: none. The existing aspect test `Tests/Aspects/Misc/LanguageVersion.cs` pins `LAMA0052` and passes
  only because the value 800 happens to have an arm in the switch.

### LV-3. Every language version table stops at C# 14, and `Latest` cannot simply move to C# 15

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32` (`Latest`),
    `:38-43` (`All`), `:45` (`FormatSupportedVersions`), `:50` (`DefaultParseOptions`), `:52-62`
    (`ToLanguageVersion`), `:149-159` (`GetMaxLanguageVersion`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:16-40`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60`, `:64-71`, `:111`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:167-183`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:127`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/LanguageOptions.cs:30`, `:35`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Services/CompilationContext.cs:181`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:279`,
    `:349-353`, `:425`, `:1355`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeProjectRepository.Builder.cs:596`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-92`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:51`, `:56-79`, `:106`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:32-52`
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:93`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Misc/LanguageVersion.t.cs:2`
    and `Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs:2`
- What happens today: every table stops at C# 14. `Latest` is the constant `CSharp14`, `All` holds C# 10 to C# 14,
  `ToLanguageVersion` maps both `V5_0_0` and `V5_10_0` to `CSharp14`, `GetMaxLanguageVersion` maps every Roslyn 5.x
  to `CSharp14`, and `LanguageVersionProvider` maps every .NET SDK major of 10 or more to `CSharp14`. The constraint
  named in the title is real: `Metalama.Framework.Engine.5.0.0.csproj:6` compiles the same source files into the
  Roslyn 5.0 variant, so `Latest` is one value shared by both variants, and it feeds `DefaultParseOptions` and every
  fallback listed above. Three statements of the original behaviour matrix needed correction. The latest variant
  does not parse `15.0` either, because `LanguageVersion.CSharp15` is absent both from the Roslyn 5.0 packages and
  from the consumed Roslyn 5.10 prerelease, so `MSBuildProjectOptions.LanguageVersion` falls back to `Latest` in
  both variants and the case of an explicit `15.0` cannot arise today. The reachable case is `LangVersion=preview`, where the project
  version is `int.MaxValue - 1` and the minimum returns the SDK cap unchanged. And in the consumed Roslyn the six
  C# 15 features are gated on `LanguageVersion.Preview` rather than on a C# 15 that the compiler does not know, so
  compile-time code that uses C# 15 syntax is rejected with the preview-feature error. One mechanism was also
  misdescribed: `CompileTimeCompilationBuilder.cs:349-353` calls `CSharpSyntaxTree.Create` rather than `ParseText`,
  so the compile-time trees are not re-parsed at the compile-time language version; only the predefined polyfill
  trees are parsed at it, and the guard that actually rejects a template written above the compile-time version is
  `RoslynVersionSyntaxVerifier`, constructed at `TemplateCompiler.cs:106`.
- Consequence: diagnostic reported, and no wrong output, in the current state. After the move to Roslyn 5.12 and
  before this change, the class becomes an error on every project at the SDK default language version, raised as an
  `ArgumentOutOfRangeException` from `ToDisplayStringSafe` while formatting `LAMA0051` rather than as the intended
  diagnostic.
- Proposed change: add `CSharp15 = (LanguageVersion) 1500` to `AllLanguageVersions`, add `CSharp15` to `All`, and
  add the numeric case to `ToDisplayStringSafe` in the same change or before it. The last item is a prerequisite
  rather than a refinement, because `FormatSupportedVersions` projects every member of `All` through that method, so
  adding `CSharp15` to `All` alone turns both diagnostic paths into unhandled exceptions in both variants; this is
  LV-2. Define `Latest` as `RoslynApiVersion.Current.ToLanguageVersion()` instead of the constant, which is
  expressible because `RoslynApiVersion.Current` is generated per variant at `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:93`. Map only the 5.12 variant to `CSharp15` in `ToLanguageVersion` and keep `V5_0_0` and `V5_10_0`
  at `CSharp14`. `All` may stay a shared constant, because it is used only for membership and for the diagnostic
  text, which keeps the aspect test baselines identical in both variants; the consequence is that the Roslyn 5.0
  variant lists `15.0` in those messages although its own parser rejects the string first. Make
  `LanguageVersionProvider` map an SDK major of 11 or more to `CSharp15`, and clamp its result to the variant
  maximum, because `LangVersion=preview` makes the project version `int.MaxValue - 1` and the minimum then returns
  the SDK cap unchanged. Sort `FormatSupportedVersions` explicitly, because `All` is an `ImmutableHashSet` whose
  enumeration order is not part of its contract and the two baselines pin the printed order; both baselines change
  to include `15.0`.
- Size: M, with the sequencing constraint that no part of it is correct before LV-13. No Roslyn that Metalama
  consumes today exposes C# 15 as a language version, so there is no interim state in which any part of this change
  can land.
- Status: new work, sequenced after LV-13 and after LV-2. The threshold of `GetMaxLanguageVersion` is a decision:
  see the open questions.
- Verification: the code lens confirmed every site, corrected the parse claim about the compile-time trees, and
  added `LanguageVersionExtensions.cs` to the change list as a prerequisite. The semantics lens confirmed the
  numeric value, the display string and the per-variant availability against the compiler sources, refuted the
  assumption that the 5.10 variant parses `15.0`, and established that a language version the running Roslyn does
  not know is reported as a compilation error rather than rejected at the construction of the parse options. The
  scope lens confirmed that no site is changed on the working branch, that no pull request or issue covers it, and
  that findings TP-2 and TP-11 of theme 02 propose the same edits.
- Open questions: the threshold of `GetMaxLanguageVersion` for the `msbuild.exe` path. `LanguageVersion.CSharp15`
  was added to the `main` branch of `dotnet/roslyn` on 2026-08-11, before that branch reached 5.12, so the exact
  boundary in the Roslyn source is below 5.12. The value `(5, >= 12)` is a defensible conservative choice, because
  5.12 is the lowest minor version whose stable package is expected to expose `LanguageVersion.CSharp15`, and
  because a threshold above the exact boundary only caps templates to C# 14 on a Visual Studio that ships no stable
  Roslyn package. The value 10 must not be used, because the stable Roslyn packages below 5.12, up to and including
  5.9.0, accept the C# 15 features only under the preview language version. Separately, whether
  `MetalamaTemplateLanguageVersion` may ever be `15.0` for a library that must also load in Rider: the answer today
  is no, and `Directory.Build.props:11-16` already records that bound.

### LV-4. `CompileTimeProjectManifest.ResolvedLanguageVersion` is dead code and disagrees with the two live fallbacks

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Manifest/CompileTimeProjectManifest.cs:94-95`
    (the comment that explains the integer form), `:99-101` (the property and its `CSharp13` default)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:1355`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeProjectRepository.Builder.cs:596`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `:149-159`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CompileTime/CompileTimeProjectManifestTests.cs:14-66`
- What happens today: the property is marked `[JsonIgnore]`, so it is not serialized, and a search of both
  repositories finds no reader, so it is dead code. The two fallbacks that are actually used both resolve an absent
  language version to `SupportedCSharpVersions.Latest`, which is C# 14 today, rather than to the `CSharp13` that the
  comment documents. The property therefore records a historical fact that nothing enforces. A manifest without a
  language version is re-parsed at C# 14, which is a superset of C# 13, so nothing breaks in practice. One nuance
  qualifies that statement very slightly: C# 14 makes `field` a contextual keyword inside a property accessor, so
  archived compile-time source that used an identifier of that name inside an accessor would bind differently when
  re-parsed at C# 14.
- Consequence: no impact today. Both fallbacks are written in terms of `Latest` rather than of a constant, so
  raising `Latest` would silently raise the version at which manifests written by older versions of Metalama are
  re-parsed, which makes this finding a dependency of LV-3.
- Proposed change: a decision between three options. Deleting the property is provably inert, because nothing reads
  it and it is not serialized. Routing both fallbacks through it restores the documented value but changes behaviour
  and therefore needs a test. The better outcome may be a resolution property that clamps the manifest version to
  `SupportedCSharpVersions.GetMaxLanguageVersion` of the running Roslyn and that both call sites actually use, which
  would also close the gap that LV-5 describes on the non-null branch.
- Size: S.
- Status: decision required, namely whether to delete `ResolvedLanguageVersion`, to route the two fallbacks through
  it at `CSharp13`, or to replace it by a clamping property shared with LV-5. The decision must be taken with LV-5,
  because a clamp added without settling the fate of the property would leave a third divergent default in place.
- Verification: the code lens confirmed that the property has exactly one occurrence in each repository, that the
  two fallbacks are the only readers of `manifest.LanguageVersion`, and that no test pins the property. The
  semantics lens did not apply, because the finding rests on no external premise. The scope lens confirmed that no
  pull request touches the file and that the two closest issues, #1185 and #1142, are the origin of the current
  design rather than a plan to change it.
- Open questions: none.

### LV-5. The manifest round-trips the language version as an integer and the API version as an enum name

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Serialization/LanguageVersionJsonConverter.cs:18-43`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Manifest/CompileTimeProjectManifest.cs:94-97`,
    `:101`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Manifest/TemplateSymbolManifest.cs:31`, `:49`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Serialization/ManifestJsonContext.cs:73`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Serialization/ManifestSerializer.cs:20`, `:43`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeProjectRepository.Builder.cs:526-562`
    (`ReportMixedVersionWarnings`, the natural call site for the proposed warning), `:596`, `:653-662`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:425`,
    `:561-620`, `:1355`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `:52-62`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-39`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CompileTime/CompileTimeProjectManifestTests.cs:14-68`
- What happens today: the converter writes the language version as an integer and reads back any integer with no
  validation, which the comment at `CompileTimeProjectManifest.cs:94-95` explains as deliberate, because a manifest
  may be deserialized by a lower Roslyn version than the one that serialized it. The unit test round-trips the value
  9999 and asserts that it survives, so the absence of validation is pinned. Nothing between the deserialization and
  the two use sites validates the value: the only checks in `TryDeserializeCompileTimeProject` concern the manifest
  version and the Metalama version. The situation cannot arise today, because the writer is capped at C# 14, and the
  writer of a manifest carrying 1500 cannot be the current latest variant either, since `LanguageVersion.CSharp15`
  is absent from the consumed build; it can only be the latest variant after the renumbering to Roslyn 5.12. The
  reaction of the Roslyn 5.0 variant is an error diagnostic and not an exception. Neither the constructor of
  `CSharpParseOptions` nor `WithLanguageVersion` validates, so the parse options are built without error; the lazy
  `Errors` property then runs `ValidateOptions`, the validity table stops at C# 14, and the compilation reports
  `CS8192`, "Provided language version is unsupported or invalid", at the start of every syntax tree. The failure is
  handled: `TryEmit` relays the diagnostics and writes a troubleshooting directory, and
  `TryDeserializeCompileTimeProject` logs a warning and returns false. The `RoslynApiVersion` of
  `TemplateSymbolManifest.UsedApiVersion` is written as the enum member name instead, which is the subject of the
  second silent failure mode of LV-13.
- Consequence: no impact today, because no Roslyn that Metalama currently consumes can produce the value 1500. After
  LV-3 and the renumbering, an aspect library whose templates are compiled with C# 15 and which is consumed under
  the Roslyn 5.0 variant, that is in Rider 2026.2 and in the C# Dev Kit, fails its compile-time build with a
  compiler error that names a language version rather than the reference that requires it.
- Proposed change: when reading a manifest, clamp the language version to `RoslynApiVersion.Current.ToLanguageVersion()` at both fallback sites, and report a warning that names the reference, the language version
  the reference requires and the version the host supports. Both sites are needed, because the first fixes the parse
  options of the syntax trees and the second the language version of the compilation. Clamping to
  `SupportedCSharpVersions.Latest` would not work once LV-3 raises that value, because it is a single value shared
  by both variants, whereas `ToLanguageVersion` is per Roslyn API version. A new descriptor is required in
  `GeneralDiagnosticDescriptors`; both sites already have an `IDiagnosticAdder` in scope and already import the
  required namespaces, and the wording should follow the precedent of `LAMA0087`, introduced by #1898. Do not format
  the unknown value with `ToDisplayStringSafe` unless LV-2 has landed, because that method has no arm for 1500 and
  throws; format the unknown value numerically otherwise.
- Size: S. Adding the numeric arm is a single line and is required by LV-3 in any case.
- Status: new work, to be delivered with LV-4 in one change, because both edit the same two lines and the same
  property.
- Verification: the code lens confirmed the serialization, the two unvalidated fallbacks and the tests that pin the
  absence of validation, and corrected the consequence class from an assertion to a reported compile-time build
  failure. The semantics lens confirmed against the compiler sources that no exception is thrown for an out-of-range
  language version, that the invalid-language-version error is the observable effect, and that the writer of such a
  manifest can only be a post-renumbering latest variant. The scope lens confirmed that neither fallback clamps
  today, that `ReportMixedVersionWarnings` examines only the Metalama version, and that the historical occurrence of
  this failure is issue #1185.
- Open questions: none. The two lenses named different compiler error numbers for an invalid language version; the
  number used here, `CS8192`, is the one verified against `ErrorCode.cs` and its resource string, and it is also the
  number recorded in the report of issue #1185.

### LV-6. `OperatorData` is not affected by C# 15, and its `MinimumLangVersion` is not compared to anything

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/OperatorData.cs:17` (the record parameter),
    `:151-267` (the C# 14 compound assignment operators), `:276` (the only use of `MinimumLangVersion`)
  - `Metalama.Framework/src/Metalama.Framework/Code/OperatorKind.cs:17-324`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/MethodBuilder.cs:233-240`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:770`, `:824`
- What happens today: the table lists the thirteen unchecked and six checked compound assignment operators that C#
  14 made user definable, each carrying `LanguageVersion.CSharp14`. The six features gated on C# 15 add no user
  definable operator: three of the five published proposals contain no occurrence of the word "operator" at all,
  extension indexers extend the indexer surface of an extension block whose operator surface was already there,
  unions mention operators only to specify that an existing user-defined conversion takes priority over the union
  conversion, and static members in interfaces adds no syntax. The table is therefore complete. `MinimumLangVersion`
  is never compared to a language version: the single use is a null filter, and every consumer of the table reads
  `Kind`, `MemberName`, `OperatorKeyword`, `Category` or `IsStatic`. The rejection paths for an operator that cannot
  be introduced use `OperatorData.IsUserDefinable`, which is membership in the by-kind dictionary, and not a version
  comparison.
- Consequence: no impact.
- Proposed change: none for 2027.0. Optionally, give `MinimumLangVersion` its first reader by reporting a diagnostic
  when an aspect introduces a compound assignment operator into a project whose language version is C# 12 or C# 13,
  next to the existing `IsUserDefinable` guard. That is a separate defect and needs its own issue, because none
  exists.
- Size: none.
- Status: new work limited to recording the verified negative; no code change is proposed for 2027.0.
- Verification: the code lens confirmed the table, the single null filter, the absence of any language version gate
  on operator introduction and the absence of any test that pins the field. The semantics lens confirmed the six
  C# 15 features against the compiler sources and read the five published proposals to establish that none of them
  adds a user definable operator. The scope lens confirmed that no pull request and no issue covers the optional
  diagnostic.
- Open questions: none. Two observations sit inside the same file and are out of scope here. The null filter at
  `:276` is a no-op, because all fifty-four entries carry a non-null version, so a wrong non-null value would be
  silent while a wrong null value would remove the entry from the by-name dictionary. And `OperatorData.cs:114-119`
  gives `OperatorKind.UnsignedRightShift` the token of the compound assignment form, which is a pre-existing and
  untested defect unrelated to C# 15 and should be raised separately.

### LV-7. The language ceiling of this repository stays at C# 14, and that is correct

- Where:
  - `Metalama.Framework/Directory.Build.props:43-46` (`LangMaxVersion` and `LangVersion`)
  - `eng/src/Program.cs:151` (the export to dependent repositories)
  - `Metalama.Extensions/Directory.Build.props:23`, `Metalama.Patterns/Directory.Build.props:26`,
    `Metalama.Migration/Directory.Build.props:18`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Metalama.Framework.Tests.UnitTests.csproj:12-16`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTestHelpers/Metalama.Framework.Tests.UnitTestHelpers.csproj:14`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.Benchmarks/Metalama.Framework.Tests.Benchmarks.csproj:9`
  - `Metalama.Framework/src/tests/Standalone/CSharp10/CSharp10.csproj:5`
  - `Directory.Build.props:11-16` (`MetalamaTemplateLanguageVersion` and the bound it records)
  - `eng/RoslynVersions/Roslyn.5.0.0.props:1-14`
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `:50`, `:59-60`
  - `Directory.Packages.props:20-28`
- What happens today: `LangMaxVersion` is `14.0`, it is used as `LangVersion` in `Metalama.Framework`, it is
  exported to dependent repositories, and three other solutions consume it. Two solutions do not participate:
  neither `Metalama.Backstage/Directory.Build.props` nor `Metalama.LinqPad/Directory.Build.props` sets
  `LangVersion`. Several projects pin a lower value of their own, and the unit test project hardcodes `14.0` rather
  than referring to the property. The `.5.0.0` shim projects recompile the same source against the Roslyn 5.0
  packages but neither change the compiler nor override `LangVersion`, so the language version is identical in both
  variants and raising `LangMaxVersion` to `15.0` would fail in both alike, for a different reason than the original
  finding gave: no Roslyn that this repository consumes defines `LanguageVersion.CSharp15`, so the compiler rejects
  the option. What the Roslyn 5.0 variant genuinely bounds is the C# constant `SupportedCSharpVersions.Latest`,
  which is passed to `CSharpParseOptions.WithLanguageVersion` and consumed by `LanguageOptions`,
  `CompilationContext`, `DefaultProjectOptions` and `CompileTimeCompilationBuilder`. Raising
  `MetalamaTemplateLanguageVersion` would break the templates of this repository in a Roslyn 5.0 host such as Rider
  2026.2, which `Directory.Build.props:11-15` already records.
- Consequence: no impact. The values are correct as they stand and no diagnostic fires today.
- Proposed change: none to the values. Extend the comment at `Metalama.Framework/Directory.Build.props:43-44` so
  that the two bounds are not confused. State that `LangMaxVersion` is the maximal C# version that this Metalama
  version supports, that it mirrors `SupportedCSharpVersions.Latest`, and that raising it requires two separate
  conditions: the compiler that builds this repository, that is `Metalama.Compiler` built on `RoslynApiMaxVersion`,
  must accept the option, and `SupportedCSharpVersions.Latest` may not name a version that the lowest supported host
  Roslyn does not define, because the engine passes it to `CSharpParseOptions`. Do not simply write that
  `LangMaxVersion` is bounded by `RoslynApiMinVersion`: that is the bound of `MetalamaTemplateLanguageVersion` and
  not of the language version of our own sources, and writing it would put an inaccurate statement into the build
  files. Optionally replace the hardcoded `14.0` of the unit test project with `$(LangMaxVersion)` so that the
  ceiling has one definition.
- Size: S, a comment plus one optional single-line change.
- Status: new work, too small for a story of its own; it should be delivered with the story that carries LV-3,
  which edits `SupportedCSharpVersions` and is the place where a reader would otherwise raise `LangMaxVersion` by
  symmetry.
- Verification: the code lens confirmed the property, its export and its three consumers, refuted the claim that
  every project of the five solutions compiles with C# 14, and refuted the mechanism attributed to the `.5.0.0`
  variant. The semantics lens did not apply, because the finding rests on no external premise. The scope lens
  confirmed that the comment is unchanged on the working branch, that no pull request touches it, and that issue
  #1896 explicitly separates the template language version from the language version of our own sources.
- Open questions: the theme 06 report is written on the assumption that this theme raises `LangMaxVersion` to `15.0`
  and `SupportedCSharpVersions.Latest` to C# 15. LV-7 states that the first does not happen. Only one of the two
  statements can stand, and the reconciliation belongs to the review of both documents. Separately, what
  `BuildOptions.props` of PostSharp.Engineering sets could not be read, because that package is not restored in this
  environment; either way, the projects of `Metalama.Backstage` and `Metalama.LinqPad` do not take `LangMaxVersion`.

### LV-8. The aspect test harness already tolerates a language version that the running Roslyn does not know

- Where:
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:48`, `:53`, `:409`, `:681-700`
    (`@LanguageVersion`), `:702-721` (`@DependencyLanguageVersion`)
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:72`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestCase.cs:46-53`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestExecutor.cs:309-315`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:218-223`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests.5.0.0/Metalama.Framework.Tests.AspectTests.5.0.0.csproj`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-50`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-92`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:34-39`
- What happens today: the mechanism exists and is wired end to end. When `LanguageVersionFacts.TryParse` rejects the
  argument of the directive while the argument is an integral number of at least 10, the option sets `SkipReason`
  instead of throwing, because throwing would kill test discovery; the reason reaches the test case and produces a
  skipped test. The aspect suite is compiled twice over the same sources, so every test file is discovered by both
  variants. The mechanism is not variant aware: it skips whenever the Roslyn that the test assembly was built
  against does not recognise the number. Since no consumed Roslyn knows C# 15, a test marked with that version is
  skipped in both variants today, and the behaviour becomes selective only after the latest variant moves to Roslyn
  5.12. Running is not sufficient on its own either, because `Latest` and `All` stop at C# 14, so such a test would
  take the unsupported-version branch and crash in `ToDisplayStringSafe` until LV-2 and LV-3 land. Two limits are
  worth recording: the skip is opt-in per test file, so a test that uses C# 15 syntax without the directive produces
  parse errors in the Roslyn 5.0 variant rather than being skipped, and no test in the repository currently
  exercises the skip branch, whose arguments today are `8.0`, `10`, `12.0`, `13.0` and `preview`.
- Consequence: no impact.
- Proposed change: none.
- Size: none.
- Status: new work limited to the record. The C# 15 suites that will use the directive, and the directory and
  constant conventions they follow, belong to theme 06.
- Verification: the code lens confirmed the directive handling, the path from `SkipReason` to the skipped test
  result and the shared sources of the two variant projects, and corrected the claim of differential behaviour by
  inspecting the parse table of the consumed Roslyn assembly. The semantics lens did not apply, because the finding
  rests on no external premise beyond that inspection. The scope lens confirmed that no pull request and no issue
  proposes a change to the harness, and that `@RequiredConstant` and `@ForbiddenConstant` already provide the second
  selector for a test that must run in one variant only.
- Open questions: none.

### LV-9. Nothing builds or tests under the .NET 11 SDK, and the SDK ceiling misreports

- Where:
  - `eng/src/Program.cs:18-26` (the single SDK version and the reason for one feature band), `:35-37` (the only
    `DotNetComponent`), `:61` (`DotNetSdkVersion`)
  - `.gitignore:65`
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8-13` and its `test.json:1-4`
  - `Metalama.Framework/src/tests/Standalone/SupportedPlatform.UntestedTargetFramework/README.md:34-35`
  - `Metalama.Framework/src/tests/docker/linux-x64/CompilerLogs/Dockerfile:21` and
    `Metalama.Framework/src/tests/docker/linux-x64/CompilerLogs/global.json:1-5`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:30-33`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:322`, `:349-352`, `:399`,
    `:405-413`, `:118-121`, `:243-248`
  - `Metalama.Framework/docs/testing.md:224-226`
  - `Metalama.Framework/docs/platform-support.md:195-201`, `:338-339`
- What happens today: the build container installs one .NET SDK, pinned at 10.0.400, and the same constant generates
  `global.json`, which is git-ignored. The single feature band is deliberate, and `eng/src/Program.cs:19-25` records
  the `MSBuildExtensionsPath` conflict that two bands produced. No project in the repository targets `net11.0`.
  Outside the documentation and the analysis reports, the only occurrence of the string is the comment of the
  platform test project that explains why the target framework is excluded, namely that the agents have no
  targeting pack. The Docker scenarios pin .NET SDK 10.0.302
  as well. On the target framework axis the platform check does admit `net11.0`: the value `11.0` is not greater
  than `MaximumNETCoreAppVersion`, so `LAMA0600` is not reported. On the .NET SDK axis it does not.
  `_MetalamaSdkVersion` is `$(NETCoreSdkVersion)` with the prerelease suffix removed, and it is compared with
  `[MSBuild]::VersionGreaterThan` against `MaximumSdkVersion`, which is `11.0`. That intrinsic parses both operands
  with a version type whose unspecified components are zero and which compares four components in order, so the
  ceiling is `11.0.0.0` and every shipped .NET 11 SDK, starting at `11.0.100`, is greater than it. `LAMA0601` is
  therefore reported for every project built with the .NET 11 SDK, whatever its target framework, with a message
  saying that Metalama.Framework does not support that SDK. Finally, the absence of an SDK scenario is in part a
  documented division of work: the `SupportedPlatform.UntestedTargetFramework` README assigns the varying .NET SDK
  matrix to `metalama/Metalama.Tests.DotNetSdk`.
- Consequence: two consequences. The coverage gap itself has no effect on shipped behaviour and hides LV-1 and any
  .NET 11 SDK regression. The gap also hides a defect that does affect shipped behaviour: the ceiling expression
  makes `LAMA0601` fire for every project built with the .NET 11 SDK, including a `net10.0` project, which
  contradicts the supported set recorded in [`platform-support.md`](../platform-support.md). The defect is a warning
  and not a crash, so the build continues unless the user promotes warnings to errors.
- Proposed change: four items, of which the first is independent of the container. First, express the .NET SDK
  ceiling at `Metalama.Framework.props:33` so that a feature band of the highest supported major version does not
  exceed it, for example `11.999`, and state in a comment that the comparison pads the shorter operand with zeros.
  `MinimumSdkVersion` needs no change, because `10.0.400` is not less than `10.0.0.0`. Second, once the
  `MSBuildExtensionsPath` mitigation recorded in `CLAUDE.md` is in PostSharp.Engineering, add a second
  `DotNetComponent` for the .NET 11 SDK, keep `global.json` on the .NET 10 SDK for the product build, and add
  `net11.0` and `net11.0-windows` to the platform test project. Third, cover LV-1 with a separate
  `DefaultLanguageVersion/DotNet11` scenario, because extending the platform test project does not cover it: its
  `test.json` forbids only `LAMA0600`, `LAMA0601` and `LAMA0602`, while the clamp warning carries no code, and an
  unexpected diagnostic fails a scenario only under `FailOnUnexpectedDiagnostics`. Fourth, settle the division of
  work with `metalama/Metalama.Tests.DotNetSdk` before adding a second SDK to this container.
- Size: M for the container and the scenarios, consuming at least one continuous integration cycle per the
  experience recorded in `CLAUDE.md`. The `MaximumSdkVersion` correction alone is S: one line and a comment.
- Status: new work. The container half is the prerequisite that theme 06 owns and that the `net11.0` test legs of
  several themes consume. The ceiling half is the same defect as finding UT-2 of theme 06 and belongs with LV-1,
  because both are defects of the same pair of package build files.
- Verification: the code lens confirmed the single SDK, the absence of `net11.0` anywhere but in a comment and the
  Docker pins, refuted the behaviour matrix cell that said `LAMA0601` is not reported under the .NET 11 SDK by
  reading the implementation of the MSBuild comparison, and corrected the claim that the platform test would have
  caught LV-1. The semantics lens did not apply, because the finding rests on repository files and on the MSBuild
  implementation that the code lens read. The scope lens confirmed that none of the sixteen sub-issues of #1921
  covers building or testing under the .NET 11 SDK, and that issue #1884 deferred the two target frameworks for want
  of targeting packs.
- Open questions: whether `metalama/Metalama.Tests.DotNetSdk` already covers the .NET 11 SDK, which could not be
  checked because that repository is not cloned; and whether the generated `global.json` carries a `rollForward`
  policy, which is decided in PostSharp.Engineering and is not visible here.

### LV-10. The remaining `net10.0` literals name our own outputs, and the MSBuild pins name the SDK generation

- Where:
  - `eng/src/BuildMetalama.csproj:5-6`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/Metalama.Framework.CompilerExtensions.csproj:53-54`, `:63-64`, `:87-88`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj:5-6`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:19-24`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31`, `:36`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:35-36`, `:468`
  - `Metalama.Framework/Directory.Build.props:31` and `Metalama.LinqPad/Directory.Build.props:11`
  - `Directory.Packages.props:35-50` (`MicrosoftBuildVersion`)
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:18`
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:84`, `:90-94`
  - `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/Metalama.DesignTime.HostSimulator.csproj:6` and `MSBuildEnvironment.cs:50`
  - `eng/src/DesignTimeSolution.cs:42`
  - `Metalama.Framework/docs/platform-support.md:196-197`, `:338-339`
- What happens today: the literals fall into two groups that must not be explained together. The first group names
  the Core flavour of our own payload. The selection at run time is performed by
  `TargetedAssemblyReference.SatisfiesCurrentProcess`, which compares the target framework declared by an extension
  package against a field that is `net472` for a .NET Framework process and `net10.0` for every other process, so a
  host running on .NET 11 still asks for the `net10.0` extension assemblies, which is what the extension property
  files declare. The embedded payload is chosen the same way and not by version, so a .NET 11 host extracts the Core
  payload compiled for `net10.0` and loads it. The expression at `ExtensionLoaderBase.cs:31` duplicates the same
  computation, but its value reaches only the trace message; the filter is the property above. Two further sites
  contain the literal for a different reason: the two `Directory.Build.props` conditions restrict code quality
  analysis to a single inner build. The second group is pinned to the .NET SDK generation rather than to the payload
  flavour. The assembly version of `Microsoft.Build` is frozen at 15.1.0.0 across releases, so the compile-time
  reference to 18.0.2 binds against any later MSBuild; what the identity does not settle is the runtime. MSBuild
  18.0 through 18.9 are built for `net10.0`, and MSBuild 18.12, the version that the .NET 11 SDK consumes, is built
  for `net11.0`, which a `net10.0` host process running on the .NET 10 runtime cannot load. The two locator hosts in
  this repository do not fail with an assembly load error, because both restrict the candidate software development
  kits to a major version at or below the running runtime, and `MSBuildInitializer` throws a diagnostic exception
  when nothing qualifies.
- Consequence: no impact for 2027.0, conditional on the build staying pinned to a .NET 10 SDK. A residual
  user-facing consequence exists on a machine that carries only the .NET 11 SDK, where `Metalama.Framework.Workspaces` and the LinqPad driver find no usable MSBuild although
  [`platform-support.md`](../platform-support.md) admits that SDK into the supported set.
- Proposed change: no code change for 2027.0. The description of the group boundary is corrected above, and the
  residual consequence is recorded as an open question rather than under "no impact". If the Core flavour ever
  moves, the first group moves together, and `eng/src/BuildMetalama.csproj:5-6` already cross-references one of its
  members; the second group moves with the `dotNetSdkVersion` pin of `eng/src/Program.cs`.
- Size: text only. Any code change is a separate decision about a `net11.0` asset for `Metalama.Framework.Workspaces`.
- Status: new work: none in code. The state was produced by pull request metalama/Metalama#1877, and the Premium
  mirror of the same literal is in the open pull request metalama/Metalama.Premium#85.
- Verification: the code lens confirmed every literal at its cited line, corrected the claim that
  `ExtensionLoaderBase` performs the selection and the grouping of three sites that do not select a payload flavour
  at all. The semantics lens confirmed that Roslyn still builds the .NET Core `csc` for `net10.0` on `main`, that
  the assembly version of `Microsoft.Build` is frozen on four release lines, and refuted the inference that the
  frozen identity alone lets a `net10.0` process host the MSBuild of a .NET 11 SDK. The scope lens confirmed that no
  live product literal names `net8.0` or `net9.0` any more, apart from a documentation comment and a foreign package
  layout.
- Open questions: whether `Metalama.Framework.Workspaces` must gain a `net11.0` asset for a user machine that
  carries only the .NET 11 SDK. Separately, the exact MSBuild version that the released .NET 11 SDK will carry is an
  assumption: the .NET 11 line of `dotnet/sdk` currently pins a preview of the 18.12 line, which can still move
  within that line. One incidental observation, unrelated to this theme: the generated WPF temporary project file
  `Metalama.Framework/src/tests/Metalama.AspectWorkbench/Metalama.AspectWorkbench_3gb1zv23_wpftmp.csproj:13` is tracked in git and still declares `net8.0-windows`; it is
  regenerated by the build and read by nothing, and removing it from source control is separate housekeeping.

### LV-11. `MSBuildVersion` and the Build Tools component pin Visual Studio 18.9

- Where:
  - `eng/src/Program.cs:26` (the .NET SDK constant), `:40-41` (the Build Tools component), `:63` (`MSBuildVersion`),
    `:95-100` (the `MsbuildSolution` entry and the `ManyDotNetSolutions` entry), `:18-26` (the comment that ties the
    .NET SDK version to the component)
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:34-37`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:417-418` (`LAMA0602`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:31-43`, `:74-123`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:149-159`
  - `Metalama.Framework/src/tests/Standalone/Issue31024/Issue31024.proj:13`, `:20` and
    `Metalama.Framework/src/tests/Standalone/Issue31024/NetFrameworkBuildApp/Program.cs:12-14`
  - `Metalama.Framework/docs/platform-support.md:354-368` (the measurement checklist)
- What happens today: three constants pin the toolchain of the container: the Build Tools component at 18.9.2,
  `MSBuildVersion` at 18.9, and the .NET SDK version that the component installs. `MSBuildVersion` is a hard
  requirement rather than a preference: PostSharp.Engineering resolves the desktop MSBuild through it and reports
  that it could not find an `msbuild.exe` matching the required version when no installation matches. The version
  list embedded in the PostSharp.Engineering build tools package stops at Visual Studio 18.9.2, so no later Build
  Tools version can be requested yet. Two test entries use the desktop MSBuild and only one of them reaches the
  language version code: the `MsbuildSolution` entry builds SDK-style projects, so `NETCoreSdkVersion` is defined
  and `LanguageVersionProvider` takes the .NET SDK branch, while the branch that reads the Roslyn under
  `MSBuildBinPath` is covered by the standalone scenario `Issue31024`, whose old-style .NET Framework project is
  built by `$(MSBuildExePath)` and whose source says so. The Visual Studio release that Metalama must cover in
  November 2026 is version 18.12 rather than a new major version: Roslyn `main` is 5.12 and inserts into Visual
  Studio under a title naming 18.12, MSBuild `main` is 18.12.0, and the quarterly Visual Studio 2026 releases so
  far are 18.0, 18.3, 18.6 and 18.9. That release therefore carries Roslyn 5.12.
- Consequence: no impact in continuous integration today, and no diagnostic for a newer Visual Studio, because the
  platform requirement declares only a minimum and `LAMA0602` fires only below that floor. The gap is a coverage gap
  for the Visual Studio 18.12 MSBuild, and it lies in the standalone scenario rather than in the `MsbuildSolution`
  entry. On a developer machine whose only Visual Studio is later than the pin, the pin is not silent: the
  MSBuild-based tests fail with a message naming the required version.
- Proposed change: raise the three constants together when PostSharp.Engineering exposes the Visual Studio 18.12
  Build Tools, and regenerate the container. Raising the component without raising the .NET SDK version
  reintroduces the two-feature-band restore failure that the comment at `eng/src/Program.cs:19-26` records. Perform
  the change together with the feature-band measurement of `MinimumVisualStudioVersion` that
  `Metalama.Framework.props:34-37` already schedules for after 2026-11-10. The arm that
  `SupportedCSharpVersions.GetMaxLanguageVersion` needs for the `msbuild.exe` path belongs to LV-3 and must not use
  the threshold 10, because no stable Roslyn package below 5.12 exposes `LanguageVersion.CSharp15`.
- Size: S in this repository, dependent on PostSharp.Engineering exposing the component.
- Status: new work, and to be sequenced with the November 2026 measurement that theme 06 owns and with the container
  change of LV-9, because both edit `eng/src/Program.cs` and both regenerate the container. Issue #1902 asked for
  the previous version of this move and was closed as not planned, on the ground that regenerating the Visual Studio
  base images is expensive; the move it declined has since landed anyway.
- Verification: the code lens confirmed the three pins, established that `MSBuildVersion` is a hard requirement from
  the diagnostic strings of the build tools assembly, refuted the attribution of the coverage gap to the
  `MsbuildSolution` entry, and added the .NET SDK constant to the change list. The semantics lens confirmed the
  Roslyn and MSBuild version numbers of the November 2026 release from the version files and insertion metadata of
  both upstream repositories, and refuted the threshold `(5, >= 10)` proposed for the `msbuild.exe` path. The scope
  lens confirmed that the 18.9 pin is already merged on the default branch, that no open pull request or issue
  raises it further, and that the November measurement exists only as prose in a document and as a comment in a
  build file.
- Open questions: the branding of the November 2026 Visual Studio release could not be verified from the permitted
  sources, but the version number, which is what the component and `MSBuildVersion` require, is settled at 18.12.
  Note that on the default branch the .NET SDK version comes from a PostSharp.Engineering product family constant
  rather than from a literal, so a story must not reintroduce the literal.

### LV-12. The consumed Roslyn 5.10 prerelease has no stable counterpart, so the transition is a renumbering to 5.12.0

- Where:
  - `Directory.Packages.props:23`, `:25-30`
  - `nuget.base.config:5-7`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:85`, `:117-132`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:234`, `:756`,
    `:794`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CompileTime/NuGetHelperTests.cs:1240-1258`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:3`, `:5`, `:11-12` and `eng/RoslynVersions/Latest.props:2`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-54`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:157-172`, `:236-308`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/MetalamaSourceTransformer.cs:23-31`, `:38-44`,
    `:52-57`
  - `Metalama.Framework/docs/updating-roslyn.md:12`, `:15-17`
  - `Metalama.Framework/docs/platform-support.md:232-236`, `:253`, `:274-276`
  - `Directory.Packages.md:171-172`, `:174-186`, `:201`
- What happens today: `RoslynApiMaxVersion` and `RoslynMaxVersion` are the prerelease `5.10.0-1.26365.3`, and
  `ToNuGetVersionString` maps the latest variant to the same string. Four external facts, verified on 2026-09-03,
  settle the target. Only the stable `Microsoft.CodeAnalysis.CSharp` versions 5.0.0, 5.3.0, 5.6.0 and 5.9.0 are
  served by nuget.org; there is no 5.10, 5.11 or 5.12 package. Roslyn minor versions advance with the monthly Visual
  Studio snaps, while a stable package is published only at every third minor, in step with the quarterly Visual
  Studio 2026 releases 18.0, 18.3, 18.6 and 18.9. The .NET 11 general availability feature band of
  `dotnet/sdk` pins the Roslyn toolset at a 5.12 build of 2026-09-01, which makes the November 2026 baseline a
  Roslyn 5.12 baseline and makes the sentence "Roslyn 5.11 or thereabouts" in
  [`platform-support.md`](../platform-support.md) an understatement. Finally, Roslyn 5.10 is a real version of the
  product, so the accurate statement is not that Roslyn 5.10 does not exist, but that no
  stable package of that minor is served by nuget.org and none is expected, which is what matters here, because
  `ToNuGetVersionString` names a package that a user machine must restore. Because
  [`platform-support.md`](../platform-support.md) places no Visual Studio below the November 2026 baseline in the
  supported set, the variant identity `5.10.0` will serve no host in PB-2027.0, which is exactly the renumbering
  case of [`updating-roslyn.md`](../updating-roslyn.md). Two further observations confirm 5.12 rather than 5.10 or
  5.11 as the required target. `LanguageVersion.CSharp15` is absent from the stable package 5.9.0 and from the
  consumed 5.10 prerelease, and is present on `main`, which is 5.12. The grammar of the consumed 5.10 prerelease
  still marks five declarations as experimental, namely the union declaration, the unsafe expression, the with
  element and the name field of the break and continue statements, while `main` marks only the unsafe expression.
- Consequence: build or restore error on every user machine if `ToNuGetVersionString` names a package that its
  source does not serve, since the string is written into the generated reference-assembly project and alone
  declares the prerelease source. If the identity understates the Roslyn version that the payload binds against, a
  host below that version is still handed the latest variant, because the variant policy selects one variant and
  never falls back after a failed load; the result is an exception that fails the build at compile time, with a
  crash report, and a silent absence of Metalama at design time, rather than wrong output.
- Proposed change: two steps that both go through the renumbering procedure of LV-13. Step one, as soon as
  `Metalama.Compiler` builds on a 5.12 prerelease: renumber the latest variant to `5.12.0` and set
  `RoslynApiMaxVersion`, `RoslynMaxVersion` and the version string of the latest variant to that prerelease; the
  hyphen keeps the prerelease package source declared, and the renamed variant property file keeps reading
  `$(RoslynApiMaxVersion)`. Step two, within three weeks of the stable 5.12 as
  [`platform-support.md`](../platform-support.md) requires: edit the same three strings to `5.12.0`, which turns the
  prerelease source off, and re-derive `SystemTextJsonVersion` in the variant property file from the actual nuspec
  of the stable `Features` package rather than assuming a higher 10.0.x version. That floor tracks the servicing
  version current when the package is built: the 5.9.0 package requires 10.0.1 and the consumed 5.10 preview
  requires 10.0.8, so a 5.12 package built alongside .NET 11 may require an 11.0.x version that the current pin of
  10.0.11 does not satisfy. Add `Syntax-5.12.0.xml` and keep `Syntax-5.10.0.xml`, following
  [`updating-roslyn.md`](../updating-roslyn.md), which requires adding a file named for the new version rather than
  renaming the previous one; the consequences of the disappearing experimental markers belong to theme 02. Correct
  `platform-support.md:235` and `Directory.Packages.md:201` from 5.11 to 5.12, re-derive the variant ranges at
  `platform-support.md:253` and `Directory.Packages.md:171-172`, and correct the cadence wording so that it names
  the monthly Visual Studio snaps for the Roslyn minors and the quarterly Visual Studio releases for the stable
  packages. At step two, also update the comments that describe Roslyn 5.10 as a preview at
  `Directory.Packages.props:25-27` and
  `nuget.base.config:5-7`. The same 5.10 naming appears in the sentence added by pull request #1912 about re-reading
  the Visual Studio package ceilings after 2026-11-10 and should be corrected with them.
- Size: S for the strings in each step, plus the documentation edits; the renumbering itself is LV-13.
- Status: new work, and one story with LV-13 and LV-14.
- Verification: the code lens confirmed the version properties, the derivation of the prerelease source from the
  hyphen, the two-step shape of the change and the renumbering rule, and corrected the consequence class from silent
  wrong output to a compile-time exception and a design-time silence. The semantics lens confirmed the nuget.org
  stable set, the version of the `main` branch, the release branches and the Roslyn toolset pinned by the .NET 11
  feature band, and refuted two subsidiary premises, namely the list of release branches and the mechanism of the
  release cadence. The scope lens confirmed that the prerelease strings are unchanged, that `Roslyn.5.12.0.props`
  and `Syntax-5.12.0.xml` do not exist, and that no open pull request or issue names Roslyn 5.11 or 5.12.
- Open questions: whether the stable 5.12 `Features` package raises the `System.Text.Json` floor beyond `10.0.11`.
  The prerequisite, that `Metalama.Compiler` moves to Roslyn 5.12 first, is an assumption, because that repository
  is not cloned. The November 2026 date itself is an inference from the release cadence rather than a published
  fact.

### LV-13. Every site that the renumbering from 5.10.0 to 5.12.0 touches, and its two silent failure modes

- Where:
  - `eng/RoslynVersions/Roslyn.5.10.0.props:3`, `:5`, `:7`, `:8-10`, `:11-12` and `eng/RoslynVersions/Latest.props:2`
  - `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:16-18`, `:30-35`, `:39-42`;
    `eng/src/GenerateMetaSyntaxRewriter/Model/SyntaxDocument.cs:22`;
    `eng/src/GenerateMetaSyntaxRewriter/Model/RoslynVersion.cs:13`, `:15`;
    `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs:13`, `:21`, `:37-38`, `:52`;
    `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:81-96`, `:442-473`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:52-62`, `:77-87`,
    `:134-144`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:48`, `:70`, `:85`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-37`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:77`, `:79`, `:244`,
    `:633-656`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/RoslynVariantPolicyTests.cs:21-30`, `:36-45`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:22-24`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:30-39`, `:66-80`,
    `:119-138`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Serialization/ManifestJsonContext.cs:73` and
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Manifest/TemplateSymbolManifest.cs:31`
  - `Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj:18`, `:21`, `:24`, `:27`, `:30`, `:33`;
    `Metalama.Framework/src/Metalama.Framework.Sdk/Metalama.Framework.Sdk.csproj:19`, `:22`, `:25`, `:28`;
    `Metalama.Framework/src/Metalama.Framework.Introspection/Metalama.Framework.Introspection.csproj:23`;
    `Metalama.Framework/src/Metalama.Framework.CompileTimeContracts/Metalama.Framework.CompileTimeContracts.csproj:18`
  - `Metalama.Framework/docs/extensibility.md:127-135`; `Directory.Packages.md:215`;
    `Metalama.Framework/docs/platform-support.md:233-235`, `:248-249`; `Metalama.Framework/docs/updating-roslyn.md:16`
- What happens today: nothing. Both failure modes below are consequences of the renumbering, and the variant policy
  maps a host presenting Roslyn 5.12 to the `5.10.0` variant correctly today. One statement of the original
  narrative is withdrawn: the explanation recorded at `Directory.Packages.md:215`, that the location of the
  diagnostic `CS1014` changed between Roslyn 5.0 and the 5.10 build, is not supported by the parser, whose three
  relevant methods are textually identical between the two and both report on the identifier. What is observed is
  that the two aspect test baselines differ; the cause of that difference is therefore not established, and the
  preprocessor symbol names the variant boundary at which the difference was observed. Nothing found in the Roslyn
  sources predicts a different outcome for that test input under Roslyn 5.12, so no baseline is expected to change
  with the renumbering.
- Consequence: nothing today, and two silent failure modes afterwards. Failure mode one is silent wrong output:
  `TargetedAssemblyReference.SatisfiesCurrentProcess` accepts an extension assembly only when its declared
  `TargetRoslynVersion` equals the current version exactly, and `ExtensionLoaderBase` drops the others without a
  diagnostic, so an extension package that declares the pattern documented in
  `Metalama.Framework/docs/extensibility.md:127-135` is no longer loaded after the renumbering and the user sees
  neither its aspects nor a message. Failure mode two is an assertion or crash: `UsedApiVersion` is written as the
  enum member name, so an aspect library built by a 2027.0 preview carries the old member name in its embedded
  manifest, and the deserializer throws if the enumeration loses that member.
- Proposed change: eight edit sites. First, rename `Roslyn.5.10.0.props` to `Roslyn.5.12.0.props` and set
  `ThisRoslynVersionNoPreview` to `5.12.0`; line 3 keeps `$(RoslynApiMaxVersion)`, line 7 keeps the empty project
  suffix, and lines 11 to 12 follow LV-12. Second, import the renamed file from `Latest.props`. Third, in the
  generator, add `Syntax-5.12.0.xml` and keep `Syntax-5.10.0.xml`, so that the enumeration keeps the old member and
  failure mode two cannot occur. The list must stay in version order: `versionNames` is built as the legacy list
  followed by the current names, the position in that array is the version order used by `RoslynVersion.Index`, by
  `VersionDetector`, by the numeric values of the generated enumeration and by the ordinal comparisons of
  `RoslynVersionSyntaxVerifier`, so simply appending the old name to the legacy list would invert two versions and
  mis-attribute the minimal version of every syntax node. Write `versionNames` explicitly in version order and skip
  code generation through a separate collection. Note that the 5.12 grammar drops the experimental markers of the
  union, the with element and the labeled break and continue fields, so the generator starts emitting code for those
  nodes; that consequence belongs to theme 02 and is not counted in the size below. Fourth, in
  `SupportedCSharpVersions`, add the new member to `ToLanguageVersion`, `ToNuGetVersionString` and `ToVersion`, and
  keep the old arms, because the switches must stay exhaustive. Fifth, in the variant policy, set the threshold to
  version 5.12 and the variant name to `5.12.0`; the name must equal `ThisRoslynVersionNoPreview`, because the
  resource extractor appends it to the assembly name. Sixth, move the version values of the variant policy tests
  between the two theories. Seventh, update the `InternalsVisibleTo` entries that carry a literal version; the
  entries that use the property follow it. Eighth, update the documents, with
  [`updating-roslyn.md`](../updating-roslyn.md) handled by LV-14 and `platform-support.md:233-235` added to the
  list. Finally, make failure mode one visible. The range test originally proposed for
  `SatisfiesCurrentProcess` is unsound as stated: a package registers one item per variant, every satisfying path is
  loaded and every exported extension is registered with no deduplication, so accepting any declared version at or
  below the current one would register every extension of that package twice. Prefer a diagnostic, or at least a
  warning, when a package declares extension assemblies and every one of them is rejected. Keep the preprocessor
  symbol and the two aspect tests unchanged, as `Directory.Packages.md:215` intends.
- Size: M for the renaming. The grammar file pulls in the generated code for the new nodes, whose size belongs to
  theme 02.
- Status: new work, and one story with LV-12 and LV-14. The Premium mirror of the renaming is finding PR-1 of theme
  07 and is delivered as a separate pull request, because a pull request cannot span two repositories.
- Verification: the code lens confirmed every site, corrected two line references that overran their files, added
  the eleventh `.5.0.0` shim project to the count, and refuted the ordering of the proposed generator edit and the
  soundness of the proposed range test. The semantics lens confirmed the target version, the numbering of C# 15, the
  frozen assembly-version scheme that makes the roll-forward argument work, and the disappearance of the
  experimental markers, and refuted the recorded cause of the `CS1014` baseline difference. The scope lens confirmed
  that no site is changed, that the previous renumbering is the precedent and that the two failure modes are
  untouched.
- Open questions: the Roslyn version of the November 2026 Visual Studio baseline remains checklist item 1 of
  `Metalama.Framework/docs/platform-support.md:354-368`. The alternatives are 5.12 or, if the date slips by a
  quarter, 5.15; 5.11 is not a candidate, because no stable package of that minor exists. If a baseline nevertheless
  presented a minor without a stable package, the latest variant would have to stay on the prerelease feed.

### LV-14. `updating-roslyn.md` and three other documents name members that do not exist

- Where:
  - `Metalama.Framework/docs/updating-roslyn.md:31`, `:33`, `:34`
  - `Metalama.Framework/docs/platform-support.md:83`, `:189`
  - `Directory.Packages.md:163`, `:181`, `:195`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime.Rpc/CLAUDE.md:115`, `:218`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/Rpc/CLAUDE.md:24`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTestHelpers/TestClasses/SerializationTestsBase.cs:16`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:77`, `:79`, `:244`,
    `:633-656` (`GetHostRoslynVersion`, whose JetBrains marker branch is `:638-653`)
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-54`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/RoslynVariantPolicyTests.cs:36-45`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime.Rpc/RpcContractMessagePackOptions.cs:37`
  - `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj:25-26`
- What happens today: step 10 of the procedure names `ResourceExtractor.GetRoslynVersion`, which does not exist, and
  `JsonSerializationBinder`, which exists nowhere in either repository; and it states that the Resources project
  must list the new assemblies, which is true only when a variant is added, because that project references the
  variant projects by path and the latest variant has an empty project suffix. Two other documents repeat the first
  name at five further places. The member that exists is `ResourceExtractor.GetHostRoslynVersion`, whose result is
  mapped to a variant directory name by `RoslynVariantPolicy.TryGetVariantName`; both names date from the pull
  request that closed issue #1898. The remote procedure call layer serializes with MessagePack and contains no
  binder at all, so the passage in its `CLAUDE.md` is stale in its substance and not only in the name; two further
  documents name the same absent type, one of them attributing a member to a test context class that does not
  declare it. Whoever follows step 10 will search for names that return nothing and may conclude that the step is
  done, which matters because step 10 is the only place in the procedure that points at the variant name mapping,
  and a stale mapping produces a resource lookup failure written to the crash report directory rather than a
  diagnostic.
- Consequence: no impact on code, and a real risk that the renumbering of LV-13 leaves the variant name mapping
  stale.
- Proposed change: rewrite step 10 with the current names. Replace the second item with `RoslynVariantPolicy.TryGetVariantName` and its test, delete the item that names the absent binder, and move the Resources project item
  from the shared list into the add-a-variant list, since the renumbering path needs no edit there. Replace the
  absent member name at the five sites in the two other documents and say that the mapping to a variant is done by
  the policy. Rewrite the serialization section of the remote procedure call `CLAUDE.md` so that it describes the
  MessagePack contract, remove the absent member from the test `CLAUDE.md`, and correct the documentation comment in
  the test helper.
- Size: S.
- Status: new work, and part of the same story as LV-12 and LV-13, because it supplies the corrected procedure that
  the renumbering executes.
- Verification: the code lens confirmed that neither name exists in the source, identified the members that do,
  corrected two line references that overran their files, and added three further stale sites that the original
  finding did not list. The semantics lens did not apply, because the finding rests on no external premise. The
  scope lens confirmed that every stale name is present in the working tree, that the rename was made by the pull
  request that closed issue #1898 without updating the documents, and that no issue tracks the correction.
- Open questions: none.

## Withdrawn findings

No finding of this theme was withdrawn. Every one of the fourteen findings survived the three verification lenses.
Nine of them were corrected rather than confirmed as written, and each correction is recorded in the verification
item of the finding it belongs to. The corrections that change a conclusion rather than a detail are these: the
trigger of LV-1, LV-2, LV-3 and LV-5 moves from the present to the Roslyn 5.12 rebase, because no Roslyn that
Metalama consumes today knows C# 15; the behaviour matrix cell that said that no `LAMA0601` is reported under the
.NET 11 SDK is inverted, which turns a coverage gap into a live defect (LV-9); the mechanism attributed to the
Roslyn 5.0 variant in LV-7 belongs to the shared constant rather than to the variant projects; and the recorded
cause of the two differing aspect test baselines in LV-13 is withdrawn for want of evidence.

## Non-findings

The following were checked and found unaffected by C# 15, by the .NET 11 SDK or by the move to the stable Roslyn
5.12, with the file that establishes it.

- The mapping of a host that presents Roslyn 5.12 while the latest variant is still `5.10.0`. The member named in
  the procedure no longer exists (LV-14); the host version is read by
  `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:633-656` and mapped by
  `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-54`, which selects the
  latest variant for any version at or above 5.10, and
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/RoslynVariantPolicyTests.cs:36-45`
  pins that answer. The payload loads by roll-forward, as `Directory.Packages.md:178-182` describes.
- `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:425-428`: the
  `EMBED_SYSTEM_TYPES` symbol is defined for any version at or above C# 14, so C# 15 keeps it.
- `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.cs:553`,
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionHelperProvider.cs:219`,
  `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/ProjectModel.ProjectFeaturesImpl.cs:25`: two
  relational comparisons against C# 14 and C# 9, and one equality pattern that matches C# 9 and C# 10 alone. C# 15
  takes the same branch as C# 14 at all three sites, which is the intended outcome.
- `Metalama.Framework/src/Metalama.Framework.Engine/Services/CompilationContext.cs:180-181` reads the effective
  version of the first syntax tree and enumerates no version.
- `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:239-240` hashes the
  template language version into the project hash, so a change of that version invalidates the compile-time cache as
  intended.
- `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:56-79` validates
  `MetalamaTemplateLanguageVersion` against `All`, and the standalone scenario that pins `12.0` for a library built
  by an older Metalama keeps its meaning.
- `Metalama.Framework/src/tests/Standalone/CSharp10/CSharp10.csproj:5` and the aspect tests that pin a version below
  C# 14 keep their meaning.
- `Directory.Packages.props:217-219` and
  `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:53-55`: the Roslyn package
  versions follow `RoslynApiMinVersion`, `RoslynApiMaxVersion` and the per-variant override, so they follow the two
  properties of LV-12 automatically.
- `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:37-38`: the generated source
  directory follows the variant identity and needs no edit of its own for the stable transition.
- `eng/src/Program.cs:149-152` exports `RoslynApiMaxVersion`, `RoslynMaxVersion` and `LangMaxVersion` to dependent
  repositories, so the transition propagates to Metalama.Premium through the version strings alone.
- `.teamcity/settings.kts:72`, `:82` carries no .NET SDK, runtime or Roslyn version and invokes `Build.ps1` inside
  the generated image, so the container definition is the only place where the .NET SDK is decided.
- No source of `Metalama.Extensions`, `Metalama.Patterns`, `Metalama.Migration` or `Metalama.LinqPad` mentions a
  language version, a Roslyn version or a Roslyn `InternalsVisibleTo` name, apart from `$(LangMaxVersion)` in the
  three `Directory.Build.props` files (LV-7).
- No file in the repository mentions `CSharp15`, the value 1500 or `15.0` as a language version. The only matches
  are `ToolsVersion="15.0"` in two legacy project files and the analysis documents themselves.
- `eng/RoslynVersions/Roslyn.5.10.0.props:8-10` defines the only Roslyn variant preprocessor symbol, used by two
  aspect tests; it survives the renumbering unchanged, as `Directory.Packages.md:215` intends.

Two items that the first version of this analysis placed here have moved into findings. The comparison of a .NET 11
SDK version against `MaximumSdkVersion` was recorded as producing no warning; it produces one, and it is now part of
LV-9. The default target frameworks of the nested reference-assembly project, which still name an out-of-support
target framework, belong to theme 06.

## Related themes

- The move to the stable Roslyn 5.12 is one story shared with three other themes. This theme owns the version
  decision (LV-12), the enumeration of the edit sites (LV-13) and the correction of the procedure (LV-14). Theme 02
  owns the grammar file and the regeneration that follows it (TP-1 and TP-9), theme 05 owns the design-time and
  release-gate half (DT-3 and DT-8), and theme 07 owns the mirror edit in Metalama.Premium (PR-1), which is a
  separate pull request because a pull request cannot span two repositories.
- The language version tables are one story shared with themes 02 and 05. This theme owns LV-2, LV-3, LV-6 and
  LV-7; theme 02 contributes the same table edits seen from the template verifier and the template language
  constants (TP-2 and TP-8), and theme 05 contributes the deliberate non-change at design time (DT-4).
- Every engine reference to a C# 15 API member depends on the variant gating strategy, which theme 03 owns as
  finding CM-10. The Roslyn 5.0 variant compiles the same source files against a Roslyn that has neither the union
  syntax nor the union symbol API, production source carries no conditional compilation today, and the three
  candidate mechanisms differ in reach. Nothing in this theme names such a member, so this theme is not blocked by
  that decision; the themes that consume the tables raised here are.
- The `net11.0` and .NET 11 SDK test matrix is owned by theme 06, which carries the container change and the test
  legs that consume it. LV-9 is the statement of the prerequisite from this side, and the `MaximumSdkVersion`
  correction inside it is the same defect as finding UT-2 of theme 06, which belongs with LV-1 because both are
  defects of the same pair of package build files.
- The November 2026 platform measurement is owned by theme 06 and gathers LV-11 with the package pins whose ceilings
  are read from the same measurement. Grouping them avoids reopening
  [`platform-support.md`](../platform-support.md) and [`Directory.Packages.md`](../../../Directory.Packages.md) four
  times for one date.
- The test harness convention for a C# 15 suite is owned by theme 06 and gathers LV-8 with the design-time suite of
  theme 05 and the directory and constant conventions of theme 06.
- The documentation sweep of the previous baseline is owned by theme 06 and gathers LV-10 with the host runtime
  statement of theme 05 and the Premium drift points of theme 07. Four of its five members edit
  [`platform-support.md`](../platform-support.md) and [`Directory.Packages.md`](../../../Directory.Packages.md), so
  one pull request avoids five conflicting edits to the same documents.
