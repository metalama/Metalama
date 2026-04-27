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

If the package is loaded into a VS or third-party IDE design-time analyzer host, derive the cap with the commands in *Verifying a package's VS-shipped version* below. If it's only in `Metalama.Compiler.exe`, tests, or end-user runtime, take the latest compatible with the relevant TFM (and per the user-surfacing rule, stay on the .NET 8 line for any package referenced by user-surfacing projects). When a package serves multiple categories, apply the strictest cap. For transitive dependencies, run `dotnet list package --include-transitive` on the consuming project and re-apply.

## Verifying a package's VS-shipped version

Run on the host (the dev container has no VS). The VS install carries the NuGet version data in two places; use both for reliable cap derivation, neither alone is sufficient.

### 1. The VS package cache (NuGet versions for `_netfx`-bundled OOB packages)

VS caches its .NET Framework binding-redirect target packages under `C:\ProgramData\Microsoft\VisualStudio\Packages` with directory names that embed the NuGet version directly:

```powershell
Get-ChildItem 'C:\ProgramData\Microsoft\VisualStudio\Packages' -Directory |
    Where-Object { $_.Name -match '_netfx,version=' } |
    ForEach-Object { $_.Name }
# Example: newtonsoft.json_13.0.3_netfx,version=1.0.1.0,productarch=neutral
# The NuGet version is between the first `_` and `_netfx`: here, 13.0.3
```

This covers `Newtonsoft.Json`, `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe` — packages VS ships standalone for .NET Framework binding. Other packages (StreamJsonRpc, MessagePack, MS.VS.Threading, System.Text.Json, etc.) are bundled inside extensions and don't appear here.

### 2. `FileVersionInfo.ProductVersion` of bundled DLLs (NuGet versions for everything else)

For packages bundled inside extensions, walk the VS install and read each DLL's `ProductVersion` (= `AssemblyInformationalVersion`), which embeds the NuGet version followed by `+commitsha`. Take the highest version across all bundled copies — that's what binding redirects route to:

```powershell
function NugetVersionOf($path) {
    try {
        $pv = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($path).ProductVersion
        if ($pv -match '^([\d]+\.[\d]+\.[\d]+(?:\.[\d]+)?(?:-[\w\.\-]+)?)') { return $Matches[1] }
        return $pv
    } catch { return $null }
}
$vsRoot = 'C:\Program Files\Microsoft Visual Studio\2022\Professional'  # or the 18.x equivalent
foreach ($name in 'StreamJsonRpc.dll','MessagePack.dll','Microsoft.VisualStudio.Threading.dll',
                  'System.Text.Json.dll','System.Collections.Immutable.dll',
                  'Microsoft.Bcl.AsyncInterfaces.dll','System.IO.Pipelines.dll',
                  'System.Diagnostics.DiagnosticSource.dll','Microsoft.CodeAnalysis.CSharp.dll',
                  'Microsoft.Build.dll') {
    $found = Get-ChildItem -Path $vsRoot -Recurse -Filter $name -ErrorAction SilentlyContinue -Force
    $highest = ($found | ForEach-Object { NugetVersionOf $_.FullName } | Where-Object { $_ } |
        ForEach-Object { try { [pscustomobject]@{ v=$_; sortable=[version]($_ -split '-')[0] } } catch {$null} }) |
        Where-Object { $_ } | Sort-Object sortable -Descending | Select-Object -First 1
    "$name : $($highest.v)"
}
```

### What NOT to use as the cap

`AssemblyVersion` (the binding identity in the DLL header) is **not** the NuGet version. Many .NET libraries keep AsmVer frozen across NuGet patches for binding-redirect stability: `System.Memory` 4.5.x and 4.6.x both expose AsmVer 4.0.2.0; `Microsoft.Build` 17.x all expose AsmVer 15.1.0.0. Capping on AsmVer is meaningless.

