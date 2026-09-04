### S-29. C# 15 unions: introducing a union and a case on the attribute form

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Priority: nice to have for 2027.0. It is discretionary under the doctrine of section 2 of
  [`DECISIONS.md`](../DECISIONS.md) and slips to 2027.1 if the release runs short.
- Blocked by: S-18-1, S-18-5
- Findings: none. The requirement was decided after the theme documents were written; the design is
  [`analysis-reports/11-introducing-unions-design.md`](../analysis-reports/11-introducing-unions-design.md).

---

Section 4 of [`DECISIONS.md`](../DECISIONS.md) requires that Metalama 2027.0 support the introduction of a union type
and the introduction of a union case, that is a leg of a union. This is the largest single piece of C# 15 work in the
release, and it has two halves that differ in kind: introducing a whole union is the creation of a new declaration,
while introducing a case into a union that already exists in source is a signature change of a declaration that the
user wrote. This story carries the first half and the second half for a type carrying the union attribute. The
second half for a `union` declaration is story S-30.

#### Context

Introducing a whole union means that the type builder acquires a model for the case list, which the grammar makes
mandatory, and that the introduction pipeline materializes the members that the compiler synthesizes, namely one
public constructor per case and the `Value` property; the pipeline never re-reads the final model from Roslyn, so a
synthesized member that an aspect must see has to exist as a builder. The precedent is not the record materialization
of #1879, which does not generalise because a user may not declare those members at all and there is therefore no
override to serve, but the introduction of a namespace, which registers a builder without an injection. Introducing a
case has the introduction of a parameter into a partial constructor as its precedent, delivered for C# 14 in #1143,
and it depends on a grammar rule that
[`analysis-reports/11-introducing-unions-design.md`](../analysis-reports/11-introducing-unions-design.md) settles:
exactly one part of a partial union carries the case list, a second one is `CS8863`, and none is `CS9370`. The
consequence is that a generated partial part can never add a case to a union declared with the `union` keyword, so
that operation works at build time only, while the same operation on a type carrying the union attribute is ordinary
member introduction whose design-time result is correct. Question Q1 chooses between shipping both forms and
shipping the attribute form alone, and the build-time-only form is story S-30. About half of the work needs no
C# 15 Roslyn member and can proceed before S-13.

Two closed issues bound this work. #1622 reported that a constructor introduced into an introduced type was missing
from the design-time generated source, because the transformation degraded its observability when it replaced the
implicit constructor; the per-case constructors of a union replace the implicit parameterless constructor in the same
way, so the design-time claim of this story is verified against that fix rather than assumed. #1869 added `IsPartial`
to the named type builder through pull request #1878, so the case list model extends a member set that changed in
this release.

#### Scope

- Add a model for the case list to the named type builder, its data and the introduced type, with validation.
- Add a transformation shape that registers a builder into the code model without injecting syntax, modelled on the
  introduction of a namespace, and materialize the synthesized `Value` property and the case constructors through it.
- Prototype that step first, because whether a member builder with no injected member survives the linker injection
  registry was not verified and is the risk that decides whether the step is one day or three.
- Add the advice surface for introducing a union and for introducing a case, reporting that the operation is not
  supported by the current compiler in every path that would need C# 15 syntax, so that the surface, the
  documentation and the eligibility tests can be reviewed before the compiler exists.
- Deliver the case introduction for a type carrying the union attribute, which is member introduction and whose
  design-time result is correct.
- Emit the union declaration in the type introduction transformation and add the union arms to the injection rewriter
  and to the design-time generator, inside the variant block.
- Extend the eligibility rules so that each states which of the two union forms it tests, because
  `ITypeSymbol.IsUnion` is true for both while the restrictions of a union declaration apply to one.
- Write the aspect tests with their committed baselines, including the design-time scenarios.
- State, in the pull request description, which pages of `metalama/Metalama.Documentation` must follow, including the
  note that an added case is a build-time-only change.

#### Acceptance criteria

- An aspect can introduce a union type, and an aspect can read the members that the compiler synthesizes for it.
- An aspect can add a case to a type carrying the union attribute, and the editor and the build agree about the
  result.
- Every eligibility rule of the story names the union form it tests, and none rejects advice that is legal on a type
  carrying the union attribute.
- The aspect tests and their expected output are committed, and the pull request description explains each difference
  from the previous baseline.

#### Not in scope

This story does not introduce a closed class, which is S-28. It does not introduce structs, records, enums or
delegates, which are #869, #867, #866 and #865 and stay open. It does not add a case to a `union` declaration, which
is S-30.

— Claude for @gfraiteur
