### S-28. C# 15 closed classes: introducing

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Priority: nice to have for 2027.0. It is discretionary under the doctrine of section 12 of
  [`DECISIONS.md`](../DECISIONS.md) and slips to 2027.1 if the release runs short.
- Blocked by: S-13, S-16
- Findings: [CM-4](../03-code-model-unions-closed.md), [LK-5](../04-linker-and-advice.md)

---

An aspect cannot introduce a closed class, because no builder property expresses the modifier and
`ModifierHelper.GetTypeSyntaxModifierList` emits neither it nor `partial` for a type. Section 5f of
[`DECISIONS.md`](../DECISIONS.md) puts this writer in scope for 2027.0.

#### Context

A closed class is an ordinary class with one more modifier, which is why the writer is sized M and why every part of
it is already identified. The reader that this story consumes, which reports whether a named type is closed, is
delivered by S-16. Section 5f records that the decision raises the stake of question Q2 rather than depending on it:
an aspect that introduces a closed class emits the modifier at build time and nothing at design time on the hosts
that the lower Roslyn variant serves, so the editor and the build disagree about the exhaustiveness of the hierarchy.

#### Scope

- Add the settable closed property to `INamedTypeBuilder`, with the getter on `INamedType` delivered by S-16.
- Validate in the setter that the type kind is a class and that the type is neither sealed nor static, which are the
  restrictions the language states.
- Store the value in `NamedTypeBuilderData` and expose it on `IntroducedNamedType`.
- Emit the closed keyword in `GetTypeSyntaxModifierList` and suppress `abstract` there, while `IsAbstract` keeps
  reporting true.
- Emit the modifier before `partial`, because `partial` must sit immediately before the type keyword.
- Gate the reference to `SyntaxKind.ClosedKeyword` on the latest Roslyn variant, per S-13.

#### Acceptance criteria

- An aspect introduces a closed class whose generated code compiles.
- The generated modifier list reads `closed partial class` and never `abstract closed class`.
- Introducing a closed struct, a sealed closed class or a static closed class is refused with a diagnostic that names
  the language restriction.
- Both Roslyn variants build.

— Claude for @gfraiteur
