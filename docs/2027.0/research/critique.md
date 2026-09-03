# Completeness critique of the .NET 11 / C# 15 / Roslyn 5.x research inventory

Date of critique: 2026-09-03. Consumer: Metalama, a Roslyn-based C# source-rewriting and
aspect-weaving framework that replaces the compiler, generates and links C# syntax, and runs
inside devenv.exe, the out-of-process Roslyn analyzer host, Rider, VS Code C# Dev Kit, MSBuild
and its own compiler executable.

## Method

I read the nine agent summaries and the notes directory
(`C:/Users/GaelFraiteur/AppData/Local/Temp/claude/C--src-Metalama-2027-0-Metalama/86248111-7c7e-4f30-bf61-ae10afe3e5e4/scratchpad/net11/`),
then ran targeted verification against primary sources on each axis the task statement flagged as
easy to forget, in order to distinguish "the inventory omitted it" from "there is genuinely nothing
there". Verification performed:

1. WebSearch: `Roslyn .NET 11 reference assembly MethodImplAttributes.Async runtime async refasm`
2. WebSearch: `dotnet 11 Hot Reload Edit and Continue Roslyn 5.12 changes EnC capabilities`
3. WebFetch: `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/features/interceptors.md`
4. WebSearch: `C# 15 .NET 11 "file-based apps" "#:" directives IgnoredDirectiveTrivia Roslyn changes`
5. WebSearch: `Roslyn DocumentationCommentId extension members "E.extension" doc comment ID format C# 14 15 unions`
6. WebSearch: `dotnet 11 SDK net11.0 target framework defaults ImplicitUsings Nullable AnalysisLevel WarningLevel changes`

Searches 1, 2 and 6 returned only generic or pre-.NET-11 material, which is itself evidence: those
axes cannot be researched from prose documentation and must be attacked through primary source
files in dotnet/roslyn, dotnet/runtime and dotnet/sdk. Searches 3, 4 and 5 returned concrete facts
that are absent from the inventory and are recorded below.

## What the inventory already covers well (no gap reported)

Listed so a follow-up stage does not re-commission them.

- **New syntax nodes and SyntaxKind values.** `UnionDeclarationSyntax`, `WithElementSyntax`,
  `UnsafeExpressionSyntax`, `UnionKeyword = 8452`, `ClosedKeyword = 8453`, `SafeKeyword = 8454`,
  `UnsafeExpression = 8769`, `WithElement = 9081`, `UnionDeclaration = 9082`. Two independent
  agents diffed Syntax.xml between `release/dev18.0` and `main`.
- **Grammar changes with no new node.** The `closed` and `safe` contextual modifiers in the
  existing `Modifiers` token list; the optional `Name` field inserted mid-node on
  `BreakStatementSyntax` and `ContinueStatementSyntax`; `IndexerDeclarationSyntax` in a new
  position (inside `ExtensionBlockDeclarationSyntax`); the case-type list of a union modelled as a
  `ParameterListSyntax` whose `ParameterSyntax` entries carry a `Type` and no `Identifier`; the
  `(X.Y) when` switch-arm reparse. This axis is well served.
- **C# scripting API.** The Roslyn API agent diffed `Scripting Core` and `Scripting CSharp`
  PublicAPI files across the whole 5.x range and found zero changes. The only scripting-adjacent
  detail is that `DeclarationModifiers.Closed` is granted for `TypeKind.Submission` as well as
  `TypeKind.Class`. Nothing further to commission.
- **Cut and deferred C# 15 features.** Dictionary expressions, null-conditional await, chained
  relational comparison, target-typed static member access, relaxed modifier ordering, compound
  assignment in initializers, extension members on typeless receivers, runtime async streams,
  extension constants, type parameter inference from constraints, extension events and extension
  constructors are all identified as not shipping, with `MessageID.cs` used as the decisive gate.
  The reverted union "Try-Both" matching and the removed `IUnion<TUnion>` are also captured.
- **The "C# 16" labelling confusion.** Three agents independently established that
  `LanguageVersion` has no `CSharp16` member and that the breaking-change document's
  "langversion:16" means `LanguageVersion.Preview`.
- **The need to re-check between Preview 7 and GA.** Every agent flagged it; it is a scheduling
  item for the orchestrator, not a research gap.

---

## Gap 1. Debug information: sequence points, PDBs, and Edit-and-Continue / Hot Reload

