# .NET 11 — Runtime and Base Class Library research notes

Research date: 2026-09-03. .NET 11 is at Preview 7 (11.0.0-preview.7, released 2026-08-11).
All statements below were verified against the primary sources listed with each item.

Primary sources used:

- `https://learn.microsoft.com/en-us/dotnet/core/compatibility/11` (breaking-change index; page `ms.date` 2026-06-04, `updated_at` 2026-08-28). Note: the canonical URL is `/compatibility/11`, **not** `/compatibility/11.0` (that returns HTTP 404).
- Individual breaking-change pages under `https://learn.microsoft.com/en-us/dotnet/core/compatibility/<area>/11/<slug>`.
- `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/{overview,runtime,libraries,sdk}` (last updated for Preview 7).
- `https://github.com/dotnet/core/blob/main/release-notes/11.0/README.md`, `.../supported-os.md`, `https://github.com/dotnet/core/blob/main/release-policies.md`.
- `https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview{1..7}/{libraries,runtime,sdk}.md`.

**Caveat carried by Microsoft itself**: the breaking-changes index page states, verbatim, "This article is a work in
progress. It's not a complete list of breaking changes in .NET 11." Additional breaking changes may land between
Preview 7 and GA.

---

## 1. Support calendar

| Fact | Value | Source |
| --- | --- | --- |
| General availability | **2026-11-10** | `release-notes/11.0/README.md` |
| Support level | **Standard Term Support (STS)** | `release-notes/11.0/README.md`, `supported-os.md` |
| Support duration | **Two years** | `release-notes/11.0/README.md` |
| End of support | **2028-11-09** | `release-notes/11.0/README.md` |

Exact sentence from `release-notes/11.0/README.md`:

> .NET 11 is a Standard Term Support (STS) release and will be supported for two years, from November 10, 2026 to
> November 9, 2028, on multiple operating systems.

`release-policies.md` confirms the current policy shape (this is a change from the historical 18-month STS window):

- STS releases are supported for **two years** and are released in **even-numbered** years.
- LTS releases are supported for **three years** and are released in **odd-numbered** years.
- Support phases: Preview (unsupported) → Go-Live (RC, supported in production) → Active → Maintenance (last six
  months, security fixes only) → End of life.
