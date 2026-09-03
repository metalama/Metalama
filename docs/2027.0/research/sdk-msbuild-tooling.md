# .NET 11 SDK, MSBuild, NuGet and Visual Studio 2027 — research notes

Research date: **2026-09-03**.
State of the world at that date: **.NET 11 Preview 7** (released 2026-08-11) is the newest public build.
**No RC has shipped yet.** GA is expected **November 2026** (10 November 2026 is the date given by
`dotnet/core` for the start of the STS support window; .NET 11 is an **STS** release supported
10 Nov 2026 → 9 Nov 2028).

Every fact below is followed by the primary source it came from. Where a fact is an inference rather
than a quotation, it is marked **[inference]**.

---

## 1. Version numbers — the anchor table

| Component | Value at Preview 7 (Aug 2026) | Value expected at GA (Nov 2026) | Source |
|---|---|---|---|
| .NET SDK | `11.0.100-preview.7.26381.103` | `11.0.100` | `release-notes/11.0/preview/preview7/build-metadata.json`, `sdk.md` |
| Runtime / ref packs | `11.0.0-preview.7.26381.103` | `11.0.0` | `build-metadata.json` |
| MSBuild | `18.12.0` (VersionPrefix on `dotnet/msbuild` `main`) | **18.12** | `dotnet/msbuild/eng/Versions.props` |
| MSBuild `AssemblyVersion` | **`15.1.0.0`** (unchanged, deliberately frozen) | `15.1.0.0` | `dotnet/msbuild/eng/Versions.props` |
| Roslyn | `5.12.0` (`MajorVersion 5`, `MinorVersion 12`, `PatchVersion 0`, `PreReleaseVersionLabel 1`) | **5.12** | `dotnet/roslyn/eng/Versions.props` |
| NuGet client | **7.12** *(inferred)* — 7.9 shipped with VS 18.9 / SDK 10.0.400 | 7.12 | `NuGet-7.9.md` + minor-version-tracks-VS pattern **[inference]** |
| Visual Studio | 18.10 / 18.11 (monthly) | **Visual Studio 2027 = 18.12** *(inference from MSBuild 18.12 + Roslyn 5.12)* | see §6 |
| C# | 15 | 15 | `whats-new/csharp-15` |

### The rule that generates the above
Since Visual Studio 2026 (18.0, Nov 2025), **Visual Studio ships a monthly minor version** and the
version numbers of MSBuild, Roslyn and the NuGet client track the VS minor version:

* MSBuild `18.<VS minor>` — confirmed by `MSBUILDDISABLEFEATURESFROMVERSION=18.10` being the escape
  hatch documented in the **Preview 7** MSBuild notes (Preview 7 ≈ VS 18.10 Insiders).
* Roslyn `5.<VS minor>` — `Roslyn package version 5.0.0 → minimum Visual Studio 2026 version 18.0`
  (learn.microsoft.com/visualstudio/extensibility/roslyn-version-support), and `main` is 5.12.
* NuGet `7.<VS minor>` — "NuGet 7.9.0 ships in **Visual Studio 2026 version 18.9.0** and
  **.NET SDK 10.0.400**" (`NuGet-7.9.md`).

Timeline check: 18.0 = Nov '25, 18.4 = Mar '26, 18.6 = May '26, 18.9 = Aug '26 (per the SDK/MSBuild/VS
versioning doc), therefore **18.12 = Nov '26**. That is the version that will be branded
**Visual Studio 2027**.

### SDK ↔ VS mapping (from learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs, ms.date 2026-06-08)

| SDK version | MSBuild/VS version | Ship date | Lifecycle |
|---|---|---|---|
| 9.0.300 | 17.14 | May '25 | Nov '26 |
| 10.0.100 | **18.0** | Nov '25 | Nov '28 |
| 10.0.200 | 18.4 | Mar '26 | May '26 |
| 10.0.300 | 18.6 | May '26 | Aug '26 |
| 10.0.400 | 18.9 | Aug '26 | Nov '28 |

Preview pairing table from the same page:

| SDK preview | Visual Studio |
|---|---|
| 11.0.100 Preview 1 | 18.4.0 Insiders |
| 11.0.100 Preview 2 | 18.5.0 Insiders |
| 11.0.100 Preview 3 | 18.6.0 Insiders |
| 11.0.100 Preview 4 | 18.7.0 Insiders |
| 11.0.100 Preview 5 | 18.8.0 Insiders |

(The page had not been updated for Previews 6 and 7 as of 2026-06-14; extrapolating gives
Preview 6 → 18.9 Insiders and Preview 7 → 18.10 Insiders. **[inference]**)

### Targeting / minimum-VS policy (unchanged text, applied to .NET 11)
* "Each new TargetFramework **requires** a new Visual Studio version or a new `dotnet` version."
* "The first version of Visual Studio that supports a new TargetFramework becomes a floor for the
  feature bands of that SDK for **Roslyn API surface, MSBuild targets, source generators, analyzers**,
  and so on."
* "The first version of a new .NET SDK that supports a new TargetFramework can still be used with the
  prior version of Visual Studio to allow one quarter for tooling and infrastructure to migrate."
* Therefore, **[inference]** by analogy with `10.0.100 → VS ships 18.0, minimum 17.14, max TFM in
  minimum VS = net9.0`: SDK `11.0.100` will ship with VS 18.12, have a minimum of roughly VS 18.9,
  and `net11.0` will be officially supported only in **VS 18.12+**. The docs did not yet contain the
  11.0.x rows on 2026-09-03 — see Open questions.
* "Targeting a newer runtime in an older Visual Studio version isn't supported and produces a build
  warning."

---

## 2. The .NET 11 SDK

Primary source: <https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk>
(ms.date 2026-08-12, "last updated for Preview 7"), plus
`https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview{1..7}/sdk.md`.

### 2.1 SDK footprint
* Linux/macOS installers deduplicate assemblies with **symbolic links**; duplicate `.dll`/`.exe`
  identified by content hash. Affects tarballs, `.pkg`, `.deb`, `.rpm`.
* 35% of the SDK directory was duplicate files; on `linux-x64` that is 816 files / 140 MB.
* linux-x64 tarball 230 MB → 189 MB (−17.8%); deb 164 → 122 (−25.6%); rpm 165 → 122 (−26.0%);
  containers −8..17%.
* **crossgen is skipped for assemblies that exist only under `DotnetTools/`** — those ship IL-only.
  Saves a further 23.6 MB on linux-x64.
* **Windows deduplication is planned for a future preview** (not in Preview 7).

**Relevance to a compiler-replacement product:** an assembly that lives only under `DotnetTools/` is
IL-only in .NET 11; assemblies present both inside and outside `DotnetTools/` are still crossgen'd and
the duplicate removed. Symlinked SDK layouts can surprise code that resolves an assembly by walking the
SDK directory or that compares file identity by path.

### 2.2 NativeAOT `dotnet` CLI — **on by default**
* Preview 6 shipped it behind `DOTNET_CLI_ENABLEAOT`; **Preview 7 flipped the default on every
  platform**. Opt out with `DOTNET_CLI_ENABLEAOT=false` (falsy = `false`, `0`, `no`, `off`).
* Listed as a **breaking change** in both the release notes and
  `learn.microsoft.com/dotnet/core/compatibility/sdk/11/native-cli-command-handling-enabled`.
* Commands fully served from the AOT path: `dotnet --version`, `--info`, `--help`,
  `dotnet <command> --help` for every built-in command, `--cli-schema`, `dotnet sdk check`,
  `dotnet sln list` / `sln migrate` / `sln remove`, `dotnet tool list --local`, `tool run`,
  `tool uninstall --local`, `tool search`.
* **External-command resolution and invocation now happens from the AOT path**: global tools, local
  tools, PATH commands and app-base commands (`dotnet ef`, `dotnet dev-certs`) are resolved and
  launched out-of-process without booting the managed CLI.
