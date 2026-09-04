# Proposed user stories for Metalama 2027.0

This document proposes the user stories of the .NET 11, C# 15 and Roslyn 5.12 work for Metalama 2027.0. It exists so
that the decomposition can be reviewed before anything is filed. It is a draft for approval: no issue has been
created, and none is to be created in `metalama/Metalama` or in `metalama/Metalama.Premium` until the product owner
approves this document.

The sources are the theme documents of this directory, which carry the verified findings and the file and line
reference of every claim; [`DECISIONS.md`](DECISIONS.md), which records the answers taken by the product owner on
2026-09-04; [`OPEN-QUESTIONS.md`](OPEN-QUESTIONS.md), which records what is not answered; the later analyses under
[`analysis-reports`](analysis-reports), which answer questions that the theme documents left open; and the survey of
the open pull requests of the two repositories. The platform baseline itself is not decided here:
[`platform-support.md`](../platform-support.md) remains the authority on which platforms 2027.0 supports,
[`Directory.Packages.md`](../../../Directory.Packages.md) on which package versions that permits, and
[`updating-roslyn.md`](../updating-roslyn.md) on the procedure for moving a Roslyn variant.

Each story is written as the issue body would be posted, preceded by the metadata that a person filing it has to
choose. The issue type, the labels and the milestone are proposals and must be checked against the label set of the
repository before an issue is created. Every story is a sub-issue of the meta-issue #1921, which groups the platform
work of this release. The C# 15 stories, which are S-11 to S-22, are grouped under a new feature issue named C# 15,
itself a sub-issue of #1921. That structure follows the previous release, in which the closed issue #1039, named
C# 14, grouped twenty sub-issues under the closed meta-issue #1045, named .NET 10 Support. The `Size` field is an
estimate of the effort of the pull request, on the small, medium and large scale that the repository already uses.
The `Blocked by` field names the stories that must be merged before this one starts, and the Mermaid graph of the
next section draws the same relation.

Two rules of [`DECISIONS.md`](DECISIONS.md) govern how the stories are written. Section 7 says that a story states
the capability, the scope and the acceptance criteria, and not the shape of a public application programming
interface; section 7b says that the drafted shapes under `analysis-reports` are illustrative material for the
implementer and carry no authority. The stories below therefore name the files, the properties and the members that
exist today, and describe a new member by what it must report rather than by its signature.

Every verified finding of the theme documents is assigned exactly once: to one story, or to the last section with
the reason why it produces no story. A finding may be named again in the text of the story that owns it.

## Decisions required before the stories are created

Most of these questions are answered. The subsections below state the question, the options and their consequences,
and then either the answer that [`DECISIONS.md`](DECISIONS.md) records or, where the question is still open, the
recommendation and the work that waits on it. A reader who wants only the open items should read D-3, D-5, D-9, D-10,
D-11 and D-12.

### D-1. Does C# 15 support ship in 2027.0, and in what depth?

Every C# 15 item is downstream of one external event, which is `Metalama.Compiler` rebasing onto the stable Roslyn
that carries `LanguageVersion.CSharp15`. That Roslyn is expected to be 5.12, shipping in November 2026 with Visual
Studio 2027 and the .NET 11 SDK, against a general availability date of 2027-01-01.

- Option A, full support. Stories S-09 and S-11, and the dependent feature stories S-12 to S-22, must be written,
  reviewed and tested in roughly six weeks after the stable Roslyn exists. None of the feature work can be validated
  before it, because a test that requests C# 15 today is skipped and the suite still reports success.
- Option B, the plumbing and a defensive subset of the work. A C# 15 project builds, reports the designed diagnostics
  instead of crashing, and is not silently downgraded by the MSBuild clamp, while the language features are
  documented as unsupported.
- Option C, defer the Roslyn move. This option is not viable, because three published packages would keep a
  dependency on a Roslyn package that nuget.org does not serve, which is the restore failure of #1106.

Answer: Option A, recorded in [`DECISIONS.md`](DECISIONS.md) section 1. The schedule risk of the window between the
November 2026 Roslyn and the general availability date is accepted rather than avoided.

### D-2. How is the C# 15 Roslyn application programming interface gated between the two variants?

The same engine sources are compiled for the `Roslyn.5.0.0` variant and for the latest variant, and Roslyn 5.0 has
neither the union syntax node nor the union and closed symbol members. Three mechanisms were considered: conditional
compilation, numeric syntax kind values with a run-time guard, and a per-variant service that reads the members by
reflection. A numeric kind names no absent member but cannot override a virtual visitor method or call a syntax
factory. A reflection shim repeats what #1215 deliberately removed.

Answer: conditional compilation, recorded in [`DECISIONS.md`](DECISIONS.md) section 2. A symbol in the manner of
`ROSLYN_5_12_0_OR_GREATER` is defined by the latest variant property file, and the sources that name the C# 15 Roslyn
members are compiled only in that variant. The notes in `eng/RoslynVersions/Roslyn.5.0.0.props` and in the latest
variant property file that state that no production source branches on the variant, and the corresponding paragraph
of [`Directory.Packages.md`](../../../Directory.Packages.md), are superseded and are rewritten by S-02.

### D-3. What does the Roslyn 5.0 variant do when it meets a union or a closed type that it cannot represent?

Open. It is question Q2 of [`OPEN-QUESTIONS.md`](OPEN-QUESTIONS.md), and it applies to every C# 15 reader. The public
assembly `Metalama.Framework` is not built per Roslyn version, so a member that reports whether a type is a union
exists in every host while the engine code that answers it is compiled only in the latest variant. On the hosts that
the `Roslyn.5.0.0` variant serves, which are Rider and the Visual Studio Code C# Dev Kit, such a member reports false,
an aspect sees a union as an ordinary struct, and the editor and the command line disagree.

- Option A, report nothing. This is the behaviour that follows from doing nothing, and it is the failure mode that
  the platform doctrine exists to prevent.
- Option B, report the divergence. The diagnostic analyzer reports a design-time warning once per project, and the
  warning carries an opt-out. It is reported in an editor whose user can act on it only by changing the development
  environment.

Recommendation: Option B, which the draft in
[`analysis-reports/12-csharp15-api-drafts.md`](analysis-reports/12-csharp15-api-drafts.md) also recommends. The
severity and the opt-out of that diagnostic are question Q3 and are settled inside the story. One measurement could
change the answer, which is Q6, the Roslyn version that a current Rider presents. The answer decides one bullet of
S-12 and one bullet of S-18, which are the two stories that read a C# 15 type on both variants, and neither story is
blocked on it beyond that bullet.

### D-4. For a union that an aspect targets, is the answer a single refusal rule, per-advice rules, or nothing?

The language forbids instance fields, auto-properties and field-like events in a union declaration, forbids a public
single-parameter constructor, and requires every explicit constructor to chain to a generated one. Several ordinary
advices therefore emit code that the compiler rejects, with the diagnostic reported on generated code.

- Option A, one eligibility rule that declares a union an unsupported advice target. The rule is small, and it covers
  the three compiler errors and the silent case of an initializer injected into a constructor that has no syntax.
- Option B, per-advice rules that permit what a union can carry and refuse only what it cannot. These rules are more
  work, and each rule is a place where the language restrictions can be misread.
- Option C, no rule at all. The generated code is then silently invalid.

Answer: Option B, recorded in [`DECISIONS.md`](DECISIONS.md) section 3. Unions are supported as aspect targets, and
what a union cannot carry is refused with a clear diagnostic. Question Q9 attaches a condition to every such rule:
`ITypeSymbol.IsUnion` is true both for a `union` declaration and for a type carrying
`System.Runtime.CompilerServices.UnionAttribute`, while the member restrictions apply to the first form only, so each
rule must state which of the two forms it tests.

### D-5. Are introduced unions and introduced closed classes in scope?

These are two answers, and one of them is only half taken.

Introducing a closed class is out of scope, recorded in [`DECISIONS.md`](DECISIONS.md) section 5b and question Q4.
The work is small and every part of it is identified, and it is deferred because no customer scenario is known that
needs an aspect-generated closed hierarchy, and because of the design-time divergence of D-3. A known scenario
reopens it. Findings CM-4 and LK-5 are withdrawn on that basis.

Introducing a union, and introducing a case into an existing union, is required, recorded in
[`DECISIONS.md`](DECISIONS.md) section 5c, which overrides section 5b for unions only. This is the largest single
piece of C# 15 work in the release and it is story S-17.

What is still open is question Q1, the second half of that requirement.

- Option A, ship both authoring forms. For a type carrying the union attribute, adding a case is the introduction of
  a constructor, a generated partial part can express it, and the editor and the build agree. For a type declared
  with the `union` keyword, exactly one part carries the case list, so the operation rewrites the part the user
  wrote, it works at build time only, and the editor cannot show the added case. That divergence needs a design-time
  diagnostic which reports it but does not repair it.
- Option B, ship the attribute form only. Nothing diverges, and an aspect cannot add a case to a union that a user
  wrote with the concise syntax.

Recommendation: Option A, in that order, taking the attribute form first because it is small and its design-time
result is correct. If only one form fits the release it should be the attribute form. The answer decides the size of
S-17 and whether that story carries a design-time diagnostic.

### D-6. Does the template language version move to C# 15?

`MetalamaTemplateLanguageVersion` is pinned to `14.0`, and the pin is bounded by the lowest supported Roslyn variant,
because the compile-time assembly of an aspect library must be compilable inside every supported design-time host.

- Option A, keep `14.0`. Aspect authors cannot use a C# 15 feature inside a template, which is a documented
  limitation that costs nothing.
- Option B, raise the default to `15.0`. The compile-time compilation of such a project fails inside a host on
  Roslyn 5.0 with an unsupported language version error, which is a hard failure and not a degradation.
- Option C, offer `15.0` as an opt-in property, at the cost of a support matrix with two template language versions.

Answer: Option A, recorded in [`DECISIONS.md`](DECISIONS.md) section 4. The same section adds a consequence that
changes one story: a labeled `break` or `continue` inside a template is forbidden and is reported with a diagnostic,
because the annotator cannot classify a label whose loop may be in a different scope than the statement that names
it. Run-time code that an aspect transforms and that uses a label outside a template is not affected and must keep
working once the syntax model is regenerated. Story S-19 delivers the rejection and the run-time correctness, and no
longer proposes support for labels in templates.

### D-7. Is the .NET 11 SDK installed in the build container, and what does `global.json` pin?

- Option A, install both feature bands and keep `global.json` on the .NET 10 SDK. It exercises a configuration that
  is known to be fragile, because two bands under one `dotnet` directory already produced an `MSB4062` restore
  failure through a stale `MSBuildExtensionsPath`, and the mitigation in PostSharp.Engineering is incomplete by that
  one variable.
- Option B, install the .NET 11 SDK alone. One band and no conflict, but the product is then built by a compiler
  whose default language version differs from the one the tests assume.
- Option C, keep one SDK and accept that `net11.0` stays declared and untested.

Answer: Option C, recorded in [`DECISIONS.md`](DECISIONS.md) sections 6b and 6c. The container change has no
justification, because no .NET 11 application programming interface is wanted. Two things stay in scope and need no
installed SDK: the supported-toolchain check must not report `LAMA0601` for a supported .NET SDK, and the
`LangVersion` clamp must not rewrite the language version that a `net11.0` project implies. Both are properties of a
comparison and are verified by a test of that comparison. Story S-04 delivers them. Findings LV-9, UT-1, UT-6, UT-7,
UT-8 and PR-8 are withdrawn on this basis.

### D-8. Does a `net11.0` leg run in the test matrix?

Adding `net11.0` beside `net10.0` in every test project doubles the Core dimension of the longest part of the build,
and an unknown number of expected-output files may diverge between the legs.

Answer: no, recorded in [`DECISIONS.md`](DECISIONS.md) sections 6 and 6c. A leg is justified only by a .NET 11
application programming interface that Metalama wants to use, and the analysis in
[`analysis-reports/09-net11-api-value.md`](analysis-reports/09-net11-api-value.md) found none: the .NET 11 additions
are numeric types, domain name resolution, compression, process management, text, streams and vector intrinsics, and
none of them is on a path that Metalama uses. Neither repository contains a polyfill file, no production source
branches above `NET8_0_OR_GREATER`, and every shim serves the `netstandard2.0` and `net472` assets, which a `net11.0`
asset would not remove. Finding UT-5 is withdrawn on this basis.

