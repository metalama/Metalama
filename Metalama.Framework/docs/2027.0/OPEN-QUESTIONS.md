# Open questions of the 2027.0 C# 15 and .NET 11 work

This document lists what is not decided. It is the companion of [`DECISIONS.md`](DECISIONS.md), which lists what
is. A question leaves this document when it is answered there.

Each entry states the question, what is already known, the options with their consequences, who or what settles it,
and what depends on it. An entry marked as blocking names the work that cannot start until it is answered. The
identifiers are stable, so a gap in the numbering means that a question has been answered and removed rather than
that one is missing.

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
