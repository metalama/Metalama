# Claude Instructions for Metalama

## Critical Rules

- **NEVER** run `Build.ps1 build` yourself - ask the user to run it (timeout too low, causes retries)
- **NEVER** sign commits, PRs, or issues with "Generated with Claude Code"
- **NEVER** clear global NuGet packages - it's never needed
- **NEVER** replace `<see>` tags with `<c>` to fix XML doc errors - fix the reference or add `using` instead
- **ALWAYS** include the issue number in commit messages: `Fix foo (#1212)`
- Prefer `pwsh` (PowerShell 7), never use the old `cmd` for commands

## Repository Structure

C# monorepo with layered "solutions" (first-level directories). Solutions are defined in `eng/src/Program.cs`. Solution N depends on solution N-1 only via `PackageReference`, never `ProjectReference`.

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
- `PostSharp.Engineering`: build orchestration SDK
- `Metalama.Documentation`: conceptual documentation
- `Metalama.Samples`: examples

## Building

| Scenario | Command |
|----------|---------|
| Single solution changes | `dotnet build` / `dotnet test` |
| Cross-solution changes | Ask user to run `Build.ps1 build` |
| Faster Framework build | Use `Metalama.Framework.LatestRoslyn.slnf` instead of full solution |
| Kill locked processes | `Build.ps1 tools kill` |

**Notes:**
- When adding package references, also add `PackageVersion` to `Directory.Packages.props`
- Two `Build.ps1 build` runs cannot run in parallel
- Don't build full solution just to run a single test - ask user first
- Focus on green tests before fixing cosmetic warnings

## Git Workflow

- **Branch naming**: `topic/YYYY.N/XXXX-short-description` (XXXX = issue number)
- **Merge target**: For `topic/YYYY.N/*`, always merge to `develop/YYYY.N` - ignore default branch
- **Breaking changes**: Add comment to issue describing the change, add `breaking` label
- **Not breaking**: Adding members to interfaces marked with `[InternalImplement]` (including inherited) is NOT a breaking change. Most code model interfaces inherit `[InternalImplement]` from `ICompilationElement`.

## Testing

| Type | Description | Project suffix | Output |
|------|-------------|----------------|--------|
| Aspect tests | Snapshot-based, runs through Metalama pipeline | `*AspectTests` | `Foo.t.cs` |
| Unit tests | Classic xUnit | `*UnitTests` | - |
| Standalone tests | Self-contained projects | - | Optional `test.json` |

Aspect tests support: code transformations, diagnostics, code fixes, live templates, design-time code generation, diff preview. Can execute `Program.Main` and compare output.

Docs: [Aspect testing](https://doc.metalama.net/conceptual/aspects/testing/aspect-testing)

### Test Directives

Test options can be specified in two ways:
1. **In-source comments**: `// @OptionName` or `// @OptionName(value)` within `#if TEST_OPTIONS` blocks
2. **Configuration file**: `metalamaTests.json` in test directory or parent directories

Options in test files override options from configuration files.

**Common directives:**

| Directive | Description |
|-----------|-------------|
| `@Skipped(reason)` | Skip test with reason |
| `@TestScenario(scenario)` | Set scenario: `Default`, `DesignTime`, `CodeFix`, `Preview`, `LiveTemplate` |
| `@IgnoredDiagnostic(id)` | Suppress diagnostic (e.g., `CS0649`, `CS8618`) |
| `@ClearIgnoredDiagnostics` | Clear inherited ignored diagnostics |
| `@LanguageVersion(version)` | Set C# version |
| `@RequiredConstant(name)` | Skip if constant not defined |
| `@ForbiddenConstant(name)` | Skip if constant is defined |
| `@DefinedConstant(name)` | Define preprocessor symbol |
| `@RemoveOutputCode` | Output only diagnostics |
| `@FormatOutput` | Format output code |


**Example test file:**
```csharp
#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @IgnoredDiagnostic(CS0649)
#endif

using Metalama.Framework.Aspects;

[MyAspect]
public class Target { }
```

**Example metalamaTests.json:**
```json
{
  "WriteInputHtml": true,
  "FormatOutput": true,
  "IgnoredDiagnostics": ["CS0649", "CS8618"]
}
```

See `Metalama.Testing.AspectTesting/TestOptions.cs` for full documentation.

## Writing Documentation

- Use `<see>` tags for type/member references
- Maintain consistent lexicon within class families (same suffix)
- Keep code examples short
- Cross-reference conceptual docs via `<seealso href="@..."/>` tags
- Use api-docs-reviewer subagent when writing XML doc or API doc

**Pre-PR Checklist:**
1. Document all new/modified public APIs
2. Search `../Metalama.Documentation/content` for affected articles
3. Create issue at https://github.com/metalama/Metalama.Documentation for doc changes
4. Check that all changes in the PR are documented in issues related to the PR. Suggest the user to create or update these issues if not (check the list of recent issues in doubt).

## Key Paths

| Path | Contents |
|------|----------|
| `../Metalama.Documentation/content` | Conceptual documentation |
| `eng/src/Program.cs` | Solution definitions |
| `%TEMP%\Metalama\CompileTimeTroubleshooting\` | Build error details |

## Dependency Injection

Custom immutable DI (not MEDI). Core types in `Metalama.Framework.Sdk/Services/`.

**Scopes:** `IGlobalService` (singleton), `IProjectService` (per-compilation), `IBackstageService` (infrastructure)

**Rules:**
- `WithService()` returns NEW provider (immutable) - use `WithServiceConditional` to avoid duplicates
- No constructor injection - resolve manually: `serviceProvider.GetRequiredService<T>()`
- Never require a service as a method parameter - report complex problems for user to study
- Register in `ServiceProviderFactory`, test with `AdditionalServiceCollection`

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