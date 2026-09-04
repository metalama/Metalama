### S-13. Give the compile-time path and the design-time classifier a union dispatch

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Framework-Templates`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-03, S-12
- Findings: [CM-9](../03-code-model-unions-closed.md), [TP-6](../02-syntax-generator-and-templates.md),
  [LK-10](../04-linker-and-advice.md)

---

`FindCompileTimeCodeVisitor` and `ProduceCompileTimeCodeRewriter` of `CompileTimeCompilationBuilder` classify and
rewrite type declarations through four overrides that route to a private `VisitTypeDeclaration`, and a union
declaration reaches none of them. A run-time union in a file that also contains an aspect is therefore copied into
the compile-time compilation and breaks it with a message about a language version or a missing framework type, while
a union nested in a compile-time class is copied verbatim and one nested in a run-time class is dropped. The
`TextSpanClassifier` has the same gap at `Formatting/TextSpanClassifier.cs:113-119`.

#### Context

A user encounters first a run-time union declared in the same file as an aspect, so the acceptance test is one aspect
test that puts both in one file. The classifier cannot be corrected on its own: `TemplateAnnotator` has no
union dispatch either, and its default path annotates an unhandled type declaration as run-time or compile-time,
which the classifier accepts as compile-time. Routing every unhandled type declaration in the classifier to the
compile-time helper would therefore classify a run-time union, a run-time interface and an extension block as
compile-time, and would require the formatting baselines to be re-adopted. The three findings are one story because
all three edit visitors that must agree.

#### Scope

- Give `ProduceCompileTimeCodeRewriter` a dispatch for a union declaration, either by an override or by a type test
  that replaces the four kind-specific overrides and excludes extension blocks, and route it to the existing private
  `VisitTypeDeclaration`, which classifies by templating scope.
- Give `FindCompileTimeCodeVisitor` the same coverage, so that a union carrying a compile-time attribute is
  classified and reaches the manifest.
- Give `TemplateAnnotator` a dispatch for a union declaration that annotates it with the scope its declaration
  implies, before changing the classifier.
- Correct `TextSpanClassifier` so that a compile-time union declaration is classified, without classifying a run-time
  union, a run-time interface or an extension block as compile-time.
- Re-adopt the formatting baselines that the classifier change affects, reading each difference rather than adopting
  it blindly.
- Add the aspect test with a run-time union and an aspect in one file, and a test for a union nested in a
  compile-time class and in a run-time class.

#### Acceptance criteria

- A file that declares a run-time union and an aspect compiles, and the union does not appear in the compile-time
  compilation.
- A union nested in a run-time type is reported with the diagnostic that a struct in the same position reports, and
  is not copied verbatim.
- A compile-time union declaration is coloured as compile-time at design time, and a run-time one is not.
- Both Roslyn variants build.

— Claude for @gfraiteur
