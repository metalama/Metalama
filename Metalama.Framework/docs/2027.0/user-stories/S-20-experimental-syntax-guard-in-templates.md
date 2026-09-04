### S-20. Templates: raise errors when experimental syntax is used

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework-Templates`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-13, S-15
- Findings: [TP-4](../02-syntax-generator-and-templates.md), [TP-5](../02-syntax-generator-and-templates.md)

---

A collection expression with a with-element crashes the template compiler in run-time scope today, with an
`InvalidCastException` surfaced as an unexpected-exception error and a crash report rather than as a template
diagnostic. The regeneration of S-13 produces the visitor for that node and removes the crash, so only the tests
remain. The regeneration does not remove the failure of the unsafe expression, which keeps its experimental marker on
the target Roslyn and is therefore still stripped from the generated code, so it passes the annotator and surfaces as
a C# error on the generated compile-time file.

#### Context

The unsafe expression is not part of C# 15: it is gated on the preview language version and is out of scope for
2027.0. The correct remedy is therefore not to support it but to reject it in the template compiler, and to do so
without naming it, because the same protection must apply to every future experimental node. The generator already
knows which declarations it removed as experimental, so the guard can be driven by that knowledge rather than by a
per-node override that would have to be written again each time Roslyn adds an experimental node.

#### Scope

- Make the syntax generator record the node kinds it removed as experimental, and add a name-free guard in the
  template compiler that reports a template diagnostic for any node of such a kind.
- Add the aspect tests for a with-element in a collection expression, in run-time and in compile-time scope, beside
  the existing collection-expression test of the C# 12 suite, and not in a syntax directory that does not exist.
- Confirm after regeneration that a with-element no longer crashes the template compiler, and record the result in
  the test rather than in a comment.

#### Acceptance criteria

- A with-element in a template compiles in both scopes, with committed expected output.
- An experimental syntax node in a template is reported with a template diagnostic naming the template, and never
  reaches the compile-time compilation.
- Adding a new experimental node to the grammar requires no new override for the guard to cover it.

#### Not in scope

This story does not support the unsafe expression, which is a preview language feature and is out of scope for
2027.0. That support is already tracked by the open issue #985, which lists the unsafe expression among the deferred
template compiler features. This story delivers the guard that reports such a node with a template diagnostic, and it
does not deliver the support that #985 tracks, so #985 stays open and is referenced from this story.

— Claude for @gfraiteur