### D-9. Is the November 2026 measurement a release blocker?

`Metalama.Framework.props` and `Directory.Packages.md` both schedule a re-reading of the Visual Studio floor, the
feature band and the host-capped package pins after 2026-11-10, and the build container pins Visual Studio Build
Tools 18.9.2, which is a quarterly release below the long-term servicing floor that PB-2027.0 names.

- Option A, measure immediately after the Visual Studio 2027 and long-term servicing releases and treat the result as
  a release blocker, so that the pins describe the hosts that 2027.0 actually supports.
- Option B, ship with the current values and re-derive in 2027.0.1, accepting that the declared minimum names a floor
  that the build host never exercises.

Recommendation: Option A for the Roslyn version of the baseline and for the variant identity, which are checklist
items 1 and 2 of [`platform-support.md`](../platform-support.md) and which decide whether S-09 renumbers to 5.12 or
to another value; Option B for the host-capped package pins, which are S-08. Whichever is chosen, the measurement
must not compete for the same engineering days as S-09 and S-11, which are the critical path.

### D-10. Should the aspect test harness fail rather than skip when a requested language version is unavailable?

Today a language version that the running Roslyn does not recognise marks the test skipped with a reason, and the
suite passes. That is correct while a variant genuinely cannot serve the version and wrong when a whole C# 15 suite
disappears without anyone noticing.

- Option A, keep skipping and rely on reviewers reading the skip list.
- Option B, fail when the version is one that the current baseline claims to support, and skip only when the running
  variant is the reason.

Recommendation: Option B. It changes the behaviour of a shared harness that the Patterns and the Premium suites also
use, so it needs an explicit decision inside S-11.

### D-11. Is the prerelease Roslyn pin a release gate for previews as well as for general availability?

`Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting` and `Metalama.LinqPad` declare a dependency on a
Roslyn version that nuget.org does not serve. That failure already reached a user in #1106.

- Option A, gate general availability only, which reproduces that failure for anyone who restores a preview.
- Option B, gate every public publication, which means either moving to the stable Roslyn earlier or renumbering the
  latest variant to the stable 5.9.0, which exposes the same public application programming interface as the consumed
  preview and is available today, and adding a build-time check that refuses to pack a prerelease Roslyn pin.

Recommendation: Option B. It costs one target and removes a class of user-facing failure. The answer decides one
acceptance criterion of S-09.

### D-12. How far does Metalama.Premium follow the core in 2027.0?

- Option A, Premium follows fully: S-10 renumbers the variants, S-22 adds the reference graph work and the
  architecture rule tests, and S-23 closes the coverage gap of the Roslyn 5.0.0 variant, all inside the same narrow
  window.
- Option B, Premium ships the renumbering only, which it must do in any case for the variant names and the payload
  paths to match the core, and defers the union-aware architecture rules to 2027.1, accepting that a rule on a type
  used as a union case under-reports with no diagnostic.

Recommendation: the renumbering of S-10 is mandatory in either option; S-22 and S-23 are the negotiable part. A
separate part of the same question is whether the missing execution of the Roslyn 5.0.0 variant is accepted for
another release: that variant serves Rider and the C# Dev Kit, and no member of the team exercises those hosts by
hand now that Visual Studio 2022 is dropped, so the risk rose this release rather than staying constant.

### D-13. Is Roslyn 5.12 added beside Roslyn 5.10, or does it replace it?

Answer: it replaces it, recorded in [`DECISIONS.md`](DECISIONS.md) section 8. No supported Visual Studio presents
Roslyn 5.10 or 5.11, because Roslyn publishes a stable package every third minor version and neither is published. A
variant whose identity is 5.10.0 would serve no host that a 5.12.0 identity does not serve, and rule 8 of the
doctrine forbids a variant that serves an empty set. The variant set of 2027.0 therefore stays at two. Two statements
of [`platform-support.md`](../platform-support.md) follow from this and are recorded as question Q10 rather than
applied, because the document belongs to the product owner; S-25 carries them once they are approved.

## Ordering

The list below reads as a dependency order. A story that names no blocker can start at once.

1. S-01, the move of `Metalama.Compiler` to the stable Roslyn 5.12. It is not work in this repository, it is the head
   of the critical path, and its schedule is the single most valuable piece of planning information for 2027.0.
2. S-02, the variant gating, S-03, the type-declaration predicate, S-04, the two MSBuild comparisons, S-05, the
   residue of the previous baseline, S-06, the language version display mapping and the manifest fallbacks, and
   S-07, the declaration-kind switches. All six are independent of the Roslyn gate and are the pre-gate lane.
3. S-08, the November 2026 measurement. Calendar-gated rather than dependency-gated, and it must not compete with the
   critical path.
4. S-09, the renumbering of the latest variant to Roslyn 5.12 and the regeneration of the syntax model. Blocked by
   S-01. It is the gate of the whole release.
5. S-10, the Premium mirror of the renumbering. Blocked by S-09.
6. S-11, C# 15 as a supported language version. Blocked by S-04, S-06 and S-09. After it, a 2027.0 preview accepts a
   C# 15 project instead of reporting `LAMA0051`.
7. S-12, the union in the public code model. Blocked by S-02, S-03 and S-11. It is the surface that six later stories
   consume.
8. S-13, S-14, S-15, S-16 and S-22, the union consumers. All blocked by S-12; S-13 also by S-03.
9. S-17, the introduction of a union and of a union case. Blocked by S-03, S-12 and S-16, and by decision D-5 for its
   second half. About half of its work can proceed before S-09.
10. S-18, closed hierarchies in the code model, S-19, labels, S-20, the experimental syntax guard, and S-21,
    extension indexers. Blocked by S-11, and S-19 and S-20 also by S-09.
11. S-23 and S-24, the Metalama.Premium items that are independent of every gate.
12. S-25, the documentation. Deliberately last, because a document written before the code is a second thing to
    correct.

```mermaid
graph TD
  S01["S-01 Metalama.Compiler on Roslyn 5.12"] --> S09["S-09 Renumber the latest variant to 5.12"]
  S09 --> S10["S-10 Premium renumbering"]
  S09 --> S11["S-11 Raise the language version ceiling"]
  S04["S-04 LangVersion clamp and SDK ceiling"] --> S11
  S06["S-06 Display mapping and manifest fallbacks"] --> S11
  S02["S-02 Variant gating"] --> S12["S-12 Union in the code model"]
  S03["S-03 Type-declaration predicate"] --> S12
  S11 --> S12
  S03 --> S13["S-13 Compile-time and classifier dispatch"]
  S12 --> S13
  S12 --> S14["S-14 Advice applied to a union"]
  S12 --> S15["S-15 Design-time union partial part"]
  S12 --> S16["S-16 Comparers that a union breaks"]
  S12 --> S22["S-22 Patterns and the reference graph"]
  S16 --> S17["S-17 Introduce a union and a case"]
  S03 --> S17
  S02 --> S18["S-18 Read closed hierarchies"]
  S11 --> S18
  S09 --> S19["S-19 Labels in templates and in inlining"]
  S11 --> S19
  S09 --> S20["S-20 Experimental syntax guard"]
  S11 --> S20
  S11 --> S21["S-21 Extension indexers"]
  S09 --> S25["S-25 Documentation"]
  S11 --> S25
  S05["S-05 Residue of the previous baseline"]
  S07["S-07 Declaration-kind switches"]
  S08["S-08 November 2026 measurement"]
  S23["S-23 Premium Roslyn 5.0.0 variant tests"]
  S24["S-24 Premium build-file residuals"]
```

## Stories

### S-01. Move Metalama.Compiler to the stable Roslyn 5.12

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Compiler`
- Size: L
- Blocked by: nothing in this repository
- Findings: none. `Metalama.Compiler` is not cloned in the session that produced this analysis, so nothing in it was
  verified.

---

`Metalama.Compiler` on `develop/2027.0` targets the Roslyn prerelease `5.10.0-1.26365.3`, which is a build of the
`main` branch of `dotnet/roslyn` of 2026-07-15 and will never have a stable counterpart. Step 1 of
[`updating-roslyn.md`](../updating-roslyn.md) makes every Roslyn move in `metalama/Metalama` conditional on
`Metalama.Compiler` moving first, so the whole C# 15 plan of 2027.0 depends on a date that is decided in that
repository.

#### Context

nuget.org serves the stable `Microsoft.CodeAnalysis.CSharp` versions 5.0.0, 5.3.0, 5.6.0 and 5.9.0 and nothing above
them in the 5 generation, and `eng/Versions.props` on the `main` branch of `dotnet/roslyn` reads minor version 12.
Roslyn publishes a stable package every third minor version, in step with the quarterly Visual Studio 2026 releases,
so the November 2026 baseline, that is the Visual Studio 2026 long-term servicing channel, Visual Studio 2027 and the
.NET 11 SDK, is expected to carry Roslyn 5.12. `LanguageVersion.CSharp15` does not exist in any Roslyn that Metalama
consumes today; it was added to `main` on 2026-08-11. Section 8 of [`DECISIONS.md`](DECISIONS.md) records that 5.12
replaces the 5.10 variant rather than being added beside it.

#### Scope

- Rebase `Metalama.Compiler` onto the Roslyn version that Visual Studio 2027 and the .NET 11 SDK carry, expected to
  be 5.12.
- Publish the resulting `Metalama.Compiler` version and record it in `eng/AutoUpdatedVersions.props` of
  `metalama/Metalama`, where `MetalamaCompilerVersion` is currently `2027.0.0`.
- Report the date at which the rebase is expected to be complete, because the schedule of S-09, S-11 and every story
  downstream of them is derived from it.

#### Acceptance criteria

- `Metalama.Compiler` builds and passes its tests against the stable Roslyn that the November 2026 hosts carry.
- The published `Metalama.Compiler` version pins a Roslyn package that nuget.org serves.
- `metalama/Metalama` can raise `RoslynApiMaxVersion` and `RoslynMaxVersion` to that version without any prerelease
  package source.

#### Not in scope

Every edit in `metalama/Metalama` and in `metalama/Metalama.Premium`. Those are stories S-09 and S-10.

— Claude for @gfraiteur

### S-02. Apply the variant gating decision to the engine sources and to the doctrine

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [CM-10](03-code-model-unions-closed.md)

---

`Metalama.Framework.Engine.5.0.0` compiles the same source files as `Metalama.Framework.Engine` against Roslyn 5.0,
whose public application programming interface has neither `UnionDeclarationSyntax` nor `ITypeSymbol.IsUnion`.
Production source carries no conditional compilation today, and the only variant constant,
`ROSLYN_5_10_0_OR_GREATER`, is defined by `eng/RoslynVersions/Roslyn.5.10.0.props:10` and is used by two aspect tests.
Section 2 of [`DECISIONS.md`](DECISIONS.md) settles the mechanism: the C# 15 Roslyn members are reached through
conditional compilation and one implementation assembly per Roslyn version. This story applies that decision, so that
the fourteen sites which depend on it do not each decide it again.

#### Context

Issue #1881 removed 177 `#if ROSLYN_*` blocks from 152 production files and wrote the note, in both variant property
files, that no production source branches on the variant. Section 2 supersedes that note for the C# 15 members and
rejects the two alternatives that were considered, which are numeric syntax kind values with a run-time guard and a
per-variant service that reads the members by reflection; the second repeats what #1215 deliberately removed. The
decision is deliberately narrow: it covers the members that Roslyn 5.0 does not have, and it does not reopen the
general policy for anything else.

#### Scope

- Rewrite the note in `eng/RoslynVersions/Roslyn.5.0.0.props:8-10` and in the latest variant property file, and the
  corresponding paragraph of `Directory.Packages.md`, so that they state the current policy: production source may
  branch on the latest variant symbol, and only for members that the lower variant does not expose.
