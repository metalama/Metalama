### S-27. Documentation: conceptual documentation of C# 15 and PB-2027.0

- Issue type: User Story
- Labels: `documentation`, `enhancement`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Documentation`
- Size: L
- Blocked by: S-15, S-29, S-16, S-19, S-21, S-24
- Findings: none. No theme document of this analysis produces a story for `metalama/Metalama.Documentation`, which is
  what question Q13 of [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) records. The repository is not cloned in the session
  that produced this analysis, so no page was read, no page path was verified, and every statement below about the
  existing pages is marked as an assumption.

---

No story of this plan writes the user-facing documentation of the C# 15 work. S-29 asks, at
`user-stories.md:1398-1399`, that its pull request description state which pages of `metalama/Metalama.Documentation`
must follow. S-30 repeats the same sentence at `user-stories.md:2027-2028`. S-24 states, at
`user-stories.md:1874-1876`, that it does not edit the conceptual documentation, because that is a separate repository
and therefore a separate pull request, and that it lists the pages rather than editing them. Two stories therefore
defer the page list, and the third excludes the pages. Nobody writes them. This story owns them.

#### Context

Two pages of the public documentation are cited from this repository and describe values that PB-2027.0 changes. The
requirements page is cited as the last source of [`platform-support.md`](../../platform-support.md), at `:385`. The
MSBuild property page is the help link of the three platform-check warnings, at
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:42`; the codes are `LAMA0600` for
the target framework, `LAMA0601` for the .NET SDK and `LAMA0602` for the Visual Studio version, which
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:278-280` documents. It is an
assumption that both pages state the previous baseline, because neither was read.

The platform statements that the pages have to carry are settled.
[`platform-support.md`](../../platform-support.md), at lines 111 to 116, gives the canonical short form of PB-2027.0, at
lines 124 to 134 excludes Visual Studio 2022 in its entirety, at lines 211 to 215 names `net10.0` and `net11.0` as the
supported user target frameworks and records the Windows Presentation Foundation break, and at lines 293 to 299
summarises what the baseline drops.

One documented value is being changed by another story and is documented in both repositories.
`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` holds the default of
`MetalamaCompileTimeTargetFrameworks`, which is `netstandard2.0;net8.0;net48` today and which S-03 changes to name
`net10.0`. Finding UT-3 records, at
[`06-user-tfm-patterns-tests-docs.md`](../06-user-tfm-patterns-tests-docs.md), lines 259 and 260, that whether the value
documented in `Metalama.Documentation` also names `net8.0` is an open question, and that the repository was not
present in the environment of that analysis. It is still not present, so the question is carried here rather than
answered.

The C# 15 subjects come from stories that this one follows. C# 15 as a supported language version is S-15. Reading a
union in the code model is S-18-1 and introducing one is S-29, with S-30 conditional on question Q1. Reading a closed
hierarchy is S-16 and introducing a closed class is S-28. Extension indexers are S-21. The rejection of a labeled
`break` or `continue` in a template is S-19, which follows section 4 of [`DECISIONS.md`](../DECISIONS.md).

Three facts of that work are easy to lose in a page and change what a reader should expect.

Section 2b of [`DECISIONS.md`](../DECISIONS.md) records that the public assembly `Metalama.Framework` is not built per
Roslyn version while the engine is, so on the hosts that the `Roslyn.5.0.0` variant serves, which are Rider and the
Visual Studio Code C# Dev Kit, a member that reports whether a type is a union reports the value of an ordinary type.
Whether the product reports that divergence is question Q2, and the pages state the outcome once it is settled.

Section 5e of [`DECISIONS.md`](../DECISIONS.md) records that there are two authoring forms of a union, the `union`
declaration and a type carrying `System.Runtime.CompilerServices.UnionAttribute`, that Roslyn reports both as unions,
and that the member restrictions apply to the first form only. A page that states a restriction has to say which of
the two forms it concerns, which is the reader-facing half of question Q9.

A case added to a `union` declaration is a build-time-only change, which S-30 states in its scope at
`user-stories.md:2027-2028` and which the reader has to be told before using the feature.

#### Scope

- Revise the requirements page so that it states PB-2027.0: Visual Studio 2026 and later, the current releases of
  Rider and of the Visual Studio Code C# Dev Kit, the .NET 10 and .NET 11 software development kits, `net10.0` and
  `net11.0` as user target frameworks, .NET Framework 4.7.2 and later, and .NET Standard 2.0 and 2.1. State plainly
  that Visual Studio 2022, `net8.0` and `net9.0` are no longer supported, and that a Windows Presentation Foundation
  application on .NET 8 or .NET 9 has no compatible asset.
- Revise the MSBuild property page for the three platform-check codes, for the `MetalamaCheckSupportedPlatform`
  property and for the `MetalamaSupportedPlatformExclusion` item that suppress them, and for the documented value of
  `MetalamaCompileTimeTargetFrameworks`, which S-03 changes.
- Answer the open question of finding UT-3, which is whether the documented value of
  `MetalamaCompileTimeTargetFrameworks` names `net8.0`, and correct it if it does.
- Write the page that states which C# 15 features an aspect may use in the run-time code it produces, and that the
  template language stays at C# 14 by section 4 of [`DECISIONS.md`](../DECISIONS.md).
- Write the page about unions: what an aspect reads about a union, what it may introduce, and which of the two
  authoring forms each statement concerns.
- Write the page about closed hierarchies: what an aspect reads, and that an aspect may introduce a closed class
  whose generated modifier list reads `closed partial class`.
- Write the page about extension indexers: overriding one, and introducing one into an extension block that declares
  a named receiver.
- Add the rejection of a labeled `break` and `continue` to the template language reference, with the reason, which is
  that the annotator cannot classify the label as compile-time or run-time, and state that run-time code that an
  aspect transforms is not affected.
- State, on every page of the C# 15 set, which design-time hosts show the feature, and report the divergence of
  question Q2 once it is settled.
- State that a case added to a `union` declaration is a build-time-only change, if question Q1 chooses to ship that
  form and S-30 is delivered.
- Publish the page list, so that S-29, S-24 and S-30 reference this issue instead of enumerating pages in their own
  pull request descriptions.

#### Acceptance criteria

- No page states a supported platform, a target framework or a C# version that PB-2027.0 has dropped.
- Every C# 15 capability that 2027.0 ships has a page, and no page documents a capability that no story of this
  release delivers.
- Every statement about a union names the authoring form it concerns.
- Every page of the C# 15 set states which design-time hosts show the feature.
- The documented value of `MetalamaCompileTimeTargetFrameworks` equals the default that the shipped engine carries.
- S-29, S-24 and S-30 reference this issue for their page list, and none of them enumerates pages of its own.

#### Not in scope

This story does not edit the internal architecture documents under `Metalama.Framework/docs`, which question Q17 of
[`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) records as outside the documentation story of this release and which S-24
partly owns. It does not document the introduction of structs, records, enums or delegates, which are the open issues
#869, #867, #866 and #865 and which section 5c of [`DECISIONS.md`](../DECISIONS.md) leaves out of scope. It does not
change the samples, which are S-25.

— Claude for @gfraiteur
