# Open questions of the 2027.0 C# 15 and .NET 11 work

This document lists what is not decided. It is the companion of [`DECISIONS.md`](DECISIONS.md), which lists what is.
A question leaves this document when it is answered there, and the entry then says where the answer is.

Each entry states the question, what is already known, the options with their consequences, who or what settles it,
and what depends on it. An entry marked as blocking names the work that cannot start until it is answered.

## Product decisions

### Q1. Does leg introduction into a `union` declaration ship, given that it works at build time only?

Blocking, for one story.

Section 5c requires the introduction of a union leg. Sections 5d and 5e establish that the answer differs by the
form of the union. For a type carrying `UnionAttribute`, adding a leg is the introduction of a constructor, a
generated partial part can express it, and the editor and the build agree. For a type declared with the `union`
keyword, the case list lives in exactly one part and a generated part may not carry one, so the operation requires
rewriting the part the user wrote. The build-time half is small, because the linker already rewrites a type
parameter list in the same field and method for partial constructor parameter introduction. The design-time half
cannot be expressed at all: every route from a generated part is closed by a compiler rule.

- Option A, ship both forms. The attribute form has a correct design-time result. The declaration form is
  build-time only and needs a design-time diagnostic saying that the editor cannot show the added case. The editor
  and the build then disagree about conversions and about switch exhaustiveness, which the diagnostic reports but
  does not repair.
- Option B, ship the attribute form only. Nothing diverges, and an aspect cannot add a case to a union that a user
  wrote with the concise syntax.

The analysis recommends Option A, in that order, and states that if only one form fits the release it should be the
attribute form. Settled by the product owner.

### Q2. Does the lower Roslyn variant report the divergence, or stay silent?

Blocking, for one story, and it applies to every C# 15 reader.

Section 2b records that the public application programming interface assembly is not built per Roslyn version,
while the engine is. On the hosts that the `Roslyn.5.0.0` variant serves, which are Rider and the Visual Studio
Code C# Dev Kit, `IsUnion` and `IsClosed` therefore report false, an aspect sees a union as an ordinary struct, and
a `closed` modifier that an aspect emits at build time is absent at design time. Nothing reports the difference.

- Option A, stay silent. This is the behaviour that follows from doing nothing. The editor and the command line
  disagree and the user has no indication of it, which is the failure mode that the platform doctrine exists to
  prevent.
- Option B, report. The draft in `analysis-reports/12-csharp15-api-drafts.md` proposes a design-time warning
  reported once per project, with an opt-out, from the diagnostic analyzer. It fires in an editor whose user can
  act on it only by changing the integrated development environment.

The draft recommends Option B. One measurement could change the answer, which is Q6. Settled by the product owner.

### Q3. Is the severity and the opt-out of each new design-time diagnostic right?

Not blocking. Two diagnostics are drafted: the one of Q2, and the one that reports that the editor cannot show a
case added to a `union` declaration under Option A of Q1. Both are design-time only, both fire in situations the
user cannot repair from the editor, and both therefore need a severity and an opt-out chosen deliberately. The
drafts propose a warning with an opt-out for the first. Settled when the stories are written.

### Q4. Does an aspect introduce a closed class?

Answered on 2026-09-04 in section 5f of [`DECISIONS.md`](DECISIONS.md): yes. A closed class is an ordinary class
with one more modifier, and the analysis had already found every part of the writer identified and cheap. The
answer raises the stake of Q2, because an aspect that introduces a closed class emits the modifier at build time
and nothing at design time on the hosts that the lower Roslyn variant serves.

## Measurements that the calendar settles

### Q5. Which Roslyn version and which private runtime does the November 2026 Visual Studio baseline carry?

Blocking, for the release, and already tracked as checklist item 1 of
[`platform-support.md`](../platform-support.md).

Section 8 derives Roslyn 5.12 from the publication cadence: nuget.org serves 5.0, 5.3, 5.6 and 5.9 and nothing
else in the 5 generation, and the `main` branch reads minor version 12. The derivation is an inference from a
cadence and the checklist requires a measurement against a real installation after 2026-11-10. The private runtime
must be measured at the same time, because the embedded Core flavour of the payload depends on it.

### Q6. Which Roslyn version does a current Rider present, and the Visual Studio Code C# Dev Kit?

Blocking, for the variant set, and already tracked as checklist item 2 of
[`platform-support.md`](../platform-support.md).

Rider was measured at Roslyn 5.0.0 on 2026-09-01 and the C# Dev Kit was not measured. Rider builds its own Roslyn
rather than taking a published package, so it could in principle present a version that no Visual Studio presents.
The measurement decides whether the `Roslyn.5.0.0` variant is still needed, and therefore whether Q2 exists at all:
if every supported design-time host presented Roslyn 5.12, the divergence would disappear with the lower variant.
Due at the release candidate, on 2026-11-20.

## Technical questions that an analysis settles

### Q7. What is the impact of union and leg introduction on the comparers?

