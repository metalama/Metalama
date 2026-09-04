### S-15. C# 15: enable the language version

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-01, S-02, S-09, S-13
- Findings: [LV-3](../01-language-version-and-hosts.md), [LV-6](../01-language-version-and-hosts.md),
  [LV-7](../01-language-version-and-hosts.md), [LV-8](../01-language-version-and-hosts.md),
  [TP-2](../02-syntax-generator-and-templates.md), [TP-8](../02-syntax-generator-and-templates.md),
  [DT-4](../05-design-time-workspaces-linqpad.md), [DT-7](../05-design-time-workspaces-linqpad.md),
  [UT-19](../06-user-tfm-patterns-tests-docs.md)

---

`SupportedCSharpVersions.Latest` returns `LanguageVersion.CSharp14` at
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `All` lists C# 10 to
C# 14 at `:38-43`, `ToLanguageVersion` maps both the lower and the latest Roslyn variant to C# 14 at `:59-60`, and
`GetMaxLanguageVersion` returns C# 14 for every Roslyn 5 at `:152`. This story raises them, and after it a 2027.0
preview accepts a C# 15 project instead of reporting `LAMA0051`.

#### Context

`Latest` is a shared constant today, and C# 15 is valid only in the latest Roslyn variant, so it has to become
variant-aware rather than a constant: the Roslyn 5.0 variant rejects the value 1500. There is no interim state in
which any part of this change is correct, because no Roslyn that Metalama consumes before S-13 has
`LanguageVersion.CSharp15`. Two items of the story close with no code: the operator table needs nothing for C# 15,
because C# 15 adds no user-definable operator, and the design-time pipeline verifies no language version by design,
which is correct and is recorded rather than changed. Section 4 of [`DECISIONS.md`](../DECISIONS.md) keeps
`MetalamaTemplateLanguageVersion` at `14.0`, so the template language does not move with the run-time ceiling and the
distinction has to be stated where both values are written.

The precedent of this story is the closed issue #1039, which grouped the twenty C# 14 stories of the previous release
under #1045. One open issue touches the same code: #1900 reports that
`LanguageVersionProvider.GetLanguageVersionFromMSBuild` throws when neither `NETCoreSdkVersion` nor `MSBuildBinPath`
is defined for a project, which the user sees as `LAMA0001` from the design-time analyzer. This story adds an arm to
the same method for the .NET 11 SDK, so it must not change that failure path, and #1900 is referenced from it.

#### Scope

- Raise `SupportedCSharpVersions.Latest` and `All`, add C# 15 to `AllLanguageVersions` as a numeric cast so that the
  name compiles against both variants, make `Latest` variant-aware, and map only the renamed latest variant in
  `ToLanguageVersion`.
- Add the arm to `GetMaxLanguageVersion` at the Roslyn version at which the toolset actually raises the implied
  version, and not at the version of the consumed preview.
- Extend `LanguageVersionProvider` for the .NET 11 SDK, so that the compile-time compilation is capped at the version
  the SDK actually offers.
- Extend the accepted value list of the `LangVersion` clamp in `Metalama.Framework.targets:118` and the related
  MSBuild constants, whose warning S-01 has already corrected.
- Make `CompileTimeAspectPipeline.VerifyLanguageVersion` and the template verifier report `LAMA0052` for a version
  above the ceiling rather than crash, and cover `LAMA0232` and `LAMA0282` for C# 15 syntax used in a template.
- Extend the comment at `Metalama.Framework/Directory.Build.props` so that the ceiling of this repository, the
  ceiling of the product and the template language version are three distinct values with three distinct reasons.
- Make the aspect test harness able to request C# 15, and decide D-10, that is whether an unavailable language
  version that the baseline claims to support fails the test rather than skipping it.
- Establish the test conventions of a `Tests/Aspects/CSharp15` directory whose `metalamaTests.json` names the
  constant of the renamed variant, following the layout of the C# 14 suite.

#### Acceptance criteria

- A project that sets `LangVersion` to `15.0` is compiled by Metalama with no diagnostic, and one that sets `16.0`
  reports `LAMA0051` naming the supported versions.
- A `net11.0` project whose language version is implied is not rewritten to a lower version.
- An aspect test that requests C# 15 runs on the latest variant, and its treatment on the lower variant follows the
  answer to D-10 and is visible in the test output.
- `MetalamaTemplateLanguageVersion` is unchanged, and the reason is written next to the value.
- The operator table and the design-time pipeline are unchanged, and the analysis that says so is recorded.

#### Not in scope

This story does not raise the template language version, which section 4 of [`DECISIONS.md`](../DECISIONS.md) excludes,
and it does not deliver the language features themselves, which are S-18-1 to S-18-6.

— Claude for @gfraiteur
