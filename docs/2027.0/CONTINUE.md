# Continuation brief: the .NET 11 and C# 15 analysis for Metalama 2027.0

This document lets another session, in the cloud or on a workstation, continue the analysis without
repeating the research. Read it first, then read `research/DIGEST.md` and `research/TERRAIN.md`.

Written 2026-09-03 on branch `topic/2027.0/26-09-03-net11-research`, based on `develop/2027.0` at
`acce80e1ab`.

## The objective

Establish everything that .NET 11 and C# 15 require from Metalama 2027.0, in both the `metalama/Metalama`
and the `metalama/Metalama.Premium` repositories, and express it as a set of Markdown files under
`docs/2027.0/`, one file per user story, which are then filed as GitHub issues.

## What is already done

The research is complete and is committed under `docs/2027.0/research/`.

| File | Lines | Contents |
| --- | --- | --- |
| `research/DIGEST.md` | 4548 | What .NET 11, C# 15 and Roslyn 5.10 to 5.12 change. Every claim is verified against a primary source. Section 8 records seventeen contradictions between sources and how each was resolved. Section 9 records forty questions the published sources do not answer. |
| `research/TERRAIN.md` | 3265 | Where the Metalama source tree is sensitive to the shape of the C# language and to platform versions. A table of 308 hotspots with paths and line numbers. Section 3 traces how each kind of language addition propagates from the grammar to the tests. Section 4 does the same for each platform axis. Section 5 lists the places that fail silently. |
| `research/*.md` | | The per-topic notes that the two documents above consolidate, and the completeness critique that drove the second research round. |
| `research/analysis-workflow.js` | | The impact-analysis workflow described below, as it was written. |

## What remains

The impact analysis, and then the writing of the story files. The analysis was designed and started but was
stopped before it finished. Its design is in `research/analysis-workflow.js`:

1. Twenty-one themes, one agent each, each deriving candidate user stories from the digest, the terrain and
   the actual code. The themes are named in the `THEMES` array of the script.
2. Three adversarial verification lenses applied to each theme's candidates: whether the premise is
   factually correct about .NET 11 and C# 15; whether the work is already done or already tracked as a
   GitHub issue; and whether the story is in scope for 2027.0 and correctly shaped. A candidate refuted by
   two of the three lenses is dropped. Each lens also reports work the candidates do not cover.
3. Consolidation into `BACKLOG.md`, grouped into epics, with duplicates merged, dependencies made explicit,
   and an appendix recording the candidates that were considered and rejected.

Then the backlog is split into one Markdown file per story under `docs/2027.0/`, and `docs/2027.0/README.md`
is written as the index.

### Before running the workflow

Three constants at the top of `research/analysis-workflow.js` carry absolute paths from the workstation it
was written on. Change all three:

- `NOTES` must point at the `docs/2027.0/research` directory of the checkout.
- `REPO` must point at the root of the `metalama/Metalama` checkout.
- `PREMIUM` must point at the root of the `metalama/Metalama.Premium` checkout.

The `metalama/Metalama.Premium` repository is private and is a separate clone. One of the twenty-one themes
covers it, and several others read it. Without it, that theme cannot be answered and must be recorded as
outstanding rather than guessed.

The prompts also state that the checkout is on branch `topic/2027.0/26-09-03-net11-impact`. That branch was
abandoned. The correct branch is `topic/2027.0/26-09-03-net11-research`, which is this one.

## The findings that must not be re-derived

These were established against primary sources or against the code, and they order the work.

### C# 15 cannot be supported until Metalama.Compiler merges a newer Roslyn

The Roslyn fork that `metalama/Metalama.Compiler` has merged, and that this repository references through
`RoslynApiMaxVersion`, which is `5.10.0-1.26365.3`, does not declare `LanguageVersion.CSharp15`.

| Branch | Roslyn version | Declares `CSharp15` |
| --- | --- | --- |
| `dotnet/roslyn` `main` | 5.12 | Yes, `CSharp15 = 1500` |
| `dotnet/roslyn` `release/stable` | 5.10 | No |
| `dotnet/roslyn` `release/dev18.3` | 5.3 | No |
| `metalama/Metalama.Compiler` `topic/2027.0/207-merge-roslyn-5.10` | 5.10 | No |

That merge is the root dependency of the whole language wave, and it belongs to a third repository.

### The upgrade is not optional