- State in the same place which members are covered, namely `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`,
  `SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes`, `ITypeSymbol.IsClosed` and the
  `Name` field of `BreakStatementSyntax` and `ContinueStatementSyntax`, and that the list is closed rather than a
  precedent for new branches.
- Record that the variant symbol is named after the variant and is therefore renamed by S-09, and add it to the
  rename list of that story.
- Record what the lower variant does at each site, which is to report the value that an ordinary type would report,
  and reference D-3 for whether it also reports a diagnostic.
- Settle the suppression of `RSEXPERIMENTAL006`: it is required while the latest variant is built against a Roslyn
  that still marks the union and closed members experimental, and it disappears when the variant reaches Roslyn 5.12.
- Deliver one worked example in the smallest consumer, so that the pattern is visible in the code rather than only in
  a document.
- State whether the public `Metalama.Framework.Sdk` kind helpers, which are part of the extensibility surface, may
  name the new kinds at all, since a public surface cannot easily be narrowed later.

#### Acceptance criteria

- Both variant property files and `Directory.Packages.md` describe the policy that is actually in force, and no
  document still states that production source carries no variant branch.
- One production source file compiles a C# 15 Roslyn member behind the variant symbol, and both variants build.
- The list of members that may be gated is written down, and the rule for adding to it is written down.

#### Not in scope

The union and closed features themselves. This story is the mechanism and one example.

— Claude for @gfraiteur

### S-03. Recognise any type declaration by a type test instead of by an enumerated syntax kind

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: nothing
- Findings: [CM-2](03-code-model-unions-closed.md), [CM-6](03-code-model-unions-closed.md),
  [LK-3](04-linker-and-advice.md), [DT-1](05-design-time-workspaces-linqpad.md),
  [DT-6](05-design-time-workspaces-linqpad.md)

---

`SyntaxKindExtensions.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` enumerates exactly
the class, struct, interface, record and record struct kinds, `IsBaseTypeDeclaration` derives from it at `:41`, and
the same enumeration is written by hand three more times in
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs`, at `:33-34`, `:61-62` and
`:116-117`. `SourceNamedTypeImpl.IsPartial` tests `SyntaxKind.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:344`. Four themes reported
the same five places, and every consequence they describe follows from them.

#### Context

A `partial union` reports `IsPartial` false, so `LAMA0048` is reported although the type is partial, the design-time
generator never produces the partial file, the code fix then adds a second `partial` modifier, a suppression never
reaches a diagnostic located on a union header, and the linker consumers of the same predicates fall through. The
remedy is not union-specific: interfaces and extension blocks are already missing from several of these lists today,
so this is a correctness change that also admits unions later without naming a member that the Roslyn 5.0 variant
does not have. This is the largest piece of union plumbing that can be delivered before the Roslyn gate.

#### Scope

- Replace the enumerated kinds by a test on the abstract syntax type where the intent is a declaration that can
  contain members, in `SyntaxKindExtensions.IsTypeDeclaration`, in `SyntaxExtensions.FindMemberDeclarationOrNull`,
  `FindSymbolDeclaringNode` and `GetDeclaringType`, and in `Linking/SymbolExtensions.GetDeclarationFlags`.
- Choose deliberately between the narrow predicate that the documentation of `SyntaxKindExtensions` promises and the
  broad Roslyn helper `SyntaxFacts.IsTypeDeclaration`, which also matches delegates, enums and extension blocks and
  would therefore change shipped behaviour, and record the choice in the documentation comment.
- Review every consumer of the two predicates, and treat with care the two sites where the parameter list of a union
  is a case list and not a parameter list, which are `ImplicitLastOverrideReferenceInliner` and
  `LinkerLateTransformationRegistry`.
- Keep the record-only kind lists as they are, because they serve the record-synthesized-member logic and not the
  general question of whether a node is a type declaration.
- Keep the convention that `KindCheckOptimizationAnalyzer` of #1307 enforces, which exempts a test on an abstract
  syntax type.
- Add the unit tests that pin `IsPartial` and the suppression path for an interface and for an extension block, which
  are the cases that are wrong today.

#### Acceptance criteria

- No enumeration of concrete type-declaration kinds remains in `SyntaxKindExtensions` or in `SyntaxExtensions`.
- `IsPartial` is true for a partial interface and for every partial type declaration whose kind the predicate now
  admits, and the design-time generator produces the partial file for it.
- A suppression located on the header of an interface or of an extension block is applied.
- `KindCheckOptimizationAnalyzer` reports nothing on the rewritten sites, and both Roslyn variants build.

#### Not in scope

Naming any C# 15 syntax kind. This story compiles unchanged for both variants and adds no variant branch.

— Claude for @gfraiteur

### S-04. Correct the `LangVersion` clamp and the .NET SDK ceiling comparison of the platform check

- Issue type: Bug
- Labels: `bug`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [LV-1](01-language-version-and-hosts.md), [UT-2](06-user-tfm-patterns-tests-docs.md)

---

`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118-121` rewrites an implicitly
set `LangVersion` that is not one of `12.0`, `13.0`, `14.0`, `default`, `latest`, `latestMajor` or `preview` down to
`12.0`, and the warning at `:243-248` carries no `Code` attribute and explains the rewrite falsely. Separately,
`_MetalamaSdkVersion` at `:399` strips only the prerelease label and is then compared with `VersionGreaterThan`
against `MaximumSdkVersion` `11.0` at `:412`, so every .NET 11 SDK, including `11.0.100`, reports `LAMA0601`
although `Metalama.Framework.props:33` declares .NET 11 supported and `:39` names 10.0 and 11.0 as the supported
versions. The second defect is live today.

#### Context

Section 6b of [`DECISIONS.md`](DECISIONS.md) removes the build container work from the release and names these two
defects as what remains: both are properties of a comparison and are verified without an installed .NET 11 SDK. The
clamp is not yet reachable, because the compiler toolset caps the implied version of a `net11.0` project at `14.0`
until the Roslyn of S-09; it becomes reachable on the day that cap moves to `15.0`, and it then costs a project three
language versions at once, because a project that implied `15.0` drops to `12.0`. The ceiling defect is reachable
now: `MaximumSdkVersion` is documented as the last supported major and minor line, and comparing it against a full
version makes every feature band of that line exceed it. `MinimumSdkVersion` legitimately keeps feature-band
precision, because a contributing package may require `10.0.200`, so the two rules cannot share one property.

#### Scope

- Add a second property holding the first two components of `$(NETCoreSdkVersion)` and use it in the maximum rule at
  `Metalama.Framework.targets:412` only, leaving the minimum rule at `:406-408` on the full version, with a comment
  stating why the two differ.
- Give the `MetalamaCheckLangVersion` warning a `Code` attribute, allocated from the `LAMA06xx` platform range beside
  `LAMA0600` to `LAMA0602`, and rewrite its text so that it describes the rewrite that actually happened.
- State the suppression mechanism correctly: an MSBuild task warning is suppressed by `MSBuildWarningsAsMessages` and
  not by `NoWarn`.
- Extend the accepted value list of the clamp condition when `LangMaxVersion` moves, which is S-11, and reference
  that story from the comment so the two lists do not drift.
- Add unit tests or a standalone scenario that exercises both comparisons without an installed .NET 11 SDK, following
  section 6b of [`DECISIONS.md`](DECISIONS.md).

#### Acceptance criteria

- A build with a .NET SDK of the `11.0` line reports no `LAMA0601`, and a build with a `12.0` SDK still reports it.
- A build with a .NET SDK of `10.0.100` still reports no warning, and one with `9.0.100` still reports `LAMA0601`.
- The language version warning carries a code, and adding that code to `MSBuildWarningsAsMessages` suppresses it.
- The warning text states the version the project had and the version it was given, and no sentence of it is false.

#### Not in scope

Installing a .NET 11 SDK in the build container, and adding a `net11.0` scenario, both excluded by sections 6b and 6c
of [`DECISIONS.md`](DECISIONS.md).

— Claude for @gfraiteur

### S-05. Remove the residue of the previous platform baseline from the engine defaults and the test gates

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [UT-3](06-user-tfm-patterns-tests-docs.md), [UT-4](06-user-tfm-patterns-tests-docs.md),
  [UT-9](06-user-tfm-patterns-tests-docs.md), [UT-10](06-user-tfm-patterns-tests-docs.md)

---

Four leftovers of the move that dropped .NET 8 and .NET 9 share one property: each is invisible in a green build and
none produces a failure. `CompileTimeAssemblyLocator.cs:43` still names `net8.0` in the default target frameworks of
the nested compile-time reference project, `DefaultProjectOptions.cs:56` reports the target framework `net8.0` to
every test whatever the test assembly targets, two facts of the Contracts unit tests are excluded on every leg
because their guard names `NET6_0` rather than `NET6_0_OR_GREATER`, and two aspect tests never run because their
guard names `ROSLYN4_4_OR_GREATER`, a symbol that no variant defines.

#### Context

An additional compile-time package resolves its `netstandard2.0` asset instead of its `net10.0` asset because of the
first value, and an out-of-support target framework is restored on every build. The two guards are dead in different
ways: the Contracts guard excludes code that genuinely does not compile on .NET Framework, so it keeps a target
framework condition and only the symbol name changes; the aspect test guard survived the cleanup of #1881 because the
name does not follow the underscore convention of the variant symbols, and each run already reports the two tests as
skipped with that reason. This story is scheduled before the C# 15 test suite is written, because that suite adds new
constant gates of exactly this shape.

#### Scope

- Change `_defaultCompileTimeTargetFrameworks` in
  `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` to name `net10.0`
  instead of `net8.0`, and edit the fixtures that pin it, which are the `Issue1789` standalone scenario, its
  `README.md`, the unit test data and the comments in `IProjectOptions.cs` and `test.ps1`.
- Use `net10.0` and not `net11.0` for that value, because PB-2027.0 keeps a .NET 10 SDK as the build-time SDK.
- Raise the default of `DefaultProjectOptions.TargetFramework` from `net8.0` to `net10.0`, and re-accept the one
  aspect test snapshot that prints the value.
- Replace `NET6_0` by `NET6_0_OR_GREATER` in `Metalama.Patterns.Contracts.UnitTests/DoubleTests.cs`, keeping the
  target framework condition, then run the two facts and adopt the result.
- Remove the `ROSLYN4_4_OR_GREATER` gate from the two `InterfaceImplementation` aspect tests, keep their
  `NET6_0_OR_GREATER` requirement so that the directive and the conditional name the same symbol, run them and adopt
  their output.

#### Acceptance criteria

- No `net8.0` literal remains in the engine defaults or in the fixtures that pin them.
- A test reads the target framework of the leg it runs on, and the aspect test that prints it is re-accepted.
- The four previously excluded tests execute, and their result is read rather than adopted blindly.
- No test directive names a preprocessor symbol that no configuration defines.

#### Not in scope

Any `net11.0` leg, excluded by sections 6 and 6c of [`DECISIONS.md`](DECISIONS.md).

— Claude for @gfraiteur

### S-06. Make the language version display mapping non-throwing and settle the compile-time manifest fallbacks

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [LV-2](01-language-version-and-hosts.md), [LV-4](01-language-version-and-hosts.md),
  [LV-5](01-language-version-and-hosts.md)

---

`LanguageVersionExtensions.ToDisplayStringSafe` maps the numeric values 1300 and 1400 by a numeric cast and has no
arm for 1500, so it throws `ArgumentOutOfRangeException` while formatting `LAMA0051` or `LAMA0052` and the user sees
`LAMA0001` with a request to open a support ticket instead of the designed diagnostic. In the same area,
`CompileTimeProjectManifest.ResolvedLanguageVersion` documents C# 13, is read by nothing, and disagrees with the two
live fallbacks at `CompileTimeCompilationBuilder.cs:1355` and `CompileTimeProjectRepository.Builder.cs:596`.

#### Context

These three items become dangerous only after S-11 raises the supported language version, and all three are cheaper
to fix before it. The display mapping is a numeric cast and therefore compiles against the Roslyn 5.0 variant as
well, so it needs no variant branch. The manifest question has a recorded precedent: #1185 reported the failure of a
compile-time project produced by a higher Roslyn version and read by a lower one, with the Roslyn error `CS8192`,
which is exactly what an aspect library compiled at C# 15 and consumed under the Roslyn 5.0 variant would produce
again. #1142 is the reason the value is serialized as an integer, and that must not change.

#### Scope

- Add the arm for the numeric value 1500 to `LanguageVersionExtensions.ToDisplayStringSafe:33-39`, and give the
  method a formatted fallback for an unknown value so that it never throws.
- Either delete `CompileTimeProjectManifest.ResolvedLanguageVersion` or route both fallbacks through it with the
  value the comment documents, so that the manifest has one answer rather than three.
- Add, at the reading side, a clamp of the manifest language version to the maximum that the running variant
  accepts, and a warning that names both versions, so that a library compiled at a higher language version degrades
  instead of failing with a compiler error.
- Format the unknown value numerically in that warning, or add the display arm first, so that the diagnostic path
  itself cannot throw.

#### Acceptance criteria

- Requesting an unsupported language version reports `LAMA0051` or `LAMA0052` with the version named, and never
  `LAMA0001`.
- A compile-time project manifest that carries no language version resolves to one documented value, and the two
  reading sites agree with it.
- A manifest that carries a language version above what the running Roslyn variant accepts produces a named warning
  and a clamped parse, and not `CS8192`.
- The change compiles for both Roslyn variants with no variant branch.

— Claude for @gfraiteur

### S-07. Repair the declaration-kind switches that silently fall through, in both repositories

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Extensions`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`, `metalama/Metalama.Premium`
- Size: M
- Blocked by: nothing
- Findings: [DT-5](05-design-time-workspaces-linqpad.md), [TP-7](02-syntax-generator-and-templates.md),
  [PR-10](07-premium.md), [PR-11](07-premium.md)

---

Four places switch over declaration kinds and do the wrong thing for a kind they do not list, and none of the four is
caused by C# 15. `CSharpAttributeHelper.cs:74-191` returns null for records, record structs and extension blocks, and
the caller propagates the null, so the add-attribute code action reports success and does nothing.
`ChangeVisibilityCodeAction` in Metalama.Premium skips interfaces and indexers in the same way. The member switch of
`TransformCompileTimeType` throws for an indexer and falls through for an extension block, so a template declared
inside an extension block is copied verbatim. `ReferenceValidationContext.GetInboundGranularity` throws for a
validated extension block, and the exception is reported as an error diagnostic from inside the user validator.

#### Context

Extension blocks are a C# 14 feature and already ship, so the last of these is a defect that a customer can hit
today, and the first two are wrong for records, which have shipped for years. In every case the remedy is to test an
abstract syntax base type or to add the missing arm, which also admits unions later without naming an experimental
member. The two repositories are edited by two pull requests from one reviewed design, because a pull request cannot
span them.

#### Scope

- In `Metalama.Framework/src/Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs`, replace the per-kind
  arms for type and member declarations by one call to `MemberDeclarationSyntax.AddAttributeLists`, narrowed so that
  namespaces, enum members, global statements and incomplete members keep returning null, and keep the special cases
  for parameters, accessors and the compilation unit, which do not derive from that type.
- Keep the trivia behaviour that the tests of #779 pin, because the caller restores the leading trivia of the old
  node.
- In `Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs`, apply the modifiers
  through the abstract type declaration, in an override of `VisitCore` and not of `Visit`, which is sealed in
  `SafeSyntaxRewriter`.
- In `CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.TransformCompileTimeType`, decide between
  reporting a diagnostic for a template declared inside an extension block and supporting it; support requires
  extracting the member loop, because an extension block declaration is a type declaration whose `Identifier`
  returns default and whose base-list mutators throw.
- Add the missing arm for the extension block kind in `ReferenceValidationContext.GetInboundGranularity`, with an
  aspect test that validates an extension block containing an extension method and that compiles against the Roslyn
  version Premium consumes today.

#### Acceptance criteria

- The add-attribute code action adds the attribute to a record, a record struct and an extension block, and the
  existing trivia tests do not regress.
- The change-visibility code action changes the visibility of an interface and of an indexer.
- A template declared inside an extension block either compiles or is reported with a diagnostic that names the
  reason, and is never copied verbatim.
- A reference validator that validates an extension block reports its own diagnostics and no exception.

#### Not in scope

Unions. Every arm added here is written so that a union is admitted later without a further edit, but no C# 15 member
is named.

— Claude for @gfraiteur

### S-08. Re-derive the November 2026 baseline: Visual Studio build tools, MSBuild and the host-capped pins

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: the November 2026 releases, that is 2026-11-10
- Findings: [LV-11](01-language-version-and-hosts.md), [UT-11](06-user-tfm-patterns-tests-docs.md),
  [UT-12](06-user-tfm-patterns-tests-docs.md), [UT-13](06-user-tfm-patterns-tests-docs.md)

---

`eng/src/Program.cs:40` pins `VisualStudioBuildToolsComponentVersion.v18_9_2`, which is a quarterly release below the
November 2026 long-term servicing floor that PB-2027.0 names, and `MSBuildVersion` at `:63` must keep matching it.
`Metalama.Framework.props:34-37` and `Directory.Packages.md` both schedule a re-reading of the Visual Studio floor,
the feature band and the host-capped package pins after 2026-11-10. Four findings from three themes wait on that one
measurement, and grouping them avoids four separate reopenings of the same two documents.

#### Context

#1902 declined the previous attempt to move the build tools, on the grounds that regenerating the Visual Studio base
images is expensive, and it recorded that the component version must exist in PostSharp.Engineering before the change
can be made at all. The measurement itself is checklist item 1 of [`platform-support.md`](../platform-support.md).
Two of the four items are comment corrections rather than version changes: the `Microsoft.NET.Test.Sdk` pin carries a
comment that still names Visual Studio 2022 as the lowest supported host, and `Microsoft.Build` is pinned at the
lowest supported host by doctrine and needs verification rather than a bump.

#### Scope

- Measure the Visual Studio version, the .NET SDK feature band, the Roslyn version and the private runtime of the
  November 2026 long-term servicing channel and of Visual Studio 2027, per checklist item 1 of
  [`platform-support.md`](../platform-support.md).
- Raise `VisualStudioBuildToolsComponentVersion` and `MSBuildVersion` in `eng/src/Program.cs` together, once
  PostSharp.Engineering exposes the newer component version, which is an external prerequisite.
- Re-derive the `Microsoft.NET.Test.Sdk` pin against the measured floor and rewrite its comment, which states the
  right rule and the wrong value.
- Verify, rather than raise, the `Microsoft.Build` pin, which doctrine keeps at the lowest supported host, and
  correct the parenthetical of `Directory.Packages.md` that states the frozen assembly version for the 17 line only.
- Re-run the vulnerability audit, remove the audit suppressions whose cause the Roslyn floor move removed, and
  correct the package version comments that name a dropped target framework.
- Restate the audit rule correctly where it is described: `NuGetAuditMode` defaults to `direct` except for .NET 10
  and later target frameworks, where it defaults to `all`.

#### Acceptance criteria

- The build container names a Visual Studio version that PB-2027.0 lists as supported, and `MSBuildVersion` matches
  the installed build tools.
- Every package pin whose comment names a measured host names the host that was actually measured.
- No audit suppression remains whose cause has been removed, and the audit reports nothing new.
- [`platform-support.md`](../platform-support.md) records the measurement and the date it was taken.

#### Not in scope

The audience paragraph of `Directory.Packages.md`, which #1903 owns and which is referenced rather than rewritten.
The .NET SDK component of the container, excluded by section 6b of [`DECISIONS.md`](DECISIONS.md).

— Claude for @gfraiteur

### S-09. Renumber the latest Roslyn variant to the stable 5.12 and regenerate the syntax model

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`, `breaking`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-01
- Findings: [LV-12](01-language-version-and-hosts.md), [LV-13](01-language-version-and-hosts.md),
  [LV-14](01-language-version-and-hosts.md), [TP-1](02-syntax-generator-and-templates.md),
  [TP-9](02-syntax-generator-and-templates.md), [DT-3](05-design-time-workspaces-linqpad.md),
  [DT-8](05-design-time-workspaces-linqpad.md)

