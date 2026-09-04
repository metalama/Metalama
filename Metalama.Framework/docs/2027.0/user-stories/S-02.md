### S-02. Apply the variant gating decision to the engine sources and to the doctrine

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [CM-10](../03-code-model-unions-closed.md)

---

`Metalama.Framework.Engine.5.0.0` compiles the same source files as `Metalama.Framework.Engine` against Roslyn 5.0,
whose public application programming interface has neither `UnionDeclarationSyntax` nor `ITypeSymbol.IsUnion`.
Production source carries no conditional compilation today, and the only variant constant,
`ROSLYN_5_10_0_OR_GREATER`, is defined by `eng/RoslynVersions/Roslyn.5.10.0.props:10` and is used by two aspect tests.
Section 2 of [`DECISIONS.md`](../DECISIONS.md) settles the mechanism: the C# 15 Roslyn members are reached through
conditional compilation and one implementation assembly per Roslyn version. This story applies that decision, so that
the findings which depend on it, which are every finding of the code model theme and at least fourteen findings of
the other themes, do not each decide it again.

#### Context

Issue #1881 removed 177 `#if ROSLYN_*` blocks from 152 production files and wrote the note, in both variant property
files, that no production source branches on the variant. Section 2 supersedes that note for the C# 15 members and
rejects the two alternatives that were considered, which are numeric syntax kind values with a run-time guard and a
per-variant service that reads the members by reflection; the second repeats what #1215 deliberately removed. The
decision is deliberately narrow: it covers the members that Roslyn 5.0 does not have, and it does not reopen the
general policy for anything else.

#### Scope

- Rewrite the note in `eng/RoslynVersions/Roslyn.5.0.0.props:8-10` and in the latest variant property file, and the
  corresponding paragraph of [`Directory.Packages.md`](../../../../Directory.Packages.md), so that they state the
  current policy: production source may branch on the latest variant symbol, and only for members that the lower
  variant does not expose.
- State in the same place which members are covered, namely `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`,
  `SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes`, `ITypeSymbol.IsClosed` and the
  `Name` field of `BreakStatementSyntax` and `ContinueStatementSyntax`, and that the list is closed rather than a
  precedent for new branches.
- Record that the variant symbol is named after the variant and is therefore renamed by S-09, and add it to the
  rename list of that story.
- Record what the lower variant does at each site, which is to report the value that an ordinary type would report,
  and reference D-3 for whether it also reports a diagnostic.
- Settle the suppression of `RSEXPERIMENTAL006`: it is required while the latest variant is built against a Roslyn
  that still marks the union and closed members experimental, and it disappears when the variant reaches Roslyn 5.12.
- Deliver one worked example in the smallest consumer, so that the pattern is visible in the code rather than only in
  a document.
- State whether the public `Metalama.Framework.Sdk` kind helpers, which are part of the extensibility surface, may
  name the new kinds at all, since a public surface cannot easily be narrowed later.
- Reference the open issue #1217, which asks for `Metalama.Extensions.Metrics` to support several Roslyn versions,
  and state whether the policy written here applies to that package or leaves it outside the closed list.

#### Acceptance criteria

- Both variant property files and [`Directory.Packages.md`](../../../../Directory.Packages.md) describe the policy that
  is actually in force, and no document still states that production source carries no variant branch.
- One production source file compiles a C# 15 Roslyn member behind the variant symbol, and both variants build.
- The list of members that may be gated is written down, and the rule for adding to it is written down.

#### Not in scope

This story does not deliver the union and closed features themselves. It delivers the mechanism and one example.

— Claude for @gfraiteur
