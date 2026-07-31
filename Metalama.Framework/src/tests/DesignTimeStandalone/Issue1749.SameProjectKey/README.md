# Issue 1749: the pipeline of one project given the compilation of another

**Green.** This scenario reproduced a design-time failure in which two different projects shared a `ProjectKey` and one
project therefore received the pipeline of the other. It is a design-time scenario only: a batch build gives each
project its own compiler process, so no pipeline is ever shared.

## What it reproduced

```
warning CS8785: Generator 'MetalamaSourceGenerator' failed to generate source. ...
An item with the same key has already been added. Key: Contract, 2c261a018ff9f98d
```

thrown by `Enumerable.ToDictionary` in `ProjectVersionProvider.Implementation.GetProjectReferencesAsync`, over an old
compilation that held two `Contract` references resolving to a single `ProjectKey`.

## The mechanism

`DesignTimeAspectPipeline` calls `ExecuteAsync( reference.Compilation )` on a pipeline obtained from
`DesignTimeAspectPipelineFactory.TryGetPipeline( ProjectKey )`, which compares nothing but the key. When two different
projects shared a key, the pipeline of one was handed the compilation of the other. The stored
`ProjectVersion.Compilation` of the first became `oldCompilation` and the compilation of the second became
`newCompilation`. Two compilations of two different projects hold different `ExternalReferences` instances, so the
reference-equality shortcut did not apply, and the reference loop then ran over an old compilation holding two
`Contract` references of one key.

No edit and no second analysis pass are required to reach it.

The overload of `TryGetPipeline` that compares options would have refused, because `MSBuildProjectFullPath`,
`ProjectDir`, `RootNamespace`, `MetalamaBuildTouchFile` and `ProjectAssetsFile` all differ between two project files.
The overload that keys on `ProjectKey` alone did not.

## The fix

`Metalama.Framework.targets` defines a `METALAMA_PROJECT_<hash>` compilation symbol computed from the project path, the
target framework, the configuration and the platform, and `ProjectKey` hashes it. Two projects therefore no longer
share a key.

Two further changes are required for the guarantee to hold in this scenario:

- The symbol is defined even when `MetalamaEnabled` is `False`. Both `Contract` projects of this scenario disable
  Metalama, and the symbol was initially defined inside the property group conditioned on that property, so the two
  projects still shared a key.
- `DesignTimeAspectPipelineFactory.TryGetPipeline` compares the path, the target framework and the configuration of the
  two sets of options, and throws a diagnosable exception when they differ, instead of serving a foreign pipeline.

A project that does not reference Metalama at all carries no symbol, so two such projects that produce a single assembly
name still share a key. That configuration is accepted rather than reported: the references contribute no aspect, the
first one is retained, and a warning is logged.

## The configuration

| Project | Role |
|---|---|
| `Contract1`, `Contract2` | both `AssemblyName=Contract`, versions 1.0 and 1.1, `MetalamaEnabled=false` |
| `Aspects1` | `AssemblyName=Aspects`; injects both `Contract` assemblies; referenced by nothing |
| `Aspects2` | `AssemblyName=Aspects`; references no `Contract`; referenced by `Consumer` |
| `Consumer` | references `Aspects2` only |

`Aspects1` and `Aspects2` shared a `ProjectKey` before the fix. `Aspects1` is analyzed first, so the pipeline cached
under that key held a compilation whose reference list contained the two `Contract` assemblies. `Consumer` then reached
that pipeline through its reference to `Aspects2`.

Three properties of the configuration are essential:

- **`MetalamaEnabled=false` on the two `Contract` projects.** `MetalamaProjectClassifier.TryGetMetalamaVersion` requires
  the `METALAMA` preprocessor symbol, so the two `Contract` references are skipped by the reference loop. With Metalama
  enabled, the reference loop of `Aspects1` starts to compare `Contract1` against `Contract2`, pauses that pipeline, and
  the pause propagates to `Aspects1`, whose state then holds no `ProjectVersion`. The scenario no longer applies.
- **`Aspects2` must follow `Consumer` in the solution file.** The simulator loads Metalama twice, in two load contexts:
  `MSBuildWorkspace` runs generators inside `Project.GetCompilationAsync` with its own loader, and the simulator runs
  its own through `IsolatedAnalyzerAssemblyLoader`. Only the diagnostics of the second one are collected. The instance
  on the workspace side builds the compilation of `Aspects2` while resolving the consumer and evicts its own `Aspects1`
  pipeline, so it does not throw. The collected instance has never seen `Aspects2`, still holds the pipeline of
  `Aspects1`, and does throw. Analyzing `Aspects2` before `Consumer` causes the eviction to occur in the collected
  instance as well, and the scenario then passes without having exercised anything.
- **No source file of `Aspects1` names a type of `Contract`.** That would be `CS0433`, because the type exists in both
  assemblies. The reference exists only so that the compilation holds two `CompilationReference` instances of one
  `ProjectKey`.

## Known limitation, shared with the sibling scenarios

The injected hint paths are `bin\$(Configuration)\...`, which the workspace evaluates as `Debug`, the first
configuration of the solution, whereas the harness prepares the build in the requested configuration. In a `Release`
run the injected paths point into `bin\Debug`. `Standalone/Issue1749` and `DesignTimeStandalone/Issue1749` carry the
same limitation, so this scenario has been verified in `Debug` only.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net9.0\Metalama.DesignTime.HostSimulator.dll Issue1749.SameProjectKey.sln --timeout 300
```

Use the **net9.0** build. Add `--trace "*"` to obtain the trace of Metalama itself.
