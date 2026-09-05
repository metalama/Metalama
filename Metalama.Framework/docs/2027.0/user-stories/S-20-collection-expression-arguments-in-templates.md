### S-20. C# 15 collection expression arguments: support in templates

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework-Templates`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: S-13, S-15
- Findings: [TP-4](../02-syntax-generator-and-templates.md), [TP-5](../02-syntax-generator-and-templates.md)

---

A collection expression that carries a with-element crashes the template compiler in run-time scope today, with an
`InvalidCastException` surfaced as an unexpected-exception error and a crash report rather than as a template
diagnostic. The regeneration of the syntax model in S-13 produces the visitor for that node and removes the crash,
so what remains is to prove it with tests.

#### Context

An earlier draft of this story also proposed a guard that would report any syntax node which the generator had
stripped as experimental. That proposal is withdrawn, because it cannot be implemented and because it is not needed.

It cannot be implemented, because detecting an experimental node requires naming its kind, and the source of this
product may not reference an experimental Roslyn member. An experimental member may be removed at any time, and a
reference to one would break the assemblies that carry it.

It is not needed, because an experimental language feature is gated on `LanguageVersion.Preview`, and
`CompileTimeAspectPipeline.VerifyLanguageVersion` already reports `PreviewCSharpVersionNotSupported` for a project
that selects the preview language version without setting `AllowPreviewLanguageFeatures`. Forbidding the preview
language version forbids every experimental feature at once, which is the product's existing answer and does not
name any of them. The unsafe expression, which is gated on the preview version and is out of scope for 2027.0, is
covered by that rule and needs nothing of its own.

#### Scope

- Add the aspect tests for a with-element in a collection expression, in run-time scope and in compile-time scope,
  beside the existing collection-expression test of the C# 12 suite rather than in a syntax directory that does not
  exist.
- Confirm after the regeneration of S-13 that a with-element no longer crashes the template compiler, and record
  the result in the test rather than in a comment.

#### Acceptance criteria

- A with-element in a template compiles in both scopes, with committed expected output.
- No template that uses a with-element produces an unexpected-exception error or a crash report.

#### Not in scope

A guard that detects experimental syntax, for the two reasons given in the context. A project that selects the
preview language version, which the existing preview rule already governs.

— Claude for @gfraiteur