**What the inventory has.** Almost nothing. The only debug-adjacent facts in the nine summaries
are: (a) `EmitDifferenceOptions.MethodImplEntriesSupported`, an init-only property added in Roslyn
5.3 for Edit-and-Continue, mentioned in one line without explanation; (b) runtime-async
`ResumeInfo.DiagnosticIP` and the `ASYNC` native-to-IL mapping, and the note that the debugger was
taught to see through the Task-returning thunk, with two `MethodDesc` variants per metadata token;
(c) an explicit open question that "Edit-and-Continue / Hot Reload behaviour for runtime-async
methods is undocumented", with a Roslyn TODO about where `AwaitYieldPoint` / `AwaitResumePoint`
records need to be inserted, and the observation that `AbstractEditAndContinueAnalyzer.cs` contains
no runtime-async handling; (d) the extension-indexer test plan item "check that EnC is blocked",
still unchecked; (e) `SourceHashAlgorithm.Sha384` and `Sha512` added in Roslyn 5.6.

**Why this is a gap for a source-rewriting tool.** A tool that rewrites source and emits its own
syntax owns the mapping from generated syntax back to user source, and that mapping lives entirely
in debug information: sequence points, the document table and its per-document checksum algorithm,
local-scope and local-variable records, the state-machine hoisted-local map in
`MethodDebugInformation`, the custom debug information blobs carrying EnC local slot maps and
lambda/closure maps, and the `EmbeddedSource` and SourceLink debug directory entries. Two headline
.NET 11 changes disturb exactly this area:

- **Runtime async removes the state machine.** With no `<M>d__N` type there is no state-machine
  hoisted-local scope and no `[AsyncStateMachine]`-driven async stepping information for those
  methods. What replaces them, and what an EnC delta must now emit, is undocumented in the
  inventory; the inventory's own open questions confirm the absence.
- **`SourceHashAlgorithm.Sha384` / `Sha512`.** The document checksum algorithm is written into the
  PDB document table and is the algorithm used for `#line`-mapped documents and `#pragma checksum`.
  If the SDK default moves off SHA-256 for net11.0, every tool that computes or compares document
  checksums must follow. Whether the default changed was never checked.

**Follow-up research prompt.** (see `researchPrompt` in the structured summary)

---

## Gap 2. `#line` directives, line mapping, and generated-code detection

**What the inventory has.** Nothing on `#line`. Nothing on generated-code detection. The single
adjacent fact is CS9378, a dedicated error for a `#!` shebang directive that is not at the start of
the file, which replaced a misleading CS1040 in Preview 4.

**Why this is a gap.** A tool that replaces user source with generated source relies on two
mechanisms to keep the rest of the ecosystem coherent:

- **`#line`, including the C# 10 `#line span` form** (`#line (1,1)-(2,2) 5 "file.razor"`), to point
  diagnostics and the debugger back at the user's original file and column. Any change to how the
  parser models these directives, to the checksum requirement of `#line hidden` and
  `#pragma checksum`, or to how a mapped span interacts with the new syntax, changes the output a
  rewriter must produce.
- **Generated-code detection**, which decides whether analyzers and IDE features run over the
  output. The mechanisms are the generated-file name heuristics (`.g.cs`, `.designer.cs`,
  `TemporaryGeneratedFile_*`), the `<auto-generated>` header comment, `[GeneratedCode]` and
  `[CompilerGenerated]`, the `generated_code = true` EditorConfig key, and the unconditional
  treatment of source-generator output as generated. Whether any of this moved in the .NET 11 wave
  was never checked, and it decides whether the now-larger .NET 11 analyzer set (the SDK agent
  established that `AnalysisLevel=latest` silently meant .NET 9 rules and now correctly means .NET
  11 rules) fires on generated output.

A specific new interaction nobody examined: source produced by
`RegisterPreCompilationSourceOutput` is added to the *initial* compilation. Whether those trees
carry the same generated-code marking as ordinary generator output is unestablished.

---

## Gap 3. Reference assemblies and metadata emission of the new attributes and flags

