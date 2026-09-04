### S-03. Recognise any type declaration by a type test instead of by an enumerated syntax kind

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: nothing
- Findings: [CM-2](../03-code-model-unions-closed.md), [CM-6](../03-code-model-unions-closed.md),
  [LK-3](../04-linker-and-advice.md), [DT-1](../05-design-time-workspaces-linqpad.md),
  [DT-6](../05-design-time-workspaces-linqpad.md)

---

`SyntaxKindExtensions.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` enumerates exactly
the class, struct, interface, record and record struct kinds, `IsBaseTypeDeclaration` derives from it at `:41`, and
the same enumeration is written by hand three more times in
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs`, at `:33-34`, `:61-62` and
`:116-117`. `SourceNamedTypeImpl.IsPartial` tests `SyntaxKind.IsTypeDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:344`. Four themes reported
the same five places, and every consequence they describe follows from them.

#### Context

A `partial union` reports `IsPartial` false, so `LAMA0048` is reported although the type is partial, the design-time
generator never produces the partial file, the code fix then adds a second `partial` modifier, a suppression never
reaches a diagnostic located on a union header, and the linker consumers of the same predicates fall through. The
remedy is not union-specific: interfaces and extension blocks are already missing from several of these lists today,
so this is a correctness change that also admits unions later without naming a member that the Roslyn 5.0 variant
does not have. This is the largest piece of union plumbing that can be delivered before the Roslyn gate.

#### Scope

- Replace the enumerated kinds by a test on the abstract syntax type where the intent is a declaration that can
  contain members, in `SyntaxKindExtensions.IsTypeDeclaration`, in `SyntaxExtensions.FindMemberDeclarationOrNull`,
  `FindSymbolDeclaringNode` and `GetDeclaringType`, and in `Linking/SymbolExtensions.GetDeclarationFlags`.
- Choose deliberately between the narrow predicate that the documentation of `SyntaxKindExtensions` promises and the
  broad Roslyn helper `SyntaxFacts.IsTypeDeclaration`, which also matches delegates, enums and extension blocks and
  would therefore change shipped behaviour, and record the choice in the documentation comment.
- Review every consumer of the two predicates, and treat with care the two sites where the parameter list of a union
  is a case list and not a parameter list, which are `ImplicitLastOverrideReferenceInliner` and
  `LinkerLateTransformationRegistry`.
- Keep the record-only kind lists as they are, because they serve the record-synthesized-member logic and not the
  general question of whether a node is a type declaration.
- Keep the convention that `KindCheckOptimizationAnalyzer` of #1307 enforces, which exempts a test on an abstract
  syntax type.
- Add the unit tests that pin `IsPartial` and the suppression path for an interface and for an extension block, which
  are the cases that are wrong today.

#### Acceptance criteria

- No enumeration of concrete type-declaration kinds remains in `SyntaxKindExtensions` or in `SyntaxExtensions`.
- `IsPartial` is true for a partial interface and for every partial type declaration whose kind the predicate now
  admits, and the design-time generator produces the partial file for it.
- A suppression located on the header of an interface or of an extension block is applied.
- `KindCheckOptimizationAnalyzer` reports nothing on the rewritten sites, and both Roslyn variants build.

#### Not in scope

This story names no C# 15 syntax kind. It compiles unchanged for both variants and adds no variant branch.

— Claude for @gfraiteur
