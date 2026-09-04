### S-07. Metalama.Premium: change-visibility code action

- Issue type: Bug
- Labels: `bug`, `Area-Extensions`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: S
- Blocked by: nothing
- Findings: [PR-10](../07-premium.md)

---

`ChangeVisibilityCodeAction` in `Metalama.Premium` switches over declaration kinds and does the wrong thing for a
kind it does not list: it skips interfaces and indexers, so the code action reports success and changes nothing.
This is the same defect that S-08 repairs in `CSharpAttributeHelper`, and it is a separate story because a pull
request cannot span two repositories.

#### Context

The defect is not caused by C# 15, and it is reachable today for an interface and for an indexer, both of which have
shipped for years. The remedy is the one that S-08 applies, which is to apply the modifiers through the abstract type
declaration rather than through an enumeration of concrete kinds, and it also admits unions later without naming an
experimental member. The design is reviewed once, in S-08, and applied here.

#### Scope

- In `Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs`, apply the modifiers
  through the abstract type declaration, in an override of `VisitCore` and not of `Visit`, which is sealed in
  `SafeSyntaxRewriter`.

#### Acceptance criteria

- The change-visibility code action changes the visibility of an interface and of an indexer.

— Claude for @gfraiteur