---

`RoslynApiMaxVersion` and `RoslynMaxVersion` are `5.10.0-1.26365.3`, a build of the `main` branch of `dotnet/roslyn`
of 2026-07-15 restored from the `roslyn-consolidated` feed, and no stable 5.10 or 5.11 package exists or will exist.
Leaving the prerelease is therefore a renumbering of the latest variant to 5.12.0 and not the edit of a version
label. This story is one pull request because the pieces cannot land separately without breaking the build, and it is
the gate of the whole release.

#### Context

Section 8 of [`DECISIONS.md`](DECISIONS.md) records that 5.12 replaces the 5.10 variant rather than being added
beside it, because no supported host presents Roslyn 5.10 or 5.11 and rule 8 of the doctrine forbids a variant that
serves an empty set. A version mismatch has two silent failure modes: `TargetedAssemblyReference` compares the
declared Roslyn version by equality, and `ExtensionLoaderBase` drops a non-matching extension assembly with no
diagnostic, which removes a pipeline stage rather than reporting an error. The regeneration is the second half:
`TreeReader.RemoveExperimentalDeclarations` strips every node carrying `ExperimentalUrl`, which is why no generated
visitor, version verifier or design-time hasher knows the union declaration, the with-element or the `Name` field of
`break` and `continue`. Three published packages depend on this story as a release gate, because they currently
declare a dependency on a Roslyn package that nuget.org does not serve, which already failed for a user in #1106.

#### Scope

- Set the version strings of `Directory.Packages.props`, of the variant property file whose
  `ThisRoslynVersionNoPreview` and `DefineConstants` are written literally, and of
  `SupportedCSharpVersions.ToNuGetVersionString`, following steps 7 and 8 of
  [`updating-roslyn.md`](../updating-roslyn.md).
- Insert the retired version name into the version list of `eng/src/GenerateMetaSyntaxRewriter` in version order and
  not at the end, because the enumeration values are positional indices and a compile-time project manifest already
  on disk carries the name.
- Rename the variant projects, the variant preprocessor symbol and every literal that names the variant, including
  `RoslynVariantPolicy` and its tests.
- Add the stable grammar as a new `Syntax-5.12.0.xml` rather than overwriting the previous file, and regenerate, so
  that the rewriters, the version verifier arms and the design-time hashers gain the union declaration, the
  with-element and the labeled `break` and `continue`.
- Add a guard that compares the local grammar file with the grammar of the exact `Microsoft.CodeAnalysis.CSharp`
  package that is consumed, rather than one keyed on a prerelease label, because the unsafe expression keeps its
  experimental marker and a label-based check would fail permanently.
- Verify that removing the prerelease label removes the `roslyn-consolidated` package source, which
  `SupportedCSharpVersions.ToNuGetVersionString` derives from the hyphen, and re-derive the per-variant
  `System.Text.Json` version from the stable package.
- Rewrite step 10 of [`updating-roslyn.md`](../updating-roslyn.md), which names members that #1911 renamed, and split
  the add-a-variant list from the renumbering list.
- Add a pack-time check that refuses to publish a package pinning a prerelease Roslyn, subject to decision D-11.
- Add two design-time diff test cases for the newly generated hashers, which are a union rename and a change to the
  label of a `break` statement.

#### Acceptance criteria

- No file names the retired variant version, and both variants build and pass their tests.
- A compile-time project manifest written by the previous release still deserializes.
- An extension assembly declared for the previous variant version is either loaded or refused with a diagnostic, and
  never dropped in silence.
- `Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting` and `Metalama.LinqPad` restore from nuget.org
  alone.
- The generated syntax rewriters, the version verifier and the design-time hashers cover the union declaration, the
  with-element and the `Name` field of `break` and `continue`.

#### Not in scope

Adding C# 15 to the supported language versions, which is S-11, and the Premium mirror, which is S-10.

— Claude for @gfraiteur

### S-10. Mirror the Roslyn 5.12 renumbering in Metalama.Premium

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: M
- Blocked by: S-09
- Findings: [PR-1](07-premium.md)

---

