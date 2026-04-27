# Dependency Versioning Policy

The authoritative source for how we choose versions in `Directory.Packages.props`. The props file carries short inline rationales; the why and how live here.

## Current floors (Metalama 2026.1 LTS, GA target 2026-07-01)

| Floor                  | Version                                  | Source                       |
| ---------------------- | ---------------------------------------- | ---------------------------- |
| Visual Studio 2022     | 17.14 Current Channel, latest patch      | Support policy below         |
| Visual Studio 2026     | Latest Stable patch (currently 18.5.x)   | No 2026-LTSC channel until ~2026-11 |
| .NET SDK (build)       | Any in-MS-support .NET 8/9/10 SDK        | `Metalama.Compiler.exe` replaces the SDK's Roslyn |
| Runtime TFMs           | `net472`, `net8.0`, `net9.0`             | Project files                |
| Roslyn API min         | 4.12.0 (`RoslynApiMinVersion`)           | Lowest design-time analyzer host in MS support |
| Roslyn API max         | 5.0.0 (`RoslynApiMaxVersion`)            | Optional bump to 5.5 to match VS 2026 18.5 |
| MSBuild                | `MicrosoftBuildVersion` per TFM          | VS-shipped                   |

## Why versioning is constrained

Metalama runs in three host types we don't control: Visual Studio, third-party design-time analyzer hosts (Rider, VS Code C# Dev Kit), and the user's app at runtime. Each places different upper bounds on what versions of `Newtonsoft.Json`, `System.Memory`, `Microsoft.CodeAnalysis.*`, `Microsoft.Build.*`, `MessagePack`, `StreamJsonRpc`, `Microsoft.VisualStudio.Threading`, etc. we can depend on. When Metalama loads into one of those hosts, the host-provided assembly wins; depending on a higher version than the host ships causes a load failure or a silent wrong-API call.

## Execution buckets

| Bucket | Where | Version constraint |
| --- | --- | --- |
| VS-loaded | devenv.exe, OOP analyzer host, design-time MSBuild in VS | ≤ what VS floor ships |
| Design-time analyzer (non-VS) | VS Code C# Dev Kit, Rider, OmniSharp | ≤ what that IDE's Roslyn LSP ships (≥ 4.12 across major IDEs today) |
| Compiler-loaded | `Metalama.Compiler.exe`, replacing the SDK's Roslyn at build time | Latest compatible with our hosted Roslyn fork |
| End-user runtime | The user's app process | Latest compatible with the user's TFM |

A package can serve multiple buckets; the strictest constraint wins. At build time `Metalama.Compiler.exe` replaces the SDK's Roslyn, so the SDK's Roslyn version doesn't constrain `RoslynApiMinVersion`. The SDK's Roslyn only matters at design time, and only when the IDE doesn't bundle its own Roslyn payload (VS, Rider, and the VS Code C# Dev Kit all do).

## User-surfacing vs internal packages

Independent of the bucket model, every dependency is also classified by *audience*:

- **User-surfacing**: the package ends up in NuGets that user code references (`Metalama.Framework`, `Metalama.Backstage`, `Metalama.Patterns.*`, `Flashtrace*`). When a user adds our NuGet, transitive resolution may pull these into their project.
- **Internal**: the package is only consumed by projects we host ourselves — `Metalama.Framework.Engine` (compile-time), `Metalama.Framework.DesignTime*` (loaded into VS as our analyzer payload), `Metalama.Compiler.exe`, `Metalama.Tool` (CLI), tests.

While we still support .NET 8 SDK as a build target (.NET 8 LTS through 2026-11), **user-surfacing packages stay on the .NET 8.0 line** (`System.*` 8.0.x, `Microsoft.Extensions.*` 8.0.x). Bumping a user-surfacing dependency to a higher .NET major forces transitive upgrades on consumers that still target net8.0 and partially erodes the "we support .NET 8 SDK" promise. Internal packages are free to bump up to the VS-shipped cap regardless.

Within the chosen .NET line we still take the **highest available patch** (e.g., `System.Drawing.Common` 8.0.x → latest 8.0.26 for security fixes); the rule only freezes the *major.minor*, not the patch level.

## Support policy (rules for choosing the VS floor)

A Visual Studio version is eligible as a floor for a Metalama release if all of the following hold:

1. In MS support today.
2. In MS support at our GA date.
3. At least ~1 month of MS runway from GA.
4. When MS supports only the latest patch of a channel, our floor tracks that latest patch.

The chosen floor is the lowest version satisfying all four rules.

### Worked example: Metalama 2026.1 LTS, GA 2026-07-01

