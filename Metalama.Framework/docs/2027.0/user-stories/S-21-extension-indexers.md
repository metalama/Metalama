### S-21. C# 15 extension indexers: advising and introducing

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-15
- Findings: [LK-6](../04-linker-and-advice.md), [LK-7](../04-linker-and-advice.md),
  [UT-16](../06-user-tfm-patterns-tests-docs.md)

---

`Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:1406` rejects the introduction of an
indexer into an extension block by design, a restriction that #1587 recorded in the documentation rather than
lifted. C# 15 adds extension indexers to the language, so that deliberate restriction becomes a gap. This story is
self-contained and has no dependency on the union work, so it can run in parallel with S-18.

#### Context

Section 11 of [`DECISIONS.md`](../DECISIONS.md) records that extension indexers need no application programming
interface change in order to be overridden, and that introducing one needs the removal of a single validation call
plus an eligibility rule requiring the named receiver that an extension block with an indexer must declare. The
language adds three further restrictions that the eligibility rules must carry: no `init` accessor, and none of the
modifiers that an extension member may not have. The overriding half follows the extension property path and is
expected to be correct when the override is inlineable; the non-inlined case is bounded by the pre-existing
`LAMA0699` of the open issue #937, which this story states rather than fixes. Every test needs C# 15 as a requestable
language version, which is why the story waits for S-15.

The introduction path that this story extends was delivered for C# 14 by the closed issues #1035, which added
advising on extension members, and #1160, which added introduction into an existing extension block. The indexer is
the one extension member kind that #1160 left rejected, which #1587 then recorded in the documentation instead of
lifting.

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

This story does not remove the `LAMA0699` limitation on non-inlined indexer overrides, which is #937.

— Claude for @gfraiteur
