# Issue #1722 — deterministic reproduction

Reproduces https://github.com/metalama/Metalama/issues/1722 (namespaces anonymized; the reporter's `FlyingChi.*`
names are not used): a `FileNotFoundException` raised while evaluating aspect **eligibility** when an aspect
NuGet package that references a compile-time helper package is consumed and the helper package is then upgraded
to a new version.

## How to run

```
powershell -NoProfile -ExecutionPolicy Bypass -File repro.ps1 -MetalamaVersion <local-version>
```

Exit code `0` = the final consumer build succeeded = issue #1722 is fixed.
Non-zero = the final consumer build failed with `FileNotFound … ml!Issue1722.Primitives_<hash> … while
evaluating eligibility` = the bug is still present.

`Issue1722.proj` is a thin MSBuild wrapper that invokes `repro.ps1`. **Run `repro.ps1` directly** — see the
"Harness caveat" below.

## What it does
1. Packs `Issue1722.Primitives` **1.0.0**, `Issue1722.Aspects` **1.0.0** (built against Primitives 1.0.0, so its
   embedded compile-time manifest is frozen at Primitives 1.0.0), and `Issue1722.Primitives` **2.0.0** (identical
   source, only the version differs) into a local feed.
2. Empties the compile-time cache.
3. **Step A** — consumes `Aspects 1.0.0` + `Primitives 1.0.0`. Succeeds, and caches
   `ml!Issue1722.Aspects_<hash>` referencing `ml!Issue1722.Primitives @ 1.0.0`.
4. **Step B** — upgrades to `Primitives 2.0.0` (Aspects stays 1.0.0) and rebuilds, reusing the compile-time
   cache. This is where the bug fires.

Every `dotnet` call is launched as an independent top-level process (`Start-Process -Wait`) so the two consumer
builds do not share an in-process compile-time domain (see the caveat).

## Root cause
`ml!<name>_<hash>` is `CompileTimeCompilationBuilder.ComputeProjectHash`. For a referenced project it folds in
that reference's **source hash only** (`reference.Hash`), NOT its identity/version. But the reference's own
`ml!` name derives from its FULL hash, which DOES include the version:
- `ml!Issue1722.Primitives_<hashA>` = Primitives @ 1.0.0
- `ml!Issue1722.Primitives_<hashB>` = Primitives @ 2.0.0

Primitives 1.0.0 and 2.0.0 have identical source, so the **aspect's** cache key is unchanged. In step B the
stale `ml!Aspects` (referencing `<hashA>`) is reused while the domain loads `<hashB>`.
`CompileTimeDomain.ResolveAssembly` has **no path fallback** for referenced package compile-time assemblies, so
the mismatch is a fatal `FileNotFoundException` — during eligibility, the first place the compiled `ml!Aspects`
delegate's baked assembly reference is JITed.

This explains the reporter's clues: eligibility fails while templates work (templates resolve types via the code
model against the loaded assembly; only the compiled eligibility delegate carries the stale baked reference);
"rewriting the extension call as a static call fixes it" is incidental (identical IL) — editing recompiles
`ml!Aspects` against the current Primitives hash.

## Proposed fix
1. Include the referenced project's **full identity/hash** (its `CompileTimeIdentity` / `ComputeProjectHash`),
   not just its source hash, in `ComputeProjectHash`, so the aspect's `ml!` assembly invalidates when a
   dependency's identity changes and the baked reference always matches the loaded name.
2. And/or give `CompileTimeDomain.ResolveAssembly` a path fallback for referenced package compile-time assemblies
   (register each `CompileTimeProject.CompiledAssemblyPath` by `CompileTimeIdentity.Name`), so a residual
   mismatch degrades gracefully instead of throwing.

## Harness caveat (why `repro.ps1` must run top-level)
The bug only manifests when step A and step B run in **separate** compile-time domains. When the two builds run
under one parent MSBuild invocation (e.g. `Issue1722.proj` built by `ManyDotNetSolutions`, or the builds run as
`<Exec>` children), MSBuild keeps a worker process warm that both builds reuse, so `ml!Primitives @ 1.0.0` stays
loaded and the reference resolves — the bug **self-heals**. This masking is the same reason the reporter's
failure was intermittent and disappeared after an edit/rebuild.

`repro.ps1` avoids this by launching each build as an independent top-level process, which reproduces
deterministically from an empty cache. It could not be made to fail from inside a parent MSBuild despite
`--disable-build-servers`, `-nodeReuse:false`, disabling the MSBuild server, killing worker nodes, and
`Start-Process`/`cmd start` detachment. A committed regression test should therefore invoke `repro.ps1` as a
top-level process (e.g. from an integration test) rather than relying on the `.proj` being built in-place.