| VS channel / version          | EOL          | Eligible? | Reason |
| ----------------------------- | ------------ | --------- | ------ |
| VS 2022 17.10 LTSC            | 2026-01-13   | No        | Already EOL (rule 1) |
| VS 2022 17.12 LTSC            | 2026-07-14   | No        | Only 13 days runway from GA (rule 3) |
| VS 2022 17.14 Current Channel | 2032-01-13   | Yes       | Latest patch tracks the channel |
| VS 2026 Stable                | rolling      | Yes       | Only VS 2026 channel currently shipping |

VS 2022 floor = 17.14 Current Channel (latest patch); VS 2026 floor = latest Stable patch at GA. After VS 2022 17.12 LTSC ends (2026-07-14), no further VS 2022 LTSCs are planned — Microsoft moved the LTSC model to VS 2026. The first 2026-LTSC ships ~2026-11-10; until then, only Stable is supported.

### LTS lifetime

LTS branches don't freeze their declared floor: as Microsoft drops a VS version from support, our supported set drops with it. We test against the floor that was current at LTS GA.

## TFM constraints

`net472` limits us to packages that retain a `netstandard2.0` (or `net4x`) asset — many modern System.* packages have dropped `netstandard2.0`. `net8.0` / `net9.0` are generally permissive, but per-TFM caps are still required when the consuming process ships a specific .NET runtime. Use the conditional version pattern:

```xml
<MicrosoftBuildVersion Condition="'$(TargetFramework)'=='net8.0'">17.10.46</MicrosoftBuildVersion>
<MicrosoftBuildVersion Condition="'$(TargetFramework)'=='net9.0'">17.14.28</MicrosoftBuildVersion>
```

## Conventions in `Directory.Packages.props` and `eng\Versions.props`

| Suffix              | Meaning                                                                    |
| ------------------- | -------------------------------------------------------------------------- |
| `FooVersion`        | The single version we use everywhere, or the API/min version when split. |
| `FooMinVersion`     | API compatibility floor. We declare the `PackageReference` at this version and commit to only using API surface available in it, so any host or downstream consumer with `Foo` ≥ `FooMinVersion` can satisfy the binding. |
| `FooLatestVersion`  | Version used when we are the host: unit tests, CLI tools, `Metalama.Compiler.exe`, anything we ship as a standalone executable. Free to pick the latest compatible with our TFM. |

The `Min` / `Latest` split exists when a single value would force us to choose between API-compatible flexibility (low floor for substitutability) and freshness (latest patches when we control the runtime).

Add a one-line inline comment in the props file when a version is pinned for a non-obvious reason: VS-shipped compatibility, transitive-dependency lock, intentional staleness, or a `*LatestVersion` below true latest.

## Roslyn variant coverage

We ship per-Roslyn-version variants of our analyzer/source-generator NuGet payload. NuGet auto-selects the highest-compatible variant for the consumer's Roslyn host.

