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

`UseSharedCompilation=false` is **not** passed, deliberately. The default shared compiler is what makes this scenario
cover two distinct defects in one build, and passing the flag would hide the second one.

## The second defect: two projects, one compiler process, one domain

With the shared compiler, `Middle1` and `Middle2` are compiled **in the same process**. `Middle1` loads `Contract 1.0`
into the `CompileTimeDomain`, and `Middle2` then used to fail:

```
CSC : error LAMA0001: Cannot load '...\Metalama\CompileTime\Contract\...\Contract.dll':
Could not load file or assembly 'Contract, Version=1.1.0.0, ... PublicKeyToken=9f073587addbe099'.
Assembly with same name is already loaded
```

`LAMA0079` cannot catch this, and should not: the two compilations are each perfectly valid. `Middle1` references one
version of `Contract` and `Middle2` references another, and neither closure contains a conflict. The reservation table
that reports `LAMA0079` is an instance field of `CompileTimeProjectRepository.Builder`, so it only ever sees one
compilation. The versions meet in the domain, not in a closure, so the answer is isolation rather than a diagnostic.

`AspectPipeline` chooses a domain through `ICompileTimeDomainFactory.GetOrCreateDomain`, which reuses one only when
`CompileTimeDomain.IsCompatibleWithAssemblies` accepts the assemblies it is given. That mechanism was already correct;
its **input set** was not. It received only the extension assemblies, and an untransformed compile-time assembly like
`Contract` is an ordinary reference rather than an extension, so the conflict was invisible and the domain was reused.
`GetUntransformedCompileTimeAssemblyPaths` now adds those references to the set, and the two projects get separate
domains.

Only untransformed assemblies need to be listed: a transformed projection is renamed to `ml!<name>_<hash>`, unique per
content, so it cannot conflict with anything.

## What `test.json` asserts, and why it is deterministic in CI

The scenario now pins **both** defects with no flags to pass:

- `error LAMA0079.*Contract` must appear, which is the closure collision inside `Consumer`.
- `Assembly with same name is already loaded` must **not** appear, which is the cross-compilation collision above. That
  string is the whole assertion, because the failure had no diagnostic id of its own: it arrived as `LAMA0001`, the
  generic unhandled-exception report.

Both are exercised by the harness's own plain `dotnet build`, so the regression is caught by an ordinary CI run rather
than by a locally supplied property.

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
