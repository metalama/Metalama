# Open questions of the 2027.0 C# 15 and .NET 11 work

This document lists what is not decided. It is the companion of [`DECISIONS.md`](DECISIONS.md), which lists what
is. A question leaves this document when it is answered there.

Each entry states the question, what is already known, the options with their consequences, who or what settles it,
and what depends on it. An entry marked as blocking names the work that cannot start until it is answered. The
identifiers are stable, so a gap in the numbering means that a question has been answered and removed rather than
that one is missing.

## Product decisions

### Q1. Does case introduction into a `union` declaration ship, given that it works at build time only?

Blocking, for one story.

Section 4 of [`DECISIONS.md`](DECISIONS.md) requires the introduction of a union case and establishes that the
answer differs by the form of the union. For a type carrying `UnionAttribute`, adding a case is the introduction of
a constructor, a generated partial part can express it, and the editor and the build agree. For a type declared
with the `union` keyword, the case list lives in exactly one part and a generated part may not carry one, so the
operation requires rewriting the part the user wrote. The build-time half is small, because the linker already
rewrites a type parameter list in the same field and method for partial constructor parameter introduction. The
design-time half cannot be expressed at all: every route from a generated part is closed by a compiler rule.

- Option A, ship both forms. The attribute form has a correct design-time result. The declaration form is
  build-time only and needs a design-time diagnostic saying that the editor cannot show the added case. The editor
  and the build then disagree about conversions and about switch exhaustiveness, which the diagnostic reports but
  does not repair.
- Option B, ship the attribute form only. Nothing diverges, and an aspect cannot add a case to a union that a user
  wrote with the concise syntax.

The analysis recommends Option A, and states that if only one form fits the release it should be the attribute
form. Settled by the product owner.

### Q2. Does the lower Roslyn variant report the divergence, or stay silent?

Blocking, for one story, and it applies to every C# 15 reader.

Section 6 of [`DECISIONS.md`](DECISIONS.md) records that the public application programming interface assembly is
not built per Roslyn version, while the engine is. On the hosts that the `Roslyn.5.0.0` variant serves, `IsUnion`
and `IsClosed` therefore report false, an aspect sees a union as an ordinary struct, and a `closed` modifier that
an aspect emits at build time is absent at design time. Nothing reports the difference. Section 7 records that
those hosts probably include a supported Visual Studio as well as Rider and the Visual Studio Code C# Dev Kit.

- Option A, stay silent. This is the behaviour that follows from doing nothing. The editor and the command line
  disagree and the user has no indication of it, which is the failure mode that the platform doctrine exists to
  prevent.
- Option B, report. The draft in `analysis-reports/12-csharp15-api-drafts.md` proposes a design-time warning
  reported once per project, with an opt-out, from the diagnostic analyzer. It fires in an editor whose user can
  act on it only by changing the integrated development environment. The reporting mechanism has recently been
  implemented, so this is a question of whether to report rather than of how.
- Option C, remove the members on the lower variant rather than answering false. Both options above take as given
  that the public members exist in every host and answer false where the engine cannot answer, which follows from
  the public assembly not being built per Roslyn version. Whether that assembly could carry a variant-specific
  part has not been examined.

The draft recommends Option B. Settled by the product owner.

### Q3. Is the severity and the opt-out of each new design-time diagnostic right?

Not blocking. Two diagnostics are drafted: the one of Q2, and the one that reports that the editor cannot show a
case added to a `union` declaration under Option A of Q1. Both are design-time only, both fire in situations the
user cannot repair from the editor, and both therefore need a severity and an opt-out chosen deliberately. The
drafts propose a warning with an opt-out for the first. Settled when the stories are written.

## Measurements that the calendar settles

### Q5. Which Roslyn version and which private runtime does the November 2026 Visual Studio baseline carry?

Blocking, for the release, and already tracked as checklist item 1 of
[`platform-support.md`](../platform-support.md).

Section 7 of [`DECISIONS.md`](DECISIONS.md) derives Roslyn 5.12 from the publication cadence: nuget.org serves 5.0,
5.3, 5.6 and 5.9 and nothing else in the 5 generation, and the `main` branch reads minor version 12. The derivation
is an inference from a cadence and the checklist requires a measurement against a real installation after
2026-11-10. The private runtime must be measured at the same time, because the embedded Core flavour of the payload
depends on it.

### Q6. Which Roslyn version does a current Rider present, and the Visual Studio Code C# Dev Kit?

Blocking, for the variant set, and already tracked as checklist item 2 of
[`platform-support.md`](../platform-support.md).

Rider was measured at Roslyn 5.0.0 on 2026-09-01 and the C# Dev Kit was not measured. Rider builds its own Roslyn
rather than taking a published package, so it could in principle present a version that no Visual Studio presents.
The question matters only if Rider relies on a Roslyn version that no supported Visual Studio uses on 2027-01-31
and whose non-experimental features require an interface that an older Roslyn does not have. Section 7 of
[`DECISIONS.md`](DECISIONS.md) makes that less likely, because Roslyn 5.0 appears to serve a serviced Visual Studio
18.0 as well, in which case the lower variant is required whatever Rider presents. Due at the release candidate, on
2026-11-20.

## Technical questions that an analysis settles

### Q8. How is a closed type from a referenced assembly handled?

Not blocking. Section 11 of [`DECISIONS.md`](DECISIONS.md) records that the existing derived type index already
gives the complete set of derived types for a closed type of the current compilation, because the language requires
every subtype to be in the same module. A closed type that comes from a referenced assembly is the case the index
does not answer, and the drafts leave it open.

## Corrections that need approval

### Q10. Three statements of `platform-support.md` follow from the analysis and are not applied

Not blocking, and the corrections are described in sections 7 and 9 of [`DECISIONS.md`](DECISIONS.md). The document
is the doctrine and belongs to the product owner, so they are recorded rather than applied, and story S-24 carries
them once they are approved.

The Roslyn API section says that the November 2026 long-term servicing channel baseline carries "Roslyn 5.11 or
thereabouts", and the variant table offers a row for a measured version of "5.10 or above". Neither 5.10 nor 5.11
is published, so both should name 5.12, subject to the measurement of Q5.

The document says that the Visual Studio 2026 long-term servicing channel opens in November 2026 and is therefore
the first pinnable version. The servicing activity on the `release/dev18.0` branch of `dotnet/roslyn` suggests that
a serviced Visual Studio 18.0 exists already.

The document says that the `net10.0` toolset rolls forward to .NET 11. `RollForward=Major` selects .NET 11 only
when no .NET 10 runtime is installed, so the sentence should say when the roll-forward happens.