- Six months after a version goes out of support, newer SDKs emit `NETSDK1138` ("The target framework is out of
  support") when a project targets it.

Preview release dates (from `release-notes/11.0/README.md`):

| Date | Version |
| --- | --- |
| 2026-02-10 | 11.0.0 Preview 1 |
| 2026-03-10 | 11.0.0 Preview 2 |
| 2026-04-14 | 11.0.0-preview.3 |
| 2026-05-12 | 11.0.0-preview.4 |
| 2026-06-09 | 11.0.0-preview.5 |
| 2026-07-14 | 11.0.0-preview.6 |
| 2026-08-11 | 11.0.0-preview.7 |

### Supported operating systems (`release-notes/11.0/supported-os.md`, last updated 2026-07-13)

- **Windows**: Windows 11 26H1 / 25H2 / 24H2 (incl. IoT and E) / 23H2 (E); Windows 10 21H2 (E), 21H2 (IoT),
  1809 (E), 1607 (E). Architectures Arm64, x64, x86. Windows Server 2025, 23H2, 2022, 2019, 2016 (x64);
  Server Core 2025/2022/2019/2016; Nano Server 2025/2022/2019.
- **macOS**: 26, 15, 14 (Arm64, x64). Rosetta 2 x64 emulation supported on Arm64.
- **Linux**: Alpine 3.23/3.22; Azure Linux 3.0; CentOS Stream 10/9; Debian 13; Fedora 44/43; openSUSE Leap 16.0;
  RHEL 10/9/8; SLES 16.0/15.7; Ubuntu 26.04/25.10/24.04/22.04.
- **Android**: 16, 15, 14; **API 24 is the minimum SDK target** (this is the .NET MAUI breaking change
  "Minimum Android API level raised to 24").
- **Apple mobile**: iOS 26/18, iPadOS 26/18, tvOS 26; iOS 12.2 is the minimum SDK target.

---

## 2. Complete breaking-change index for .NET 11

Reproduced verbatim from `https://learn.microsoft.com/en-us/dotnet/core/compatibility/11`.

### ASP.NET Core

Delegated to `https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/overview` (not covered in these notes).

### Core .NET libraries

| Title | Type |
| --- | --- |
| Assembly.GetCallingAssembly behavior changes when stack trace support is disabled | Behavioral |
| CborReader and CborWriter enforce a default maximum nesting depth | Behavioral |
| Complex special-value results now follow C23 Annex G | Behavioral |
| CRC32 validation added when reading ZIP archive entries | Behavioral |
| DateOnly and TimeOnly TryParse methods throw for invalid input | Behavioral |
| Decimal and BigInteger floating-point conversions are correctly rounded | Behavioral |
| DeflateStream and GZipStream write headers and footers for empty payload | Behavioral |
| Environment.TickCount made consistent with Windows timeout behavior | Behavioral |
| Math.Round and MathF.Round return correctly rounded results | Behavioral |
| NamedPipeServerStream with PipeOptions.CurrentUserOnly tightens Unix socket file permissions | Behavioral |
| Nullable.GetUnderlyingType throws for custom Type subclasses | Behavioral |
| PackagePart.GetStream() returns a non-seekable stream for compressed parts in ReadWrite packages | Behavioral |
| API obsoletions with non-default diagnostic IDs (.NET 11) | Source incompatible |
| SafeFileHandle.IsAsync and FileStream.IsAsync accurately reflect non-blocking state on Unix | Behavioral |
| TAR-reading APIs verify header checksums when reading | Behavioral |
| TarWriter uses HardLink entries for hard-linked files | Behavioral |
| ZipArchive.CreateAsync eagerly loads ZIP archive entries | Behavioral |

### Cryptography

| Title | Type |
| --- | --- |
| API obsoletions | Source incompatible |
| Composite ML-DSA on Windows uses native implementation | Behavioral |
| DSA removed from macOS | Behavioral |
| Linux AIA certificate fetching limited to two fetches per chain build | Behavioral |

### Deployment

| Title | Type |
| --- | --- |
| configProperties in .runtimeconfig.dev.json override .runtimeconfig.json | Behavioral |

### Extensions

| Title | Type |
| --- | --- |
| ChangeToken.OnChange async overloads rebind existing Task-returning callbacks | Behavioral |
| FileConfigurationProvider doesn't raise reload token after ignored load failure | Behavioral |
| FileConfigurationSource.OnLoadException callback is called for IO errors | Behavioral |
| IHost.RunAsync and IHost.StopAsync throw when a BackgroundService fails | Behavioral |
| Some Microsoft.Extensions packages included in shared framework | Behavioral |

### Globalization

| Title | Type |
| --- | --- |
| Japanese Calendar minimum supported date corrected | Behavioral |

### Interop

| Title | Type |
| --- | --- |
| NativeAOT uses lib prefix for native library outputs on Unix | Behavioral |

### JIT compiler

| Title | Type |
| --- | --- |
| Minimum hardware requirements updated | Behavioral |

### Networking

| Title | Type |
| --- | --- |
| SslStream server-side AIA certificate downloads disabled by default | Behavioral |

### .NET MAUI

| Title | Type |
| --- | --- |
| Minimum Android API level raised to 24 | Behavioral |

### SDK and MSBuild

| Title | Type |
| --- | --- |
| dnx scripts bypass global.json SDK selection | Behavioral |
| mono launch target not set for .NET Framework apps | Behavioral |
| NativeAOT CLI command handling enabled by default | Behavioral |
| NU1703 warns for packages that use deprecated MonoAndroid framework assets | Source incompatible |
| NuGet pack warns for package IDs with restricted characters | Behavioral |
| SDK local container runtime selection prefers platform-native tools | Behavioral |
| Template engine packages no longer support netstandard2.0 | Binary/source incompatible |
| VSTest removes dependency on Newtonsoft.Json | Binary/source incompatible |

---

## 3. Runtime breaking changes

### 3.1 Minimum hardware requirements updated (Preview 1) — JIT / whole runtime

Source: `/compatibility/jit/11/minimum-hardware-requirements`, corroborated by `/whats-new/dotnet-11/runtime`.

**x86/x64 — all operating systems**: the JIT/AOT baseline moves from `x86-64-v1` to **`x86-64-v2`**.

- Previously guaranteed: `CMOV`, `CX8`, `SSE`, `SSE2`.
- Now additionally guaranteed: `CX16`, `POPCNT`, `SSE3`, `SSSE3`, `SSE4.1`, **`SSE4.2`**.
- ReadyToRun target moves from `x86-64-v2` to **`x86-64-v3`** on Windows and Linux (adds `AVX`, `AVX2`, `BMI1`,
  `BMI2`, `F16C`, `FMA`, `LZCNT`, `MOVBE`). Apple x64 R2R target is unchanged at `x86-64-v2`.

| OS | Previous JIT/AOT min | New JIT/AOT min | Previous R2R target | New R2R target |
| --- | --- | --- | --- | --- |
| Apple | x86-64-v1 | x86-64-v2 | x86-64-v2 | (no change) |
| Linux | x86-64-v1 | x86-64-v2 | x86-64-v2 | x86-64-v3 |
| Windows | x86-64-v1 | x86-64-v2 | x86-64-v2 | x86-64-v3 |

**Arm64** — the two primary sources disagree on Windows, and both were updated in August 2026:

| OS | Previous JIT/AOT min | New JIT/AOT min | Previous R2R target | New R2R target |
| --- | --- | --- | --- | --- |
| Apple | Apple M1 | (no change) | Apple M1 | (no change) |
| Linux | armv8.0-a | (no change) | armv8.0-a | armv8.0-a + LSE |
| Windows | armv8.0-a | see conflict below | armv8.0-a | armv8.2-a + RCPC |

Conflict, reported here rather than resolved:

- The **breaking-change page** (`/compatibility/jit/11/minimum-hardware-requirements`, `ms.date` 2026-08-15,
  `updated_at` 2026-08-19) says: "For Windows, there's no change to the minimum hardware. .NET continues to support
  `armv8.0-a` devices, including Windows 10 IoT devices that don't provide the `LSE` instruction set", and its table
  shows Windows Arm64 JIT/AOT minimum as "(No change)".
- The **what's-new runtime page** (`/whats-new/dotnet-11/runtime`, `ms.date` 2026-08-15, `updated_at` 2026-08-19)
  says: "For Windows, the baseline is updated to require the `LSE` instruction set", and its table shows the new
  Windows Arm64 JIT/AOT minimum as `armv8.0-a + LSE`.

Both pages carry the same `ms.date`/`updated_at`, so neither is demonstrably more recent. The breaking-change page is
the normative compatibility document and also gives the reason ("The Arm64 minimum baseline remains unchanged so that
.NET continues to support hardware that's supported by Windows 10 IoT"), which reads as the deliberate decision. See
open questions.

**Failure mode on unsupported hardware** (verbatim message): "The current CPU is missing one or more of the baseline
instruction sets." ReadyToRun images that do not meet the new R2R target still run, but fall back to JIT compilation,
which adds startup overhead.

### 3.2 configProperties in `.runtimeconfig.dev.json` override `.runtimeconfig.json` (Preview 6) — Deployment

Source: `/compatibility/deployment/11/runtimeconfigdev-configproperties-precedence`; `dotnet/runtime#126606`.

Precedence is reversed. Previously `<app>.runtimeconfig.json` won for a duplicate key under `runtimeOptions.configProperties`;
now `<app>.runtimeconfig.dev.json` wins. Example given in the doc uses `System.GC.Concurrent`. Affected APIs: none
(host behavior only).

This matters for any tool whose behavior is driven by `AppContext` switches written into `runtimeconfig`: a stale
`.runtimeconfig.dev.json` in an output directory now silently overrides production settings.

### 3.3 NativeAOT uses `lib` prefix for native library outputs on Unix (Preview 3) — Interop

Source: `/compatibility/interop/11/nativeaot-lib-prefix`; `dotnet/runtime#124611`.

Non-executable NativeAOT outputs on Unix are now named `libmylib.so`, `libmylib.dylib`, `libmylib.a` rather than
`mylib.so` / `mylib.a`. Opt out with the new MSBuild property:

```xml
<PropertyGroup>
  <UseNativeLibPrefix>false</UseNativeLibPrefix>
</PropertyGroup>
```

### 3.4 In-process crash report logging (mobile only)

Source: `/whats-new/dotnet-11/runtime`. A new in-process crash-reporting path logs the managed stack trace, module
list and key runtime state to a well-known path before the process exits, replacing (on mobile platforms) the
out-of-process monitor for that information.

### 3.5 More than 1024 logical processors

Source: `/whats-new/dotnet-11/runtime`; `dotnet/runtime#126763`. The runtime previously failed to initialize on
machines with more than 1024 logical processors because `sched_getaffinity` was called with the default `cpu_set_t`.
The CPU set is now allocated dynamically. The GC retains a 1024-**heap** limit; the internal constant
`MAX_SUPPORTED_CPUS` was renamed `MAX_SUPPORTED_HEAPS`.

---

## 4. Runtime features (non-breaking) relevant to a compiler-adjacent tool

### 4.1 Runtime Async (Runtime Async V2) — preview feature

Source: `/whats-new/dotnet-11/runtime`.

- Opt in per project with `<Features>runtime-async=on</Features>`. A `net11.0` project **no longer requires**
  `<EnablePreviewFeatures>true</EnablePreviewFeatures>`.
- Opt out per project with `<UseRuntimeAsync>false</UseRuntimeAsync>`.
- The `DOTNET_RuntimeAsync` and `UNSUPPORTED_RuntimeAsync` environment variables **have been removed**.
- **The .NET 11 runtime libraries themselves are compiled with `runtime-async=on`.** They no longer contain
  compiler-generated async state machines.
- Consequence for anything that reads stack traces: with runtime-async, an async call chain produces one frame per
  method instead of three (method + `AsyncMethodBuilderCore.Start<TStateMachine>` + method). The documented example
  goes from 13 frames to 5. Exception stack traces (`catch (Exception ex)`) look the same either way; the change is
  in *live* stack traces seen by `new StackTrace()`, profilers and debuggers.
- Covariant `Task` → `Task<T>` overrides: the runtime generates a void-returning thunk that bridges the calling
  convention difference so virtual dispatch works for both flavors, on CoreCLR and NativeAOT.
- Runtime-async methods can now be inlined during ReadyToRun (crossgen2) compilation.
- Async continuations can opt out of `ExecutionContext` capture/restore when there is nothing to restore.
  Applies to `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and the runtime-async path.
- The async profiler now instruments both runtime-async methods and compiler-generated async state machines, so
  diagnostic tools see one consistent event model.

### 4.2 Devirtualization and JIT changes with observable semantics

Source: `/whats-new/dotnet-11/runtime`.

- Generic virtual methods can now be devirtualized (including non-shared GVMs and shared GVMs that do not need a
  runtime lookup). Default interface methods on generic interfaces can be devirtualized.
- `Activator.CreateInstance<T>()` results are now treated as having an exact type.
- **Saturating floating-point conversions**: unchecked `float`/`double` conversions to small integer types now
  saturate to the type bounds instead of wrapping through an intermediate truncation. (Listed under JIT improvements,
  not as a breaking change, but it is an observable semantic change for out-of-range conversions.)
- `string.Equals` / `ReadOnlySpan<T>.SequenceEqual` on two compile-time constants fold to a constant `true`/`false`.
  Applies to string literals, `const string` fields, and UTF-8 literals (`"PNG"u8`).
- Bounds-check elimination for `i + cns < len`, for index-from-end (`values[^1]`), and after an `IsEmpty` guard.
- Redundant `checked` context removal; switch-expression folding for small constant sets; `SELECT(cond, cns, cns)`
  folding; redundant branch and test elimination.
- `Math.BigMul(long, long, out long)` now emits a single `MUL r/m64` on x64.
- New intrinsics: Arm SVE2 `ShiftRightLogicalNarrowingSaturate(Even|Odd)`;
  `System.Runtime.Intrinsics.X86.AvxVnni.V512` (512-bit AVX-VNNI multiply-add on `AVX512-VNNI` hardware).
- New instruction-set detection: `SVE_AES`, `SVE_SHA3`, `SVE_SM4`, `SHA3`, `SM4` are reported separately, so
  `Sve2*.IsSupported` works for them. An AVX10 CPUID-cache bug that could misreport AVX10 is fixed.
- ARM64 with SVE: `Vector<T>` values are passed **by reference** rather than by value, matching the ARM calling
  convention for scalable types.
- F16C acceleration for `Half` ↔ `float`/`double` conversions on x64.

### 4.3 ReadyToRun and NativeAOT

- `Comparer<T>.Default` and `EqualityComparer<T>.Default` are now specialized in R2R images (previously they fell
  back to JIT because of reflection R2R could not see). Reported up to 20× improvement for collection operations.
- NativeAOT interface dispatch now routes through a shared, patchable dispatch helper instead of a direct fat-pointer
  call sequence. Generic virtual method dispatch uses the same shared dispatch-cell infrastructure.
- Cached interface dispatch on non-JIT platforms (for example iOS): up to 200× improvement in interface-heavy code.
- `Assembly.GetCallingAssembly()` now works on NativeAOT when stack trace data is available (see the breaking change
  in §5.1).

### 4.4 WebAssembly

- CoreCLR on WebAssembly runs the libraries test suite end to end.
- WebCIL V1 is the default for CoreCLR WASM builds. The shared WebCIL header gains a `TableBase` field
  (28 → 32 bytes). Both Mono and CoreCLR readers accept V0 and V1. Crossgen2's `WasmObjectWriter` emits V1;
  CoreCLR-flavored WASM SDK builds default `WasmWebcilVersion` to `V1`.
- Native re-link works for CoreCLR WASM apps (full Emscripten pipeline; `NativeFileReference` works).
- NativeAOT publish for WASM no longer drops satellite assemblies coming from NuGet packages.

### 4.5 GC

- `DOTNET_GCTrimYoungestKeepPercent`: new GC configuration switch that lets the memory-footprint latency mode
  (`DOTNET_GCLatencyLevel=0`) keep a configurable percentage of the youngest generation during trimming
  (`dotnet/runtime#109863`, Preview 5).
- GC heap hard limit support is now available for 32-bit processes (`dotnet/runtime#101024`, Preview 1).
- GC compaction keeps the `heap_segment_used` watermark accurate after relocating objects into a region
  (`dotnet/runtime#128217`).

---

## 5. Core library breaking changes, in detail

### 5.1 `Assembly.GetCallingAssembly` behavior changes when stack trace support is disabled (Preview 7)

Source: `/compatibility/core-libraries/11/assembly-getcallingassembly-stacktracesupport-disabled`; `dotnet/runtime#129963`.

- Previously: on NativeAOT, `Assembly.GetCallingAssembly()` always threw `PlatformNotSupportedException`. On CoreCLR
  it returned the calling assembly even when the `StackTraceSupport` feature switch was `false`.
- Now: on NativeAOT it returns the calling assembly by inspecting stack trace data. On **both** NativeAOT and CoreCLR
  it throws `NotSupportedException` when `StackTraceSupport` is `false`.
- Exact exception message: "Unable to retrieve stack trace information when StackTraceSupport feature switch is set
  to false."
- Mitigations: set `StackTraceSupport` to `true` (or remove the switch); stop calling `GetCallingAssembly`; or catch
  `NotSupportedException`.

### 5.2 `Nullable.GetUnderlyingType` throws for custom `Type` subclasses (Preview 4)

Source: `/compatibility/core-libraries/11/nullable-getunderlyingtype-throws`; `dotnet/runtime#126905`,
`dotnet/runtime#124216`. This is the single most relevant BCL change for a reflection-model implementer.

- New **virtual** method on `System.Type`:

  ```csharp
  public virtual Type? GetNullableUnderlyingType();
  ```

- `Nullable.GetUnderlyingType(Type)` now forwards to it. The base `System.Type` implementation **throws
  `NotSupportedException`** with the message "Derived classes must provide an implementation."
- Previously `Nullable.GetUnderlyingType` hard-coded a comparison against the executing runtime's
  `typeof(Nullable<>)`, so it returned `null` for `Type` instances from another reflection universe. Most notably,
  `MetadataLoadContext` always reported `Nullable<T>` as non-nullable.
- Types shipped with .NET that override the new virtual and are therefore unaffected: the runtime `Type`
  implementation, `TypeDelegator`, `TypeBuilder`, `EnumBuilder`, `GenericTypeParameterBuilder`,
  `TypeBuilderInstantiation`, `SymbolType`, `ModifiedType`, the `SignatureType` family, and the
  `MetadataLoadContext` types.
- **Any custom `System.Type` subclass must add an override.** Recommended implementations from the doc:

  ```csharp
  // Never represents Nullable<T>:
  public override Type? GetNullableUnderlyingType() => null;

  // Constructed generic types:
  public override Type? GetNullableUnderlyingType()
  {
      if (IsGenericType && !IsGenericTypeDefinition
          && GetGenericTypeDefinition() == typeof(Nullable<>))
      {
          return GetGenericArguments()[0];
      }
      return null;
  }

  // Delegating wrapper:
  public override Type? GetNullableUnderlyingType() => _innerType.GetNullableUnderlyingType();
  ```

- There is **no** AppContext switch or configuration to revert.
- Compatibility detail from the Preview 5 release notes: for the *open* generic `Nullable<>`,
  `typeof(Nullable<>).GetNullableUnderlyingType()` returns the generic parameter, while
  `Nullable.GetUnderlyingType(typeof(Nullable<>))` still returns `null` for compatibility.

### 5.3 `CborReader` / `CborWriter` enforce a default maximum nesting depth (Preview 5)

Source: `/compatibility/core-libraries/11/cbor-max-depth`.

- `CborReader` throws `CborContentException` when reading a container (array, map, or indefinite-length string) that
  would exceed the maximum depth. **Default 64.**
- `CborWriter` throws `InvalidOperationException` when writing a container beyond the maximum depth.
  **Default 1000.**
- New options types: `CborReaderOptions { MaxDepth = … }`, `CborWriterOptions { MaxDepth = … }`. Unlike
  `Utf8JsonReader`/`Utf8JsonWriter`, `MaxDepth = 0` means *no nesting allowed*; use `MaxDepth = -1` or omit the
  property for the runtime default.
- No AppContext switch restores unlimited depth.
- Affected APIs: `CborReader.ReadStartArray()`, `ReadStartMap()`, `ReadStartIndefiniteLengthByteString()`,
  `ReadStartIndefiniteLengthTextString()`; `CborWriter.WriteStartArray(int?)`, `WriteStartMap(int?)`,
  `WriteStartIndefiniteLengthByteString()`, `WriteStartIndefiniteLengthTextString()`.

### 5.4 `System.Numerics.Complex` follows C23 Annex G special values (Preview 7)

Source: `/compatibility/core-libraries/11/complex-annex-g-special-values`; `dotnet/runtime#131132`.

`Complex` now delegates most of its implementation to the new generic `Complex<double>`, and inherits its
C23 Annex G (IEC 60559-compatible complex arithmetic) special-value behavior for signed zeros, infinities and NaNs.

Examples from the doc:

| Expression | Old result | New result |
| --- | --- | --- |
| `Complex.Atan(new Complex(double.PositiveInfinity, 1.0))` | `(NaN, NaN)` | `(π/2, 0)` |
| `Complex.Acos(new Complex(double.NegativeInfinity, double.NaN))` | `(NaN, NaN)` | `(NaN, +∞)` |
| `Complex.Cosh(new Complex(double.PositiveInfinity, double.PositiveInfinity))` | `(NaN, NaN)` | `(+∞, NaN)` |
| `new Complex(+∞, +∞) * new Complex(1.0, 0.0)` | `(NaN, NaN)` | `(+∞, +∞)` |

Division by a zero divisor yields a directed infinity, or NaN for `0/0`. The change spans `operator *`, `operator /`,
`Multiply`, `Divide`, `Reciprocal`, `Abs`, `Pow`, `Sqrt`, `Exp`, `Log`, `Log10`, and the trigonometric, hyperbolic
and inverse-trigonometric functions. Annex G leaves the sign of a zero-valued quotient component unspecified, so that
may differ too. No compatibility switch.

### 5.5 CRC32 validation when reading ZIP archive entries (Preview 3)

Source: `/compatibility/core-libraries/11/ziparchive-entry-crc32-validation`; `dotnet/runtime#124766`.
`System.IO.Compression` now verifies the CRC32 of a ZIP entry while reading and throws
`System.IO.InvalidDataException` on mismatch. Affected API: `ZipArchiveEntry.Open()`.

### 5.6 `DateOnly` / `TimeOnly` `TryParse` and `TryParseExact` throw for invalid input (Preview 2)

Source: `/compatibility/core-libraries/11/dateonly-timeonly-tryparse-argumentexception`.

Invalid `DateTimeStyles` values or invalid format specifiers now throw `ArgumentException` instead of returning
`false`. Example message: "The value '999' is not valid for DateTimeStyles. (Parameter 'style')".
Affected: all `DateOnly.TryParse`, `DateOnly.TryParseExact`, `TimeOnly.TryParse`, `TimeOnly.TryParseExact`
overloads that accept `DateTimeStyles` or a format.

### 5.7 `decimal` and `BigInteger` floating-point conversions are correctly rounded (Preview 7)

Source: `/compatibility/core-libraries/11/decimal-biginteger-floating-point-conversions`;
`dotnet/runtime#130565`, `dotnet/runtime#130566`.

Previous behavior:

- `float` → `decimal` kept only 7 significant decimal digits; `double` → `decimal` kept only 15.
- `decimal` → `float`/`double` could round more than once.
- `decimal` → `Half`/`BFloat16` went via `float`.
- `BigInteger` → `double` **truncated** the discarded bits instead of rounding to nearest.
- `BigInteger` → `float`/`Half`/`BFloat16` went via `double` (double rounding, could be one ULP off).

New behavior: the exact source value is rounded **once** to the nearest representable destination value.

| Expression | Old | New |
| --- | --- | --- |
| `((decimal)1.23).ToString("G29")` | `1.23` | `1.229999999999999982236431606` |
| `(double)10000000000000.099609375m` (`G99`) | `10000000000000.09765625` | `10000000000000.099609375` |
| `(double)(BigInteger)(long.MaxValue / 2)` (`G17`) | `4.6116860184273874E+18` | `4.6116860184273879E+18` |

**Important for a compiler**: the doc states explicitly, "If a conversion is evaluated as a compile-time constant, a
compiler hosted by the .NET 11 Preview 7 SDK or a later SDK can embed the new result in the output assembly when the
project is rebuilt, **regardless of the project's target framework**." That is, constant folding performed by the
compiler process changes with the compiler host's runtime, not with the target framework.

Affected APIs include `decimal(float)`, `decimal(double)` constructors, all explicit conversions between `decimal`
and `float`/`double`/`Half`/`BFloat16`, `decimal.ToSingle`/`ToDouble`, `Convert.ToDecimal`/`ToSingle`/`ToDouble`,
`decimal.CreateChecked/CreateSaturating/CreateTruncating` when converting to or from `float`/`double`, explicit
conversions from `BigInteger` to `double`/`float`/`Half`/`BFloat16`, and equivalent conversions through
`IConvertible` or the generic math interfaces. No compatibility switch.

To emulate the old `decimal` conversion, format with `G7` (from `float`) or `G15` (from `double`) and re-parse; the
old conversion rounded to nearest, ties to even, symmetric for positive and negative.

### 5.8 `DeflateStream` and `GZipStream` write headers and footers for an empty payload (Preview 1)

Source: `/compatibility/core-libraries/11/deflatestream-gzipstream-empty-payload`.

An empty compression now produces **2 bytes** for Deflate and **20 bytes** for GZip, instead of 0. To restore the old
behavior, special-case empty content and do not run it through the stream.

### 5.9 `Environment.TickCount` / `TickCount64` on Windows (Preview 1)

Source: `/compatibility/core-libraries/11/environment-tickcount-windows-behavior`.

- Previously `Environment.TickCount64` on Windows returned Win32 `GetTickCount64`: fixed 10–16 ms cadence (typically
  15.5 ms), **including** sleep/hibernation time.
- Now it returns Win32 `QueryUnbiasedInterruptTime`: excludes non-awake time, and updates at the frequency of the
  system interrupt timer. This matches Linux and macOS and matches the OS wait APIs (`SleepEx`,
  `WaitForMultipleObjectsEx`), which have excluded non-awake time since Windows 8 / Server 2012.
- `Environment.TickCount` still returns the truncated `TickCount64` and overflows about every 49 days.
- Code that must include sleep time should use `DateTime.UtcNow` (and account for clock adjustments).

### 5.10 `Math.Round` and `MathF.Round` return correctly rounded results (Preview 7)

Source: `/compatibility/core-libraries/11/math-round-digits`; `dotnet/runtime#130574`.

- Previously the `digits` overloads computed `Round(value * 10^digits, mode) / 10^digits`. Roughly **5%** of random
  inputs over the supported digit range produced an incorrectly rounded result.
- The `digits` argument was limited to 0–15 for `double` and 0–6 for `float`; out-of-range threw
  `ArgumentOutOfRangeException`.
- Now the result is computed from the exact value of the input using arbitrary-precision arithmetic. **Any
  non-negative `digits` value is accepted**; only negative values throw. Digit counts at or beyond the round-trip
  precision (17 for `double`, 9 for `float`) leave the value unchanged.
- Out-of-range `MidpointRounding` values now throw immediately rather than being silently coerced
  (Preview 7 release notes).

| Expression | Old | New |
| --- | --- | --- |
| `Math.Round(655.925, 2, MidpointRounding.AwayFromZero)` | `655.93` | `655.92` |
| `Math.Round(1111111111111111.5, 1, MidpointRounding.AwayFromZero)` | `1111111111111111.6` | `1111111111111111.5` |
| `Math.Round(1.5, 16, MidpointRounding.ToEven)` | throws `ArgumentOutOfRangeException` | `1.5` |

The corrected behavior and lifted range flow through `double.Round`, `float.Round`, `Half.Round`, `NFloat.Round`.
`Math.Round(decimal, int)` and the single-argument `Math.Round(double)` / `MathF.Round(float)` are unaffected.

### 5.11 `NamedPipeServerStream` with `PipeOptions.CurrentUserOnly` on Unix (Preview 4)

Source: `/compatibility/core-libraries/11/namedpipeserverstream-unix-permissions`; `dotnet/runtime#127239`.

The backing Unix domain socket file is now `chmod`'d to **`0600`** immediately after `bind()`, instead of inheriting
the process umask (commonly 0644 or 0755). The change is **ratcheted** within a process: once any
`NamedPipeServerStream` for a given pipe name specifies `CurrentUserOnly`, the socket file stays `0600` for the
remainder of the shared server entry's lifetime, even if a later instance for the same name omits the option.

Relevant to any product that uses named pipes for cross-process IPC on Linux/macOS (for example a design-time service
talking to a language server or an out-of-process analyzer host running under a different account).

### 5.12 `PackagePart.GetStream()` returns a non-seekable stream for compressed parts in `ReadWrite` packages (Preview 7)

Source: `/compatibility/core-libraries/11/packagepart-getstream-non-seekable`; `dotnet/runtime#129698`.

All of these must hold simultaneously to observe the change: the package is opened with `FileAccess.ReadWrite`
(internally `ZipArchiveMode.Update`); the part is opened for reading only; the part is compressed
(`CompressionOption` other than `NotCompressed`); the part was not written or modified earlier in the same session;
and the consumer seeks or reads `Position`.

Now `stream.CanSeek == false`; `Seek` and setting `Position` throw `NotSupportedException`. `Stream.Length` still
works (reported from entry metadata). The optimization is gated behind `NET11_0_OR_GREATER`, so it does not apply to
earlier target frameworks. Forward-only consumers (`XmlReader`, `XDocument.Load`, the Open XML SDK, `CopyTo`) are
unaffected. Workaround: copy into a `MemoryStream`.

### 5.13 API obsoletions with non-default diagnostic IDs (core libraries)

Source: `/compatibility/core-libraries/11/obsolete-apis`.

| Diagnostic ID | Obsoleted API | Severity | Replacement |
| --- | --- | --- | --- |
| `SYSLIB0064` | `RSACryptoServiceProvider.Encrypt(byte[], bool)` and `RSACryptoServiceProvider.Decrypt(byte[], bool)` | Warning | Overloads that accept an `RSAEncryptionPadding` |

Because the diagnostic ID is custom, suppressing `CS0618` does **not** suppress it; suppress `SYSLIB0064`.

### 5.14 `SafeFileHandle.IsAsync` and `FileStream.IsAsync` on Unix (Preview 3)

Source: `/compatibility/core-libraries/11/safefilehandle-isasync-unix`; `dotnet/runtime#125220`.

- Previously on Unix, `IsAsync` returned `true` for regular files opened with `FileOptions.Asynchronous` even though
  no `O_NONBLOCK` was set, and returned `false` for descriptors (pipes, sockets) that genuinely had `O_NONBLOCK`.
- Now `IsAsync` reflects the actual `O_NONBLOCK` state: `false` for regular files, `true` for non-blocking pipes and
  sockets.
- Additionally, on non-Windows platforms constructing a `SendPacketsElement` with a `FileStream` no longer throws
  `ArgumentException` regardless of whether the stream is async. Guard any test that expected the exception with
  `OperatingSystem.IsWindows()`.
- This was a prerequisite for the new `SafeFileHandle.CreateAnonymousPipe` API (see §7.3).

### 5.15 TAR-reading APIs verify header checksums (Preview 1)

Source: `/compatibility/core-libraries/11/tar-checksum-validation`; `dotnet/runtime#118577`, `dotnet/runtime#117455`.

`TarReader` validates the checksum of each entry and throws `System.IO.InvalidDataException` on mismatch, stopping
the read. Affected: `TarReader.GetNextEntry(bool)`, `TarReader.GetNextEntryAsync(bool, CancellationToken)`,
`TarFile.ExtractToDirectory(Stream, string, bool)`, `TarFile.ExtractToDirectoryAsync(...)`.

### 5.16 `TarWriter` uses `HardLink` entries for hard-linked files (Preview 3)

Source: `/compatibility/core-libraries/11/tarwriter-hardlink-entries`; `dotnet/runtime#123874`.

`TarWriter` detects files that share an inode and writes a `TarEntryType.HardLink` entry for subsequent occurrences
instead of duplicating content, matching GNU tar. New API to restore the old behavior:

```csharp
var options = new TarWriterOptions { HardLinkMode = TarHardLinkMode.CopyContents };
using var writer = new TarWriter(stream, options, leaveOpen: false);
```

Extracting an archive that contains `HardLink` entries to a file system without hard-link support throws
`IOException`; the new `TarExtractOptions` class controls whether hard links are extracted as hard links or copied
as separate files.

### 5.17 `ZipArchive.CreateAsync` eagerly loads entries (Preview 1)

Source: `/compatibility/core-libraries/11/ziparchive-createasync-eager-load`; `dotnet/runtime#121938`,
`dotnet/runtime#121624`.

The central directory is now read asynchronously inside `ZipArchive.CreateAsync`. Exceptions for malformed entries
(`InvalidDataException`) are now thrown from `CreateAsync` rather than from the first access to `Entries`. Accessing
`Entries` no longer performs synchronous reads on the underlying stream.

### 5.18 Other library breaking changes recorded only in the preview release notes

These appear in `release-notes/11.0/preview/*/libraries.md` but not (yet) on the Learn breaking-change index:

- **`WindowLog` renamed to `WindowLog2`** (Preview 7, `dotnet/runtime#129977`). Affects
  `BrotliCompressionOptions.WindowLog`, `ZLibCompressionOptions.WindowLog`, `ZstandardCompressionOptions.WindowLog`,
  and `ZstandardDecompressionOptions.MaxWindowLog` → `WindowLog2` / `MaxWindowLog2`, plus the related
  `Default`/`Min`/`Max` static properties and the `windowLog` parameters. **No compatibility alias.** Only affects
  code written against earlier .NET 11 previews.
- **`TensorPrimitives.Clamp` no longer throws when `min > max`** (Preview 7, `dotnet/runtime#130703`). The scalar
  path now matches the vectorized path (`Min(Max(x, min), max)`) instead of throwing `ArgumentException`.
- **`Process.Run` / `RunAsync` / `RunAndCaptureText` / `RunAndCaptureTextAsync` / `StartAndForget` take
  `IEnumerable<string>?`** for `arguments` instead of `IList<string>?` (Preview 7, `dotnet/runtime#130630`), and
  `Process.Run` / `RunAsync` gained a `bool silent = false` parameter positioned **before** the existing optional
  parameters (Preview 6, `dotnet/runtime#129509`). Both affect only the new-in-.NET-11 APIs.
- **`Microsoft.Extensions.Logging` moved several internal types** to a new location and ships `[Obsolete]`
  compatibility shims in the original namespaces (Preview 4, `dotnet/runtime#127194`). Affects logging-library
  authors and source generators; the shims are slated for removal in a future release.
- **`System.Security.Cryptography.Xml` mitigations may reject signed or encrypted XML that previously verified**
  (Preview 4, `dotnet/runtime#126957`).
- **`System.DirectoryServices.AccountManagement` now escapes LDAP filter values** (Preview 4,
  `dotnet/runtime#126433`). Code that double-escaped now produces different filter strings.
- **`ZipArchive` Update mode no longer drops data descriptors when re-saving** (Preview 4, `dotnet/runtime#126447`).
  Byte-for-byte archive comparisons before and after a no-op update will differ.
- **`MetadataLoadContext.CoreAssembly` lost its `[NotNull]` annotation** (Preview 4, `dotnet/runtime#126142`),
  because the property is genuinely nullable until a core assembly is loaded. This is a nullable-annotation change
  that can produce new nullable warnings in consumer code.

---

## 6. Cryptography

### 6.1 DSA removed from macOS (Preview 1) — the headline cryptography removal

Source: `/compatibility/cryptography/11/dsa-removed-macos`; tracked by `dotnet/docs#48201`.

- **Only finite-field DSA is removed. Elliptic Curve DSA (ECDSA) is not affected.**
- On macOS, `DSA`, `DSACryptoServiceProvider`, X.509 certificates with DSA keys, and any API that interacts with DSA
  keys now throw **`PlatformNotSupportedException`**.
- Reason: .NET relied on Apple's now-obsolete `SecurityTransforms` library, which has no replacement. Apple's
  implementation supported only DSA-1024 with SHA-1 and never supported DSA key generation.
- iOS, tvOS and MacCatalyst never supported DSA.
- Recommended replacement: **EC-DSA** (or another modern digital-signature algorithm).
- Enumerated affected APIs: `DSA.Create` (all overloads), `DSACryptoServiceProvider` constructors,
  `DSACertificateExtensions.GetDSAPrivateKey(X509Certificate2)`,
  `DSACertificateExtensions.GetDSAPublicKey(X509Certificate2)`,
  `DSACertificateExtensions.CopyWithPrivateKey(X509Certificate2, DSA)`, plus "any APIs that interact with DSA keys".

No corresponding removal is documented for Windows or Linux: DSA continues to work there through CNG and OpenSSL
respectively.

### 6.2 Composite ML-DSA on Windows uses the native CNG implementation (Preview 7)

Source: `/compatibility/cryptography/11/compositemldsa-windows-native`; `dotnet/runtime#129612`.

`System.Security.Cryptography.CompositeMLDsa` on Windows now uses the native CNG/BCrypt implementation instead of a
managed layer over ML-DSA, RSA and ECDSA. Windows implements exactly four parameter sets natively, all pairing
ML-DSA with ECDSA:

| Windows parameter set | Algorithm | `CompositeMLDsaAlgorithm` member |
| --- | --- | --- |
| `44-ECDSA-P256-SHA256` | Composite ML-DSA-44 + ECDSA P-256 | `MLDsa44WithECDsaP256` |
| `65-ECDSA-P256-SHA512` | Composite ML-DSA-65 + ECDSA P-256 | `MLDsa65WithECDsaP256` |
| `65-ECDSA-P384-SHA512` | Composite ML-DSA-65 + ECDSA P-384 | `MLDsa65WithECDsaP384` |
| `87-ECDSA-P384-SHA512` | Composite ML-DSA-87 + ECDSA P-384 | `MLDsa87WithECDsaP384` |

Every other composite algorithm now throws `PlatformNotSupportedException` on Windows. This is a **regression in
coverage**: every ML-DSA + RSA composite worked before and no longer does. ML-DSA + EdDSA (Ed25519, Ed448) already
threw. Guard with `CompositeMLDsa.IsAlgorithmSupported(...)`. Composite ML-DSA **certificate** APIs continue to throw
`PlatformNotSupportedException` on Windows, unchanged.

### 6.3 Linux AIA certificate fetching limited to two fetches per chain build (Preview 7)

Source: `/compatibility/cryptography/11/aia-fetch-limit-linux`; `dotnet/runtime#130456`.

On Linux and other OpenSSL-based platforms, `X509Chain.Build(X509Certificate2)` now performs at most **two**
Authority Information Access fetches per chain build, matching the limit Windows has always had. Chains requiring
three or more AIA downloads now fail. Mitigation: add the intermediates to `chain.ChainPolicy.ExtraStore`, or install
them in the user/system intermediate store.

### 6.4 Cryptography obsoletions (Preview 6)

Source: `/compatibility/cryptography/11/obsolete-apis`.

| Diagnostic ID | Obsoleted API | Severity | Replacement |
| --- | --- | --- | --- |
| `SYSLIB0065` | the **`set` accessor** of `AsnEncodedData.RawData` | Warning | Use the constructor of the appropriate type to decode data, or `AsnEncodedData.CopyFrom(AsnEncodedData)` for mutable scenarios |

### 6.5 New cryptography APIs

- **`System.Security.Cryptography.X25519DiffieHellman`** (abstract), with platform implementations
  `X25519DiffieHellmanCng` (Windows) and `X25519DiffieHellmanOpenSsl` (Linux, macOS). The portable
  `X25519DiffieHellman.GenerateKey()` factory picks the right one. Supports key generation, PKCS#8 and
  SubjectPublicKeyInfo import/export, PEM serialization, raw private/public key access, and
  `DeriveRawSecretAgreement(X25519DiffieHellman)`.
- **`CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, byte)`**: constant-time comparison of a span with a
  single known byte value.
- HMAC and KMAC **verification** APIs (Preview 1).

---

## 7. Reflection, assembly loading and plug-in hosting

### 7.1 `System.Type.GetNullableUnderlyingType()` — new virtual

See §5.2 for the breaking-change detail. As a feature: `Type.GetNullableUnderlyingType()` returns the underlying
value type for `Nullable<T>` or `null` otherwise. Overrides ship on `Type` (runtime implementation),
`TypeBuilder`, `EnumBuilder`, `GenericTypeParameterBuilder`, `TypeDelegator`, `TypeBuilderInstantiation`,
`SymbolType`, `ModifiedType`, the `SignatureType` family and the `MetadataLoadContext` types.

### 7.2 `ConstructorInfo.GetGenericArguments()`

Now has an override, giving a consistent way to retrieve generic type arguments for constructor definitions,
matching the behavior already available on other `MethodBase` subclasses.
(Source: `/whats-new/dotnet-11/libraries`.)

### 7.3 `AssemblyLoadContext.SetAssemblyLocationOverride` (Preview 7) — most relevant to plug-in hosts

Source: `/whats-new/dotnet-11/libraries`; Preview 7 release notes; `dotnet/runtime#129773`.

```csharp
public static void SetAssemblyLocationOverride(Func<Assembly, string, string> callback);
```

A **set-once** static callback that overrides the value returned by `Assembly.Location`, on **CoreCLR, Mono and
NativeAOT**. The callback receives the assembly and the location the runtime would otherwise report, and returns the
location to use. Intended for hosts that stage assemblies in temporary directories or bundle them (single-file
publishing, embedded resources, virtual file systems) and want `Assembly.Location` to report something meaningful to
diagnostics and resource-loading code. Set-once semantics prevent a later component from silently redirecting an
in-flight override.

```csharp
AssemblyLoadContext.SetAssemblyLocationOverride((assembly, defaultLocation) =>
    assembly.GetName().Name is { } name
        ? Path.Combine(realInstallDirectory, name + ".dll")
        : defaultLocation);
```

### 7.4 `MetadataLoadContext.GetLoadContext(Assembly)` (Preview 4)

Source: `/whats-new/dotnet-11/libraries`; `dotnet/runtime#126926`.

```csharp
public static MetadataLoadContext? GetLoadContext(Assembly assembly);
```

Mirrors `AssemblyLoadContext.GetLoadContext`. Lets tooling that reflects over assemblies in an isolated
`MetadataLoadContext` walk back from an `Assembly` reference to the owning context.

Related fixes: `MetadataLoadContext` no longer returns internal array types instead of `Type[]` from several methods
(Preview 2, `dotnet/runtime#124251`); `MetadataLoadContext.CoreAssembly` lost its `[NotNull]` annotation
(Preview 4, `dotnet/runtime#126142`).

### 7.5 Function-pointer support in `System.Reflection.Emit` (Preview 1)

Source: `release-notes/11.0/preview/preview1/libraries.md`; `dotnet/runtime#119935` (creating and working with
function pointer types), `dotnet/runtime#121128` (references to unmanaged function pointers).

### 7.6 `System.Reflection.Metadata`

No dedicated `System.Reflection.Metadata` feature or breaking change is documented in the .NET 11 breaking-change
index, the what's-new library page, or any preview release note. See open questions.

### 7.7 Other assembly-loading-adjacent notes

- Better `InvalidCastException` message when a generic argument comes from a different `AssemblyLoadContext`
  (Preview 4, `dotnet/runtime#125973`).
- `Assembly.GetCallingAssembly()` now works on NativeAOT (see §5.1), which changes what a plug-in host can rely on
  when it is AOT-compiled — and now throws when `StackTraceSupport=false` on **both** CoreCLR and NativeAOT.

---

## 8. `System.Text.Json`

No `System.Text.Json` **breaking change** is listed in the .NET 11 breaking-change index. New features
(`/whats-new/dotnet-11/libraries`):

- **Generic type-info retrieval**: `JsonSerializerOptions.GetTypeInfo<T>()` and
  `JsonSerializerOptions.TryGetTypeInfo<T>(out JsonTypeInfo<T>?)`, removing the manual downcast from the non-generic
  `GetTypeInfo(Type)`.
- **`JsonNamingPolicy.PascalCase`**: a new built-in naming policy alongside camelCase, snake_case and kebab-case.
- **`System.Text.Json.Serialization.JsonNamingPolicyAttribute`**: per-member naming-policy override.
- **Type-level `JsonIgnoreAttribute`**: applying `[JsonIgnore]` at class or struct level sets the default ignore
  behavior for all members.
- **F# discriminated union support** out of the box (emits `{"$type":"Circle","radius":1.5}` for `Circle 1.5`).
- **`Utf8JsonWriter.Reset(Stream, JsonWriterOptions)`** and the `IBufferWriter` equivalent — repool a writer with
  different options without allocating.
- **`JsonSerializer.SerializeAsyncEnumerable`**: new overloads that write to a `System.IO.Pipelines.PipeWriter`, and
  a `topLevelValues: bool` parameter that emits NDJSON (one top-level value per line) instead of a JSON array.
  Available on both the `Stream` and `PipeWriter` overloads.
- **C# union type serialization**: a new `JsonTypeInfoKind.Union` contract kind; new `JsonUnionAttribute` and
  `JsonUnionCaseInfo`; type-classifier APIs `JsonTypeClassifier` and `JsonSerializerOptions.TypeClassifiers`.
  Union types are a C# 15 preview language feature.
- **`JsonSerializerOptions.InferClosedTypePolymorphism`**: infers polymorphic metadata for C# closed hierarchies
  without explicit `[JsonDerivedType]` annotations. Explicit registrations take precedence.
- **`JsonMetadataServices.CreateIReadOnlySetInfo`**: enables serialization of `IReadOnlySet<T>`.

---

## 9. `System.IO`

### Breaking

See §5.5 (ZIP CRC32), §5.8 (Deflate/GZip empty payload), §5.12 (`PackagePart.GetStream`), §5.14
(`SafeFileHandle.IsAsync`), §5.15–5.17 (TAR, `ZipArchive.CreateAsync`), §5.11 (named pipes).

### New

- **Four in-memory `Stream` adapters** (Preview 6): `ReadOnlyMemoryStream` (over `ReadOnlyMemory<byte>`),
  `WritableMemoryStream` (over a writable `Memory<byte>`, fixed size), `ReadOnlySequenceStream` (over a
  `ReadOnlySequence<byte>` without flattening it), `StringStream` (over a `string` or `ReadOnlyMemory<char>` with a
  specified encoding).
- **`SafeFileHandle.Type`** property: reports whether a handle is a file, pipe, socket, directory or other OS object.
- **`SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle, out SafeFileHandle, bool asyncRead, bool asyncWrite)`**:
  creates a connected anonymous pipe pair with independent async behavior per end.
- **`RandomAccess.Read` / `RandomAccess.Write` work with non-seekable handles** such as pipes.
- On Windows, `Process` now uses overlapped I/O for redirected stdout/stderr, reducing thread-pool blocking.
- **`ZipArchiveEntry.Open(FileAccess)`** and **`OpenAsync(FileAccess, CancellationToken)`**; new
  `ZipArchiveEntry.CompressionMethod` property returning the `ZipCompressionMethod` enum
  (`Stored`, `Deflate`, `Deflate64`).
- **ZIP password support**: `ZipArchiveEntry.Open(ReadOnlySpan<char>)`,
  `ZipArchiveEntry.OpenAsync(ReadOnlySpan<char>, CancellationToken)`, and `ZipEncryptionMethod` for creating
  encrypted entries.
- **Span-based Deflate/ZLib/GZip encoders and decoders**: `DeflateEncoder`, `ZLibEncoder`, `GZipEncoder` and their
  decoders, mirroring `BrotliEncoder`/`BrotliDecoder`, with `OperationStatus`-returning `Compress`/`Decompress`.
- **Zstandard** (`ZstandardStream`, `ZstandardEncoder`, …) moved into the `System.IO.Compression` namespace
  (Preview 3); the API surface is otherwise unchanged.
- **`TarFile.CreateFromDirectory` / `CreateFromDirectoryAsync` overloads taking `TarEntryFormat`**
  (Pax, Ustar, Gnu, V7). Previously `CreateFromDirectory` always produced Pax.
- **`TarReader` reads GNU sparse format 1.0 (PAX)**; 0.1 was already supported.
- **Hard-link creation APIs** (`File.CreateHardLink`, Preview 1).
- **`System.IO.Pipelines` contention reduction** (Preview 7, `dotnet/runtime#130884`): buffer rent/return moved
  outside the pipe lock; the per-pipe segment pool is FIFO; the pipe lock is now a `System.Threading.Lock`;
  continuations are scheduled on local thread-pool queues.

---

## 10. `System.Threading` and `Task`

- **`Interlocked.And` / `Interlocked.Or` generic overloads** (Preview 1, `dotnet/runtime#120978`): atomic bitwise
  operations on any enum or integer type.
- **`TextWriter` `CancellationToken` overloads** (Preview 1, `dotnet/runtime#122127`) on every `WriteAsync` and
  `WriteLineAsync`: `WriteAsync(char, CancellationToken)`, `WriteAsync(string?, CancellationToken)`,
  `WriteAsync(char[], int, int, CancellationToken)`, `WriteAsync(ReadOnlyMemory<char>, CancellationToken)`, and the
  five matching `WriteLineAsync` overloads.
- **Async continuations opt out of `ExecutionContext` capture/restore** when there is nothing to restore
  (see §4.1). Applies to `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`.
- **Runtime Async** changes what an async call stack looks like at run time (see §4.1).
- `System.Threading.RateLimiting` fixes (Preview 4): `FixedWindowRateLimiter` reports a `RetryAfter` metadata value
  pointing at the next window boundary (`dotnet/runtime#124478`); `TokenBucketRateLimiter.AttemptAcquire(0)` no
  longer mishandles partial refills (`dotnet/runtime#124498`); `ChainedRateLimiter` forwards `IdleDuration` and
  replenishment behavior from inner limiters (`dotnet/runtime#126158`).
- `IHost.RunAsync` / `StopAsync` / `WaitForShutdownAsync` now fail when a `BackgroundService` fails (see §11.3).

No breaking change is documented for `Task`, `ValueTask`, `CancellationToken`, `ThreadPool` or `SemaphoreSlim`.

---

## 11. `Microsoft.Extensions.*` breaking changes

### 11.1 Nine `Microsoft.Extensions.*` packages are now in the shared framework (Preview 4)

Source: `/compatibility/extensions/11/extensions-in-shared-framework`.

The following are now part of the .NET base shared framework (available with `Sdk="Microsoft.NET.Sdk"` and the other
.NET SDKs):

1. `Microsoft.Extensions.Caching.Abstractions`
2. `Microsoft.Extensions.Configuration.Abstractions`
3. `Microsoft.Extensions.DependencyInjection.Abstractions`
4. `Microsoft.Extensions.Diagnostics.Abstractions`
5. `Microsoft.Extensions.FileProviders.Abstractions`
6. `Microsoft.Extensions.Hosting.Abstractions`
7. `Microsoft.Extensions.Logging.Abstractions`
8. `Microsoft.Extensions.Options`
9. `Microsoft.Extensions.Primitives`

Consequences:

- An explicit `PackageReference` on any of these produces build warning **`NU1510`** when the project targets only
  `net11.0`. Multi-targeted projects do not get `NU1510` because the package is still needed for the older TFM.
- These assemblies are **no longer copied to the output folder**.
- "In rare cases, the additional APIs in the default load set might cause name or type conflicts." Resolve with a
  more specific `using`, a `using` alias, or a fully qualified type name. This is a **source-compatibility hazard for
  any library that defines a type whose simple name collides with one of these**.
- If a dependent library was compiled against an older version of one of these packages, it can now fail at run time
  with `MissingMethodException` or `TypeLoadException`; recompile against the .NET 11 reference assemblies.
- The doc lists prior breaking changes in these packages that surface on upgrade, including
  `ActivatorUtilities.CreateInstance` behavior (.NET 8), `FromKeyedServicesAttribute.Key` nullability (.NET 8),
  non-keyed service used when keyed not found (.NET 9), `GetKeyedService`/`GetKeyedServices` with `AnyKey` (.NET 10),
  `ProviderAliasAttribute` moved assembly (.NET 10), `BackgroundService` unhandled exceptions (.NET 6), and
  `BackgroundService` running all of `ExecuteAsync` as a `Task` (.NET 10).

### 11.2 `ChangeToken.OnChange` async overloads rebind existing callbacks (Preview 7)

Source: `/compatibility/extensions/11/changetoken-onchange-async-overloads-rebind-callbacks`;
`dotnet/runtime#129624`, API review `dotnet/runtime#69099`.

New overloads:

```csharp
public static IDisposable OnChange(Func<IChangeToken?> changeTokenProducer, Func<Task> changeTokenConsumer);
public static IDisposable OnChange<TState>(Func<IChangeToken?> changeTokenProducer, Func<TState, Task> changeTokenConsumer, TState state);
```

**This is a silent overload-rebinding change.** Source that passed an `async` lambda previously bound to the `Action`
overload and compiled as `async void` (fire and forget; re-registration happened at the first incomplete `await`;
later exceptions surfaced on the synchronization context or thread pool). After recompiling against .NET 11 the same
source binds to `Func<Task>`, compiles as `async Task`, and re-registration happens only after the returned task
completes; multiple changes during the callback are coalesced into one later invocation. The compiler reports no
ambiguity. To keep the old behavior, cast: `(Action)(async () => { … })`. No AppContext switch — overload selection
happens at compile time.

### 11.3 `IHost.RunAsync` and `IHost.StopAsync` throw when a `BackgroundService` fails (Preview 3)

Source: `/compatibility/extensions/11/ihost-runasync-stopasync-throw-backgroundservice-failure`;
`dotnet/runtime#124863`.

When a `BackgroundService.ExecuteAsync` throws and `HostOptions.BackgroundServiceExceptionBehavior` is
`StopHost` (the default), the tasks returned from `RunAsync`, `StopAsync`, `WaitForShutdownAsync` and their
synchronous equivalents now **fail** instead of completing successfully, so the process exits with a non-zero exit
code. A single failure rethrows the service's exception; multiple failures are combined into an
`AggregateException`. Affected: `HostingAbstractionsHostExtensions.RunAsync/Run/StopAsync/WaitForShutdownAsync/
WaitForShutdown` and the default `IHost.StopAsync` implementation.

### 11.4 `FileConfigurationSource.OnLoadException` is called for I/O errors (Preview 7)

Source: `/compatibility/extensions/11/fileconfigurationsource-onloadexception-io-errors`;
`dotnet/runtime#113964`, `dotnet/runtime#126093`.

Previously the `Exception` on `FileLoadExceptionContext` was always `InvalidDataException` or `FileNotFoundException`,
and I/O errors were only observable through `TaskScheduler.UnobservedTaskException`. Now I/O errors are forwarded to
`OnLoadException`, so the exception can be of **any** type (commonly `IOException`, or anything thrown by a custom
`IFileProvider`). Code that unconditionally casts to `InvalidDataException`/`FileNotFoundException` can now throw
`InvalidCastException`. I/O errors are no longer observable through `UnobservedTaskException` except when no
`OnLoadException` callback is registered.

### 11.5 `FileConfigurationProvider` does not raise the reload token after an ignored load failure (Preview 7)

Source: `/compatibility/extensions/11/fileconfigurationprovider-reload-token-load-failure`;
control-flow consequence of `dotnet/runtime#126093`.

After `Load()` fails and the `OnLoadException` callback sets `FileLoadExceptionContext.Ignore = true`, the provider
no longer calls `OnReload`, so the token from `GetReloadToken()` does not fire. Affected:
`FileConfigurationProvider.Load()`, `IniConfigurationProvider.Load(Stream)`, `JsonConfigurationProvider.Load(Stream)`,
`XmlConfigurationProvider.Load(Stream)`.

---

## 12. Globalization

**Japanese Calendar minimum supported date corrected** (Preview 1) —
`/compatibility/globalization/11/japanese-calendar-min-date`; CLDR issue `CLDR-11375`.

`JapaneseCalendar.MinSupportedDateTime` changes from **1868-09-08** to **1868-10-23** (the corrected start of the
Meiji era). `JapaneseCalendar` now rejects Gregorian dates between 1868-09-08 and 1868-10-23 as invalid.

No other globalization breaking change is listed. ICU/NLS behavior is otherwise unchanged in the documented set.

---

## 13. Networking

**`SslStream` server-side AIA certificate downloads disabled by default** (Preview 3) —
`/compatibility/networking/11/sslstream-aia-downloads-disabled`; `dotnet/runtime#125049`.

When `SslStream` validates **client** certificates as a server, it no longer downloads missing intermediates through
the AIA extension. If the client does not send the full chain, the handshake now fails with a certificate-validation
error. This applies **only when no custom `SslServerAuthenticationOptions.CertificateChainPolicy` is provided**; when
a custom `X509ChainPolicy` is supplied, its `DisableCertificateDownloads` value is respected (so a custom policy must
set `DisableCertificateDownloads = true` explicitly to match the new default). Reasons cited: performance
degradation from slow AIA servers, and the security risk of making outbound HTTP requests to client-influenced
endpoints. Affected: `SslStream.AuthenticateAsServer`, `SslStream.AuthenticateAsServerAsync`.

New networking APIs (non-breaking):

- Request-body compression wrappers: `GZipCompressedContent`, `BrotliCompressedContent`,
  `ZstandardCompressedContent` (set `Content-Encoding` and stream compressed content).
- `SocketsHttpHandler.ShouldEvictConnection` callback for per-connection eviction decisions.
- Typed DNS resolution: `Dns.ResolveSrv`, `ResolveMx`, `ResolveTxt`, `ResolveCName`, `ResolvePtr`, `ResolveNs` and
  `Async` variants, returning `DnsResult<T>` with records, response code and negative-cache TTL.
- `HttpClient` automatically downgrades to HTTP/1.1 when a request needs NTLM/Negotiate over HTTP/2.
- `QuicStream.Priority` and `QuicStream.DefaultPriority` (RFC 9218; 0 highest to 255 lowest, default 127).
- `System.Net.Mime.MediaTypeNames.Video` constants: `Mp4`, `Mpeg`, `Ogg`, `QuickTime`, `WebM`.
- Happy Eyeballs support in `Socket.ConnectAsync` (Preview 1).
- `socks5h://` proxy scheme support in `HttpClient` (Preview 1, `dotnet/runtime#123218`); previously threw
  `NotSupportedException`.
- TLS handshake hardening: `TlsFrameHelper` bounds-checking fixes for malformed ClientHello records; on Linux,
  certificate-validation failures now surface as standard TLS alerts to the peer rather than a connection drop.

---

## 14. Trimming, single-file and Native AOT

No breaking change to trimming or single-file **defaults** is documented for .NET 11. The relevant documented items:

- **NativeAOT `lib` prefix on Unix** (breaking, §3.3), opt out via `UseNativeLibPrefix`.
- **`Assembly.GetCallingAssembly()` now supported under NativeAOT**, but throws `NotSupportedException` when the
  `StackTraceSupport` feature switch is `false` (§5.1). This is the one feature-switch behavior change that affects
  trimmed/AOT publishing.
- **NativeAOT interface dispatch** now uses a shared, patchable dispatch helper, reducing call-site binary size
  (§4.3).
- **`[RequiresUnsafe]` removed from a large set of pointer-taking APIs** (`/whats-new/dotnet-11/libraries`,
  "Unsafe API accessibility"). Previously calling these from `unsafe` code still required project-level
  `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` because the attribute enforced that independently; now only the
  standard `unsafe` block or method modifier is required. Affected: `Buffer.MemoryCopy`;
  `ReadOnlySpan<T>(void*, int)` and `Span<T>(void*, int)`; `System.Runtime.CompilerServices.Unsafe` pointer methods
  (`AsRef`, `Read`, `Write`, `Copy`); `System.Runtime.InteropServices.NativeMemory` methods; `System.Text.Encoding`
  pointer overloads on all encoding classes; `System.Numerics.Vector` pointer-based `Load`/`Store`; and interop
  marshalling types in `System.Runtime.InteropServices.Marshalling`. This pairs with the C# 15 "memory safety"
  feature.
- **`AssemblyLoadContext.SetAssemblyLocationOverride`** explicitly targets single-file and bundled scenarios (§7.3).
- `System.ServiceModel.Syndication` publishes without the previous false-positive **IL2067** trim warning, and
  NativeAOT keeps the formatter metadata (Preview 5, `dotnet/runtime#114028`).
- `dotnet publish` no longer removes native DLLs on subsequent runs of single-file publish (SDK fix).
- NativeAOT publish for WebAssembly no longer drops satellite assemblies from NuGet packages.
- SDK: `DOTNET_CLI_ENABLEAOT` now defaults to **enabled** (§16).

---

## 15. Other notable new APIs a compiler-adjacent tool would plausibly want

### Process and diagnostics

- One-shot process helpers: `Process.Run`, `Process.RunAsync`, `Process.RunAndCaptureText`,
  `Process.RunAndCaptureTextAsync` (returns `ProcessTextOutput` with `StandardOutput` and `ExitStatus.ExitCode`),
  `Process.ReadAllText(TimeSpan?)`, `Process.ReadAllBytes(TimeSpan?)` and async variants,
  `Process.ReadAllLines(TimeSpan?)` and `Process.ReadAllLinesAsync(CancellationToken)` returning
  `ProcessOutputLine` values that distinguish stdout from stderr.
- `Process.StartAndForget`; `ProcessStartInfo.StartDetached`; `ProcessStartInfo.KillOnParentExit` (Windows only).
- `SafeProcessHandle.Start(ProcessStartInfo)`, `SafeProcessHandle.ProcessId`, `SafeProcessHandle.Kill()`,
  `SafeProcessHandle.Signal(PosixSignal)`, `SafeProcessHandle.WaitForExit()`,
  `SafeProcessHandle.WaitForExitAsync(CancellationToken)`, `SafeProcessHandle.Open(int)`,
  `SafeProcessHandle.TryOpen(int, out SafeProcessHandle)`, `SafeProcessHandle.Resume()`.
- `ProcessStartInfo.InheritedHandles` — specify exactly which OS handles the child inherits, instead of the
  all-or-nothing `UseShellExecute = false` default.
- `ProcessStartInfo.StandardInputHandle` / `StandardOutputHandle` / `StandardErrorHandle` — supply already-open
  `SafeFileHandle` values for redirection.
- `ProcessStartInfo.StartSuspended` (Windows and macOS), paired with `SafeProcessHandle.Resume()`.
- `Process.TryGetProcessById(int, out Process)` — returns `false` instead of throwing.
- Process startup callbacks: `WindowsProcessStartArguments.Start` and `UnixProcessStartArguments.Start` supply a
  callback with prepared command line / argument vector, environment and standard-handle data, so an application can
  use a custom native process-creation API while keeping `Process` features (Preview 7, `dotnet/runtime#128862`).
- **`Console` honors `FORCE_COLOR`** (`https://force-color.org/`) alongside `NO_COLOR`. When `FORCE_COLOR` is set,
  `Console.IsOutputRedirected` no longer suppresses ANSI escape codes. Directly relevant to a build tool whose output
  is piped through `tee`, a CI log viewer, or `less -R`.

### Text and strings

- `String` overloads taking `System.Text.Rune`: `Contains(Rune)`, `Contains(Rune, StringComparison)`,
  `StartsWith(Rune[, StringComparison])`, `EndsWith(Rune[, StringComparison])`,
  `IndexOf(Rune[, StringComparison])`, `LastIndexOf(Rune[, StringComparison])`, `Replace(Rune, Rune)`,
  `Split(Rune, StringSplitOptions)`, `Split(Rune, int, StringSplitOptions)`, `Trim(Rune)`, `TrimStart(Rune)`,
  `TrimEnd(Rune)`.
- `Char.Equals(char, StringComparison)`.
- `TextInfo.ToLower(Rune)`, `TextInfo.ToUpper(Rune)`.
- **`StringBuilder.MoveChunks(StringBuilder source)`** (static): moves all content from `source` into a new
  `StringBuilder` without copying characters; `source.Length` becomes 0 afterwards.
- `Base64` additions: `EncodeToChars`, `EncodeToString`, `EncodeToUtf8`, `DecodeFromChars`, `DecodeFromUtf8`, in both
  allocating and span-based forms (parity with `Base64Url`).
- `Utf16.IsValid(ReadOnlySpan<char>)`; `Utf8.IndexOfInvalidSubsequence(ReadOnlySpan<byte>)` and
  `Utf16.IndexOfInvalidSubsequence(ReadOnlySpan<char>)` (return `-1` for valid input).
- `RegexOptions.AnyNewLine`: makes `^`, `$` and `.` treat the full Unicode newline set (`\r\n`, `\n`, `\u0085`,
  `\u2028`, `\u2029`) as line terminators. Plus non-backtracking-engine super-linear-time and correctness fixes; a
  `resumeAt` fix for conditionals inside loop bodies in the regex compiler and source generator; and a `SYSLIB1045`
  code-fixer fix that no longer creates duplicate class names across multiple partial declarations of a class.
- `StringSyntaxAttribute` gains `CSharp`, `FSharp` and `VisualBasic` constants — directly useful for annotating APIs
  that take C# source text.
- `Uri.UriSchemeData` constant for the `data:` scheme.
- Span-based IDN APIs on `IdnMapping` (Preview 1).

### Numerics

- `System.Numerics.Decimal32`, `Decimal64`, `Decimal128` — IEEE 754-2019 decimal floating-point types supporting
  generic math, infinities and NaN.
- `System.Numerics.Complex<T>` — generic complex over `float`, `double`, `Half`, `BFloat16` and the new decimal
  types.
- `System.Numerics.BFloat16` (Preview 1) plus `BitConverter.GetBytes(BFloat16)`,
  `BitConverter.ToBFloat16(byte[], int)`, `BitConverter.ToBFloat16(ReadOnlySpan<byte>)`,
  `BitConverter.BFloat16ToInt16Bits`, `BFloat16ToUInt16Bits`, `Int16BitsToBFloat16`, `UInt16BitsToBFloat16`.
- `INumberBase<TSelf>.TryParsePartial` — partial parsing that reports how much input was consumed (for delimited
  formats such as CSV, without substring copies).
- **Hexadecimal IEEE-754 formatting and parsing** for `double`, `float` and `Half`: `value.ToString("X")` produces
  e.g. `"0X1.921FB54442D18P+1"`, and `double.Parse(hex, NumberStyles.HexFloat)` round-trips exactly. Useful for
  golden-file tests and C/C++ `printf("%a", …)` interop.
- `DivisionRounding` enum for integer division modes (Preview 1).
- `Random.NextInteger<T>()` (where `T : IBinaryInteger<T>, IMinMaxValue<T>`, with upper-bound and range overloads)
  and `Random.NextBinaryFloat<T>()` (where `T : IBinaryFloatingPointIeee754<T>`).
- `Matrix4x4.GetDeterminant()` is SSE-vectorized (~15% faster).

### Vectors

`Vector64<T>`, `Vector128<T>`, `Vector256<T>`, `Vector512<T>` and `Vector<T>` gain:

- Patterned construction: `CreateGeometricSequence`, `CreateAlternatingSequence`, `CreateHarmonicSequence`.
- Interleave / de-interleave: `Zip`, `ZipLower`, `ZipUpper`, `Unzip`, `UnzipEven`, `UnzipOdd`.
- Rearrange: `ConcatLowerLower`, `ConcatLowerUpper`, `ConcatUpperLower`, `ConcatUpperUpper`, `Reverse`.
- SVE `CreateWhile` gains signed, `double` and `single` variants.

### Collections and LINQ

- `Enumerable.FullJoin`, plus tuple-returning `Join` and `GroupJoin` overloads (no result selector) and optional
  `IEqualityComparer<T>` overloads on `Join`, `GroupJoin`, `LeftJoin`, `RightJoin`, `FullJoin`. Available on
  `Enumerable`, `Queryable` and `AsyncEnumerable`.
- `EqualityComparer<T>.Create(Func<T, TKey> keySelector[, IEqualityComparer<TKey>])` — build a comparer from a key
  selector.
- `BitArray.PopCount()`.
- `ReadOnlySpan<T>.Min` / `Max` extension methods, with optional `IComparer<T>` overloads. Empty-span semantics match
  the LINQ operators (throw `InvalidOperationException` for value types, return `null` for reference types) without
  the enumerator allocation. (Preview 7, `dotnet/runtime#128306`.)
- `FrozenDictionary` collection-expression support (Preview 1).
- `EmptyServiceProvider.Instance` — a shared, allocation-free `IServiceProvider` that resolves nothing, implementing
  `IServiceProvider`, `IKeyedServiceProvider`, `IServiceProviderIsService` and `IServiceProviderIsKeyedService`
  explicitly. `GetService` returns `null`; `IsService` returns `false`; `GetRequiredService` still throws
  `InvalidOperationException`. (Preview 7, `dotnet/runtime#129578`.)

### Discriminated-union scaffolding — preview feature

`System.Runtime.CompilerServices.UnionAttribute` and `System.Runtime.CompilerServices.IUnion` are the runtime side of
the C# discriminated-union design. The docs state they are not directly user-facing yet: "the C# compiler and source
generators are the expected producers", but they ship in the framework so libraries can author against the surface.
Language design: `https://github.com/dotnet/csharplang/blob/main/proposals/unions.md`.

### DataAnnotations and options

- Asynchronous validation: `AsyncValidationAttribute` (override `IsValidAsync(object?, ValidationContext,
  CancellationToken)`), `IAsyncValidatableObject`, and `Validator.ValidateObjectAsync`, `TryValidateObjectAsync`,
  `ValidatePropertyAsync`, `ValidateValueAsync`.
- `Microsoft.Extensions.Options`: asynchronous options validation, including startup validation through the new
  `IAsyncStartupValidator`; and a generic `OptionsBuilder<TOptions>.Validate<TValidator>()` overload taking a
  DI-registered `IValidateOptions<TOptions>` implementation type.
- `Microsoft.Extensions.Configuration.ConfigurationIgnoreAttribute`; `ConfigurationBinder` now binds an empty array to
  a constructor parameter instead of throwing; `PhysicalFilesWatcher` no longer throws when its root directory does
  not exist; `InMemoryDirectoryInfo` resolves `..` consistently with the physical provider.

### Diagnostics and logging

- `MemoryCache` emits built-in OpenTelemetry metrics when `MemoryCacheOptions.TrackStatistics = true`. Meter name
  `Microsoft.Extensions.Caching.Memory.MemoryCache`; instruments `dotnet.cache.requests` (tag
  `dotnet.cache.request.type` = `hit`/`miss`), `dotnet.cache.evictions`, `dotnet.cache.entries`,
  `dotnet.cache.estimated_size`. New constructor overload
  `MemoryCache(IOptions<MemoryCacheOptions>, ILoggerFactory, IMeterFactory)`.
- `Microsoft.Extensions.Diagnostics` `AddTracing` API with `EnableTracing`/`DisableTracing` rules (by source name and
  operation name), configurable from configuration; plus `ActivitySourceFactory`, and `ActivitySource` is
  **unsealed**.
- The logging source generator supports **generic methods** decorated with `[LoggerMessage]` (primarily to avoid
  boxing). Standard constraints (`class`, `struct`, `unmanaged`, interfaces, base types, `new()`) are supported; the
  `allows ref struct` anti-constraint is **not** and produces `SYSLIB1011`.

---

## 16. SDK and MSBuild items with runtime consequences

Covered here only where they change what a hosted tool observes at run time.

- **`DOTNET_CLI_ENABLEAOT` defaults to enabled** (Preview 7) —
  `/compatibility/sdk/11/native-cli-command-handling-enabled`. The NativeAOT-compiled `dotnet` CLI command-handling
  fast path is now on by default on Windows, macOS and Linux. Truthy values: `true`, `1`, `yes`, `on`; falsy:
  `false`, `0`, `no`, `off`. Commands fully served from the AOT path: `dotnet --version`, `--info`, `--help`,
  `dotnet <command> --help` for every built-in command, `dotnet --cli-schema`, `dotnet sln list`, `dotnet sln
  migrate`, `dotnet sln remove`. Tool and external-command invocations (global tools, PATH commands, app-base
  commands) resolve and launch out-of-process from the AOT path, skipping 600–700 ms of managed CLI startup.
- **MSBuild server enabled by default**. Opt out with `DOTNET_CLI_USE_MSBUILD_SERVER=false` or `MSBUILDUSESERVER=0`.
  Additionally, the CLI no longer unconditionally writes `MSBUILDUSESERVER=0`: if `DOTNET_CLI_USE_MSBUILD_SERVER` is
  unset, `MSBUILDUSESERVER` is left untouched. **A warm MSBuild worker persisting between CLI invocations changes the
  file-locking picture for any tool that loads user assemblies into the build process.**
- **VSTest removes its dependency on `Newtonsoft.Json`** (Preview 4) —
  `/compatibility/sdk/11/vstest-removes-newtonsoft-json`. `Microsoft.NET.Test.SDK` no longer brings
  `Newtonsoft.Json` transitively. On .NET, `System.Text.Json` is used; on .NET Framework, JSONite.
  Symptoms: compile failures in test projects that used `Newtonsoft.Json` types without a direct reference; and at
  run time `FileNotFoundException: Could not load 'Newtonsoft.Json'` (including
  `Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed`) for projects that used
  `<ExcludeAssets>runtime</ExcludeAssets>` and for **test extensions (data collectors, test adapters)** that relied
  on VSTest supplying the assembly. Removed public APIs in
  `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities`: `Message.Payload` (type
  `Newtonsoft.Json.Linq.JToken?`), `Serialization.DefaultTestPlatformContractResolver`,
  `Serialization.TestCaseConverter`, `Serialization.TestObjectConverter`,
  `Serialization.TestPlatformContractResolver<T>`, `Serialization.TestResultConverter`,
  `Serialization.TestRunStatisticsConverter`, and `VersionedMessage`.
- **Template engine packages drop `netstandard2.0`** (Preview 4) —
  `/compatibility/sdk/11/template-engine-netstandard`; `dotnet/sdk#54041`. Affected packages:
  `Microsoft.TemplateEngine.Abstractions`, `.Core`, `.Core.Contracts`, `.Edge`,
  `.Orchestrator.RunnableProjects`, `.Utils`, `.IDE`, and `Microsoft.TemplateEngine.TemplateLocalizer.Core`.
  They now target only **`net9.0`, `net11.0` and `net472`**. Reason: `NuGet.*` client packages stopped targeting
  `netstandard2.0` at version 7.0.
- **`AnalysisLevel=latest` corrected for .NET 11**: projects with `AnalysisLevel=latest` were incorrectly using
  .NET 9 analyzer rules instead of .NET 11 rules; this is fixed. Expect new diagnostics on projects that set
  `latest`.
- **`CA1873`** ("Avoid potentially expensive logging") reduced false positives (property accesses, `GetType()`,
  `GetHashCode()`, `GetTimestamp()` are no longer flagged; diagnostics apply only to Information-level logging and
  below by default) and now names one of nine reasons in the message (method invocation, object creation, array
  creation, boxing conversion, string interpolation, collection expression, anonymous object creation, await
  expression, with expression).
- Analyzer fixes: `CA1515` and `CA1034` false positives when **C# extension members** are present; `CA1859`
  improper handling of default interface implementations.
- New warning `NETSDK1235`: emitted when `PackAsTool=true` and a custom `NuspecFile` is specified. Pack still
  proceeds.
- `NU1703` warns for packages that use deprecated `MonoAndroid` framework assets; NuGet pack warns (`NU5052`) for
  package IDs with restricted characters.
- `dnx` scripts bypass `global.json` SDK selection; the mono launch target is no longer set for .NET Framework apps;
  local container runtime selection prefers `wslc` (Windows) and `container` (macOS) over Docker and Podman.

---

## 17. Open questions and unresolved conflicts

1. **Arm64 Windows minimum baseline.** The breaking-change page says the Windows Arm64 JIT/AOT minimum is unchanged
   at `armv8.0-a`; the what's-new runtime page says it is raised to `armv8.0-a + LSE`. Both carry `ms.date`
   2026-08-15 and `updated_at` 2026-08-19, so recency does not settle it. Recorded in §3.1.
2. **`System.Reflection.Metadata`.** No change to `System.Reflection.Metadata` (the `MetadataReader`/
   `MetadataBuilder` family) is documented anywhere in the .NET 11 breaking-change index, the what's-new library
   page, or the seven preview release notes. Whether the library is genuinely unchanged, or whether changes exist
   that are simply not documented in these sources, could not be established.
3. **Completeness.** Microsoft states the breaking-change article "is a work in progress. It's not a complete list of
   breaking changes in .NET 11." Preview 8, RC 1 and RC 2 are still ahead of GA, and the preview release notes have
   already carried breaking changes (`WindowLog` → `WindowLog2`, `TensorPrimitives.Clamp`, the
   `Microsoft.Extensions.Logging` internal-type move, `System.Security.Cryptography.Xml` mitigations, LDAP filter
   escaping, `ZipArchive` data descriptors, `MetadataLoadContext.CoreAssembly` nullability) that never reached the
   Learn index.
4. **`DSA` on Windows and Linux.** The removal is documented only for macOS. No source states any restriction or
   deprecation of finite-field DSA on Windows or Linux in .NET 11, but the absence of a statement is not proof.
5. **Runtime Async at GA.** `/whats-new/dotnet-11/runtime` calls Runtime Async "a preview feature" requiring
   `<Features>runtime-async=on</Features>`, while also stating the runtime libraries themselves ship compiled with it.
   Whether `runtime-async` becomes the default for user code at GA, or remains opt-in, is not stated.
6. **`System.Numerics.Decimal32/64/128` maturity.** `/compatibility/core-libraries/11/math-round-digits` references
   them as recommended alternatives, and `/whats-new/dotnet-11/libraries` documents them as shipping, but no source
   consulted says whether they are marked `[Experimental]` or `[RequiresPreviewFeatures]`.
7. **`ZipArchiveEntry` password support and `ZipEncryptionMethod`.** Documented in `/whats-new/dotnet-11/libraries`
   as shipping, but the exact enum members of `ZipEncryptionMethod` were not established from the sources consulted.
8. **Windows Arm64 `Assembly.GetCallingAssembly` / `StackTraceSupport` interaction with the `ILLink` feature switch
   defaults.** The `StackTraceSupport` feature switch defaults were not re-verified for .NET 11; whether NativeAOT
   publishing still defaults it to `false` in any SDK-provided profile is unknown.