**What the inventory has.** The individual metadata artifacts are well documented in isolation:
`IsClosedTypeAttribute` with a `Type[] DerivedTypes` property, `[CompilerFeatureRequired("ClosedClasses")]`
on every constructor of a closed class, `UnionAttribute` and `IUnion`, `MemorySafetyRulesAttribute(2)`
at module level, `RequiresUnsafeAttribute` per member, `MethodImplAttributes.Async` (0x2000) on the
MethodDef row, `TypeAttributes.ExtendedLayout`, and `ExtensionMarkerName` plus `DefaultMemberAttribute`
on extension grouping types. One agent also established that `IsClosedTypeAttribute` and
`CompilerFeatureRequiredAttribute` are filtered out of `ISymbol.GetAttributes()` for source and
metadata symbols alike.

**Why this is still a gap.** Nobody asked what happens on the *reference assembly* path (`/refout`,
`/refonly`, `ProduceReferenceAssembly`). Reference assemblies drop method bodies and private
members, and the rules for which attributes survive are hand-maintained in Roslyn. This matters in
several directions at once:

- `MethodImplAttributes.Async` describes a *body* convention (the `ret` type deliberately disagrees
  with the signature). A reference assembly has no body. Whether the flag survives, and what an
  IL reader then sees, is unknown.
- `IsClosedTypeAttribute.DerivedTypes` records the derived-type set for consuming compilers, but a
  reference assembly may omit internal derived types, so `ClosedDerivedTypeInfo.IsComplete` could
  differ between the implementation and reference assemblies. One agent listed "how the compiler
  computes the subtype set of a closed class read from a reference assembly, and what `IsComplete`
  being false signifies" as explicitly unresolved.
- `RequiresUnsafeAttribute` is the cross-assembly carrier of requires-unsafe-ness, and the
  inventory notes that `extern` is "not guaranteed to be preserved in reference assemblies", which
  implies the reference-assembly path was a design consideration nobody followed up.
- The extension-member lowering emits skeleton properties whose accessors throw
  `NotImplementedException` plus separate static implementation methods. Which survive into a
  reference assembly decides whether an extension indexer is consumable from a ref-asm reference.

The reading side is the same question: the runtime-BCL agent recorded that no change to
`System.Reflection.Metadata` is documented anywhere and could not establish whether the library is
genuinely unchanged, and the runtime-async agent asked whether `System.Reflection.Metadata`,
Mono.Cecil and ILVerify accept the 0x2000 bit. Commission them together.

---

## Gap 4. Interceptors in the .NET 11 wave

**What the inventory has.** One negative finding: the Roslyn interceptors document says the feature
is stable since the .NET 9.0.2xx SDK, the attribute form is
`InterceptsLocationAttribute(int version, string data)`, no interceptors entry appears in the
feature-status working set or the C# 15 table, and no new `IDS_Feature` exists, "so no C# 15 change
to interceptors was found".

**Why this is not enough.** That negative was established only from the feature-status table and
`MessageID.cs`, which by construction detect only a *language-version-gated* change. My WebFetch of
`docs/features/interceptors.md` surfaced three facts of first-order importance to a source-rewriting
tool that are absent from the inventory:

- The version 1 data encoding contains a **16-byte xxHash128 content checksum of the file
  containing the intercepted call**. Any tool that rewrites the user's source before or alongside a
  generator that emits interceptors invalidates that checksum. This is a direct, hard interaction
  between source rewriting and interceptors, and the inventory does not mention it at all.
- The supported public API is
  `GetInterceptableLocation(this SemanticModel, InvocationExpressionSyntax, CancellationToken)`,
  and the document recommends generators depend on it rather than construct the attribute by hand.
  The inventory records the attribute shape but neither this method, nor the `InterceptableLocation`
  type, nor its members.
- The document says the compiler "may introduce new encodings for the location in the future, with
  corresponding new version numbers", and that interceptors cannot currently intercept
  "constructors, delegates, properties, local functions, operators, etc.", with support for more
  member kinds possibly added later. C# 15 adds extension indexers and C# 14 added extension
  operators; whether the interceptable set grew was never checked.

Also unchecked: whether a generator may emit interceptors from the new
`RegisterPreCompilationSourceOutput` stage, whose source joins the initial compilation, and the
current status of the `InterceptorsNamespaces` / `InterceptorsPreviewNamespaces` MSBuild properties
in the .NET 11 SDK.

---

## Gap 5. The source-generator driver: ordering, caching and host behaviour beyond the new API

