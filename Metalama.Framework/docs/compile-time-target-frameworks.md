# Compile-time projects and target frameworks

This document describes the doctrine that governs how Metalama builds and identifies the **compile-time
assembly** of a project across target frameworks (TFMs), and the consequences for how those assemblies coexist
and must be resolved at design time. It reflects the intended design, verified against the implementation in
`Metalama.Framework.Engine.CompileTime`.

## Background

For every project that contains compile-time code (aspects, templates, fabrics, hierarchical options, etc.),
Metalama extracts that code and compiles it into a separate **compile-time assembly**, represented at runtime by
a `CompileTimeProject`. The compile-time assembly is named `ml!<run-time-assembly-name>_<hash>` (see
`CompileTimeCompilationBuilder.CompileTimeAssemblyPrefix` and `OutputPathHelper.GetOutputPaths`). It is loaded
into a `CompileTimeDomain`, which owns a `MetalamaAssemblyLoadContext` (a .NET `AssemblyLoadContext`).

## The doctrine

1. **Run-time TFMs are a run-time concern of the user project.** A user library (for example
   `Metalama.Patterns.Contracts`, which multi-targets `net472;net8.0;netstandard2.0`) declares whichever TFMs it
   wants to support **at run time**. Declaring a TFM says nothing about the compile-time process — the author is
   not thinking about Metalama's compile-time compilation when choosing TFMs.

2. **The compile-time compilation always targets `netstandard2.0`.** Regardless of the consuming project's
   run-time TFM, the compile-time assembly is compiled against the `netstandard2.0` reference assemblies
   (`CompileTimeAssemblyLocator` reads `assemblies-netstandard2.0.txt`) and with the `NETSTANDARD_2_0`
   preprocessor symbol defined (`CompileTimeCompilationBuilder.CreateEmptyCompileTimeCompilation`). There is only
   ever one *flavor* of compiled compile-time IL: netstandard2.0.

