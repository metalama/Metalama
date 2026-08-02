# Issue1744

Asserts that a failure of the nested reference-assembly build is reported as an actionable diagnostic, and not as
`LAMA0001` "unexpected exception occurred in Metalama" with a crash report and a telemetry report. See issues #1744,
#1745, #1746 and #1747.

## How the failure is provoked

`MetalamaReferenceAssemblyRestoreTimeout` is set to one millisecond. No process can start and complete in that time,
so `DotNetTool` kills the nested build and reports a timeout, which the assembly locator turns into `LAMA0083`.

This provocation was chosen because it is deterministic and needs no network access, unlike the conditions actually
reported in the issues (a NuGet feed that requires credentials, a `global.json` that pins an absent .NET SDK, a task
of the nested build crashing). The code path under test is shared with all of them: `DotNetTool` throws
`ProcessFailedException`, `CompileTimeAssemblyLocator` turns it into a `DiagnosticException`, and
`CompileTimeExceptionHandler` reports the diagnostics it carries instead of writing a crash report.

`MetalamaAssemblyLocatorSalt` gives the scenario a cache directory of its own. The cache is never populated, because
the nested build never completes, so every run exercises the failure rather than reading a previous result.

The sibling condition, a nested build that completes with a non-zero exit code, is reported as `LAMA0082`. Its
classification of the child output is covered by `ReferenceAssemblyBuildFailureClassifierTests`, and the architecture
mismatch that causes it in the field is reproduced by the
`docker/win-x64/ReferenceAssemblyArchitectureMismatch` scenario.

## Why the build is expected to fail

The compile-time reference assemblies cannot be resolved, so the compilation genuinely cannot proceed: `LAMA0083` is
an error and the build fails, hence `IgnoreExitCode`. What this scenario asserts is the *shape* of the failure:

- `LAMA0083` is reported, and its message names `MetalamaReferenceAssemblyRestoreTimeout`, the property that raises
  the budget, as well as the path of the binary log of the nested build.
- `LAMA0001` is **not** reported: the condition belongs to the environment and must not be presented as a defect of
  Metalama, nor produce a crash report or a telemetry report.
