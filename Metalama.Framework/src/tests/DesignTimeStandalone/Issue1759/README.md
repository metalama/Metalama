# Issue 1759 at design time: a fabric whose type name is ambiguous in the consumer

**Red.** This scenario reproduces [#1759](https://github.com/metalama/Metalama/issues/1759) on `develop/2026.1` at
`23d57ba747`. It fails, and it is expected to fail until the defect is fixed.

## What it reproduces

The design-time pipeline fails to initialize, and the failure escapes as an unhandled exception rather than a
diagnostic:

```
System.InvalidOperationException: The type 'SharedFabrics.SharedProjectFabric' cannot be used at run-time,
because the assembly 'ml!FabricsA_9065f04512aa6559' is not referenced in project 'Consumer'.
   at Metalama.Framework.Engine.CodeModel.Helpers.ReflectionMapper.GetNamedTypeSymbolByMetadataName(...)
   at Metalama.Framework.Engine.CodeModel.Helpers.ReflectionMapper.GetNamedTypeSymbol(...)
   at Metalama.Framework.Engine.CodeModel.Helpers.ReflectionMapper.GetTypeSymbolCore(...)
   at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(...)
   at Metalama.Framework.Engine.Fabrics.FabricDriver.GetCreationData(...)
   at Metalama.Framework.Engine.Fabrics.ProjectFabricDriver.Create(...)
   at Metalama.Framework.Engine.Fabrics.FabricManager.CreateDrivers(...)
   at Metalama.Framework.Engine.Fabrics.FabricManager.ExecuteFabrics(...)
   at Metalama.Framework.Engine.Pipeline.AspectPipeline.TryInitialize(...)
```

The stack matches the reported signatures frame for frame down to `AspectPipeline.TryInitialize`. Both entry points
named in the issue are reached, `AnalysisProcessProjectSourceGenerator.ComputeAsync` and
`TheDiagnosticAnalyzer.AnalyzeSemanticModel`, as well as a third one the issue does not mention,
`TheDiagnosticSuppressor.ReportSuppressions`. The consumer therefore loses every design-time feature, and no diagnostic
explains why.

## The configuration

| Project | Contains | References |
|---|---|---|
| `FabricsA` | `SharedFabrics.SharedProjectFabric`, a `TransitiveProjectFabric` | |
| `FabricsB` | a type declaration of the same full name | |
| `Consumer` | an aspect of its own, so that it runs a pipeline | `FabricsA` and `FabricsB` |

The two libraries have different assembly names, so the consumer binds two distinct assembly symbols and each of them
exports a type named `SharedFabrics.SharedProjectFabric`. Two independently published libraries that happen to declare
a fabric of the same namespace and name produce this configuration in any project that references both.

## Why the type cannot be resolved

`FabricDriver.GetCreationData` resolves the fabric type into the run-time compilation through
`ReflectionMapper.GetNamedTypeSymbolByMetadataName`, which proceeds in two steps:

1. `Compilation.GetTypeByMetadataName( metadataName )`, which searches the compilation and all of its references and
   returns `null` when the name is found in more than one assembly.
2. Only when the first step returned `null`, a lookup of the assembly name among the referenced assemblies, which
   throws the exception above when that assembly is absent.

Both libraries export the fabric type, so step 1 is ambiguous and returns `null`. Step 2 then looks for the assembly
that declares the fabric instance, which is the compile-time assembly `ml!FabricsA_<hash>`, because the fabric was
instantiated from the compile-time projection. A compile-time assembly is built in memory and can never be a metadata
reference of a compilation, so that lookup necessarily finds nothing and the exception is raised.

## The message is misleading, and that matters

`Consumer` references `FabricsA` through an ordinary project reference. The assertion made by the message, that the
assembly is not referenced, is false. The message is a tautology: it names an assembly whose absence is structurally
certain, and it discards the only fact of interest, which is the reason the first step failed.

Two consequences follow, and both are worth weighing before choosing a fix:

- The recovery path immediately below the throw site, which resolves a type against the highest version when several
  versions of an assembly are present, is unreachable for any type that comes from a transformed compile-time project,
  because the assembly name it is given always begins with `ml!`. Passing the run-time identity of the compile-time
  project instead of the identity of the compile-time assembly would both revive that recovery and make the message
  truthful.
- A guard that tests whether the compilation references the run-time assembly of the fabric does not cover this
  scenario, because that assembly is referenced. The scenario is therefore a test of whether a candidate fix addresses
  the reported condition or only a condition that resembles it.

## The control

Removing the reference to `FabricsB` from `Consumer`, and changing nothing else, makes the solution build and analyze
cleanly. The transitive fabric of `FabricsA` is discovered and applied as expected. The second library, and only the
second library, is what turns a working configuration into a crash.

## Not limited to design time

A plain `dotnet build` of this solution fails with the same exception, reported as `LAMA0001`. The scenario lives here
rather than under `Standalone/` because the reported occurrences are all design-time, where the cost to the user is
much higher: a batch build stops with an error the user can act upon, whereas the IDE silently loses all Metalama
features for the project. A `Standalone/` twin would be justified if the two sites ever diverge, as they did for
[#1749](https://github.com/metalama/Metalama/issues/1749).

## How the outcome is asserted

`test.json` sets `ErrorRegexes` and deliberately sets no `ForbiddenDiagnosticsRegexes`. The three assertion modes of
`TestableSolution.EvaluateOutput` are mutually exclusive, in the order "assert on diagnostics", "report a non-zero exit
code", "match `ErrorRegexes` against the whole output". Declaring a diagnostics assertion would therefore suppress the
exit-code check, and the scenario would be reported as successful while crashing, because the simulator does not
surface this exception as a diagnostic in the canonical MSBuild format. The exception is written to the output as a
`# Metalama ERROR` trace line and the simulator reports the failure through its exit code.

The current outcome is thus a failure by exit code. Once the defect is fixed, the exit code becomes zero and
`ErrorRegexes` takes over, so that a fix which merely swallows the exception while still logging it does not pass.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net9.0\Metalama.DesignTime.HostSimulator.dll Issue1759.sln --timeout 300
```

Use the **net9.0** build, as `eng/src/DesignTimeSolution.cs` does. Add `--trace "*"` to obtain the trace of Metalama
itself. The host simulator is a test project, so `Build.ps1 build` does not build it; build it with `dotnet build`
first.
