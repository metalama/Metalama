# Trivia and Formatting

This document describes how Metalama manages whitespace, trivia, and C# code formatting across the generation and output pipeline. For the broader pipeline context, see [pipeline.md](pipeline.md); for the short version of this document, see the "Syntax Generation and Simplification" section in the repository root `CLAUDE.md`.

## Overview

Metalama intentionally separates two concerns:

- **Correctness**, handled during syntax generation. The generator produces over-specified, explicit C# — fully-qualified type names, redundant casts, explicit `new DelegateType(methodGroup)` wrappers, and so on. This guarantees the Roslyn compiler accepts the tree regardless of surrounding context.
- **Presentation**, handled in a later simplify-and-format pass (`CodeFormatter`). That pass removes the redundant qualifications and reflows whitespace only when the output is meant for human eyes.

Trivia (whitespace, comments, end-of-line markers, directives) follows the same split. There are three handling modes, selected per project and per pipeline — from "don't emit any trivia at all" (fastest) to "fully indent and simplify" (human-readable).

## Why three modes? The allocation budget

Roslyn syntax nodes are immutable green/red trees. Every `node.WithLeadingTrivia(...)`, `node.WithTrailingTrivia(...)`, or `node.NormalizeWhitespace()` call allocates — a new green node, usually a new token, and a new `SyntaxTriviaList`. Across a compilation that passes through thousands of generated nodes, decorating every node with elastic EOLs, indentation whitespace, and line-feed separators is measurable GC pressure and CPU time.

The `Optional*` / `*IfNecessary` vocabulary pervasive throughout `Metalama.Framework.Engine.SyntaxGeneration` and `Metalama.Framework.Engine.Utilities.Roslyn` exists to turn those allocations into no-ops when the output will not be textualized, or doesn't need to be pretty. Choosing a formatting mode is therefore primarily choosing a trivia-allocation budget, not just a styling preference.

Three diagnostic analyzers enforce this discipline (`MetalamaPerformanceAnalyzer`, warning range `LAMA0830`–`LAMA0832`):

| Analyzer | Rule | Preferred alternative |
|---|---|---|
| `LAMA0830` | `NormalizeWhitespace` is expensive | `NormalizeWhitespaceIfNecessary` |
| `LAMA0831` | Avoid chained `With*` calls on syntax nodes | `PartialUpdate` extensions (generated) |
| `LAMA0832` | Avoid `WithLeadingTrivia`/`WithTrailingTrivia` | `WithOptional*` / `WithRequired*` helpers |

These warnings are *suggestions* in engine code (`.editorconfig` at `Metalama.Framework.Engine/.editorconfig`) and silenced entirely in test projects (`Metalama.Testing.UnitTesting/.editorconfig`, `Metalama.Testing.AspectTesting/.editorconfig`). Production engine code that bypasses them uses `#pragma warning disable LAMA083x` with an explanation.

## The three modes — `CodeFormattingOptions`

Defined in `Metalama.Framework.Sdk/Formatting/CodeFormattingOptions.cs`:

```csharp
public enum CodeFormattingOptions
{
    Default,    // Syntactically correct; no pretty formatting.
    None,       // No text output required, only a syntax tree.
    Formatted   // Proper indentation and spacing.
}
```

The enum is wrapped by `SyntaxGenerationOptions` (`Metalama.Framework.Engine/SyntaxGeneration/SyntaxGenerationOptions.cs`), which exposes two booleans that gate every trivia-related call site:

```csharp
internal bool WillBeTextualized => _codeFormattingOptions != CodeFormattingOptions.None;
internal bool WillBeFormatted   => _codeFormattingOptions == CodeFormattingOptions.Formatted;
```

Mapping:

| Mode | `WillBeTextualized` | `WillBeFormatted` | Typical use |
|---|---|---|---|
| `None` | false | false | AST-only flows (test validation; no textualization) |
| `Default` | **true** | false | Compiled-assembly output — fast, verbose, machine-readable |
| `Formatted` | true | **true** | Design-time, preview, HTML, live templates, compile-time assembly |

Two subtleties:

