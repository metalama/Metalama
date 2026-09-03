# Platform support

This document describes the doctrine that governs which external platforms a Metalama release supports, how that
set is derived from the vendors' own support calendars, and how it is named. It also records the resulting set,
called the platform baseline, for Metalama 2027.0.

It is the authoritative source for the question "can we drop `net8.0`?" and for every question of that shape.

It is a companion to [`Directory.Packages.md`](../../Directory.Packages.md), which governs the versions of NuGet
packages we may reference. This document governs the platforms those packages must load into. When the two
disagree, this document decides the platform floors and `Directory.Packages.md` decides the package versions
derived from them.

## Why a doctrine is needed

Metalama is loaded into processes we do not control: `devenv.exe`, the Visual Studio out-of-process Roslyn
analyzer host, the Rider backend, the Visual Studio Code C# Dev Kit language server, MSBuild, and
`Metalama.Compiler.exe`. Each of those has a host runtime and a host Roslyn, and neither is chosen by the user's
project. The user's target framework says nothing about either. A design-time host running .NET 8 cannot load a
`net10.0` assembly, whatever the edited project targets.

The failure is also asymmetric. Getting the upper bound wrong, by referencing a newer package than the host ships,
produces a load error at the first invocation. Getting the lower bound wrong, by shipping only a target framework
the host cannot load, produces no visible error at all in Visual Studio:
`ServiceHub.RoslynCodeAnalysisService` logs the failure and the integrated development environment simply shows no
Metalama diagnostics, no code lens and no generated code. Issue
[#1710](https://github.com/metalama/Metalama/issues/1710) was diagnosed only after finding 8396 silently logged
exceptions. We therefore derive the lower bound deliberately rather than discovering it from bug reports.

## The doctrine

A platform version is in the supported set of a Metalama release if all of the following hold. Rules 1 to 4
generalise the Visual Studio floor rules previously stated in `Directory.Packages.md` to every axis below.

1. In vendor support today. The version is not already end of life when the decision is taken.
2. In vendor support at our general availability date.
3. Thirty-day runway. At least 30 days of vendor support remain after our general availability date. Supporting a
   platform that reaches end of life two weeks after we ship costs a full target framework or payload variant for
   two weeks of coverage, so we do not.
4. Latest patch tracks the channel. Where the vendor supports only the latest patch of a channel, our floor is
   that latest patch, not the version we happened to test against.
5. Mainstream, not extended. "In vendor support" means the phase in which the vendor still fixes functionality
   defects. A version in a security-only phase, such as Visual Studio extended support or the second year of a
   Visual Studio long-term servicing channel, is not in the supported set: we cannot get a Roslyn defect fixed
   there, so we cannot support it.
6. Grace on the way out. Within a release we do not withdraw support from a platform sooner than three months
   after it leaves vendor mainstream support. Rule 3 governs what a new release takes on. Rule 6 governs what a
   shipped release keeps. A long-term support branch does not freeze its declared floor: as the vendor drops a
   version, our supported set drops with it, and we keep testing against the floor that was current at the
   long-term support general availability date.
7. Roslyn follows fast. A new stable Roslyn version is supported within three weeks of its stable release.
8. An axis enters the matrix only if some shipped asset depends on it. Before adding a target framework, a Roslyn
   variant or a version cap for a platform, name the asset whose selection actually changes. Most of our surface
   is `netstandard2.0` and is host-agnostic. See "What actually varies" below.

The supported set is the union over each axis of the versions satisfying rules 1 to 5, and the floor of an axis is
the lowest such version. Shipped target frameworks are then derived from the floors, never chosen independently.

## The axes

| Axis | What it constrains | Selected at run time by |
| --- | --- | --- |
| Visual Studio | The private runtime and the Roslyn our design-time payload loads into, and the version cap of every Visual-Studio-shipped dependency | The user's Visual Studio installation |
| Other design-time hosts | The same, for Rider and the Visual Studio Code C# Dev Kit | The user's integrated development environment |
| .NET SDK | The runtime MSBuild executes on, and therefore the toolset and build-task target framework | `global.json` and the installed SDKs |
| .NET runtime | What a user application may target | The user's project |
| .NET Framework | The `net472` floor, and the binding-redirect ceilings of `devenv.exe` | Windows, and the user's project |
| Roslyn API | Which per-Roslyn variant of our payload NuGet resolves | The host's Roslyn version |

## What actually varies

Only three selections in the product depend on the host, and each is worth knowing before arguing about a target
framework. The first two are in this repository. The third is in `metalama/Metalama.Compiler`.

1. Desktop or Core, with no version fallback. `Metalama.Framework.CompilerExtensions` is a single
   `netstandard2.0` analyzer shim that embeds two flavours of the real implementation and selects between them on
   one Boolean: `RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework" )`, in
   `ResourceExtractor._isNetFramework`. There is one Core flavour, not one per .NET major version, and there is no
   fallback if it fails to load. Its target framework is therefore the single most consequential number in this
   document: it must be loadable by every host in the supported set that is not .NET Framework. It is set in
   `Metalama.Framework.CompilerExtensions.Resources.csproj` and in the `CoreAssemblyToEmbed` items of
   `Metalama.Framework.CompilerExtensions.csproj`.
2. Which Roslyn variant. `ResourceExtractor.GetRoslynVersion` reads the version of the assembly that contains
   `SyntaxNode` and maps it to a variant directory. It has an explicit branch for the JetBrains build, whose
   Roslyn reports version `42.42.42.42` and carries the real version in
   `AssemblyInformationalVersionAttribute`. The variants live in `eng/RoslynVersions/` and are bounded by
   `RoslynApiMinVersion` and `RoslynApiMaxVersion` in `Directory.Packages.props`.
3. Which toolset directory. In `metalama/Metalama.Compiler`, `build/Metalama.Compiler.props` selects
   `tasks/<target framework>` from `$(MSBuildRuntimeType)` and the host runtime version. This is a build-time
   selection driven by the .NET SDK, and it is unrelated to the two above.

Everything else is target-framework-agnostic and does not participate in this analysis: the user-surfacing
packages, the analyzer shim itself, and the compile-time compilation, which always targets `netstandard2.0`. See
[`compile-time-target-frameworks.md`](compile-time-target-frameworks.md) for why the compile-time compilation is
a separate concern from this baseline.

## Denomination

Each release names its supported set a platform baseline, written `PB-<release>`, whose canonical short form
lists the floor of each of the six axes in a fixed order, followed by the shipped target frameworks derived
from them:

```
PB-<release> = <VS floor> · <other-IDE floor> · <SDK floor> · User=<tfm> · <.NET Framework floor> ·
               Roslyn <min>–<max> · Core=<tfm> / Desktop=<tfm>
```

Cite the baseline by name in issues, release notes and pull request descriptions, for example "this drops below
PB-2027.0", and change its contents only through this document.

## PB-2027.0, for Metalama 2027.0, general availability 2027-01-01

```
PB-2027.0 = VS 2026 LTSC · VS Code C# Dev Kit / Rider current · .NET 10 SDK · User=net10.0 ·
            .NET Framework 4.7.2 · Roslyn 5.0–5.x · Core=net10.0 / Desktop=net472
```

### Visual Studio

Evaluated at general availability 2027-01-01.

| Channel or version | Vendor status | Runway from general availability | In set |
| --- | --- | --- | --- |
| VS 2022 17.10 LTSC and earlier | End of life 2026-01-13 or before | none | No, by rule 1 |
| VS 2022 17.12 LTSC | End of life 2026-07-14 | none | No, by rule 1 |
| VS 2022 17.14 Current Channel | Mainstream ends 2027-01-13; extended, security-only, to 2032-01-13 | 12 days | No, by rules 3 and 5 |
| VS 2026 LTSC, baseline of 2026-11 | Security servicing through approximately 2027-11 | approximately 10 months | Yes |
| VS 2027 Stable, released 2026-11 | Feature updates and servicing through approximately 2027-11 | rolling | Yes |

Version 17.14 is the only Visual Studio 2022 version that reaches 2027 at all. Microsoft made it the final 2022
baseline on the Current Channel and created no 17.14 long-term servicing channel, and every earlier long-term
servicing channel has expired. It leaves mainstream support 12 days after our general availability date, so rule 3
excludes it, and its remaining lifetime to 2032 is security-only, which rule 5 excludes independently. Metalama
2027.0 therefore does not support Visual Studio 2022.

Visual Studio 2026 and later update in place each November. Visual Studio 2026 shipped on 2025-11-11 with .NET 10,
and Visual Studio 2027 is expected in November 2026 with .NET 11 and C# 15. Each gets one year of feature updates
and servicing, followed by one year of long-term servicing channel security servicing. The Visual Studio 2026
long-term servicing channel opens in November 2026, so it is the first pinnable Visual Studio 2026 version and
therefore our floor. Under rule 5 its second year, which is security-only, does not extend our set: the Visual
Studio 2026 long-term servicing channel leaves our supported set when Visual Studio 2027 has itself moved to
long-term servicing.

### Consequence: the Core flavour is `net10.0`

Roslyn's own `docs/contributing/target-framework-strategy.md` names the Visual Studio private runtime in each
branch. It reads "`$(NetVisualStudio)` (presently `net8.0`)" on `release/dev18.0`, which is Roslyn 5.0 and Visual
Studio 18.0, and on `release/dev18.3`, which is Roslyn 5.3 and Visual Studio 18.3. It reads "presently `net10.0`"
on `release/stable`, which is Roslyn 5.10, and on `main`. Roslyn minor versions map to Visual Studio minor
versions (4.14 to 17.14, 5.0 to 18.0, 5.3 to 18.3), so the private runtime moved to .NET 10 at approximately
Visual Studio 18.10, before the November 2026 long-term servicing channel baseline.

With Visual Studio 2022 out of the set, no host in PB-2027.0 runs a .NET runtime below 10, and the single embedded
Core flavour becomes `net10.0`. This is the change that makes `net8.0` droppable. It is not implied by dropping
`net8.0` as a user target framework, which is a separate and unrelated decision. See issue
[#1876](https://github.com/metalama/Metalama/issues/1876) and pull request
[#1877](https://github.com/metalama/Metalama/pull/1877) for the implementation.

This inference has one dependency worth watching: the upstream move off .NET 8,
[dotnet/roslyn#84192](https://github.com/dotnet/roslyn/pull/84192), was merged in June 2026, reverted the next
day, and relanded. Confirm the actual private runtime of the Visual Studio 2026 long-term servicing channel
baseline once it ships, on 2026-11-10, before the 2027.0 general availability date. See the checklist below. If it
turns out to be .NET 8, the Core flavour must stay `net8.0` for 2027.0.

### Other design-time hosts

| Host | Runtime | Roslyn | In set |
| --- | --- | --- | --- |
| JetBrains Rider 2026.2.0.2 | .NET 10.0.5, measured | 5.0.0, measured | Yes, the current version |
| Visual Studio Code with the C# Dev Kit | Ships its own runtime; Roslyn's `$(NetVSCode)` is `net10.0` on `release/dev18.3` and `main` | `roslyn-language-server` 5.8 and later, not measured | Yes, the current version |
| OmniSharp | not applicable | not applicable | No: deprecated and untested |
| Visual Studio for Mac | not applicable | not applicable | No: sunset by Microsoft |

We support the current release of Rider and of the C# Dev Kit, not a named floor. JetBrains and the C# extension
both update continuously and neither publishes a support calendar we can apply rules 1 to 3 to. The rule for this
axis is therefore: take the current release as it stands when the baseline is written, and measure it again at the
release candidate. For 2027.0 that second measurement is due on 2026-11-20.

The measurement of 2026-09-01, on Rider 2026.2.0.2, gave both values.

The backend runtime is .NET 10.0.5. Every `Rider.Backend*.runtimeconfig.json` under `lib/ReSharperHost` declares
`net8.0` with `rollForward` set to `LatestMajor`, and the only shared frameworks Rider bundles, under
`lib/ReSharperHost/windows-x64/dotnet/shared`, are `Microsoft.NETCore.App`, `Microsoft.AspNetCore.App` and
`Microsoft.WindowsDesktop.App`, all at 10.0.5. The declared `net8.0` is therefore not the runtime the backend
executes on. `Core=net10.0` is safe for Rider.

The backend Roslyn is 5.0.0. `lib/ReSharperHost/Microsoft.CodeAnalysis.dll` carries assembly version
`42.42.42.42`, which is the JetBrains build marker, and product version `5.0.0-dev`, which is what
`ResourceExtractor.GetRoslynVersion` parses. Rider therefore presents Roslyn 5.0, not Roslyn 4.x and not Roslyn
5.10. The other copies of `Microsoft.CodeAnalysis.dll` in the Rider layout belong to the bundled .NET SDK
(Roslyn 5.3) and to the bundled MSBuild (Roslyn 5.7); neither is loaded into the process that hosts our analyzer.

### .NET SDK, at build time

.NET 8 and .NET 9 both reach end of support on 2026-11-10, seven weeks before general availability, so neither is
in the set under rule 1. The floor is the .NET 10 SDK. The .NET 11 SDK, released in November 2026, is also in the
set.

The build container carries a constraint that this floor does not express. Visual Studio installs a .NET SDK of its
own through the `Microsoft.NetCore.Component.SDK` component, and Visual Studio 2026 18.9 installs 10.0.400. When
the container installs a second SDK of a different feature band beside it, `C:\Program Files\dotnet` holds both,
`MSBuildExtensionsPath` and `MSBuildSDKsPath` resolve to different SDK directories, and a solution restore fails
with `MSB4062`, because `NuGet.Build.Tasks` of one band requires a newer `Microsoft.Build.Framework` than the other
band provides. The `dotNetSdkVersion` constant in `eng/src/Program.cs` therefore names the version that Visual
Studio installs, and the same constant feeds the container component and `global.json`, so the two cannot drift
apart. It departs on purpose from `PreferredVersions.DotNetSdk.V_10_0` in PostSharp.Engineering, which is 10.0.102
and is shared with the repositories that are still on Visual Studio 2022. Visual Studio 2022 17.14 ships no .NET 10
SDK, so those repositories have no conflict to resolve, and this constraint appeared only with the move to Visual
Studio 2026.

### .NET runtime, for user target frameworks

The supported user target frameworks are `net10.0`, which is a long-term support release supported to 2028-11, and
`net11.0`, which is a standard-term support release published in November 2026. `net8.0` and `net9.0` are out of
support at general availability and are no longer supported target frameworks. This is a breaking change for
users, most visibly for `Metalama.Patterns.Wpf`, whose `net8.0-windows` asset becomes `net10.0-windows` and leaves
a Windows Presentation Foundation application on .NET 8 or .NET 9 with no compatible asset.

### .NET Framework

The floor stays 4.7.2. .NET Framework 4.6.2 reaches end of support on 2027-01-12 and is already below our floor.
Versions 4.7.2, 4.8 and 4.8.1 are supported for the lifetime of the operating systems that carry them. The
`net472` assets serve `devenv.exe`, `MSBuild.exe` and user projects that target .NET Framework, and they also fix
the binding-redirect ceilings on the out-of-band package family documented in
[`Directory.Packages.md`](../../Directory.Packages.md).

### Roslyn API

`RoslynApiMinVersion` is the lowest Roslyn version that any host in the set presents, and a payload variant may
exist only if it serves a host in the set, by rule 8. The variant set is therefore derived, and each variant must
name the host it covers.

Two sub-axes feed this floor, and they are settled differently.

The Visual Studio sub-axis is settled by the calendar. With Visual Studio 2022 17.14, which carries Roslyn 4.14,
out of the set, the lowest Visual Studio in the set is the Visual Studio 2026 long-term servicing channel baseline
of November 2026. Roslyn minor versions track Visual Studio minor versions, so that baseline carries Roslyn 5.11
or thereabouts. No Visual Studio in the set presents a Roslyn version between 5.0 and 5.9. This inference rests on
the release cadence rather than on a measurement, which is why checklist item 1 below records the Roslyn version
of that baseline as well as its private runtime.

The other-design-time-host sub-axis is not settled by a calendar, because Rider and the C# Dev Kit publish none,
and the doctrine substitutes the current release for rules 1 to 3. Their Roslyn version is therefore a
measurement, and it is the only input that can place a host below the Visual Studio floor. The .NET SDK does not
feed this axis: the SDK's Roslyn governs a design-time host only when that host bundles no Roslyn of its own, and
Visual Studio, Rider and the C# Dev Kit all bundle their own. `Metalama.Compiler` always selects the latest
variant, so it does not feed it either.

The consequence is that one measurement decides the whole variant set. A variant identity binds against the Roslyn
version the payload references, and assembly binding rolls forward but never back, so a host below a variant's
identity falls back to the next lower variant, or to nothing at all when none exists.

| Rider and C# Dev Kit Roslyn, measured at general availability | Variants to ship | `RoslynApiMinVersion` |
| --- | --- | --- |
| 5.10 or above | the latest variant alone | 5.10 |
| 5.0 to 5.9 | a Roslyn 5.0 variant and the latest variant | 5.0 |
| below 5.0 | the Roslyn 4.12 variant and the latest variant | 4.12 |

The default is a single variant, and the failure of getting this wrong is the silent one described at the top of
this document: a host with no loadable variant reports nothing, and a host that falls back to an older variant
loses the features guarded by the newer variant's preprocessor symbols without reporting anything either.

The measurement of 2026-09-01 puts Rider 2026.2.0.2 at Roslyn 5.0.0, which is the middle row. For PB-2027.0
therefore:

- Ship a Roslyn 5.0 variant alongside the latest variant. The 5.0 to 5.9 range is not empty: Rider sits at its
  lower bound, so without that variant every Rider user falls back to a Roslyn 4 payload.
- Drop the `Roslyn.4.12.0` variant. No host in the set is below Roslyn 5.0 once Visual Studio 2022 and the .NET 8
  and .NET 9 SDKs are out.
- Set `RoslynApiMinVersion` to `5.0.0`.

The C# Dev Kit is not measured, because it is not installed on the machine the measurement was taken on. It does
not change the outcome unless it is below Roslyn 5.0, which `roslyn-language-server` has not been for several
releases. Confirm it at the release candidate, together with the second Rider measurement.

`RoslynApiMaxVersion` follows Visual Studio 2027 and the .NET 11 SDK, within three weeks of their stable release,
by rule 7. The procedure for taking on a new Roslyn version is in
[`updating-roslyn.md`](updating-roslyn.md).

### Shipped assets under PB-2027.0

| Asset | Repository | Target framework |
| --- | --- | --- |
| User-surfacing packages: `Metalama.Framework`, `Metalama.Patterns.*`, `Metalama.Backstage`, `Flashtrace*` | Metalama | `netstandard2.0`, plus `net472` and `net10.0` where a package needs them |
| `Metalama.Framework.CompilerExtensions`, the analyzer shim | Metalama | `netstandard2.0` |
| The embedded Desktop flavour | Metalama | `net472` |
| The embedded Core flavour | Metalama | `net10.0` |
| The compile-time compilation | Metalama | `netstandard2.0`, always, and unrelated to this baseline |
| `Metalama.Compiler.Interface`, from `Metalama.Compiler.Sdk` | Metalama.Compiler | `netstandard2.0` |
| The `Metalama.Compiler` toolset and the `Metalama.Compiler.Sdk` tasks | Metalama.Compiler | `net472` and `net10.0` |

Extension packages follow the same floors. The target frameworks an extension author must declare are documented
in [`extensibility.md`](extensibility.md); that list is derived from this table and has no independent authority.

### What PB-2027.0 drops relative to 2026.1

- Visual Studio 2022 in its entirety, by rules 3 and 5 applied to 17.14.
- The .NET 8 and .NET 9 SDKs at build time, and the corresponding toolset and build-task directories.
- The `net8.0` and `net9.0` user target frameworks.
- The `net8.0` embedded Core flavour.
- The `Roslyn.4.12.0` variant, replaced by a Roslyn 5.0 variant, which is what Rider presents.

## What this means in this repository

The Visual Studio axis is the binding constraint here, because this repository ships the design-time payload. Two
files carry the whole of it.

- `Metalama.Framework.CompilerExtensions.Resources.csproj` declares the two flavours. Its `TargetFrameworks`
  property is the Core and Desktop pair of the baseline, and nothing else selects between them at run time.
- `Metalama.Framework.CompilerExtensions.csproj` embeds the build output of that project through the
  `DesktopAssemblyToEmbed` and `CoreAssemblyToEmbed` items, whose paths repeat the same target framework names.
  The two files must move together, and a mismatch produces an empty resource set rather than a build error.

Two further points bear on any change to the floors.

- A path segment that names a target framework is not always one of ours.
  `CoreAssemblyToEmbed` includes `runtimes/win/lib/net8.0/System.Threading.AccessControl.dll`, where the first
  path segment is our build output and the second is an asset folder inside the
  `System.Threading.AccessControl` package. Only the first follows this baseline. Changing the second matches no
  file and drops the assembly from the embedded resources without any error.
- The extension loader compares target framework names as strings. `TargetedAssemblyReference` and
  `ExtensionLoaderBase` each derive a target framework name from the same .NET Framework Boolean as
  `ResourceExtractor`, then compare it for equality against the `TargetFramework` metadata of
  `MetalamaExtensionAssembly`. Both carry the Core name as a literal. When the Core flavour moves, those literals
  move with it, otherwise no extension loads on .NET.

## What this means in Metalama.Compiler

`metalama/Metalama.Compiler` ships two packages, `Metalama.Compiler` and `Metalama.Compiler.Sdk`, and neither
contains a design-time assembly. The only asset an integrated development environment loads is
`analyzers/dotnet/cs/Metalama.Compiler.Interface.dll`, which is `netstandard2.0`. The Visual Studio axis of the
baseline therefore does not constrain that repository at all. Only the .NET SDK axis does.

Concretely, under PB-2027.0:

- `MetalamaNetRoslyn` and `NetRoslynAll` in `eng/targets/TargetFrameworks.props` follow the .NET SDK floor, which
  is `net10.0`. `NetVS` and `NetVSShared` may hold their upstream values, because every project that reads them is
  in `Ide.slnf` and only `Metalama.Compiler.slnf` is built. The one in-solution reference, in
  `Metalama.Compiler.Arm64.Package.csproj`, is a condition on a `net472` project and is false either way.
- The toolset and the `Metalama.Compiler.Sdk` tasks ship `net472` and `net10.0`. `net11.0` is not needed, because
  the `net10.0` compiler declares `rollForward=Major` and runs on .NET 11.
- A host runtime below the SDK floor must be reported, not left to fail while loading an assembly. The toolset
  does this with `LAMA0622`, from `MetalamaCompilerCheckHostRuntime` in `build/Metalama.Compiler.targets`.

Two places in that repository will drift when the floor next moves:

- `buildTransitive/Metalama.Compiler.Sdk.props` selects `tasks/net10.0` from `$(MSBuildRuntimeType)` alone, with
  no version guard and no equivalent of `LAMA0622`. Below the SDK floor it fails with a raw assembly-load error.
- The .NET Framework MSBuild bridge in `build/Metalama.Compiler.props` probes the shared framework directory for
  `10.*` only. When the SDK floor moves past .NET 10, or on a machine that carries only a later runtime, the
  bridge silently falls back to `net472` instead of driving the CoreCLR compiler.

The companion change for PB-2027.0 in that repository is
[metalama/Metalama.Compiler#211](https://github.com/metalama/Metalama.Compiler/pull/211).

## Verification checklist before the 2027.0 general availability date

Rules 1 to 8 are applied against calendars. These three items are applied against machines, and no removal of
`net8.0` should ship without them.

1. The Visual Studio 2026 long-term servicing channel baseline private runtime and Roslyn version. After
   2026-11-10, install the baseline and record both. Confirm that `ServiceHub.RoslynCodeAnalysisService` runs on
   .NET 10, either from the Visual Studio installation layout, under
   `Microsoft Visual Studio\<year>\<sku>\dotnet\net10.0\runtime\`, or from Roslyn's
   `docs/contributing/target-framework-strategy.md` on the branch that shipped it. If it is .NET 8, the Core
   flavour stays `net8.0` for 2027.0. Read the Roslyn version from the same installation: the Visual Studio floor
   of the Roslyn axis is inferred from the release cadence until this is measured, and a baseline below Roslyn
   5.10 puts a Visual Studio host in the 5.0 to 5.9 range and makes a Roslyn 5.0 variant mandatory.
2. The Rider and C# Dev Kit backend runtime and Roslyn version. Done for Rider on 2026-09-01: .NET 10.0.5 and
   Roslyn 5.0.0, recorded in the "Other design-time hosts" section above. Outstanding for the C# Dev Kit, which
   was not installed on that machine. Repeat both at the release candidate on 2026-11-20, because this axis
   follows the current release rather than a calendar.
3. A design-time smoke test on the floor. Run the design-time verification protocol of
   [`Directory.Packages.md`](../../Directory.Packages.md) on the floor Visual Studio and on the previous one. A
   mismatch between `net8.0` and `net10.0` does not surface in the integrated development environment: check the
   log of `ServiceHub.RoslynCodeAnalysisService` for load failures, not the editor.

## Sources

- [.NET 8 and .NET 9 end of support, 2026-11-10](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/)
- [Visual Studio 2022 product lifecycle and servicing](https://learn.microsoft.com/en-us/visualstudio/releases/2022/servicing-vs2022)
- [Visual Studio product lifecycle and servicing, 2026 and later](https://learn.microsoft.com/en-us/visualstudio/releases/2026/servicing-vs)
- [Visual Studio channels and release rhythm, 2026 and later](https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-rhythm)
- [.NET Framework lifecycle frequently asked questions](https://learn.microsoft.com/en-us/lifecycle/faq/dotnet-framework)
- [Roslyn target framework strategy](https://github.com/dotnet/roslyn/blob/main/docs/contributing/target-framework-strategy.md)
- [dotnet/roslyn#84192, move the Visual Studio private runtime off .NET 8](https://github.com/dotnet/roslyn/pull/84192)
- [Metalama requirements, public documentation](https://doc.metalama.net/conceptual/requirements)
