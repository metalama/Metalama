# Issue #1741 - unsupported legacy Razor precompile

Regression test for https://github.com/metalama/Metalama/issues/1741: building a Metalama Razor/Blazor project with
`UseRazorSourceGenerator=false` crashed with `LAMA0001` ("Cannot determine the compile-time language version").

## Cause

The legacy Razor build runs a `RazorCompileComponentDeclaration` pass whose `Csc` invocation forwards no
`/analyzerconfig`. None of Metalama's MSBuild configuration (`build_property.*`: extension assemblies, compile-time
assemblies, user-code trust, language version, and so on) therefore reaches the compiler, so Metalama cannot run
reliably in that pass. The language version was merely the first missing value that threw.

## Expected outcome

Rather than support the mode partially (which would silently misbehave for any aspect that depends on a missing
option), Metalama rejects it: the `MetalamaCheckRazorComponentDeclaration` target
(`BeforeTargets="RazorCompileComponentDeclaration"`, in `Metalama.Framework.targets`) fails the build with a clear
error directing the user to the Razor source generator, which is the default and is fully supported.

The build is therefore **expected to fail**, which `test.json` declares (`IgnoreExitCode` plus the expected error
message in `ExpectedDiagnosticsRegexes`).

- **Fix present**: the build fails with the `MetalamaCheckRazorComponentDeclaration` error and the test passes.
- **Fix absent**: the build either succeeds or fails with `LAMA0001`, so the expected message is missing and the
  test fails.

## How to run

`Build.ps1 test`, which builds every scenario under `Standalone`. To reproduce by hand, run
`dotnet build Issue1741.csproj` and look at the diagnostics.
