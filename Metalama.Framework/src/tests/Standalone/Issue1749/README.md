# Issue 1749 — two compile-time projects with the same compile-time assembly name

## What this solution builds

| Project | Assembly | Version | Metalama |
|---|---|---|---|
| `Contract1` | `Contract` | 1.0.0.0 | disabled, `[assembly: CompileTime]` |
| `Contract2` | `Contract` | 1.1.0.0 | disabled, `[assembly: CompileTime]` |
| `Middle1` | `Middle1` | — | enabled, references `Contract` 1.0, defines an `[Inheritable]` aspect |
| `Middle2` | `Middle2` | — | enabled, references `Contract` 1.1, defines an `[Inheritable]` aspect |
| `Consumer` | `Consumer` | — | enabled, references both `Middle1` and `Middle2` |

`Contract1` and `Contract2` have `MetalamaEnabled=false` and carry an assembly-level `[CompileTime]` attribute, so
Metalama resolves them through `CompileTimeProject.TryCreateUntransformed`, which is the code path taken by the
public assembly of an SDK-based or weaver-based aspect. That path sets `CompileTimeIdentity` to the run-time
identity verbatim, with no `ml!` prefix and no content hash, so both versions claim the compile-time assembly
name `Contract`.

MSBuild unifies two references with the same assembly name to the highest version, which would leave a single
`Contract` in the closure and defeat the test, so `Consumer` injects both files into `ReferencePath` after
reference resolution.

## How to build

```
dotnet build Issue1749.sln /p:UseSharedCompilation=false
```

`UseSharedCompilation=false` is not optional. With the shared compiler, `Middle1` and `Middle2` are compiled in
the same process, and the second one already fails to load its own version of `Contract`.

## Result on `develop/2026.1` (2026.1.21)

The build fails while building `Consumer`:

```
error LAMA0001: Unexpected exception occurred in Metalama:
Cannot load '...\CompileTime\Contract\unspecified\8850b318c92a7825\...\Contract.dll':
Could not load file or assembly 'Contract, Version=1.0.0.0, Culture=neutral, PublicKeyToken=...'.
Assembly with same name is already loaded

System.IO.FileLoadException
   at System.Runtime.Loader.AssemblyLoadContext.LoadFromAssemblyPath(String assemblyPath)
   at Metalama.Framework.Engine.CompileTime.CompileTimeDomain.LoadAssembly(...)
   at Metalama.Framework.Engine.CompileTime.CompileTimeProject.TryCreateUntransformed(...)
   at Metalama.Framework.Engine.CompileTime.CompileTimeProjectRepository.Builder.TryGetCompileTimeProjectFromPath(...)
```

This is **not** the failure reported in issue #1749. The closure never gets built, so the
`ArgumentException: An item with the same key has already been added` thrown by
`CompileTimeProject.ClosureProjectsByCompileTimeAssemblyName` is never reached: `CompileTimeDomain` refuses the
second assembly first, because an `AssemblyLoadContext` is also keyed by simple name.

The condition the issue describes is therefore reachable in a real build, but on this code path it is fatal
earlier and for a different reason.
