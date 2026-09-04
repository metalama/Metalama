# 00. Work already in progress

This document records the work that is already done or in flight for the 2027.0 release, so that the user stories of
this analysis do not duplicate it. It covers three open pull requests, the pull requests merged into
`develop/2027.0` of `metalama/Metalama` and of `metalama/Metalama.Premium` since 2026-08-15, and the five open issues
that overlap with the analysis. It closes with the one place where a merged or in-flight change is directly reusable
by a new story: the synthesized record member mechanism of #1879 and its relation to union types.

The document does not decide the platform baseline. [`platform-support.md`](../platform-support.md) remains the
authority on PB-2027.0, [`Directory.Packages.md`](../../../Directory.Packages.md) on which package versions the
baseline permits, and [`updating-roslyn.md`](../updating-roslyn.md) on the procedure that a Roslyn transition
follows.

The survey was made on 2026-09-03 and 2026-09-04 on `/home/user/Metalama`, branch
`topic/2027.0/26-09-03-update-eng-7e3j07`, and on `/home/user/metalama.premium`, branch `develop/2027.0`. Two
reading conventions apply throughout. First, a file and line citation without further qualification refers to the
working tree of those two branches. Second, the files that #1879 adds or changes do not exist in the working tree,
because the pull request is not merged, so every citation to them names the pull request branch explicitly.
`Metalama.Compiler` is not cloned, so every statement about the compiler that ships with the stable Roslyn is marked
as an assumption.

## Summary

1. Three pull requests are open. #1879 makes `meta.Proceed()` work on compiler-synthesized record members,
   metalama/Metalama.Premium#85 aligns Metalama.Premium with PB-2027.0, and metalama/Metalama.Premium#84 mirrors the
   re-derived package caps into Metalama.Premium. None of the three touches the C# language version, the `net11.0`
   target framework or the C# 15 syntax.
2. The infrastructure half of the release is largely settled on `develop/2027.0`. The Roslyn 5.10 prerelease is
   consumed, the Roslyn 4.12 variant is removed, .NET 8 and .NET 9 are removed, the template language version is
   C# 14, the host Roslyn floor is enforced, and the platform baseline is documented.
3. What is not settled anywhere is the language axis. No merged or open pull request accepts C# 15, targets
   `net11.0`, or handles union declarations, closed hierarchies, labeled `break` and `continue`, collection
   expression arguments or extension indexers.
4. The one reusable mechanism is the substitution pattern of #1879. It is a design pattern for a member that has no
   body syntax of its own, not an implementation that a union story can call, because the members that the C# 15
   proposal synthesizes for a union are a different family from the record members that #1879 materializes.

## 1. Open pull requests

| Pull request | State | What it covers for this analysis | What it leaves open |
| --- | --- | --- | --- |
| #1879, `meta.Proceed()` on compiler-synthesized record members | Open, not a draft. Head `37a92204` over base `36e12789`, nine commits, 110 files. Four reviews requesting changes by the product owner; the latest, on the head commit at 2026-09-02, has no answering commit. No continuous integration build or test result is visible on GitHub for the head commit. | Materialization of a synthesized record member body from its symbol, a linker substitution whose replaced node is the type declaration, the non-inlined `_Source` path, the public helper `CanBeDeclaredExplicitly` and the eligibility rule built on it, the diagnostics `LAMA0552` and `LAMA0652`, and deterministic ordering of synthesized override targets. | The record copy constructor, the reviewer's request that a materialized body read the `_Source` layer of an overridden property, the property reads that only warn through `LAMA0652`, the reporting of `LAMA0552` outside the eligibility mechanism, and the absence of a continuous integration result. |
| metalama/Metalama.Premium#85, Metalama.Premium on Roslyn 5.10 and net10.0 | Open, not a draft, mergeable state blocked, because the review requesting changes on commit `ab8c49f` has not been superseded by an approval. The combined status of head `3737150` is success, from TeamCity build 334244 of 2026-09-03. | The whole Metalama.Premium alignment with PB-2027.0: the Roslyn version pins, the removal of the 4.12.0 variant, the 5.0.0 lower variant and the 5.10.0 latest variant, the package payload and the variant selection metadata, the `roslyn-consolidated` feed, `net10.0` in place of `net8.0`, and `net472` in place of `net471`. | The build tool project, which stays on `net9.0` with engineering 2023.2.412, the `NETSDK1233` and `LAMA0602` warnings left visible in the three standalone solutions, and the decision on how the shared engine sources would compile for the 5.0.0 variant if they named a Roslyn 5.10 syntax type. |
| metalama/Metalama.Premium#84, mirror of the re-derived package caps | Open, not a draft, approved by the product owner on commit `c150ecc` at 2026-09-02. The combined status of that commit is a failure, from TeamCity build 334128; the body attributes the failure to a restore error that is identical on `develop/2027.0`. | Single central pins of `System.Memory` and `System.Runtime.CompilerServices.Unsafe` in Metalama.Premium, equal to the core pins, the removal of the variant-conditional properties that carried them, and the rationale comment that cites the measured binding-redirect ceilings. | A green Metalama.Premium build, which the body defers to the alignment work, and every other part of the alignment: the 4.12.0 variant, the runtime dependency versions, the `net8.0` targets and the Roslyn version pins. |

