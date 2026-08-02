# Issue1744 (design time)

Asserts that a failure of the nested reference-assembly build degrades the design-time experience gracefully: the
analysis does not crash, does not report `LAMA0001` and does not let the exception escape into `AD0001`. See issues
#1744, #1745, #1746 and #1747.

The failure is provoked exactly as in the standalone scenario of the same name: `MetalamaReferenceAssemblyRestoreTimeout`
is set to one millisecond, so the nested build is killed before it can complete. The design-time pipeline reaches that
build through `SystemTypeResolver`, exactly as the compile-time pipeline does.

The pre-build that the harness runs before the host simulator fails here, which is expected and ignored: a design-time
scenario is allowed to be one that does not compile.

## What this scenario does and does not assert

It asserts that the design-time host survives the condition. It does **not** assert that the user is told about it: at
design time the exception is caught by `TheDiagnosticAnalyzer` and no diagnostic reaches the editor, so there is
nothing for the simulator to observe beyond the absence of a crash. Surfacing the condition in the IDE is a separate
concern, tracked by #1758.

What changed with #1744 on this path is therefore not observable here: the failure is now recognized as a
`DiagnosticException`, so it is neither written as a crash report nor sent as a telemetry report. This scenario guards
the part that is observable, namely that the analysis continues to degrade gracefully rather than throwing.
