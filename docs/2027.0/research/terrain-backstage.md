# Terrain map: Metalama.Backstage, for the C# 15 and .NET 11 wave

Scope: `C:\src\Metalama-2027.0\Metalama\Metalama.Backstage\src\**`, plus the one consumer of this subsystem that
lives in the premium repository (`Metalama.Licensing.BuildTasks`).

Branch surveyed: `topic/2027.0/26-09-03-net11-impact`, based on `develop/2027.0`.

All paths below are absolute unless they start with `Metalama.Backstage/`, in which case they are relative to
`C:\src\Metalama-2027.0\Metalama\`.

---

## 0. The one-paragraph answer

Backstage is the only Metalama subsystem that references **no Roslyn assembly at all**. It contains no syntax
node, no `SyntaxKind`, no `LanguageVersion`, no symbol, and no code that enumerates the shape of the C# language.
The C# 15 grammar additions (`UnionDeclarationSyntax`, `UnsafeExpressionSyntax`, `WithElementSyntax`, the optional
`Name` field on `BreakStatementSyntax` and `ContinueStatementSyntax`, the `closed` modifier, extension-block
indexers) cannot reach it. The C# 14 wave (#1034, #1035, #1036, #1094, #1105, #1108-#1116, #1127, #1131, #1143,
#1159, #1160) produced **zero commits** in `Metalama.Backstage/src`. Verified by
`git log --all --oneline --grep=<issue> -- Metalama.Backstage`, which returns nothing for every one of them.

What Backstage *is* exposed to is the other half of the wave: the **.NET 11 runtime**, the **.NET 11 SDK**, the
raised **`AnalysisLevel`** that follows the target framework, and the **removal of finite-field DSA on macOS**,
which is issue [#1860] and is the single largest item in this subsystem. `LangVersion` is `latest` for every
project here (inherited, see §2.1), so C# 15 syntax silently becomes legal in this codebase the moment the .NET 11
SDK is installed, with no gate and no opt-in.

---

## 1. Answer to question 1: sensitivity to the set of C# language constructs

**None.** This is a positive finding, and it should be stated as such rather than left implicit.

Evidence:

| Probe | Result |
| --- | --- |
| `grep -rn "Microsoft.CodeAnalysis"` over `Metalama.Backstage/src/**/*.cs` | 1 hit, and it is a string literal in a test: `Metalama.Backstage/src/tests/Metalama.Backstage.Tests/Telemetry/ExceptionReportContentTests.cs:73` — `[InlineData( "Microsoft.CodeAnalysis", true )]`, an assembly-name prefix used by the telemetry redaction filter. |
| `grep -rn "SyntaxKind\|LanguageVersion\|CSharp14"` over the same tree | 0 hits. |
| Roslyn `PackageReference` in any `.csproj` under `Metalama.Backstage/src` | 0. |
| `RoslynApiMinVersion` / `RoslynApiMaxVersion` referenced | 0. |
| `eng/RoslynVersions/` variant participation | none; Backstage ships one payload, not one per Roslyn variant. |

The only place in the subsystem that mentions Roslyn at all is the *process-kind* vocabulary, which names the
Roslyn analysis process as a host, not as an API:

- `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/ProcessKind.cs:30` — `RoslynCodeAnalysisService`
- `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:46-49` — the process names that map to it
- `Metalama.Backstage/src/Metalama.Backstage/Telemetry/ExceptionSensitiveDataHelper.cs:29` — `"Roslyn"` in the list
  of assembly-name prefixes considered non-sensitive

The nearest thing to a "language" enumeration in the subsystem is the **license key field vocabulary**, which is a
private binary format and has nothing to do with C#. It is described in §4.5 because it is the structural analogue
that a reviewer might otherwise look for.

**Consequence for the C# 15 work:** no member of this subsystem must be extended for a new type declaration, a new
modifier, a new expression form, a new collection-expression element, or a new optional field on a statement. The
subsystem's exposure is entirely on the platform axis.

---

## 2. Answer to question 2: sensitivity to the .NET runtime, the .NET SDK, Roslyn, or the host IDE

### 2.1 Compilation-level sensitivity (affects every file)

| Location | What it does | Sensitive to |
| --- | --- | --- |
| `%USERPROFILE%\.nuget\packages\postsharp.engineering.sdk\2023.2.412\sdk\BuildOptions.props:4` | `<LangVersion>latest</LangVersion>`, imported by `Metalama.Backstage/Directory.Build.props` (`<Import Sdk="PostSharp.Engineering.Sdk" Project="BuildOptions.props"/>`) | **SDK version.** With the .NET 11 SDK, every Backstage project silently compiles at C# 15. No project under `Metalama.Backstage/src` sets `LangVersion` itself. |
| `%USERPROFILE%\.nuget\packages\postsharp.engineering.sdk\2023.2.412\sdk\CodeQuality.targets:17-19` | `TreatWarningsAsErrors=true` when `ContinuousIntegrationBuild=True` | **SDK version**, through `AnalysisLevel`. |
| `Directory.Build.props:1-37` (repository root) | `NoWarn` and `WarningsNotAsErrors` lists | The `AnalysisLevel` default follows the target framework, so moving `net10.0` to `net11.0` turns on rules that are off today. See §3. |
| `Metalama.Backstage/Directory.Build.props:24` | `<NoWarn>$(NoWarn);CS8002;IDE0028</NoWarn>` | Same mechanism, Backstage-local. |

`Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj` also sets `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`,
consumed by `Utilities/HashUtilities.cs:34` and `:51` and by `Threading/MutexAcl.cs:40` (`public static unsafe Mutex Create`).

### 2.2 Target frameworks declared in this subsystem

| Project | `TargetFramework(s)` | Notes |
| --- | --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj:5` | `netframework4.7.2;net10.0;netstandard2.0` | The three-flavour shape of PB-2027.0. |
| `Metalama.Backstage/src/Metalama.Testing.Hooks/Metalama.Testing.Hooks.csproj:5` | `netstandard2.0;net10.0` | |
| `Metalama.Backstage/src/Metalama.Backstage.Tools/Metalama.Backstage.Tools.csproj:5` | `netstandard2.0` | Embeds the two tool zips as resources. |
| `Metalama.Backstage/src/Metalama.Backstage.Commands/Metalama.Backstage.Commands.csproj:5` | `net10.0` | `InvariantGlobalization=True` (line 13). |
| `Metalama.Backstage/src/Metalama.Backstage.Worker/Metalama.Backstage.Worker.csproj:35` | `net10.0`, `RollForward=Major` (line 38) | ASP.NET Core web host. |
| `Metalama.Backstage/src/Metalama.Backstage.Desktop.Windows/Metalama.Backstage.Desktop.Windows.csproj:6` | `net10.0-windows10.0.17763.0`, `RollForward=Major` | WPF plus WinRT toast notifications. |
| `Metalama.Backstage/src/Metalama.Backstage.DotNetTool/Metalama.Backstage.DotNetTool.csproj:5` | `net10.0`, `RollForward=Major` (line 18) | |
| `Metalama.Backstage/src/utilities/PostSharp.LicenseKeyGenerator/*.csproj:5` | `net10.0-windows` | |
| `Metalama.Backstage/src/utilities/PostSharp.LicenseKeyReader/*.csproj:5` | `net10.0-windows` | |
| `C:\src\Metalama-2027.0\Metalama.Premium\src\Metalama.Licensing.BuildTasks\Metalama.Licensing.BuildTasks.csproj:4` | **`net8.0;net472`** | **Stale.** This project references `Metalama.Backstage` as a `PackageReference` and is on the `topic/2027.0/1829-durable-and-immutable-contracts` branch, yet still declares `net8.0`, which PB-2027.0 dropped. It also carries `System.Formats.Asn1`, `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` package references. |

