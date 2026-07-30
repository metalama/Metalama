# Issue 1749 — one project's pipeline given another project's compilation

**Red. The fix for the site it names lives in another pull request**, not here: PR #1755 rewrites
`GetProjectReferencesAsync` for issue #1750 and covers this `ToDictionary` along with the two other strict inserts in the
same method. This scenario is the design-time integration counterpart of that PR's unit tests, and it should go green
when #1755 merges. Design-time only.

Note that a second, deeper failure sits behind this one. See "Where it stands" at the end.

## What it reproduces

```
warning CS8785: Generator 'MetalamaSourceGenerator' failed to generate source. ...
An item with the same key has already been added. Key: Contract, 2c261a018ff9f98d
```

thrown by the plain `Enumerable.ToDictionary` at `ProjectVersionProvider.Implementation.cs:135`:

```csharp
var oldProjectReferences = oldCompilation?.ExternalReferences.OfType<CompilationReference>()
    .ToDictionary( x => x.Compilation.GetProjectKey(), x => x.Compilation );
```

This is the unguarded twin of the site fixed 66 lines below it, at `:156`. That fix guarded the **new** compilation's
references and left the **old** compilation's untouched.

## The mechanism, which is not what it looks like

The site needs `oldCompilation` to be non-null, so it looks like it needs a second analysis pass. It does not, and the
route explains why.

`DesignTimeAspectPipeline.cs:571` calls `pipeline.ExecuteAsync( reference.Compilation )` on a pipeline obtained from
`DesignTimeAspectPipelineFactory.TryGetPipeline( ProjectKey )` at `:412`, which compares nothing but the key. So when two
different projects share a `ProjectKey`, **one project's pipeline is handed the other project's compilation**. Its stored
`ProjectVersion.Compilation` becomes `oldCompilation` and the other project's becomes `newCompilation`. Two compilations
of two different projects trivially have different `ExternalReferences` array instances, so the fast path at `:105` does
not short-circuit, and `:135` runs over an old compilation that holds two `Contract` references of one `ProjectKey`.

No edit, no second pass, and no new simulator capability are required.

The options-comparing `TryGetPipeline` overload at `:170` would have refused: `MSBuildProjectFullPath`, `ProjectDir`,
`RootNamespace`, `MetalamaBuildTouchFile` and `ProjectAssetsFile` are all compiler-visible and all differ between two
project files, so `IProjectOptions.Equals` is false and that overload evicts and recreates the pipeline. The by-key
overload compares only the key.

## The configuration

| Project | Role |
|---|---|
| `Contract1`, `Contract2` | both `AssemblyName=Contract`, versions 1.0 and 1.1, `MetalamaEnabled=false` |
| `Aspects1` | `AssemblyName=Aspects`; injects both `Contract` assemblies; referenced by nothing |
| `Aspects2` | `AssemblyName=Aspects`; references no `Contract`; referenced by `Consumer` |
| `Consumer` | references `Aspects2` only |

`Aspects1` and `Aspects2` share a `ProjectKey`. `Aspects1` is analyzed first, so the pipeline cached under that key holds
a compilation whose reference list contains the two `Contract` assemblies. `Consumer` then reaches that pipeline through
its reference to `Aspects2`.

Three things are load-bearing:

- **`MetalamaEnabled=false` on the two `Contract` projects.** `MetalamaProjectClassifier.TryGetMetalamaVersion` requires
  the `METALAMA` preprocessor symbol, so with Metalama off the two `Contract` references are skipped by the reference
  loop. Turn it on and `Aspects1`'s own loop starts diffing `Contract1` against `Contract2`, pauses that pipeline, and
  the pause propagates to `Aspects1`, whose state then holds no `ProjectVersion`. The scenario evaporates.
- **`Aspects2` must come after `Consumer` in the `.sln`.** The simulator loads Metalama twice, in two load contexts:
  `MSBuildWorkspace` runs generators inside `Project.GetCompilationAsync` with its own loader, and the simulator runs its
  own through `IsolatedAnalyzerAssemblyLoader`. Only the second one's diagnostics are collected. The workspace-side
  instance builds `Aspects2`'s compilation while resolving the consumer and evicts its own `Aspects1` pipeline, so it
  does not throw; the collected instance has never seen `Aspects2`, still holds `Aspects1`'s pipeline, and does. Analyze
  `Aspects2` before `Consumer` and the eviction happens in the collected instance too, and the test passes silently.
- **No source file of `Aspects1` names a type of `Contract`.** That would be `CS0433`, since the type exists in both
  assemblies. The reference exists only to give the compilation two `CompilationReference`s of one `ProjectKey`.

## Known exposure, shared with its siblings

The injected hint paths are `bin\$(Configuration)\…`, which the workspace evaluates as `Debug` (the solution's first
configuration) while the harness prepares the build in the requested configuration. In a `Release` run the injected paths
point into `bin\Debug`. `Standalone/Issue1749` and `DesignTimeStandalone/Issue1749` carry the same exposure, so it has
been verified in `Debug` only.

## Where it stands

Guarding `:135` was tried here and then reverted, because PR #1755 does it better: it routes both reference sets through
one `GetProjectReferencesByKey` helper, so all three strict inserts in the method are covered rather than two, and it
also fixes the root cause of #1750, which is that a null or empty `Compilation.AssemblyName` yields an identity-less
`ProjectKey` (`Invariant.AssertNotNull` is a no-op outside `DEBUG`). Duplicating that here would only create a conflict.

With the guard applied locally, this scenario did **not** go green. It failed further on with

```
AssertionFailedException: 'ml!Aspects_<hash>' is a compile-time assembly but it is not a part of the current project.
   at CompileTimeSerializationBinder.BindToName ... :56
```

which is the same second failure that `Issue1749.SameAssemblyIdentity` now shows, and which is not yet diagnosed. So
expect this scenario to need the deeper `ProjectKey` fix as well as #1755, and treat a green run after #1755 merges as a
result to verify rather than to assume.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net9.0\Metalama.DesignTime.HostSimulator.dll Issue1749.SameProjectKey.sln --timeout 300
```
