### S-26. Documentation: internal architecture documents

- Issue type: User Story
- Labels: `enhancement`, `documentation`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-18-1, S-18-3, S-18-5, S-28 and S-29, which are the stories whose result these documents describe, and
  S-30 if question Q1 of [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) files it. A documentation story is normally blocked
  by the stories whose result it describes, because a document written before the code is a second thing to correct.
- Findings: none

---

Nine documents of `Metalama.Framework/docs` are named in no document of `Metalama.Framework/docs/2027.0`, which is
the completeness review of this analysis. They are
[`compilation-model.md`](../../compilation-model.md), [`pipeline.md`](../../pipeline.md),
[`linker-architecture.md`](../../linker-architecture.md), [`linker-overview.md`](../../linker-overview.md),
[`linker-callsite.md`](../../linker-callsite.md), [`kind-check-optimization.md`](../../kind-check-optimization.md),
[`trivia-and-formatting.md`](../../trivia-and-formatting.md), [`design-time-memory.md`](../../design-time-memory.md) and
[`cross-process-communication.md`](../../cross-process-communication.md). Each was read against the stories of this
release. Five of them describe a mechanism that a story changes and are the scope of this story. Four of them need
nothing, and the reason for each is stated below so that the question is not asked a third time.

#### Context

This story is the internal counterpart of S-24. S-24 carries the platform, dependency and extensibility
documentation, which states the previous platform baseline. This story carries the documents that describe how the
engine works, which no story of the release names. The division follows the subject: a statement about a target
framework, a package version or a Roslyn variant belongs to S-24, and a statement about the code model, the pipeline,
the linker or the kind-check doctrine belongs here.

Like S-24, this story is scheduled after the work it describes, for the same reason.

Four of the nine documents need no change, and the reason is recorded rather than left implicit.

[`linker-callsite.md`](../../linker-callsite.md) describes the closure check of the `[OnInitialized]` call-site advice
and the propagation of `ContainsInitializableTypes` through the transitive manifest. None of the three terms of that
check is keyed on a type kind, a syntax kind or a language version: they test the `IInitializable` interface and
whether the compilation is partial. The one property of the document that the release relies on is the statement of
the section "Term 3" that the derived type index excludes types declared in referenced assemblies, and section 11 of
[`DECISIONS.md`](../DECISIONS.md) confirms that reading a closed hierarchy needs no new way to enumerate derived types,
so that statement stays correct as written.

[`trivia-and-formatting.md`](../../trivia-and-formatting.md) describes the three formatting modes, the conditional
trivia convention and the `CodeFormatter` pipeline. The linker analysis established that the code formatter pipeline
is agnostic of the node kind, that the custom simplifier passes an unknown node through, and that a collection
expression needs no parentheses under a cast whether or not it carries a leading with-element, at
[`04-linker-and-advice.md`](../04-linker-and-advice.md) lines 980 to 993. The generated file that the document names in
the section "Performance: rules of thumb for contributors" is written with a placeholder for the Roslyn variant, so
the renaming of S-13 does not make the path wrong.

[`design-time-memory.md`](../../design-time-memory.md) states its rule in terms of the object types that an object
outliving a request may hold, and not in terms of the declarations that the pipeline analyses, so a union adds no
type to that list. The design-time diagnostics that question Q2 and story S-30 may add are already governed by the
rule stated in the section "What the pipeline stores for longer than one request", which requires the arguments of a
diagnostic to be durable. The document's section "What has not been examined" already records that the source
generator pipeline, which S-18-4 changes, was not audited, so the union arm of the design-time generator makes no
statement of the document untrue.

[`cross-process-communication.md`](../../cross-process-communication.md) describes the two boundaries and the three
rules that govern them, and it already names `Metalama.Vsx` as the cross-version consumer of
`Metalama.Framework.DesignTime.Contracts`. The design-time analysis established that the remote procedure call
surface and the cross-version contracts transport no type kind, no syntax kind and no language version, that the only
kind on the wire is `AspectExplorerDeclarationKind`, and that the contracts assembly needs no change for this
release, at [`analysis-reports/05-design-time-workspaces-linqpad.md`](../analysis-reports/05-design-time-workspaces-linqpad.md)
lines 118 and 129. The absence of `Metalama.Vsx` from the analysis is a gap of the analysis, not
of this document.

#### Scope

- [`compilation-model.md`](../../compilation-model.md). The section "Declaration Types" divides declarations into
  symbol-based source declarations and builder-based introduced declarations, and the section "How Transformations
  Are Applied" with its list "AddDeclaration Routing" describes how a builder reaches an updatable collection. S-29
  adds a transformation that implements `IIntroduceDeclarationTransformation` without `IInjectMemberTransformation`,
  in order to register the synthesized `Value` property and the per-case constructors of an introduced union in the
  code model without injecting syntax. That shape serves namespaces alone today, and a namespace is not a member.
  State the shape, state that the members registered through it are marked implicitly declared, and add the case to
  the table of the section "Declaration Origin", which has no row for a member that the compiler would synthesize and
  that Metalama registers as a builder. State in the section "DeclarationBuilder vs DeclarationBuilderData" that the
  named type builder carries a case list, which S-29 adds, and a closedness flag, which S-28 adds.
