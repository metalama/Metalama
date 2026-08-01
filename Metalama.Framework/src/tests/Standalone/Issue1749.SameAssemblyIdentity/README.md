# Issue 1749 — two references with one assembly identity, colliding in the manifest collector

**Passing.** The build succeeds, which is the assertion.

A design-time twin lives at `DesignTimeStandalone/Issue1749.SameAssemblyIdentity`, and it is **still failing**, which
vindicates having both. The same configuration throws at two different sites keyed on two different things: this one on
the assembly identity, the design-time one on an aspect class full name in `TransitiveAspectsManifest.cs:66`. Fixing
this one did not fix that one, and the design-time side has since moved on to a third failure.

## What it used to produce, before the fix

```
CSC : error LAMA0001: Unexpected exception occurred in Metalama:
An element with the same key but a different value already exists.
Key: 'Aspects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=<the repository's test key>'
```

thrown by `ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest>.Builder.Add` at
`TransitivePipelineContributorSource.cs:165`, reached through `AspectPipeline.CreatePipelineContributorSources`.

## Why the collision exists

This is the simplest member of the #1749 family. It needs no signing trick, no culture trick and no shared source file:
only **two projects with the same `<AssemblyName>` and the same version**, both referenced by one consumer. Their
`AssemblyIdentity` values are then equal, and the collector adds one manifest per referenced identity.

It also fires **earlier** than the closure collision that `Issue1749.PublicKeyVariants` targets, during
pipeline-contributor collection rather than during compile-time serialization. That is why the two scenarios must keep
their versions different: equal versions reach this site first and mask the other one.

## The surprise that makes it buildable

Roslyn reports **no** diagnostic at all for two references whose identities are equal. Not `CS1703`, not `CS1704`, not a
warning. Verified with a `MetalamaEnabled=false` control using the identical injection: zero warnings, zero errors.

That is the opposite of the same-simple-name-different-version case, which is `CS1704` when both assemblies are
weak-named. The rule, measured across the matrix:

| Two references, same simple name | Result |
|---|---|
| equal identities | accepted, no diagnostic |
| both weak-named, different versions | **`CS1704`** |
| same key, different versions | accepted |
| different public keys | accepted |
| different cultures | accepted |

`extern alias` does not rescue the `CS1704` case, because it is a reference-binding error rather than a name-lookup one.
And Metalama never reads reference aliases: `Properties.Aliases` appears nowhere in `Metalama.Framework.Engine` or
`Metalama.Framework.DesignTime`.

## Why the consumer names nothing

Both variants declare `SharedAspects.InheritedAspect`, so naming it would be `CS0433`. The consumer does not need to:
the collector walks every reference regardless of use. Each variant also declares a differently named target class
(`ExportedBaseA`, `ExportedBaseB`) so that the two assemblies are not interchangeable and the scenario is not a
degenerate one.

## The fix

`TransitivePipelineContributorSource` now checks `ContainsKey` before the `Add`, keeps the first reference and logs the
one it drops. Keeping the first is consistent rather than a fresh loss of information: `CompileTimeProjectRepository`'s
own cache is keyed by `AssemblyIdentity`, so the second reference already resolved to the first one's
`CompileTimeProject` before this code ran.

## What `test.json` asserts

Exit code 0 and no duplicate-key message. The compiler is content with the two references, so with the `Add` guarded the
build simply succeeds. `IgnoreExitCode` is deliberately **not** set: the exit code is the assertion.

The forbidden pattern matches on the exception message rather than on a diagnostic id, so that the same `test.json`
works for the design-time twin, where the same message arrives wrapped in `CS8785` instead of `LAMA0001`.