Metalama.Premium#85 left Premium with a latest variant named 5.10.0, two prerelease fallback literals in
`Directory.Packages.props` and a `nuget.base.config` declaring the `roslyn-consolidated` feed. The version string
appears in eleven tracked files, including the `InternalsVisibleTo` entries, the packaging paths and the
`MetalamaExtensionAssembly` items with their `TargetRoslynVersion` metadata.

#### Context

If the two repositories do not move in the same release, one of three things happens: Premium resolves a preview from
a feed that the core has removed, or Premium removes the feed while the exported `RoslynApiMaxVersion` still carries a
prerelease label, or Premium ships variant assemblies whose names no longer match the variant that the core loads,
which the extension loader drops without a diagnostic. This story is separate from S-09 only because a pull request
cannot span two repositories.

#### Scope

- Rename `eng/RoslynVersions/Roslyn.5.10.0.props` to the new version, set `ThisRoslynVersionNoPreview` accordingly
  and update `eng/RoslynVersions/Latest.props`.
- Change the two fallback literals `RoslynVersion` and `RoslynMaxVersion` in `Directory.Packages.props`.
- Update every remaining occurrence of the version string, which `git grep` finds in eleven tracked files: the
  `InternalsVisibleTo` entries of the CodeFixes and Validation projects, the `TfmSpecificPackageFile` paths of the
  two package projects, and the `MetalamaExtensionAssembly` and `MetalamaDesignTimeExtensionAssembly` items with
  their `TargetRoslynVersion` metadata in the four property files.
- Remove `nuget.base.config`, or keep it with its comment rewritten, according to whether the core still needs the
  prerelease source after S-09.

#### Acceptance criteria

- No file in Metalama.Premium names the retired variant version.
- Premium restores with no prerelease Roslyn package source.
- The variant assembly names and their `TargetRoslynVersion` metadata match the variant that the core payload loads,
  verified by a design-time run rather than by inspection.

— Claude for @gfraiteur

### S-11. Enable C# 15 as a supported language version across the engine, the targets and the test framework

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-04, S-06, S-09
- Findings: [LV-3](01-language-version-and-hosts.md), [LV-6](01-language-version-and-hosts.md),
  [LV-7](01-language-version-and-hosts.md), [LV-8](01-language-version-and-hosts.md),
  [TP-2](02-syntax-generator-and-templates.md), [TP-8](02-syntax-generator-and-templates.md),
  [DT-4](05-design-time-workspaces-linqpad.md), [DT-7](05-design-time-workspaces-linqpad.md),
  [UT-19](06-user-tfm-patterns-tests-docs.md)

---

`SupportedCSharpVersions.Latest` returns `LanguageVersion.CSharp14` at
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `All` lists C# 10 to
C# 14 at `:38-43`, `ToLanguageVersion` maps both the lower and the latest Roslyn variant to C# 14 at `:59-60`, and
`GetMaxLanguageVersion` returns C# 14 for every Roslyn 5 at `:152`. This story raises them, and after it a 2027.0
preview accepts a C# 15 project instead of reporting `LAMA0051`.

#### Context

`Latest` is a shared constant today, and C# 15 is valid only in the latest Roslyn variant, so it has to become
variant-aware rather than a constant: the Roslyn 5.0 variant rejects the value 1500. There is no interim state in
which any part of this change is correct, because no Roslyn that Metalama consumes before S-09 has
`LanguageVersion.CSharp15`. Two items of the story close with no code: the operator table needs nothing for C# 15,
because C# 15 adds no user-definable operator, and the design-time pipeline verifies no language version by design,
which is correct and is recorded rather than changed. Section 4 of [`DECISIONS.md`](DECISIONS.md) keeps
`MetalamaTemplateLanguageVersion` at `14.0`, so the template language does not move with the run-time ceiling and the
distinction has to be stated where both values are written.

#### Scope

- Raise `SupportedCSharpVersions.Latest` and `All`, add C# 15 to `AllLanguageVersions` as a numeric cast so that the
  name compiles against both variants, make `Latest` variant-aware, and map only the renumbered latest variant in
  `ToLanguageVersion`.
- Add the arm to `GetMaxLanguageVersion` at the Roslyn version at which the toolset actually raises the implied
  version, and not at the version of the consumed preview.
- Extend `LanguageVersionProvider` for the .NET 11 SDK, so that the compile-time compilation is capped at the version
  the SDK actually offers.
- Extend the accepted value list of the `LangVersion` clamp in `Metalama.Framework.targets:118` and the related
  MSBuild constants, whose warning S-04 has already corrected.
- Make `CompileTimeAspectPipeline.VerifyLanguageVersion` and the template verifier report `LAMA0052` for a version
  above the ceiling rather than crash, and cover `LAMA0232` and `LAMA0282` for C# 15 syntax used in a template.
- Extend the comment at `Metalama.Framework/Directory.Build.props` so that the ceiling of this repository, the
  ceiling of the product and the template language version are three distinct values with three distinct reasons.
- Make the aspect test harness able to request C# 15, and decide D-10, that is whether an unavailable language
  version that the baseline claims to support fails the test rather than skipping it.
- Establish the test conventions of a `Tests/Aspects/CSharp15` directory whose `metalamaTests.json` names the
  constant of the renumbered variant, following the layout of the C# 14 suite.

#### Acceptance criteria

- A project that sets `LangVersion` to `15.0` is compiled by Metalama with no diagnostic, and one that sets `16.0`
  reports `LAMA0051` naming the supported versions.
- A `net11.0` project whose language version is implied is not rewritten to a lower version.
- An aspect test that requests C# 15 runs on the latest variant, and its treatment on the lower variant follows the
  answer to D-10 and is visible in the test output.
- `MetalamaTemplateLanguageVersion` is unchanged, and the reason is written next to the value.
- The operator table and the design-time pipeline are unchanged, and the analysis that says so is recorded.

#### Not in scope

Raising the template language version, excluded by section 4 of [`DECISIONS.md`](DECISIONS.md), and the language
features themselves, which are S-12 to S-22.

— Claude for @gfraiteur

### S-12. Expose the union in the public code model and add the syntax visitor overrides

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-02, S-03, S-11
- Findings: [CM-1](03-code-model-unions-closed.md), [CM-7](03-code-model-unions-closed.md)

---

A C# 15 union is indistinguishable from a struct in the public code model: Roslyn reports `TypeKind.Struct`,
`IsRecord` is false, and nothing tells an aspect that instance fields, auto-properties and field-like events are
forbidden in it. Separately, the syntax visitors of the engine inherit the Roslyn dispatch, which routes a union
declaration to `VisitUnionDeclaration`, and no visitor overrides it, so a union declaration is never seen by the
visitors that classify, hash and rewrite type declarations. This story is the surface that six later stories consume.

#### Context

The code model must report the union without a new `TypeKind` value. The precedent is decisive:
`TypeKind.RecordClass` and `TypeKind.RecordStruct` are already obsolete with the message that names
`TypeKind.Class` and `INamedType.IsRecord`, and a new value would need an arm in each of the seventeen switches over
that enumeration, twelve of which throw in the default arm. The shape of the members is decided when the story is
implemented, per section 7 of [`DECISIONS.md`](DECISIONS.md); a draft that follows the precedent of `IsRecord` is in
[`analysis-reports/12-csharp15-api-drafts.md`](analysis-reports/12-csharp15-api-drafts.md) and is illustrative only.
Two constraints are not negotiable. The reads name Roslyn members that the lower variant does not have, so they
follow the gating of S-02. And `ITypeSymbol.IsUnion` is true both for a `union` declaration and for a type carrying
`System.Runtime.CompilerServices.UnionAttribute`, while the member restrictions apply to the first form only, so the
code model must let a consumer tell the two apart, which is question Q9.

#### Scope

- Expose on `INamedType` whether a named type is a union and what its case types are, following the precedent of
  `IsRecord`, and document the union in the summary of that interface.
- Do not add a `TypeKind` value, and record in the story that the reason is the obsolete record kinds and the
  seventeen switches.
- Let a consumer distinguish a `union` declaration from a type carrying the union attribute, because the language
  restrictions apply to the declaration form only.
- Add the `VisitUnionDeclaration` overrides that a type test cannot replace, because the Roslyn visitor dispatches a
  virtual method and a numeric kind cannot override one, in the visitors inventoried by CM-7, and share the struct
  helper only where it does not read the parameter list as a primary constructor parameter list.
- Add a guard, such as a test over the visitor inventory, that a future type-declaration kind cannot be omitted from
  the same set of visitors without a failure.
- Decide, per D-3, whether the lower Roslyn variant reports a diagnostic when it meets a union it cannot represent,
  and implement the chosen behaviour.

#### Acceptance criteria

- An aspect can tell a union from an ordinary struct, can enumerate its case types, and can tell the two authoring
  forms apart.
- The same code model members exist on the lower Roslyn variant and report the value of an ordinary struct there, and
  the behaviour chosen for D-3 is covered by a test.
- Every visitor of the CM-7 inventory sees a union declaration, and the guard fails if a new one is added without it.
- Both Roslyn variants build, and no switch over `TypeKind` gained an arm.

#### Not in scope

Introducing a union, which is S-17, and the eligibility rules of advice applied to a union, which are S-14.

— Claude for @gfraiteur

