# Issue 1749 at design time — two aspect classes with one full name

**This scenario is red until the defect is fixed.**

The same project configuration as `Standalone/Issue1749.SameAssemblyIdentity`, but it fails at a **different site**, and
that is the whole reason both exist. Two projects with one assembly identity break the compile-time pipeline and the
design-time pipeline in two different places, so a fix verified on one says nothing about the other.

| | Site | Key |
|---|---|---|
| `Standalone/Issue1749.SameAssemblyIdentity` | `TransitivePipelineContributorSource.cs:165` | `AssemblyIdentity` |
| this one | `TransitiveAspectsManifest.cs:66` | aspect class **full name** |

## What it reproduces

```
warning CS8785: Generator 'MetalamaSourceGenerator' failed to generate source. ...
An element with the same key but a different value already exists. Key: 'SharedAspects.InheritedAspect'
```

thrown by `ToImmutableDictionary` inside `TransitiveAspectsManifest.Create`, reached through
`DesignTimeAspectPipelineResult.CreateTransitiveManifest` → `LiveTransitiveAspectManifest` →
`SerializedTransitiveAspectManifestWithoutValidators` → `DesignTimeAspectPipeline.GetDesignTimeProjectVersionAsync:602`
→ `AnalysisProcessProjectSourceGenerator.ComputeAsync`.

## Why the key collides

```csharp
inheritedAspects.GroupBy( a => a.AspectClass )
    .ToImmutableDictionary( g => g.Key.FullName, ... )
```

The grouping is by `AspectClass`, which is an object, and the key is its `FullName`, which is a string. `VariantA` and
`VariantB` each contribute their own `IAspectClass` instance for `SharedAspects.InheritedAspect`, so the `GroupBy`
produces two groups and the `ToImmutableDictionary` receives one name twice.

At design time the consumer sees both variants as `CompilationReference`s and merges the inherited aspects of both into
its own transitive manifest, which is why this site is reached here and not in a batch build.

## What the design-time pipeline survives on the way

The `ProjectKey` collision that `Issue1749.SameProjectKey` targets is present here too and is absorbed, as it should be:

```
# Metalama WARNING, ProjectVersionProvider: Two referenced projects have the key 'Aspects, <hash>'.
Ignoring 'Aspects, Version=1.0.0.0, ...'.
```

So this scenario also serves as a positive check that the `:156` guard works.

## Cost to the user

The exception comes out of the source generator, so the consuming project loses **all** design-time support: no
generated code, no diagnostics, and therefore no explanation. The build of the same solution fails with a different
error at a different place, which makes the two symptoms hard to connect.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net9.0\Metalama.DesignTime.HostSimulator.dll Issue1749.SameAssemblyIdentity.sln --timeout 300
```

Use the **net9.0** build. The net8.0 one fails to load `Metalama.Framework.CompilerExtensions`, on the existing scenarios
too, and `eng/src/DesignTimeSolution.cs` invokes net9.0 anyway. Add `--trace "*"` for Metalama's own trace.

`test.json` matches on the exception message rather than a diagnostic id or a key, so it holds for both this site and the
compile-time twin's.

## Observed and not diagnosed

The run also logs `AspectPipeline: The following call stack has been holding the lock for 'Aspects, <hash>' for a long
time`, from `DesignTimeAspectPipeline.WithLockAsync` under the source generator. It does not prevent the run from
finishing and it is not asserted, but it suggests the `ProjectKey` collision also makes two projects contend for one
pipeline lock.