3. **Metalama builds one compile-time assembly per run-time TFM.** Even though the compilation targets
   netstandard2.0, the compile-time *source* can differ per run-time TFM, because the compile-time code is
   extracted from the run-time syntax trees, which were parsed with the **consuming project's** preprocessor
   symbols. Dead `#if` branches are therefore stripped according to the run-time TFM
   (`RemovePreprocessorDirectivesRewriter` removes `DisabledTextTrivia`; `DirectiveTrivia.IsActive` reflects the
   run-time compilation's symbols). Consequently:
   - `ComputeSourceHash` mixes the run-time `targetFramework` and the declared preprocessor symbols into the
     hash, so each run-time TFM yields a **distinct** compile-time assembly identity (`ml!…_<hash>`).
   - Two TFMs whose compile-time source happens to be identical after dead-code removal (e.g. a library with no
     compile-time `#if`) still produce **distinct identities**, because the TFM is part of the hash. This
     redundancy is deliberate: the hash is a conservative over-approximation that never serves a stale/wrong
     build for a TFM whose compile-time code *does* differ.

4. **All per-TFM copies share the same run-time `AssemblyIdentity`.** `ml!Contracts_<hashNet472>` and
   `ml!Contracts_<hashNetStd>` are distinct assemblies (distinct CLR `Type` identities for the same
   `ContractOptions.FullName`), but both correspond to the same logical run-time identity
   `Metalama.Patterns.Contracts, Version=…`.

5. **Multiple copies can — and are expected to — coexist in one `CompileTimeDomain`.** When a solution contains
   projects on different TFMs, each project's pipeline builds/loads its own per-TFM copy of a shared library's
   compile-time assembly. `AspectPipeline` obtains its domain via `ICompileTimeDomainFactory.GetOrCreateDomain`,
   which **reuses** an existing domain whenever `CompileTimeDomain.IsCompatibleWithAssemblies` passes.

   Note what that check actually receives: `AspectPipeline.TryInitialize` passes only the **extension assembly**
   paths (`IExtensionLoader.GetExtensionAssemblyPaths`), never the compile-time assemblies — which do not exist yet
   at that point, since the domain is chosen *before* `CompileTimeProjectRepository.Create` runs. So the per-TFM
   copies are not judged compatible; they are never compared at all. For a project with no extension assemblies
   (the common case) the collection is **empty**, the check returns `true` trivially, and the first live domain is
   reused unconditionally. Both copies therefore load side by side into the same `AssemblyLoadContext`. This is a
   normal, expected state, **not** a bug — and, per the rejected alternative below, not the cause of #1710 either.

   Independently of #1710, `IsCompatibleWithAssemblies` would not separate the copies even if it were given them:
   it compares by simple name, and `ml!<name>_<hashA>` and `ml!<name>_<hashB>` have *distinct simple names*, so
   they never look conflicting. Segregating on the underlying **run-time** identity would be a correctness
   improvement in its own right, but it is not a fix for #1710.

   This is **host-independent**: the code path has no IDE-specific branch. It has been verified experimentally
   (see `src/tests/Standalone/Issue1710`) that **both JetBrains Rider and Visual Studio** load the two copies into
   one domain and hit the resulting failure. Rider merely *surfaces* the error while Visual Studio's Roslyn
   out-of-process host (`ServiceHub.RoslynCodeAnalysisService`) logs it without surfacing it — a reporting
   difference, not a behavioural one. The underlying mismatch also reproduces with no IDE at all, in
   `Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/CrossTfmInheritedOptionsTests`.

   It is, however, **design-time only**: at build time each project is compiled in its own process, so each gets
   its own `CompileTimeDomain` and the copies never meet. (`dotnet build` on the Issue1710 solution succeeds.)

6. **The consumer must resolve to the *right* copy.** Because several copies with the same logical identity may
   be loaded, every place that maps a symbol/name to a compile-time `Type` must resolve to the copy that belongs
   to the project currently being processed. Within a single pipeline this is handled by
   `CompileTimeProjectRepository`, whose `_projects` dictionary is keyed by run-time `AssemblyIdentity` and
   therefore holds exactly one copy per logical identity; `ProjectSpecificCompileTimeTypeResolver` resolves types
   through that repository. The correctness requirement is that objects and types flowing **across** project
   boundaries (inherited aspects, inherited options, deserialized manifests) are resolved to the copy of the
   project that consumes them, not left bound to the copy of the project that produced them.

## Why this matters: cross-project inheritance

The delicate case is inheritance across a project boundary between two different TFMs. Consider an upstream
project `U` (e.g. `net472`, using copy `A` of a shared library) and a downstream project `D` (e.g. `net8.0`,
using copy `B`) where `D` references `U` and inherits an aspect or hierarchical options from it.

`D`'s own compile-time closure resolves the shared library to copy `B` (by run-time identity, via its
repository). The inherited artifacts, however, originate from `U` and are therefore naturally bound to copy `A`.
Resolving them "to the right copy" — i.e. to `D`'s copy `B` — is the crux of the doctrine, and is what the
resolution below implements.

Note that a consumer cannot re-materialize a producer-copy object on its own: to *serialize* an object the
binder must be able to name the copy its type comes from, and only the producing project's closure can name its
own copy. This is why the conversion is a producer-serializes / consumer-deserializes handshake rather than
something `D` can do after the fact.

## Past violation — issue #1710 (fixed)

Hierarchical options used to violate point 6 at the inheritance boundary. `HierarchicalOptionsManager` maintains
one `OptionTypeNode` per option type **keyed by `Type.FullName`** (copy-agnostic), whose canonical `Type` is the
**downstream** project's copy (`B`). When `D` evaluated options for a declaration inheriting from `U`, the
base-declaration options were provided by `TransitivePipelineContributorSource.TryGetOptions` from `U`'s manifest
— i.e. **copy-`A`-typed** — and were merged against `D`'s copy-`B`-typed default/namespace options in
`OptionTypeNode.MergeOptions`. The user-defined `ApplyChanges` casts its argument to its own copy's type
(`(ContractOptions) changes`) and threw `InvalidCastException`, because copy `A` and copy `B` are distinct CLR
types. In other words, the inherited options were **not resolved/normalized to the downstream project's copy**
before the merge.

