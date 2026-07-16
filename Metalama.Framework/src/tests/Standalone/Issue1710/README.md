# Issue #1710 — reproduction (open in an IDE)

Reproduces https://github.com/metalama/Metalama/issues/1710: a design-time `InvalidCastException` while merging
hierarchical `ContractOptions` that belong to two different TFM-specific compile-time copies of
`Metalama.Patterns.Contracts`.

## Structure

| Project | TFM | Contracts copy it gets |
|---|---|---|
| `Issue1710.Library` | `netstandard2.0` | `lib/netstandard2.0` → `ml!Metalama.Patterns.Contracts_<hashA>` |
| `Issue1710.App` | `net472` | `lib/net472` → `ml!Metalama.Patterns.Contracts_<hashB>` |

`Metalama.Patterns.Contracts` multi-targets `net472;net8.0;netstandard2.0`, so each consuming project resolves a
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
domain whenever `CompileTimeDomain.IsCompatibleWithAssemblies` passes. Since the copies have distinct simple names
(`ml!…_<hash>`), they never look conflicting, the domain is reused, and both copies end up loaded together.

A useful sanity signal that the setup is correct even when it does not crash: the build emits `LAMA5007` for
`Derived.SetValue(int)/value` in **Issue1710.App**, for a contract declared in **Issue1710.Library** — proving the
contract is inherited across the project/TFM boundary and that `GetContractOptions()` ran.

## Is it Rider-only? No — answered

The crash was reported only from **Rider** (`JetBrains.Roslyn.Worker`), but this repro established that the
"Rider-only" framing is **wrong**. The domain-reuse code path above (`GetOrCreateDomain`, introduced by #579) is
**host-independent**, and the mismatch also reproduces with no IDE at all
(`Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/CrossTfmInheritedOptionsTests`).

Opening this solution in **both** IDEs (after clearing `%TEMP%\Metalama`, since a stale compile-time cache can mask
the failure) shows:

- **Rider**: fails, and surfaces the error.
- **Visual Studio**: fails too, but does not surface it. The failure is only visible in the
  `ServiceHub.RoslynCodeAnalysisService` log under `%TEMP%\Metalama\Logs\<version>\` — which contained **8396**
  occurrences of the cast failure while the IDE itself showed nothing.

So the difference between the two hosts is one of *reporting*, not of behaviour. The single Rider report was
simply the only telemetry sample.
