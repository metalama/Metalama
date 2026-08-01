# Standalone Tests

When creating standalone tests with multiple projects:

1. **Study existing examples first** - Look at `CompileTimeContract` or `TestWeaver` before designing a new structure. They show the correct patterns for `MetalamaExtensionAssembly`, `MetalamaCompileTimeAssembly`, and project references.

2. **Understand the MSBuild items**:
   - `MetalamaExtensionAssembly`: Loads extension assemblies at runtime (must have `ExportExtensionAttribute`)
   - `MetalamaCompileTimeAssembly`: Adds assemblies to compile-time project references
   - These serve different purposes - don't conflate them

3. **Project structure for SDK extensions**:
   - Contracts project: Contains `[CompileTime]` interfaces, `MetalamaEnabled=false`
   - Extension project: Contains `IProjectServiceFactory` impl, `MetalamaEnabled=false`, references Contracts
   - Consumer project: References Contracts via `ProjectReference`, loads Extension via `MetalamaExtensionAssembly`, adds Contracts via `MetalamaCompileTimeAssembly`

4. **C# limitations with `in` parameters**: Cannot use `yield return` in methods with `in` parameters. Use array initialization instead: `return new[] { ... }`.


## How tests are discovered and run

`Build.ps1 test` builds every scenario under this directory (`ManyDotNetSolutions`). Each immediate subdirectory is
one scenario; the entry point is chosen by this order and the search stops at the first match, so a directory does
not recurse once an entry point is found:

1. `*.proj` (built with the `Build` target, for custom orchestration)
2. `*.sln` / `*.slnx`
3. `*.csproj`
4. `Program.cs`

A scenario succeeds when its build (and, unless `BuildOnly`, its run) succeeds, unless a `test.json` overrides that.

## Describing the expected outcome: `test.json`

Place a `test.json` next to the scenario entry point to assert an outcome other than "builds and runs cleanly" (for
example, a build that must fail with a specific diagnostic). Prefer this over a custom `*.proj` wrapper. The schema is
`TestOptions` in PostSharp.Engineering; the fields most used here are:

- `IgnoreExitCode` (bool): do not fail on a non-zero exit code. Required when the scenario is *expected* to fail.
- `ExpectedDiagnosticsRegexes` (string[]): each regex must match at least one diagnostic, otherwise the test fails.
- `ForbiddenDiagnosticsRegexes` (string[]): each regex must match no diagnostic. Prefer this over
  `FailOnUnexpectedDiagnostics` to assert "must not appear", because it does not fire on incidental warnings.
- `FailOnUnexpectedDiagnostics` (bool): fail if any diagnostic is not matched by an expected regex. Brittle, because
  it also fires on unrelated warnings (for example `NU1902`); usually leave it off.
- `BuildOnly` (bool): build but do not run.

Matching is case-insensitive and runs against the whole diagnostic **line** (every line containing `: error ` or
`: warning `), not against the diagnostic code alone, so a regex may match either a code (`LAMA0077`) or message text.
MSBuild `<Error>` tasks without a code are matched on their message.

Example: a build expected to fail with a specific error (see `Issue1741`, and `AdditionalDiagnosticAnalyzer` for a
`LAMA` code):

```json
{
    "IgnoreExitCode": true,
    "ExpectedDiagnosticsRegexes": [ "does not support the legacy Razor build" ],
    "BuildOnly": true
}
```

Add a `README.md` next to `test.json` explaining why the outcome is expected (see `Issue1743`, `Issue1749`).

## Attention

- Tests under this directory should only use `PackageReference` to reference Metalama. `ProjectReference` should only be used within the same solution, in the same test.