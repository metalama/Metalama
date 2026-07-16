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
   compile-time assembly. In IDEs that run one shared analysis process across all projects regardless of TFM —
   notably **JetBrains Rider** (`JetBrains.Roslyn.Worker`) — those copies are loaded side by side into the same
   `AssemblyLoadContext`. (Visual Studio isolates analysis differently, so the copies do not meet.) This is a
   normal, expected state, **not** a bug.

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

- `D`'s own compile-time closure resolves the shared library to copy `B` (by run-time identity, via its
  repository).
- The inherited artifacts arrive **bound to copy `A`**:
  - As a `CompilationReference` (design time), `D` reads `U`'s live in-memory transitive manifest
    (`ITransitiveAspectManifestProvider.GetTransitiveAspectsManifest`), whose objects are copy-`A`-typed.
  - As a `PortableExecutableReference` (built assembly), `D` deserializes `U`'s embedded manifest anchored to
    `U`'s `CompileTimeProject` (issue #1611), which resolves types through `U`'s closure — again copy `A`.
  - Reuse of the upstream's already-built `CompileTimeProject` for a `CompilationReference`
    (`IUpstreamCompileTimeProjectProvider`, issue #1611) further ensures copy `A` is the one brought into `D`'s
    domain for `U`'s content.

Resolving "to the right copy" for these inherited artifacts is the crux of the doctrine.

## Known violation — issue #1710

Hierarchical options currently violate point 6 at the inheritance boundary. `HierarchicalOptionsManager`
maintains one `OptionTypeNode` per option type **keyed by `Type.FullName`** (copy-agnostic), whose canonical
`Type` is the **downstream** project's copy (`B`). When `D` evaluates options for a declaration that inherits
from `U`, the base-declaration options are provided by `TransitivePipelineContributorSource.TryGetOptions` from
`U`'s manifest — i.e. **copy-`A`-typed** — and are merged against `D`'s copy-`B`-typed default/namespace options
in `OptionTypeNode.MergeOptions`. The user-defined `ApplyChanges` casts its argument to its own copy's type
(`(ContractOptions) changes`) and throws `InvalidCastException`, because copy `A` and copy `B` are distinct CLR
types. In other words, the inherited options are **not resolved/normalized to the downstream project's copy**
before the merge.

This is observed only at design time in Rider, because that is where two copies of the shared library's
compile-time assembly are loaded into a single `AssemblyLoadContext` (see point 5).

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
- `IUpstreamCompileTimeProjectProvider` / `DesignTimeUpstreamCompileTimeProjectProvider` — reuse of the upstream
  copy for `CompilationReference` (issue #1611).
- `HierarchicalOptionsManager` / `HierarchicalOptionsManager.OptionTypeNode` — option merge; site of issue #1710.
- `TransitivePipelineContributorSource.TryGetOptions` — supplies upstream-copy-typed inherited options.
