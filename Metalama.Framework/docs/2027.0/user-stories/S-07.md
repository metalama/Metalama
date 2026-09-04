### S-07. Repair the declaration-kind switches that silently fall through

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Extensions`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: nothing
- Findings: [DT-5](../05-design-time-workspaces-linqpad.md), [TP-7](../02-syntax-generator-and-templates.md),
  [PR-11](../07-premium.md)

---

Four places switch over declaration kinds and do the wrong thing for a kind they do not list, and none of the four is
caused by C# 15. `CSharpAttributeHelper.cs:74-191` returns null for records, record structs and extension blocks, and
the caller propagates the null, so the add-attribute code action reports success and does nothing.
`ChangeVisibilityCodeAction` in `Metalama.Premium` skips interfaces and indexers in the same way, which is story
S-27. The member switch of `TransformCompileTimeType` throws for an indexer and falls through for an extension block,
so a template declared inside an extension block is copied verbatim.
`ReferenceValidationContext.GetInboundGranularity` throws for a
validated extension block, and the exception is reported as an error diagnostic from inside the user validator.

#### Context

Extension blocks are a C# 14 feature and already ship, so the last of these is a defect that a customer can
encounter today, and the first two are wrong for records, which have shipped for years. In every case the remedy is
to test an abstract syntax base type or to add the missing arm, which also admits unions later without naming an
experimental member. The remedy is the same in both repositories, so S-27 is written from the same reviewed design,
and it is a separate story because a pull request cannot span two repositories.

#### Scope

- In `Metalama.Framework/src/Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs`, replace the per-kind
  arms for type and member declarations by one call to `MemberDeclarationSyntax.AddAttributeLists`, narrowed so that
  namespaces, enum members, global statements and incomplete members keep returning null, and keep the special cases
  for parameters, accessors and the compilation unit, which do not derive from that type.
- Keep the trivia behaviour that the tests of #779 pin, because the caller restores the leading trivia of the old
  node.
- In `CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.TransformCompileTimeType`, decide between
  reporting a diagnostic for a template declared inside an extension block and supporting it; support requires
  extracting the member loop, because an extension block declaration is a type declaration whose `Identifier`
  returns default and whose base-list mutators throw.
- Add the missing arm for the extension block kind in `ReferenceValidationContext.GetInboundGranularity`, with an
  aspect test that validates an extension block containing an extension method and that compiles against the Roslyn
  version Premium consumes today.

#### Acceptance criteria

- The add-attribute code action adds the attribute to a record, a record struct and an extension block, and the
  existing trivia tests do not regress.
- A template declared inside an extension block either compiles or is reported with a diagnostic that names the
  reason, and is never copied verbatim.
- A reference validator that validates an extension block reports its own diagnostics and no exception.

#### Not in scope

This story does not handle unions. Every arm added here is written so that a union is admitted later without a
further edit, but no C# 15 member is named. The change-visibility code action of `Metalama.Premium` is story S-27.

— Claude for @gfraiteur
