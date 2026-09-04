# Impact of .NET 11, C# 15 and Roslyn 5.10 on Metalama 2027.0

This directory holds the impact analysis of .NET 11, C# 15 and Roslyn 5.10 on Metalama and Metalama.Premium for the
2027.0 release. It has one document per theme and a consolidated list of proposed user stories. The documents are an
analysis of the code as it stands on `develop/2027.0` in September 2026. They do not decide the platform baseline:
[`platform-support.md`](../platform-support.md) remains the authority on which platforms 2027.0 supports, and
[`Directory.Packages.md`](../../../Directory.Packages.md) on which package versions that permits.

## Inputs

### The platform baseline

PB-2027.0, general availability 2027-01-01, as recorded in [`platform-support.md`](../platform-support.md):

```
PB-2027.0 = VS 2026 LTSC · VS Code C# Dev Kit / Rider current · .NET 10 SDK · User=net10.0 ·
            .NET Framework 4.7.2 · Roslyn 5.0–5.x · Core=net10.0 / Desktop=net472
```

Two consequences of the baseline drive this analysis.

- Visual Studio 2027 ships in November 2026 with .NET 11 and C# 15, and both it and the Visual Studio 2026 long-term
  servicing channel present a Roslyn at or above 5.10. The latest payload variant, `Roslyn.5.10.0`, serves them.
  The `Roslyn.5.0.0` variant serves Rider and the C# Dev Kit, and probably a serviced Visual Studio 18.0 as well:
  the Roslyn release branch for that version still received commits in late August 2026, which a branch does not
  get unless the channel is serviced. Section 14.2 of [`DECISIONS.md`](DECISIONS.md) records the evidence, and the
  measurement in the platform checklist is what settles it.
- The .NET 11 SDK and the `net11.0` user target framework are in the supported set. The compile-time compilation is
  compiled by the Roslyn of the host and capped by the SDK that runs the build.

### The Roslyn version consumed

`RoslynApiMaxVersion` is `5.10.0-1.26365.3`, a build of the `main` branch of `dotnet/roslyn` of 2026-07-15, restored
from the `roslyn-consolidated` feed. That version will never have a stable counterpart. Roslyn publishes a stable
version every third minor, in step with the quarterly Visual Studio 2026 releases: nuget.org serves 5.0.0, 5.3.0
(2026-03-10), 5.6.0 (2026-07-02) and 5.9.0 (2026-08-17) and nothing above, and `eng/Versions.props` on `main` already
reads 5.12. The November 2026 baseline, that is the Visual Studio 2026 long-term servicing channel, Visual Studio
2027 and the .NET 11 SDK, is therefore expected to carry Roslyn 5.12. The transition from the prerelease is a
renumbering of the latest variant from `5.10.0` to `5.12.0`, following step 7 of
[`updating-roslyn.md`](../updating-roslyn.md), and it depends on `Metalama.Compiler` moving to Roslyn 5.12 first.

The C# 15 language version does not exist in any Roslyn that Metalama consumes today. The stable 5.9.0 assemblies,
inspected for this analysis, and the consumed 5.10 preview carry the union, closed-hierarchy, collection-argument
and pre-compilation API under the `RSEXPERIMENTAL006` and `RSEXPERIMENTAL007` markers, and the C# 15 features are
reachable only under `LanguageVersion.Preview`. On `main`, `LanguageVersion.CSharp15` exists, `default` and `latest`
map to it, the six features below are gated on it, and the experimental markers of the new syntax are removed. The
grammar and the public API of the stable 5.12 may still differ from `main` as read on 2026-09-03, so every finding
that depends on them must be re-checked when the stable `Syntax.xml` is imported.

### The C# 15 feature set