Because `RollForward=Major` is set on the Worker, the Desktop tray application and the dotnet tool, a `net10.0`
build of each runs on .NET 11 without change. That is the deliberate design and it is why .NET 11 does not force a
target-framework bump on these three.

### 2.3 Preprocessor symbols in this subsystem

The complete list, obtained by grepping `#if`/`#elif` over `Metalama.Backstage/src/**/*.cs`:

| File and line | Symbol | Purpose |
| --- | --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/CryptographyHelper.cs:20` | `NET472 \|\| NET5_0_OR_GREATER` | Chooses `DSA.Create(parameters)` over `DSA.Create()` + `ImportParameters`. |
| `Metalama.Backstage/src/Metalama.Backstage/StringExtensions.cs:5` and `:14` | `NET5_0_OR_GREATER` | `string.Contains(char, StringComparison)` polyfill. |
| `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/ProfilingService.cs:5` | `!WORKER_PROCESS && (NETFRAMEWORK \|\| NET6_0_OR_GREATER)` → defines `PROFILING_ENABLED` | Gates the JetBrains `SelfApi` profiler. Also `:14, :26, :34, :46, :56, :80, :91`. |
| `Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserDeviceDetectionService.cs:9` and `:151` | `NETCOREAPP \|\| NETFRAMEWORK` | Gates `Microsoft.Win32.Registry` use. |
| `Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserInterfaceService.cs:11` and `:68` | `NETFRAMEWORK \|\| NETCOREAPP` | Gates the default-browser lookup through the registry. |
| `Metalama.Backstage/src/Metalama.Backstage/Threading/*.cs` | `METALAMA_BACKSTAGE`, `HAS_METALAMA_TESTING_HOOKS` | Not platform symbols. They mark which *copy* of the shared `Threading/` source files is being compiled; the copies also live in `Metalama.Framework.CompilerExtensions` and `Metalama.Framework.DesignTime.Contracts`. Defined in `Metalama.Backstage.csproj:38-49`. |
| `Metalama.Backstage/src/Metalama.Backstage/Telemetry/TelemetryUploader.cs:465, 478, 483`; `Threading/NamedLockService.cs:564`; `Tools/DevBackstageToolsLocator.cs:12` | `DEBUG` | Configuration, not platform. |

There is **no** `#if NET10_0_OR_GREATER` and no `#if ROSLYN_*` anywhere in the subsystem.

### 2.4 Runtime-version and platform probes at execution time

| File and line | Expression | Comment |
| --- | --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/StandardDirectories.cs:83` | `RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) && Environment.Version < new Version( 8, 0 )` | The only explicit runtime-version comparison in the subsystem. It is dead under PB-2027.0 for the `net10.0` flavour, but the `netstandard2.0` flavour can still be loaded by an old host, so the branch is not removable without an argument. See §5.1. |
| `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:171` | `Environment.OSVersion.Version.Major >= 6 && Process.GetCurrentProcess().SessionId == 0` | Windows unattended detection. |
| `Metalama.Backstage/src/Metalama.Backstage/Utilities/LockingProcessDetector.cs:28` | `RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) && Environment.OSVersion.Version.Major >= 6` | Restart Manager gate. |
| `Metalama.Backstage/src/Metalama.Backstage/Telemetry/ExceptionReporter.cs:735, 737` | `Environment.OSVersion.Version`, `Environment.Version` written into the report | Reporting only. |
| `Metalama.Backstage/src/Metalama.Backstage/Telemetry/UsageSession.cs:85` | `Environment.Version.ToString()` as the `Net.Version` metric | Reporting only. |
| `Metalama.Backstage/src/Metalama.Backstage/Telemetry/LocalExceptionReporter.cs:74-77` | `RuntimeInformation.FrameworkDescription`, `ProcessArchitecture`, `OSDescription`, `OSArchitecture` | Reporting only. |
| `Metalama.Backstage/src/Metalama.Backstage.Commands/Misc/VersionCommand.cs:21-23` | The same three | Diagnostic command. |

### 2.5 Operating-system dispatch points (four of them, each an extension point)

All four are `if`/`else if` chains over `RuntimeInformation.IsOSPlatform`, and three of the four have a silent
fallback. They are the places a fifth operating system, or a change in how a platform reports itself, would land.

1. `Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:152-169`
   — `CreateMachineIdProvider`: Windows → `WindowsMachineIdProvider`, Linux → `LinuxMachineIdProvider`,
   OSX → `MacMachineIdProvider`, otherwise → `MachineNameMachineIdProvider`.
2. `Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:311-322`
   — the user-interface service: Windows → `IdeExtensionStatusService` **and** `WindowsUserInterfaceService`,
   Linux → `LinuxUserInterfaceService`, otherwise → `BrowserBasedUserInterfaceService`. Note that
   `IIdeExtensionStatusService` is registered **only** on Windows.
3. `Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:377-392`
   — `TryAddProcessManagerService`: Windows/Linux/OSX, and `else { // Not supported. }` with no registration at all.
4. `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:274-289`
   — `GetParentProcesses`: Windows/Linux/OSX, `else throw new NotSupportedException`. This is the only one of the
   four that fails loudly.

Further per-platform code paths, each with its own implementation class:

- `Metalama.Backstage/src/Metalama.Backstage/Utilities/ParentProcessSearchWindows.cs` — `NtQueryInformationProcess`,
  `GetProcessImageFileName`, `OpenProcess`, `CloseHandle` (`ntdll`, `kernel32`, `psapi`), lines 22-56.
- `Metalama.Backstage/src/Metalama.Backstage/Utilities/ParentProcessSearchLinux.cs` — `/proc` parsing.
- `Metalama.Backstage/src/Metalama.Backstage/Utilities/ParentProcessSearchMac.cs:28-77` — shells out to
  `ps -o ppid= -o command= <pid>` and parses the output.
- `Metalama.Backstage/src/Metalama.Backstage/Maintenance/WindowsProcessManager.cs`,
  `LinuxProcessManager.cs`, `MacProcessManager.cs`, over the shared
  `Maintenance/ProcessManagerBase.cs`.
- `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/WindowsMachineIdProvider.cs:30-33` — the 32-bit
  registry view of `SOFTWARE\Microsoft\Cryptography\MachineGuid`.
- `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/LinuxMachineIdProvider.cs:22-23` —
  `/etc/machine-id`, then `/var/lib/dbus/machine-id`.
- `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/MacMachineIdProvider.cs:26-27` — `ioreg -rd1 -c
  IOPlatformExpertDevice`, matched with the regular expression at line 30.
- `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:294-354` — `IsRunningInDockerContainer`,
  reading `/proc/1/cgroup` and `/proc/1/environ`.

### 2.6 Native interoperation (P/Invoke), by file

`grep -rn "DllImport\|Marshal\.\|unsafe "` over the production sources yields exactly four files:

| File | Count | Imports |
| --- | --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Threading/MutexAcl.cs` | 12 | `advapi32!ConvertStringSecurityDescriptorToSecurityDescriptorW` (line 213-225), `kernel32!CreateMutexExW` (line 232). Struct `SECURITY_ATTRIBUTES` at line 235. |
| `Metalama.Backstage/src/Metalama.Backstage/Utilities/ParentProcessSearchWindows.cs` | 6 | see above |
| `Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserDeviceDetectionService.cs` | 5 | `User32!GetLastInputInfo` (line 42-43), `user32!EnumDisplayMonitors` (line 47-48), `user32!GetMonitorInfo` (line 50-51) |
| `Metalama.Backstage/src/Metalama.Backstage/Utilities/LockingProcessDetector.cs` | 4 | Windows Restart Manager (`RmStartSession`, `RmRegisterResources`, `RmGetList`, `RmEndSession`) |

None of these uses `LibraryImport` or a source generator, so none is affected by a change in the interop source
generator. All are `DllImport`, which .NET 11 continues to support. The risk here is not language but runtime
marshalling behaviour, which has been stable.

### 2.7 Host integrated development environment sensitivity

| File and line | What it assumes |
| --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:34-138` | The complete `GetProcessKind` table: process names `devenv`, `servicehub.roslyncodeanalysisservice(s)`, `devhub`, `servicehub.host` plus `$codelensservice$` on the command line, `visualstudio`, `csc`, `vbcscompiler`, `resharpertestrunner(64)`, `microsoft.codeanalysis.languageserver`, `microsoft.visualstudio.code.languageserver`, `msbuild`, `testhost`, `dotnet` plus one of `jetbrains.resharper.roslyn.worker` / `jetbrains.roslyn.worker` / `vbcscompiler.dll` / `csc.dll` / `languageserver.dll` / `omnisharp.dll` / `resharpertestrunner.dll` / `msbuild.dll` / `dotnet-format.dll`, and the `linqpad` prefix. **Line 36-37 states that the same logic is duplicated in `Metalama.Framework.CompilerExtensions.ProcessKindHelper` and must be changed in both places.** |
| `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/ProcessKind.cs:9-100` | The enumeration those names map to. |
| `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:68` | `this._runtimeInformation.ProcessKind == ProcessKind.Rider` disables the `DOTNET_HOST_PATH` and `DOTNET_ROOT*` hints, because Rider's bundled .NET ships only the SDKs Rider needs. Issue #1627. |
| `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:39-52` | `HasSdk`: a `dotnet` executable is accepted only if a sibling `sdk` directory exists, because Visual Studio and Rider both bundle a runtime-only `dotnet`. |
| `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:167-200` | On Windows, `%ProgramW6432%` before `%ProgramFiles%`, then the `x64` subdirectory when the process is x64 on an ARM64 operating system. Issue #1745. |
| `Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserDeviceDetectionService.cs:153-166` | Reads `HKLM\SOFTWARE\WOW6432Node\Microsoft\VisualStudio` and returns `true` for any subkey whose name parses as a decimal **`>= 17`**. Under PB-2027.0 the floor is Visual Studio 2026, which is version 18, so this constant is now below the supported floor. It is also not obvious that Visual Studio 2026 still writes under `WOW6432Node`. |
| `Metalama.Backstage/src/Metalama.Backstage/Maintenance/ProcessManagerBase.cs:18-30` | `_processesToKill`: `VBCSCompiler`, `MSBuild`, `servicehub.roslyncodeanalysisservice`, `jetbrains.resharper.roslyn.worker`, `jetbrains.roslyn.worker`, `omnisharp`, `Metalama.Backstage.Worker`, `Metalama.Backstage.Desktop.Windows`. Note that the C# Dev Kit language server is **absent** from this list although it is present in `GetProcessKind`. |

### 2.8 .NET SDK sensitivity

| File and line | What |
| --- | --- |
| `Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:235` | `Path.Combine( _rootDirectory, tool.Name, "bin", _buildConfiguration, "net10.0", "Packed" )` — a **hard-coded target framework name in a path**. This is exactly the line that #1876 had to change (`git show 575be8b88a`), and it will have to change again if the Worker's target framework moves. |
| `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:245-274` | `DOTNET_ROOT_X64`, `DOTNET_ROOT_X86`, `DOTNET_ROOT(x86)`, `DOTNET_ROOT_ARM64`, `DOTNET_ROOT` precedence, taken from the .NET SDK specification. |
| `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/MiniDumper.cs:100-107` | Invokes `<dotnet> dump collect -p <pid> -o <file>`, so it depends on the `dotnet-dump` tool being installed. |
| `Metalama.Backstage/src/Metalama.Backstage.Worker/Metalama.Backstage.Worker.csproj:126` | `<Exec Command="dotnet publish $(ProjectPath) --no-build -o $(OutputPath)Packed -c $(Configuration)" />` inside the `PackAndZip` target, so the packaging step runs the SDK on the build machine. |
| `Metalama.Backstage/src/Metalama.Backstage/Serialization/BackstageJsonContext.cs:24-61` | A `System.Text.Json` **source-generated** context. The generator ships with the SDK, so its output, and the diagnostics it emits, follow the SDK version. Two defects already recorded in this file are consequences of generator behaviour: `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/DiagnosticsConfiguration.cs:24-31` (#1777: the generator treats every `init` property as a constructor parameter and assigns it unconditionally, so an omitted section becomes `null`) and `:39-53` (#1778: a get-only property is serialized but cannot be deserialized). Both are reasons to re-test the configuration round trip after an SDK bump. |
| `Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj:23` | `<PackageReference Include="System.Text.Json" VersionOverride="$(SystemTextJsonMinVersion)" NoWarn="NU1903" />` — the *minimum* version, because the host may provide it. Same rationale as `Directory.Packages.md`. |

---

## 3. Answer to question 3: how the previous wave was absorbed here

### 3.1 The C# 14 wave produced nothing in this subsystem

For each of #1034, #1035, #1036, #1094, #1105, #1108, #1109, #1110, #1111, #1112, #1113, #1114, #1115, #1116,
#1127, #1131, #1143, #1159, #1160, `git log --all --oneline --grep=<n> -- Metalama.Backstage` returns no commit.
The commits those issues produced are all in `Metalama.Framework/src/Metalama.Framework.Engine`,
`Metalama.Framework/src/Metalama.Framework`, and the aspect and template test suites. There is no pattern in this
subsystem to imitate for a *language* change, because none was needed.

### 3.2 The pattern that *does* apply is the platform wave, PB-2027.0 / issue #1876

This is the precedent to follow. The `net8.0` → `net10.0` move landed in two commits and had four distinct kinds
of effect in Backstage:

1. **Target-framework strings in project files.** `git show 575be8b88a` ("Replace the net8.0 target framework by
   net10.0 (#1876)") changed one line in each of seven Backstage project files plus the five Backstage test project
   files, and the `utilities/` pair.
2. **Target-framework strings in code.** The same commit changed exactly one C# line in Backstage:
   `Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:235`. The lesson is that a path
   segment naming a target framework is the one place a target-framework move reaches source code, and it is
   invisible to the compiler.
3. **Package references that were only needed by the old floor.** `git show cf2874353f` ("Do not embed
   System.Threading.AccessControl for .NET (#1876)") removed the explicit `System.Threading.AccessControl`
   reference from three Backstage projects, because the newer runtime carries it in the shared framework.
   The remaining reference is `Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj:27`, needed for
   the `netstandard2.0` and `net472` flavours.
4. **The raised analysis level.** The same commit added to the repository-root `Directory.Build.props`:

   > `AnalysisLevel` defaults to the version of the target framework, so dropping net8.0 in favour of net10.0
   > (issue #1876) raised it from 8.0 to 10.0 and turned on rules that were previously off by default. […]
   > `CodeQuality.targets` sets `TreatWarningsAsErrors` when `ContinuousIntegrationBuild` is true, which would
   > turn a target framework change into a broken build.

   The three rules suppressed were `IDE0270`, `IDE0074` and `IDE0033`. They were then resolved and the suppression
   removed by commit `69f3dcd2d4` ("Resolve what the raised analysis level reports (#1893)"), which touched
   `Metalama.Backstage/src/Metalama.Backstage/Extensibility/ServiceProviderExtensions.cs` and
   `Metalama.Backstage/src/Metalama.Backstage/Telemetry/ExceptionSensitiveDataHelper.cs`.

   **This is the step most likely to repeat verbatim for .NET 11.** The sequence is: bump the target framework,
   observe the new default-on rules, add a temporary `NoWarn` in the root `Directory.Build.props` with a comment
   naming each rule and the issue, then resolve them in a follow-up commit and remove the suppression.

### 3.3 The pattern that applies to the DSA problem, issue #1861

The macOS/.NET 11 DSA failure is issue **#1860**; the mitigation that has already landed on this branch is issue
**#1861**, in four commits, all in `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/`:

| Commit | Title |
| --- | --- |
| `ad0937d4ed` | Add regression test for lazy licensing authority key instantiation (#1861) |
| `8532d10481` | Instantiate the licensing authority keys lazily (#1861) |
| `2de6bfcb2e` | Separate the licensing authority from its provider (#1861) |
| `17e0e8f3c9` | Take an array of keys in the explicit authority provider (#1861) |

The pattern it establishes, and which #1864 will extend:

- **Test first, and observe the attempt rather than the failure.** `ILicensingAuthorityObserver`
  (`Licensing/Licenses/ILicensingAuthorityObserver.cs:164-175`) exists purely so a test can assert that a code path
  creates **no** `DSA` object. Its remarks say why explicitly: *"This observation cannot be replaced by running the
  code path on a platform where finite field DSA is unavailable, because the test suite does not run on such a
  platform."* The observer is called **before** the creation (`OnLicensingAuthorityCreating`) so the attempt is
  observed even where the creation throws.
- **Make the expensive, platform-dependent object lazy.** `LicensingAuthorityProvider`
  (`Licensing/Licenses/LicensingAuthorityProvider.cs:102-146`) holds a
  `Dictionary<byte, Lazy<LicensingAuthority>>` and creates each authority on first use.
- **Separate the key from the provider.** `LicensingAuthority` now takes a `DSA` in an internal constructor
  (`Licensing/Licenses/LicensingAuthority.cs:29-33`) and an XML string in the public one (`:40`), so the choice of
  algorithm is one place.
- **Three providers over one base class**, each naming the host it serves:
  `ProductionLicensingAuthorityProvider` (`:190`, keys 0 and 1, hard-coded `<DSAKeyValue>` XML at `:195` and `:197`),
  `TestLicensingAuthorityProvider` (`:225`, key 255, a per-process `DSA.Create()` at `:232`),
  `ExplicitLicensingAuthorityProvider` (`:196`, used by the license key generator and the licensing web).
- The service registration chooses between the production and test providers at
  `Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:216-219`.

The regression tests are `Metalama.Backstage/src/tests/Metalama.Backstage.Tests/Licensing/Authority/LazyAuthorityCreationTests.cs`
(three facts: `ServiceRegistrationCreatesNoAuthority` at `:50`, `TrialLicenseCreatesNoAuthority` at `:58`, and the
control `SignatureVerificationCreatesAuthority` at `:74` which proves the observer fires at all) and
`.../Authority/TestLicensingAuthorityObserver.cs`.

**What #1861 did not do, and #1864 must.** Laziness only defers the failure. A machine on macOS with .NET 11 that
holds a *signed* production license key still reaches `DSA.Create` and still fails. #1861 bought the unsigned and
trial paths; it did not fix signed keys.

---

## 4. Answer to question 4: the extension points

Restated for this subsystem, since the five categories named in the task are language categories and none of them
applies. The equivalent extension points here are the ones a *platform* change lands on.

### 4.1 A new operating system, or a platform that stops reporting itself the same way

Four dispatch points, listed in §2.5. Three fall back silently, one throws.

### 4.2 A new host process (a new integrated development environment, or a renamed one)

- Add a member to `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/ProcessKind.cs`.
- Add a case to `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:41-138`.
- Add the same case to `Metalama.Framework.CompilerExtensions.ProcessKindHelper`, which is a separate copy
  (`ProcessUtilities.cs:36-37` says so).
- Consider `Metalama.Backstage/src/Metalama.Backstage/Maintenance/ProcessManagerBase.cs:18-30` if the process holds
  file locks.
- Nothing else. The configuration surface picks the new member up automatically:
  `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/DiagnosticsConfiguration.cs:103-105` builds
  `_defaultProcesses` from `Enum.GetValues( typeof(ProcessKind) )`, and `MiniDumper.cs:46`,
  `ProfilingService.cs:53` and `LoggerFactory` look the kind up by `ToString()`.

### 4.3 A new signature algorithm, which is what issue #1864 proposes

The elliptic-curve authority proposed by #1864 lands on exactly these members:

| Member | File and line | Change required |
| --- | --- | --- |
| `LicensingAuthority._key` (`DSA`) | `Licensing/Licenses/LicensingAuthority.cs:27` | Must become an abstraction over `DSA` and `ECDsa`, or the class must gain a sibling. |
| `LicensingAuthority._sha1` | `Licensing/Licenses/LicensingAuthority.cs:24` | `SHA1.Create()`, shared and locked. An elliptic-curve authority would use a modern digest, so `GetHash` at `:47-53` is not reusable as is. |
| `LicensingAuthority.VerifySignature` / `Sign` | `:61-67` and `:72-78` | Both call `DSA.VerifySignature` / `DSA.CreateSignature` under `lock ( this._key )`. |
| `LicensingAuthority(int, string)` | `:40` | Takes the key as `<DSAKeyValue>` XML. An elliptic-curve key is not expressible in that format. |
| `CryptographyHelper.CreateDsaFromXml` and `ParseDsaParameters` | `Licensing/Licenses/CryptographyHelper.cs:16` and `:36-149` | A hand-rolled `DSAKeyValue` XML parser with a `switch` over the node names `P`, `Q`, `G`, `Y`, `J`, `X`, `Seed`, `PgenCounter` (lines 84-131) and a `default: throw` at `:133`. Any new key format needs its own reader. |
| `ILicensingAuthorityProvider.GetAuthority(byte)` | `Licensing/Licenses/ILicensingAuthorityProvider.cs:176` | The provider contract is keyed by a single byte identifier, with no notion of algorithm. A new algorithm can be introduced by allocating a new key identifier, which is the cheapest route. |
| `ProductionLicensingAuthorityProvider._keys` | `Licensing/Licenses/ProductionLicensingAuthorityProvider.cs:192-198` | `Dictionary<byte, string>` with identifiers 0 and 1. A new elliptic-curve public key would be identifier 2. |
| `LicenseKeyData.VerifySignature` | `Licensing/Licenses/LicenseKeyData.Validation.cs:46-68` | Dispatches on `this.SignatureKeyId`, so the key identifier already carries the algorithm implicitly. |
| `License.TryGetConsumptionProperties` | `Licensing/Licenses/License.cs:99-106` | Contains `licenseKeyData.SignatureKeyId is 0 or 1 && (…)` — a **hard-coded key-identifier set** used for the revocation check. A new key identifier 2 would bypass this test silently. See §5.4. |

### 4.4 A new configuration file or a new configuration section

`Metalama.Backstage/src/Metalama.Backstage/Serialization/BackstageJsonContext.cs:25-60` must gain a
`[JsonSerializable]` attribute, and the type must be a `record` deriving from `ConfigurationFile` with a
`[ConfigurationFile]` attribute. Every `init` property that holds a nested section must normalize `null` in its
initializer, for the reason documented at `Diagnostics/DiagnosticsConfiguration.cs:24-31`.

### 4.5 A new license key field

Not a language matter, but it is the subsystem's own versioned vocabulary and it is worth recording because it is
the structural analogue of a grammar extension.

- `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/LicenseFields/LicenseFieldIndex.cs:12-42` — the
  `byte`-valued enumeration.
- `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/LicenseFields/LicenseFieldsExtensions.cs:23-33` —
  `IsMustUnderstand`: `i is <= 128 or >= 254`. Indices 129 to 253 may be unknown to a reader.
- `LicenseFieldsExtensions.cs:47-57` — `IsPrefixedByLength`: `i is > 21 and < 254`. A length-prefixed field can be
  skipped by a reader that does not know it.
- `LicenseKeyData.Validation.cs:31-39` — an unknown *must-understand* field rejects the key with
  `"the license key contains unknown must-understand fields"`.
- `LicenseKeyDataSerializer.CurrentVersion = 2` (`LicenseKeyDataSerializer.cs:12`).

This design is a genuine forward-compatibility mechanism, and it is the model the C# 15 work in other subsystems
might reasonably be compared against: a reader that meets something it does not know either skips it explicitly or
refuses the whole input, and the choice is encoded in the identifier itself rather than left to the reader.

---

## 5. Answer to question 5: where the subsystem would silently do the wrong thing

Ordered by consequence.

### 5.1 Signature verification catches the wrong exception type — this is issue #1860

`Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/LicenseKeyData.Validation.cs:46-68`:

```csharp
public bool VerifySignature( ILicensingAuthorityProvider licensingAuthorityProvider )
{
    try { … return licensingAuthorityProvider.GetAuthority( … ).VerifySignature( buffer, this.Signature ); }
    catch ( CryptographicException ) { return false; }
}
```

`DSA.Create` on macOS with .NET 11 throws `PlatformNotSupportedException`, which derives from
`NotSupportedException` and **not** from `CryptographicException`. The exception therefore escapes
`VerifySignature`, escapes `License.TryGetConsumptionProperties`
(`Licensing/Licenses/License.cs:108`, which has no `catch`), escapes `LicenseConsumptionService` (no `catch`
anywhere in `Licensing/Consumption/*.cs`), and reaches the caller. This is a loud failure, not a silent one, which
is the better of the two outcomes; but the shape of the `catch` invites the wrong fix. Widening the `catch` to
`NotSupportedException` would convert "this platform cannot verify signatures" into "this license key has an
invalid signature", which is the *silent* wrong answer: every paying customer on macOS would be told their key is
forged. The correct handling has to distinguish the two, and the distinction has to be made where the authority is
created, not where the signature is checked.

Related dead branch: `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/StandardDirectories.cs:83`
compares `Environment.Version < new Version( 8, 0 )` on macOS. Under PB-2027.0 no supported runtime satisfies it,
so the `osxForwardCompatibleApplicationDataDirectory` migration path at lines 98-124 can no longer be reached from
the `net10.0` flavour. It remains reachable from the `netstandard2.0` flavour if that is ever loaded on an old
runtime. If the branch is removed without that argument, a user upgrading from a very old Metalama on macOS loses
the directory migration silently.

### 5.2 The setup web server token permission is applied reflectively and failure is swallowed

`Metalama.Backstage/src/Metalama.Backstage/UserInterface/SetupWebServerToken.cs:146-165`:

```csharp
var unixFileModeType = Type.GetType( "System.IO.UnixFileMode, System.Runtime" );
var setUnixFileMode = unixFileModeType == null ? null : typeof(File).GetMethod( "SetUnixFileMode", … );
setUnixFileMode?.Invoke( null, new[] { path, Enum.ToObject( unixFileModeType!, _ownerReadWriteUnixFileMode ) } );
```

Both the type lookup and the method lookup are null-tolerant, and the invocation is null-conditional. If .NET 11
moves `System.IO.UnixFileMode` to a different assembly, changes the overload set of `File.SetUnixFileMode`, or the
assembly is trimmed, the permissions are simply **not applied**, no warning is logged, and the token file that
authenticates the local setup web server stays readable by every local user. That is precisely the exposure the
token was introduced to close (issue #1769, documented at
`Metalama.Backstage.Worker/Worker/WebServer/SetupWebServerAuthentication.cs:18-24`). The remarks at
`SetupWebServerToken.cs:142-145` explain why reflection is used (the assembly also targets `netstandard2.0`), but
there is no assertion, no log, and no test that the call actually happened.

### 5.3 The named-lock service degrades to a process-local lock without failing

`Metalama.Backstage/src/Metalama.Backstage/Threading/NamedLockService.cs`:

- `IsMachineWideRefusal` at `:432` treats `IOException`, `PlatformNotSupportedException` and
  `NotSupportedException` as "this machine cannot provide named objects at all", and latches
  `_areNamedObjectsUnavailable` for the lifetime of the process.
- The catch-all at `:397`, `catch ( Exception e ) when ( !IsCallerDefect( e ) )`, degrades on anything else.
- `ReportDegraded` falls back to a **static** `ConcurrentDictionary<string, SemaphoreSlim>` (`:93`), and the
  remarks at `:82-92` state plainly that this dictionary is per-assembly-copy, so two loaded copies of the file do
  not exclude each other, and cross-process exclusion is lost entirely.

This is a deliberate, documented degradation and the alternative (failing the compilation) is worse. But if .NET 11
changes the exception a named mutex raises on any platform, the product will keep building while silently losing
cross-process exclusion over the configuration files, the tool extraction directory and the crash-dump directory.
The only signal is a `LockEventReported` event routed to the log at
`Extensibility/RegisterServiceExtensions.cs:378-395`, and its filter suppresses it unless tracing is on.

### 5.4 The revocation check is keyed to signature key identifiers 0 and 1

`Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/License.cs:99-106`:

```csharp
if ( licenseKeyData.SignatureKeyId is 0 or 1
     && (licenseKeyData is { LicenseId: not 0 and not 22 and < 100 } || RevokedLicenseKeys.Ids.Contains( … )) )
```

A key signed by a new authority (identifier 2, which is what #1864 would introduce) skips the revocation list
entirely and is accepted. Nothing reports it. This is the clearest silent-wrong-answer risk that the elliptic-curve
work would introduce if the condition is not revisited at the same time.

### 5.5 The process manager is not registered on an unrecognized platform

`Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:377-392` ends with
`else { // Not supported. }`. `IProcessManager` is then absent from the service provider, and every caller resolves
it with `GetBackstageService<…>()?` rather than `GetRequiredBackstageService`. The cleanup commands report success
having killed nothing.

The same shape, with a less severe consequence, at `:311-322`: `IIdeExtensionStatusService` is registered only on
Windows, and `ToastNotificationDetectionService.cs:90` reads it through `?.`, so on a non-Windows machine the
recommendation is simply never made.

### 5.6 The machine identifier falls back to the machine name without distinguishing "unavailable" from "unknown"

`Metalama.Backstage/src/Metalama.Backstage/Infrastructure/MachineIdProvider.cs:48-70` catches every exception,
logs a warning, and returns `Environment.MachineName`. The value then feeds the cross-product device hash used by
the license audit (`Licensing/Audit/LicenseAuditTelemetryReport.cs:63`). A change in how .NET 11 exposes the
registry on Windows, or a Linux image without `/etc/machine-id`, therefore produces a *different but plausible*
device hash rather than an error, and the device count silently changes. The comment at `:67-68` acknowledges that
the resulting count is a lower bound.

### 5.7 The Windows detection heuristics return `null`, which the callers read as "yes"

`Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserDeviceDetectionService.cs:125-145`:

```csharp
var hasRecentUserInput = GetLastInputTime() is null or { TotalMinutes: < 15 };
var hasLargeMonitor    = GetTotalMonitorWidth() is null or >= 1280;
```

Both helpers (`:71-99` and `:102-123`) have a bare `catch { return null; }`. If the `user32` calls start failing,
the device is classified as interactive and the product opens a browser window or a toast on a machine that has no
user. The `null`-means-yes convention is deliberate but it converts a detection failure into a user-visible action.

### 5.8 The Windows tool zip is extracted on every platform

`Metalama.Backstage/src/Metalama.Backstage.Tools/BackstageToolsExtractor.cs:58-62` — `ExtractAll` extracts both
`BackstageTool.Worker` and `BackstageTool.DesktopWindows` unconditionally. On Linux and macOS the WPF payload is
written to disk and never used. Not a correctness defect, but it means a failure to extract the Windows tool fails
the whole extraction on a platform that does not need it.

### 5.9 Package-version drift in the premium licensing build task

`C:\src\Metalama-2027.0\Metalama.Premium\src\Metalama.Licensing.BuildTasks\Metalama.Licensing.BuildTasks.csproj:4`
declares `net8.0;net472` on a 2027.0 branch. It is an MSBuild task, so a host below the .NET 10 SDK floor would
load the `net8.0` asset and never report that it is below the baseline. This is the same class of defect that
`platform-support.md` records for `buildTransitive/Metalama.Compiler.Sdk.props` in the compiler repository.

---

## 6. Checklist for the .NET 11 work in this subsystem

1. Decide whether any Backstage project must move to `net11.0`. On the evidence, **none must**: the Worker, the
   Desktop tray application and the dotnet tool all declare `RollForward=Major`, and `Metalama.Backstage` itself
   is a library whose `net10.0` asset loads on .NET 11. Rule 8 of the doctrine (an axis enters the matrix only if
   a shipped asset depends on it) argues against adding a `net11.0` asset.
2. If a target framework does move, change
   `Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:235` in the same commit.
3. Install the .NET 11 SDK and build with `-p:ContinuousIntegrationBuild=True`. Expect new default-on analyzer
   rules from the raised `AnalysisLevel` and from `LangVersion=latest` moving to C# 15. Follow the #1876 pattern:
   temporary `NoWarn` in the repository-root `Directory.Build.props` with a comment naming each rule, then a
   follow-up commit resolving them, as #1893 did.
4. Re-run the configuration round-trip tests after the SDK bump, because `BackstageJsonContext` is
   source-generated and two of its recorded defects (#1777, #1778) were generator-behaviour defects.
5. For #1860 and #1864, treat `LicensingAuthority`, `CryptographyHelper` and the `SignatureKeyId is 0 or 1`
   condition in `License.cs:99` as one unit. Extend `ILicensingAuthorityObserver` coverage to the new authority so
   the laziness guarantee of #1861 is not lost.
6. Re-measure `IsVisualStudioInstalled` (`WindowsUserDeviceDetectionService.cs:153-166`) against a Visual Studio
   2026 installation. The `>= 17` constant and the `WOW6432Node` path both predate the supported floor.
