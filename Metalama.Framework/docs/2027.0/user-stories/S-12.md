### S-12. Expose the union in the public code model and add the syntax visitor overrides

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-02, S-03, S-11
- Findings: [CM-1](../03-code-model-unions-closed.md), [CM-7](../03-code-model-unions-closed.md)

---

A C# 15 union is indistinguishable from a struct in the public code model: Roslyn reports `TypeKind.Struct`,
`IsRecord` is false, and nothing tells an aspect that instance fields, auto-properties and field-like events are
forbidden in it. Separately, the syntax visitors of the engine inherit the Roslyn dispatch, which routes a union
declaration to `VisitUnionDeclaration`, and no visitor overrides it, so a union declaration is never seen by the
visitors that classify, hash and rewrite type declarations. This story is the surface that six later stories consume.

#### Context

The code model must report the union without a new `TypeKind` value. The precedent of the record kinds settles the
question: `TypeKind.RecordClass` and `TypeKind.RecordStruct` are already obsolete with the message that names
`TypeKind.Class` and `INamedType.IsRecord`, and a new value would need an arm in each of the seventeen switches over
that enumeration, twelve of which throw in the default arm. The shape of the members is decided when the story is
implemented, per section 7 of [`DECISIONS.md`](../DECISIONS.md); a draft that follows the precedent of `IsRecord` is in
[`analysis-reports/12-csharp15-api-drafts.md`](../analysis-reports/12-csharp15-api-drafts.md) and is illustrative only.
Two constraints hold for every member added by this story. The reads name Roslyn members that the lower variant does
not have, so they follow the gating of S-02. And `ITypeSymbol.IsUnion` is true both for a `union` declaration and
for a type carrying `System.Runtime.CompilerServices.UnionAttribute`, while the member restrictions apply to the
first form only, so the code model must let a consumer tell the two apart, which is question Q9.

#### Scope

- Expose on `INamedType` whether a named type is a union and what its case types are, following the precedent of
  `IsRecord`, and document the union in the summary of that interface.
- Do not add a `TypeKind` value, and record in the story that the reason is the obsolete record kinds and the
  seventeen switches.
- Let a consumer distinguish a `union` declaration from a type carrying the union attribute, because the language
  restrictions apply to the declaration form only.
- Add the `VisitUnionDeclaration` overrides that a type test cannot replace, because the Roslyn visitor dispatches a
  virtual method and a numeric kind cannot override one, in the visitors inventoried by CM-7, and share the struct
  helper only where it does not read the parameter list as a primary constructor parameter list.
- Add a guard, such as a test over the visitor inventory, that a future type-declaration kind cannot be omitted from
  the same set of visitors without a failure.
- Decide, per D-3, whether the lower Roslyn variant reports a diagnostic when it meets a union it cannot represent,
  and implement the chosen behaviour.

#### Acceptance criteria

- An aspect can tell a union from an ordinary struct, can enumerate its case types, and can tell the two authoring
  forms apart.
- The same code model members exist on the lower Roslyn variant and report the value of an ordinary struct there, and
  the behaviour chosen for D-3 is covered by a test.
- Every visitor of the CM-7 inventory sees a union declaration, and the guard fails if a new one is added without it.
- Both Roslyn variants build, and no switch over `TypeKind` gained an arm.

#### Not in scope

This story does not introduce a union, which is S-17, and it does not add the eligibility rules of advice applied to
a union, which are S-14.

— Claude for @gfraiteur