### S-13. Give the compile-time path and the design-time classifier a union dispatch

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Framework-Templates`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-03, S-12
- Findings: [CM-9](03-code-model-unions-closed.md), [TP-6](02-syntax-generator-and-templates.md),
  [LK-10](04-linker-and-advice.md)

---

`FindCompileTimeCodeVisitor` and `ProduceCompileTimeCodeRewriter` of `CompileTimeCompilationBuilder` classify and
rewrite type declarations through four overrides that route to a private `VisitTypeDeclaration`, and a union
declaration reaches none of them. A run-time union in a file that also contains an aspect is therefore copied into
the compile-time compilation and breaks it with a message about a language version or a missing framework type, while
a union nested in a compile-time class is copied verbatim and one nested in a run-time class is dropped. The
`TextSpanClassifier` has the same gap at `Formatting/TextSpanClassifier.cs:113-119`.

#### Context

The case a user hits first is a run-time union declared in the same file as an aspect, so the acceptance test is one
aspect test that puts both in one file. The classifier cannot be corrected on its own: `TemplateAnnotator` has no
union dispatch either, and its default path annotates an unhandled type declaration as run-time or compile-time,
which the classifier accepts as compile-time. Routing every unhandled type declaration in the classifier to the
compile-time helper would therefore classify a run-time union, a run-time interface and an extension block as
compile-time, and would require the formatting baselines to be re-adopted. The three findings are one story because
all three edit visitors that must agree.

#### Scope

- Give `ProduceCompileTimeCodeRewriter` a dispatch for a union declaration, either by an override or by a type test
  that replaces the four kind-specific overrides and excludes extension blocks, and route it to the existing private
  `VisitTypeDeclaration`, which classifies by templating scope.
- Give `FindCompileTimeCodeVisitor` the same coverage, so that a union carrying a compile-time attribute is
  classified and reaches the manifest.
- Give `TemplateAnnotator` a dispatch for a union declaration that annotates it with the scope its declaration
  implies, before changing the classifier.
- Correct `TextSpanClassifier` so that a compile-time union declaration is classified, without classifying a run-time
  union, a run-time interface or an extension block as compile-time.
- Re-adopt the formatting baselines that the classifier change affects, reading each difference rather than adopting
  it blindly.
- Add the aspect test with a run-time union and an aspect in one file, and a test for a union nested in a
  compile-time class and in a run-time class.

#### Acceptance criteria

- A file that declares a run-time union and an aspect compiles, and the union does not appear in the compile-time
  compilation.
- A union nested in a run-time type is reported with the diagnostic that a struct in the same position reports, and
  is not copied verbatim.
- A compile-time union declaration is coloured as compile-time at design time, and a run-time one is not.
- Both Roslyn variants build.

— Claude for @gfraiteur

### S-14. Inject, link and validate advice applied to a union, and read its synthesized members

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-12
- Findings: [LK-1](04-linker-and-advice.md), [LK-2](04-linker-and-advice.md), [LK-8](04-linker-and-advice.md),
  [CM-8](03-code-model-unions-closed.md)

---

`LinkerInjectionStep.Rewriter.cs:316-324` and `LinkerLinkingStep.LinkingRewriter.cs:37-85` each dispatch on the
concrete type declaration kinds, so a member injected into a union is never inserted and, once inserted, would never
be linked. Repairing only the first produces a worse state than repairing neither. This story decides whether an
aspect applied to a union produces correct code, wrong code or a clear diagnostic.

#### Context

Section 3 of [`DECISIONS.md`](DECISIONS.md) requires full support: the linker injects and links advice applied to a
union, and advice that a union cannot carry is refused with a clear diagnostic rather than producing code that the
compiler rejects. The language forbids instance fields, auto-properties and field-like events in a union declaration,
forbids a public single-parameter constructor and requires every explicit constructor to chain to a generated one, so
several ordinary advices would otherwise emit code that the compiler rejects with the diagnostic reported on
generated code, which is the worst experience Metalama can offer. There is a fourth, silent case: an initializer
injected into a constructor that has no syntax. Question Q9 constrains every rule written here, because the
restrictions apply to the `union` declaration form and not to a type carrying the union attribute.

#### Scope

- Replace the per-kind dispatch of the injection rewriter and of the linking rewriter by a version-neutral dispatch
  over the abstract type declaration, which avoids naming a syntax type the lower variant lacks.
- Preserve the parameter list in the fallback path, because for a union that list holds the case types, and keep the
  fallback out of the record and struct paths, whose removed-primary-constructor branch would delete it and whose
  positional branch calls `GetDeclaredSymbol` on a parameter that has no declared symbol.
- Correct the insert-position walks so that a member injected into a union is placed in a valid position.
- Add the eligibility rules that refuse what a union declaration cannot carry, naming each restriction, and make each
  rule state which of the two union forms it tests.
- Read the code model member added by S-12 rather than the Roslyn flag in those rules, because the eligibility rules
  live in the public assembly, which does not reference Roslyn.
- Make the synthesized `Value` property and the per-case constructors readable by the code model and reachable by the
  linker, extending the mechanism of metalama/Metalama#1879 rather than duplicating it, and rebase onto that pull
  request, whose gates are keyed on `IsRecord`.
- Decide whether `meta.Proceed()` in an override of the synthesized `Value` is rejected as it is for synthesized
  record members.
- Do not allocate the diagnostic identifiers that #1879 takes, and do not reuse the one it removes.

#### Acceptance criteria

- An aspect that introduces a method or a nested type into a union produces code that compiles, and the introduced
  member is linked.
- An aspect that introduces an instance field, an auto-property, a field-like event, a public single-parameter
  constructor or an unchained constructor into a union declaration is refused with a diagnostic that names the
  language restriction, and emits nothing.
- The same advice applied to a type carrying the union attribute is not refused, because the restriction does not
  apply to it.
- An aspect can read the synthesized `Value` property and the per-case constructors of a union.
- No advice on a union produces a compiler error reported on generated code.

#### Not in scope

Introducing a union or a union case, which is S-17.

— Claude for @gfraiteur

### S-15. Emit a union partial part at design time instead of a struct partial part

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-12
- Findings: [CM-3](03-code-model-unions-closed.md), [DT-2](05-design-time-workspaces-linqpad.md),
  [LK-4](04-linker-and-advice.md)

---

`DesignTimeSyntaxTreeGenerator.CreatePartialType` selects a struct declaration for any `TypeKind.Struct` that is not
a record, at
`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:749`, and
Roslyn reports a union as `TypeKind.Struct`. The generated document therefore declares a partial struct against a
partial union, and the compiler reports `CS0261` on the type in the editor. This is the part of union support that a
user sees first.

#### Context

Two implementation points are settled here and nowhere else. The discriminator must be the kind of the primary
declaration syntax rather than the Roslyn union flag, because that flag is also true for a hand-written class or
struct carrying the union attribute, whose generated part must stay a class or a struct; emitting a union part for
such a type would itself produce `CS0261`. And the generated part must omit the case list, because exactly one part
of a partial union carries it and a second one is `CS8863`, as
[`analysis-reports/11-introducing-unions-design.md`](analysis-reports/11-introducing-unions-design.md) settles. The
`closed` modifier needs no counterpart in the generated part, because the compiler merges the modifiers of partial
parts, which is a verified negative statement and is recorded rather than implemented.

#### Scope

- Add the arm to `CreatePartialType` that emits a union declaration, keyed on the kind of the primary declaration
  syntax, with the partial modifier, the identifier, the type parameters, no case list, and the base list passed
  through as every other arm does, because a union may implement interfaces.
- Keep the generated part a class or a struct for a hand-written type carrying the union attribute.
- Gate the syntax factory call on the latest Roslyn variant, per S-02, because the factory does not exist in the
  lower variant.
- Add a design-time aspect test for a partial union with introduced members, with its generated partial documents
  committed.
- Record in the story that no `closed` counterpart is needed, with the reason.

#### Acceptance criteria

- A partial union with introduced members shows no error in the editor, and the generated document declares a partial
  union with no case list.
- A hand-written class or struct carrying the union attribute still receives a partial class or partial struct part.
- The design-time test and its committed output are read before they are adopted.

— Claude for @gfraiteur

### S-16. Repair the two comparer defects that a union exposes

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-12
- Findings: none. Both defects were found by
  [`analysis-reports/13-union-comparers.md`](analysis-reports/13-union-comparers.md) and are recorded in section 10 of
  [`DECISIONS.md`](DECISIONS.md), after the theme documents were written.

---

`AspectInstanceComparer.Compare` in
`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs:198-269` orders aspect
instances by the position of the primary declaration syntax of their target, and has one escape hatch at `:250-265`
for two implicitly declared methods of a record; anything else reaches the `AssertionFailedException` at `:267`. A
union misses that hatch three ways, so an aspect that targets more than one synthesized member of a union crashes.
Separately, `DeclarationEqualityComparer` reimplements the conversion rules and enumerates implicit conversion
operators only, so it does not know the conversions that Roslyn grants a union.

#### Context

The analysis of the comparers was asked for as a risk assessment of union introduction and refuted most of the
hazards, which is the useful part of its answer: the constructor signature comparer compares parameter types, no
comparer keys a constructor on its name and parameter count, the missing declaring syntax of a synthesized
constructor reaches no member comparer, and the determinism fix that #1879 had to make for records is not needed a
second time. Two defects remain. The first affects the reading half and is therefore not conditional on any open
question. The second is a prerequisite of S-17 rather than a follow-up of it.

#### Scope

- Generalise the record special case of `AspectInstanceComparer.Compare` to any implicitly declared members that
  share a span, rather than adding a union arm beside the record one, and remove the assertion that requires the
  declaring type to be a record.
- Cover the three ways in which a union misses the present hatch: the synthesized `Value` is a property and not a
  method, the synthesized case constructors are constructors and there may be several of them carrying the span of
  the union declaration, and the declaring type is not a record.
- Teach the conversion reimplementation of `DeclarationEqualityComparer` the conversions that the language grants a
  union, so that an introduced union accepts the implicit conversion from its case types.
- Add a union case to `ComparerAgreesWithRoslynTests` and to `DeclarationComparerTests`, which are the two tests that
  would have caught the second defect.

#### Acceptance criteria

- An aspect that targets several synthesized members of a union orders them deterministically and does not throw.
- The comparer agrees with Roslyn about the conversions of a union, verified by the test that compares the two.
- The record path is unchanged in behaviour, verified by the existing record tests.

— Claude for @gfraiteur

### S-17. Introduce a union type and introduce a case into an existing union

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-03, S-12, S-16, and decision D-5 for the second half
- Findings: none. The requirement was taken after the theme documents were written; the design is
  [`analysis-reports/11-introducing-unions-design.md`](analysis-reports/11-introducing-unions-design.md).

---

Section 5c of [`DECISIONS.md`](DECISIONS.md) requires that Metalama 2027.0 support the introduction of a union type
and the introduction of a union case, that is a leg of a union. This is the largest single piece of C# 15 work in the
release, and it has two halves that differ in kind: introducing a whole union is the creation of a new declaration,
while introducing a case into a union that already exists in source is a signature change of a declaration that the
user wrote.

#### Context

Introducing a whole union means that the type builder acquires a model for the case list, which the grammar makes
mandatory, and that the introduction pipeline materializes the members that the compiler synthesizes, namely one
public constructor per case and the `Value` property; the pipeline never re-reads the final model from Roslyn, so a
synthesized member that an aspect must see has to exist as a builder. The precedent is not the record materialization
of #1879, which does not generalise because a user may not declare those members at all and there is therefore no
override to serve, but the introduction of a namespace, which registers a builder without an injection. Introducing a
case has the introduction of a parameter into a partial constructor as its precedent, delivered for C# 14 in #1143,
and it depends on a grammar rule that
[`analysis-reports/11-introducing-unions-design.md`](analysis-reports/11-introducing-unions-design.md) settles:
exactly one part of a partial union carries the case list, a second one is `CS8863`, and none is `CS9370`. The
consequence is that a generated partial part can never add a case to a union declared with the `union` keyword, so
that operation works at build time only, while the same operation on a type carrying the union attribute is ordinary
member introduction whose design-time result is correct. Decision D-5 chooses between shipping both forms and
shipping the attribute form alone. About half of the work needs no C# 15 Roslyn member and can proceed before S-09.

#### Scope

- Add a model for the case list to the named type builder, its data and the introduced type, with validation.
- Add a transformation shape that registers a builder into the code model without injecting syntax, modelled on the
  introduction of a namespace, and materialize the synthesized `Value` property and the case constructors through it.
- Prototype that step first, because whether a member builder with no injected member survives the linker injection
  registry was not verified and is the risk that decides whether the step is one day or three.
- Add the advice surface for introducing a union and for introducing a case, reporting that the operation is not
  supported by the current compiler in every path that would need C# 15 syntax, so that the surface, the
  documentation and the eligibility tests can be reviewed before the compiler exists.
- Deliver the case introduction for a type carrying the union attribute, which is member introduction and whose
  design-time result is correct.
- Deliver the case introduction for a `union` declaration only if D-5 chooses Option A, with the linker rewriting the
  case list of the part the user wrote and a design-time diagnostic stating that the editor cannot show the added
  case.
- Emit the union declaration in the type introduction transformation and add the union arms to the injection rewriter
  and to the design-time generator, inside the variant block.
- Extend the eligibility rules so that each states which of the two union forms it tests, per question Q9.
- Write the aspect tests with their committed baselines, including the design-time scenarios, and the documentation
  page under `../Metalama.Documentation/content` including the note that an added case is a build-time-only change.

#### Acceptance criteria

- An aspect can introduce a union type, and an aspect can read the members that the compiler synthesizes for it.
- An aspect can add a case to a type carrying the union attribute, and the editor and the build agree about the
  result.
- If D-5 chooses Option A, an aspect can add a case to a `union` declaration, the build result is correct, and a
  design-time diagnostic states that the editor cannot show it.
- Every eligibility rule of the story names the union form it tests, and none rejects advice that is legal on a type
  carrying the union attribute.
- The aspect tests and their expected output are committed and were read before adoption.

#### Not in scope

Introducing a closed class, excluded by section 5b of [`DECISIONS.md`](DECISIONS.md), and the introduction of
structs, records, enums and delegates, which are #869, #867, #866 and #865 and stay open.

— Claude for @gfraiteur

### S-18. Read closed hierarchies in the code model

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: S-02, S-11
- Findings: [CM-5](03-code-model-unions-closed.md)

---

Nothing is broken today by a closed class: it is a class with a modifier that the compiler merges across partial
parts, Roslyn adds the abstract flag to it so the code model reports `IsAbstract` correctly, and the derived-type
model is already right. What is missing is that an aspect cannot tell that a class is closed, and that the
documentation does not say that the enumeration of direct derived types is exhaustive for such a type.

#### Context

The language requires every subtype of a closed type to be in the same module, and the derived type index already
restricts itself to the current compilation, so the existing enumeration of direct derived types is already the
complete set for a closed type declared in the current compilation. Only the flag and its documentation are new. The
one case the index does not answer is a closed type that comes from a referenced assembly, which is question Q8 and
is left open. The read names a Roslyn member that the lower variant does not expose, so it follows the gating of
S-02, and the emission of a `closed` modifier is out of scope because section 5b of [`DECISIONS.md`](DECISIONS.md)
puts the closed writer out of the release.

#### Scope

- Expose on `INamedType` whether a named type is closed, following the precedent of the other type flags, and gate
  the read to the latest Roslyn variant.
- Document in the derived-type options that, for a closed type declared in the current compilation, the direct
  enumeration is exhaustive.
- Record what the value is for a builder and for an introduced type, which is false while the writer is out of scope.
- Record question Q8, the closed type read from a referenced assembly, as a known limitation in the documentation
  rather than implementing it.

#### Acceptance criteria

- An aspect can tell a closed class from an ordinary abstract class, and the value is false on the lower Roslyn
  variant.
- The documentation of the derived-type options states the exhaustiveness rule and its condition.
- Both Roslyn variants build.

#### Not in scope

Introducing a closed class, excluded by section 5b of [`DECISIONS.md`](DECISIONS.md) and by question Q4.

— Claude for @gfraiteur

### S-19. Reject a labeled `break` or `continue` in a template, and keep run-time labels correct when inlining

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework-Templates`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-09, S-11
- Findings: [TP-3](02-syntax-generator-and-templates.md), [LK-9](04-linker-and-advice.md),
  [UT-17](06-user-tfm-patterns-tests-docs.md)