The three subsections below expand the two right-hand columns of the table.

### 1.1. #1879, `meta.Proceed()` on compiler-synthesized record members

The pull request is configured to close issue #1343. It changes no Markdown file, no `.props` file, no `.targets`
file and nothing under `eng/`.

What it covers:

- Materialization of the body of a compiler-synthesized record member from its symbol alone, for the
  `EqualityContract` getter, `Equals(R?)`, `GetHashCode`, `ToString`, `PrintMembers` and `Deconstruct`. The
  implementation is `SynthesizedRecordMemberBodyGenerator` in
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/SynthesizedRecordMemberBodyGenerator.cs` on the branch of
  #1879, with the kind enumeration `SynthesizedRecordMemberKind` and a body contract of statements and a result that
  both the inlined and the non-inlined path consume.
- A linker substitution for a member that has no body syntax of its own, whose replaced node is the type
  declaration. It is `SynthesizedRecordMemberSubstitution` in
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Substitution/SynthesizedRecordMemberSubstitution.cs`, and
  it is selected for `RecordDeclaration` and `RecordStructDeclaration` roots at line 911 of
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs`, both on the
  branch of #1879.
- The non-inlined path, that is a `_Source` member carrying the materialized body, through
  `LinkerRecordHelper.GetOriginalImplMethod` and `GetOriginalImplProperty` in
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs` on the branch of #1879, with
  `GetNotSupportedBody` retained as the fallback for a member whose kind is `None`.
- A public code model member that answers whether a member can be written in source,
  `MemberExtensions.CanBeDeclaredExplicitly` in
  `Metalama.Framework/src/Metalama.Framework/Code/MemberExtensions.cs`, wrapped as the eligibility rule
  `EligibilityExtensions.MustBeDeclarableExplicitly` in
  `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityExtensions.cs` and used by
  `OverrideMethodAdviceRule` and `OverrideFieldOrPropertyOrIndexerAdviceRule` in
  `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs`, all on the branch of #1879.
- The diagnostics. `LAMA0552` reports an introduction with `OverrideStrategy.Override` that cannot override
  `Equals(object)`, `Equals(Base)`, `==` or `!=` of a record. `LAMA0651` is removed and the warning `LAMA0652`
  reports a reachable materialized `Equals` or `GetHashCode` that reads a virtual or non-sealed override
  auto-property through the property. A new story must therefore not allocate or reuse the identifiers `LAMA0552`,
  `LAMA0651` and `LAMA0652`.
- A non-inlineability hook for a generated body that declares a local variable,
  `SynthesizedRecordMemberBodyGenerator.BodyDeclaresLocalVariable`, read by
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs` on the
  branch of #1879. It is true only for `ToString`.
- `LinkerRewritingDriver.HasMaterializedBackingField` for a property and for an event, which decides whether
  generated code can read a linker-emitted backing field by name; the participation of field-like events in the
  generated `Equals` and `GetHashCode`; deterministic emission order of synthesized override targets, obtained by
  sorting with `StructuralSymbolComparer.Default` in `GetSynthesizedMethodOverrideTargets` and
  `GetSynthesizedPropertyOverrideTargets`; and the copying of the `readonly` modifier in `GetSpecialImplMethod`, so
  that the `_Source` member of a record struct does not produce `CS8656`.

What it leaves open:

- The record copy constructor is not materialized. `OverrideConstructorAdviceRule`, the introduce-parameter rule and
  the add-initializer rule at lines 54, 148 and 179 of
  `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs` keep
  `MustNotBeRecordCopyConstructor`, so `meta.Proceed()` never reaches the linker for a copy constructor. The pull
  request body defers this to a separate issue. A story on the subject does not duplicate #1879, but it must
  reference those three rules and must add executing tests, because the two copy constructor files on the branch of
  #1879 are expected-output files with no source file beside them and therefore run nothing.
- The reviewer's latest request on the head commit is unanswered. It asks for tests that cover materialization
  together with property overrides, and it requires the materialized body to read the lowest layer of an overridden
  property rather than the top-level property. The current output reads the linker-materialized backing field. No
  separate story should be created for this while it is pending on the pull request itself.
- The generated `Equals` and `GetHashCode` read a virtual or non-sealed override auto-property through the property,
  where the C# compiler reads its backing field. `VerifyRecordMemberMaterialization` in
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.cs` on the branch of #1879 only
  reports `LAMA0652` for it, because the linker cannot add a backing field for a property that no advice overrides.
