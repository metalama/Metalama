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

## Platform Support

Which Visual Studio, other IDE, .NET SDK, .NET runtime, .NET Framework and Roslyn versions a release supports is decided by the doctrine in `Metalama.Framework/docs/platform-support.md`, which also names the resulting set a platform baseline (`PB-<release>`). Read it before changing any target framework, before adding or removing a Roslyn variant, and before answering a question of the form "can we drop `netX.0`?". Two rules that are misapplied most often:

- **The host runtime, not the user target framework, sets the floor.** Metalama loads into `devenv.exe`, the Roslyn out-of-process analyzer host, the Rider backend and the C# Dev Kit language server. Dropping a TFM for user projects is a separate decision from dropping it as a host TFM. The baseline records both floors, and only the host one constrains the design-time payload.
- **A wrong lower bound produces no visible error.** `ServiceHub.RoslynCodeAnalysisService` logs the load failure and Visual Studio shows no diagnostics, no code lens and no generated code, so this is derived from the vendor calendars up front rather than discovered from bug reports.

`Directory.Packages.md` is the companion document: the baseline decides which platforms our packages must load into, and `Directory.Packages.md` decides which package versions that permits.

## Package Versioning

In `Directory.Packages.props`, dependencies fall into two categories:

- **API dependencies**: When Metalama is *hosted* (e.g., in Visual Studio), the host provides these dependencies. We must use minimum versions compatible with the lowest supported host version.

- **Runtime (latest) dependencies**: When Metalama *hosts itself* (e.g., standalone tools, tests), we provide the dependencies. These use the latest versions to avoid vulnerability warnings.

The `*LatestVersion` properties (e.g., `MessagePackLatestVersion`, `SystemTextJsonLatestVersion`) are for runtime dependencies. Note that "latest" means latest at the time of updating - some packages like MessagePack are pinned to older major versions for compatibility (MessagePack 2.x for StreamJsonRpc compatibility).

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
2. **File locks**: After failed builds, run `Build.ps1 tools kill` before retrying. Use this instead of killing `dotnet`/`MSBuild`/`VBCSCompiler` processes by name or pattern: doing so also kills the nodes of any other build in flight and leaves worse locks behind.

   **Locked output files are by design, not a symptom.** Metalama loads *user* assemblies into the compiler process: extension assemblies (`MetalamaExtensionAssembly`) and untransformed compile-time assemblies. VBCSCompiler (the shared compiler server) and MSBuild worker nodes both outlive a build on purpose, so those DLLs stay locked in a server after the build that produced them has finished. Expect it; do not investigate it as a defect.

   What it breaks: `Build.ps1 test` begins with `PrepareCommand` → `CleanCommand`, a recursive delete of every `bin`/`obj`. One held file throws `UnauthorizedAccessException` or `IOException`, the exception propagates out of `CleanCommand`, and **the whole run dies before a single test executes**. Worse, the harness still returns **exit code 0**, so the failure reads as success. Never trust that exit code alone; grep the output for `UnauthorizedAccessException`, `IOException` and `project(s) failed`.

   Two traps when clearing locks:
   - `Build.ps1 tools kill` runs *as* `BuildMetalama.dll`, so it cannot kill its own process tree and says so. **Never chain it into the next command** (`tools kill; test`) — its own process is often still holding the DLL the next command must overwrite, which fails with `MSB3027`/`MSB3021` on `BuildMetalama.dll`.
   - Killing MSBuild nodes leaves node reuse expecting them, so the next build can fail with `MSB4166: Child node exited prematurely`. Pass `-nodeReuse:false` on the first build afterwards.

   The sequence that works: kill the servers, **verify** the outputs are actually deletable, then run the suite as a *separate* invocation.

   Every diagnostic `dotnet build` you run spawns servers that will lock things later, so this bites most when alternating between direct builds and `Build.ps1 test`.