- [`pipeline.md`](../../pipeline.md). The section "Level 4: Parallel Type Processing", under "Processing Structure",
  states that several aspect instances on one type execute sequentially and does not state how they are ordered. The
  order comes from `AspectInstanceComparer.Compare` in
  `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs:198-269`, whose only
  escape hatch for two targets that share a span requires two implicitly declared methods of a record, at `:250-265`,
  and which otherwise throws at `:267`. S-18-5 generalises that escape hatch to any implicitly declared members that
  share a span and removes the assertion that requires a record. State the ordering rule and its new condition, since
  the table "Key Files" already directs a reader to that file for the parallel processing alone.
- [`linker-overview.md`](../../linker-overview.md). The section "Step 3 - Linking" states that
  `LinkerLinkingStep.LinkingRewriter` goes through every class. S-18-3 replaces the per-kind dispatch of
  `LinkerLinkingStep.LinkingRewriter.cs:37-85` and of `LinkerInjectionStep.Rewriter.cs:316-324` by a dispatch over the
  abstract type declaration, so the sentence must say any type declaration and must name the union as one of them.
  The section "Step 1 - Injection" states that the collected information produces `LinkerInjectionRegistry`. S-29
  registers member builders that have no injected member, which
  `LinkerInjectionRegistry.GetTransformationForBuilder` has never received, so record what such a builder does in the
  registry once S-29 has established it.
- [`linker-architecture.md`](../../linker-architecture.md). The section "Primary Constructor Handling" states that
  primary constructors are found on records and on classes, and that
  `ApplyMemberLevelTransformationsToPrimaryConstructor` modifies the parameter list and the base list of the type
  declaration. For a union declaration that parameter list is the case list and not a parameter list. S-18-3 requires
  the fallback path to preserve it and to stay out of the record and struct paths, and S-18-1 names
  `ImplicitLastOverrideReferenceInliner` and `LinkerLateTransformationRegistry` as the two consumers where the same
  distinction applies. State the distinction in that section. If question Q1 files S-30, add to the section
  "Constructor Rewriting Flow", under "Source Constructors", that the case list of the part the user wrote is
  rewritten in the same field and by the same method as the parameter list of a partial constructor, which is the
  precedent S-30 follows.
- [`kind-check-optimization.md`](../../kind-check-optimization.md). The section "Golden Rule", the section "Pattern F:
  SyntaxNode Patterns" and entry 6 of the section "Edge Cases" instruct a contributor to test the discriminator kind
  before pattern matching, and entry 6 instructs a contributor to enumerate the two record kinds rather than to test
  `RecordDeclarationSyntax`. S-18-1 and S-18-3 follow that instruction: they add the union kind to the enumerations
  that exist rather than replacing them by a test on the abstract syntax type, and the addition sits inside the
  latest variant block, because the lower Roslyn variant does not declare the kind. State that rule in the document,
  with the variant block as its condition, so that a contributor who meets the same choice takes the same decision.
  `KindCheckOptimizationAnalyzer.cs:833-837` exempts a test on an abstract syntax type from the discriminator rule,
  and no document records that exemption; state it and state that it is an exemption rather than the preferred form.
  Add the union declaration to the table "SyntaxKind to Syntax Types (Common)" of the section "Type Mappings". State that no row is added to the table "DeclarationKind to IDeclaration Types" of the same section and
  none to the multi-kind table of "Pitfall 1", because S-18-1 adds neither a `TypeKind` value nor a declaration kind
  for a union, and because S-16 exposes closedness as a flag and not as a kind.

#### Acceptance criteria

- Every enumeration of type declarations in the five documents lists the union declaration, and none of them lists
  the class, the struct, the interface and the record alone.
- [`kind-check-optimization.md`](../../kind-check-optimization.md) states that a new type-declaration kind is added to
  the existing kind enumerations inside the variant block, states that a test on an abstract syntax type is exempt
  from the discriminator rule, names the analyzer code that implements the exemption, and states that this release
  adds no `TypeKind` value and no declaration kind.
- [`pipeline.md`](../../pipeline.md) states how the aspect instances of one type are ordered, and states the condition
  under which two targets that share a span are ordered by signature instead of throwing.
- [`compilation-model.md`](../../compilation-model.md) states that a declaration may be registered as a builder with no
  injected syntax, and names the two cases in which that happens, which are the namespace and the synthesized members
  of an introduced union.
- [`linker-overview.md`](../../linker-overview.md) and [`linker-architecture.md`](../../linker-architecture.md) state that a
  union declaration carries a case list in the syntactic position where a class carries a primary constructor
  parameter list.
- Every file, member and line reference added by this pull request resolves in the shipped code of the release.
- The pull request description lists the four documents that were reviewed and left unchanged, with the reason for
  each.

#### Not in scope

This story does not touch the documents that S-24 owns, which are [`platform-support.md`](../../platform-support.md),
[`extensibility.md`](../../extensibility.md), [`testing.md`](../../testing.md),
[`compile-time-target-frameworks.md`](../../compile-time-target-frameworks.md),
[`Directory.Packages.md`](../../../../Directory.Packages.md), `Directory.Packages.props`, `NOTES.md`, the two root
`CLAUDE.md` files and the project and member comments that state the previous platform baseline. It does not touch
[`linker-inlining.md`](../../linker-inlining.md), whose section on label renaming S-19 owns, nor
[`updating-roslyn.md`](../../updating-roslyn.md), whose step 10 S-13 rewrites, nor the paragraph of
[`Directory.Packages.md`](../../../../Directory.Packages.md) and the variant property file notes that state the gating
policy, which S-13 owns. It does not edit the conceptual documentation under `../Metalama.Documentation/content`,
which is a separate repository and therefore a separate pull request.

— Claude for @gfraiteur
