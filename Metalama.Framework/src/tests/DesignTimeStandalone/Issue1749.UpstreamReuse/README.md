# Issue 1749 — the upstream-reuse cache adds under a different key than its guard checks

**This scenario is red until the defect is fixed.** Design-time only: `IUpstreamCompileTimeProjectProvider` is
registered by `DesignTimeAnalysisProcessServiceProviderFactory` and by nothing else, so no build configuration can
reach this site.

## What it reproduces

```
warning CS8785: Generator 'MetalamaSourceGenerator' failed to generate source. ...
An item with the same key has already been added. Key: Shared, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
```

thrown by `Dictionary.Add` at `CompileTimeProjectRepository.Builder.cs:284`, reached through
`Builder.TryGetCompileTimeProject` → `TryGetCompileTimeProjectFromCompilation` → `TryBuild` →
`CompileTimeProjectRepository.Create` → `AspectPipeline.TryInitialize` →
`AnalysisProcessProjectSourceGenerator.ComputeAsync`.

The exception also surfaces a second time during the analyzer phase, from `TheDiagnosticSuppressor.ReportSuppressions`,
where `LocalExceptionReporter` swallows it and writes a crash report under `%TEMP%\Metalama\CrashReports`. Only the
generator's failure is visible as a diagnostic.

## Why the guard does not guard

```csharp
// :269
if ( this._projects.TryGetValue( compilationReference.Compilation.Assembly.Identity, out referencedProject ) )
{
    return true;
}
...
// :284
this._projects.Add( upstreamProject.RunTimeIdentity, upstreamProject );
```

The lookup and the insertion use **different keys**. `upstreamProject` comes from
`DesignTimeUpstreamCompileTimeProjectProvider.TryGetUpstreamConfiguration`, which resolves the pipeline by
`compilation.GetProjectKey()`. A `ProjectKey` is an assembly name, a hash of the preprocessor symbols and a flag: there
is no version in it. Two projects named `Shared` at versions 1.0 and 2.0 therefore share one pipeline slot, and the
configuration handed back carries whichever version's `CompileTimeProject` got there first.

So for the second of the two references the lookup misses on its own identity and the insertion collides on the other
one. In the order that happens to avoid the throw, the outcome is not success but a silent mis-resolution: one reference
receives the other version's compile-time project.

`TryReserveCompileTimeAssemblyName`, which reports `LAMA0079`, cannot help. It is called only on the
`PortableExecutableReference` path at `:401`, and a design-time project reference never takes that path.

## Confirmed independent of declaration order

Swapping the two `ProjectReference` lines still throws, with the roles of the two versions exchanged. That is why the
forbidden pattern stops at `Version=` instead of naming a version.

## Not covered by the existing scenario

`DesignTimeStandalone/Issue1749`, run unchanged, does **not** throw here. It reports the
`ProjectVersionProvider` guard's warning (`Two referenced projects have the key 'Contract, …'`) and every project
succeeds. Its two same-named assemblies are `MetalamaEnabled=false`, so they never contribute a compile-time project and
never reach `:284`.

## Traversal order matters, for a reason that is not this defect

With `--traversal Reverse` the run still exits 1, but with `Specified method is not supported` from
`FakeWorkspaceProvider`, five times over, and the targeted pattern does not match. That is a limitation of the simulator
rather than a product defect: it has no real workspace behind Metalama's `WorkspaceProvider`. The scenario therefore
relies on the default solution order, in which `Consumer` comes last.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net9.0\Metalama.DesignTime.HostSimulator.dll Issue1749.UpstreamReuse.sln --timeout 300
```

Use the **net9.0** build; the net8.0 one fails to load `Metalama.Framework.CompilerExtensions` in this environment, on
the existing scenarios too.
