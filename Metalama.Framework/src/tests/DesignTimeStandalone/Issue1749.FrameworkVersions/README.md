# Issue 1749 — two versions of Metalama.Framework in one reference graph

## What this solution builds

| Project | Compiled against | Referenced by the consumer as |
|---|---|---|
| `OldAspects` | `Metalama.Framework` 2025.1.18, from nuget.org | a **file reference** to the built assembly |
| `NewAspects` | the `Metalama.Framework` built by this repository | a `ProjectReference` |
| `Consumer` | the `Metalama.Framework` built by this repository | — |

Both aspect projects define an `[Inheritable]` aspect on a base class, and `Consumer` derives from both, so
inheritance carries both aspects through the transitive manifest, which is written by the compile-time serializer
and therefore goes through `CompileTimeSerializationBinder.BindToName`.

`OldAspects` is deliberately **not** a `ProjectReference`. NuGet unifies the `Metalama.Framework` version across a
project reference, which would erase the conflict. A file reference, or equally a package reference to a prebuilt
package, is what a consumer of an aspect library published against an older Metalama actually has.

`nuget.config` clears the repository's package source mapping, which otherwise routes `Metalama.Framework*` to the
local feed only and makes the publicly released version unresolvable. `OldAspects` pins `LangVersion` because the
`Metalama.Compiler` shipping with 2025.1.18 predates the language version the current .NET SDK defaults to.

## How to build

```
dotnet build Issue1749.FrameworkVersions.sln /p:UseSharedCompilation=false
```

## Result on `develop/2026.1` (2026.1.21): this does NOT reproduce

The build **succeeds**, and the old aspect is applied: `Consumer` calls `GetOldMessage()`, which only exists
because `OldAspect` introduced it.

The compile-time project embedded in `OldAspects.dll` lists `Metalama.Framework, Version=2025.1.x` among its
references, but that reference never becomes a second compile-time project:

1. `Builder.TryGetCompileTimeProject(AssemblyIdentity, ...)` misses `_projects`, which holds the framework project
   under the identity of the *loaded* `Metalama.Framework`.
2. It falls back to `IAssemblyLocator.TryFindAssembly`, which resolves to the `Metalama.Framework.dll` the consumer
   actually references, i.e. the current version.
3. `TryGetCompileTimeProjectFromPath` then finds that file's identity in `_projects` and returns the single
   `_frameworkProject`.

`Metalama.Framework` also carries no assembly-level `[CompileTime]` attribute, so it never takes the
`CompileTimeProject.TryCreateUntransformed` path that would build a second project named after it.

So the route "the reference graph contains two Metalama versions", which issue #1749 proposes for its
`Key: Metalama.Framework` instance, does not by itself produce two compile-time projects claiming that name. The
hard-coded framework project absorbs the older reference. Whatever produces the reported duplicate is something
else, and is still unidentified.

Kept as a negative result: it rules out the most obvious hypothesis and documents why.
