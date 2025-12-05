# Claude Instructions for Metalama

## Critical Rules

- **NEVER** run `Build.ps1 build` yourself - ask the user to run it (timeout too low, causes retries)
- **NEVER** sign commits with "Generated with Claude Code"
- **NEVER** clear global NuGet packages - it's never needed
- **ALWAYS** include the issue number in commit messages: `Fix foo (#1212)`
- Prefer `pwsh` (PowerShell 7), but never use the old `cmd` for commands.

## Repository Structure

C# monorepo with layered "solutions" (first-level directories: `Metalama`, `Metalama.Extensions`, etc.). Solutions are defined in `eng/src/Program.cs`. Solution N depends on solution N-1 only via `PackageReference`, never `ProjectReference`.

Main solutions:

- `Metalama.Backstage`: infrastructure concerns: licensing, logging, telemetry, ...
- `Metalama.Framework`: core framework
- `Metalama.Extensions`: extensions built on the core framework, but not usable aspects
- `Metalama.Patterns`: specific aspects built on `Metalama.Framework` or `Metalama.Patterns`
- `Metalama.LinqPad`: LinqPad driver
- `Metalama.Migration`: PostSharp API annotated with documentation to upgrade to Metalama
- `eng`: build orchestration, built on `PostSharp.Engineering` (not a solution)

Other repos (hosted on https://github.com/postsharp or https://github.com/metalama, locally in `..` or `../..`):

- `Metalama.Premium`: premium features built on `Metalama.Framework`, `Metalama.Extensions` or `Metalama.Patterns`
- `PostSharp.Engineering`: build orchestration front-end
- `Metalama.Documentation`: conceptual documentation
- `Metalama.Samples`: vertical examples

## Building

| Scenario | Command |
|----------|---------|
| Changes within a single solution | `dotnet build` / `dotnet test` |
| Cross-solution changes | Ask user to run `Build.ps1 build` |

- When adding package references, also add `PackageVersion` to `Directory.Packages.props` (Central Package Management)
- Two `Build.ps1 build` runs cannot run in parallel

## Git Workflow

- **Branch naming**: `topic/YYYY.N/XXXX-short-description` (XXXX = issue number)
- **Merge target**: For `topic/YYYY.N/*`, always merge to `develop/YYYY.N` - ignore default branch
- **Breaking changes**: When the PR contains breaking changes in the public API:
  1. Add a comment to the main issue describing the breaking change
  2. Add the `breaking` label to the issue

## Testing

| Type | Description | Project suffix | Output |
|------|-------------|----------------|--------|
| Aspect tests | Snapshot-based, runs `Foo.cs` through Metalama pipeline | `*AspectTests` | `Foo.t.cs` (actual: `obj/Debug/tfm/metalama/Foo.t.cs`) |
| Unit tests | Classic xUnit | `*UnitTests` | - |
| Standalone tests | Self-contained projects in `Metalama.Framework/src/tests/Standalone/*` | - | Optional `test.json` specifies expected output. Otherwise, expected output is success. |

Docs: [Aspect testing](https://doc.metalama.net/conceptual/aspects/testing/aspect-testing), [Compile-time testing](https://doc.metalama.net/conceptual/aspects/testing/compile-time-testing)

### Aspect Tests Capabilities

Snapshot-based testing framework. Use for: code transformations, diagnostics/warnings, code fixes, live templates, design-time code generation, diff preview. Can execute `Program.Main` and compare output. Directives via `// @Directive` in `#if TESTRUNNER` - see `TestOptions.cs`.

## Writing Documentation

### XML Documentation Style

- Use `<see>` tags for type/member references
- Maintain consistent lexicon and structure within class families (same suffix)
- Keep code examples short
- Cross-reference conceptual docs via `<seealso href="@..."/>` tags

### Pre-PR Documentation Checklist

1. Document all new/modified public APIs
2. Search `../Metalama.Documentation/content` for affected conceptual articles
3. For conceptual doc changes, create issue at https://github.com/metalama/Metalama.Documentation

## Key Paths

| Path | Contents |
|------|----------|
| `../Metalama.Documentation/content` | Conceptual documentation |
| `../Metalama.Documentation/code` | Sample code |
| `eng/src/Program.cs` | Solution definitions |

## Dependency Injection

Custom immutable DI (not MEDI). Core types in `Metalama.Framework.Sdk/Services/`.

**Scopes**: `IGlobalService` (singleton), `IProjectService` (per-compilation), `IBackstageService` (infrastructure)

**Key rules**:
- `WithService()` returns NEW provider (immutable) - use `WithServiceConditional` to avoid duplicates
- No constructor injection - resolve manually: `serviceProvider.GetRequiredService<T>()`
- `AddShared<T>()` for services cached across provider family; `Add<T>()` for isolated instances
- Register in `ServiceProviderFactory`, test with `AdditionalServiceCollection`

## Incremental Learning

Update this file when you discover something that will save time in future sessions.
- Use `Build.ps1 tools kill` to kill processes.
- Use CLAUDE-TODO.md to track the work list.
- Before preparing a PR, check CLAUDE-TODO.md.
- Read NOTES.md when it changes for important context about ongoing work and breaking changes.

## Lessons Learned

### Debugging Build Issues

1. **Check troubleshooting files**: When Metalama build fails, look at `%TEMP%\Metalama\CompileTimeTroubleshooting\...\errors.txt` for the actual error and the references list.

2. **File locks are common**: After failed builds, assemblies get locked by compiler processes. Always run `Build.ps1 tools kill` before retrying.

3. **Trace the data flow**: When MSBuild items don't work as expected, trace from `.csproj` → `.targets` file transformation → Engine code that reads the property. The bug with `MetalamaCompileTimeAssembly` was found by checking the targets file didn't resolve `%(FullPath)`.

### General Efficiency

1. **Ask before building**: When changes span multiple solutions, ask the user to run `Build.ps1 build` early rather than discovering issues incrementally.

2. **Test incrementally**: Build and test each component before combining them. 

3. **Create issues promptly**: When discovering a bug during development, create the issue immediately so it's tracked even if the fix is in the same PR.


## Quick learnings

- Never require a service as a method parameter, but report a complex problem to be studied by user
- Instead of compiling the full Metalama.Framework.sln, build Metalama.Framework.LatestRoslyn.slnf (faster build) unless asked otherwise.
- don't focus on solving cosmetic warnings before habing green tests
- When I say I want to start to work a gitHub issue:

* Read all details about this issue online and do your research
* Check conceptual documentation under ../Metalama.Documentation/content to see how it's supposed to work
* Create a branch for that issue (see git-workflow)
- You don't need to build the full solution to run a single test. It is VERY slow. Ask the user first if you thing you should.