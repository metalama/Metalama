### S-31. Build: an opt-in flag to compile against the experimental Roslyn C# 15 API

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: none. The story comes from the review of this analysis, which asked whether the C# 15 work can start
  before a Roslyn that offers the application programming interface without an experimental marker.

---

The engine cannot name a C# 15 Roslyn member today, although the member exists in the Roslyn that this repository
consumes. `RoslynApiMaxVersion` is `5.10.0-1.26365.3` at `Directory.Packages.props:28`, and that build already
declares `SyntaxKind.UnionDeclaration`, `UnionDeclarationSyntax`, `ITypeSymbol.IsUnion`, `ITypeSymbol.IsClosed` and
`ITypeSymbol.UnionCaseTypes`. Every one of them is marked `[RSEXPERIMENTAL006]`, which turns a reference into a
build error. This story adds the opt-in that removes that error in this repository's own build, so that the engine
work of S-16, S-18-1 and S-21 can begin before S-10 and S-13 deliver a Roslyn that carries the members without the
marker.

The opt-in is a permanent harness that ships disabled, not scaffolding that a later story deletes. The situation it
answers recurs at every language version: Roslyn publishes the application programming interface of a feature behind
an experimental marker one or more releases before it declares the language version that makes the feature
supported, and the work that consumes it is blocked for that whole interval. S-13 turns the harness off when the
renamed variant carries the C# 15 members unmarked. It stays in the repository, parameterised rather than written
for C# 15, so that the next cycle enables it by naming the diagnostic identifiers and the grammar nodes of that
cycle instead of by building it again.

#### Context

The flag is a development switch and never a product behaviour. It changes how Metalama is compiled, not what
Metalama permits a user to compile: the refusal of the preview language version in a user project is unchanged, and
section 5 of [`DECISIONS.md`](../DECISIONS.md) keeps the template language at C# 14.

Four gates stand between the engine and the C# 15 members, and they need four different remedies rather than one
switch. Each remedy is expressed as a list or a value that the harness reads, and never as a C# 15 constant in the
build files, because the harness outlives this release. The empty list is the disabled state and reproduces the
behaviour of the repository before this story.

The first is the experimental marker. `[RSEXPERIMENTAL006]` is a diagnostic rather than a language construct, so
`NoWarn` removes it and no conditional compilation is involved. The marker is safe to suppress for the union, closed
hierarchy, collection argument and labeled jump members, because the evidence that their shape is settled is
specific: at the Roslyn 5.11 window the same `PublicAPI.Unshipped.txt` lines reappear with the prefix removed and
nothing else changed. It is not safe to suppress `RSEXPERIMENTAL007`, which covers the pre-compilation source
outputs, nor the unsafe evolution members, because both are still marked on the `main` branch and may still be
withdrawn.

The second is the syntax model. `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954` declares
`UnionDeclarationSyntax` with an `ExperimentalUrl` attribute, and `TreeReader.RemoveExperimentalDeclarations` in
`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:35-43` removes every node and field that carries one before
generation. The grammar file is copied unchanged from the Roslyn version it is named after, and the remarks of that
method require it to stay that way, so the stripping is made conditional rather than the file edited.

The third is the language version value, and it is the one that decides the shape of the whole story.
`LanguageVersion.CSharp15` does not exist in Roslyn 5.10, so a conditional block whose inactive branch names it does
not compile at all, and a flag with such a branch is not a switch that can be turned off. The repository already
solves this: `AllLanguageVersions` exists to expose a `LanguageVersion` "regardless of the version of Roslyn we are
compiling with", as numeric casts, and the value of `CSharp15` is 1500, read from the Roslyn 5.11 window. With that
constant both branches compile against either Roslyn, and what remains conditional is only which value is passed to
the parser, because Roslyn 5.10 reaches every C# 15 feature under `LanguageVersion.Preview` alone. That last part
needs no flag either: a probe of whether the running Roslyn recognises the value chooses between the two and stops
choosing the fallback by itself once the Roslyn moves. The repository already reads the running Roslyn this way in
`LanguageVersionExtensions.ToDisplayStringSafe`, whose comment records that `ToDisplayString` throws for a version
the bound Roslyn does not support, and in `TestOptions.cs:682`, which skips a test whose requested version the
current Roslyn does not recognise.

The fourth is the refusal of the preview language version in the aspect tests.
`CompileTimeAspectPipeline.VerifyLanguageVersion` reports `PreviewCSharpVersionNotSupported` unless the project sets
`MetalamaAllowPreviewLanguageFeatures`, and that opt-in already exists as `IProjectOptions.AllowPreviewLanguageFeatures`
and is named in the text of `LAMA0051`. The aspect test framework cannot reach it: `TestProjectOptions` inherits
`false` from `DefaultProjectOptions` and there is no test directive for it, while `@LanguageVersion` already exists
at `TestOptions.cs:254`. A test directive for the existing option is therefore the remedy, and it is permanent and
useful on its own rather than something to remove later.