This was a **design-time** failure, because that is where two copies of the shared library's compile-time assembly
are loaded into a single `AssemblyLoadContext` (see point 5); at build time each project gets its own domain. It
was **not** specific to an IDE: it was observed in both Rider and Visual Studio, and reproduces with no IDE at all
in `CrossTfmInheritedOptionsTests`. It was originally reported only from Rider because Rider surfaces the error,
whereas Visual Studio logs it in `ServiceHub.RoslynCodeAnalysisService` without surfacing it — measured on the
`Issue1710` solution, the Visual Studio log contained 8396 occurrences of the cast failure while the IDE showed
nothing.

### Resolution

The inherited manifest is now **deserialized into the consuming project's copy** (point 6), rather than kept
bound to the producer's copy. The serialized form is compilation-neutral by definition — compile-time types are
always written as their run-time names — so the resolution to a particular copy happens entirely on the two ends:
who names the type when writing, and whose closure resolves the name when reading. The producer must serialize
with its own service provider, because only its closure can *name* (resolve) its own compile-time copy; a consumer
cannot serialize a producer-copy object, since that copy is not in its closure. The consumer then deserializes
those bytes with its own service provider, whose binder resolves each run-time name to the **consumer's**
compile-time copy. The whole manifest (inherited aspects and options) is thereby bound to the consumer's copy, so
an inherited aspect's `IsInheritable`/`BuildAspect` runs in the consumer's copy, its option query resolves to the
consumer's option type, and the merge no longer crosses copies.

- **Project references (`CompilationReference`)**: `DesignTimeAspectPipeline.GetDesignTimeProjectVersionAsync`
  serializes the referenced project's manifest using that project's own service provider — read from
  `DesignTimeAspectPipeline.CurrentConfiguration` rather than by requesting a fresh configuration, so a *paused*
  reference pipeline still yields a manifest — and carries it on
  `DesignTimeProjectReference.SerializedTransitiveAspectManifest`.
  `TransitivePipelineContributorSource.Create` then deserializes it with the **consuming** project's service
  provider.
- **Package references (`PortableExecutableReference`)**: also deserialized with the consuming project's service
  provider. This replaces the upstream anchoring introduced by issue #1611, which is no longer needed here: the
  consumer's closure already contains the canonical upstream projection (via
  `IUpstreamCompileTimeProjectProvider`), so an inherited aspect deserialized in the consumer's copy still matches
  the consumer's `IAspectClass.Type`.

`DesignTimeProjectReference` carries both the live manifest and its serialized form. They are always both present
or both absent, and are not interchangeable: the serialized form feeds the engine (above), while the live object
is required by `DesignTimeProjectVersion.ReferencedExtensions`, which needs the concrete
`DesignTimeAspectPipelineResult` to read its design-time extension collections — a shape the serialized manifest
does not carry.

**Validation.** On the `Issue1710` solution in Visual Studio, the `ServiceHub.RoslynCodeAnalysisService` log went
from **8396** cast failures (before) to **0** (after), with no `ERROR` lines at all — while **both** per-TFM copies
were still loaded, confirming the fix corrects the *merge* rather than suppressing the (expected) coexistence.

### Rejected alternative: segregating the `CompileTimeDomain`s

A natural intuition is that the bug is caused by the two copies being loaded into the *same* domain (point 5), and
that segregating domains would fix it. **It would not, and this was verified experimentally.**

Substituting an `ICompileTimeDomainFactory` that never reuses a domain — a stronger segregation than any real
implementation, since it is unconditional — and rerunning the `CrossTfmInheritedOptionsTests` scenario produced
**4 distinct domains**, with the two copies in genuinely different `MetalamaAssemblyLoadContext`s (`#6` for
`.netframework4.7.2`, `#4` for `.netstandard2.0`, versus both in `#4` when domains are shared). The
`InvalidCastException` still occurred, unchanged. Rerunning the same experiment *with* the fix above gave no error
and a successful pipeline, still across 4 domains — so the outcome tracks the fix, not the domain topology.