- A single predicate (`WillBeTextualized`) gates both per-node elastic-trivia emission *and* the `NormalizeWhitespace(elasticTrivia: true)` call during generation. Both are required whenever the tree will be textualized — `Default` and `Formatted` therefore behave identically at the per-node level: both produce trivia, both normalize. The difference between them lives entirely in `WillBeFormatted` and the later `CodeFormatter` pass (see §6). The XML doc on `WillBeTextualized` explains why normalization is still needed in `Formatted` mode: *Roslyn's `Formatter` reflows only elastic trivia and preserves non-elastic trivia verbatim — it does not synthesize new trivia between tokens that have none*. `NormalizeWhitespace(elasticTrivia: true)` is what sprays those elastic markers through the tree so the formatter has something to reflow.
- **Directives are always preserved, even in `None`.** Preprocessor directives (`#if`, `#pragma`, `#line`, etc.) affect compilation semantics, so the `WithOptional*` helpers check `ContainsDirectives()` and fall through to the real `With*` call when found.

### The directive-preservation invariant (load-bearing)

> **`#` directives must NEVER be dropped from the output, even when the syntax node that carries them as trivia is itself dropped or rewritten. The only trivia class that may be dropped is XML documentation comments.**

The reason this rule is absolute: `#if` / `#endif`, `#region` / `#endregion`, `#pragma warning disable` / `restore`, `#nullable enable` / `restore`, and `#line` come in **balanced pairs that span across nodes**. The opening directive is leading trivia of one node; the closing directive is trailing trivia of another. The two ends typically have *no syntactic relationship* — `#if FOO` may be leading trivia of the first `using` directive while `#endif` is trailing trivia of the EOF token, with hundreds of unrelated declarations in between. Dropping just one end produces invalid C# (e.g. CS1028 *Unexpected preprocessor directive*) even when every node in between is well-formed.

Because the rewriters in this codebase work declaration-by-declaration, no single rewriter has the global view needed to "drop both ends together". The only safe rule is therefore: **never drop directive trivia, even from a node you intend to remove**. Reattach the trivia to a sibling, the parent, or a synthesized stub. If a node is replaced (for example, the `RunTimeAssemblyRewriter` replacing a compile-time-only method body with `throw new NotSupportedException(...)`), the replacement node must carry over the original node's leading and trailing trivia in full — not just the whitespace, but every directive too.

XML documentation comments (`/// <summary>...`) are the *only* trivia that may be discarded. They have no spanning semantics, they are not load-bearing for compilation, and discarding them is a deliberate design choice (the run-time stripped assembly intentionally drops compile-time-only XML doc).

How the rule shows up in code:

- `WithOptionalLeadingTrivia` / `WithOptionalTrailingTrivia` already enforce the rule on the *attachment* side: they fall through to a real `With*` call whenever `ContainsDirectives()` is true, regardless of mode.
- The rule is *not* enforced on the *removal* side. Any rewriter that calls `node.WithLeadingTrivia(SyntaxTriviaList.Empty)` or replaces a node without forwarding its trivia is on the hook to verify (by inspection or by `ContainsDirectives`) that no directives are being dropped.
- When you visit a node and decide to drop it entirely (`return null` from a `CSharpSyntaxRewriter`), check the leading/trailing trivia for directives first. If there are any, attach them to a sibling node or to a no-op placeholder so the final tree still has them.

If you find yourself wanting to "fix" an unbalanced-directive bug by suppressing the surviving end, stop — the bug is upstream, in the rewriter that dropped the other end. Find that rewriter and forward the trivia instead.

Static instances: `SyntaxGenerationOptions.Formatted` (public) and `SyntaxGenerationOptions.Unformatted` (internal, `None`).

## Who picks which mode

Each pipeline overrides `AspectPipeline.GetSyntaxGenerationOptions()`:

| Pipeline | Mode | File |
|---|---|---|
| `CompileTimeAspectPipeline` | Project-configured (`IProjectOptions.CodeFormattingOptions`) | `Pipeline/CompileTime/CompileTimeAspectPipeline.cs:43` |
| `BaseDesignTimeAspectPipeline` | Always `Formatted` | `Pipeline/DesignTime/BaseDesignTimeAspectPipeline.cs:18` |
| `PreviewAspectPipeline` | Always `Formatted` | `Pipeline/DesignTime/PreviewAspectPipeline.cs:29` |
| `LiveTemplateAspectPipeline` | Always `Formatted` | `Pipeline/LiveTemplates/LiveTemplateAspectPipeline.cs:43` |