* Still falls back to the managed CLI: anything that needs MSBuild or NuGet in-process — `build`,
  `run`, `test`, `pack`, `publish`, `sln add`, file-based app execution.
* Measured: `dotnet tool list` 378 ms → 68 ms; `dotnet dev-certs https` ~700 ms → 200–220 ms.
* The AOT muxer resolves the versioned SDK directory the same way the managed muxer does
  (multi-SDK installs).
* `dotnet --info` emits workload version, workload list **and MSBuild version** from the AOT binary.

### 2.3 MSBuild server — **on by default**
* "The MSBuild server is now enabled by default. This keeps a warm MSBuild worker between CLI
  invocations."
* Opt out: `DOTNET_CLI_USE_MSBUILD_SERVER=false` **or** `MSBUILDUSESERVER=0`.
* `DOTNET_CLI_USE_MSBUILD_SERVER=false` is **authoritative**: it forwards `MSBUILDUSESERVER=0` so the
  server cannot be silently re-enabled by response files, `MSBUILDFORCEMULTITHREADED=1`, or `/mt`.
* Separately (Preview 6): the CLI **no longer unconditionally writes `MSBUILDUSESERVER=0`**. Previously
  the CLI overrode any user-set value; now, if `DOTNET_CLI_USE_MSBUILD_SERVER` is unset the CLI leaves
  `MSBUILDUSESERVER` alone.

**Relevance to a compiler-replacement product:** a warm, long-lived MSBuild process that persists
across `dotnet build` invocations is now the default. Anything that caches per-build state in static
fields of an MSBuild task assembly, or that assumes the MSBuild process dies at the end of a build,
now sees process reuse by default rather than as an opt-in. This compounds the existing situation
where VBCSCompiler and MSBuild worker nodes outlive a build and hold user assemblies loaded.

### 2.4 Code analysis and analyzers in the SDK
* **`AnalysisLevel` corrected for .NET 11** (Preview 2): "Projects with `AnalysisLevel=latest` were
  incorrectly using **.NET 9** analyzer rules instead of the expected **.NET 11** rules. This is now
  fixed." So a project on the .NET 11 SDK with `AnalysisLevel=latest` gets a *different, larger* rule
  set than it did on the .NET 10 SDK.
* **CA1873** (Avoid potentially expensive logging) reworked:
  * No longer flags property accesses, `GetType()`, `GetHashCode()`, `GetTimestamp()`.
  * Applies only to **Information-level logging and below** by default.
  * Message now names the reason, e.g.
    `warning CA1873: Evaluation of this argument may be expensive and unnecessary if logging is disabled (method invocation)`.
  * Nine reasons: method invocation, object creation, array creation, boxing conversion, string
    interpolation, collection expression, anonymous object creation, await expression, with expression.
  * Preview 7 also fixed "CA1873: log level comparison".
* Analyzer bug fixes: **CA1515** and **CA1034** false positives when **C# extension members** are
  present; **CA1859** improper handling of default interface implementations; **CA1033** no longer
  reports for interfaces with default implementations (Preview 5); **CA2007** no longer reported for
  pattern-based `await using` / `await foreach`; **CA1860** works with abstract collections.
* **New analyzers in Preview 1**: CA1517 (prefer `ReadOnlySpan`), CA1830 (`StringBuilder`
  optimization), CA1876 (`AsParallel` misuse), CA1877 (nested `Path.Combine`), CA2026 (`JsonElement`
  parsing), CA2027 (non-cancelable `Task.Delay`).
* Source generator fix (Preview 7): "Don't generate embedded `ValidatableTypeAttribute` for .NET 11
  and later" — the ASP.NET validation generator stops emitting the embedded attribute because the
  type now ships in the framework.

### 2.5 New / changed MSBuild properties in the .NET 11 SDK