3. **Trace data flow**: For MSBuild issues, trace from `.csproj` → `.targets` → Engine code
4. **Reading Metalama's own trace during a build**: all three of these are required, and omitting any one produces no output at all.

   ```powershell
   $env:METALAMA_CONSOLE_TRACE="*"
   dotnet build <project> -t:rebuild --disable-build-servers -v:detailed
   ```

   `METALAMA_CONSOLE_TRACE` makes Backstage log to the console instead of a file; `--disable-build-servers` stops the
   compiler running in a persistent server whose output MSBuild discards; `-v:detailed` makes MSBuild relay the
   compiler's output. Trace records look like `# Metalama TRACE <project>, Thread <n>, <Category>: <message>`, so
   grep for `# Metalama`. Pipe to a file, because the log is large.

   Do **not** try to get these logs by enabling `logging.processes.Compiler` in
   `%LOCALAPPDATA%\Metalama\diagnostics.json` — Metalama rewrites that file and resets the flag, so it does not stick.

5. **Do not filter build output when checking whether a build succeeded**: piping through `Select-String`/`grep` for
   error patterns hides both the failure summary and the exit code, which reads as success. Capture the whole log
   (`Tee-Object`) and inspect it.

6. **Cross-solution changes**: Run `Build.ps1 build` early rather than discovering issues incrementally. Claude may run `Build.ps1 build` itself in this environment (this overrides the general `eng` skill guidance that says to ask the user). Because it is long-running, start it in the background (`run_in_background`) with a high timeout and continue with other work until it completes.

7. **Changing the build container is riskier than it looks.** The container is generated from the component list in
   `eng/src/Program.cs` by `Build.ps1 generate-scripts`. Three findings, each of which cost a continuous integration
   cycle to learn.

   - **Only one .NET SDK feature band may be installed.** Visual Studio installs an SDK of its own through the
     `Microsoft.NetCore.Component.SDK` component, and Visual Studio 2026 18.9 installs 10.0.400. When a second band
     is installed beside it, `MSBuildExtensionsPath` and `MSBuildSDKsPath` resolve to different SDK directories and
     a solution restore fails with `MSB4062`, because `NuGet.Build.Tasks` of one band requires a newer
     `Microsoft.Build.Framework` than the other band provides. The `dotNetSdkVersion` constant in `Program.cs`
     names the version that Visual Studio installs and feeds both the container component and `global.json`.
   - **The desktop `MSBuild.exe` is a second, independent build surface, and it does not behave like
     `dotnet build`.** Two things use it: the `MsbuildSolution` entry for `Metalama.Framework.TestApp.sln`, and the
     nested reference-assembly build that Metalama itself runs (`CompileTimeAssemblyLocator`). A Visual Studio
     component that `dotnet build` does not need may still be required by one of them. Removing
     `Microsoft.NetCore.Component.SDK` leaves `MSBuild.exe` with no `C:\BuildTools\MSBuild\Sdks` directory and it
     fails to resolve `Microsoft.NET.Sdk` with `MSB4276`. Do not conclude that a component is unused because the
     .NET SDK obtains the same payload from NuGet.
   - **Diagnosing a container failure from the outside.** Download the build log with
     `downloadBuildLog.html?buildId=<id>`. Restore and build binary logs are published under `artifacts/logs`, and
     a binary log is a gzip stream, so decompressing it and searching the strings reveals the MSBuild properties,
     which is how the two SDK directories above were found. The binary log of Metalama's nested
     reference-assembly build is written under the agent's temporary directory and is not published, so a failure
     there cannot be diagnosed from the build log.

8. **`Build.ps1 build` may fail locally while continuous integration passes.** On a machine with several .NET SDKs
   and more than one Visual Studio, it has failed with
   `System.MissingMethodException: Method not found: 'Boolean Microsoft.NET.StringTools.SpanBasedStringBuilder.Equals(...)'`
   inside `ArtifactManifestFile`. `BuildMetalama` carries its own `Microsoft.NET.StringTools.dll` and loads MSBuild
   through `Microsoft.Build.Locator`, and the two versions disagree. The continuous integration container has one
   SDK and does not reproduce it. Do not spend cycles on it; use continuous integration as the gate.