Where the flag is declared follows from the two scopes it must reach, and the repository root is not one of them.
The root `Directory.Build.props` also covers the projects of the `Roslyn.5.0.0` variant, where these members do not
exist at any language version, and it does not reach the syntax generator in any case, because
`eng/src/Directory.Build.props` is a standalone project file that imports only `../Versions.props` and MSBuild stops
at the first such file it finds. The scope that matches the engine is
`eng/RoslynVersions/Roslyn.5.10.0.props`, which is imported through `Latest.props` by exactly the latest-variant
projects and which already defines `ROSLYN_5_10_0_OR_GREATER`.

The value is not committed in the enabled state. MSBuild exposes an environment variable as a property, so the
property defaults to false in the repository and a developer or a feature-branch build configuration enables it
without editing a file. This removes the failure this story would otherwise introduce, which is a release built from
a commit where the flag was left on.

#### Scope

- Add `eng/RoslynPreview.props`, declaring `AllowRoslynPreviewFeatures` with a default of false and defining
  `ALLOW_PREVIEW_LANG_VERSION` when it is true. Declare beside it the list of experimental diagnostic identifiers
  that the harness suppresses, as a property rather than a literal, so that a later cycle names its own; set it to
  `RSEXPERIMENTAL006` for this one, and add its content to `NoWarn` only when the flag is true.
- Import that file from `eng/RoslynVersions/Roslyn.5.10.0.props`, for the engine and the tests, and from
  `eng/src/Directory.Build.props`, for the syntax generator.
- Fail the build with a clear message when `AllowRoslynPreviewFeatures` and `ContinuousIntegrationBuild` are both
  true, so that a release cannot carry the flag. This check is permanent and is not disabled by S-13.
- Add `CSharp15` to `AllLanguageVersions`, as the numeric value 1500, following the entries that are already there
  for C# 10 to C# 14.
- Add the probe that yields `AllLanguageVersions.CSharp15` when the running Roslyn recognises it and
  `LanguageVersion.Preview` otherwise, and route the engine through it rather than through a conditional block.
- Give `TreeReader.RemoveExperimentalDeclarations` a list of the experimental features it must keep, identified by
  the value of the `ExperimentalUrl` attribute that the grammar file carries, and pass the four settled features of
  this cycle when the flag is set. An empty list keeps the behaviour the method has today, and the unsafe
  expression is never in the list.
- Add a test directive that sets `AllowPreviewLanguageFeatures` on a test project, and override the property in
  `TestProjectOptions`. This is permanent and is independent of the flag.

#### Acceptance criteria

- With the flag unset, the build is byte-identical in behaviour to the build before this story, and no source names
  an experimental Roslyn member.
- With the flag set, a source file of the engine compiles a reference to `SyntaxKind.UnionDeclaration` and to
  `ITypeSymbol.IsUnion` with no diagnostic, and the generated meta syntax rewriter visits a union declaration.
- With the flag set, an aspect test whose target code declares a union runs, subject to the reference assemblies
  named in the not-in-scope section below.
- Both Roslyn variants build with the flag unset, and the latest variant builds with it set.
- The suppressed diagnostic identifiers and the kept grammar nodes are read from properties, so that enabling the
  harness for a later language version is a change of those values and not of the harness.
- A build with `ContinuousIntegrationBuild` and the flag both set fails with the message this story adds.
- Zero warnings under `-p:ContinuousIntegrationBuild=True` for every project touched.

#### Not in scope

The reference assemblies that a union needs. `System.Runtime.CompilerServices.UnionAttribute` and `IUnion` are .NET
11 runtime types, so a test that declares a union needs a target framework that provides them, whatever Roslyn
compiles it. Whether Roslyn embeds them when they are absent, as it does for other compiler-recognised attributes,
or reports an error, decides whether the aspect test project needs a `net11.0` leg for its target code. That
question is narrower than the target framework decision of section 9 of [`DECISIONS.md`](../DECISIONS.md), which
concerns the product assets and the product test matrix, and it is answered when this story is implemented.

Turning the harness off, which belongs to S-13. That story renames the latest variant and moves the sources from
`ALLOW_PREVIEW_LANG_VERSION` to the permanent `ROSLYN_5_12_0_OR_GREATER` gate, then empties the list of suppressed
diagnostic identifiers and the list of kept grammar nodes. `eng/RoslynPreview.props`, its two imports, the
conditional arm of the syntax generator, the continuous integration check and the test directive all stay, so that
the next cycle enables the harness rather than writing it again. The probe of the language version value needs no
action at all: it stops taking the fallback when the Roslyn moves.

Any change to what a user project may compile. The refusal of the preview language version, the value of
`MetalamaTemplateLanguageVersion` and the supported language version list of a shipped build are unchanged.

`metalama/Metalama.Premium`, whose engines compile per variant in the same way. S-14 mirrors the variant work of
S-13, and it carries this flag if the Premium engine sources need it before that.

— Claude for @gfraiteur
