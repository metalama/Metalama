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

The `*LatestVersion` properties (e.g., `MessagePackLatestVersion`, `StreamJsonRpcLatestVersion`) are for runtime dependencies. Note that "latest" means latest at the time of updating - some packages like MessagePack are pinned to older major versions for compatibility (MessagePack 2.x for StreamJsonRpc compatibility).

## Dependency Injection

Custom immutable DI (not MEDI). Core types in `Metalama.Framework.Sdk/Services/`.

**Scopes:** `IGlobalService` (singleton), `IProjectService` (per-compilation), `IBackstageService` (infrastructure)

**Rules:**
- `WithService()` returns NEW provider (immutable) - use `WithServiceConditional` to avoid duplicates
- No constructor injection - resolve manually: `serviceProvider.GetRequiredService<T>()`
- Never require a service as a method parameter - report complex problems for user to study
- Register in `ServiceProviderFactory`, test with `AdditionalServiceCollection`

## Container Environment

When running in a container, `gh` is not available. Use the Approval MCP server for GitHub operations.

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
4. **Cross-solution changes**: Ask user to run `Build.ps1 build` early rather than discovering issues incrementally
- When working on an issue creat a file called <Isse-number>-TODO.md to track progress.
- don't include *-TODO.md in commits
- After the user does a full build sing `Build.ps1 build`, the msbuild binlogs are under artifacts/logs
- when you start working on an issue, mark the status as In Progress and make sure it is assigned to me
- in tests never use hardcoded delays, always use other sync mechanims such as barriers, taskcompletionsource, sync points
- Never await without cancellation token - ever
- Github comments and issues and PRs must be signed by CLaude - not commits. No ad link, just signature.
- don't loose time solving cosmetic warnings (such as redundant usings) until the finalizing stage of a commit
- `Build.ps1 build` does not build test projects, only packable projects.

## Debugging Tests

When you need to debug anything, you can use ITestOutputService to write the test output.

## Framework Extensibility

For creating extension packages (like HtmlWriter or Validation), see `Metalama.Framework/docs/extensibility.md`. It covers:
- Extension package structure and `.csproj` configuration
- `MetalamaExtensionAssembly` registration in props files
- Service registration via `IProjectServiceFactory` and `PipelineExtension`
- Test framework plugins (`MetalamaTestPlugIn`)
- Roslyn-version-specific builds