---

`TemplateAnnotator` has no visit for a labeled statement, so the scope of a labeled loop is not propagated and the
label of a `break` or `continue` is dropped, which silently retargets the statement to the innermost loop.
Independently, the inlining substitution copies user labels verbatim into a destination that may already declare
them, and labeled loops make that collision realistic for the first time. Both failure modes produce wrong code with
no diagnostic.

#### Context

Section 4 of [`DECISIONS.md`](DECISIONS.md) settles the first half by forbidding rather than supporting: a labeled
`break` or `continue` inside a template is reported with a diagnostic, because the label belongs to a loop whose
scope may differ from the scope of the statement that names it and the annotator cannot classify it. The same section
requires that run-time code which an aspect transforms and which uses a label outside a template keeps working once
the syntax model is regenerated by S-09. The second half is therefore not affected by the decision: the inlining
substitution moves user code, and the collision it creates is between a label of the moved body and a label of the
destination.

#### Scope

- Report a new error from the visit of a `break` and of a `continue` statement in `TemplateAnnotator` when the
  statement carries a label, on the label token, beside the other rejections of syntax that the annotator cannot
  classify.
- Confirm that the rejection fires only inside a template, because the annotator runs under the guard that tests
  whether the code is inside a template, and add a test that a run-time labeled loop outside a template is untouched.
- Rename the labels of an inlined body when the destination already declares them, allocating unique names through
  the existing lexical scope factory, and rewrite the labeled statement, the `goto` target and the label of `break`
  and `continue` consistently.
- Gate the strongly typed part of that rewrite on the latest Roslyn variant, because the label of `break` and
  `continue` is a member that the lower variant does not have.
- Add the two metric unit tests and the observability aspect test with a labeled loop in a getter, which record that
  the metric providers count nodes and statements generically and need no new case.
- Extend the inlining documentation with the reason a label is renamed.

#### Acceptance criteria

- A labeled `break` or `continue` in a template is reported with a diagnostic on the label, and no template silently
  loses a label.
- An aspect that overrides a method whose body contains a labeled loop produces code that compiles, including when
  the destination declares the same label.
- A labeled `break` in transformed run-time code targets the same loop it targeted in the source.
- The metric providers are unchanged, and the tests that pin them pass.

#### Not in scope

Supporting labels in templates, excluded by section 4 of [`DECISIONS.md`](DECISIONS.md).

— Claude for @gfraiteur

### S-20. Guard experimental C# syntax in templates and add the with-element tests

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework-Templates`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-09, S-11
- Findings: [TP-4](02-syntax-generator-and-templates.md), [TP-5](02-syntax-generator-and-templates.md)

---

A collection expression with a with-element crashes the template compiler in run-time scope today, with an
`InvalidCastException` surfaced as an unexpected-exception error and a crash report rather than as a template
diagnostic. The regeneration of S-09 produces the visitor for that node and closes the crash, so what remains is
tests. What the regeneration does not close is the unsafe expression, which keeps its experimental marker on the
target Roslyn and is therefore still stripped from the generated code, so it passes the annotator and surfaces as a
C# error on the generated compile-time file.

#### Context

The unsafe expression is not part of C# 15: it is gated on the preview language version and is out of scope for
2027.0. The correct remedy is therefore not to support it but to reject it in the template compiler, and to do so
without naming it, because the same protection must apply to every future experimental node. The generator already
knows which declarations it removed as experimental, so the guard can be driven by that knowledge rather than by a
per-node override that would have to be written again each time Roslyn adds an experimental node.

#### Scope

- Make the syntax generator record the node kinds it removed as experimental, and add a name-free guard in the
  template compiler that reports a template diagnostic for any node of such a kind.
- Add the aspect tests for a with-element in a collection expression, in run-time and in compile-time scope, beside
  the existing collection-expression test of the C# 12 suite, and not in a syntax directory that does not exist.
- Confirm after regeneration that a with-element no longer crashes the template compiler, and record the result in
  the test rather than in a comment.

#### Acceptance criteria

- A with-element in a template compiles in both scopes, with committed expected output.
- An experimental syntax node in a template is reported with a template diagnostic naming the template, and never
  reaches the compile-time compilation.
- Adding a new experimental node to the grammar requires no new override for the guard to cover it.

#### Not in scope

Supporting the unsafe expression, which is a preview language feature and is out of scope for 2027.0.

— Claude for @gfraiteur

### S-21. Support extension indexers in advice, in overriding and in the contracts

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-11
- Findings: [LK-6](04-linker-and-advice.md), [LK-7](04-linker-and-advice.md),
  [UT-16](06-user-tfm-patterns-tests-docs.md)

---

`Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:1406` rejects the introduction of an
indexer into an extension block by design, a restriction that #1587 recorded in the documentation rather than
lifted. C# 15 adds extension indexers to the language, so that deliberate restriction becomes a gap. This story is
self-contained and has no dependency on the union work, so it can run in parallel with S-12 to S-17.

#### Context

Section 9 of [`DECISIONS.md`](DECISIONS.md) records that extension indexers need no application programming
interface change in order to be overridden, and that introducing one needs the removal of a single validation call
plus an eligibility rule requiring the named receiver that an extension block with an indexer must declare. The
language adds three further restrictions that the eligibility rules must carry: no `init` accessor, and none of the
modifiers that an extension member may not have. The overriding half follows the extension property path and is
expected to be correct when the override is inlineable; the non-inlined case is bounded by the pre-existing
`LAMA0699` of the open issue #937, which this story states rather than fixes. Every test needs C# 15 as a requestable
language version, which is why the story waits for S-11.

#### Scope

- Remove the validation that rejects an indexer in an extension block, and replace it with the eligibility rules that
  the language requires, which are a named receiver parameter, no `init` accessor and none of the forbidden
  modifiers.
- Create the accessor methods of the introduced indexer in the introduction transformation, as the other extension
  member kinds do.
- Restore the word that #1587 removed from the documentation of the two extension block introduction overloads, and
  replace the aspect test whose expected output is the current rejection.
- Add the overriding tests for a source extension indexer, and state the `LAMA0699` boundary of #937 in the story
  rather than fixing it.
- Add the contract advice tests, including the receiver-parameter contract that #1127 established, and first
  determine whether the not-null fabric enumeration reaches an extension block at all, because the indexer is a
  member of the block and not of the enclosing static class.

#### Acceptance criteria

- An aspect can introduce an indexer into an extension block, and the generated code compiles.
- An extension block that declares an indexer without a named receiver, or an indexer with an `init` accessor, is
  refused with a diagnostic that names the language restriction.
- Overriding a source extension indexer produces correct code when the override is inlineable, and reports
  `LAMA0699` otherwise.
- A contract applied to an extension indexer parameter and to the receiver parameter behaves as it does for an
  extension property.

#### Not in scope

Removing the `LAMA0699` limitation on non-inlined indexer overrides, which is #937.

— Claude for @gfraiteur

### S-22. Make the pattern libraries, the extension libraries and the reference graph correct on unions

- Issue type: User Story
- Labels: `enhancement`, `Area-Patterns`, `Area-Extensions`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`, `metalama/Metalama.Premium`
- Size: L
- Blocked by: S-12
- Findings: [UT-14](06-user-tfm-patterns-tests-docs.md), [UT-14a](06-user-tfm-patterns-tests-docs.md),
  [UT-14b](06-user-tfm-patterns-tests-docs.md), [UT-14c](06-user-tfm-patterns-tests-docs.md),
  [UT-14d](06-user-tfm-patterns-tests-docs.md), [PR-12](07-premium.md)

---

A union is an ordinary struct with an opaque value property for every library that reads the code model, and that
premise has four consequences in the pattern and extension libraries plus one in the reference graph. Two of them are
product defects: the immutability classification calls a union mutable, which produces spurious observability
warnings, and the caching key generation and the cache item serialization treat a union as an opaque struct and
produce a silently wrong result.

#### Context

The immutability rule must key on the declaration form and not on the Roslyn union flag, because that flag is also
true for a hand-written type carrying the union attribute whose state is unconstrained. The caching defect has a
practical discriminator, which is the interface that the compiler makes every union implement, but the affected
projects target `net472` and `netstandard2.0` and cannot bind to it at compile time, so the check has to be made in
another way. The remaining three consequences are tests: the observability aspect already rejects a union with two
diagnostics and that behaviour is pinned rather than changed, and the multicast selector is correct for every target
except the implicit parameterless constructor, where materialising an override produces a compiler error. The
reference graph item needs one override in the core reference index walker, without which a reference from a union to
its case types is never attributed and an architecture rule under-reports with no diagnostic; the Premium half is two
architecture rule tests, which is why both repositories are listed and why the core half lands first.

#### Scope

- Classify a union declaration in the immutability library as shallowly immutable, and as deeply immutable when every
  case type is, treating an interface, a type parameter, a nullable value type and a nested union case type
  conservatively, and document the rule against the definition the library gives rather than against the `readonly`
  modifier.
- Make the caching key generation and the cache item serialization treat a union by its case value rather than as an
  opaque struct, and choose a discriminator that the target frameworks of the affected projects can express.
- Add the observability test that pins the two diagnostics with which the library already rejects a union.
- Prevent the multicast selector from selecting the implicit parameterless constructor of a union, or make
  constructor advice ineligible on a union in the engine, and record which of the two was chosen.
- Attribute a reference from a union declaration to its case types in the core reference index walker, by entering
  the union type declaration as the current declaration before visiting its case list.
- Add the two architecture rule tests in Metalama.Premium, one for a rule on a type used as a union case and one for
  a closed class.

#### Acceptance criteria

- A union of immutable case types does not produce an observability warning, and a hand-written type carrying the
  union attribute is not classified from the attribute alone.
- A cached method whose parameter is a union produces a different cache key for two different cases, and a cached
  value of union type survives a serialization round trip.
- An architecture rule on a type used as a union case reports the reference from the union.
- The multicast and observability tests are committed with their expected output.

— Claude for @gfraiteur

### S-23. Execute the Roslyn 5.0.0 variant of the Metalama.Premium engines in tests

- Issue type: User Story
- Labels: `enhancement`, `Area-Extensions`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: L
- Blocked by: nothing
- Findings: [PR-2](07-premium.md), [PR-13](07-premium.md)

---

The three Roslyn 5.0.0 variant projects of the Premium validation and code fix engines are compiled by the solution
build and are executed by no test. That variant is the one that serves Rider and the Visual Studio Code C# Dev Kit,
which nobody exercises by hand now that Visual Studio 2022 is dropped, so the risk of this gap rose this release
rather than staying constant.

#### Context