Answered on 2026-09-04 in section 10 of [`DECISIONS.md`](DECISIONS.md), from the analysis in
`analysis-reports/13-union-comparers.md`. Most hazards were refuted. Two defects were found: the aspect instance
ordering throws when two targets share a span, which a union causes and which affects the reading half, and the
conversion reimplementation of `DeclarationEqualityComparer` does not know the union conversions. The original
question follows.

Raised by the product owner on 2026-09-04. The hazards to confirm or refute
are that a synthesized case constructor has no declaring syntax, that two case constructors differ only in their
single parameter type, that introducing a leg changes the constructor set of an existing type and therefore the
collections cached against it, that a builder must be equated with the symbol it becomes for a type whose members
the compiler synthesizes, and that the order of synthesized members must be deterministic, which is a defect that
pull request #1879 had to fix for records. Every one of those hazards was refuted except the ordering of aspect
instances, which fails for a different reason than the one anticipated.

### Q8. How is a closed type from a referenced assembly handled?

Not blocking. Section 9 records that the existing derived type index already gives the complete set of derived
types for a closed type of the current compilation, because the language requires every subtype to be in the same
module. A closed type that comes from a referenced assembly is the case the index does not answer, and the drafts
leave it open.

### Q9. Which eligibility rules must distinguish the two forms of union?

Not blocking, and it is a trap rather than a question. `ITypeSymbol.IsUnion` is true both for a `union` declaration
and for a type carrying the attribute, while the restrictions of a union declaration apply only to the first. A
rule keyed on `IsUnion` alone would reject advice that is legal on the second. Every rule written for the union
work must state which of the two it tests.

## Corrections that need approval

### Q10. Two statements of `platform-support.md` follow from section 8 and are not applied

Not blocking. The Roslyn API section says that the November 2026 long-term servicing channel baseline carries
"Roslyn 5.11 or thereabouts", and the variant table offers a row for a measured version of "5.10 or above". Neither
5.10 nor 5.11 is published, so both should name 5.12. The document is the doctrine and belongs to the product
owner, so the corrections are recorded rather than applied.

### Q11. The roll-forward statement of `platform-support.md` overstates the mechanism

Not blocking. The document says that the `net10.0` toolset rolls forward to .NET 11. The analysis of the .NET 11
interfaces found that `RollForward=Major` selects .NET 11 only when no .NET 10 runtime is installed, so the
sentence should say when the roll-forward happens.

## Coverage gaps found by the completeness review

The synthesis ended with a review that asked what the analysis had missed. Twelve gaps were reported and are
recorded here rather than silently closed. Each names where to look. None of them invalidates a finding or a story;
they are places the analysis did not reach.

### Q12. Static members in interfaces has no story

It is one of the six features gated on `LanguageVersion.CSharp15`, and the only one that no story delivers. The
Roslyn feature status row describes it as non-virtual static interface members on runtimes without default
interface implementation support, which is why it matters here: the aspect test project has a `net48` leg. Either a
story delivers it, following the test directory convention that the C# 15 language version story establishes, or
the analysis records why none is needed.

### Q13. Three neighbouring repositories are absent from the analysis

`Metalama.Vsx` appears nowhere, although two package pinning rules of `Directory.Packages.md` are derived from the
lowest installed version of it. `Metalama.Samples` appears nowhere, although the samples compile against the
shipped packages and lose the `net8.0` and `net9.0` target frameworks with this release. `Metalama.Documentation`
has no story, and two stories defer their user-facing documentation to a page list that no story writes.

### Q14. Subsystems that no report examined

`Metalama.Framework.EditorExtensions` is the only top-level directory of the framework sources that appears in no
document, and it is a shipped assembly compiled once for every Roslyn variant against the minimum Roslyn version.
Seven engine subsystems are likewise absent: syntax serialization, aspect ordering, hierarchical options,
additional outputs, observers, queries and reflection mocks. Syntax serialization is the one that deserves a
decision rather than a note, because it holds one serializer per supported type and turns a compile-time value into
run-time syntax, which is a question for a union value.

Host process detection is also unexamined, and its two copies have already diverged on the C# Dev Kit, which is one
of the design-time hosts of the baseline.

### Q15. A third option for the divergence of Q2 was never considered

Q2 offers two options, silence and a diagnostic. Both take as given that the public members exist in every host and
answer false where the engine cannot answer, which follows from the public assembly not being built per Roslyn
version. Whether that assembly could carry a variant-specific part, or whether the members could be absent rather
than false on the lower variant, was never examined.

### Q16. The Metalama.Compiler story may understate its own scope

The story that moves `Metalama.Compiler` to the stable Roslyn assumes that rebasing is the whole of the work. That
repository is a Roslyn fork and was not cloned for this analysis, so the assumption is untested.

### Q17. The internal architecture documents are outside the documentation story

Nine documents of `Metalama.Framework/docs` are named nowhere, among them the compilation model, the pipeline, the
three linker documents and the design-time memory rules. Several stories change what they describe.
