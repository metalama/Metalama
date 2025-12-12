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

## Dependency Injection

Custom immutable DI (not MEDI). Core types in `Metalama.Framework.Sdk/Services/`.

**Scopes:** `IGlobalService` (singleton), `IProjectService` (per-compilation), `IBackstageService` (infrastructure)

**Rules:**
- `WithService()` returns NEW provider (immutable) - use `WithServiceConditional` to avoid duplicates
- No constructor injection - resolve manually: `serviceProvider.GetRequiredService<T>()`
- Never require a service as a method parameter - report complex problems for user to study
- Register in `ServiceProviderFactory`, test with `AdditionalServiceCollection`