The gap is behavioural rather than an interface gap: the variant projects are referenced by the package resource
projects, so a use of an application programming interface that Roslyn 5.0 does not have fails the build. What is not
covered is behaviour that differs when the engines bind against the older Roslyn, and a defect of that kind is not
detected before a Rider user reports it. The order inside the story matters: the existing aspect test projects
reference the unsuffixed engine by a hardcoded path, so a variant shim added before they are made variant-aware would
compile the test sources under one property set and load the other engine.

#### Scope

- Make `Metalama.Extensions.Validation.AspectTests` and `Metalama.Extensions.CodeFixes.AspectTests` variant-aware, in
  the manner of the core aspect test project, by resolving the engine project reference through the variant suffix.
- Add the aspect test shims for the Roslyn 5.0.0 variant, and confirm that the extension assembly item names the file
  that is actually in the output directory.
- Decide separately whether the unit test shims are added, since they additionally need a Roslyn 5.0.0 build of the
  core unit test helper package, which is a change in the other repository; they belong in a second pull request or
  are dropped in favour of the aspect test shims alone.
- Correct the comment in those project files that still describes a variant constant which no longer exists, in the
  change that removes its cause.

#### Acceptance criteria

- The validation and code fix engines are executed by tests in both Roslyn variants, and both are green.
- No test project resolves the engine by a hardcoded path.
- No project file describes a preprocessor constant that no configuration defines.

— Claude for @gfraiteur

### S-24. Clean up the Metalama.Premium build-file residuals

- Issue type: Bug
- Labels: `bug`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: S
- Blocked by: nothing
- Findings: [PR-3](07-premium.md), [PR-5](07-premium.md), [PR-6](07-premium.md), [PR-7](07-premium.md)

---

Metalama.Premium#85 and #86 aligned Premium with PB-2027.0 and moved its build image to the Visual Studio 2026 build
tools. Four residuals remain, all of them small, independent of every gate and confined to build files: obsolete
container components, a template language version pinned below what the repository now supports, package pins and
comments that state a rule with the wrong value, and a pair of central package entries that makes an intended client
update inert.

#### Context

The .NET 8 SDK and the .NET 6 runtime components of the container were justified by target frameworks that Premium no
longer has, and a stale Visual Studio 2022 channel manifest is still at the top of the docker context.
`MetalamaTemplateLanguageVersion` is `13.0` under a comment that names a Visual Studio version, whereas the value is
bounded by the lowest Roslyn variant of the repository, which is now 5.0.0 and supports C# 14; raising it is expected
to change which system-type polyfills the compile-time compilation embeds. The `Microsoft.Build` pins do not follow
the core doctrine, and the licensing build task must move off its older target framework in the same change, because
the newer package has no compile asset for it. The two contradictory `StackExchange.Redis` entries mean the intended
version never takes effect.

#### Scope

- Remove the .NET 8 SDK and the .NET 6 runtime components from `eng/src/Program.cs`, remove the stale Visual Studio
  channel manifest from the docker context, and decide whether the prerelease flag of the SDK version follows the
  core repository.
- Raise `MetalamaTemplateLanguageVersion` from `13.0` to `14.0` and rewrite its comment to name the lowest Roslyn
  variant of the repository rather than a Visual Studio version.
- Align the `Microsoft.Build` pins with the core doctrine, together with the move of the licensing build task off its
  older target framework, delete the dead property and the entry that no project references, and correct the
  rationale comments.
- Resolve the contradictory pair of `StackExchange.Redis` entries into one, and confirm the resolved version in the
  restored assets file.

#### Acceptance criteria

- The container installs no component whose reason has been removed, and the docker context carries no manifest for
  an unsupported Visual Studio.
- The template language version of Premium equals the value the core repository uses, and its comment names the
  Roslyn floor.
- Every package pin of Premium that mirrors a core pin has the same value, and the comment states the rule the core
  doctrine states.
- The Redis client resolves to the intended version.

— Claude for @gfraiteur

### S-25. Bring the platform, dependency and extensibility documentation up to the shipped 2027.0 state

- Issue type: User Story
- Labels: `enhancement`, `documentation`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`, `metalama/Metalama.Documentation`
- Size: M
- Blocked by: S-09, S-11
- Findings: [UT-18](06-user-tfm-patterns-tests-docs.md), [LV-10](01-language-version-and-hosts.md),
  [DT-9](05-design-time-workspaces-linqpad.md), [PR-9](07-premium.md), [PR-14](07-premium.md)

---

Several documents and project comments still state the previous baseline: they name `net8.0` or C# 14 as the latest,
they describe a check that has since been disabled, and they do not name the repositories outside `metalama/Metalama`
that repeat the Core flavour literal. Five findings propose prose only, and four of them edit the same two documents,
which is why they are one pull request.

#### Context

The story is deliberately last, because a document written before the code is a second thing to correct. Two
corrections come from decisions rather than from the theme documents: the statement that the November 2026 long-term
servicing baseline carries a Roslyn version near 5.11 should name 5.12, and the variant table row that offers a
measured version of 5.10 or above should read 5.12 or above, both recorded as question Q10; and the statement that
the `net10.0` toolset rolls forward to .NET 11 overstates the mechanism, because the roll-forward selects .NET 11
only when no .NET 10 runtime is installed, which is question Q11. Both belong to the product owner, so this story
applies them only once they are approved.

#### Scope

- Correct the locations that still name `net8.0` or C# 14 as the latest, and the standalone project comments that
  describe a check that has since been disabled, including the comment of the default language version scenario.
- State that the remaining `net10.0` literals name our own outputs and move only with the embedded Core flavour, so
  that they are not confused with the .NET SDK pin.
- State in the `Metalama.Framework.Workspaces` and `Metalama.LinqPad` package documentation that the host runtime
  major decides which .NET SDK the in-process MSBuild registration may use, so that a user on a machine carrying only
  the newer SDK understands the failure.
- Add a section about Metalama.Premium beside the existing one about Metalama.Compiler in
  [`platform-support.md`](../platform-support.md), listing the Premium build files that repeat the Core flavour
  literal, which is repeated in eight places with no test on the comparison, and the task directory selection of the
  licensing targets.
- State in the extensibility guide that the target framework metadata of an extension assembly is compared for string
  equality against the Core flavour name of the current platform baseline, so that a merely compatible value does not
  match.
- Apply the two corrections of question Q10 and the one of question Q11 once the product owner approves them.

#### Acceptance criteria

- No document or project comment names a target framework or a C# version that PB-2027.0 has dropped as the latest.
- [`platform-support.md`](../platform-support.md) lists the drift points of Metalama.Premium as it lists those of
  Metalama.Compiler.
- The two introspection packages state the host runtime and the .NET SDK they require.
- The roll-forward statement names the condition under which the roll-forward happens.

#### Not in scope

The audience paragraph of `Directory.Packages.md`, which #1903 owns. The well-known source generator attribute list
of `Metalama.Framework.props`, which is functional and not documentation: an attribute that a newer framework adds
and that the list omits changes aspect eligibility for a generated partial member with no diagnostic, so it needs its
own behavioural change and test and must not be lost in a documentation review.

— Claude for @gfraiteur

## Already in progress

Four pull requests interact with these stories. Two are open and two were merged after the theme documents were
written, and the second pair must be confirmed rather than assumed, because the sources disagree about their state.

### metalama/Metalama#1879, materializing compiler-synthesized record members

Open. It makes `meta.Proceed()` work in an aspect that overrides a compiler-synthesized record member, and it builds
the mechanism that S-14 and S-17 consume: a generator that reproduces the body of a synthesized member from its
symbol, a linker substitution whose replaced node is the type declaration, a public helper that answers whether a
member can be declared explicitly, and an eligibility rule built on it. Every gate of that mechanism is keyed on
whether the type is a record. What remains for this release: S-14 must be rebased onto it and must extend it rather
than duplicate it, and S-17 must not assume that it generalises to unions, because a user may not declare the
synthesized union members at all and there is therefore no override to serve. The diagnostic identifiers that it
takes must not be allocated again, and the one it removes must not be reused.

### metalama/Metalama.Premium#84, the out-of-band package caps

Open, with a failing build that the pull request body reports as predating the change. It mirrors the re-derived
package caps of #1897 into Premium. What remains: S-24 edits the same two files and must be based on this branch or
rebased onto it after it merges, and must not reintroduce the variant-conditional properties that it removes.

### metalama/Metalama.Premium#85, the Premium alignment with PB-2027.0

Recorded as merged on 2026-09-03. It dropped the Roslyn 4.12 variant, made 5.0.0 the lower variant with a project
suffix, added the latest variant with its package source, and moved every target framework to `net10.0`. Finding
PR-15 is fully covered by it. What remains: S-10, the renumbering of the variant it added, and S-23, the tests that
would execute the lower variant it created.

### metalama/Metalama.Premium#86, the engineering update and the build image

Recorded as merged on 2026-09-03. It retargeted the engineering project and moved the image to the Visual Studio 2026
build tools. Finding PR-4 is fully covered by it. What remains: the four residuals of S-24, which it did not scope.

## Findings not turned into a story

Thirteen verified findings produce no story. The reason is given for each.

- [LV-9](01-language-version-and-hosts.md), nothing builds or tests under the .NET 11 SDK. Out of scope for 2027.0:
  section 6b of [`DECISIONS.md`](DECISIONS.md) removes the build container work from the release. The .NET SDK
  ceiling defect that this finding also names is carried by UT-2 in story S-04.
- [UT-1](06-user-tfm-patterns-tests-docs.md), the build container carries no .NET 11 SDK. Out of scope: section 6b
  states that installing it and settling what `global.json` pins is not important for 2027.0, and that the two
  defects it would have exposed are verified by a test of a comparison instead.
- [UT-5](06-user-tfm-patterns-tests-docs.md), no test project has a `net11.0` leg. Out of scope: section 6c records
  that no .NET 11 application programming interface justifies a `net11.0` asset or a `net11.0` test leg.
- [UT-6](06-user-tfm-patterns-tests-docs.md), the tested-target-framework scenario omits `net11.0`. Out of scope for
  the same reason, and it additionally needs targeting packs that the build agents do not have.
- [UT-7](06-user-tfm-patterns-tests-docs.md), the standalone scenarios run on `net10.0` only. Out of scope: the
  language version clamp that these scenarios would have proved is verified without an installed SDK, in S-04.
- [UT-8](06-user-tfm-patterns-tests-docs.md), the design-time host simulator follows a constant target framework.
  Out of scope for the same reason. The separate observation about the .NET SDK selection rule of `Metalama.Tool` and
  `Metalama.LinqPad` is documented by S-25 rather than changed.
- [PR-8](07-premium.md), no Premium test leg exercises the .NET 11 SDK. Out of scope: it follows the core decision,
  and the core decision is not to add one.
- [CM-4](03-code-model-unions-closed.md), introduced types cannot be closed. Out of scope: section 5b of
  [`DECISIONS.md`](DECISIONS.md) and question Q4 put the closed writer outside 2027.0. The work is small and every
  part of it is identified, so a known customer scenario reopens it.
- [LK-5](04-linker-and-advice.md), type introduction cannot emit `closed` or `union`. Out of scope for the closed
  half, for the same reason; the union half is superseded by section 5c and is delivered by S-17.
- [TP-10](02-syntax-generator-and-templates.md), the closed modifier and patterns over unions need nothing from the
  template compiler. No impact. Its one conditional action, the polyfill attributes that a closed compile-time class
  would need, is closed by section 4 of [`DECISIONS.md`](DECISIONS.md), which keeps the template language at C# 14.
- [UT-15](06-user-tfm-patterns-tests-docs.md), closed classes reach no pattern-specific code. No impact, and it
  proposes observation tests for a behaviour that cannot change while the closed writer is out of scope.
- [PR-4](07-premium.md), the engineering tool targeted `net9.0`. Already delivered by Metalama.Premium#86; the story
  must not be proposed a second time, and the merge is to be confirmed before S-24 starts.
- [PR-15](07-premium.md), the Premium variant set alignment. Already delivered by Metalama.Premium#85, with the same
  confirmation.

— Claude for @gfraiteur
