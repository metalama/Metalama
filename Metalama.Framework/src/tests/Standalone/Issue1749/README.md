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

## Both `Contract` assemblies are signed, and that is load-bearing

Two references of one simple name are a valid C# compilation only when at least one carries a public key, or their
cultures differ. Two **weak-named** assemblies of one simple name at different versions is `error CS1704`, and that is
what this scenario used to be.

It passed anyway, for the wrong reason. Metalama reports `LAMA0079` and aborts before Roslyn surfaces its
reference-manager diagnostics, so which error appeared depended on ordering the test did not control, and `test.json` set
`IgnoreExitCode` without pinning `CS1704` out. The scenario could have been satisfied by an error it never meant to
assert.

Signing both with the **same** key, keeping the version difference, makes the configuration valid, so `LAMA0079` is the
only error and it concerns something the user can act on. `test.json` now forbids `CS1704` so the premise cannot rot
again silently.

`FailOnUnexpectedDiagnostics` would be the blunt instrument here and is deliberately not used: it fires on incidental
warnings too, such as the `CA1822` the generated code produces, which would make the scenario brittle.

## Why `LAMA0079` is still right here, when `Issue1749.PublicKeyVariants` no longer reports it

The two scenarios differ in the branch they take. These `Contract` assemblies are **untransformed**, so their
compile-time name *is* their run-time name: two versions claim one name and one `AssemblyLoadContext` cannot hold both.
That conflict is unavoidable, so an error is the only answer.

`Issue1749.PublicKeyVariants` uses **transformed** projects, whose `ml!<name>_<hash>` names are unique per content once
the hash covers the full assembly identity. Several projections of one run-time assembly in a closure are legitimate
there, exactly as they are for the per-TFM copies of a multi-targeted library.

## How to build

```
dotnet build Issue1749.sln /p:UseSharedCompilation=false
```

`UseSharedCompilation=false` is not optional, and `test.json` now passes it through its `Properties`, because the
harness otherwise runs a plain `dotnet build` and the scenario fails for the wrong reason.

## The separate defect the shared compiler exposes

With the shared compiler, `Middle1` and `Middle2` are compiled **in the same process**, and the second fails:

```
CSC : error LAMA0001: Unexpected exception occurred in Metalama:
Cannot load '...\Metalama\CompileTime\Contract\...\Contract.dll':
Could not load file or assembly 'Contract, Version=1.1.0.0, ... PublicKeyToken=9f073587addbe099'.
Assembly with same name is already loaded
```

This is a **different** collision from the one this scenario asserts, and it is not fixed. `LAMA0079` is reported by
`CompileTimeProjectRepository.Builder`, whose reservation table is an instance field, so it sees the compile-time
assembly names of **one** compilation's closure. Here each of `Middle1` and `Middle2` has exactly one `Contract` in its
own closure and neither is in conflict; the two versions collide only when both are loaded into the `CompileTimeDomain`
shared by the compiler process.

That makes it the more realistic shape of #1749, since it needs nothing but an ordinary build of a solution where two
projects reference two versions of one untransformed compile-time assembly. Fixing it means isolating compile-time
assemblies per compilation rather than per process, or renaming them the way transformed projects are renamed, and it is
tracked separately from this scenario.

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
