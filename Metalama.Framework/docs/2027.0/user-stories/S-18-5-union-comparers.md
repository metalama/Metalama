### S-18-5. C# 15 unions: declaration comparers

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-18-1
- Findings: none. Both defects were found by
  [`analysis-reports/13-union-comparers.md`](../analysis-reports/13-union-comparers.md) and are recorded in section 10 of
  [`DECISIONS.md`](../DECISIONS.md), after the theme documents were written.

---

`AspectInstanceComparer.Compare` in
`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs:198-269` orders aspect
instances by the position of the primary declaration syntax of their target, and has one special case at `:250-265`
for two implicitly declared methods of a record; anything else reaches the `AssertionFailedException` at `:267`. A
union falls outside that special case in three ways, so an aspect that targets more than one synthesized member of a
union crashes. Separately, `DeclarationEqualityComparer` reimplements the conversion rules and enumerates implicit
conversion operators only, so it does not know the conversions that Roslyn grants a union.

#### Context

The analysis of the comparers was asked for as a risk assessment of union introduction and refuted most of the
hazards, which is the useful part of its answer: the constructor signature comparer compares parameter types, no
comparer keys a constructor on its name and parameter count, the missing declaring syntax of a synthesized
constructor reaches no member comparer, and the determinism fix that #1879 had to make for records is not needed a
second time. Two defects remain. The first affects the reading half and is therefore not conditional on any open
question. The second is a prerequisite of S-29 rather than a follow-up of it.

#### Scope

- Generalise the record special case of `AspectInstanceComparer.Compare` to any implicitly declared members that
  share a span, rather than adding a union arm beside the record one, and remove the assertion that requires the
  declaring type to be a record.
- Cover the three ways in which a union falls outside the present special case: the synthesized `Value` is a property
  and not a method, the synthesized case constructors are constructors and there may be several of them carrying the
  span of the union declaration, and the declaring type is not a record.
- Teach the conversion reimplementation of `DeclarationEqualityComparer` the conversions that the language grants a
  union, so that an introduced union accepts the implicit conversion from its case types.
- Add a union case to `ComparerAgreesWithRoslynTests` and to `DeclarationComparerTests`, which are the two tests that
  would have caught the second defect.

#### Acceptance criteria

- An aspect that targets several synthesized members of a union orders them deterministically and does not throw.
- The comparer agrees with Roslyn about the conversions of a union, verified by the test that compares the two.
- The record path is unchanged in behaviour, verified by the existing record tests.

— Claude for @gfraiteur
