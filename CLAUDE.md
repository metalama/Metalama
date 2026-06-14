# Claude Instructions for Metalama

## Prerequisites

Before starting work:

1. **Check plugin**: Verify the `eng@postsharp-engineering` and `metalama-dev@postsharp-engineering` plugins from `PostSharp.Engineering.AISkills` are available and load their _skills_ now. If not, ask the user to install them - it contains essential git workflow, build system, and release management knowledge, and general knowledge about developing Metalama.

   **IMPORTANT**: For git operations (commit, PR, merge) or when asked to "start working on issue X", ALWAYS read `eng.md` skill first to get correct conventions. Don't attempt commits or PRs without consulting it.

2. **Check branch**: Before making any modifications, verify you're on a feature branch (`topic/YYYY.N/XXXX-description`). If on `develop/*` or `release/*`, propose creating/switching to a topic branch first.

**Main solutions:**
- `Metalama.Backstage`: infrastructure (licensing, logging, telemetry)
- `Metalama.Framework`: core framework
- `Metalama.Extensions`: extensions built on the core framework
- `Metalama.Patterns`: aspects built on `Metalama.Framework`
- `Metalama.LinqPad`: LinqPad driver
- `Metalama.Migration`: PostSharp API with upgrade documentation
- `eng`: build orchestration (not a solution)

**Related repos** (in `..` or `../..`):
- `Metalama.Premium`: premium features
- `Metalama.Vsx`: Visual Studio Tools for Metalama
- `PostSharp.Engineering`: build orchestration SDK
- `Metalama.Documentation`: conceptual documentation
- `Metalama.Samples`: examples

## Building

- **Faster Framework build**: Use `Metalama.Framework.LatestRoslyn.slnf` instead of full solution

## Package Versioning

In `Directory.Packages.props`, dependencies fall into two categories:

- **API dependencies**: When Metalama is *hosted* (e.g., in Visual Studio), the host provides these dependencies. We must use minimum versions compatible with the lowest supported host version.

- **Runtime (latest) dependencies**: When Metalama *hosts itself* (e.g., standalone tools, tests), we provide the dependencies. These use the latest versions to avoid vulnerability warnings.

The `*LatestVersion` properties (e.g., `MessagePackLatestVersion`, `SystemMemoryLatestVersion`) are for runtime dependencies. Note that "latest" means latest at the time of updating - some packages like MessagePack are pinned to older major versions for compatibility (MessagePack 2.x for StreamJsonRpc compatibility).

## Dependency Injection

Custom immutable DI (not MEDI). Core types in `Metalama.Framework.Sdk/Services/`.

**Scopes:** `IGlobalService` (singleton), `IProjectService` (per-compilation), `IBackstageService` (infrastructure)

**Rules:**
- `WithService()` returns NEW provider (immutable) - use `WithServiceConditional` to avoid duplicates
- No constructor injection - resolve manually: `serviceProvider.GetRequiredService<T>()`
- Never require a service as a method parameter - report complex problems for user to study
- Register in `ServiceProviderFactory`, test with `AdditionalServiceCollection`

## Container Environment

When running in a container, `gh` is not available, and `git push` / `git fetch` over the network do not work either. **All GitHub network operations (push, fetch from remote, gh API calls, etc.) MUST go through the host-approval MCP server (`mcp__host-approval__execute_command`).**