Refresh the cap derivation when the floor VS version changes, when MS releases a Current-Channel feature update that rotates shipped assemblies, or when a transitive dependency unexpectedly fails to load in VS. Before merging a `Directory.Packages.props` change that adds or upgrades a VS-loaded dependency, cross-reference each transitive's NuGet version against the values from the commands above, then smoke-test by loading the change in VS 17.14 latest patch and reproducing a representative design-time scenario.

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
5. **Then** run `Build.ps1 build` (the slow cross-solution rebuild). Catches what restore can't:
   - `CS0507` (cannot change access modifiers when overriding) — a transitive package's API changed access (Spectre.Console.Cli 0.55 made `Command<T>.Execute` `protected`); update our overrides to match
   - `CS1705` (assembly with higher version than referenced) — a transitive's binding identity advanced past what we declare; bump the offending package to a NuGet version that ships the higher AsmVer (StreamJsonRpc 2.22.23 was built against `System.Threading.Tasks.Extensions` AsmVer 4.2.1.0, requiring the 4.6.x NuGet line)
6. **Then** run targeted tests.

## Known caps and rationales

Documented inline in `Directory.Packages.props` / `eng\Versions.props`. Listed here for discoverability:

- `Newtonsoft.Json` — VS-shipped. `NewtonsoftJsonMinVersion` tracks the VS floor.
- `System.Memory`, `System.Buffers`, `System.Runtime.CompilerServices.Unsafe` — VS-shipped OOB family. Pinned in lockstep at the 4.6.x line so the `netstandard2.0` asset is retained for `net472` projects. The `8.x` DLLs in modern VS bundles are inbox copies from .NET 8 runtime, not a separate NuGet to track.
- `Microsoft.CodeAnalysis.*` — `RoslynApiMinVersion` / `RoslynApiMaxVersion` define the API contract floor and ceiling. Runtime implementation comes from VS (design-time) or Metalama.Compiler.exe (build-time).
- `Microsoft.Build.*` — `MicrosoftBuildVersion`, conditional per TFM. Note: the FileVersion of the VS-shipped `Microsoft.Build.dll` (e.g., 17.14.40) is *not* the same as the NuGet version — the latest published `Microsoft.Build` 17.14.x NuGet on nuget.org is 17.14.28. Always verify the NuGet exists before pinning.
- `Microsoft.NET.Test.Sdk` — pinned to the VS floor (currently `17.14.1`).
- `MessagePack`, `StreamJsonRpc` — both ILMerged into Metalama, so the chosen version doesn't create an external binding. The only constraint is their transitive impact on the OOB family above (`System.Memory`, `System.Buffers`, `System.Runtime.CompilerServices.Unsafe`): the picked MessagePack/StreamJsonRpc version must work against the OOB versions we ship.
- `MetalamaTemplateLanguageVersion` — capped at C# 13 (in `Directory.Build.props`) so templates and build-time code remain compatible with VS 2022 (no C# 14 compiler).

## Common gotchas (lessons from past version bumps)

- **AsmVer ≠ NuGet version.** Cap on the NuGet version derived from the commands in *Verifying a package's VS-shipped version*, never on the binding-identity AsmVer in the DLL header.
- **FileVersion ≠ NuGet version for `Microsoft.Build.*`.** The VS-shipped `Microsoft.Build.dll` may report a `FileVersion` higher than any published NuGet (e.g., `17.14.40` shipped vs `17.14.28` latest on nuget.org). Always verify the NuGet exists.
- **Pre-1.0 packages can break in "minor" bumps.** `Spectre.Console.Cli` 0.53 → 0.55 changed `Command<T>.Execute` from `public` to `protected`, requiring CS0507 fixes in 6 files. Read changelogs before bumping any package whose major is `0`.
- **ILMerged transitives still bind externally.** Even when `StreamJsonRpc` is ILMerged into Metalama, its non-merged transitive deps (`System.Threading.Tasks.Extensions`, `System.Diagnostics.DiagnosticSource`, etc.) still need their AsmVer to satisfy the merged code's binding. Bumping the ILMerged package may force bumps on its transitives.
- **`netstandard2.0` is the OOB-family bright line.** `System.Memory` / `System.Buffers` / `System.Runtime.CompilerServices.Unsafe` / `System.Numerics.Vectors` retain `netstandard2.0` only through their 4.6.x line. The `8.x`+ DLLs in modern VS payloads are inbox copies from the .NET 8 runtime, not standalone NuGets.
- **VS's "Current Channel" auto-updates feature bands.** What's "latest 17.14.x" on day N may be "latest 17.16.x" on day N+30 if Microsoft ships a feature update. Re-derive caps when refreshing for a new GA.
- **MSB3277 conflicts in `net472` packs are resolved by an explicit `<PackageReference>` in the consuming csproj.** When a transitive (e.g. `DiffPlex` 1.9 → `System.Memory` 8.x; `K4os.Hash.xxHash` 1.0.8 → `System.Memory` 4.5.x) drags a different OOB-family version into a `net472` build, the central pin doesn't win unless something near the root has a direct `<PackageReference Include="System.Memory"/>`. The fix is a one-line pin in the project that triggers the warning, e.g. `Metalama.Framework.Engine.csproj` and `Metalama.Extensions.HtmlWriter.csproj` both add `<PackageReference Include="System.Memory"/>` (no version — central wins) under a `<!-- Resolve conflict -->` comment, alongside the existing `System.Text.Json` pin of the same shape.

