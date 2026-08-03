# Issue1744

Asserts that a failure of the nested reference-assembly build is reported as `LAMA0082`, an actionable diagnostic
quoting what the failing build said, and not as `LAMA0001` "unexpected exception occurred in Metalama" with a crash
report and a telemetry report. See issues #1744, #1745, #1746 and #1747.

## How the failure is provoked

`MetalamaAssemblyLocatorHooksDirectory` points at `AssemblyLocatorHooks`, whose
`Metalama.AssemblyLocator.Build.targets` is imported into the temporary project that resolves the compile-time
reference assemblies and raises an error before that project produces anything.

This provocation was chosen because it is deterministic and needs no network access, unlike the conditions actually
reported in the issues (a NuGet feed that requires credentials, a `global.json` that pins an absent .NET SDK, a task
of the nested build crashing). The code path under test is shared with all of them: `DotNetTool` throws
`ProcessFailedException`, `CompileTimeAssemblyLocator` classifies the output and throws a `DiagnosticException`, and
`CompileTimeExceptionHandler` reports the diagnostics it carries instead of writing a crash report.

Two settings make the scenario repeatable, and both are necessary:

- `MetalamaAssemblyLocatorSalt` gives the scenario a cache directory of its own. The failing build never writes the
  list of reference assemblies, so that directory is never populated and every run reaches the failure instead of
  reading a previous result.
- `UseSharedCompilation` is `false`. The compiler server outlives a build and can serve an assembly locator that a
  previous compilation created successfully, in which case this scenario would build cleanly and assert nothing.

An earlier version of this scenario provoked the failure with a one-millisecond
`MetalamaReferenceAssemblyRestoreTimeout`. That does not work: killing the `dotnet` process does not stop the MSBuild
worker processes it started, which finish the build and populate the cache, so the scenario passed once and silently
stopped asserting afterwards.

## Why the build is expected to fail

The compile-time reference assemblies cannot be resolved, so the compilation genuinely cannot proceed: `LAMA0082` is
an error and the build fails, hence `IgnoreExitCode`. What this scenario asserts is the *shape* of the failure:

- `LAMA0082` is reported, and it quotes what the nested build said rather than discarding it.
- `LAMA0001` is **not** reported: the condition belongs to the environment and must not be presented as a defect of
  Metalama, nor produce a crash report or a telemetry report.

The sibling condition, a nested build that exceeds its time budget, is reported as `LAMA0083`. The classification of
the child output is covered by `ReferenceAssemblyBuildFailureClassifierTests`, and the architecture mismatch that
causes these failures in the field is reproduced by the
`docker/win-x64/ReferenceAssemblyArchitectureMismatch` scenario.