- `LAMA0552` is reported by `IntroduceMethodAdvice` rather than through an eligibility rule, because the
  introduction path evaluates no member-level eligibility against the member it overrides.
- A record member whose kind is `None` that reached the substitution generator would still hit the assertion failure
  at line 920 of
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs` on the
  branch of #1879. The pull request relies on `LAMA0552` and on the hiding of unspeakable names to keep such members
  away from the linker.
- No continuous integration build or test result is visible on GitHub for the head commit. A green build and a green
  test run are self-reported in the pull request body. A story that depends on `CanBeDeclaredExplicitly` or on the
  new diagnostics must therefore state the dependency on #1879 merging.

### 1.2. metalama/Metalama.Premium#85, Metalama.Premium on Roslyn 5.10 and net10.0

The pull request changes 47 files, every one of them a build file except one expected-output snapshot. It carries
the four commits of metalama/Metalama.Premium#82, which was closed without merging on 2026-09-03, so the Roslyn half
lands only through #85 and nothing of it exists on `develop/2027.0` today. The FACTS statement that
Metalama.Premium `develop/2027.0` is not aligned with PB-2027.0 describes the base branch, not this pull request.

What it covers: `RoslynVersion` and `RoslynMaxVersion` at `5.10.0-1.26365.3` in `Directory.Packages.props`, with the
`Metalama.Framework.Implementation.5.10.0` pin added and the `.4.12.0` pin removed; the deletion of
`eng/RoslynVersions/Roslyn.4.12.0.props` and the rename of the three variant project directories from `.4.12.0` to
`.5.0.0`; `eng/RoslynVersions/Roslyn.5.0.0.props` as the suffixed lower variant and the new
`eng/RoslynVersions/Roslyn.5.10.0.props` as the unsuffixed latest variant imported by
`eng/RoslynVersions/Latest.props`; the package payload of both variants for `net472` and `net10.0` and the
`MetalamaExtensionAssembly` metadata that selects them; the `InternalsVisibleTo` entries for the 5.10.0 assemblies;
the new `nuget.base.config` that declares the `roslyn-consolidated` feed and maps `Microsoft.CodeAnalysis.*` to it;
the removal of every `ROSLYN_*` define from Metalama.Premium; `net10.0` in place of `net8.0` in the 35 build files
that contained it, including the `metalama/net10.0` package paths and the `tasks/net10.0` licensing build task path;
`net472` in place of `net471` in the two caching backends and the load test project; `NuGetAuditMode` set to
`direct` for the test projects; and the conditioning of one ILRepack input on `net472`.

What it leaves open: `eng/src/BuildMetalamaPremium.csproj` stays on `net9.0` and `PostSharpEngineeringVersion` stays
at 2023.2.412, while the Metalama topic branch has already moved its own build tool project to `net10.0` with
engineering 2023.2.420 and the Visual Studio 18.9.2 build image, so Metalama.Premium needs the same follow-up; the
`NETSDK1233` and `LAMA0602` warnings are deliberately left visible in the three standalone solutions that the build
definition builds with the desktop MSBuild; and no `net11.0` string exists anywhere on the branch, so any `net11.0`
test or standalone project in Metalama.Premium is new work.

One consequence matters for the C# 15 stories. Metalama.Premium defines no `ROSLYN_*` constant and has no
preprocessor branch on the Roslyn version in any C# file, and the `.5.0.0` variant projects compile the same shared
sources as the unsuffixed projects. Any code in `Metalama.Extensions.Validation.Engine` or
`Metalama.Extensions.CodeFixes.Engine` that must name a Roslyn 5.10 syntax type therefore needs either a new define
in `eng/RoslynVersions/Roslyn.5.10.0.props` or a version-neutral implementation. The stale hint on line 13 of the
two unit test projects, which still tells test authors to use a `ROSLYN_X_Y_OR_GREATER` symbol, should be corrected
in the same story.

### 1.3. metalama/Metalama.Premium#84, mirror of the re-derived package caps

The pull request mirrors #1912 into Metalama.Premium as part of #1897 and changes two build files, with 10 additions
and 21 deletions.

What it covers: single central pins of `System.Memory` at 4.6.3 and `System.Runtime.CompilerServices.Unsafe` at
6.1.2 in `/home/user/metalama.premium/Directory.Packages.props`, equal to the core pins at lines 114 and 121 of
`/home/user/Metalama/Directory.Packages.props`; the complete removal of the variant-conditional properties that
carried those versions, from `Directory.Packages.props` and from `eng/RoslynVersions/Roslyn.5.0.0.props`, so that a
new `eng/RoslynVersions/Roslyn.5.10.0.props` needs no override of either package; and a rationale comment that cites
the measured binding-redirect ceilings and requires the values to stay equal to the core repository.

What it leaves open: a green Metalama.Premium build, since the pin values are verified only against the Metalama
core build and the measurement table at lines 101 to 124 of `/home/user/Metalama/Directory.Packages.md`; and every
other part of the PB-2027.0 alignment, which #85 carries.

Two constraints follow for a story. A Metalama.Premium alignment story must build on this branch, or rebase onto it
after merge, because it edits the same two files. The rationale comment names Visual Studio 2022 17.14 as one of the
two measured hosts, and PB-2027.0 drops Visual Studio 2022, so a story may correct the wording without changing the
values. Any change to the two versions requires re-measuring the binding redirects of the November 2026 long-term
servicing baseline first, an obligation recorded at line 385 of
[`Directory.Packages.md`](../../../Directory.Packages.md) in the core repository.

## 2. Pull requests merged into `develop/2027.0` since 2026-08-15

### 2.1. `metalama/Metalama`

| Pull request | Merged | Theme it settled |
| --- | --- | --- |
| #1912, re-derive the Visual Studio package caps and remove the `System.Memory` split (#1897) | 2026-09-03 | One central pin of `System.Memory` at 4.6.3 and of `System.Runtime.CompilerServices.Unsafe` at 6.1.2, equal to the binding-redirect ceilings of Visual Studio 2026 18.9, with the per-variant overrides removed. `Directory.Packages.md` records that the ceilings must be re-read against the November 2026 long-term servicing baseline after 2026-11-10. |
| #1910, raise the template language version to C# 14 (#1896) | 2026-09-03 | `MetalamaTemplateLanguageVersion` is 14.0 in `Directory.Build.props`, tied by its comment to `RoslynApiMinVersion`, so templates may use C# 14 but not C# 15 until the lower variant itself reaches the Roslyn that supports it. A standalone scenario pins the value. |
| #1911, degrade to no implementation when the host Roslyn is below the supported floor (#1898) | 2026-09-02 | The host-to-variant mapping in `RoslynVariantPolicy.cs` sends any host at or above 5.10 to the latest variant and 5.0 to 5.9 to the lower variant, with no upper bound, and reports the new error `LAMA0087` below the floor. A Roslyn 5.10 stable host therefore already selects the latest payload. |
| #1895, remove the `AnalysisLevel` pin (#1893) | 2026-09-02 | The analysis level follows the target framework version, so a project that later targets `net11.0` may surface further integrated development environment rules under `ContinuousIntegrationBuild`. The change to `LanguageVersionProvider.cs` is a null-check simplification and leaves the mapping from the SDK major version to the language version unchanged. |
| #1889, declare the prerelease Roslyn package source in the reference assembly locator project (#1885) | 2026-09-02 | The `roslyn-consolidated` source is derived from the hyphen in the Roslyn version string and written into the generated `nuget.config` on user machines. Adopting a stable Roslyn is the edit of that one version string, which removes the source again. |
| #1883, support Roslyn 5.10 and remove obsolete Roslyn version-specific symbols (#1881) | 2026-09-02 | The Roslyn 5.10 prerelease as the latest variant, the removal of the 4.12.0 variant, the real 5.10 grammar with the experimental declarations stripped before code generation, and the removal of 177 preprocessor blocks in 152 production files. The body records that C# 15 needed no change, because the language version of the consumed 5.10 preview stops at C# 14. |
| #1886, warn when the target framework, the .NET SDK or the Visual Studio version is outside the tested matrix (#1884) | 2026-09-02 | The declared support matrix in `Metalama.Framework.props`, with a maximum .NET Core application version and a maximum SDK version of 11.0, so a `net11.0` project and the .NET 11 SDK are inside the declared set. The standalone scenario has no `net11.0` case, and the `LangVersion` rewrite in `Metalama.Framework.targets` was not touched. |
| #1891, document the platform support doctrine and the PB-2027.0 baseline | 2026-09-02 | The doctrine and the baseline in [`platform-support.md`](../platform-support.md), together with a pre-release checklist that measures the November 2026 baseline. Documentation only, with no product change. |
| #1877, remove explicit support for .NET 8 and .NET 9 (#1876) | 2026-09-02 | Every `net8.0` and `net9.0` target framework of the four solutions became `net10.0`, the Core extension-assembly folder literal is `net10.0`, and the .NET 10 SDK is the build minimum. `Directory.Packages.md` notes that the user-surfacing package pins are still on the .NET 8.0 line pending #1903. |
| #1874, report PostSharp-compatible user and device hashes in the license audit (#1873) | 2026-08-31 | Licensing audit telemetry. No bearing on the language, the Roslyn version or .NET 11. |
| #1875, move the durable and immutable contracts to `IIncrementalObject` | 2026-08-31 | A follow-up to #1871 that relocates the two markers and trims redundant registrations. Unrelated to the platform or language move. |
| #1871, add the `[Durable]` and `[ImmutableType]` contracts and the analyzers that verify them | 2026-08-31 | A new analyzer assembly that targets `netstandard2.0` and references the minimal supported Roslyn API, packed into the `Metalama.Framework` package, so the Roslyn floor of PB-2027.0 applies to it as well. It also replaces retained syntax trees in the design-time pipeline by a document key and a durable diagnostic. |
| #1863, report licensing problems as diagnostics instead of crashing (#1859) | 2026-08-20 | Continuous integration detection and licensing error handling. Merged with base `develop/2026.1` and inherited by `develop/2027.0`. No bearing on the analysis. |
| #1862, instantiate the licensing authority keys lazily (#1861) | 2026-08-20 | The one merged change that reacts to .NET 11 itself. `DSA.Create` throws on macOS with .NET 11, so the keys are created only when a signature is verified. A signed licence key still fails on macOS with .NET 11, which remains open under #1860. |
| #1857, do not crash the pipeline when an aspect instance target does not resolve (#1856) | 2026-08-20 | Design-time robustness. An aspect instance whose target does not resolve in the current compilation is skipped instead of throwing. Unrelated to the platform or language move. |

### 2.2. `metalama/Metalama.Premium`

| Pull request | Merged | Theme it settled |
| --- | --- | --- |
| metalama/Metalama.Premium#81, adapt to the durability and immutability contracts | 2026-08-31 | The Metalama.Premium side of #1829 and #1871: a design-time result identified by a document key rather than by a syntax tree, and immutability markers on the architecture predicates. It settles nothing about .NET 11, C# 15 or Roslyn 5.10. |

This is the only pull request merged into `develop/2027.0` of Metalama.Premium since 2026-08-15. The other commits
on that branch are TeamCity version bumps and the version initialization commit.
metalama/Metalama.Premium#83 was merged with base `develop/2026.1` and is not in `develop/2027.0`.

## 3. Open issues that overlap with this analysis

| Issue | What it covers | Relation to this analysis |
| --- | --- | --- |
| #1913 | The alignment of Metalama.Premium with PB-2027.0: `net10.0` in place of `net8.0`, the removal of the Roslyn 4.12 variant, the 5.0.0 lower variant, the 5.10.0 latest variant, and the `roslyn-consolidated` feed in `nuget.base.config`. | Implemented by metalama/Metalama.Premium#85, which is open. The issue is still open and GitHub records no pull request that closes it, because a closing reference across repositories does not link. No story should restate its content. |
| #1903 | Re-derivation of the .NET 8.0 line pins of the user-surfacing packages, which #1877 left in place when it removed .NET 8 and .NET 9. | The obligation is recorded in [`Directory.Packages.md`](../../../Directory.Packages.md) and referenced by #1877. A runtime dependency story must treat these pins as owned by #1903. |
| #985 | The catch-all for later C# features in the template compiler. | It is the standing home of the template compiler work for any language version above the one currently supported, which makes it the natural parent of the C# 15 template stories rather than a duplicate of them. |
| #1217 | Support for several Roslyn versions in `Metalama.Extensions.Metrics`. | That package has no Roslyn variant today, so any story that adds Roslyn-version-specific code to it depends on #1217 first. |
| #1343 | `meta.Proceed()` in an aspect that overrides a compiler-synthesized record member. It is a user story of milestone 2027.0. | It stays open and is configured to be closed by #1879. Section 4 below is the only part of it that a new story may build on. |

## 4. Union support and the synthesized record member mechanism of #1879

A union story can reuse the design of #1879 but not its implementation. The distinction matters, because the two
member families are different.

The C# 15 proposal for unions, read from the `main` branch of `dotnet/csharplang` on 2026-09-03, lowers a union
declaration to a struct declaration, explicitly not a record struct. The generated type carries a `[Union]`
attribute, implements an `IUnion` interface, has one public constructor per case type and one public `object?
Value { get; }` auto-property. The proposal states that a user-declared member conflicting with a generated member
is an error, and it mentions no synthesis of `Equals`, `GetHashCode`, `ToString`, an operator or `Deconstruct`. The
members that a union synthesizes are therefore constructors and an auto-property, and not the family that
`SynthesizedRecordMemberBodyGenerator` materializes. What the compiler that ships with the November 2026 Roslyn
actually synthesizes, and whether `INamedTypeSymbol.IsRecord` is false for a union, are assumptions in this
document, because `Metalama.Compiler` is not cloned.

What is reusable is the shape of the solution: a syntax node substitution whose replaced node is the type
declaration rather than a member body, a body generator that produces statements and a result and handles the
return, assignment and discard cases, a non-inlineability hook for a generated body that declares a local variable,
the emission of the generated body into a `_Source` member on the non-inlined path, and a public code model
predicate in the manner of `CanBeDeclaredExplicitly` that answers whether a member can be written in source. A union
story should be phrased as an extension of that pattern, not as the addition of synthesized member support in
general.

The mechanism of #1879 is gated on records in four places, and each gate would have to be extended, all four
citations being to the branch of #1879:

- `SynthesizedRecordMemberBodyGenerator.GetMemberKind` returns `None` unless the containing type is a record, at
  line 102 of
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/SynthesizedRecordMemberBodyGenerator.cs`.
- The substitution selection at line 911 of
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs` matches only
  `SyntaxKind.RecordDeclaration` and `SyntaxKind.RecordStructDeclaration`. An implicitly declared union member whose
  primary declaration syntax is a `UnionDeclarationSyntax` would reach the assertion failure at line 920 of the same
  file when an aspect calls `meta.Proceed()`.
- `LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` and `GetSynthesizedPropertyOverrideTargets`, at lines 48
  and 76 of `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs`, collect only members
  whose primary declaration syntax is a record declaration.
- `MemberExtensions.IsRecordMemberAddedUnconditionally`, at line 87 of
  `Metalama.Framework/src/Metalama.Framework/Code/MemberExtensions.cs`, returns false unless the declaring type is a
  record. A union analog is needed for the generated `Value` property and the per-case constructors.

Outside #1879, nine sites in the working tree already switch on the record declaration kinds and would each need a
`UnionDeclaration` case, all under `Metalama.Framework/src/Metalama.Framework.Engine/Linking/`:
`LinkerSyntaxHandler.cs:104-105`, `LinkerRewritingDriver.cs:324`,
`LinkerAnalysisStep.SemanticBodyAnalyzer.cs:244` and `:418`, `LinkerAnalysisStep.AspectReferenceCollector.cs:203`,
`Inlining/ImplicitLastOverrideReferenceInliner.cs:69`, `LinkerInjectionStep.Rewriter.cs:641-642`,
`LinkerLateTransformationRegistry.cs:149-152` and `:190-191`, and `SymbolExtensions.cs:29-30`.

Three constraints follow for the union stories. The story must state its dependency on #1879 merging, because
`CanBeDeclaredExplicitly` and the two new diagnostics do not exist on `develop/2027.0`. It must not allocate the
diagnostic identifiers `LAMA0552`, `LAMA0651` and `LAMA0652`. It must state that the behaviour of the shipped
compiler for unions is unverified, and it must be re-checked when the stable grammar and the stable public
application programming interface are imported.
