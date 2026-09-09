### S-28. C# 15 closed classes: introducing

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Priority: nice to have for 2027.0. It is discretionary under the doctrine of section 2 of
  [`DECISIONS.md`](../DECISIONS.md) and slips to 2027.1 if the release runs short.
- Blocked by: S-13, S-16
- Findings: [CM-4](../03-code-model-unions-closed.md), [LK-5](../04-linker-and-advice.md)

---

An aspect cannot introduce a closed class, because no builder property expresses the modifier and
`ModifierHelper.GetTypeSyntaxModifierList` emits neither it nor `partial` for a type. Section 4 of
[`DECISIONS.md`](../DECISIONS.md) puts this writer in scope for 2027.0.

#### Context

A closed class is an ordinary class with one more modifier, which is why the writer is sized M and why every part of
it is already identified. The reader that this story consumes, which reports whether a named type is closed, is
delivered by S-16. On the hosts that the lower Roslyn variant serves, the writer refuses the modifier instead of
ignoring it: the setter throws an `InvalidOperationException`, so an aspect never silently obtains an ordinary
abstract class where it requested a closed class. Section 6 of [`DECISIONS.md`](../DECISIONS.md) lets the reader
report false on such a host, and the writer differs from the reader because a request that is ignored changes the
generated code.

#### Scope

- Add the settable closed property to `INamedTypeBuilder`, with the getter on `INamedType` delivered by S-16.
- Validate in the setter that the type kind is a class and that the type is neither sealed nor static, which are the
  restrictions the language states.
- Store the value in `NamedTypeBuilderData` and expose it on `IntroducedNamedType`.
- Emit the closed keyword in `GetTypeSyntaxModifierList` and suppress `abstract` there, while `IsAbstract` keeps
  reporting true.
- Emit the modifier before `partial`, because `partial` must sit immediately before the type keyword.
- Gate the reference to `SyntaxKind.ClosedKeyword` on the latest Roslyn variant, per S-13.
- Refuse the value in the variant that cannot emit the keyword, so that an aspect never silently obtains a class that
  is not closed.

#### Acceptance criteria

- An aspect introduces a closed class whose generated code compiles.
- The generated modifier list reads `closed partial class` and never `abstract closed class`.
- Introducing a closed struct, a sealed closed class or a static closed class is refused with an exception that names
  the language restriction, which the aspect pipeline reports as an error.
- Setting the property in the variant that cannot emit the keyword is refused as well.
- Both Roslyn variants build.

— Claude for @gfraiteur