The reason is that the cast does not care which `AssemblyLoadContext` a type lives in — only that copy `A` and copy
`B` are two different `Type`s, which is already true when they share a domain. Separating them cannot make them the
same type; it only makes unification more impossible. Domain sharing is a *sibling consequence* of the per-TFM
hash, not the cause: the causal chain is `ComputeSourceHash` folds in the TFM → two copies exist → `U`'s pipeline
binds its options to copy `A` → those objects flow into `D`'s pipeline → they meet `D`'s copy-`B` options in
`MergeOptions` → the cast fails. No link in that chain is a domain.

Only two levers cut that chain: re-materialize the inherited artifacts in the consumer's copy (the resolution
above), or make the two copies one (see the `ComputeSourceHash` observation below).

**Known cost.** The manifest is serialized and deserialized for every cross-project reference on every design-time
pipeline execution, including the common case where the producer's and consumer's copies are identical and the
round-trip is pure overhead. A "pay only when the copies differ" fast path is the natural follow-up: comparing the
producer's and consumer's `CompileTimeProject` copies depends only on configuration-scoped state, so unlike the
deserialized graph — which binds to the compilation via generic `CompileTimeType` (`SerializationReader`) and is
therefore *not* configuration-scoped — that decision can legitimately be cached in the pipeline configuration.

## Observations and potential issues (to revisit, not yet fixed)

- **Force-defining `NETSTANDARD_2_0` is confusing and probably wrong.**
  `CompileTimeCompilationBuilder.CreateEmptyCompileTimeCompilation` appends `"NETSTANDARD_2_0"` to the
  preprocessor symbols of the parse options it builds (around line 392). This is misleading: the fact that the
  compile-time compilation is built against the netstandard2.0 *reference assemblies* is a property of the
  reference set, not something that should be surfaced as a user-visible preprocessor symbol. It conflates
  "compiled against netstandard2.0 references" with "the consumer targets netstandard2.0", and it means any code
  that reaches this compilation with a live `#if NETSTANDARD_2_0` would be forced into its netstandard2.0 branch
  regardless of the consuming project's real TFM — which contradicts point 3 (dead-code removal is supposed to
  follow the *consuming* project's TFM). The compile-time code trees themselves are parsed separately with no
  preprocessor symbols and have already had their directives stripped, so in practice the symbol currently only
  affects the predefined-types trees; but the symbol should most likely not be defined at all. Flagged for
  removal/clarification; not changed here.

## Key code references

- `CompileTimeCompilationBuilder.CreateEmptyCompileTimeCompilation` — always compiles against netstandard2.0.
- `CompileTimeCompilationBuilder.ComputeSourceHash` — mixes run-time `targetFramework` + declared symbols into
  the identity (per-TFM copies).
- `RemovePreprocessorDirectivesRewriter` — strips dead `#if` branches per run-time TFM.
- `OutputPathHelper.GetOutputPaths` — builds the `ml!<name>_<hash>` compile-time assembly name.
- `CompileTimeProjectRepository` — one copy per run-time `AssemblyIdentity` within a pipeline.
- `ProjectSpecificCompileTimeTypeResolver.GetCompileTimeNamedTypeCore` — resolves types via the repository.
- `ICompileTimeDomainFactory.GetOrCreateDomain` / `CompileTimeDomain.IsCompatibleWithAssemblies` — domain reuse;
  why the per-TFM copies end up in one `AssemblyLoadContext` (see point 5).
- `IUpstreamCompileTimeProjectProvider` / `DesignTimeUpstreamCompileTimeProjectProvider` — reuse of the upstream
  project's own projection for `CompilationReference` (issue #1611).
- `HierarchicalOptionsManager` / `HierarchicalOptionsManager.OptionTypeNode` — option merge; site of issue #1710.
- `ITransitiveAspectManifestProvider.GetSerializedTransitiveAspectsManifest` — supplies the referenced project's
  manifest in serialized form, for deserialization into the consumer's copy (issue #1710).
- `TransitivePipelineContributorSource.TryGetOptions` — supplies inherited options; consumer-copy-typed since the
  manifest is deserialized with the consuming project's service provider.
- `src/tests/Standalone/Issue1710` — IDE reproduction (open in an IDE; `dotnet build` succeeds by design).
- `Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/CrossTfmInheritedOptionsTests` — in-process regression
  test, no IDE required.