The list below is read from `MessageID.RequiredVersion` in `src/Compilers/CSharp/Portable/Errors/MessageID.cs` on
the `main` branch of `dotnet/roslyn` on 2026-09-03, and from the grammar file
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` in this repository. The Roslyn gate column names the gate on
`main`; in the consumed 5.10 preview every feature is gated on `LanguageVersion.Preview`.

A feature that adds no syntax node may still add a Roslyn application programming interface, and that is what
decides whether a payload variant is needed. The `closed` modifier is the example: the table records no new syntax
for it, and yet `SyntaxKind.ClosedKeyword` is a new enumeration member, so reading or emitting the modifier
requires a build against a Roslyn that declares it. Section 14.1 of [`DECISIONS.md`](DECISIONS.md) states the
criterion.

| Feature | Roslyn gate | New syntax in the 5.10 grammar | Proposal |
| --- | --- | --- | --- |
| Union types | `LanguageVersion.CSharp15` | `UnionDeclarationSyntax`, a new `TypeDeclarationSyntax`; kinds `UnionDeclaration` and `UnionKeyword` | `dotnet/csharplang`, unions |
| Closed hierarchies | `LanguageVersion.CSharp15` | None. The `closed` modifier is a token of kind `ClosedKeyword` in the modifier list | `dotnet/csharplang`, closed hierarchies |
| Labeled `break` and `continue` | `LanguageVersion.CSharp15` | An optional `Name` field on `BreakStatementSyntax` and `ContinueStatementSyntax` | `dotnet/csharplang`, labeled break and continue |
| Collection expression arguments | `LanguageVersion.CSharp15` | `WithElementSyntax`, a new `CollectionElementSyntax`; kind `WithElement` | `dotnet/csharplang`, collection expression arguments |
| Extension indexers | `LanguageVersion.CSharp15` | None. An `IndexerDeclarationSyntax` inside an `ExtensionBlockDeclarationSyntax` | `dotnet/csharplang`, extension members |
| Static members in interfaces | `LanguageVersion.CSharp15` | None | See the Roslyn API delta document |
| Unsafe evolution | `LanguageVersion.Preview` | `UnsafeExpressionSyntax`; kind `UnsafeExpression` | Not part of C# 15; out of scope for 2027.0 |

In the prerelease grammar every one of these nodes and fields carries an `ExperimentalUrl` attribute, and
`TreeReader.RemoveExperimentalDeclarations` in `eng/src/GenerateMetaSyntaxRewriter` removes them before code
generation. The generated syntax rewriters, the template compiler and the Roslyn version checker therefore know
nothing of them today. The stable grammar is expected to drop the attribute for the features that ship in C# 15, at
which point the generator starts emitting code for them and the hand-written parts of the pipeline must follow.

### The language version plumbing today

Every site that decides, caps or displays the C# language version stops at C# 14.

| Site | Current value or behaviour |
| --- | --- |
| `SupportedCSharpVersions.Latest` and `All` | `CSharp14`; `All` is C# 10 to C# 14 |
| `SupportedCSharpVersions.ToLanguageVersion` | Both `RoslynApiVersion.V5_0_0` and `V5_10_0` map to `CSharp14` |
| `SupportedCSharpVersions.GetMaxLanguageVersion` | Every Roslyn 5.x maps to `CSharp14` |
| `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` | Every SDK major of 10 or more maps to `CSharp14` |
| `LanguageVersionExtensions.ToDisplayStringSafe` | Handles the numeric values 1300 and 1400; throws for 1500 |
| `CompileTimeProjectManifest.ResolvedLanguageVersion` | Defaults to `CSharp13` when the manifest carries no version |
| `Metalama.Framework.targets`, the `LangVersion` rewrite | Accepts `12.0`, `13.0`, `14.0`, `default`, `latest`, `latestMajor` and `preview`; rewrites anything else that `Metalama.Compiler` marked as implicitly set to `12.0` and warns |
| `CompileTimeAspectPipeline.VerifyLanguageVersion` | Reports `preview` unless `AllowPreviewLanguageFeatures` is set, and reports any version outside `All` |
| `MetalamaTemplateLanguageVersion` in `Directory.Build.props` | `14.0`, raised by #1896 |

## Method

Each theme document was produced by reading the code paths end to end and citing the file and line of every claim,
then re-reading the same paths adversarially to falsify the claim. External facts were taken from the primary
sources named in each document, not from secondary material. No project was built and no test was run for this
analysis. A finding is marked as verified when the code was read, and as plausible when it rests on an inference
that the stable Roslyn 5.10 or the csharplang proposals may still change.

## Documents

The theme documents are listed in the order in which the work has to happen.

| Document | Theme |
| --- | --- |
| [`01-language-version-and-hosts.md`](01-language-version-and-hosts.md) | The C# 15 language version, the .NET 11 SDK as build host, the stable Roslyn 5.10 |
| [`02-syntax-generator-and-templates.md`](02-syntax-generator-and-templates.md) | The syntax generator, the template compiler and the template language |
| [`03-code-model-unions-closed.md`](03-code-model-unions-closed.md) | Union types and closed hierarchies in the code model |
| [`04-linker-and-advice.md`](04-linker-and-advice.md) | The linker, advice, transformations and eligibility |
| [`05-design-time-workspaces-linqpad.md`](05-design-time-workspaces-linqpad.md) | The design-time pipeline, workspaces, LinqPad and the test framework |
| [`06-user-tfm-patterns-tests-docs.md`](06-user-tfm-patterns-tests-docs.md) | The `net11.0` user target framework, runtime dependencies, patterns and documentation |
| [`07-premium.md`](07-premium.md) | Metalama.Premium |
| [`08-roslyn-api-delta.md`](08-roslyn-api-delta.md) | The Roslyn public API delta from 5.0 to 5.10 and the feature semantics |
| [`user-stories/`](user-stories/README.md) | The proposed user stories, one file per story, for review before any issue is created |

## Related issues

Closed on `develop/2027.0`: #1876 removed .NET 8 and .NET 9, #1881 moved to Roslyn 5.10 and dropped the Roslyn 4.12
variant, #1885 declared the prerelease package source, #1896 raised the template language version to C# 14, #1897
and #1912 re-derived the Visual Studio package caps, #1898 degrades to no implementation below the Roslyn floor.

Open: #1903 re-derives the .NET 8.0 line pins of user-surfacing packages, #1913 aligns Metalama.Premium with
PB-2027.0 (metalama/Metalama.Premium#82 carries its Roslyn half), #985 is the template compiler catch-all for later
C# features, #1217 asks for multiple Roslyn versions in Metalama.Extensions.Metrics.
