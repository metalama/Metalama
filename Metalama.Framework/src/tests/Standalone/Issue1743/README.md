# Issue #1743 - duplicate aspect weaver

Regression test for https://github.com/metalama/Metalama/issues/1743: pipeline initialization aborted with an
unhandled `ArgumentException` from `ImmutableDictionary` when the same aspect weaver type name reached a compilation
twice.

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
| `Issue1743.App` | References all three and applies `[Virtualize]`. |

`AspectPipeline.LoadPlugIns` instantiates the plug-in types of every compile-time project of the closure, so
`Issue1743.App` gets two `Issue1743.DuplicatedWeaver` instances whose `Type.FullName` is equal but whose declaring
assemblies differ. `AspectDriverFactory` indexes weavers by that full name, which is the key
`RequireAspectWeaverAttribute` stores, so the two entries collided.

In the reported crashes the two copies came from a user's reference graph rather than from two projects: a
`Metalama.Community.*` library reaching the compilation both as a package and through a project reference, or two
transitive paths resolving to different builds. Two source projects reproduce the same collision without depending
on a package layout.

## Expected outcome

The two weavers here come from assemblies of a **different identity**, so they are not interchangeable and Metalama
cannot know which one the user means. The build is therefore expected to fail with `LAMA0077`, which `test.json`
declares as the expected diagnostic.

- **Bug present**: the build fails with the unhandled `ArgumentException` above, reported as `LAMA0001`, so the
  expected `LAMA0077` is missing and the test fails.
- **Bug absent**: the build fails with `LAMA0077` and nothing else.

The complementary case, two *truly duplicate* weavers (two instances of the same type from the same assembly, which
is what the same weaver assembly reaching the compilation twice produces), is deduplicated silently and is covered
by `DuplicateAspectWeaverTests` in `Metalama.Framework.Tests.UnitTests`.

## How to run

`Build.ps1 test`, which builds and runs every solution under `Standalone`. To reproduce by hand, run
`dotnet build Issue1743.sln` and look at the diagnostics.