The compile-time assembly built by `CompileTimeCompilationBuilder` always uses `Formatted` regardless of the pipeline it is invoked from — its construction is not on the hot path, and the generated compile-time code is a debugging surface that should be readable.

Tests have one more lever: the `@TestUnformattedOutput` directive in aspect tests re-runs the pipeline in `None` mode to verify that the generator produces syntactically correct trees even when no trivia is emitted. This catches bugs where code generation silently relies on elastic EOLs for correctness.

The user-facing entry points on `IProjectOptions`:

- `CodeFormattingOptions` — the mode chosen above. Defaults to `Default` and is upgraded to `Formatted` when the user sets `FormatOutput=true` or `WriteHtml=true` in their project.
- `FormatCompileTimeCode` — independently triggers a `CodeFormatter` pass over the compile-time assembly after it is built.
- `WriteHtml` — triggers HTML rendering, which needs readable code.

The `CodeFormatter` service is only registered in the project service provider when at least one of these is enabled (`ServiceProviderFactory.cs`). In plain `Default`-mode compilation the formatter is never instantiated.

## Generation: conditional trivia

### `SyntaxGenerationContext` and cached trivia lists

`SyntaxGenerationContext` (`SyntaxGeneration/SyntaxGenerationContext.cs`) holds the active `SyntaxGenerationOptions` plus memoized trivia lists that respect them:

```csharp
[Memo]
public SyntaxTriviaList OptionalElasticEndOfLineTriviaList
    => this.Options.WillBeTextualized
        ? new SyntaxTriviaList( this.ElasticEndOfLineTrivia )
        : default;

[Memo]
internal SyntaxTriviaList TwoElasticEndOfLinesTriviaList
    => this.Options.WillBeTextualized
        ? new SyntaxTriviaList( this.ElasticEndOfLineTrivia, this.ElasticEndOfLineTrivia )
        : default;
```

In `None` mode these return `default(SyntaxTriviaList)` — an empty value-type instance, zero allocation. In any other mode the `[Memo]` attribute ensures the list is built once per context and reused at every call site.

### The `Optional` / `Required` / `IfNecessary` convention

The extension methods in `Utilities/Roslyn/SyntaxExtensions.cs` (lines ~156–319) follow a strict naming convention:

- **`*IfNecessary`** — checks a mode flag; no-op when the flag is off. Example: `NormalizeWhitespaceIfNecessary` no-ops in `None`.
- **`WithOptional*`** — checks `WillBeTextualized`; returns the original node unchanged when false, except when the trivia contains directives.
- **`WithRequired*`** — always allocates. Used when the trivia is load-bearing for syntax itself (pragmas on their own line, region markers).

Representative implementations:

```csharp
internal static TNode NormalizeWhitespaceIfNecessary<TNode>( this TNode node, SyntaxGenerationContext context )
    where TNode : SyntaxNode
{
    if ( !context.Options.WillBeTextualized ) return node;

#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
    return node.NormalizeWhitespace( elasticTrivia: true, eol: context.EndOfLine );
#pragma warning restore LAMA0830
}

internal static TNode WithOptionalLeadingTrivia<TNode>(
    this TNode node, SyntaxTriviaList leadingTrivia, SyntaxGenerationOptions options )
    where TNode : SyntaxNode
{
    if ( !options.WillBeTextualized && !leadingTrivia.ContainsDirectives() )
        return node;

    return node.WithLeadingTrivia( leadingTrivia );
}

internal static TNode WithSimplifierAnnotationIfNecessary<TNode>(
    this TNode node, SyntaxGenerationOptions options )
    where TNode : SyntaxNode
{
    if ( !options.WillBeFormatted ) return node;
    return node.WithSimplifierAnnotation();
}
```

The `ContainsDirectives` helper is a hand-rolled loop rather than `trivias.Any(t => t.IsDirective)` specifically to avoid allocations: the predicate would materialize a delegate, and calling `Any` via `IEnumerable<T>` boxes the struct enumerator. The hand-rolled `foreach` over `SyntaxTriviaList` uses its struct enumerator directly. This guard sits on the hot path of every optional-trivia attachment.

