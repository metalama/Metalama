# NuGet asset flow along a dependency chain: measured facts

This note records an experiment run on 2026-10-12 with the .NET 11 SDK (11.0.100-rc.1) to establish how
`PrivateAssets`, `ExcludeAssets` and `developmentDependency` behave along a chain of three packages, because the
package convention of [aspects projects](aspects-projects.md), section 6.2, and the transitive requirement
problem discussed in the [Patterns study](aspects-projects-patterns-study.md) depend on it. The documentation
of NuGet is ambiguous on the propagation of exclusions down a chain; the facts below are measured, not recalled.

## Setup

Six base packages, each with `lib/netstandard2.0/<Id>.dll` and a `buildTransitive/<Id>.props` that defines a
property `Flow_<Id>=true`, so that a consumer can print which props were imported:

| Package | `developmentDependency` | References |
|---|---|---|
| `Redist` | false | none |
| `Build` | true | `Redist` |
| `Aspects` | true | `Redist` |
| `LNormal` | false | `Redist`; `Build` with `PrivateAssets="none"` |
| `LPrivate` | false | `Redist`; `Build` with `PrivateAssets="all"` |
| `LDefault` | false | `Redist`; `Build` with no metadata |

Seven business packages `B1` to `B7`, each referencing one of them with a different metadata, and seven consumers
`C1` to `C7`, each referencing one business package. Everything is packed to a local feed and restored from it.

## Results

Declared dependencies of the business packages (from the nuspec):

| Business package | Reference | Nuspec dependency |
|---|---|---|
| `B1` | `Build` with `PrivateAssets="all"`, `Redist` | `Redist` only |
| `B2` | `LNormal` | `LNormal`, default `exclude="Build,Analyzers"` |
| `B3` | `LNormal` with `PrivateAssets="build;buildtransitive;analyzers"` | `LNormal` with `include="Runtime,Compile,Native,ContentFiles"` |
| `B4` | `LNormal` with `ExcludeAssets="build;buildtransitive"` | `LNormal` with `exclude="Build,Analyzers,BuildTransitive"` |
| `B5` | `LPrivate` | `LPrivate` |
| `B6` | `Aspects` with `PrivateAssets="all" ExcludeAssets="compile;runtime"`, `Redist` | `Redist` only |
| `B7` | `LDefault` | `LDefault` |

Props imported in each consumer, which is what reaches a project that does not itself reference Metalama:

| Consumer | `Redist` props | `Build` props | library props |
|---|---|---|---|
| `C1` | yes | no | n/a |
| `C2` | yes | yes | yes |
| `C3` | no | no | no |
| `C4` | no | no | no |
| `C5` | yes | no | yes |
| `C6` | yes | no | n/a |
| `C7` | yes | yes | yes |

In `C3` and `C4`, every package of the chain is still restored with its `lib` assets, but the `build` asset of
every package in the chain, including `Redist` two levels down, is replaced by `buildTransitive/_._`.

In `B6`, the `Aspects` props are imported, the compiler receives no reference to `Aspects.dll`, and the output
directory does not contain it.

## Facts

1. **`developmentDependency` does not make a reference private.** `LDefault` references `Build` without metadata
   and declares it as an ordinary dependency; `Build` then flows to `C7` with its props. The flag only changes
   what `dotnet add package` writes into the project file. A package that must not flow has to be referenced
   with an explicit `PrivateAssets="all"`, and the documentation must say so.
2. **A dependency reached only through a private reference is absent from the nuspec.** `B1` declares `Redist`
   because it references it directly; it would not declare it through `Build` alone. This is the basis of the
   pack-time check of the aspects projects document, section 6.2, and it is confirmed.
3. **Build-asset exclusions propagate down the whole chain.** Excluding or privatizing the build assets of a
   direct reference (`B3`, `B4`) removes the build assets of every package reachable through it, two levels
   down, while their `lib` assets still flow. A library cannot rely on its own `buildTransitive` props reaching a
   consumer if an intermediate package excluded build assets on the way.
4. **`buildTransitive` flows through the default `exclude="Build,Analyzers"`.** `C2`, `C5` and `C7` import the
   props of packages two levels down. This is what makes `Redist` the one package whose props reach every
   consumer of a business package, as long as no intermediate reference excluded build assets.
5. **`PrivateAssets="all" ExcludeAssets="compile;runtime"` hides the `lib` of a package while its props still run**
   (`B6`, `C6`). The design does not use this form any more: a `Compiler` package has no `lib/` folder, so an
   ordinary reference already gives the compiler nothing and the engine the props. The fact is recorded because
   it is what makes a package with a `lib/` folder usable as a compile-time-only input when needed.

## What the facts decide

- The package convention of section 6.2 holds: `Build` private, `Redist` flowing, the pack-time check needed.
- The transitive requirement, that a consumer of a business package which exposes inheritable aspects or
  transitive fabrics must itself build with Metalama, cannot be expressed by NuGet. Fact 1 rules out
  `developmentDependency` as a carrier, fact 2 rules out a private `Build` reference, and fact 3 shows that
  even a flowing `buildTransitive` check can be silenced by an intermediate exclusion. The check therefore has
  to travel with the assembly, not with the package graph: the engine writes the requirement into the produced
  assembly, and the `Redist` targets, which reach every consumer that did not exclude build assets, read it and
  report. This mechanism is specified in the aspects projects document.
