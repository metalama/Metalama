# 02. The syntax generator, the template compiler and the template language

This document covers the code generator that produces the meta-syntax rewriters from the Roslyn grammar
(`eng/src/GenerateMetaSyntaxRewriter`), the template compiler and its annotator, the compile-time code rewriter, the
Roslyn version checker, the design-time code hashers and the template language version. It records how each of them
behaves when C# 15 syntax reaches it, and what has to change for Metalama 2027.0. The analysis reads the code as it
stands on 2026-09-03 on branch `topic/2027.0/26-09-03-update-eng-7e3j07` of the `Metalama` repository. Each finding
was then re-checked by three verification passes: a code pass that re-read the cited code and tried to falsify the
claim, a semantics pass that re-checked every external premise against `dotnet/roslyn` and `dotnet/csharplang`, and a
scope pass that established whether the proposed change is already implemented, in flight or tracked. The platform
baseline PB-2027.0 is decided by [`platform-support.md`](../platform-support.md), the permitted package versions by
[`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving to a new Roslyn by
[`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

No project was built and no test was run for this analysis.

## Summary

1. The generator removes every declaration that the grammar file marks with `ExperimentalUrl`
   (`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`), so the generated rewriters, the version checker,
   the partial-update extensions and the design-time hashers know nothing today of the union declaration, the
   with-element, the unsafe expression and the label of a `break` or `continue` statement. Regeneration is a
   consequence of the renumbering of the latest Roslyn variant to the stable 5.12, which is owned by theme 01.
2. Regeneration alone produces a silent defect. `SupportedCSharpVersions.ToLanguageVersion` maps the latest variant
   to C# 14 (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:60`), so a
   regenerated version checker would accept C# 15 syntax in a C# 14 template and would report neither LAMA0232 nor
   LAMA0282. The mapping must change in the same commit as the grammar.
3. Labeled `break` and `continue` are the one piece of genuine template-compiler work that C# 15 requires. The label
   is invisible to the annotator, absent from the generated factory call, and absent from both design-time hashes,
   so an emitted `break` targets the innermost loop instead of the labeled one, with no diagnostic.
4. A collection expression that carries a `with(...)` element crashes the template compiler in run-time scope today.
   The crash disappears when the generator emits a visitor for the node, so the remaining work is a pair of tests.
5. The unsafe expression keeps its experimental marker on the target Roslyn, so it stays stripped after
   regeneration. It needs a guard that does not name the type, because the same source is compiled against Roslyn
   5.0, which has no such type.
6. Union declarations and extension blocks fall through hand-written kind lists in the compile-time code path. A
   union nested in a compile-time type is copied without a manifest entry and without template compilation; a union
   nested in a run-time type is dropped; a template declared inside an extension block is copied untransformed.
7. Raising the template language to C# 15 touches a small and well-identified set of constants, and it is blocked on
   the move to Roslyn 5.12, because no Roslyn that Metalama consumes today defines `LanguageVersion.CSharp15`. It
   also carries a product decision about the default template language of a `net11.0` project in a Roslyn 5.0 host.
8. The `closed` modifier, patterns over union values, and both analyzer projects require no work in this theme.

## Findings

### TP-1. Regeneration of the generated syntax files when the stable grammar drops `ExperimentalUrl`

- Where:
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`, `:35-43`, `:55-74`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeType.cs:37`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/Field.cs:51`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs:11-57`
  - `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:16-18`, `:28`, `:30-48`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:64-98`, `:118-156`, `:403-431`, `:432-479`, `:535-609`,
    `:637-708`, `:761-800`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:496-508`, `:816-822`, `:1290-1311`, `:1954-1978`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-4.0.1.xml:1200-1209`
  - `eng/src/Program.cs:237-260`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:60`, `:85`, `:142`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:37-38`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj:34-35`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Tests/LocalVariables/CompileTimeVariableInSwitch.ct.cs:27`
  - `.gitignore:62`, `Directory.Packages.props:28`
  - `Metalama.Framework/docs/updating-roslyn.md:12`, `:15-22`, `:29`
- What happens today: `TreeReader.ReadTree` calls `RemoveExperimentalDeclarations` before the version detector and
  the generator run (`TreeReader.cs:19`). That method removes every node type whose `ExperimentalUrl` is set
  (`TreeReader.cs:35-43`) and every experimental field, recursively (`TreeReader.cs:55-74`). The predicate is
  `TreeType.cs:37` and `Field.cs:51`, and the documentation of both members states the reason: Roslyn marks the
  corresponding API with `ExperimentalAttribute`, and a reference from generated code is then an `RSEXPERIMENTAL`
  error. No project, property or target file in the repository suppresses that diagnostic. The grammar file declares
  five such items: `UnsafeExpressionSyntax` (`Syntax-5.10.0.xml:496`), `WithElementSyntax` (`:816`), the `Name` field
  of `BreakStatementSyntax` (`:1296`) and of `ContinueStatementSyntax` (`:1307`), and `UnionDeclarationSyntax`
  (`:1954`). Generation runs from `OnPrepareCompleted` (`eng/src/Program.cs:237-260`), which the `prepare` step of
  `Build.ps1` raises, and the outputs are written under `Metalama.Framework/.generated/<version>` and are git-ignored
  (`.gitignore:62`), so nothing has to be committed and nothing has to be edited in step: no member of the generated
  partial classes is hand-written.
- The trigger is not a version number. No stable Roslyn 5.10, 5.11 or 5.12 package exists on nuget.org, and the
  November 2026 baseline is expected to carry Roslyn 5.12. What removes the experimental markers is one Roslyn
  commit, "Add C# 15 language version" (`dotnet/roslyn` pull request 84799 of 2026-08-11), which lands inside the
  5.11 window. Regeneration therefore starts to emit code as soon as the consumed `Microsoft.CodeAnalysis.CSharp`
  build is later than that commit. In practice the step is the renumbering of the latest variant described by steps 7
  and 8 of [`updating-roslyn.md`](../updating-roslyn.md), with the stable grammar added as a new file (step 4 forbids
  renaming the previous one) and the version name changed in `GenerateMetaSyntaxRewriter.cs:16-18`.
- What regeneration produces, in the latest variant only: new `Visit` and `Transform` members for the union
  declaration and the with-element (`Generator.cs:403-431`); a `switch (this.TargetApiVersion)` in
  `TransformBreakStatement` and `TransformContinueStatement`, because the fields then have different minimal versions
  (`Generator.cs:432-479`, `VersionDetector.cs:11-57`); new `MetaSyntaxFactoryImpl` methods and a `name` parameter on
  the break and continue factories (`Generator.cs:535-609`); new overrides in the version checker that report the
  version-specific diagnostic (`Generator.cs:118-156`); a `name` parameter on the partial-update extensions
  (`Generator.cs:761-800`); and `this.Visit(node.Name)` plus three new visits in the two code hashers
  (`Generator.cs:637-708`). The `UnsafeExpressionSyntax` node does not un-strip, because it keeps its
  `ExperimentalUrl` marker on `dotnet/roslyn` `main`; see TP-5. Because the identity of the latest variant changes,
  `RoslynApiVersion.g.cs` also changes (`Generator.cs:64-98`), and the three hand-written references at
  `SupportedCSharpVersions.cs:60`, `:85` and `:142` change with it.
- Consequence: build error. If the grammar file and the consumed package do not move together, the build fails in one
  direction with `RSEXPERIMENTAL006` on the latest variant and generates nothing in the other, and no test detects
  either mismatch.
- Proposed change: none in the generator itself, beyond the renumbering that theme 01 owns. Add to
  [`updating-roslyn.md`](../updating-roslyn.md) the consequence that step 4 does not yet state: when the new Roslyn
  removes the marker from a declaration, the generator starts emitting code for it in the latest variant, which
  changes `MetaSyntaxRewriter.g.cs`, `RoslynVersionSyntaxVerifier.g.cs`, `SyntaxNodePartialUpdateExtensions.g.cs` and
  the two code hashers, so the grammar file and the referenced package must move in the same commit. The consistency
  check proposed by the original report is unsound as written and is the subject of the decision below.
- Size: extra small for the documentation sentence; small if a guard is added.
- Status: decision required. The decision is whether to add a guard and in what form. A check that fails whenever the
  grammar file still declares an `ExperimentalUrl` on a node would fail permanently, because the unsafe expression
  keeps its marker; and the prerelease label of the package version is not a proxy for the absence of the marker,
  because the stable 5.9.0 assemblies still carry it on the union, with-element and unsafe API. A sound check
  compares the local grammar file with the grammar of the exact package the latest variant references, declaration by
  declaration; a cheaper second best is a stored list of the declarations that are knowingly stripped.
- Verification: the code pass re-read the generator end to end and confirmed the mechanism, the absence of any
  colliding hand-written member and the absence of any snapshot change (`CompileTimeVariableInSwitch.ct.cs:27` keeps
  a three-argument factory call, because a nameless `break` leaves the target API version at the lowest value), and
  corrected several line citations and the count of stripped declarations from four to five. The semantics pass
  confirmed the stripping rationale and refuted three external premises: that a stable Roslyn 5.10 exists, that
  stability is the trigger, and that the unsafe expression un-strips. The scope pass found the change neither
  implemented, in progress nor tracked, and related it to #1881, #1885 and #1896 under the open meta-issue #1921.
- Open questions: whether the generated partial-update, visit and transform members for the two newly un-stripped
  node types compile against the stable assembly depends on those types exposing an `Update` overload and a visitor
  method of the shape the generator assumes. That is the assumption every other node satisfies, but it cannot be
  verified from this repository, so the statement that regeneration has no other effect rests on it.

### TP-2. The version checker maps the latest Roslyn variant to C# 14, so C# 15 syntax passes as C# 14

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:52-62`, `:149-159`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-39`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:111`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:41-75`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:106-108`, `:224-233`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateExpansionContext.cs:861-877`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:118-137`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/RoslynVersion.cs:19-21`
- What happens today: the mapping `RoslynApiVersion.V5_10_0 => AllLanguageVersions.CSharp14`
  (`SupportedCSharpVersions.cs:60`) is correct for the Roslyn that Metalama consumes, because neither the stable
  5.9.0 nor the consumed 5.10 prerelease defines `LanguageVersion.CSharp15`, and both gate the six C# 15 features on
  `LanguageVersion.Preview`. While the grammar carries the experimental markers, the generator also strips the new
  declarations, so no version check is emitted for them. The defect appears in the change that moves the latest
  variant to the first Roslyn that both removes the markers and defines `LanguageVersion.CSharp15`, that is Roslyn
  5.12. After that renumbering the generator emits a `VisitVersionSpecificField` call for the `Name` field of the two
  jump statements and a `VisitVersionSpecificNode` call for the with-element, both naming the renumbered enumeration
  member (`Generator.cs:118-137`, `RoslynVersion.cs:19-21`). If `ToLanguageVersion` still maps the latest variant to
  C# 14, a template using `break outer;` or a `with(...)` element is accepted under template language version 14
  without LAMA0232 (`RoslynVersionSyntaxVerifier.cs:41-75`), and an aspect using them does not warn with LAMA0282
  when applied to a C# 14 project (`TemplateExpansionContext.cs:861-877`). If the mapping is not added at all,
  `ToLanguageVersion` throws its assertion failure for the new enumeration member (`SupportedCSharpVersions.cs:61`).
  A union declaration is a type declaration and cannot appear in a template body, so it is not among the syntaxes
  this verifier can observe.
- Consequence: silent wrong output. Two diagnostics that exist precisely to protect a user from writing a template
  in a language version that the target project cannot compile are simply not reported.
- Proposed change: make these edits in the same change that renumbers the latest variant and regenerates from the
  stable grammar, and not before. Add `CSharp15 = (LanguageVersion) 1500` to `AllLanguageVersions.cs:14-18`; the
  numeric value is verified on `dotnet/roslyn` `main`. Add the arm for the new latest member to `ToLanguageVersion`
  (`SupportedCSharpVersions.cs:52-62`) and leave the existing arms at C# 14. Add `(5, >= 12) => CSharp15` before
  `(>= 5, _) => CSharp14` in `GetMaxLanguageVersion` (`SupportedCSharpVersions.cs:149-159`); do not use `(5, >= 10)`,
  because that would overstate the capability of a desktop MSBuild carrying Roslyn 5.9, 5.10 or an early 5.11 build,
  and `LanguageVersionProvider.cs:111` would then let a C# 15 project version reach a compiler that rejects it. Add
  `(LanguageVersion) 1500 => "15.0"` to `ToDisplayStringSafe` (`LanguageVersionExtensions.cs:33-39`); without it
  LAMA0282 throws an argument exception instead of being reported, because `TemplateExpansionContext.cs:872-876`
  formats the required version with that method. For tests, add an aspect test under
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/LanguageVersion/` in the style of
  `Template_OldVersion.cs`, and a standalone scenario in the style of
  `Metalama.Framework/src/tests/Standalone/TemplateLanguageVersion14`. Use `break outer;` and a collection expression
  with a `with(...)` element as the test syntax; do not use a union declaration.
- Size: small in itself, but the change is only correct together with the language version plumbing of TP-8, so it
  cannot be delivered on its own.
- Status: new work, and part of the language version cluster owned by theme 01. TP-2 is a subset of TP-8, which
  proposes the same two edits among a larger set.
- Verification: the code pass confirmed every cited site and the whole flow from the verifier to the manifest and
  back to LAMA0282, and corrected the proposal by showing that the mapping edit applied alone would invert the
  defect, because `MaximalAcceptableLanguageVersion` cannot reach C# 15 while `SupportedCSharpVersions.All` stops at
  C# 14. The semantics pass confirmed that the three syntaxes are gated on C# 15 on `main` and that the numeric value
  1500 is correct, refuted the version identity (Roslyn 5.10 has no C# 15) and the proposed `(5, >= 10)` threshold,
  and removed the union declaration from the example list. The scope pass found no implementation, no pull request
  and no issue, and recorded #1896, #1881, #1105 and #1039 as precedents.
- Open questions: none.

### TP-3. Labeled `break` and `continue` are not modelled by the annotator, and the label is dropped silently

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:1375-1379`, `:1419`, `:1423`,
    `:1360-1361`, `:1483`, `:2501`, `:2533`, `:2559`, `:2925`, `:685`, `:2590-2591`, `:2601-2606`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.ScopeContext.cs:21`, `:123-132`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs:198-263`, `:390-402`,
    `:491-497`, `:629-635`, `:2267-2289`, `:2408`, `:2722-2745`, `:2908-2985`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs:20-30`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:44-62`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:106-136`, `:152-162`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:348-352`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:44-70`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`,
    `:177`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:419-420`, `:481-519`, `:615-670`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1290-1311`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml:1270-1279`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/GotoNotSupported.cs:26`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:680-698`
- What happens today, scope classification: `VisitBreakStatement` and `VisitContinueStatement` annotate the statement
  with the current break-or-continue scope (`TemplateAnnotator.cs:1375-1379`), which every loop and switch sets to
  its own scope for its body (`TemplateAnnotator.ScopeContext.cs:21`, `:123-132`, with callers at
  `TemplateAnnotator.cs:1419`, `:1483`, `:2501`, `:2533`, `:2559`, `:2925`). Neither visitor calls the base
  implementation, so the `Name` child is never read. A `break outer;` inside a run-time inner loop nested in a
  compile-time outer loop is therefore classified run-time, and the reverse is classified compile-time, whatever the
  label designates. Neither the annotator nor the rewriter overrides `VisitLabeledStatement`, and a labeled statement
  receives no scope annotation of its own because `AddScopeAnnotationToVisitedNode` returns statements untouched
  (`TemplateAnnotator.cs:685`). According to the language proposal, the label designates the nearest enclosing switch
  or iteration statement carried by a labeled statement whose immediately nested statement is that switch or loop, so
  the scope must follow the labeled loop and not the innermost one.
- What happens today, run-time scope: the generated `TransformBreakStatement` builds the three-argument factory call
  from the stripped field list (`Generator.cs:481-519`), so the label is dropped and the generated run-time code
  contains a plain `break;` that exits the innermost enclosing switch or loop. No diagnostic is produced, because the
  generated version checker covers only modelled declarations. The design-time hashers are generated from the same
  stripped model (`Generator.cs:615-670`), so two templates that differ only by a label hash identically; see TP-9.
- What happens today, compile-time scope: the statement is kept as compile-time C# and the base rewriter preserves
  the label identifier (`TemplateCompilerRewriter.cs:2267-2289`, `:2908-2985`). The compile-time compilation does not
  accept the result. Roslyn checks the feature in the binder and not in the parser, and the check reads the language
  version from the parse options of the syntax tree; `CompileTimeCompilationBuilder.cs:348-352` attaches fresh parse
  options to every tree it creates, so the check runs again although the tree is not re-parsed. The compile-time
  language version is capped at C# 14 for every software development kit of major version 10 or later, including when
  the project uses `preview` (`LanguageVersionProvider.cs:44-70`), so this case fails loudly rather than silently.
- What happens today, a labeled statement whose loop is run-time inside a compile-time block: the forced compile-time
  annotation (`TemplateAnnotator.cs:1423`) makes the generated visitor take the base branch
  (`Generator.cs:419-420`), which casts the transformed child, an invocation expression, to a statement. The
  exception is wrapped into a syntax-processing exception (`SafeSyntaxRewriter.cs:44-62`).
- Reachability: the paths above are open today and do not wait for TP-8. In the consumed Roslyn the feature requires
  `LanguageVersion.Preview`, so an aspect project with an explicit preview language version and the preview flag
  passes `VerifyLanguageVersion` (`CompileTimeAspectPipeline.cs:62-93`) and reaches every run-time scope path. That
  check has a single caller (`CompileTimeAspectPipeline.cs:177`), so the design-time pipeline never refuses the
  language version. At C# 14 or lower the template is rejected by the binder and not by the parser, so the syntax
  tree is well formed, `TemplateCompiler.TryAnnotate` still runs and still misclassifies the statement, which is what
  design-time syntax highlighting uses, and only the following check on the semantic model stops the rewriter
  (`TemplateCompiler.cs:152-162`).
- Consequence: silent wrong output in run-time scope, and a crash for a labeled run-time loop inside a compile-time
  block. Before regeneration the emitted `break` targets the innermost loop instead of the labeled one; after
  regeneration a run-time `break outer;` whose label was on a compile-time loop produces CS9393 or CS9394 in the
  transformed user code, which is a compiler error reported in generated code.
- Proposed change: two options, and the choice between them is the decision below.
  - Reject the construct, exactly as `goto` is rejected. In `VisitBreakStatement` and `VisitContinueStatement`, when
    a label is present, call `ReportUnsupportedLanguageFeature` (`TemplateAnnotator.cs:2590-2591`), which reports
    LAMA0101, and add a `VisitLabeledStatement` override that reports the same diagnostic. This removes the silent
    wrong output and the crash for a few lines of code, and matches the existing treatment of `goto`
    (`TemplateAnnotator.cs:2601-2606`) and of a label, which is useless in a template while `goto` is rejected.
  - Support the construct. Add a `VisitLabeledStatement` override that visits the inner statement and copies its
    scope annotation, and record the mapping from label name to scope in the scope context, but only when the
    statement immediately nested in the labeled statement is a switch or an iteration statement, because the proposal
    rejects nested labels and a labeled `continue` may target only an iteration statement. When a label is present on
    a jump statement, resolve its scope from the context, annotate the statement with that scope, and report a new
    error when a run-time `break` targets a compile-time loop, since the unrolled loop has no run-time counterpart.
    In the rewriter, handle a labeled statement whose inner statement is run-time by transforming the whole labeled
    statement instead of letting the base rewriter cast.
  Four constraints apply to the second option. The reserved diagnostic ranges are 100 to 119 and 220 to 299
  (`TemplatingDiagnosticDescriptors.cs:20-30`) and the highest identifier already allocated in that file is LAMA0293,
  so a new error takes an unused identifier in those ranges and is not adjacent to LAMA0101. Annotating a
  compile-time `break` as run-time does not by itself make the label survive, because the generated factory call is
  built from the stripped field list, so the label reappears only after regeneration. The label identifier would be
  emitted verbatim, because `Transform(SyntaxToken)` reserves a unique run-time name only for the symbol kinds
  accepted by `IsLocalSymbol` (`TemplateCompilerRewriter.cs:390-402`, `:491-497`), which does not include a label
  symbol, so a template expanded twice into one method would produce a duplicate label. And reading the label must
  not name `BreakStatementSyntax.Name`, which is absent from Roslyn 5.0 (`Syntax-5.0.0.xml:1270-1279`) and
  experimental in the consumed package, so it must be read through the child nodes until the stable package is
  consumed.
- Size: small for the rejection, medium for the support.
- Status: decision required. The decision is whether Metalama supports labeled `break` and `continue` in templates in
  2027.0 or rejects them with LAMA0101. Nothing in the repository, in the open pull requests or in the issues covers
  either option; the open catch-all #985 lists other features and does not scope this one.
- Verification: the code pass confirmed that the label is invisible to the annotator, absent from the generated
  factory call and unreported by the version checker, added the two consequences the report omits (the hashers and
  the verbatim label identifier), corrected two line citations and the claim that a source error stops the annotator,
  and proposed the rejection option. The semantics pass confirmed the target-resolution rule of the proposal and
  refuted five external premises: the feature is checked in the binder and not in the parser, the compile-time
  compilation therefore rejects it loudly, the diagnostic identifiers are CS9393 and CS9394 rather than CS0159, the
  paths do not wait for TP-8, and a test annotated with the C# 15 language version would be skipped on every variant
  today. The scope pass found no implementation, no pull request and no issue.
- Open questions: the exact exception thrown by the base Roslyn rewriter when the child visit of a labeled statement
  returns an invocation expression could not be read in this session, so that part of the crash description is
  plausible rather than verified; the wrapping into a syntax-processing exception is verified.

### TP-4. A collection expression with a `with(...)` element crashes the template compiler in run-time scope

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:39`, `:446-451`, `:618-643`,
    `:685-697`, `:1319-1325`, `:3495-3503`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/MetaSyntaxRewriter.cs:106-139`, `:144-158`,
    `:171-173`, `:239`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs:196-263`, `:270-304`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/TemplatingScopeExtensions.cs:12-29`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/SyntaxAnnotationExtensions.cs:118-130`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeExceptionHandler.cs:120-135`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:44-62`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SyntaxProcessingException.cs:27-29`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:681-700`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:395-431`, `:481-519`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`, `:35-43`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:816-822`
- What happens today: the annotator visits each element of a collection expression through its private visit helper
  (`TemplateAnnotator.cs:3497`, `:618-620`), which reaches `DefaultVisitImpl` (`:630-643`). A with-element has no
  expression child, because its only node child is an argument list (`Syntax-5.10.0.xml:816-822`), so the child
  filter at `:693` selects nothing, the scope of an empty list is the neutral run-time-or-compile-time scope
  (`:446-451`), and the element receives that scope (`:685-697`). In run-time scope the generated
  `TransformCollectionExpression` transforms the element list (`Generator.cs:481-519`), which transforms each element
  (`MetaSyntaxRewriter.cs:144-158`). The transformation kind of the with-element resolves to `Transform`, because its
  own scope is undetermined (`TemplatingScopeExtensions.cs:12-29`, `SyntaxAnnotationExtensions.cs:118-130`) and the
  parent chain reaches the run-time collection expression (`TemplateCompilerRewriter.cs:196-263`), so
  `MetaSyntaxRewriter.cs:137` casts the visited node to an expression. There is no generated visitor for the node,
  because the generator strips it (`TreeReader.cs:19`, `:35-43`), so the Roslyn base rewriter runs. That method casts
  the result of visiting the argument list to an argument list, and the generated visitor returns the invocation
  expression produced by the transformation, so the cast throws an invalid-cast exception, which
  `SafeSyntaxRewriter.cs:44-62` wraps in a syntax-processing exception. In compile-time scope the collection
  expression is kept verbatim and reaches the compile-time compilation, where TP-8 decides whether it compiles.
- Reachability: no Roslyn that Metalama consumes exposes C# 15 as a non-preview language version, so a template
  containing a with-element parses only under the preview language version. `VerifyLanguageVersion` then stops the
  compile-time pipeline unless the preview flag is set (`CompileTimeAspectPipeline.cs:62-93`), and the comment at
  `:64-65` records that the other pipelines do not perform this check, so the design-time pipeline reaches the crash
  without that option.
- Consequence: crash. The exception is surfaced by `CompileTimeExceptionHandler.cs:120-135` as an unexpected-exception
  error with a crash report rather than as a diagnostic attributable to the user code, and it disappears once the
  generator emits a visitor for the node.
- Proposed change: none beyond TP-1 and TP-2. Add the tests once the stable grammar is consumed. The path named by
  the original report does not exist: there is no `Tests/Syntax/CollectionExpressions` directory. The templating
  tests live under
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests.Internals/Tests/Templating/Syntax`, whose only
  current subdirectory is `Misc`, and the existing collection-expression aspect test is
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp12/CollectionExpressions.cs`,
  beside `CSharp13` and `CSharp14` directories. Add the test as `Tests/Aspects/CSharp15/CollectionExpressions/` in
  `Metalama.Framework.Tests.AspectTests`, covering a run-time and a compile-time with-element in one file as
  `CollectionExpressions.cs` already does for spreads, or add a pair of templating tests if the two scopes must be
  pinned separately. Marking the test with the C# 15 language version is safe to write today, because an unrecognised
  version sets a skip reason rather than failing (`TestOptions.cs:681-700`); it also means the test proves nothing
  until the latest variant is Roslyn 5.12.
- Size: small.
- Status: new work, limited to tests.
- Verification: the code pass confirmed the whole path and defeated three attempts to refute it (no diagnostic fires
  instead of the crash, the node is not diverted before transformation, and neither the element nor its argument list
  is classified compile-time), and added the reachability paragraph and the consequence correction. The semantics
  pass read the Roslyn generated rewriter for the consumed 5.10 preview and verified the cast that the report had
  marked as an assumption, so that clause is now verified rather than plausible. The scope pass found no test, no
  pull request and no issue, and noted that the proposed test directory does not exist.
- Open questions: none for the crash. One design question is left for the moment the node is actually supported:
  because the child filter accepts only expressions and interpolations, the scope of the arguments inside `with(...)`
  never propagates to the collection expression, so a compile-time argument will not force the collection expression
  to compile time the way a compile-time element does.

### TP-5. Unsafe expressions are not rejected by the annotator

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:2594-2599`, `:627-641`,
    `:646-696`, `:104-114`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs:24-30`,
    `:169-177`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:105-107`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/RoslynVersionSyntaxVerifier.cs:17-31`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs:69`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:64-67`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:279`, `:349-351`,
    `:448`, `:562-597`, `:599-651`, `:1173-1175`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/Mapping/TextMapFile.cs:184-202`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:45-72`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`, `:35-43`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:100-170`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:496-508`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/UnsafeNotSupported.cs`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Runner/TemplatingTestRunner.cs:137-139`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:196-198`, `:254-256`, `:609-611`, `:681-700`
- What happens today: the `unsafe` statement is reported as LAMA0101 (`TemplateAnnotator.cs:2594-2599`,
  `TemplatingDiagnosticDescriptors.cs:24-30`), pinned by the expectation file beside `UnsafeNotSupported.cs`. The
  unsafe expression has no handler anywhere: the only occurrence of the identifier in the whole repository is the
  grammar entry (`Syntax-5.10.0.xml:496-508`), which the generator strips, so the default visit delegates to the
  Roslyn base rewriter and the annotator assigns the combined scope of the inner expression without reporting
  anything. The Roslyn parser produces the node at any language version and the language version is checked when the
  expression is bound, so a project at C# 14, which `VerifyLanguageVersion` accepts, already presents the node to the
  annotator. The preview flag is therefore not a precondition of the defect; it only decides whether the compile-time
  pipeline runs at all in a project that sets the language version to `preview`, and the design-time pipeline
  performs no such check (`CompileTimeAspectPipeline.cs:62-93`). In run-time scope the base rewriter rebuilds the
  node around the meta-expression and the compiled template contains an unsafe expression, which the compile-time
  compilation then rejects for two independent reasons: its language version is the minimum of the software
  development kit version and the project version and is therefore never `preview`
  (`LanguageVersionProvider.cs:45-72`, `CompileTimeCompilationBuilder.cs:279`), and its compilation options do not
  allow unsafe code (`CompileTimeCompilationBuilder.cs:448`). The user then sees LAMA0222
  (`TemplatingDiagnosticDescriptors.cs:169-177`) together with the compiler errors, relocated into the template
  source file at the nearest annotated position through the text map (`CompileTimeCompilationBuilder.cs:562-597`,
  `:1173-1175`, `TextMapFile.cs:184-202`), or with no location when the mapping fails.
- The construct is not merely a wrapper. According to the proposal, an unsafe expression establishes an unsafe
  context for the enclosed expression and does not extend it beyond the closing parenthesis, so removing the wrapper
  from generated code is a semantic change even when the value is unchanged.
- Consequence: a diagnostic reported in the wrong place. The compile-time compilation reports compiler errors at an
  approximate position in the template instead of the template reporting LAMA0101 at the unsafe keyword.
- Proposed change: report LAMA0101 for the expression as for the statement. The type cannot be named from the
  annotator, because Roslyn 5.0 does not define it and the consumed package marks it experimental, so extend the
  generator: instead of dropping an experimental declaration, `TreeReader` records its name and
  `Generator.GenerateVersionChecker` emits the list of stripped kind names into the generated version checker
  (`Generator.cs:100-170`); the hand-written `RoslynVersionSyntaxVerifier` resolves them once through an enumeration
  parse and reports LAMA0101 from an override of `VisitCore`. This is feasible: the verifier runs on every template
  before annotation (`TemplateCompiler.cs:105-107`) and its base declares that member as virtual
  (`SafeSyntaxWalker.cs:69`). Two adjustments are required. A diagnostic reported from the verifier does not set the
  annotator's success flag, so the template is still compiled and LAMA0222 is produced as a second diagnostic; either
  raise the failure explicitly or place the same kind test in `DefaultVisitImpl`, where reporting already sets the
  flag (`TemplateAnnotator.cs:104-114`). And the LAMA0101 message formats its argument verbatim, so a guard driven by
  syntax kind names would emit messages naming a raw kind; a table mapping each kind to a readable feature name is
  preferable. For the test, add
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/UnsafeExpressionNotSupported.cs`
  with an expected LAMA0101 and with `@RequiredConstant(ROSLYN_5_10_0_OR_GREATER)`
  (`eng/RoslynVersions/Roslyn.5.10.0.props:8-10`), because the same test sources are compiled against Roslyn 5.0,
  which cannot parse the construct. Do not add an `@AllowPreviewLanguageFeatures` directive: no such test option
  exists (`TestOptions.cs:196-198`, `:254-256`, `:609-611`, `:681-700`), and none is needed, both because the node
  parses at C# 14 and because the template test runner calls the template compiler directly
  (`TemplatingTestRunner.cs:137-139`).
- Size: small for the guard, medium if the generator refactoring is done together with TP-1.
- Status: new work. This is the only proposal in the theme that covers every experimental node generically, and it
  remains necessary after the move to Roslyn 5.12, because the unsafe expression is the one declaration that keeps
  its marker there.
- Verification: the code pass confirmed that no code in the engine handles the node and that the generator removes
  it deliberately, corrected the diagnostic location (the compiler errors are relocated into the template source file
  rather than left in the generated file), named the identifier LAMA0222, and corrected the proposed test. The
  semantics pass confirmed against `dotnet/roslyn` `main` that the node is the only remaining declaration carrying
  `ExperimentalUrl` and that its API members keep the experimental marker, refuted the claim that the expression
  parses only under `preview`, and resolved the disjunction about the compile-time compilation in favour of the loud
  branch. The scope pass found no implementation, no pull request and no issue, and related the work to #1105, the
  C# 14 precedent for reporting an error on an unsupported feature.
- Open questions: the original report asked whether the same guard can detect the experimental field
  `BreakStatementSyntax.Name`. It cannot, and it does not need to: on `dotnet/roslyn` `main` that field is no longer
  experimental, and labeled `break` and `continue` introduce no new syntax kind, so the mechanism does not apply. The
  only obstacle to naming the property is that Roslyn 5.0 does not have it.

### TP-6. A union declaration nested in a compile-time type is copied verbatim, and nested in a run-time type it is dropped

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:204-252`,
    `:254-378`, `:274-354`, `:356-370`, `:372-375`, `:417-447`, `:506-563`, `:540-544`, `:557-560`, `:1452-1477`,
    `:1496-1509`, `:1525-1530`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:58-99`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-41`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/RemovePreprocessorDirectivesRewriter.cs:17`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:36`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:743-775`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:299-330`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:349-354`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:681-698`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954`, `:2083`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml:2036`
- What happens today: a union declaration is unknown to every kind list on the compile-time code path, and the effect
  depends on where it is declared.
  - Nested in a compile-time type, the member switch lists five kinds as nested types (`:540-544`), so a union
    reaches the default case (`:557-560`) and is added through the default visit. No visitor override exists
    (`:204-252`) and the rewriter derives from the Roslyn base rewriter through `SafeSyntaxRewriter.cs:36`, so the
    node is rebuilt and nothing throws. The copy is not literally verbatim, because the expression-level and
    attribute-level overrides of the rewriter still apply, but every member-level treatment is skipped: no exclusion
    of run-time-only members, no manifest entry, and no template compilation for a template declared inside it.
  - Nested in a run-time type, `PopulateNestedCompileTimeTypes` un-nests a compile-time class after checking that it
    is private and derives from a type fabric (`:274-354`), and reports the diagnostic for a compile-time struct,
    interface, record, record struct, enumeration or delegate (`:356-370`). A union reaches the default case
    (`:372-375`) and is silently ignored, so it is neither un-nested nor reported, and compile-time code that
    references it fails to compile.
  - Declared at namespace or file level, `SyntaxKindExtensions.IsTypeDeclaration` lists the same five kinds
    (`SyntaxKindExtensions.cs:33-41`), so a union is copied without the compile-time annotation and the whole
    namespace is dropped when the union is its only compile-time member (`:1496-1509`), a top-level union is removed
    from the compile-time tree (`:1525-1530`), and `FindCompileTimeCodeVisitor` classifies a syntax tree whose only
    compile-time declaration is a union as containing no compile-time code
    (`CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:58-99`).
  The annotator and the validator have comparable gaps, and their lists are neither identical to the rewriter's nor
  to each other's: the annotator has no interface override (`TemplateAnnotator.cs:743-775`) and the validator has no
  enumeration or delegate override (`TemplatingCodeValidator.Visitor.cs:299-330`). A union therefore receives no
  scope annotation, and its modifiers and base list are not verified.
- Roslyn checks the union feature in two places, so the failure is loud while the compile-time language version stays
  below the required one. The parser recognises the contextual keyword only when the feature is enabled for the tree
  being parsed, and the declaration table repeats the check on the node, reading the language version of the tree
  that contains it. Because the compile-time trees are created with fresh parse options and are never re-parsed
  (`CompileTimeCompilationBuilder.cs:349-354`), a copied union produces a feature-availability error at the `union`
  keyword. The silent consequences appear once the compile-time language version enables unions.
- Consequence: a diagnostic reported while the compile-time language version is below the required one, and silent
  wrong output afterwards. In the silent state the manifest entry, the scope classification and the template
  transformation are missing, and a nested compile-time union is dropped without a word.
- Proposed change: add the union to six sites, not three. The list is `SyntaxKindExtensions.IsTypeDeclaration`
  (`SyntaxKindExtensions.cs:33-41`, which also repairs the namespace member walk, the member filter of the
  compilation unit and the nested scan of the compile-time code finder), the override list of
  `FindCompileTimeCodeVisitor` (`:89-99`), the member switch of the compile-time type transformation (`:540-544`),
  the reported-kind group of `PopulateNestedCompileTimeTypes` (`:356-370`), the annotator
  (`TemplateAnnotator.cs:743-775`) and the validator (`TemplatingCodeValidator.Visitor.cs:299-330`). Changing
  `IsTypeDeclaration` affects eight other call sites, whose documentation and behaviour must be reviewed. Use a type
  test that excludes extension blocks, for example a test for a type declaration that is not an extension block,
  rather than a bare test for a type declaration: `ExtensionBlockDeclarationSyntax` also derives from
  `TypeDeclarationSyntax` in both grammars (`Syntax-5.10.0.xml:2083`, `Syntax-5.0.0.xml:2036`) and is deliberately
  excluded from `IsTypeDeclaration`, so a bare type test would route C# 14 extension blocks into the nested-type path
  and change their treatment. If visitor overrides are preferred instead, they can only be written after the move to
  the stable Roslyn, because naming the type or the kind against the current packages trips the experimental
  diagnostic and the Roslyn 5.0 variant does not have the API at all; the conditional compilation symbol would then
  follow the renumbered variant. The aspect tests belong under
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp15/Unions/`, beside the
  existing `CSharp14` directory, and they are skipped rather than failing until the latest variant recognises the
  language version (`TestOptions.cs:681-698`).
- Size: medium. The individual edits are small, but they span six sites, one of which is a shared extension property
  with eight other call sites, and the whole change is gated on the Roslyn variant strategy.
- Status: new work, and the sibling of finding CM-9 of theme 03, which reports the same two visitors from the code
  model side. The two must be delivered as one change, because they edit the same file.
- Verification: the code pass confirmed both default branches, corrected four details (the three kind lists are not
  identical, the accessibility and type-fabric checks belong to the run-time parent path only, the copy is not
  literally verbatim, and three further sites were omitted), and refuted the preferred form of the proposal because
  it would capture extension blocks. The semantics pass established where Roslyn performs the feature check and
  therefore that the current failure is a diagnostic rather than silence, refuted the usability of a C# 15 language
  version directive in a test today, and closed the report's open question about the type kind: a union produces a
  symbol whose type kind is `Struct`, so no throwing default arm over type kinds is reached. The scope pass found no
  implementation, no pull request and no issue, and noted that the same defect shape is reported in four other
  themes.
- Open questions: whether a union declared in a run-time type must be un-nested like a type fabric. The likely answer
  is no, and the existing diagnostic for compile-time structs should be reported instead.

### TP-7. Extension blocks in compile-time classes bypass member-level transformation

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:147-1778`,
    `:506-562`, `:515-516`, `:557-560`, `:832-875`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.Context.cs:14`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/RewriterHelper.cs:188-189`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/RemovePreprocessorDirectivesRewriter.cs:17`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:35`, `:64`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:588-611`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:324`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/ExtensionMembers/ExtensionMembers_IntroduceMethod.cs:32-66`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:2083`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml:2036`
- What happens today: an extension block member of a compile-time type matches no case of the member switch
  (`:506-562`) and reaches the default branch (`:557-560`), which visits the member. The rewriter overrides no
  visitor for an extension block, a method, a property or an indexer (the complete override list is at `:147-1778`,
  and the only other part of the partial class adds no visitor), so the block is copied by the Roslyn base rewriter.
  The copy is not literally verbatim, because the expression-level and attribute-level overrides still apply to the
  contents of the block. What does not happen is every member-level treatment performed by
  `TransformMethodDeclaration` and its siblings (`:832-875`): the exclusion of run-time-only members from the
  compile-time compilation, the manifest entry that records the scope and the template information, and the
  compilation of a template. No diagnostic descriptor covers this case. This is the C# 14 state, and an extension
  indexer, which is an indexer declaration inside the block, takes the same path. The explicit throw for an indexer
  (`:515-516`) applies only to an indexer that is a direct member of a compile-time type, and a second crash path for
  that situation exists at `RewriterHelper.cs:188-189`. A template inside an extension block is a narrow case in
  practice, because an extension block is legal only in a static class while an aspect class must be instantiable, so
  the reachable case is a compile-time static class that declares an extension block. No test pins the behaviour: in
  all seventeen aspect test sources that contain an extension block the first occurrence follows the target marker,
  and no test file containing an extension block carries a compile-time attribute.
- Consequence: no impact specific to C# 15. The gap already exists for C# 14 extension blocks, and extension indexers
  reuse the existing indexer declaration inside the existing extension block node, so they add no syntax node, no
  syntax kind and no symbol API for this theme to handle.
- The one C# 15 dependency belongs to the language version work rather than to the rewriter: because the extension
  block is copied into the compile-time compilation, an extension indexer declared in a compile-time class is
  accepted only when that compilation is parsed at C# 15, and today it is parsed at C# 14, so Roslyn reports the
  feature-availability check at the declaration.
- Proposed change: no C# 15 work in the rewriter. Record the pre-existing gap as an issue: either report a diagnostic
  when a template is declared inside an extension block, or support such templates. Full support is more than adding
  an override. The extension block node does derive from `TypeDeclarationSyntax` in both grammars
  (`Syntax-5.10.0.xml:2083`, `Syntax-5.0.0.xml:2036`) and the linker already routes it through the type-declaration
  path without a Roslyn conditional (`LinkerInjectionStep.Rewriter.cs:324`), but the compile-time type transformation
  is written for a named type: it asserts a declared symbol, derives the template-name scope from the symbol name,
  generates serializer members, adds the missing interface members, rewrites the base list and adds the fabric
  attribute, none of which applies to an extension block. Roslyn also gives the node an empty identifier, a null base
  list, and members that throw when the identifier or the base list is rewritten, so shared type-declaration code
  needs a specific path for it. Extracting the member loop of the compile-time type transformation and calling it for
  an extension block is the smaller change.
- Size: small for the diagnostic, medium for support.
- Status: decision required. The decision is whether to support templates declared inside an extension block or to
  reject them with a diagnostic. The work is not C# 15 work and should be filed as a bug or user story of its own,
  explicitly outside the C# 15 scope.
- Verification: the code pass confirmed the default branch, the absence of the three member overrides and the
  consequences, corrected the test project name cited by the report (there is no `Metalama.Framework.Tests.Integration`
  project) and the word "verbatim", and added the exclusion of run-time-only members as a consequence more likely to
  be met than the template one. The semantics pass confirmed against `dotnet/csharplang` and `dotnet/roslyn` that
  extension blocks are a C# 14 feature and that extension indexers extend the member grammar only, and added the
  compile-time language version dependency and the restricted behaviour of the node. The scope pass found no
  implementation, no pull request and no issue, and recorded that #1159, #1160 and #1105 are precedents rather than
  coverage.
- Open questions: none in this theme. Outside it, C# 15 makes extension indexers legal while the advice factory
  unconditionally rejects introducing an indexer into an extension block, which belongs to theme 04.

### TP-8. Raising the template language to C# 15

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-43`, `:45`, `:50`,
    `:52-62`, `:149-159`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60`, `:62-71`, `:111`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-39`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:51`, `:58-79`, `:106`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:167-181`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:127`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/LanguageOptions.cs:30`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Services/CompilationContext.cs:181`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-90`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:235-247`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:349-354`, `:360`,
    `:425-433`
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:218`
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:681-699`
  - `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestLanguageVersionProvider.cs:12`
  - `Directory.Build.props:11-16`, `Directory.Packages.props:23`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:93`
  - `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:36-42`
  - `eng/RoslynVersions/Roslyn.5.0.0.props:8-9`, `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`
  - `Metalama.Framework/docs/testing.md:58`, `Metalama.Framework/docs/updating-roslyn.md:36`,
    `Metalama.Framework/docs/extensibility.md:116`, `Directory.Packages.md:213`
- Where the decision is made: `TemplateCompiler.TemplateLanguageVersion` is the compile-time language version of the
  language version provider (`TemplateCompiler.cs:51`), overridden by the `MetalamaTemplateLanguageVersion` property
  when that value is contained in the supported set (`:58-79`). The provider returns the minimum of the version
  derived from the software development kit and the project's own language version (`LanguageVersionProvider.cs:54-71`),
  and on the desktop MSBuild path it uses the maximum version of the bundled Roslyn (`:111`). The same value becomes
  the maximal acceptable language version of the version checker (`TemplateCompiler.cs:106`) and the parse-option
  version of the compile-time compilation (`CompileTimeCompilationBuilder.cs:349-354`, `:360`, `:425-433`).
- What happens today: the plumbing stops at C# 14, and no Roslyn that Metalama consumes can accept C# 15 at all.
  `LanguageVersion.CSharp15` was added to `dotnet/roslyn` `main` on 2026-08-11, later than the consumed prerelease
  and later than the stable 5.9.0. In both variants the internal validity check has no case for the value 1500, the
  mapping from a specified to an effective version returns an unrecognised value unchanged, and the parse options
  report the bad-language-version error while formatting the raw number. The six C# 15 features are gated on
  `LanguageVersion.Preview` in the consumed build. The variant problem therefore applies to both variants today, not
  only to the Roslyn 5.0 one, and a test that requests version 15.0 is skipped everywhere
  (`TestOptions.cs:681-699`).
- Consequence: no impact until the constants are raised. If they are raised before the latest variant moves to
  Roslyn 5.12, the consequence is a compiler diagnostic on the compile-time compilation and on every aspect test
  parse, on both variants, plus the argument exception of `ToDisplayStringSafe` in the LAMA0051 and LAMA0052 paths
  and the mismatches of the two expectation files that pin the list of supported versions.
- Proposed change: gate the whole change on the latest variant moving to Roslyn 5.12, then make six edits.
  1. Add `CSharp15 = (LanguageVersion) 1500` to `AllLanguageVersions.cs:14-18` and use it in
     `SupportedCSharpVersions.Latest` and `All` (`SupportedCSharpVersions.cs:31-43`). The numeric cast is required
     for both variants, not only for the lower one, because no consumed Roslyn defines the member.
  2. Map the software development kit major version 11 to C# 15 in `LanguageVersionProvider.cs:54-60`, keeping
     major version 10 at C# 14, so that a .NET 10 host keeps working.
  3. Make the two edits of TP-2 in `ToLanguageVersion` and `GetMaxLanguageVersion`.
  4. Add the display case for the value 1500 to `LanguageVersionExtensions.cs:33-39`, without which LAMA0051 and
     LAMA0052 throw instead of being reported (`GeneralDiagnosticDescriptors.cs:235-247`,
     `SupportedCSharpVersions.cs:45`).
  5. Add `15.0` to the accepted implicit language versions of `Metalama.Framework.targets:118`, otherwise a
     `net11.0` project is rewritten to C# 12 with a warning.
  6. Update the two expectation files that pin the list of supported versions.
  Resolve the variant problem by deriving `Latest` at run time from the current Roslyn API version, which is a
  per-variant constant compiled from a per-variant generated file (`Generator.cs:93`,
  `GenerateMetaSyntaxRewriter.cs:36-42`), rather than by a preprocessor branch. Derive `All` from `Latest` as well,
  because a statically declared set containing C# 15 would let the Roslyn 5.0 variant accept a template language
  version that its Roslyn cannot parse. If the preprocessor form is chosen instead, the documented doctrine has to be
  amended in six places: `Metalama.Framework/docs/testing.md:58`, `Directory.Packages.md:213`,
  `Metalama.Framework/docs/extensibility.md:116`, `Metalama.Framework/docs/updating-roslyn.md:36`,
  `eng/RoslynVersions/Roslyn.5.0.0.props:8-9` and `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`. Keep
  `MetalamaTemplateLanguageVersion` at 14.0 (`Directory.Build.props:11-16`) while `RoslynApiMinVersion` is 5.0.0
  (`Directory.Packages.props:23`).
- Size: medium for the constants, the tests and the doctrine text. It is not startable on its own, because it is
  blocked on the move of the latest variant to Roslyn 5.12, which is the separate work described by
  [`updating-roslyn.md`](../updating-roslyn.md).
- Status: decision required. The decision is the default template language of a `net11.0` project: either keep the
  default at C# 14 and require `MetalamaTemplateLanguageVersion=15.0` as an opt-in until `RoslynApiMinVersion` is
  5.12, or accept that the compile-time assembly of a C# 15 project cannot be built inside a Roslyn 5.0 host such as
  Rider 2026.2. That failure mode is now known: such a host reports the bad-language-version compiler diagnostic and
  parses the source as C# 14, so it neither throws nor accepts the version silently.
- Verification: the code pass confirmed every cited site and the whole decision path, corrected one line citation
  (`BaseTestRunner.cs:218`, not 216), added five further consumers of `Latest` and the six documented statements of
  the doctrine, and showed that the consequence is wider than the report states, because `Latest` is the parse-option
  version of the whole test suite and the fallback of four project-option sites. The semantics pass confirmed the
  numeric value and the display string against `dotnet/roslyn` `main`, refuted the assumption that the currently
  consumed Roslyn can host the change, corrected every occurrence of 5.10 as a capability gate to 5.12, and resolved
  the report's second open question. The scope pass found the constants unchanged on the branch, no pull request and
  no issue, and recorded #1896 and #1881 as the immediate predecessors under the open meta-issue #1921.
- Open questions: the mapping of the software development kit major version 11 to C# 15 is an anticipation rather
  than a verified fact. The Roslyn version of the general-availability .NET 11 software development kit is not
  published, and the only data point, the preview 5 tag of 2026-06-01, carries Roslyn 5.8.0, which has no C# 15.

### TP-9. The design-time hashers do not hash the stripped declarations

- Where:
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:615-712`, `:656-663`, `:714-723`, `:726-736`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1148-1153`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/DiffStrategy.cs:73-84`, `:157-159`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/BaseCodeHasher.cs:19`, `:27-30`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/CompilationChanges.cs:203-225`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/DesignTimeAspectPipeline.PipelineState.cs:208-245`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs:39`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/HasherTests.cs`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests.5.0.0/Metalama.Framework.Tests.UnitTests.5.0.0.csproj:4`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:10`
- What happens today: the generator emits, for every node of the stripped grammar, an override whose body consists
  only of the per-field hashing calls and which never calls the base walker (`Generator.cs:615-712`). The generated
  break-statement visitor therefore hashes only the attribute lists, the keyword and the semicolon, so a change of
  the label of a compile-time `break` in a template does not change the compile-time hash, the diff strategy treats
  the tree as unchanged (`DiffStrategy.cs:73-84`), and the compilation changes record nothing
  (`CompilationChanges.cs:203-225`). For the node types that have no override the Roslyn default visit walks the
  child nodes but not the tokens, because the hasher passes no depth (`BaseCodeHasher.cs:27-30`) and the base walker
  defaults to node depth (`SafeSyntaxWalker.cs:39`). Every token of a union declaration is consequently ignored, not
  only the identifier: renaming a union, changing its modifiers and changing its keyword are all invisible to both
  hashers, and a union with no members contributes nothing at all to the hash. The run-time hasher additionally skips
  the content of a field whose declared type is a block, an arrow expression clause or an equals value clause
  (`Generator.cs:656-663`, `:714-723`), so a labeled `break` inside a member body is invisible to it for a second
  reason. The exception is a file of top-level statements, whose statement field is declared as a statement and is
  therefore not skipped (`Syntax-5.10.0.xml:1148-1153`).
- Consequence: silent wrong output at design time. Because the hash is the only gate that decides whether a document
  is considered changed, a missed change leaves the compile-time assembly or the generated partial classes stale
  until an unrelated edit changes the hash, and the path that would require an external build
  (`DesignTimeAspectPipeline.PipelineState.cs:208-245`) is never entered, so no diagnostic and no warning is
  produced.
- Proposed change: none beyond TP-1. Regeneration closes the union, with-element and labeled-jump cases without a
  hand-written change, because the union identifier is a non-trivial token and is then hashed by name
  (`Generator.cs:726-736`) and the labels are then visited. After regeneration, add two cases to
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/Utilities/HasherTests.cs`: a union rename and a
  change of the label of a `break` statement, covering both hashers. Two constraints apply. The whole test source is
  compiled into the Roslyn 5.0 variant project by a wildcard
  (`Metalama.Framework.Tests.UnitTests.5.0.0.csproj:4`), where the syntax does not parse, so the new cases must be
  enclosed in a conditional compilation block for the latest variant (`eng/RoslynVersions/Roslyn.5.10.0.props:10`).
  And the existing theory parses with the default parse options, which carry the latest supported version, so the new
  cases need parse options at the language version that accepts unions and labeled jumps, that is a parse-options
  parameter or a separate theory.
- Size: small.
- Status: new work, limited to tests, and a member of the regeneration cluster owned by theme 01. Finding DT-1 of
  theme 05 proposes the same regeneration as the remedy for the unhashed members, so the two are one story.
- Verification: the code pass confirmed that the generated overrides never call the base walker, that the hash is the
  only change gate and that the miss is therefore silent, and corrected two details: the run-time hasher does reach a
  labeled jump in top-level statements, and the union case is broader than the identifier because no token is hashed
  at all. The semantics pass confirmed that the label is a child node while the union name is a token, so the two
  cases are missed for different reasons, and corrected the claim that regeneration closes every case: the unsafe
  expression keeps its marker and stays unhashed, which is acceptable for 2027.0 because the feature is preview only.
  The scope pass found no test, no pull request and no issue, and identified #1881 as the change that introduced the
  stripping.
- Open questions: none.

### TP-10. The `closed` modifier and patterns over union types need nothing from the template compiler

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/MetaSyntaxRewriter.cs:137`, `:180-204`, `:269-294`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/MetaSyntaxRewriter.MetaSyntaxFactoryImpl.cs:78-82`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:1508-1555`, `:2877-2964`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:864`,
    `:669-696`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:279`, `:349-354`,
    `:410-434`, `:448`, `:515-528`, `:599-651`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:45-70`
  - `Metalama.Framework/docs/compile-time-target-frameworks.md`
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:118-156`, `:514`
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:626`
- What happens today: modifiers are a token list and are transformed token by token
  (`MetaSyntaxRewriter.cs:180-204`), and a keyword token is emitted through a syntax kind rendered by name at run
  time (`MetaSyntaxRewriter.cs:137`, `:269-294`,
  `MetaSyntaxRewriter.MetaSyntaxFactoryImpl.cs:78-82`). The grammar declares no kind list on any modifiers field
  (`Syntax-5.10.0.xml:626`) and contains no entry for the `closed` keyword, so neither the generator nor the version
  checker is involved (`Generator.cs:118-156`, `:514`). The path is in any case unreachable for this modifier,
  because the template compiler is invoked only on method declarations, accessor declarations and variable
  declarators (`CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:864`) and `closed` is a
  type-declaration modifier. A `switch` or an `is` over a union value uses the existing pattern nodes, whose meaning
  changes but whose shape does not, and the annotator derives the scope from the switched or tested expression
  (`TemplateAnnotator.cs:1508-1555`, `:2877-2964`). In compile-time scope a `closed` compile-time class is copied
  with its modifiers unchanged (`:669-696`) into a compile-time compilation whose parse options carry the
  compile-time language version, and every compile-time tree is written to disk and re-parsed from its text before
  emission (`CompileTimeCompilationBuilder.cs:515-528`), so a C# 14 parse of a closed class fails there and the
  failure surfaces as LAMA0222 together with the underlying compiler errors (`:599-651`).
- Consequence: no impact on templating, and a diagnostic on the compile-time compilation. After TP-8 raises the
  compile-time language version, the diagnostic changes rather than disappearing: Roslyn requires two compiler
  attributes for every closed type and does not synthesize them, while the compile-time compilation always targets
  `netstandard2.0` (`Metalama.Framework/docs/compile-time-target-frameworks.md`), so a missing-required-member error
  is reported instead. This last statement is an inference from `dotnet/roslyn` `main` and is plausible rather than
  verified.
- Proposed change: none for the template compiler and the syntax generator. Whether the code model exposes `closed`
  belongs to theme 03. If closed classes are to be usable in compile-time code after TP-8, add the two attributes to
  the embedded system-types trees under a language version gate, in the manner of the existing gate at
  `CompileTimeCompilationBuilder.cs:425-428`. Whether one of the two is already among those trees could not be
  verified, because the system-types assets are not present in this checkout.
- Size: none for this theme. Small and optional for the compile-time compilation, and only if closed compile-time
  classes are to be supported rather than diagnosed.
- Status: decision required, and the decision belongs to theme 03: whether Metalama supports closed hierarchies in
  2027.0 at all. This finding records only that the template compiler needs nothing either way.
- Verification: the code pass confirmed the modifier path, showed that it is unreachable for this modifier rather
  than merely unaffected, and removed the report's assumption about where Roslyn checks a modifier feature by showing
  that the compile-time trees are re-parsed before emission, so the diagnostic outcome is verified. The semantics
  pass confirmed against `dotnet/csharplang` and `dotnet/roslyn` that the modifier introduces no node and that the
  union proposal introduces no pattern syntax, confirmed that the feature check is a semantic check reporting CS9327,
  and added the missing-required-member correction. The scope pass found nothing implemented, in progress or tracked,
  and recorded that the finding is a negative scope statement rather than a story.
- Open questions: none.

## Withdrawn findings

No finding of this theme was withdrawn. Every one of the ten findings of the original report survived the three
verification passes, and none was refuted at its core. Several statements inside them were refuted and are corrected
above; the four that most change the picture are recorded here so that a reader of the original report knows they
were considered.

The original report treated the stable Roslyn 5.10 as the version that brings C# 15. No stable Roslyn 5.10, 5.11 or
5.12 package exists on nuget.org, the November 2026 baseline is expected to carry Roslyn 5.12, and the removal of the
experimental markers is a single Roslyn commit of 2026-08-11 rather than a consequence of a package becoming stable.
Every version number in TP-1, TP-2, TP-6 and TP-8 is corrected accordingly.

The original report proposed a consistency check that fails when the grammar file still declares an experimental
declaration while the consumed package version carries no prerelease label. Both halves of that rule are false: the
unsafe expression keeps its marker permanently, and the stable 5.9.0 assemblies still carry markers on the union,
with-element and unsafe API. The check as proposed would fail a correct configuration and is replaced by a
documentation sentence and a reformulated guard.

The original report stated that a template containing labeled `break` is rejected by the parser before the annotator
runs, and left open whether the compile-time compilation accepts such a statement silently. Roslyn checks the feature
in the binder and not in the parser, so the annotator always sees the statement and always misclassifies it, and the
compile-time compilation re-checks the feature on its own parse options and therefore fails loudly. The same
correction resolves the equivalent open question of TP-5 about the unsafe expression.

The original report stated that a test annotated with the C# 15 language version is skipped on the Roslyn 5.0 variant
only. It is skipped on every variant today, because neither consumed Roslyn parses that version string, so such a
test suite would report success while executing nothing.

## Non-findings

The following were checked and found unaffected. The line references are those of the original report and were
re-verified only where a finding above depends on them.

- `CompileTimeCodeFastDetector` inspects only using directives and returns false from its default visit
  (`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCodeFastDetector.cs:45-83`), so new node
  kinds cannot affect it.
- The fallback of the template annotator for an unknown node kind is `DefaultVisitImpl`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:627-643`), which visits the
  children and combines the scopes of the expression children (`:686-697`); it neither throws nor reports. The
  fallback of the template compiler rewriter is the Roslyn base visitor, and a crash occurs only where that visitor
  casts a transformed child to a node type, which is TP-3 and TP-4. The default branch of the transformation helper
  throws an assertion failure only for a non-transformed node that is neither an expression, an argument nor a
  statement (`Metalama.Framework/src/Metalama.Framework.Engine/Templating/MetaSyntaxRewriter.cs:131-132`); a
  with-element in compile-time scope inside a run-time collection expression would reach it, which is the same crash
  class as TP-4.
- `Metalama.Framework.Analyzers`, which is shipped to users, registers only operation actions
  (`Metalama.Framework/src/Metalama.Framework.Analyzers/ImmutableContractAnalyzer.WriteSites.cs:61-70`,
  `Metalama.Framework/src/Metalama.Framework.Analyzers/DurableContractAnalyzer.UseSites.cs:80-92`) and has no switch
  over a syntax kind or a type kind with a throwing default.
- `Metalama.Framework.Engine.Analyzers` is an internal-only package. Its three throwing defaults
  (`Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/MetalamaAssertionAnalyzer.cs:83`,
  `Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/MetalamaPerformanceAnalyzer.cs:80`, `:157`) switch over
  operation types that the registration restricts, and the kind-check optimization analyzer registers syntax actions
  for three kinds only and returns false for unknown shapes.
- `AdditionalDiagnosticAnalyzer`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Analyzers/AdditionalDiagnosticAnalyzer.cs:31`, `:43-46`) returns
  early for any type kind other than class, struct or interface.
- Every file of the Roslyn 5.0 generated directory is unchanged by the stable grammar, because it derives from
  `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml`
  (`eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:30-37`).
- The stubs fallback include (`Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:38`,
  `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj:35`) and the property
  that would produce it (`eng/src/Program.cs:168`) are declared, but nothing under `eng/src` produces a stubs
  directory, so they are inert.
- `SyntaxTreeStructureVerifier.VerifyMetaSyntax`
  (`Metalama.Framework/src/Metalama.Testing.AspectTesting/SyntaxTreeStructureVerifier.cs:26-46`) and
  `SyntaxFactoryDebugHelper`
  (`Metalama.Framework/src/Metalama.Framework.Engine/SyntaxGeneration/SyntaxFactoryDebugHelper.cs:19-31`) use the
  current Roslyn API version and the default parse options, so they follow TP-1 and TP-8 automatically.
- `ReferenceIndexWalker.DefaultVisit`
  (`Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs:97-101`) walks the
  children of an unknown kind and does not throw. The same is true of `TextSpanClassifier.DefaultVisit`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Formatting/TextSpanClassifier.cs:278-295`), but the classifier
  is nevertheless a finding elsewhere: it is reported as LK-10 by theme 04 and grouped with CM-7 under theme 03,
  because its per-kind helpers never reach a union declaration.
