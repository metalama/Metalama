# Issue #1743 — duplicate aspect weaver

Regression test for https://github.com/metalama/Metalama/issues/1743: pipeline initialization aborted with an
unhandled `ArgumentException` from `ImmutableDictionary` when the same aspect weaver reached a compilation twice.

```
System.ArgumentException: An element with the same key but a different value already exists.
Key: 'Metalama.Community.Virtuosity.VirtuosityWeaver'
   at System.Collections.Immutable.ImmutableDictionary.ToImmutableDictionary[TSource,TKey](...)
   at Metalama.Framework.Engine.Aspects.AspectDriverFactory..ctor(...)
   at Metalama.Framework.Engine.Pipeline.AspectPipeline.TryInitialize(...)
```

## Structure

| Project | Role |
|---|---|
| `Issue1743.Weaver1` | Compiles `../DuplicatedWeaver.cs`, so it provides the plug-in `Issue1743.DuplicatedWeaver`. |
| `Issue1743.Weaver2` | Compiles the same file, so it provides the same plug-in type name under another assembly identity. |
| `Issue1743.Aspect` | Declares `[Virtualize]`, whose `[RequireAspectWeaver]` names that weaver. |
| `Issue1743.Tests` | References all three, applies `[Virtualize]`, and asserts that the weaver ran. |

`AspectPipeline.LoadPlugIns` instantiates the plug-in types of every compile-time project of the closure, so
`Issue1743.Tests` gets two `Issue1743.DuplicatedWeaver` instances whose `Type.FullName` is equal but whose
declaring assemblies differ. `AspectDriverFactory` indexes weavers by that full name, which is the key
`RequireAspectWeaverAttribute` stores, so the two entries collided.

In the reported crashes the two copies came from a user's reference graph rather than from two projects: a
`Metalama.Community.*` library reaching the compilation both as a package and through a project reference, or two
transitive paths resolving to different builds. Two source projects reproduce the same collision without depending
on a package layout.

## How to run

`dotnet test Issue1743.sln`, or `Build.ps1 test`, which builds and runs every solution under `Standalone`.

- **Bug present**: `Issue1743.Tests` fails to compile, with the `ArgumentException` above.
- **Bug absent**: the solution builds, and `DuplicatedWeaverTests.WeaverRuns` passes because the deduplicated
  weaver still made `Target.Bar` virtual.