The .NET 11 software development kit makes C# 15 the default language version of a `net11.0` project.
`_MaxAvailableLangVersion` is `15.0` in the `Microsoft.CSharp.Core.targets` of Roslyn `main`. A user who
retargets to `net11.0` therefore writes C# 15 whether or not Metalama understands it. This is RES-13 of the
digest.

### Seven features are gated on C# 15, and the memory-safety work is not among them

`MessageID.cs` maps six identifiers to `LanguageVersion.CSharp15`: collection expression arguments, unions,
non-virtual static members in interfaces, closed class hierarchies, labeled `break` and `continue`, and
extension indexers. Non-virtual static members in interfaces is absent from the published "What's new in
C# 15" page, and no proposal document exists for it. The memory-safety work, which is
`IDS_FeatureUnsafeEvolution`, maps to `LanguageVersion.Preview`. Step 3 of
`Metalama.Framework/docs/updating-roslyn.md` says that experimental features are not supported, so the
memory-safety work is out of scope for 2027.0 and owes only a diagnostic.

### A union is a struct, not a new type kind

`EnumConversions.ToTypeKind` maps a union declaration to `TypeKind.Struct`, the emitted intermediate
language extends `System.ValueType` and is sealed, and there is no `TypeKind.Union`. The new symbol surface
is `ITypeSymbol.IsUnion` and `ITypeSymbol.UnionCaseTypes`. A hand-written `[Union] class` is also a union
type, with `TypeKind.Class`. This is RES-01 of the digest.

### The collection expression argument changes syntax trees at every language version

`LanguageParser.ParseCollectionElement` on Roslyn `main` parses `with (` as a `WithElementSyntax`
unconditionally, with no language-version check. The published documentation says the pre-C#-15 behaviour is
preserved. The two disagree. This is RES-16, carried as open question OQ-02, and it matters because it means
Metalama meets the new node even in a C# 14 project.

### Runtime async is off by default for user code

`UseRuntimeAsync` is defined only inside the `dotnet/runtime` repository's own build. The .NET software
development kit targets contain no occurrence of `runtime-async` or `RuntimeAsync`. This is RES-10. Whether
it stays off at general availability is open question OQ-21.

### The grammar generator hides the new syntax today

`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs`, at lines 70 to 78 and 90 to 109, deletes every
grammar element carrying `ExperimentalUrl` before code generation runs. All five C# 15 grammar additions in
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` carry it. Nothing downstream can see them until the
refreshed grammar snapshot no longer marks them experimental.

### Three version tables would let a C# 15 construct through silently

`Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` maps both `V5_0_0` and `V5_10_0` to
`AllLanguageVersions.CSharp14`, so the template version gate compares a C# 15 node as C# 14 and accepts it.
`Utilities/LanguageVersionProvider.cs`, at lines 54 to 60, caps any software development kit of major
version 10 or above at C# 14. `Utilities/AllLanguageVersions.cs` has no `CSharp15` constant.
`Metalama.Framework.Package/build/Metalama.Framework.targets`, at lines 118 to 121, clamps the implicit
language version, so a `net11.0` project whose software development kit sets 15.0 is compiled as C# 12.

### The most likely silent breakage in the Roslyn upgrade

`BreakStatementSyntax` and `ContinueStatementSyntax` gained an optional `Name` child. The child count goes
from three to four and the new child is inserted in the middle, so an existing call to the old `Update`
overload drops the label. Every such call in Metalama has to be found.

## What was already delivered on develop/2027.0

Do not propose this work again. Issues #1876, #1881, #1884, #1885, #1887, #1893, #1894, #1896, #1897 and
#1898 are closed and merged. Together they established the platform baseline PB-2027.0, documented in
`Metalama.Framework/docs/platform-support.md`, removed `net8.0` and `net9.0`, dropped the Roslyn 4.12
variant, raised `RoslynApiMaxVersion` to the prerelease Roslyn 5.10, and raised
`MetalamaTemplateLanguageVersion` to 14.0.

Issues #1860, #1864, #1903, #1913, #1343 and #985 are open and already cover part of the ground. Reference
them rather than duplicating them.

## The house rules for the story files

`CLAUDE.md` and the `eng` skill state them. In short: be accurate; use precise software engineering
language and no analogies; state the subject in the first clause; expand any acronym that is not standard in
this codebase; assume the reader is not a native speaker of English; do not use bold for emphasis inside a
paragraph; never use an em dash. Match the register of the C# 14 issues, which were terse. A story is one
deliverable, small enough to be one pull request, and its acceptance criteria include the tests that must
exist, because a Metalama feature without an aspect test under `Tests/Aspects/` is not finished.