- When working on an issue creat a file called <Isse-number>-TODO.md to track progress.
- don't include *-TODO.md in commits
- After a full build with `Build.ps1 build` (Claude may run it, preferably in the background), the msbuild binlogs are under artifacts/logs
- when you start working on an issue, mark the status as In Progress and make sure it is assigned to me
- in tests never use hardcoded delays, always use other sync mechanims such as barriers, taskcompletionsource, sync points
- Never await without cancellation token - ever
- For assertions, use `Invariant.Assert` / `Invariant.AssertNotNull` (`Metalama.Framework.Engine`) instead of the `System.Diagnostics.Debug` assert methods, so the compiler and the `MetalamaAssertionAnalyzer` can track control flow. In projects that don't reference the engine (e.g. `Metalama.Patterns.Caching.Backend`), throw `CachingAssertionFailedException` instead; `System.Diagnostics.Debug` is only acceptable there in already-ported code that uses it throughout.
- Github comments and issues and PRs must be signed by Claude - not commits. No ad link, just the signature `— Claude for @gfraiteur`.
- **Warnings: ignore them while coding, but zero warnings is a gate for any push to a PR.** While writing and testing code, don't lose time on cosmetic warnings (such as redundant usings). But the CI build runs with `-p:ContinuousIntegrationBuild=True`, which promotes analyzer suggestions to errors: `IDE0005` ("using directive is unnecessary") is invisible in a local build and *fails* the CI build. A green local build and a green test suite therefore prove nothing about CI.

  The mechanism is worth knowing, because it gives a cheaper check than running the whole CI configuration:
  `CodeQuality.targets` sets `TreatWarningsAsErrors` when `ContinuousIntegrationBuild` is true. Every warning is
  therefore a CI error, so an ordinary local build already shows them, as warnings. `Build.ps1 <command> --ci`
  simulates the switch, but it also changes dependency resolution to the continuous integration artifact sources,
  which fails on a developer machine, so prefer reading the warnings of a normal build.

  So, before creating a PR **and before every push to an existing PR**, build every project you touched in CI mode and get zero warnings:

  ```powershell
  dotnet build <project> -c Debug -p:ContinuousIntegrationBuild=True -nodeReuse:false
  ```

  Do this for test projects too: they are not built by `Build.ps1 build`, so new test code is the most likely place for such a diagnostic to hide. Do not push and let CI find them; a red CI build costs far more than the check.

  **Re-run it for every project, every time.** Checking one project and then adding a file to another defeats the purpose: `CS0618` on an obsolete API (`TypeFactory.ToNullableType`) is also only an error under this switch, and it failed a build that way. The diagnostics that behave like this are the ones invisible in a normal build, so the check has to follow the code you touched, not the code you remember touching.
- `Build.ps1 build` does not build test projects, only packable projects.
- `Build.ps1 test` implicitly does a clean rebuild (not incremental), so do NOT chain it after `Build.ps1 build` — they overlap. After `Build.ps1 build`, run individual test projects with `dotnet test <project> --no-build`. Only re-run `Build.ps1 build` when you need a cross-solution rebuild.

## Code Style

- **Natural language in comments, documentation, diagnostic messages and exception messages must be documentation-grade**: professional, rather formal, strictly grammatical. Write complete sentences, never stems or fragments such as "Names and not paths, because ...". No contractions ("does not", not "doesn't"). No unusual metaphors, no figurative language, no rhetorical emphasis. No abbreviations beyond the ones already standard in the codebase. Prefer the plainest accurate wording over a vivid one.
- **Copywriting rules.** These apply to every piece of prose: XML documentation, code comments, Markdown documents, commit messages, pull request descriptions, and GitHub comments.
  - **Be accurate.** The statement must be true of the code as written. A summary that says "this class stores nothing" directly above a field is a defect, not a style problem.
  - **Use accurate software engineering language. Do not use analogies or slang.** Name the mechanism: "resolves the identifier through the symbol table", not "walks the symbol table"; "costs an allocation and a dictionary lookup", not "buys nothing".
  - **Do not lead with a mystery or with a rhetorical construct.** State the subject in the first clause. Write "A durable reference stores an identifier at design time and the original reference during a batch compilation", not "What a durable reference stores depends on the scope". Avoid openings such as "The one thing this must never do", "Two consequences are worth knowing", and "This is a cache and nothing else".
  - **No uncommon acronyms or abbreviations.** Expand anything that is not already standard in this codebase.
  - **Assume the reader is not a native English speaker.** Prefer short sentences and a plain vocabulary. Avoid inversion, ellipsis, and idiom. One idea per sentence.
  - **Do not use bold text for emphasis inside a paragraph**, and do not use italics to stress a word. Structure the text instead.