## Open work

- [x] VS 2022 17.14 + VS 2026 18.5 caps verified via the commands in *Verifying a package's VS-shipped version*; values reflected inline in `Directory.Packages.props` and committed under #1603.
- [x] Bump `RoslynApiMinVersion` 4.8.0 → 4.12.0 and delete `eng\RoslynVersions\Roslyn.4.8.0.props`.
- [x] Remove dead `#if ROSLYN_4_8_0` branches (one in `PrimaryConstructorTests.cs`) and strip always-true `#if ROSLYN_4_(4|8|12)_0_OR_GREATER` guards across 134 source files (commit `e247425d69`). Composite conditions like `#if NET8_0_OR_GREATER && ROSLYN_4_8_0_OR_GREATER` simplified to `#if NET8_0_OR_GREATER`. `// @RequiredConstant(ROSLYN_4_X_0_OR_GREATER)` test annotations removed. `ROSLYN_4_12_0_OR_EARLIER` and `ROSLYN_5_*` guards preserved.
- [ ] Drop the 4.8 build leg from CI (build infrastructure outside this repo).
- [x] Inline VS-floor comments in `Directory.Packages.props` updated from "17.12" → "17.14"; `Microsoft.NET.Test.Sdk` 17.12.0 → 17.14.1. (`MicrosoftBuildVersion` net9.0 stays at 17.14.28: 17.14.40 is the FileVersion of the VS-shipped DLL, not the NuGet package version — the highest published `Microsoft.Build` 17.14.x NuGet is 17.14.28.)
- [x] Bumped the OOB-package lockstep set: `System.Memory` 4.6.0 → 4.6.3, `System.Buffers` 4.5.1 → 4.6.1, `System.Runtime.CompilerServices.Unsafe` 6.1.0 → 6.1.2, plus explicit `System.Numerics.Vectors = 4.6.1` pin.
- [x] Bumped within-8.0 line: `System.Drawing.Common` 8.0.21 → 8.0.26, `SystemTextJsonVersion` (fallback) 8.0.0 → 8.0.6, plus added the missing `SystemTextJsonMinVersion = 8.0.6` MSBuild property.
- [x] ILMerge-unlocked: `StreamJsonRpc` 2.20.17 → 2.22.23, `System.Diagnostics.DiagnosticSource` 6.0.1 → 9.0.0.
- [x] Section F freshness sweep: `JetBrains.*`, `DiffEngine`, `Xunit.SkippableFact`, `Microsoft.Web.WebView2`, `BenchmarkDotNet`, `Microsoft.Azure.Functions.Worker.Extensions.Http`, `Azure.Identity`, `Azure.Security.KeyVault.Secrets`, `Spectre.Console*`, `System.IO.Abstractions*`, `System.IO.Hashing` (9.0.0 → 9.0.15), `DiffPlex`, `LibGit2Sharp`, `CommunityToolkit.Mvvm`, `Roslynator.Analyzers`, `CompiledBindings.WPF`.
- [ ] Audit transitive closure of VS-loaded projects against both manifests before 2026.1 GA.
- [ ] Consider bumping `RoslynApiMaxVersion` 5.0.0 → 5.5.0 to match VS 2026 18.5's shipped Roslyn.
- [ ] When 2026-LTSC ships (~2026-11-10), revisit whether to pin the VS 2026 floor to it for future Metalama LTS releases.
