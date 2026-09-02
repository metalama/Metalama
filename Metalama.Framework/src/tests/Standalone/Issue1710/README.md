# Issue #1710 — reproduction (open in an IDE)

Reproduces https://github.com/metalama/Metalama/issues/1710: a design-time `InvalidCastException` while merging
hierarchical `ContractOptions` that belong to two different TFM-specific compile-time copies of
`Metalama.Patterns.Contracts`.

## Structure

| Project | TFM | Contracts copy it gets |
|---|---|---|
| `Issue1710.Library` | `netstandard2.0` | `lib/netstandard2.0` → `ml!Metalama.Patterns.Contracts_<hashA>` |
| `Issue1710.App` | `net472` | `lib/net472` → `ml!Metalama.Patterns.Contracts_<hashB>` |

`Metalama.Patterns.Contracts` multi-targets `net472;net10.0;netstandard2.0`, so each consuming project resolves a
different `lib/<tfm>` assembly, each carrying its own embedded compile-time project. Because the compile-time
assembly name embeds a hash of the target framework (`CompileTimeCompilationBuilder.ComputeSourceHash`), the two
copies are **distinct CLR assemblies** whose `ContractOptions` types share a full name but not an identity.

`Library.Base.SetValue` declares a `[Positive]` contract on a **virtual** parameter; `App.Derived` overrides it, so
the contract is **inherited across the project boundary**. `[Positive]` derives from `ContractBaseAttribute`, which
is conditionally inheritable: `IConditionallyInheritableAspect.IsInheritable` calls
`targetDeclaration.GetContractOptions()`, triggering the hierarchical-options merge on the *derived* declaration —
merging Library's netstandard2.0-copy options against App's net472-copy options.

## How to run

Requires `Build.ps1 build` first (the projects consume `Metalama.Patterns.Contracts` at `$(MetalamaVersion)` from
the repo's local package feed, `artifacts/publish/private`).

Then **open `Issue1710.sln` in the IDE** (this is a *design-time* issue).

- **Bug present**: the IDE reports an `InvalidCastException` — `[A]Metalama.Patterns.Contracts.ContractOptions
  cannot be cast to [B]Metalama.Patterns.Contracts.ContractOptions`, with type A and type B originating from two
  different `ml!Metalama.Patterns.Contracts_<hash>` assemblies **in the same** `MetalamaAssemblyLoadContext` —
  typically surfaced as a Metalama error on `Derived` and/or a telemetry crash report.
- **Bug absent**: no such error.

## Why `dotnet build` does not reproduce it

`dotnet build Issue1710.sln` **succeeds**. Each project is compiled in its own process, so each gets its own
`CompileTimeDomain` and the two copies never meet. At design time a single process serves the whole solution, and
`AspectPipeline` obtains its domain via `ICompileTimeDomainFactory.GetOrCreateDomain(...)`, which **reuses** a
domain whenever `CompileTimeDomain.IsCompatibleWithAssemblies` passes. That check only ever receives the project's
*extension* assembly paths — never the compile-time assemblies — so for these projects, which have none, it is
handed an empty collection, returns `true` trivially, and both copies end up in one domain.

Note this is *not* why the cast fails: domains were segregated experimentally and the failure was unchanged. See
"Rejected alternative" in `docs/compile-time-target-frameworks.md`. The copies meeting in one domain and the cast
failing are both consequences of the per-TFM hash, not of each other.

A useful sanity signal that the setup is correct even when it does not crash: the build emits `LAMA5007` for
`Derived.SetValue(int)/value` in **Issue1710.App**, for a contract declared in **Issue1710.Library** — proving the
contract is inherited across the project/TFM boundary and that `GetContractOptions()` ran.

## Open question this repro is meant to answer

The crash was reported only from **Rider** (`JetBrains.Roslyn.Worker`). However, the domain-reuse code path above
(`GetOrCreateDomain`, introduced by #579) is **host-independent**, and the underlying mismatch reproduces in a
plain unit test with no IDE at all
(`Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/CrossTfmInheritedOptionsTests`). So it is not established
that Visual Studio is immune; the single Rider report may simply be the only telemetry sample.

**Open this solution in both Rider and Visual Studio** and compare:

- Fails in both → the bug is host-independent; the "Rider-only" framing (and the corresponding claim in
  `docs/compile-time-target-frameworks.md`) is wrong.
- Fails only in Rider → something host-specific governs domain/assembly-load-context lifetime, and *that* is what
  should be understood before adding cost to the shared design-time path.