- `TemplatingCodeValidator.Visitor.VisitCore`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:95-131`) validates
  references after visiting the children and has no per-kind switch that throws. Its only throwing switch (`:1111-1128`)
  is reached from the suppression reporting (`:1086`) for declaration nodes that the visitor's own overrides pass in
  (`:478-813`), and a union declaration has no such override, so it does not reach it. The absence of that override
  is itself part of TP-6.
- The compiler entry point does not stop on input errors
  (`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/SourceTransformer.cs:91-140`), but the template
  compiler does (`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:159-217`). Two
  statements of the original report about reachability are corrected above: the language version check has a single
  caller in the compile-time pipeline
  (`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:177`), so the
  design-time pipeline never refuses the language version, and the template annotator runs on erroneous code on
  purpose, so a misclassification happens even when the template is later rejected.
- The aspect test infrastructure tolerates a language version that the running Roslyn does not recognise by setting a
  skip reason (`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:681-699`). That tolerance is
  adequate while C# 15 does not exist, and it becomes a hazard once a C# 15 suite exists, because the suite would be
  skipped silently on every variant; that hazard is owned by theme 06.

## Related themes

- The renumbering of the latest Roslyn variant to the stable 5.12, which is what causes the regeneration described by
  TP-1 and TP-9, is owned by theme 01, together with findings LV-12, LV-13 and LV-14 of that theme, DT-3 and DT-8 of
  theme 05 and PR-1 of theme 07. This document describes only what regeneration changes in the syntax generator, the
  template compiler and the design-time hashers.
- The C# language version tables are owned by theme 01, which carries findings LV-2, LV-3, LV-6 and LV-7 alongside
  TP-2 and TP-8 of this document and DT-4 of theme 05. TP-2 is a subset of TP-8 and must not be scheduled separately.
- The Roslyn variant gating strategy, that is how the engine may name an API member that exists only in the latest
  variant, is finding CM-10 of theme 03. It is a prerequisite of TP-3, TP-5 and TP-6, each of which must otherwise
  read the new syntax without naming it.
- TP-6 is the sibling of finding CM-9 of theme 03, which reports the same two visitors of the compile-time
  compilation builder from the code model side. The two edit the same file and are one change.
- The inventory of syntax visitors that inherit the Roslyn dispatch and therefore never observe a union declaration
  is finding CM-7 of theme 03, with LK-10 of theme 04 as one of its members. The annotator and validator gaps
  recorded inside TP-6 belong to that inventory and share its gating mechanism.
- TP-3 is the template half of a feature whose other halves are finding LK-9 of theme 04, which reports that the
  inlining substitution copies user labels verbatim so that a template label may collide with a target label, and
  finding UT-17 of theme 06, which records that the metric providers count nodes generically and need only tests.
- TP-7 belongs to a cluster of switches over declaration kinds that fall through, owned by theme 05, together with
  findings DT-5 of that theme and PR-10 and PR-11 of theme 07. None of them is caused by C# 15 and all are reachable
  today.
- TP-10 belongs to the closed-hierarchies cluster owned by theme 03, together with findings CM-4 and CM-5 of that
  theme, LK-5 of theme 04 and UT-15 of theme 06. Its contribution is the verified statement that this theme needs no
  work.
- The behaviour of the test harness when a test requests a language version that the running Roslyn does not
  recognise is owned by theme 06 (findings LV-8, DT-7 and UT-19). It applies to every test proposed in this document.
- The Roslyn public API delta and the semantics of each C# 15 feature are recorded in
  [`analysis-reports/08-roslyn-api-delta.md`](analysis-reports/08-roslyn-api-delta.md).
