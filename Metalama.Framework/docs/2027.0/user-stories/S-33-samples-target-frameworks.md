### S-33. Move the sample solutions to the target frameworks of PB-2027.0

- Issue type: User Story
- Labels: `enhancement`, `breaking`, `Area-Platforms`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Samples`
- Size: M
- Blocked by: nothing. The story needs a published 2027.0 package to build against, which S-01, S-09 and S-11 gate in
  time but not in dependency. A sample that demonstrates a C# 15 feature, if the scope decides to add one, is written
  after S-11.
- Findings: none. No theme document of this analysis names `Metalama.Samples`, which is what question Q13 of
  [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) records. The repository is not cloned in the session that produced this
  analysis, so no sample project file was read, and every statement below about the samples is marked as an
  assumption.

---

The `Metalama.Framework` package now declares .NET 10 as the lowest .NET target framework it supports.
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:25-38` declares the
platform requirement of the package, whose `MinimumNETCoreAppVersion` at `:30` is `10.0`, and
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:344-346` reports the warning
`LAMA0600` for a `.NETCoreApp` target framework below that value. A sample that targets `net8.0` or `net9.0`
therefore reports, on every build, that Metalama does not support the target framework of the sample itself. This is
an assumption about the samples rather than an observation: the target frameworks the sample projects declare were
not read.

#### Context

[`platform-support.md`](../../platform-support.md), at lines 211 to 215, states that the supported user target
frameworks of PB-2027.0 are `net10.0` and `net11.0`, that `net8.0` and `net9.0` are out of support at general
availability, and that this is a breaking change for users. The same document lists the two dropped user target
frameworks at `:295-297`. The removal in this repository is issue #1876, "Remove explicit support for .NET 8 and
.NET 9", closed on 2026-09-02; its residue in the engine defaults and the test gates is story S-05.

Two consequences differ in kind, and a sample meets one or the other.

A sample that targets `net8.0` and uses only the framework packages keeps a compatible asset, because
`Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj:4` ships `netstandard2.0` beside `net10.0`. It
builds, and it reports `LAMA0600`. The failure is a warning, which a sample repository can carry unnoticed.

A sample that targets `net8.0-windows` and uses the Windows Presentation Foundation aspects has no compatible asset
at all, because `Metalama.Patterns/src/Metalama.Patterns.Wpf/Metalama.Patterns.Wpf.csproj:4` now ships `net472` and
`net10.0-windows` and nothing else. `platform-support.md:214-215` names this as the most visible break of the
release. The failure is a restore error, which is loud.

One sample pattern is known from this repository and must survive the move.
`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/RunTimeAssemblyRewriter.cs:144-150` keeps an aspect
weaver in the run-time assembly, and the comment at `:146` states that this is a pattern used by `Metalama.Samples`
for the try.postsharp.net site. A sample built on that pattern exercises a code path that no other consumer
exercises, so it is worth naming in the verification rather than assuming that a green build covers it.

The samples are published at the examples site that `README.md:148` names, so a sample that stops building is a
public defect and not only an internal one.

#### Scope

- Inventory the target framework of every sample project and of every property file that sets one, and report the
  ones that PB-2027.0 drops.
- Raise every `net8.0` and `net9.0` target framework to `net10.0`, and every `net8.0-windows` target framework to
  `net10.0-windows`.
- Raise the referenced Metalama package version to 2027.0 and rebuild every sample.
- Check every sample that pins a `LangVersion`, because S-11 changes the language version that the targets accept and
  clamp.
- Verify that the samples which reference `Metalama.Framework` from a weaver project still build, which is the
  pattern that `RunTimeAssemblyRewriter.cs:146` names.
- Report every sample that cannot move, with the reason, so that a decision to remove it is taken deliberately.
- Decide whether the release adds a sample for the C# 15 features, and if so which. Reading a union, reading a closed
  hierarchy and overriding an extension indexer are the three capabilities of the release that a sample can show
  without depending on a question that is still open.
- State whether the continuous integration configuration of the sample repository pins a .NET SDK that PB-2027.0
  drops, and raise it if it does.

#### Acceptance criteria

- No sample project targets `net8.0`, `net9.0` or `net8.0-windows`.
- Every sample builds against the 2027.0 packages with no `LAMA0600`, `LAMA0601` or `LAMA0602` warning.
- The pull request description names every sample that was removed rather than moved, and the reason for each.
- The examples site publishes no sample whose target framework PB-2027.0 has dropped.

#### Not in scope

This story does not change the platform requirement metadata of
`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props`, which belongs to
`metalama/Metalama`. It adds no `net11.0` target framework to a sample, because sections 6 and 6c of
[`DECISIONS.md`](../DECISIONS.md) find no .NET 11 application programming interface that justifies one. It does not
write the conceptual documentation that accompanies a sample, which is S-34.

— Claude for @gfraiteur
