# Issue1744 (design time)

Asserts that a failure of the nested reference-assembly build is reported to the user at design time, as the
`LAMA0082` diagnostic of the design-time pipeline, and that it does not escape as an exception. See issues #1744,
#1745, #1746 and #1747.

The failure is provoked exactly as in the standalone scenario of the same name: `MetalamaAssemblyLocatorHooksDirectory`
points at a targets file that is imported into the temporary reference-assembly project and fails its build. The
design-time pipeline reaches that build through its own entry point, exactly as the compile-time pipeline does. See the
standalone `README.md` for why the scenario also sets a locator salt and disables the shared compiler.

The pre-build that the harness runs before the host simulator fails here, which is expected and ignored: a design-time
scenario is allowed to be one that does not compile.

## What this scenario asserts

- `LAMA0082` is reported. The design-time pipeline resolves the compile-time reference assemblies at its entry point,
  where it has a diagnostic sink, so the failure is returned as a failed result carrying the diagnostic, and
  `TheDiagnosticAnalyzer` reports the diagnostics of a failed result. The user therefore sees the same actionable
  message in the editor as in a build.
- `CS8785` is **not** reported. Before this was fixed, the failure travelled as an exception, which escaped
  `BaseSourceGenerator`. That class rethrows on purpose, so Roslyn reported `CS8785`, telling the user only that a
  generator had failed, and the host simulator counted the project as failed.
- `LAMA0001` and `AD0001` are **not** reported: the condition belongs to the environment and must be presented neither
  as a defect of Metalama nor as an analyzer that threw.