### `ContextualSyntaxGenerator`

`SyntaxGeneration/ContextualSyntaxGenerator.cs` wraps Roslyn's `SyntaxGenerator` with the "annotate then maybe-normalize" two-step and adds caching. A typical factory method looks like:

```csharp
internal ArrayCreationExpressionSyntax ArrayCreationExpression( TypeSyntax elementType, IEnumerable<SyntaxNode> elements )
{
    var array = (ArrayCreationExpressionSyntax) _roslynSyntaxGenerator.ArrayCreationExpression( elementType, elements );

    return array.WithType( array.Type.WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext ) )
        .NormalizeWhitespaceIfNecessary( this.SyntaxGenerationContext );
}
```

The generator also maintains a type-syntax cache (`_typeSyntaxCache` keyed by `IRef<IType>`) to avoid re-constructing identical type references — another allocation hedge orthogonal to the trivia mode.

### Elastic vs. non-elastic trivia

Every Roslyn `SyntaxTrivia` carries an "elastic" bit. The bit does not change what the trivia *is* (a space is a space, an EOL is an EOL); it is a marker of **provenance and intent** that tells downstream formatters whether they are allowed to alter it. The authoritative definition lives in the XML doc comments on [`SyntaxFactory.cs`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/SyntaxFactory.cs) in the `dotnet/roslyn` repo:

> Elastic trivia are used to denote trivia that was not produced by parsing source text, and are usually not preserved during formatting.
> — repeated on `ElasticSpace`, `ElasticTab`, `ElasticCarriageReturnLineFeed`, `ElasticLineFeed`, `ElasticWhitespace(text)`, etc.

> Syntax formatting will replace elastic markers with appropriate trivia.
> — on `ElasticMarker`

**Non-elastic trivia — verbatim, preserve as-is.**
This is what the Roslyn parsers emit. When you parse source text, every existing whitespace character, line break, and comment is materialized as non-elastic trivia that reflects the source exactly, character-for-character. Non-elastic trivia carries the contract: *"this matches what the user wrote; don't touch it."* `Formatter.Format` and friends leave it alone.

**Elastic trivia — hand-constructed placeholder, formatter may rewrite.**
This is what you produce with the [`SyntaxFactory.Elastic*`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/SyntaxFactory.cs) family — `ElasticSpace`, `ElasticMarker` (zero-width — "included automatically by factory methods when trivia is not specified"), `ElasticEndOfLine(eol)`, `ElasticCarriageReturnLineFeed`, `ElasticLineFeed`, `ElasticTab`, `ElasticWhitespace(text)`. A parser *never* produces elastic trivia; it only appears in programmatically built trees. Elastic trivia carries the contract: *"this was synthesized because a token has to be separated from its neighbors somehow, but the exact whitespace is a placeholder — a formatter is free to substitute, lengthen, collapse, or remove it."*

Concretely, when [`Formatter.Format`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.formatting.formatter) (or [`SyntaxNode.NormalizeWhitespace`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxextensions.normalizewhitespace)) runs, it walks the tree and reflows only the elastic trivia to match the active formatting rules; non-elastic trivia is preserved exactly. `NormalizeWhitespace` additionally rewrites every token's trivia to elastic as it runs (`elasticTrivia: true` — the parameter Metalama always passes), which is what makes it so expensive and why it produces output that the formatter can later re-reflow at will.

**How Metalama uses the distinction.**
Generated code is the classic "hand-constructed" case, so Metalama's generation pipeline emits elastic trivia throughout (`SyntaxGenerationContext.ElasticEndOfLineTrivia`, `OptionalElasticEndOfLineTriviaList`, `TwoElasticEndOfLinesTriviaList`). This is what lets the `CodeFormatter` pass (§6) freely reflow generated sections in `Formatted` mode while leaving user source untouched. Non-elastic trivia is used sparingly, and only where the exact spacing is semantically load-bearing — most notably around preprocessor directives, which must sit on their own line regardless of what the formatter would otherwise prefer.

Two practical consequences:

- **Elastic trivia is still an allocation.** The bit is a flag on the already-allocated trivia; emitting elastic EOLs is just as expensive as emitting non-elastic ones. The `Optional*` guards suppress *either* kind when no formatter will ever run.
- **Don't hand-write non-elastic whitespace into generated code unless you mean it.** If you use `SyntaxFactory.Space` or `SyntaxFactory.EndOfLine(...)`, the `CodeFormatter` won't touch it during a `Formatted` pass and your output may look wrong relative to the rest of the generated code. Prefer `context.ElasticEndOfLineTrivia` / `SyntaxFactory.ElasticSpace` / `SyntaxFactory.ElasticMarker`.

See also the Roslyn issue [#302 — "Elastic marker only when necessary"](https://github.com/dotnet/roslyn/issues/302) and [#2435 — "Whitespace and EndOfLine trivia should default to non-elastic"](https://github.com/dotnet/roslyn/issues/2435) for historical design discussions on when elastic is (and is not) the right default.

## Template expansion: active indentation

One place Metalama performs active pretty-printing during generation rather than deferring is `MetaSyntaxRewriter` (`Templating/MetaSyntaxRewriter.cs`). It maintains an indent stack so nested compile-time expressions emit with sensible indentation:

- A `Stack<string>` of indent prefixes; `Indent()` pushes a new `"    "` on top.
- `GetIndentation(bool lineFeed)` returns a trivia list combining the elastic EOL (optionally) and the current indent whitespace.
- A nested `IndentRewriter` uses an 80-character threshold to decide whether to break an `ArgumentList` across lines.
- Interpolation content is never indented — CRLF inside an interpolated string would change the literal's value.

This is the exception to the "defer to the formatter" rule: template output is read in compile-time-troubleshooting files and benefits from being indented even in `Default` mode.

## The `CodeFormatter` pipeline

When pretty output is required, `Metalama.Framework.Engine/Formatting/CodeFormatter.cs` runs a multi-stage pass. Public entry points:

- `FormatAsync(Document document, diagnostics?, reformatAll, ct)` — single document, partial-format capable.
- `FormatAsync(PartialCompilation compilation, ct)` — used by the compile-time pipeline; always calls `reformatAll: false`.
- `FormatAllAsync(Compilation compilation, ct)` — whole compilation, used at end-of-pipeline and in tests.

The pipeline runs five stages against a Roslyn `Solution`:

1. **Diagnostic annotations.** If diagnostics are passed, `FormattedCodeWriter.AddDiagnosticAnnotations` attaches them as `SyntaxAnnotation`s for downstream renderers (HTML output, preview UI).
2. **Custom simplifications** (`CodeFormatter.CustomSimplifier.cs`). A `SafeSyntaxRewriter` runs twice: first without a semantic model (syntactic patterns), then with a semantic model if the first pass set `RequiresSemanticModel`. Handles Metalama-specific simplifications:
   - `new Action(() => { ... })` → `() => { ... }` in target-typed positions
   - Redundant tuple casts (semantic comparison required)
   - Redundant nullable suppression (`x!` where `x` is already non-nullable)
3. **Import addition.** `ImportAdder.AddImportsAsync(document, Simplifier.Annotation, ...)` resolves annotated type references and adds the necessary `using` directives.
4. **Roslyn simplifier + fix-up.** `Simplifier.ReduceAsync(document, Simplifier.Annotation, ...)` removes unnecessary namespace qualifications and redundant casts on annotated nodes. `SimplifierFixer` then post-processes to work around a Roslyn bug where EOL trivia after `//` comments can be dropped. The simplifier then runs **once more** as a final polish — see `CodeFormatter.cs:159–183`.
5. **Reformat.** `Formatter.FormatAsync`. When `reformatAll == true`, the entire document is reflowed. When `reformatAll == false`, a `MarkTextSpansVisitor` walks the tree and classifies spans as `GeneratedCode` vs `SourceCode` based on annotations, and only the generated spans are formatted — preserving the user's original whitespace in their own code.

After the Solution-level pipeline, both `FormatAsync(PartialCompilation)` and `FormatAllAsync(Compilation)` normalize line endings back to each source file's original EOL style via `EndOfLineHelper.NormalizeEndOfLines`, because Roslyn's `Formatter` may have normalized them to `\r\n`.

Invocation sites:

- `CompileTimeAspectPipeline.cs:199–210` — runs `FormatAsync(resultPartialCompilation, ...)` only if `CodeFormattingOptions == Formatted || WriteHtml`. When enabled on a non-test build, reports the informational diagnostic `GeneralDiagnosticDescriptors.CodeFormattingEnabled` so users are aware they are paying the cost.
- `CompileTimeCompilationBuilder.cs` — runs it on the compile-time assembly when `FormatCompileTimeCode` is set.
- Design-time preview (`UserProcessTransformationPreviewService`) — passes `reformatAll: false` so only generated spans are touched.

## `FormattingAnnotations` — the bridge between generation and formatting

`Metalama.Framework.Sdk/Formatting/FormattingAnnotations.cs` defines the annotations that let the generator communicate hints to the later formatter:

- **`Simplifier.Annotation`** — Roslyn's "try to simplify me" annotation. The SDK can't reference `Microsoft.CodeAnalysis.Simplification` directly (it would pull in a workspace dependency), so `MetalamaEngineModuleInitializer` injects the annotation at engine startup:

  ```csharp
  FormattingAnnotations.Initialize( Simplifier.Annotation );
  ```

  `WithSimplifierAnnotation<T>` then attaches the injected annotation to a node. `WithSimplifierAnnotationIfNecessary` attaches it only in `Formatted` mode.
- **`SystemGeneratedCodeAnnotation`** / **`SourceCodeAnnotation`** — distinguish Metalama-emitted spans from user source. Used by `MarkTextSpansVisitor` in the `reformatAll: false` branch so the formatter only touches generated code.
- **`PossibleRedundantAnnotation`** — reserved for future use (marking locals/return statements that *may* be removable).
- Public helpers: `WithSimplifierAnnotation<T>`, `WithGeneratedCodeAnnotation<T>`, `WithSourceCodeAnnotation<T>`.

Annotations are themselves allocations on the node. `WithSimplifierAnnotationIfNecessary` skipping them outside `Formatted` mode is a double win: direct allocation saved, and the downstream simplifier has nothing to visit, so stages 3 and 4 of the `CodeFormatter` collapse to cheap no-ops.

## What the compiler sees vs. what the human sees

- The **Roslyn compiler** always sees the verbose, over-specified syntax tree. It doesn't care about whitespace at all — the emitted IL is identical regardless of mode.
- In **`Default`** mode, this verbose syntax is what hits disk (and is what the debugger sees if source is embedded). Fast, correct, ugly. The compiler's own subsequent normalization passes do any cleanup the IL layer requires.
- In **`Formatted`** mode, the simplify-and-format pass runs *before* the text is serialized, producing human-readable output. Cost is in the hundreds of milliseconds per document — acceptable because this mode is used for design-time preview, HTML rendering, and debug-output troubleshooting.
- In **`None`** mode, no text is produced at all. Only the tree exists for downstream analysis.

## Performance: rules of thumb for contributors

- Prefer the `Optional*` / `*IfNecessary` variants unless the trivia is semantically required.
- Route all whitespace normalization through `NormalizeWhitespaceIfNecessary`. Direct `NormalizeWhitespace` calls trip `LAMA0830` and are banned outside explicitly marked hot-paths.
- Use `PartialUpdate` (generated into `.generated/<roslyn>/...SyntaxNodePartialUpdateExtensions.g.cs`) instead of chained `With*` calls on a syntax node — a single `Update` allocates one node instead of N. Enforced by `LAMA0831`.
- Read trivia lists from `SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList` rather than constructing them per call — `[Memo]`-cached and allocation-free in `None` mode.
- If you catch yourself writing `node.WithLeadingTrivia(...)` in engine code, suspect a bug. Use `WithOptionalLeadingTrivia` / `WithRequiredLeadingTrivia` so the intent is explicit and the mode-aware shortcut is honored. Enforced by `LAMA0832`.

## Related

- [`pipeline.md`](pipeline.md) — where these modes are picked per pipeline and how transformations compose.
- [`linker-architecture.md`](linker-architecture.md) — the linker also produces syntax and must follow the same trivia rules.
- The "Syntax Generation and Simplification" section in the repository-root `CLAUDE.md` — a one-paragraph operational summary.