| Variant (`eng\RoslynVersions\`) | Roslyn range it satisfies | Serves (in-MS-support hosts at GA 2026-07-01) |
| --- | --- | --- |
| ~~`Roslyn.4.8.0.props`~~ | ~~4.8 – 4.11~~ | Dropped — no in-support design-time host below 4.12, and the .NET 8 SDK's bundled Roslyn 4.11 is irrelevant at build time (Metalama.Compiler replaces it) |
| `Roslyn.4.12.0.props` | 4.12 – 4.x | VS 2022 17.14 (Roslyn 4.14), .NET 9 SDK 9.0.3xx (Roslyn 4.14), Rider / VS Code design-time hosts |
| `Roslyn.5.0.0.props` | 5.0+ | VS 2026 18.5 (Roslyn 5.5), .NET 10 SDK 10.0.x |

Public Roslyn API surface didn't change between 4.8 and 4.11 for the API set we use; the first additions we depend on (`AllowsConstraintClause`, partial property symbols, etc.) shipped in 4.12. Once no in-support host remained below 4.12, the 4.8 variant added no coverage over 4.12.

## Adding or upgrading a package

If the package is loaded into a VS or third-party IDE design-time analyzer host, cap at the strictest version that host ships (consult `eng\vs-shipped-packages.json`; if not in the manifest, run the inventory script). If it's only in `Metalama.Compiler.exe`, tests, or end-user runtime, take the latest compatible with the relevant TFM. When a package serves multiple categories, apply the strictest cap. For transitive dependencies, run `dotnet list package --include-transitive` on the consuming project and re-apply.

## Workflow: refreshing the VS-shipped manifest

Run `eng\Inventory-VsAssemblies.ps1` against a clean VS install on the host (the dev container has no VS). Curate the raw inventory down to the packages we consume, into `eng\vs-shipped-packages.<vs-version>.json`. Refresh when the floor VS version changes, when MS releases a Current-Channel feature update that rotates shipped assemblies, or when a transitive dependency unexpectedly fails to load in VS.

Before merging a `Directory.Packages.props` change that adds or upgrades a VS-loaded dependency: cross-reference each transitive against the manifest's cap, then smoke-test by loading the change in VS 17.14 latest patch and reproducing a representative design-time scenario.

## Iteration workflow when bumping versions

`Build.ps1 build` is slow (cross-solution rebuild of all packable projects). Most version-bump errors — package downgrades, version-out-of-constraint, transitive cap violations — surface from `dotnet restore` alone, which is much faster. So iterate via restore first, build second:

1. **Edit** `eng/Versions.props` and/or `Directory.Packages.props`.
2. **`dotnet restore` on each top-level `.sln`** (NOT an `.slnf`; the filtered solutions skip projects that may surface conflicts):
   ```powershell
   dotnet restore Metalama.Framework\Metalama.Framework.sln
   # repeat for each top-level solution: Metalama.Backstage, Metalama.Extensions, Metalama.Patterns, Metalama.LinqPad, Metalama.Migration
   ```
3. **Resolve every restore warning** before moving on:
   - `NU1605` (detected package downgrade) — bump the lower pin or unify
   - `NU1608` (version outside dependency constraint) — adjust the offending entry
   - `NU1701` (target framework compat fallback) — investigate; may indicate a TFM issue
   - Any new transitive that exceeds a VS-shipped cap — re-pin transitively
4. **Iterate steps 1–3** until restore is warning-free across every top-level `.sln`.
5. **Then** run `Build.ps1 build` (the slow cross-solution rebuild). Catches anything restore couldn't.
6. **Then** run targeted tests.

## Known caps and rationales

Documented inline in `Directory.Packages.props` / `eng\Versions.props`. Listed here for discoverability:

- `Newtonsoft.Json` — VS-shipped. `NewtonsoftJsonMinVersion` tracks the VS floor.
- `System.Memory`, `System.Buffers`, `System.Runtime.CompilerServices.Unsafe` — VS-shipped OOB family. Pinned in lockstep at the 4.6.x line so the `netstandard2.0` asset is retained for `net472` projects. The `8.x` DLLs in modern VS bundles are inbox copies from .NET 8 runtime, not a separate NuGet to track.
- `Microsoft.CodeAnalysis.*` — `RoslynApiMinVersion` / `RoslynApiMaxVersion` define the API contract floor and ceiling. Runtime implementation comes from VS (design-time) or Metalama.Compiler.exe (build-time).
- `Microsoft.Build.*` — `MicrosoftBuildVersion`, conditional per TFM.
- `Microsoft.NET.Test.Sdk` — pinned to the VS floor (currently `17.12.0`; bump to a 17.14-aligned version when refreshing for 2026.1).
- `MessagePack`, `StreamJsonRpc` — both ILMerged into Metalama, so the chosen version doesn't create an external binding. The only constraint is their transitive impact on the OOB family above (`System.Memory`, `System.Buffers`, `System.Runtime.CompilerServices.Unsafe`): the picked MessagePack/StreamJsonRpc version must work against the OOB versions we ship.
- `MetalamaTemplateLanguageVersion` — capped at C# 13 (in `Directory.Build.props`) so templates and build-time code remain compatible with VS 2022 (no C# 14 compiler).

## Open work

- [x] Run `eng\Inventory-VsAssemblies.ps1` against a clean VS 2022 17.14 latest-patch install and a clean VS 2026 latest-Stable install; commit raw inventory at `eng\vs-shipped-packages.vs2022.json` and `eng\vs-shipped-packages.vs2026.json`.
- [ ] Bump `RoslynApiMinVersion` 4.8.0 → 4.12.0 and delete `eng\RoslynVersions\Roslyn.4.8.0.props`.
- [ ] Remove dead `#if ROSLYN_4_8_0` branches and simplify always-true `#if ROSLYN_4_x_x_OR_GREATER` guards for versions ≤ 4.12.
- [ ] Drop the 4.8 build leg from CI.
- [ ] Bump `MicrosoftBuildVersion` (net9.0) 17.14.28 → 17.14.40, `Microsoft.NET.Test.Sdk` 17.12.0 → 17.14.x, and inline VS-floor comments in `Directory.Packages.props` from "17.12" → "17.14".
- [ ] Bump the OOB-package lockstep set: `System.Memory` 4.6.0 → 4.6.3, `System.Buffers` 4.5.1 → 4.6.1, `System.Runtime.CompilerServices.Unsafe` 6.1.0 → 6.1.2; optionally pin `System.Numerics.Vectors = 4.6.1`.
- [ ] Audit transitive closure of VS-loaded projects against both manifests before 2026.1 GA.
- [ ] Consider bumping `RoslynApiMaxVersion` 5.0.0 → 5.5.0 to match VS 2026 18.5's shipped Roslyn.
- [ ] When 2026-LTSC ships (~2026-11-10), revisit whether to pin the VS 2026 floor to it for future Metalama LTS releases.