**What the inventory has.** The new API is well covered: `RegisterPreCompilationSourceOutput` (two
overloads), `PreCompilationSourceProductionContext`,
`IncrementalGeneratorOutputKind.PreCompilation = 16`,
`WellKnownGeneratorOutputs.PreCompilationSourceOutput`, `RSEXPERIMENTAL007`, and the Razor
motivation (roughly a 50 percent speedup by removing a private intermediate compilation). The
Roslyn API agent also diffed the whole Workspaces and Features PublicAPI surface and found only
`DeclarationModifiers.Closed`.

**Why this is still a gap.** The new stage changes the shape of a compilation run, and none of its
behavioural consequences were researched. A tool that hosts or replaces the generator driver needs:

- **Ordering and visibility.** The stage's output "is added to the initial compilation, so it is
  visible to every generator's standard phase". Whether one generator's PreCompilation output is
  visible to another generator's PreCompilation stage, what the inter-generator ordering rule is,
  and whether it is deterministic, is unestablished.
- **Analyzer and semantic-model consequences.** Whether the compilation handed to analyzers now
  contains generator-authored trees, and where they fall in `Compilation.SyntaxTrees` order.
- **Driver API mechanics.** `GeneratorDriver.RunGeneratorsAndUpdateCompilation`,
  `GeneratorDriverRunResult`, `GeneratorRunResult`, `IncrementalGeneratorRunStep` and
  `TrackIncrementalGeneratorSteps` all have to account for a third output kind; the host-outputs
  surface (`RSEXPERIMENTAL004`) may also have moved.
- **Where generated files land on disk.** `EmitCompilerGeneratedFiles` and
  `CompilerGeneratedFilesOutputPath` determine the file paths a rewriting tool observes, and
  whether PreCompilation output is written there is unknown.

This gap is deliberately scoped to behaviour and hosting; the public API list is covered.

---

## Gap 6. The exact consequences of targeting `net11.0`, including down-level multi-targeting

**What the inventory has.** Scattered pieces found while researching other things: default
`LangVersion` for net11.0 is C# 15; nine `Microsoft.Extensions.*` packages moved into the shared
framework with NU1510 and possible type collisions; `AnalysisLevel=latest` now correctly resolves
to .NET 11 rules; `SdkAnalysisLevel` 11.0.100 gates NU1703 and NU1019 and ages out values below
8.0.100; Roslyn packages dropped net8.0 and net9.0 assets; template-engine packages dropped
netstandard2.0; analyzers must still target netstandard2.0; the x86-64-v2 baseline;
`NET11_0_OR_GREATER` mentioned once, incidentally, inside the `PackagePart.GetStream` entry.

**Why this is still a gap.** Nobody assembled the TFM's consequence set, and one consequence is
load-bearing and entirely unexamined:

- **Whether the C# 15 features are usable below net11.0.** The unions agent established that the
  compiler does *not* synthesize `UnionAttribute`, `IUnion` or `IsClosedTypeAttribute` and that
  "users must reference or define them". The memory-safety agent established the opposite
  convention for `MemorySafetyRulesAttribute` and `RequiresUnsafeAttribute`, which the compiler
  *does* synthesize when missing. The answer therefore differs per feature, and for a framework
  that multi-targets down to netstandard2.0 and net472 the question "which C# 15 constructs can I
  emit when the target framework is net472, and what must be polyfilled" is decisive. It was never
  asked in that form.
- **`CompilerFeatureRequiredAttribute` availability.** A closed class emits it on every constructor
  and reports CS0656 when it is absent, so down-level targets need it from somewhere.
- The routine list was never enumerated: the preprocessor symbols implied by net11.0, the contents
  and assembly versions of the reference pack, whether any SDK default changed for net11.0
  (`Nullable`, `ImplicitUsings`, `InvariantGlobalization`, `WarningLevel`, `AnalysisLevel`,
  `ChecksumAlgorithm`, `ProduceReferenceAssembly`, trimming and AOT defaults), and the roll-forward
  and `NETSDK` diagnostics involved. My verification search found the net11.0 template defaults were
  still unsettled during the preview cycle, so this needs checking against the shipped targets files.

---

## Gap 7. Symbol display, documentation comment IDs, CREF and `SyntaxGenerator` for the new constructs

**What the inventory has.** One negative and one incidental fact: "SyntaxGenerator gained no union
or extension-indexer factory" (from the Workspaces PublicAPI diff), and two CREF examples for
extension indexers, `E.extension(int).this[string]` and `E.get_Item(int, string)`, recorded as
"CREF binding is marked done" in the extension-indexer test plan.