| Property | Preview | Meaning |
|---|---|---|
| `PublishReferenceSymbols` | P1 | Whether `.pdb` files from **referenced projects** land in publish output. Complements `PublishDocumentationFile`, `PublishReferencesDocumentationFiles`, `CopyOutputSymbolsToPublishDirectory`. |
| `AppendPublishRuntimeIdentifierToRuntimeIdentifiers` | P6 | Set `false` as an escape hatch for multi-RID publish workloads (dotnet/sdk #54291). |
| `CheckSdkVulnerabilities` | P5 | Opt in to SDK vulnerability / end-of-life checks during build. |
| `LocalRegistry` | P7 | Now accepts `Docker`, `Podman`, **`Wslc`**, **`MacOSContainer`**. |
| `UseNativeLibPrefix` | .NET 11 | Default `true`. NativeAOT native library outputs on Unix get a `lib` prefix (`libmylib.so`). Set `false` to restore the old name. |
| `BuildProjectReferences` | P7 | **Behaviour change**: now defaults to `false` when `NoBuild=true`. |
| `RestoreEnableAnalyzerAssets` | P7 (NuGet) | Opt in, per target framework, to the new `analyzers` asset group in `project.assets.json`. See §4.1. |
| `RuntimeEnvironmentVariableSupport` / `@(RuntimeEnvironmentVariable)` | P7 | Projects that declare `RuntimeEnvironmentVariableSupport` receive `dotnet run -e` / `dotnet test -e` values as `@(RuntimeEnvironmentVariable)` items during build, device selection, deployment and `ComputeRunArguments`. |
| (unnamed) partial-R2R property | P4/P7 | Lets upstack tooling (macios, maui) declare a list of assemblies to be **partially R2R-compiled and excluded from the composite image**. Not intended for app developers. |
| `AdditionalEndpointDefinitions` | P4 | New parameter on the `DefineStaticWebAssetEndpoints` task. |
| `ComputeToolPackageRuntimeIdentifiersToPack` | P7 | A **target** a tool package author can implement to declare which RIDs their toolchain can build; the SDK then runs one inner pack per RID. Default matrix without it: Windows x64 packs `win-x64` + `win-arm64`; macOS x64/arm64 pack both `osx-x64` + `osx-arm64`; every other host packs only itself. |
| `ComputeAvailableDevices` | P1/P7 | Target that `dotnet watch`/`dotnet run`/`dotnet test` call for MAUI and mobile device selection. |

### 2.6 `SdkAnalysisLevel`
From learn.microsoft.com/dotnet/core/project-sdk/msbuild-props:

| `SdkAnalysisLevel` | Behaviour gated on it |
|---|---|
| `9.0.100` | HTTP restore sources: NU1302 error instead of NU1803 warning |
| `10.0.100` | Package pruning on by default; improved dependency resolver; NU1015 error for `PackageReference` without a version |
| `10.0.300` | Multi-targeting with duplicate target frameworks; NU1019 error/warning for path separators and non-ASCII in `TargetFramework` |
| **`11.0.100`** | **MonoAndroid deprecation warning NU1703**; **NU1019 error** for non-ASCII in `TargetFramework` |

**Ageing-out rule (new wording in .NET 11):** "The behavior enabled by the `SdkAnalysisLevel` value
ages out after **three major releases**. For example, version `11.0.100` only respects values down to
`8.0.100`." So pinning `SdkAnalysisLevel` to `7.0.x` no longer has any effect under the .NET 11 SDK.

### 2.7 New SDK warning: NETSDK1235
```
warning NETSDK1235: .NET Tools do not support using a custom .nuspec file, but the nuspec file
'custom.nuspec' was provided. Remove the NuspecFile property from this project to enable packing it
as a .NET Tool.
```
Emitted when a project sets `PackAsTool=true` **and** `NuspecFile`. Pack still proceeds (warning only).

### 2.8 CLI / developer productivity (condensed)
* `dotnet sln` can create and edit **solution filters** (`.slnf`): `dotnet new slnf --name MyApp.slnf`,
  then `dotnet sln MyApp.slnf add|list|remove`.
* File-based apps: `#:include helpers.cs`, `#:include models/customer.cs`, and
  `#:include ./libs/MyLibrary.dll` (the default item-type mapping treats `.dll` as a `Reference`
  item). Duplicate `#:sdk`, `#:property`, `#:package` directives are allowed across included files
  when their values match. `#:ref` directive (P5). `dotnet reference add --file app.cs <project>`
  writes a `#:project` directive.
* `dotnet run -e KEY=VALUE`. Values surface to MSBuild as `RuntimeEnvironmentVariable` items.
* `dotnet watch`: Aspire app-host integration, automatic crash recovery, better Ctrl+C for WinForms/WPF,
  device selection (`dotnet watch --device <device-id>`), re-restore when a device needs a
  `RuntimeIdentifier` absent from the original restore. Requires `<MtouchLink>None</MtouchLink>` for
  iOS Simulator projects (dotnet/macios #25295).
* Fish shell completions now static+dynamic like bash/zsh/pwsh.
* `dotnet reference add|remove` fall back to the current directory when `--project` is omitted.
* "Using launch settings from…" moved from **stdout to stderr**.
* `dotnet format --framework` for multi-targeted projects; `dotnet format` supports `hidden` severity.
* `dotnet test` (MTP mode): `--no-dependencies`, `DOTNET_TEST_RUNNER` env var (`VSTest` |
  `Microsoft.Testing.Platform`), `--use-current-runtime`/`--ucr`, `!`-prefixed `--test-modules`
  excludes, per-assembly counts, live in-flight test display (`TestInProgressMessages` IPC event),
  two-stage Ctrl+C, `--device` / `--list-devices`, run-level `--timeout` (ms/s/m suffixes; exit code 3)
  and `--maximum-failed-tests` (exit code 13), `Microsoft.Build.Traversal` project support,
  `--artifacts-path`, `--list-tests json`, `-nologo`/`--no-logo`/`--no-banner`,
  `--no-artifact-post-processing`, terminal-logger args (`--tl`, `--terminallogger`, `--tlp`) now
  forwarded to MSBuild. The SDK negotiates **MTP protocol 1.1.0 / 1.2.0 / 1.3.0** and **MTP 2.4** for
  `CancelSession`; new reverse control pipe capability `ServerControlPipeName`.
* `dotnet tool exec` and `dnx` no longer prompt for an extra approval.
* `dotnet publish` no longer removes native DLLs on subsequent single-file publishes.
* `dotnet nuget <subcommand> --help` forwards to the NuGet CLI help.
* `DOTNET_CLI_FORCE_UTF8_ENCODING=false` restores system-default console encoding (P1).
* Templates: `dotnet new xunit --xunit-version v3` (defaults to Microsoft.Testing.Platform),
  `dotnet new nunit --test-runner Microsoft.Testing.Platform`. The `dotnet/templating` repository has
  been **merged into `dotnet/sdk`**.
* Containers: multi-arch with **Podman**; platform-native local runtimes preferred (`wslc` on Windows,
  `container` on macOS) ahead of Docker then Podman; the legacy standalone `containerize` CLI is no
  longer packaged.
* Telemetry: **OpenTelemetry replaces `Microsoft.ApplicationInsights`** in the CLI (Azure Monitor +
  OTLP exporters). Same data, same `DOTNET_CLI_TELEMETRY_OPTOUT`. Motivation: NativeAOT friendliness.
  The OTLP exporter now also activates on any standard `OTEL_EXPORTER_OTLP_*` variable.

### 2.9 .NET 11 SDK/MSBuild breaking changes (learn.microsoft.com/dotnet/core/compatibility/11)

| Title | Type |
|---|---|
| `dnx` scripts bypass `global.json` SDK selection | Behavioural |
| mono launch target not set for .NET Framework apps | Behavioural |
| **NativeAOT CLI command handling enabled by default** | Behavioural |
| **NU1703 warns for packages that use deprecated MonoAndroid framework assets** | Source incompatible |
| **NuGet pack warns for package IDs with restricted characters (NU5052)** | Behavioural |
| SDK local container runtime selection prefers platform-native tools | Behavioural |
| **Template engine packages no longer support `netstandard2.0`** | Binary/source incompatible |
| VSTest removes dependency on `Newtonsoft.Json` | Binary/source incompatible |

Two more from the Preview 7 release notes that are **not yet on the compatibility index**:
* **`.NET tool` packages use the portable RID graph.** Tool restore and install resolve RIDs against
  the portable RID graph. Distributions known only to the legacy graph (some BSD variants) now need a
  portable RID entry. (dotnet/sdk #55046)
* **`NoBuild=true` no longer builds project references.** SDK projects default
  `BuildProjectReferences` to `false` when `NoBuild=true`, so `dotnet publish --no-build` and
  `dotnet pack --no-build` no longer trigger a hidden `NETSDK1085`. *If your build depends on
  `--no-build` still building out-of-date project references, set `BuildProjectReferences=true`
  explicitly.* (dotnet/sdk #55259)

### 2.10 Other .NET 11 breaking changes worth knowing for a compiler host
* **JIT: minimum hardware requirements updated** for x86/x64 and Arm64; ReadyToRun targets updated.
  (`compatibility/jit/11/minimum-hardware-requirements`) — affects any machine the compiler process
  runs on.
* **Extensions: some `Microsoft.Extensions.*` packages are now in the shared framework.** Nine
  `Microsoft.Extensions.*` libraries (Abstractions, Options, Primitives families) ship in the base
  shared framework in .NET 11+. Compile-time name conflicts are possible though rare.
  (`compatibility/extensions/11/extensions-in-shared-framework`)
* **Interop: NativeAOT uses `lib` prefix for native library outputs on Unix.**
* **Deployment:** `configProperties` in `.runtimeconfig.dev.json` now override `.runtimeconfig.json`.
* **Core libraries:** `Assembly.GetCallingAssembly` behaviour changes when stack trace support is
  disabled; API obsoletions with non-default diagnostic IDs.

---

## 3. MSBuild 18.12 in the .NET 11 SDK

Primary sources: `release-notes/11.0/preview/preview6/msbuild.md`, `preview7/msbuild.md`,
`dotnet/msbuild/eng/Versions.props`, `dotnet/msbuild/Directory.Build.props`,
learn.microsoft.com/visualstudio/msbuild/update-task-multithreaded.

### 3.1 Target frameworks
* `dotnet/msbuild/Directory.Build.props`: `FullFrameworkTFM = net472`,
  **`LatestDotNetCoreForMSBuild = net11.0`** (floats to `$(NetCurrent)` in source-only builds).
  This file is described in-repo as "the source of truth" for the .NET Core version of MSBuild.
* So MSBuild 18.12 ships as **net472** (for `MSBuild.exe` / Visual Studio) and **net11.0** (for
  `dotnet build`). A task assembly that must load in both hosts still needs `net472` +
  `netstandard2.0`/`net11.0` assets — **netstandard2.0 remains the only single TFM that loads in
  both**.
* `Microsoft.NET.Build.Tasks` (the SDK's own task assembly) is compiled for **net11.0 and net472**.

### 3.2 Multithreaded mode `-mt` — the single most consequential change for a task author
Introduced in **MSBuild 18.6**, still **experimental** and CLI-only in Preview 7.

```
dotnet build -mt
dotnet msbuild -mt MySolution.sln
```

* `-mt` builds a solution's projects **concurrently inside one MSBuild process** instead of one worker
  process per node.
* Task execution location depends on an **attribute**:
  * A task annotated **`[MSBuildMultiThreadableTask]`** runs **in-process**, sharing the process with
    every other project being built.
  * **Every other task runs isolated in a long-lived sidecar `TaskHost` process** dedicated to its
    node. Existing tasks keep working unmodified — just slower, because of the process hop.
* The attribute, not the interface, is what the engine checks.
* **`MSBuildMultiThreadableTaskAttribute` is matched by namespace + type name only, ignoring the
  defining assembly.** A task author may declare it themselves:
  ```csharp
  namespace Microsoft.Build.Framework
  {
      [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
      internal class MSBuildMultiThreadableTaskAttribute : Attribute { }
  }
  ```
  This "compatibility bridge" works on both old and new MSBuild.
* The attribute is **`Inherited = false`**. A derived class does **not** inherit multithreadability
  from its base.
* `IMultiThreadableTask` (in `Microsoft.Build.Framework`):
  ```csharp
  public interface IMultiThreadableTask : ITask
  {
      TaskEnvironment TaskEnvironment { get; set; }
  }
  ```
  Initialise the property to `TaskEnvironment.Fallback` so the task works outside the engine.
* `TaskEnvironment` members: `ProjectDirectory`, `GetAbsolutePath()`, `GetEnvironmentVariable()`,
  `SetEnvironmentVariable()`, `GetProcessStartInfo()`. **`TaskEnvironment` is itself not thread-safe**
  — capture values into locals before spawning your own threads.
* New value type **`AbsolutePath`** (readonly struct, `Microsoft.Build.Framework`):
  ```csharp
  public readonly struct AbsolutePath : IEquatable<AbsolutePath>
  {
      public string Value { get; }
      public string OriginalValue { get; }
      public AbsolutePath(string path);                     // validates Path.IsPathRooted
      public AbsolutePath(string path, AbsolutePath basePath);
      public static implicit operator string(AbsolutePath path);
  }
  ```
* **Typed task parameters (Preview 7):** tasks can declare parameters/outputs of type `AbsolutePath`,
  `System.IO.FileInfo`, `System.IO.DirectoryInfo`, and the new generic **`ITaskItem<T>`** where `T` is
  one of those path types or a directly-parsed value type (`string`, `bool`, `char`, numeric
  primitives, `decimal`, `DateTime`). The engine validates absoluteness against the task's own
  `TaskEnvironment`. A Roslyn analyzer package to guide task authors through this migration is
  **planned for a future release**.
* **Constructor injection of `TaskEnvironment` (Preview 7):** the engine looks for a public instance
  constructor taking a single `TaskEnvironment` parameter and calls it, falling back to the
  parameterless constructor. Outside the normal in-process path (out-of-proc task host, or a host that
  instantiates the task directly) the engine supplies `TaskEnvironment.Fallback`.
  > **Warning from the release notes:** a task that drops its parameterless constructor entirely
  > "won't load in older MSBuild hosts, such as SDKs earlier than .NET 11 or Visual Studio versions
  > that ship before the November release."
* API-replacement table from the Learn doc:

  | .NET API to avoid | Level | Replacement |
  |---|---|---|
  | `Path.GetFullPath(path)` | ERROR | `TaskEnvironment.GetAbsolutePath(path)`; wrap in `Path.GetFullPath(...)` only if you need canonical form |
  | `File.*` / `Directory.*` with relative paths | ERROR | resolve with `TaskEnvironment.GetAbsolutePath()` first |
  | `Environment.GetEnvironmentVariable()` | ERROR | `TaskEnvironment.GetEnvironmentVariable()` |
  | `Environment.SetEnvironmentVariable()` | ERROR | `TaskEnvironment.SetEnvironmentVariable()` |
  | `Environment.CurrentDirectory` | ERROR | `TaskEnvironment.ProjectDirectory` |
  | `new ProcessStartInfo()` | ERROR | `TaskEnvironment.GetProcessStartInfo()` |
  | `Process.Start()` | ERROR | `ToolTask` or `TaskEnvironment.GetProcessStartInfo()` |
  | static fields | WARNING | instance fields, thread-safe collections, or `IBuildEngine4.RegisterTaskObject` with `RegisteredTaskObjectLifetime.Build` |

* **Visual Studio does not yet support multithreaded execution.** "In Visual Studio, all task execution
  continues to run out of process. Visual Studio integration is planned for a future release."
* `-mt` treats MSBuild Server as a prerequisite and enables it whenever `MSBUILDUSESERVER` is not set
  explicitly.
* Measured: OrchardCore `-t:Rebuild` 146.2 s → 107.8 s on Windows (−26%), 118.8 s → 91.5 s on Linux
  (−23%).

### 3.3 **The `Csc` task is already marked multithreadable**
`dotnet/roslyn/src/Compilers/Core/MSBuildTask/Csc.cs` on `main`:
```csharp
[MSBuildMultiThreadableTask]
public class Csc : ManagedCompiler
```
`ManagedCompiler : ManagedToolTask` and `ManagedToolTask : ToolTask` do **not** carry the attribute
(it is non-inheritable), but they already consume `TaskEnvironment` throughout:
`TaskEnvironment.BuildEnvironment`, `TaskEnvironment.GetTempPath()`, `TaskEnvironment.ProjectDirectory`,
`TaskEnvironment.GetEnvironmentVariable(...)`, `TaskEnvironment.FileExists(itemSpec)`,
`TaskEnvironment.GetFullPathNoThrow(item.ItemSpec)`.

**Direct consequence for a product that replaces `Csc` with its own task:** under `-mt`, the real
`Csc` runs in-process while a replacement task that does not carry `[MSBuildMultiThreadableTask]` is
pushed into a sidecar `TaskHost` process — a behaviour and performance difference, and a difference in
which process holds loaded user assemblies. A replacement task that *does* carry the attribute must
satisfy the whole thread-safety contract above (no process-wide current directory, no
`Environment.*EnvironmentVariable`, no unguarded static state), because multiple projects will run it
concurrently in one process.

### 3.4 MSBuild Server engine changes
* Server GC is now available even with `-nr:false`: `-mt` uses the server anyway, spawning a
  **short-lived server that tears itself down after the build**. Non-`-mt` builds still disqualify the
  server when `-nr:false` is set.
* New structured event **`MSBuildServerLifecycleEventArgs`** reporting spawned / spawned-short-lived /
  reused / not-used plus the **server process ID**. Logged at low importance — appears in binary logs
  and at `-v:diag`, not in default console output.
* Nested MSBuild processes no longer deadlock (nested grants in the coordinator protocol).
* Server GC on the server node gives ~10–13% faster builds on large solutions at ~300 MB extra peak
  memory. Worker nodes and TaskHosts keep Workstation GC.

### 3.5 Task-host IPC: environment sent as a delta
Packet version 5. The full build-process environment (~6 KB) is now sent **once per task-host
connection**; unchanged environments are a **1-byte marker** on both the forward
(`TaskHostConfiguration`) and return (`TaskHostTaskComplete`) paths. Orchard Core, 17,975 external
task-host invocations: 122.0 MB → 0.1 MB (−99.8%). No project or task changes required.

### 3.6 Partial (stop-after-pass) project evaluation — new public API
```csharp
var options = new ProjectOptions { EvaluationStage = ProjectEvaluationStage.Properties };
var project = ProjectInstance.FromFile("MyApp.csproj", options);
project.EvaluationStage;   // Properties
project.Targets.Count;     // 0
```

| `ProjectEvaluationStage` | Stops after |
|---|---|
| `Properties` | pass 1 (properties + imports) |
| `ItemDefinitions` | pass 2 |
| `Items` | pass 3 |
| `UsingTasks` | pass 4 |
| `Full` (default) | pass 5 (targets) |

* **Exposed on `ProjectInstance` only.** Passing a non-`Full` stage to `Project.FromFile` /
  `FromProjectRootElement` / `FromXmlReader` throws **`ArgumentException`** — listed as an MSBuild
  breaking change.
* The CLI uses it: `msbuild -getProperty:Foo` (no `-target`) stops after properties (~15% faster,
  ~22% fewer allocations); `-getItem:Bar` stops after items (~7% faster, ~10% fewer allocations).
  **`MSBUILDDISABLEFEATURESFROMVERSION=18.10` restores the historical full evaluation.**
* The SDK adopted it for `dotnet sln add`, `dotnet reference list`, release-property lookups
  (dotnet/sdk #55271).

### 3.7 Other MSBuild engine changes
* **Faster metadata expansion.** `%(...)` expansion moved from `Regex.Replace` + `MatchEvaluator` to a
  zero-allocation `ref struct` scanner. `Metadata_Unqualified` 413 ns → 124 ns (3.3×, 624 B → 0 B);
  `Metadata_Qualified` 496 ns → 208 ns (2.4×). **No opt-out.**
* **Trim/AOT-clean evaluation object model.** `Microsoft.Build` evaluation is now trim- and
  Native-AOT-capable; property-function receiver-type discovery was rewritten. Open-world reflective
  paths — loading **tasks, SDK resolvers, loggers, build checks by name** — and task execution still
  require closed-world registration and "fail observably at run time rather than silently when it's
  missing." The `dotnet` CLI is expected to build on this to make `build`/`publish`/`pack`/`restore`/
  `clean`/`msbuild` work from a Native AOT CLI in a future release.
* Preview 6 additions: `ProjectGraphMode.Full` (load the entire dependency graph across all target
  frameworks in one pass); `MSBuildImportedProject` items exposing the import tree when
  `MSBuildProvideImportedProjects` is `true`; per-path locking in `ProjectCollection.LoadProject` for
  concurrent evaluation.
* Preview 4: `NET10_0_OR_GREATER` and cumulative defines are now emitted when targeting `net10.0` on
  MSBuild < 18.

### 3.8 MSBuild breaking changes
1. **`Project.FromFile` (and `FromProjectRootElement` / `FromXmlReader`) rejects partial evaluation** —
   `ArgumentException`.
2. **NuGet `RestoreTask` uses normal task-host routing again.** Preview 5's transient-`TaskHost`
   workaround for NuGet's static singleton state (`PluginManager`, `EnvironmentWrapper`) was removed.
   **`BuildParameters.IsLongLivedHost` and `MarkProcessAsLongLivedHost()` are gone** — hosts that
   called them must remove the calls. This is a *removed public API* on `Microsoft.Build`.

---

## 4. NuGet in the .NET 11 SDK

Primary source: `release-notes/11.0/preview/preview7/nuget.md`, plus
learn.microsoft.com/nuget/release-notes/nuget-7.9 and the .NET 11 compatibility index.

### 4.1 Restore records **analyzer assets** in `project.assets.json` — the big one for analyzer authors
* Restore writes an **`analyzers` group per package** in `project.assets.json` when the project targets
  **.NET 11 or later** and opts in with **`RestoreEnableAnalyzerAssets`**.
* Every analyzer assembly under `analyzers/` is listed and annotated with:
  * **`codeLanguage`** — `cs`, `vb`, `fs`, or `any`;
  * **`compilerApiVersion`** — `roslynX.Y`, when a `roslynX.Y` segment is present in the path.
* Purpose: make `PrivateAssets`, `ExcludeAssets`, `IncludeAssets` finally apply to a package's
  analyzers, instead of every analyzer a package ships always being loaded.
* Example produced layout:
  ```json
  "Microsoft.CodeAnalysis.NetAnalyzers/9.0.0": {
    "type": "package",
    "analyzers": {
      "analyzers/dotnet/cs/Microsoft.CodeAnalysis.NetAnalyzers.dll": { "codeLanguage": "cs" }
    }
  },
  "StyleCop.Analyzers/1.2.0-beta.556": {
    "type": "package",
    "analyzers": { "analyzers/_._": {} }
  }
  ```
  Excluded analyzers become the `_._` placeholder already used for excluded `compile`/`runtime`/
  `native` assets.
* `RestoreEnableAnalyzerAssets` is **gated per target framework**, so a multi-targeted project only
  gets the section on frameworks new enough to consume it.
* **In Preview 7 this has no effect on which analyzers actually run.** The compiler still loads every
  analyzer a package ships, because the SDK-side consumer (**dotnet/sdk #54646**) has not landed.
* **Explicit call to action for analyzer package authors:** "make sure your analyzer assemblies are
  laid out correctly under `analyzers/<codeLanguage>/` (with a `roslynX.Y` segment if you ship
  compiler-API-specific builds), so they're represented correctly in this new asset group ahead of the
  SDK enforcing it."
  (NuGet/NuGet.Client #7464, NuGet/Home #6279, NuGet/Home #14455)

### 4.2 Restore runs safely under multithreaded MSBuild
* The Restore Task and its supporting tasks were migrated to the multithreaded-safe model. Restore
  now produces the same results under `dotnet build -mt`, a reused MSBuild Server process, or several
  concurrent project restores.
* Previously restore could carry stale environment, credential and plugin state between builds sharing
  a reused process, and could resolve a relative path against the wrong project's directory.
* Static-graph restore (fresh short-lived process per build), `nuget.exe`, and Visual Studio builds are
  unaffected.
* **New public API for plugin and credential-provider authors:** a `NuGetProcessState` registry in
  `NuGet.Common` with **`RegisterResetAction(ResetKey, Action)`** / **`Reset(ResetKey)`**. NuGet's own
  environment, credential-service and plugin caches use it; a plugin with process-wide state that must
  be refreshed between reused-process restores should register a reset action the same way.
* Note the wording: "making this [the multithreaded, in-process task model] the default build mode is
  still coming in a future release."

### 4.3 Pack
* **Pack reuses existing project evaluations.** `dotnet pack` no longer passes
  `BuildProjectReferences=false` as a global property on its inner MSBuild calls, which used to
  produce a *distinct* evaluation from the ones `Build` had already created — roughly doubling
  evaluations for every affected TFM and project reference in a multi-targeting graph. Also removes a
  second source of redundant evaluations specific to single-targeting projects.
  (NuGet/NuGet.Client #7541, reverted and re-implemented as #7603 to avoid an item-name collision with
  ASP.NET Core's pack targets.)
* **NU5052 — package ID standards.** nuget.org is phasing in stricter ID rules; new IDs must be
  ASCII-only and nuget.org will soon reject non-conforming IDs.
  ```
  warning NU5052: The package ID 'Contoso.Café' is invalid. Package IDs must start with a letter,
  digit, or underscore, and contain only ASCII letters, digits, dots (.), dashes (-), and underscores
  (_), with no consecutive dots or dashes.
  ```
  Advisory only in .NET 11 — pack still produces the package. (NuGet/Announcements #75)

### 4.4 Restore performance
Restore no longer scans the full version list of the global packages folder or fallback folders when
it cannot find an exact package version there. (NuGet/NuGet.Client #7569)

### 4.5 NU1703 — deprecated MonoAndroid framework assets
Listed as a **source-incompatible** SDK breaking change and gated on `SdkAnalysisLevel = 11.0.100`.

### 4.6 Package source mapping, central package management, audit — what actually changed
* The Preview 7 NuGet notes contain **nothing** about package source mapping, central package
  management, or NuGet Audit. See Open questions.
* Carried over from **NuGet 7.0** (which shipped with .NET 10 SDK / VS 18.0): projects targeting
  .NET 10+ default to **`NuGetAuditMode=all`** (warn for vulnerabilities in transitive packages), and
  **package pruning is enabled for all projects targeting .NET 10**. `project.json` support was
  removed in 7.0. A NU1011 error with Central Package Management + floating versions was fixed.
* NuGet 7.9 (VS 18.9 / SDK 10.0.400): restore validates that audit sources use HTTPS; the NuGet SDK is
  migrating from `Newtonsoft.Json` to `System.Text.Json`; breaking changes in the NuGet client
  libraries — `SearchFilter.PackageTypes` renamed to `PackageType`, nullable annotations added to
  `NuGet.Protocol`; `NuGetProjectServiceV1` brokered service uses correct serialization settings so it
  is usable from out-of-process VS extensions.

---

## 5. Roslyn, analyzers and source generators

### 5.1 Version
* `dotnet/roslyn/eng/Versions.props` on `main`: `MajorVersion 5`, `MinorVersion 12`, `PatchVersion 0`,
  `VersionPrefix 5.12.0`, `PreReleaseVersionLabel 1`. → **Roslyn 5.12** is the compiler/analyzer API
  version the .NET 11 SDK and Visual Studio 2027 will carry.
* Independent corroboration: `dotnet/vscode-csharp` `main` `package.json` pins
  **`roslyn: 5.12.0-1.26428.1`** (and `xamlTools: 18.10.12014.341`).
* The Roslyn↔VS mapping doc (learn.microsoft.com/visualstudio/extensibility/roslyn-version-support,
  updated 2026-04-24) still tops out at **`5.0.0 → Visual Studio 2026 version 18.0`**; it has not been
  updated for the monthly 18.x cadence. **[The 5.12 ↔ 18.12 pairing is therefore an inference.]**
* `dotnet/roslyn/eng/Versions.props` also sets `MicrosoftCodeAnalysisVersionForAnalyzers = 4.12.0`
  (and `…ForAnalyzerTests`, `…ForAnalyzerExecution`, `…FromSbrp` = 4.12.0). This is the API version
  the analyzers **inside the Roslyn repo** compile against for downlevel compatibility — **not** the
  version the SDK loads at build time.

### 5.2 Target framework of Roslyn's own components
`dotnet/roslyn/eng/targets/TargetFrameworks.props` on `main`:
```
NetRoslyn                        = net10.0
NetRoslynAll                     = net10.0
NetRoslynWindowsTests            = net10.0-windows
NetVS                            = net10.0
NetVSCode                        = net10.0
NetVSShared                      = net10.0
NetRoslynNext                    = net10.0
NetRoslynBuildHostNetCoreVersion = net8.0
```
(Redirected to `$(NetCurrent)` when `DotNetBuildSourceOnly == true`.)

**Reading:** as of 2026-09-03 the Roslyn compiler and the Visual Studio / VS Code Roslyn components
still target **`net10.0`**, not `net11.0`. They run on the .NET 11 runtime by roll-forward. The
out-of-process build host (`Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost`) still has a
`net8.0` leg. This may be bumped before GA — flagged in Open questions.

### 5.3 Analyzer / source generator target framework requirement — unchanged
* Analyzers and source generators **must still target `netstandard2.0`**. The reason is unchanged:
  the compiler loads them into hosts running on .NET Framework (`MSBuild.exe`, `devenv.exe`, the
  in-proc VS pipeline) *and* on .NET (`dotnet build`, `csc.dll`), and `netstandard2.0` is the newest
  framework that both can load.
  (dotnet/roslyn discussions #72777, #51640; issue #39988)
* Nothing in the .NET 11 SDK release notes, the .NET 11 breaking-change index, or the Roslyn repo
  changes this.
* What *did* change is the **packaging expectation**: analyzers should be laid out under
  `analyzers/<codeLanguage>/` with an optional `roslynX.Y` segment (`analyzers/dotnet/roslyn5.12/cs/…`)
  so restore can record `codeLanguage` and `compilerApiVersion` (§4.1).

### 5.4 Analyzer loading and isolation
* The relevant rework is **dotnet/roslyn PR #77004, "Rework analyzer assembly loading"** (jaredpar):
  `AnalyzerAssemblyLoader` became **sealed**, with customization moved to interfaces rather than
  derivation, so that VBCSCompiler, the VS IDE, Razor and VS Code can each customise (a) *where* an
  assembly is loaded from (shadow copying) and (b) *which* `Assembly` is returned for a given
  path/name (Razor needs to control which `AssemblyLoadContext` supplies the assembly inside the VS
  out-of-process host). This landed in the **.NET 10 / Roslyn 5.0** timeframe, not .NET 11.
* **No .NET 11-specific change to analyzer loading or isolation was found** in the .NET 11 release
  notes, the compiler breaking-change document, or the compatibility index. See Open questions.

### 5.5 C# compiler breaking changes relevant to tooling
From learn.microsoft.com/dotnet/csharp/whats-new/breaking-changes/compiler breaking changes - dotnet 11
(source: `dotnet/roslyn/docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`). "This
document lists known breaking changes in Roslyn after .NET 10 general release (.NET SDK version
10.0.100) through .NET 11 general release (.NET SDK version 11.0.100)." Each entry names the VS
version that introduced it — a useful independent confirmation of the 18.x cadence:

| Change | Introduced in |
|---|---|
| safe-context of a `Span`/`ReadOnlySpan` collection expression is now *declaration-block* | VS 18.3 |
| `ref readonly` synthesized delegates require `System.Runtime.InteropServices.InAttribute` | VS 18.3 |
| `ref readonly` local functions require `InAttribute` | VS 18.3 |
| Dynamic `&&`/`\|\|` with an interface-typed left operand is an error (CS7083) | VS 18.3 |
| `nameof(this.X)` in attributes disallowed | VS 18.3 and .NET 10.0.200 |
| Parsing of `with` inside a switch-expression-arm | VS 18.4 |
| `with()` as a collection-expression element = construction arguments (LangVersion ≥ 15); use `@with` to call a method named `with` | VS 18.4 |
| Pointer types no longer require an `unsafe` context (C# 16) | VS 18.7 |
| **`safe` is a contextual keyword** (C# 16) | VS 18.9 |
| `unsafe` required for more members (C# 16, `langversion:16`) | VS 18.9 |
| **`closed` is a contextual keyword in type declaration contexts** — a type or alias named `closed` without `@` produces **CS9380**; in member declaration contexts `closed` is a modifier, so a field of type `closed` now parses as an incomplete declaration and produces **CS1519** | VS 18.10 |
| **`union` is a contextual keyword in type declaration contexts** — `union` followed by a type name parses as a union declaration; a field of type `union` now produces **CS9370** | VS 18.10 |

Note the oddity: the same document already lists **C# 16** changes at VS 18.7/18.9 while C# 15 ships
with .NET 11 at VS 18.12. The document is a running log of the Roslyn `main` branch and mixes
LangVersion-gated C# 16 preview behaviour into the .NET 11 window.

### 5.6 Default `LangVersion`
learn.microsoft.com/dotnet/csharp/language-reference/language-versioning#defaults:

| Target | Version | Default C# |
|---|---|---|
| .NET | **11.x** | **C# 15** |
| .NET | 10.x | C# 14 |
| .NET | 9.x | C# 13 |
| .NET Standard | 2.1 | C# 8.0 |
| .NET Standard | 2.0 | C# 7.3 |
| .NET Framework | all | C# 7.3 |

* "C# 15 is supported only on .NET 11 and newer versions… Using a C# language version newer than the
  version associated with your target TFM is unsupported."
* `<LangVersion>preview</LangVersion>` is required for: **union types**, the **memory-safety /
  unsafe-evolution** work (pointer relaxations, `unsafe(expression)`), and the `safe` contextual
  keyword. `union` and `closed` are contextual keywords in C# 15 proper.
* "If your project targets a `preview` framework that has a corresponding preview language version,
  the language version used is the preview language version."

---

## 6. Visual Studio 2027

### 6.1 Naming, cadence and version number
* learn.microsoft.com/visualstudio/releases/2026/release-rhythm: "beginning with Visual Studio 2026 we
  plan to deliver new **annual releases each November** along with the new major version of .NET. These
  annual releases will be **in-place updates to the prior annual year's release, rather than
  side-by-side**."
* devblogs.microsoft.com/visualstudio/visual-studio-built-for-the-speed-of-modern-development:
  Visual Studio **2027** is scheduled for **November 2026**, delivered as an in-place update that
  replaces Visual Studio 2026.
* Stable-channel product version format is **`18.<Minor>.<Servicing>`**, `<Minor>` incrementing every
  month. Insiders is `18.<Minor> Insiders <BuildNumber>`.
* **[inference]** With 18.0 = Nov '25 and 18.9 = Aug '26, **Visual Studio 2027 = version 18.12**. The
  MSBuild `VersionPrefix 18.12.0` and Roslyn `5.12.0` on `main` corroborate this.
* Support: each annual release gets 1 year of feature updates + servicing, then 1 year of security
  servicing as an **LTSC**. "The LTSC Channel for users of the Professional, Enterprise, and Build
  Tools editions of Visual Studio 2026 will be available in **November of 2026**." So in Nov 2026 a
  customer may pin to the **Visual Studio 2026 LTSC** for one more year rather than take 2027.
* "Build tools choice": the IDE is decoupled from the compilers/SDKs it carries; multiple supported
  toolset versions ship side by side.

### 6.2 Runtime the IDE and its services run on
* **`devenv.exe` still runs on .NET Framework 4.8.** learn.microsoft.com/visualstudio/releases/2026/
  vs-system-requirements: Visual Studio 2026 requires .NET Framework 4.8, installed by setup if absent.
  No announcement of a change for 2027 was found.
* **Roslyn's out-of-process services target `net10.0`** — `NetVS = net10.0` in
  `dotnet/roslyn/eng/targets/TargetFrameworks.props` on `main`. `ServiceHub.RoslynCodeAnalysisService`
  is built from those projects, so it runs on the .NET 10 (or newer, by roll-forward) runtime.
  This is a **strong inference from the Roslyn build configuration**, not a statement in a VS document.
* VS 2026 platform support for .NET: ".NET Core 10.0, 9.0, 8.0" and .NET Framework 4.8.1, 4.8, 4.7.2,
  4.7.1, 4.7, 4.6.2, 3.5 SP1 (learn.microsoft.com/visualstudio/releases/2026/compatibility, dated
  2025-11-11 — it predates .NET 11).
* **Practical consequence, unchanged from VS 2022/2026:** an analyzer or generator loaded in-proc by
  `devenv.exe` runs on .NET Framework 4.8; the same assembly loaded by
  `ServiceHub.RoslynCodeAnalysisService` runs on .NET 10; the same assembly loaded by `csc.dll` under
  `dotnet build` runs on .NET 11; the same assembly loaded by `MSBuild.exe`'s `csc.exe` runs on .NET
  Framework 4.7.2+. `netstandard2.0` remains the only TFM that satisfies all four.

### 6.3 Extension model / out-of-process hosting
No .NET 11-cycle change to the VS extension model or to how out-of-process analyzers are hosted was
found in any primary source. The one adjacent change is MSBuild's: **Visual Studio does not support
`-mt` multithreaded in-process task execution**; "In Visual Studio, all task execution continues to
run out of process."

---

## 7. JetBrains Rider and the VS Code C# Dev Kit

### 7.1 VS Code C# extension / C# Dev Kit
* `dotnet/vscode-csharp` `main` `package.json` `defaults`:
  * **`roslyn: 5.12.0-1.26428.1`**
  * `omniSharp: 1.39.14`
  * `razorOmnisharp: 7.0.0-preview.23363.1`
  * `xamlTools: 18.10.12014.341`
  * `testDiscovery: 9.9.434-g84ca4d`
  * `engines.vscode: ^1.106.0`
* CHANGELOG trail of Roslyn bumps: 2.147.x → `5.10.0-1.26376.1` / `5.11.0-1.26379.2`;
  2.148.x → `5.11.0-1.26380.4`; 2.149.x → `5.11.0-1.26405.8`; **2.150.x → `5.12.0-1.26428.1`**.
  Latest released line is **2.150.x**.
* Historic note in the changelog: 2.122.x introduced a **"balanced" source generator execution mode**
  as a performance improvement over automatic execution.
* The extension depends on the `ms-dotnettools` **.NET Runtime** extension and ships binaries for
  .NET Framework 4.7.2 / .NET 6+ depending on platform.

### 7.2 JetBrains Rider
* Current release line at 2026-09-03: **Rider 2026.2** (released 2026-07-22; 2026.2.1 on 2026-08-19).
* Rider 2026.1 shipped "early support for C# 15 Preview" and already implements `ExtendedLayoutAttribute`.
* JetBrains stated that early .NET 11 support bits were expected in **2026.2**, with the caveat that
  .NET 11 was still early preview; work on **collection expression arguments** and **dictionary
  expressions** was in progress and **C# unions** had not been started when the roadmap was written.
* **The Roslyn version Rider bundles is not published in any primary source I could reach.** Rider
  analyses C# with its own ReSharper engine and hosts Roslyn only to run third-party analyzers and
  source generators. See Open questions.
* Rider 2026.2 mentions "more reliable source generator debugging on Linux and macOS".

---

## 8. `net472` / `netstandard2.0` assets a tool must still ship

Answer: **yes, both are still required, and .NET 11 does not relax this.**

* **Analyzers and source generators: `netstandard2.0`.** Unchanged requirement; see §5.3. Multi-RID
  analyzer packages may additionally ship `roslynX.Y`-segmented folders, and .NET 11 restore now
  *records* that segment as `compilerApiVersion` (§4.1).
* **MSBuild tasks: `net472` + a .NET leg.** `dotnet/msbuild/Directory.Build.props` sets
  `FullFrameworkTFM = net472` and `LatestDotNetCoreForMSBuild = net11.0`; the SDK's own
  `Microsoft.NET.Build.Tasks` is built for **net11.0 and net472**. `MSBuild.exe` under Visual Studio is
  still .NET Framework. A single `netstandard2.0` task assembly still loads in both, at the cost of
  API surface.
* **The one place .NET 11 does drop `netstandard2.0`** is the **template engine**:
  `learn.microsoft.com/dotnet/core/compatibility/sdk/11/template-engine-netstandard`, introduced in
  **.NET 11 Preview 4**. These packages now target only **`net9.0`, `net11.0` and `net472`**:
  * `Microsoft.TemplateEngine.Abstractions`
  * `Microsoft.TemplateEngine.Core`
  * `Microsoft.TemplateEngine.Core.Contracts`
  * `Microsoft.TemplateEngine.Edge`
  * `Microsoft.TemplateEngine.Orchestrator.RunnableProjects`
  * `Microsoft.TemplateEngine.Utils`
  * `Microsoft.TemplateEngine.IDE`
  * `Microsoft.TemplateEngine.TemplateLocalizer.Core`

  Reason given: "NuGet client SDK packages (`NuGet.*`) stopped targeting `netstandard2.0` starting with
  **version 7.0**. `Microsoft.TemplateEngine.Edge` depends on `NuGet.Configuration`,
  `NuGet.Credentials` and `NuGet.Protocol`… To avoid transitive dependency conflicts, the project had
  to pin these packages to older versions and disable `CentralPackageTransitivePinningEnabled`."
  (dotnet/sdk #54041). Public API is unchanged; only the TFM set changed.
  Categorised as **binary and source incompatible**.
* **Corollary worth flagging:** the **NuGet client libraries (`NuGet.*`) have not shipped a
  `netstandard2.0` asset since NuGet 7.0** (the .NET 10 wave). Any tool that references
  `NuGet.Protocol`/`NuGet.Configuration`/`NuGet.Credentials` from a `netstandard2.0` assembly is pinned
  to NuGet 6.x.
* **VSTest removed its dependency on `Newtonsoft.Json`** (binary/source incompatible), and the NuGet
  SDK is migrating from `Newtonsoft.Json` to `System.Text.Json`.
* **`Microsoft.Extensions.*` Abstractions/Options/Primitives are now in the .NET 11 shared framework.**
  A tool that ships its own copies alongside a .NET 11 host can now hit assembly-identity conflicts.

---

## 9. What matters most if you replace the compiler

Condensed, no Metalama-specific analysis (that is a separate stage) — just the facts that bear on a
custom `Csc` task, a compiler-replacement package, or an analyzer/generator that runs in every host:

1. **MSBuild server is on by default.** The MSBuild process now survives between CLI invocations
   unless the user opts out. Static state in a task assembly is now shared across builds by default.
2. **`-mt` exists and the real `Csc` task opts into it** (`[MSBuildMultiThreadableTask]`). A
   replacement task without the attribute is relegated to a sidecar `TaskHost`. With the attribute, it
   must be genuinely thread-safe and must use `TaskEnvironment` rather than `Environment.*`,
   `Environment.CurrentDirectory`, `Path.GetFullPath`, or `new ProcessStartInfo()`.
3. **`MSBuildMultiThreadableTaskAttribute` is matched by namespace+name only**, so it can be declared
   locally to keep one assembly working on older MSBuild.
4. **A task that only has a `TaskEnvironment`-parameter constructor will not load on pre-.NET 11
   MSBuild.** Keep a parameterless constructor.
5. **`BuildParameters.IsLongLivedHost` and `MarkProcessAsLongLivedHost()` were removed** from
   `Microsoft.Build`.
6. **`ProjectInstance` gained `ProjectEvaluationStage`; `Project` throws on non-`Full`.**
7. **`NoBuild=true` now implies `BuildProjectReferences=false`.**
8. **Restore will start honouring `ExcludeAssets`/`PrivateAssets`/`IncludeAssets` for analyzers.** The
   `analyzers/<codeLanguage>[/roslynX.Y]/` layout is now load-bearing metadata; the SDK-side enforcement
   is coming.
9. **`AnalysisLevel=latest` now really means .NET 11 rules** (it silently meant .NET 9 before).
10. **`SdkAnalysisLevel` values older than `8.0.100` are ignored** by the 11.0.100 SDK.
11. **Default `LangVersion` for `net11.0` is C# 15**, which makes `union` and `closed` contextual
    keywords and changes how `with(...)` inside a collection expression parses.
12. **`netstandard2.0` is still mandatory for analyzers and generators**; `net472` is still mandatory
    for MSBuild tasks that must load in `MSBuild.exe`.
13. **`dotnet build`'s MSBuild runs on `net11.0`; Roslyn's own components still target `net10.0`;
    `devenv.exe` is still .NET Framework 4.8.** Four distinct runtimes for the same analyzer assembly.

---

## 10. Open questions

1. **Exact GA version numbers.** No RC had shipped by 2026-09-03. The GA SDK version (`11.0.100`),
   the exact MSBuild version (18.12.x), the exact Roslyn version (5.12.x) and the exact NuGet version
   are all extrapolations from `main` branches.
2. **The `11.0.1xx` rows are missing** from
   learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs, so the *documented* minimum
   Visual Studio version for the .NET 11 SDK, and the officially supported VS floor for `net11.0`
   targeting, are not yet published.
3. **Roslyn↔VS version mapping for 18.x is undocumented.** The extensibility doc still stops at
   `5.0.0 → 18.0`. The 5.12 ↔ 18.12 pairing is inferred from `dotnet/msbuild` and `dotnet/roslyn`
   `main` branches and from `dotnet/vscode-csharp`'s pinned Roslyn version.
4. **Will Roslyn bump `NetVS`/`NetRoslyn` from `net10.0` to `net11.0` before GA?** As of 2026-09-03 it
   is `net10.0`, which would mean `ServiceHub.RoslynCodeAnalysisService` and `csc.dll` in the .NET 11
   SDK are `net10.0` assemblies running on the .NET 11 runtime by roll-forward.
5. **No .NET 11-specific change to analyzer loading or isolation was found.** The last substantial
   rework (`AnalyzerAssemblyLoader` sealed, interface-based customization, dotnet/roslyn #77004) was
   .NET 10 era. It is possible that something landed in Roslyn `main` after the last published .NET 11
   preview notes; the release notes do not carry a Roslyn-internals section.
6. **NuGet in .NET 11: nothing published on package source mapping, central package management, or
   audit.** The .NET 11 preview NuGet notes cover only analyzer assets, multithreaded restore, pack
   evaluation reuse, NU5052 and a global-packages-folder scan optimisation. The NuGet 7.10/7.11/7.12
   release notes (which will correspond to VS 18.10–18.12) had not been published to
   learn.microsoft.com/nuget/release-notes when checked; the index listed up to **NuGet 7.9**.
7. **Which NuGet client version ships in SDK 11.0.100** is inferred (7.12) from the
   NuGet-minor-tracks-VS-minor pattern, not stated anywhere.
8. **Rider's bundled Roslyn version** is not published. Rider 2026.2's release material does not state
   a Roslyn version, an MSBuild version, or its .NET 11 support level in detail.
9. **`analyzers/<codeLanguage>` vs `analyzers/dotnet/<codeLanguage>` and where the `roslynX.Y` segment
   goes.** The NuGet notes say "`analyzers/<codeLanguage>/` (with a `roslynX.Y` segment if you ship
   compiler-API-specific builds)" but the worked example uses `analyzers/dotnet/cs/…`. The precise
   grammar restore parses is not spelled out in the release notes.
10. **Whether `dotnet/sdk#54646`** (the SDK-side consumer that would actually stop loading excluded
    analyzers) lands for .NET 11 GA or slips to .NET 12.
11. **No Visual Studio 2027 release notes exist yet**, so its Roslyn version, private runtime target
    framework, ServiceHub runtime and any extension-model change are all unconfirmed by a
    VS-authored document.

---

## 11. Source URLs used

* https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk
* https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview
* https://learn.microsoft.com/en-us/dotnet/core/compatibility/11
* https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/11/template-engine-netstandard
* https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs
* https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props
* https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15
* https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning
* https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version
* https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/language
* https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011
* https://learn.microsoft.com/en-us/visualstudio/msbuild/update-task-multithreaded
* https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-rhythm
* https://learn.microsoft.com/en-us/visualstudio/releases/2026/compatibility
* https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support
* https://learn.microsoft.com/en-us/nuget/release-notes/nuget-7.9
* https://learn.microsoft.com/en-us/nuget/release-notes/nuget-7.0
* https://devblogs.microsoft.com/visualstudio/visual-studio-built-for-the-speed-of-modern-development/
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/msbuild.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/sdk.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/msbuild.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/nuget.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/csharp.md
* https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/build-metadata.json
* https://github.com/dotnet/msbuild/blob/main/eng/Versions.props
* https://github.com/dotnet/msbuild/blob/main/Directory.Build.props
* https://github.com/dotnet/msbuild/blob/main/documentation/specs/multithreading/multithreaded-msbuild.md
* https://github.com/dotnet/msbuild/blob/main/documentation/specs/multithreading/thread-safe-tasks.md
* https://github.com/dotnet/roslyn/blob/main/eng/Versions.props
* https://github.com/dotnet/roslyn/blob/main/eng/targets/TargetFrameworks.props
* https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/Csc.cs
* https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/ManagedCompiler.cs
* https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/ManagedToolTask.cs
* https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md
* https://github.com/dotnet/roslyn/pull/77004
* https://github.com/dotnet/sdk/blob/main/eng/Versions.props
* https://github.com/dotnet/vscode-csharp/blob/main/package.json
* https://github.com/dotnet/vscode-csharp/blob/main/CHANGELOG.md
* https://blog.jetbrains.com/dotnet/2026/07/22/rider-2026-2-release/
