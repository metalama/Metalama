### S-18-4. C# 15 unions: design-time partial part

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Parent story: S-18, of which this is a sub-story
- Size: M
- Blocked by: S-18-1
- Findings: [CM-3](../03-code-model-unions-closed.md), [DT-2](../05-design-time-workspaces-linqpad.md),
  [LK-4](../04-linker-and-advice.md)

---

`DesignTimeSyntaxTreeGenerator.CreatePartialType` selects a struct declaration for any `TypeKind.Struct` that is not
a record, at
`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:749`, and
Roslyn reports a union as `TypeKind.Struct`. The generated document therefore declares a partial struct against a
partial union, and the compiler reports `CS0261` on the type in the editor. This is the part of union support that a
user sees first.

#### Context

This story settles two implementation points that no other story settles. The discriminator must be the kind of the
primary declaration syntax rather than the Roslyn union flag, because that flag is also true for a hand-written class
or struct carrying the union attribute, whose generated part must stay a class or a struct; emitting a union part for
such a type would itself produce `CS0261`. And the generated part must omit the case list, because exactly one part
of a partial union carries it and a second one is `CS8863`, as
[`analysis-reports/11-introducing-unions-design.md`](../analysis-reports/11-introducing-unions-design.md) settles. The
`closed` modifier needs no counterpart in the generated part, because the compiler merges the modifiers of partial
parts, which is a verified negative statement and is recorded rather than implemented.

#### Scope

- Add the arm to `CreatePartialType` that emits a union declaration, keyed on the kind of the primary declaration
  syntax, with the partial modifier, the identifier, the type parameters, no case list, and the base list passed
  through as every other arm does, because a union may implement interfaces.
- Keep the generated part a class or a struct for a hand-written type carrying the union attribute.
- Gate the syntax factory call on the latest Roslyn variant, per S-13, because the factory does not exist in the
  lower variant.
- Add a design-time aspect test for a partial union with introduced members, with its generated partial documents
  committed.
- Record in the story that no `closed` counterpart is needed, with the reason.

#### Acceptance criteria

- A partial union with introduced members shows no error in the editor, and the generated document declares a partial
  union with no case list.
- A hand-written class or struct carrying the union attribute still receives a partial class or partial struct part.
- The design-time test and its generated partial documents are committed, and the pull request description states
  what the generated documents contain.

— Claude for @gfraiteur
