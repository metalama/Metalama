### S-18-3. C# 15 unions: linker and advising

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Parent story: S-18, of which this is a sub-story
- Size: L
- Blocked by: S-18-1
- Findings: [LK-1](../04-linker-and-advice.md), [LK-2](../04-linker-and-advice.md), [LK-8](../04-linker-and-advice.md),
  [CM-8](../03-code-model-unions-closed.md)

---

`LinkerInjectionStep.Rewriter.cs:316-324` and `LinkerLinkingStep.LinkingRewriter.cs:37-85` each dispatch on the
concrete type declaration kinds, so a member injected into a union is never inserted and, once inserted, would never
be linked. Repairing only the first produces a worse state than repairing neither. This story decides whether an
aspect applied to a union produces correct code, wrong code or a clear diagnostic.

#### Context

Section 3 of [`DECISIONS.md`](../DECISIONS.md) requires full support: the linker injects and links advice applied to a
union, and advice that a union cannot carry is refused with a clear diagnostic rather than producing code that the
compiler rejects. The language forbids instance fields, auto-properties and field-like events in a union declaration,
forbids a public single-parameter constructor and requires every explicit constructor to chain to a generated one, so
several ordinary advices would otherwise emit code that the compiler rejects, and the diagnostic is then reported on
generated code, which the user cannot correct. There is a fourth, silent case: an initializer
injected into a constructor that has no syntax. One condition constrains every rule written here, because the
restrictions apply to the `union` declaration form and not to a type carrying the union attribute.

#### Scope

- Add the union arm to the per-kind dispatch of the injection rewriter and of the linking rewriter, inside the
  latest variant block that S-13 establishes. The kind is added to the existing dispatch rather than replaced by a
  test on the abstract type declaration, because the code base routes nodes by kind, and the variant block is what
  allows the arm to name a syntax type that the lower variant does not have.
- Preserve the parameter list in the fallback path, because for a union that list holds the case types, and keep the
  fallback out of the record and struct paths, whose removed-primary-constructor branch would delete it and whose
  positional branch calls `GetDeclaredSymbol` on a parameter that has no declared symbol.
- Correct the insert-position walks so that a member injected into a union is placed in a valid position.
- Add the eligibility rules that refuse what a union declaration cannot carry, naming each restriction, and make each
  rule state which of the two union forms it tests.
- Read the code model member added by S-18-1 rather than the Roslyn flag in those rules, because the eligibility rules
  live in the public assembly, which does not reference Roslyn.
- Make the synthesized `Value` property and the per-case constructors readable by the code model and reachable by the
  linker, extending the mechanism of metalama/Metalama#1879 rather than duplicating it, and rebase onto that pull
  request, whose gates are keyed on `IsRecord`.
- Decide whether `meta.Proceed()` in an override of the synthesized `Value` is rejected as it is for synthesized
  record members.
- Do not allocate the diagnostic identifiers that #1879 takes, and do not reuse the one it removes.

#### Acceptance criteria

- An aspect that introduces a method or a nested type into a union produces code that compiles, and the introduced
  member is linked.
- An aspect that introduces an instance field, an auto-property, a field-like event, a public single-parameter
  constructor or an unchained constructor into a union declaration is refused with a diagnostic that names the
  language restriction, and emits nothing.
- The same advice applied to a type carrying the union attribute is not refused, because the restriction does not
  apply to it.
- An aspect can read the synthesized `Value` property and the per-case constructors of a union.
- Each advice kind of the test matrix of this story, applied to a union declaration and to a type carrying the union
  attribute, either produces code that compiles or is refused with a Metalama diagnostic, and no case of that matrix
  produces a compiler error reported on generated code.

#### Not in scope

This story does not introduce a union or a union case, which is S-29.

— Claude for @gfraiteur
