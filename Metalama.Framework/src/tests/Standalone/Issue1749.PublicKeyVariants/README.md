# Issue 1749 — two compile-time projects with one compile-time assembly name

**Passing.** The build succeeds. This configuration used to crash with an unhandled `ArgumentException`, and it is now
simply supported.

It supersedes `Issue1749.SameCompileTimeName`, which attempted the same thing and concluded it was impossible. That
conclusion was wrong on two separable, measurable counts. See the bottom of this file.

## What it used to produce, before the fix

```
CSC : error LAMA0001: Unexpected exception occurred in Metalama:
An item with the same key has already been added. Key: ml!Contract_<16 hex digits>
```

thrown by `Enumerable.ToDictionary` at
`CompileTimeProject.ClosureProjectsByCompileTimeAssemblyName` (`CompileTimeProject.cs:105`), reached through
`TryGetProjectByCompileTimeAssemblyName` → `CompileTimeSerializationBinder.BindToName` →
`TransitiveAspectsManifest.ToResource`.

It now builds, with the two assemblies projected to two distinct compile-time assemblies that coexist in the closure.

## Why the collision exists

The compile-time name of a transformed project is `ml!<name>_<hash16>`. `ComputeProjectHash` covers the build id, the
assembly identity's **name and version**, the reference compile-time names, the source hash, a handful of project
options and the Roslyn API version. It never covers the **public key** or the **culture**.

`ClosureProjects` deduplicates on `UniqueKey = "{RunTimeIdentity} -> {CompileTimeIdentity}"`, which does see the public
key. So the closure keeps both projects and the dictionary keyed on the compile-time name cannot.

That gap is not incidental. The two identity components the C# compiler permits to differ so that two assemblies of one
simple name can be loaded side by side are exactly public key and culture. Everything csc rejects (a version difference
between two weak names, which is `CS1704`) the hash also separates. The correspondence is exact.

## The configuration

| Project | Assembly | Differs by |
|---|---|---|
| `Contract1` | `Contract`, signed with `Contract.snk` | public key |
| `Contract2` | `Contract`, unsigned | |
| `Consumer` | injects both into `@(ReferencePath)` | |

Both `Contract` projects compile the single file `Shared/Helper.cs` and nothing else, and Metalama is **enabled** for
both, so each embeds a compile-time project resource. The consumer therefore resolves them through
`TryDeserializeCompileTimeProject` (`Builder.cs:375`), not through `TryCreateUntransformed`.

Three details are load-bearing:

- **The `<Compile>` include must be normalized** with `[System.IO.Path]::GetFullPath`. `ComputeSourceHash` hashes each
  compile-time syntax tree's `FilePath` as well as its text, and Roslyn's command-line parser does not collapse a `..`
  segment, so a project-relative `..\Shared\Helper.cs` gives the two projects different source hashes, different
  compile-time names, no collision, and a green test that asserts nothing.
- **`Contract` declares no aspect, fabric, template provider or option type.** A duplicate of any of those is reported
  before the closure dictionary is built, and the scenario would assert the wrong failure.
- **The consumer never names a type of `Contract`.** That would be `CS0433`, because the type exists in both
  assemblies. It does not need to: the dictionary is built for the whole closure on the first lookup of any key, so
  serializing the consumer's own inheritable aspect is enough to trigger it.

## The fix, and why it is not a diagnostic

`ComputeProjectHash` now hashes the **full** assembly identity rather than the name and version, so the compile-time name
again means what it is supposed to mean: one `ml!` name per distinct content. The two `Contract` assemblies get two
distinct compile-time names and the dictionary keyed on that name cannot collide.

Reporting an error instead was the first thing tried here, by reserving the compile-time name on the transformed branch
the way `Builder.cs:401` does on the untransformed one. That was wrong, and the reason is worth recording:

- Several compile-time projections of one run-time assembly in a single closure are **legitimate and expected**. The
  per-TFM copies of a multi-targeted library are exactly that: same run-time name, same version, different `ml!` names.
  `ClosureProjectsGroupedByRunTimeAssemblyName` and `CanReuseLiveManifest` exist to detect the resulting ambiguity and
  fall back to the safe path, not to forbid the situation.
- So rejecting this configuration would have rejected a shape the design already supports, on the grounds of a hash that
  was simply incomplete.

The untransformed branch still reserves, and must. There the compile-time name **is** the run-time name, so two versions
of such an assembly claim one name and a single `AssemblyLoadContext` cannot hold both. That conflict is unavoidable
rather than an artefact of the hash, which is why `LAMA0079` remains the right answer for it and `Standalone/Issue1749`
still asserts it.

## What `test.json` asserts

Exit code 0, no `LAMA0001` duplicate-key crash, and no `LAMA0079` either: the point of the scenario is that the
configuration is now supported rather than diagnosed.

## Why `Issue1749.SameCompileTimeName` concluded this was impossible

Two reasons, both measured rather than argued:

1. It set `<TargetName>` to a distinct value per project so that both outputs could sit in one directory. The SDK feeds
   `TargetName` into the assembly identity, so the identities came out as `SignedVariant` and `UnsignedVariant` and the
   hashes differed through the name. A distinct `<PackageId>` is what is actually needed, since with one `AssemblyName`
   and no explicit `PackageId` restore fails on an ambiguous project name. This scenario avoids the problem entirely by
   leaving both outputs in their own `bin` directories and injecting absolute paths.
2. It included the shared source as `..\Shared\SharedAspect.cs`, so the two source hashes differed through the file
   path, as described above.

Its README also called the `CS0433` obstacle decisive. It is not: routing the aspect so that the consumer never names
the ambiguous type is sufficient, and here the consumer does not even need the aspect.