- **Never use em dashes (`—`) in code comments**, including XML doc comments. Use a colon, parentheses, or a separate sentence instead.
- **Document members with `///` XML doc comments, not `//`** - this applies to all members (including `private`/`internal` ones) and to test classes. Put the rationale in `<remarks>` rather than growing `<summary>`, and use `<see cref="..."/>`/`<c>` markup. Plain `//` comments are for statements inside method bodies, and for tool directives such as `// ReSharper disable ...`.
- **Cache derived closure lookups on the owning type.** When a computation is derived purely from immutable state (e.g. `CompileTimeProject.ClosureProjects`), expose it as a `[Memo]` member on that type instead of recomputing it in the caller. Prefer a multi-valued `ImmutableDictionaryOfArray` (via `ToMultiValueDictionary`) over encoding "ambiguous" as a `null` value - build cost is irrelevant once memoized.

## Nested Types in Separate Files

When a class has nested types that are large enough to warrant their own file, use the **partial class** pattern:

- The nested type stays as a `private` (or appropriate access) nested class
- Place it in a separate file named `OuterClass.NestedType.cs`
- The file uses `partial class OuterClass` to contain the nested type
- Use block-scoped namespace syntax (not file-scoped) in the nested type file

Example: `TemplateExpansionContext.ProceedUserExpression.cs` contains `private sealed class ProceedUserExpression` inside `internal sealed partial class TemplateExpansionContext`.

## Testing

The testing strategies and every test suite (unit, aspect, template, linker, standalone, design-time standalone, benchmarks, workspaces) are documented in `Metalama.Framework/docs/testing.md`. Read it before writing or debugging tests. A few reminders that bite in practice:

- **Aspect tests** are discovered by `.cs` file path under `Tests/`; the test name is the file name without extension. Filter with the bare name (`dotnet test <project> -f net10.0 --filter "ReplaceParameter_Covariant"`), not `Name~`, and rebuild after adding a new `.cs` test file.
- **Never commit a new aspect test without running it first and committing its expected output.** An aspect test compares the transformed code against an expected file beside it, so a test committed without one fails on every run, including CI. Run the test, read the actual output under `obj/transformed/<tfm>/...`, check that it is what the test is meant to prove, then copy it next to the `.cs`. A `@TestScenario(DesignTime)` test needs the generated partial classes as well (`<Name>.0.i.cs` and so on), because the design-time pipeline cannot change the signature of an existing declaration and exposes what it introduces as an overload in a separate document. Read the output rather than copying it blindly: a test whose baseline was adopted without being read asserts whatever the code happened to do, including a defect.
- **Unit tests** inherit `UnitTestClass` and use `CreateTestContext()` / `CreateCompilationModel(code)`.
- To emit output from a test, use `ITestOutputService`; for deterministic timing use the sync points of `Metalama.Testing.Hooks.ITestSynchronizationProvider`, never hardcoded delays. The same package holds `ITestFaultInjector`, for deterministically throwing at a chosen place. Both services are shared by every layer, so they derive from no dependency injection marker interface and are registered and resolved untyped.

## Design-Time Memory

The rules that keep the design-time code from accumulating memory are documented in `Metalama.Framework/docs/design-time-memory.md`. Read it before adding a field, a cache, a background task or an event handler to `Metalama.Framework.DesignTime`, and before storing anything derived from the code model in an object that outlives a single request. The core rule:

- The analysis process is long-lived and Roslyn produces a **new `Compilation` per keystroke**. An object that outlives a single request must not strongly reference a `Compilation`, `SyntaxTree`, `SemanticModel`, `ISymbol`, `CompilationModel` or `PartialCompilation`, nor anything that transitively reaches one, apart from the single most recent version of the project.
- Persist declarations as durable references (`IRef.ToDurable()`), and key caches by file path rather than by syntax tree or compilation instance.
- Never pass a cancellation token to `Task.Run`: when the token is already signalled the delegate never runs, so any `finally` that removes the task from a pending-work collection never executes and the closure, with everything it captured, is retained forever.
- The guard suite is `Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/MemoryLeaks/`; its `RetentionPathFinder` reports the chain of fields that retains an object when an assertion fails.

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