**Why this is a gap.** A framework that maps its own code model onto `ISymbol` and regenerates
declarations from symbols depends on three surfaces no agent examined systematically:

- **`SymbolDisplay` / `ISymbol.ToDisplayString`.** Does a union type display with the `union`
  keyword, or as a struct? Does a closed class display the `closed` modifier? A new *public enum
  member* would have shown up in the PublicAPI diff, but display *behaviour* for existing formats
  is not a PublicAPI change and would not. Since a closed class is `TypeKind.Class` with a flag and
  a union is `TypeKind.Struct` with a flag, the default display can silently be wrong.
- **`DocumentationCommentId`.** Doc comment IDs are how a tool serialises a symbol reference into a
  durable string. My verification search confirmed that the C# 14 extension lowering already feeds
  unspeakable grouping and marker type names (of the form `<G>$8B58B811E742D8E9EA7E14F878F87B0F`
  and `<M>$2C37A6F24442AF359D03A7723186221C`) into `DocumentationCommentId`. Extension indexers add
  a new member kind to that scheme and unions add a declared form with no counterpart in the
  existing ID grammar. Whether `CreateDeclarationId` and `GetFirstSymbolForDeclarationId` round-trip
  them was never checked, and a mismatch is a silent correctness defect for anything that persists
  symbol identities across compilations.
- **`SyntaxGenerator`.** The Workspaces generator gained `DeclarationModifiers.Closed` but no union
  or extension-indexer factory. What `SyntaxGenerator.Declaration(ISymbol)` does for a union
  (throw, produce a struct, produce nothing) is unestablished.

---

## Gap 8. File-based apps, `#:` ignored directives and the shebang

**What the inventory has.** A single incidental fact: Preview 4 added CS9378 for a `#!` shebang
that is not at the start of the file. One SDK note mentions that file-based app execution falls
back from the NativeAOT CLI path to the managed CLI.

**Why this is a gap.** My verification search established that this is an actively growing area in
.NET 11, not a settled .NET 10 feature. The directive set is `#:include`, `#:package`, `#:project`,
`#:property` and `#:sdk`, with one documented as "available in .NET 11 Preview 3 and .NET SDK
10.0.300 and later", and a further `#:ref` directive was added to reference another file-based app
as a library. On the Roslyn side these parse into a real node,
`IgnoredDirectiveTriviaSyntax : DirectiveTriviaSyntax`, carrying `HashToken`, `ColonToken`,
`EndOfDirectiveToken` and `IsActive`, with API design tracked by dotnet/roslyn issue 77697. Roslyn
also carries `docs/features/file-based-programs-vscode.md` for the IDE side, and issue 81252 covers
not restoring loose files that lack these directives.

This matters to a compiler-replacing, source-rewriting tool three ways: the syntax trees it
receives can contain a directive trivia kind absent from its grammar model; a shebang on the first
line shifts every subsequent position and interacts with `#line`; and a file-based app is compiled
through a different SDK path with a synthesised project, which determines whether a compiler
replacement or an analyzer is loaded at all. This is also the one axis where a genuinely new
directive kind can reach a `MetaSyntaxRewriter` generated from Syntax.xml without appearing in any
C# 15 feature list, because it is an SDK feature rather than a language feature.

---

## Axes deliberately not turned into gaps

- **Warning wave 11.** The compiler-breaking agent verified from
  `docs/compilers/CSharp/Warnversion Warning Waves.md` and the Learn warning-waves article that no
  warning wave 11 exists and that no warning is newly on by default. That is a clean, sourced
  negative. The residual question of whether the SDK raises the default `WarningLevel` to 11 before
  GA is folded into Gap 6, which enumerates the net11.0 SDK defaults.
- **Nullable and definite-assignment analysis.** Two agents independently confirmed no breaking
  change, and the new construct-specific nullability rules for unions are documented in detail.
- **The C# scripting API.** Verified unchanged by a PublicAPI diff across the whole 5.x range.
- **Preview 8 / RC delta.** Every agent already flagged that only Previews 1 through 7 exist as of
  2026-09-03 and that the work must be re-checked near GA. This is a scheduling instruction for the
  orchestrator, not a research gap, and turning it into one would consume a slot without adding
  information.
