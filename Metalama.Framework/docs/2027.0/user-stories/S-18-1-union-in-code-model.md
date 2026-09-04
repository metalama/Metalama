### S-18-1. C# 15 unions: code model

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Parent story: S-18, of which this is a sub-story
- Size: M
- Blocked by: S-13, S-15
- Findings: [CM-1](../03-code-model-unions-closed.md), [CM-2](../03-code-model-unions-closed.md),
  [CM-6](../03-code-model-unions-closed.md), [CM-7](../03-code-model-unions-closed.md),
  [LK-3](../04-linker-and-advice.md), [DT-1](../05-design-time-workspaces-linqpad.md),
  [DT-6](../05-design-time-workspaces-linqpad.md)

---

A C# 15 union is indistinguishable from a struct in the public code model: Roslyn reports `TypeKind.Struct`,
`IsRecord` is false, and nothing tells an aspect that instance fields, auto-properties and field-like events are
forbidden in it. Separately, the syntax visitors of the engine inherit the Roslyn dispatch, which routes a union
declaration to `VisitUnionDeclaration`, and no visitor overrides it, so a union declaration is never seen by the
visitors that classify, hash and rewrite type declarations. Third, the lists that enumerate the kinds of a type
declaration do not name the union kind, so a partial union reports `IsPartial` false. This story is the surface that
the other sub-stories of S-18 consume.

#### Context

The code model must report the union without a new `TypeKind` value. The precedent of the record kinds settles the
question: `TypeKind.RecordClass` and `TypeKind.RecordStruct` are already obsolete with the message that names
`TypeKind.Class` and `INamedType.IsRecord`, and a new value would need an arm in each of the seventeen switches over
that enumeration, twelve of which throw in the default arm. The shape of the members is decided when the story is
implemented, per section 7 of [`DECISIONS.md`](../DECISIONS.md); a draft that follows the precedent of `IsRecord` is in
[`analysis-reports/12-csharp15-api-drafts.md`](../analysis-reports/12-csharp15-api-drafts.md) and is illustrative only.
Two constraints hold for every member added by this story. The reads name Roslyn members that the lower variant does
not have, so they follow the gating of S-13. And `ITypeSymbol.IsUnion` is true both for a `union` declaration and
for a type carrying `System.Runtime.CompilerServices.UnionAttribute`, while the member restrictions apply to the
first form only, so the code model must let a consumer tell the two apart, which is question Q9.

The kind lists are the third part of this story, and four themes reported the same five places.
`SyntaxKindExtensions.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` enumerates exactly
the class, struct, interface, record and record struct kinds, `IsBaseTypeDeclaration` derives from it at `:41`, and
the same enumeration is written by hand three more times in
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs`, at `:33-34`, `:61-62` and
`:116-117`. `SourceNamedTypeImpl.IsPartial` tests `SyntaxKind.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:344`, and the linker reads
the same predicates in `Linking/SymbolExtensions.GetDeclarationFlags`. A `partial union` therefore reports
`IsPartial` false, so `LAMA0048` is reported although the type is partial, the design-time generator never produces
the partial file, the code fix then adds a second `partial` modifier, a suppression never reaches a diagnostic
located on a union header, and the linker consumers of the same predicates fall through.

The union kind is added to those existing lists, and the kind test is not replaced by a test on the abstract syntax
type. The reason is that the code base routes declarations and nodes by kind, which is the convention that
`KindCheckOptimizationAnalyzer` of #1307 enforces, and a type test would depart from it in five widely read
predicates. The same lists omit the interface kind and the extension block kind in several places today, and each
omission is corrected in the same way, by adding the kind.

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
- Add the union kind to `SyntaxKindExtensions.IsTypeDeclaration`, to the three hand-written enumerations of
  `SyntaxExtensions.FindMemberDeclarationOrNull`, `FindSymbolDeclaringNode` and `GetDeclaringType`, and to
  `Linking/SymbolExtensions.GetDeclarationFlags`, and add the interface kind and the extension block kind where they
  are missing as well.
- Review every consumer of those predicates, and treat with care the two sites where the parameter list of a union is
  a case list and not a parameter list, which are `ImplicitLastOverrideReferenceInliner` and
  `LinkerLateTransformationRegistry`.
- Keep the record-only kind lists as they are, because they serve the record-synthesized-member logic and not the
  general question of whether a node is a type declaration.
- Add the unit tests that pin `IsPartial` and the suppression path for a union, for an interface and for an extension
  block, which are the cases that are wrong today.
- Decide, per D-3, whether the lower Roslyn variant reports a diagnostic when it meets a union it cannot represent,
  and implement the chosen behaviour.

#### Acceptance criteria

- An aspect can tell a union from an ordinary struct, can enumerate its case types, and can tell the two authoring
  forms apart.
- The same code model members exist on the lower Roslyn variant and report the value of an ordinary struct there, and
  the behaviour chosen for D-3 is covered by a test.
- Every visitor of the CM-7 inventory sees a union declaration, and the guard fails if a new one is added without it.
- `IsPartial` is true for a partial union, for a partial interface and for every partial type declaration whose kind
  the predicate now admits, and the design-time generator produces the partial file for it.
- A suppression located on the header of a union, of an interface or of an extension block is applied.
- `KindCheckOptimizationAnalyzer` reports nothing on the edited sites.
- Both Roslyn variants build, and no switch over `TypeKind` gained an arm.

#### Not in scope

This story does not introduce a union, which is S-29, and it does not add the eligibility rules of advice applied to
a union, which are S-18-3.

— Claude for @gfraiteur