**If the MCP server is unavailable** (e.g., disconnected, the deferred-tool reminder says it's gone): STOP. Tell the user MCP is unavailable and ask them to run the command from the host. **Do NOT fall back to direct `git push` / `gh` via Bash** — it will fail, and bypassing MCP violates the human-in-the-loop policy. Treat MCP-unavailability as a policy boundary, not a tool-selection problem.

## Working on GitHub Issues

When starting work on a GitHub issue:
1. Read all details about the issue online
2. Check conceptual documentation under `../Metalama.Documentation/content`
3. Create a branch: `topic/YYYY.N/XXXX-short-description`
4. Check CLAUDE-TODO.md before preparing PR
5. Create issues promptly when discovering bugs during development

## Debugging Build Issues

1. **Check troubleshooting files**: Look at `%TEMP%\Metalama\CompileTimeTroubleshooting\...\errors.txt` for actual errors
2. **File locks**: After failed builds, run `Build.ps1 tools kill` before retrying
3. **Trace data flow**: For MSBuild issues, trace from `.csproj` → `.targets` → Engine code
4. **Cross-solution changes**: Run `Build.ps1 build` early rather than discovering issues incrementally. Claude may run `Build.ps1 build` itself in this environment (this overrides the general `eng` skill guidance that says to ask the user). Because it is long-running, start it in the background (`run_in_background`) with a high timeout and continue with other work until it completes.
- When working on an issue creat a file called <Isse-number>-TODO.md to track progress.
- don't include *-TODO.md in commits
- After a full build with `Build.ps1 build` (Claude may run it, preferably in the background), the msbuild binlogs are under artifacts/logs
- when you start working on an issue, mark the status as In Progress and make sure it is assigned to me
- in tests never use hardcoded delays, always use other sync mechanims such as barriers, taskcompletionsource, sync points
- Never await without cancellation token - ever
- Github comments and issues and PRs must be signed by Claude - not commits. No ad link, just the signature `— Claude for @gfraiteur`.
- don't loose time solving cosmetic warnings (such as redundant usings) until the finalizing stage of a commit
- `Build.ps1 build` does not build test projects, only packable projects.
- `Build.ps1 test` implicitly does a clean rebuild (not incremental), so do NOT chain it after `Build.ps1 build` — they overlap. After `Build.ps1 build`, run individual test projects with `dotnet test <project> --no-build`. Only re-run `Build.ps1 build` when you need a cross-solution rebuild.

## Nested Types in Separate Files

When a class has nested types that are large enough to warrant their own file, use the **partial class** pattern:

- The nested type stays as a `private` (or appropriate access) nested class
- Place it in a separate file named `OuterClass.NestedType.cs`
- The file uses `partial class OuterClass` to contain the nested type
- Use block-scoped namespace syntax (not file-scoped) in the nested type file

Example: `TemplateExpansionContext.ProceedUserExpression.cs` contains `private sealed class ProceedUserExpression` inside `internal sealed partial class TemplateExpansionContext`.

## Aspect Test Discovery

Aspect tests in `Metalama.Framework.Tests.AspectTests` are discovered by a custom xUnit test runner based on `.cs` file paths under `Tests/`. Test names are the **file name without extension** (e.g., `ReplaceParameter_Covariant`), not the full path. To run a specific test:

```bash
dotnet test <project> -f net8.0 --filter "ReplaceParameter_Covariant"
```

Note: `--filter "Name~ReplaceParameter"` (partial match with `~`) may not work reliably. Use `--filter "ReplaceParameter_Covariant"` (bare name) or `--list-tests` to verify discovery. After adding a new `.cs` test file, rebuild before running — the test runner discovers files compiled into the assembly.

## Debugging Tests

When you need to debug anything, you can use ITestOutputService to write the test output.

## Unit Test Patterns

For `Metalama.Framework.Tests.UnitTests` (see `InvokerTests.cs`, `ExpressionFactoryTests.cs` as references):
- Inherit `UnitTestClass`, use `CreateTestContext()` / `CreateCompilationModel(code)`
- Use `SyntaxSerializationContext` + `TemplateExpansionContext.WithTestingContext(ctx, serviceProvider)`
- Need `using Microsoft.CodeAnalysis;` for `NormalizeWhitespace()` extension on `SyntaxNode` — without it, only the `SyntaxToken` overload resolves
- `TypedExpressionSyntaxImpl.Convert()` wraps casts in `ParenthesizedExpression()`, so output is `((Type)expr)` not `(Type)expr`
- `AssertEx.DynamicEquals()` compares via `IExpression.ToExpressionSyntax()` chain
- Type resolution: `compilation.Factory.GetTypeByReflectionType(typeof(int))` for built-in types, `compilation.Types.OfName("A").Single()` for user-defined types
- Type comparison: `compilation.Comparers.Default.Equals(type1, type2)`

## Syntax Generation and Simplification

The syntax generation pipeline intentionally produces over-specified syntax (redundant casts, fully-qualified type names, explicit `new DelegateType(methodGroup)` wrappers) to ensure correctness. The `CodeFormatter` pipeline then simplifies in context:

- **Annotation**: Nodes that may be redundant are annotated with `FormattingAnnotations.WithSimplifierAnnotation<T>()` (or `WithSimplifierAnnotationIfNecessary` which checks `SyntaxGenerationOptions.WillBeFormatted`)
- **Roslyn Simplifier**: `Simplifier.ReduceAsync` removes unnecessary namespace qualifications, redundant casts, etc.
- **Custom Simplifier** (`CodeFormatter.CustomSimplifier`): Handles Metalama-specific patterns — delegate creation simplification (e.g., `new Action(() => { ... })` → `() => { ... }` in target-typed contexts), tuple cast simplification, nullable suppression removal
- **Key files**: `FormattingAnnotations.cs` (SDK layer), `SyntaxExtensions.WithSimplifierAnnotationIfNecessary` (Engine), `CodeFormatter.cs` (pipeline), `CodeFormatter.CustomSimplifier.cs`
- **Initialization**: `MetalamaEngineModuleInitializer` injects `Simplifier.Annotation` into `FormattingAnnotations` to avoid workspace dependency in SDK

## Framework Extensibility

For creating extension packages (like HtmlWriter or Validation), see `Metalama.Framework/docs/extensibility.md`. It covers:
- Extension package structure and `.csproj` configuration
- `MetalamaExtensionAssembly` registration in props files
- Service registration via `IProjectServiceFactory` and `PipelineExtension`
- Test framework plugins (`MetalamaTestPlugIn`)
- Roslyn-version-specific builds

## Patterns Documentation

Implementation documentation for patterns built on Metalama:
- **Caching**: See `Metalama.Patterns/src/docs/caching.md` for backend architecture, enhancers, background task scheduling, serialization, and synchronization