# Issue 1749 at design time: two aspect classes with one full name

**Green.** This scenario uses the same project configuration as `Standalone/Issue1749.SameAssemblyIdentity`, but it
failed at a **different site**, which is the reason both scenarios exist. Two projects with one assembly identity broke
the compile-time pipeline and the design-time pipeline in two different places, so a fix verified on one said nothing
about the other.

| | Site | Key |
|---|---|---|
| `Standalone/Issue1749.SameAssemblyIdentity` | `TransitivePipelineContributorSource` | `AssemblyIdentity` |
| this one | `TransitiveAspectsManifest.Create` | aspect class **full name** |

## What it reproduced

```
warning CS8785: Generator 'MetalamaSourceGenerator' failed to generate source. ...
An element with the same key but a different value already exists. Key: 'SharedAspects.InheritedAspect'
```

thrown by `ToImmutableDictionary` inside `TransitiveAspectsManifest.Create`, reached through
`DesignTimeAspectPipelineResult.CreateTransitiveManifest`, `LiveTransitiveAspectManifest`,
`SerializedTransitiveAspectManifestWithoutValidators`, `DesignTimeAspectPipeline.GetDesignTimeProjectVersionAsync` and
`AnalysisProcessProjectSourceGenerator.ComputeAsync`.

## Why the key collided

```csharp
inheritedAspects.GroupBy( a => a.AspectClass )
    .ToImmutableDictionary( g => g.Key.FullName, ... )
```

The grouping was by `AspectClass`, which is an object, whereas the key is its `FullName`, which is a string. `VariantA`
and `VariantB` each contribute their own `IAspectClass` instance for `SharedAspects.InheritedAspect`, so the `GroupBy`
produced two groups and the `ToImmutableDictionary` received one name twice.

At design time the consumer sees both variants as `CompilationReference` instances and merges the inherited aspects of
both into its own transitive manifest, which is why this site is reached here and not in a batch build.

## The fix

The grouping is now by `AspectClass.FullName`, so two instances of one name merge into a single group.

A second failure appeared behind it once that was fixed, while the consumer serialized its own transitive manifest:

```
AssertionFailedException: 'ml!Aspects_<hash>' is a compile-time assembly but it is not a part of the current project.
```

An inheritable aspect instance carried an aspect class belonging to a compile-time projection absent from the closure of
the consumer. That failure is resolved by two other changes of this pull request: the `ml!` name now hashes the full
assembly identity, so two distinct assemblies no longer receive a single compile-time name, and `ProjectKey` now
identifies a project, so the consumer is no longer served the pipeline of another project.

## Cost to the user before the fix

The exception came out of the source generator, so the consuming project lost **all** design-time support: no generated
code, no diagnostics, and therefore no explanation. The build of the same solution failed with a different error at a
different place, which made the two symptoms difficult to relate.

## Running it by hand

```powershell
dotnet <repo>\Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator\bin\Debug\net10.0\Metalama.DesignTime.HostSimulator.dll Issue1749.SameAssemblyIdentity.sln --timeout 300
```

Use the **net10.0** build. The net8.0 one fails to load `Metalama.Framework.CompilerExtensions`, on the existing
scenarios as well, and `eng/src/DesignTimeSolution.cs` invokes net10.0 in any case. Add `--trace "*"` to obtain the trace
of Metalama itself.

`test.json` matches on the message of the exception rather than on a diagnostic identifier or a key, so it holds for
this site and for the compile-time counterpart alike.
