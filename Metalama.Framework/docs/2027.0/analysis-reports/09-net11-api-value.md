# 09. Is there a .NET 11 application programming interface that Metalama wants?

## Question

Decision 6 of `Metalama.Framework/docs/2027.0/DECISIONS.md:57-64` states that adding `net11.0` beside `net10.0`
in every test project is not justified unless there is a .NET 11 application programming interface that Metalama
wants to use, and that the question must be answered before any test matrix story is written. Decision 6b
(`DECISIONS.md:66-81`, added 2026-09-04) further removes the build container work from the release scope and
names two defects that must be fixed without an installed .NET 11 SDK. This report answers the interface
question and then separates what genuinely requires a .NET 11 SDK or runtime from what does not.

## Answer

No .NET 11 application programming interface justifies a `net11.0` asset or a `net11.0` test leg.

The .NET 11 library additions are concentrated in numeric types, domain name resolution, compression, process
management, text and vector intrinsics. None of them lies on a path that Metalama uses. Metalama's production
source contains no polyfill file and no conditional above `NET8_0_OR_GREATER`; every compatibility shim it has
serves `netstandard2.0` and `net472`, and a `net11.0` asset would remove none of them. The two additions with any
adjacency, `System.Runtime.CompilerServices.UnionAttribute` and `AssemblyLoadContext.SetAssemblyLocationOverride`,
are both reachable without referencing .NET 11, for reasons given below. The two cryptography issues, #1860 and
#1864, need a .NET 11 runtime on macOS as a test host and explicitly not a .NET 11 target framework.

Because no interface is wanted, the build container change has no justification either, which agrees with
decision 6b rather than merely accepting it.

## What .NET 11 adds (from primary sources)

.NET 11 is a standard-term support release supported from 2026-11-10 to 2028-11-09
(https://github.com/dotnet/core/tree/main/release-notes/11.0). Seven previews were published between 2026-02-10
and 2026-08-11 (https://raw.githubusercontent.com/dotnet/core/main/release-notes/11.0/README.md). There is no
cumulative `api-diff` directory at the release level; each preview carries its own, for example
https://github.com/dotnet/core/tree/main/release-notes/11.0/preview/preview7/api-diff with one directory per
shared framework and one Markdown file per assembly.

The library additions, taken from the seven `libraries.md` files
(https://raw.githubusercontent.com/dotnet/core/main/release-notes/11.0/preview/preview<N>/libraries.md for N in 1
to 7), group as follows.

- Numeric types. `System.Numerics.BFloat16` (preview 1); `Decimal32`, `Decimal64`, `Decimal128`,
  `IDecimalFloatingPointIeee754<TSelf>`, `Complex<T>` and `INumberBase<TSelf>.TryParsePartial` (preview 7, and in
  the `System.Runtime` diff at
  https://raw.githubusercontent.com/dotnet/core/main/release-notes/11.0/preview/preview7/api-diff/Microsoft.NETCore.App/11.0-preview7_System.Runtime.md).
- Domain name resolution. `System.Net.DnsResolver`, `Dns.ResolveSrv`, `ResolveMx`, `ResolveTxt` and the record
  types (preview 7).
- Compression. Zstandard moved into `System.IO.Compression` (previews 1 and 3), ZIP encryption with
  `ZipEncryptionMethod` and password overloads (preview 7), `ZLibEncoder` and `ZLibDecoder` (preview 4).
- Process management. `Process.Run`, `RunAsync`, `RunAndCaptureText`, `ProcessStartInfo.KillOnParentExit`,
  `StartDetached`, `InheritedHandles`, `SafeProcessHandle.Start`, `Kill`, `Signal`, `WaitForExit` (preview 4);
  `ProcessStartInfo.StartSuspended`, `SafeProcessHandle.Resume`, `Process.TryGetProcessById` (previews 6 and 7).
- Text. `Rune` support across `String`, `StringBuilder` and `TextWriter` (preview 1); `char.ToUpperOrdinal`,
  `string.ToLowerOrdinal`, `MemoryExtensions.ToUpperOrdinal` (preview 7); `RegexOptions.AnyNewLine` (preview 3).
- Collections and LINQ. `Enumerable.FullJoin`, `Queryable.FullJoin`, `EqualityComparer<T>.Create` with a key
  selector, `StringBuilder.MoveChunks` (preview 5); `ReadOnlySpan<T>.Min` and `Max` (preview 7).
- Streams. `ReadOnlyMemoryStream`, `WritableMemoryStream`, `ReadOnlySequenceStream`, `StringStream` (preview 6).
- Serialization. `JsonTypeInfoKind.Union`, `JsonUnionAttribute`, `JsonTypeClassifier`,
  `JsonSerializerOptions.TypeClassifiers` (preview 6); `JsonSerializerOptions.InferClosedTypePolymorphism`
  (preview 7); `JsonNamingPolicy.PascalCase` (preview 3).
- Cryptography. `HMACSHA256.Verify` and the whole `Verify` family plus
  `CryptographicOperations.VerifyHmac` (preview 1); `X25519DiffieHellman` and
  `CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, byte)` (preview 5).
- Reflection and loading. `System.Reflection.Metadata.MetadataLoadContext.GetLoadContext(Assembly)` (preview 4);
  `Type.GetNullableUnderlyingType()` (preview 5); `AssemblyLoadContext.SetAssemblyLocationOverride` (preview 7,
  and the only addition in
  https://raw.githubusercontent.com/dotnet/core/main/release-notes/11.0/preview/preview7/api-diff/Microsoft.NETCore.App/11.0-preview7_System.Runtime.Loader.md).
- Language runtime support. `System.Runtime.CompilerServices.UnionAttribute` and
  `System.Runtime.CompilerServices.IUnion` (preview 4), which support the C# 15 union feature.

The runtime changes of preview 7
(https://raw.githubusercontent.com/dotnet/core/main/release-notes/11.0/preview/preview7/runtime.md) are internal:
asynchronous method tiering and tail-await optimization, WebAssembly work, code generation improvements and
native ahead-of-time compilation improvements. None of them is a programming interface.

One breaking change is directly relevant and is documented outside these files: finite field DSA was removed from
macOS. The reference is
https://learn.microsoft.com/en-us/dotnet/core/compatibility/cryptography/11/dsa-removed-macos, cited in the body
of metalama/Metalama#1860; the page itself could not be fetched, see "What could not be verified".

## What Metalama would gain (per candidate, with a verdict)

### Candidate 1. `System.Runtime.CompilerServices.UnionAttribute` and `IUnion`. Verdict: not worth it.

This is the only .NET 11 addition that touches a feature Metalama is committed to. Decision 3
(`DECISIONS.md:28-33`) puts unions in scope as aspect targets, so Metalama has to recognise a union.

It gains nothing from a `net11.0` target. Recognition of a union goes through Roslyn, whose `ITypeSymbol.IsUnion`
and `ITypeSymbol.IsClosed` already exist in the consumed Roslyn (FACTS.md:53), and decision 2
(`DECISIONS.md:12-22`) routes those through the Roslyn variant mechanism rather than through a target framework.

If Metalama ever has to classify the two runtime types by name, the mechanism that would do so is already
independent of the target framework. `Metalama.Framework.Engine/CompileTime/SymbolClassifier.cs:41-80` builds
`_wellKnownTypes` as a dictionary keyed by a simple name and a namespace string; the `typeof(...)` expressions at
lines 46 to 77 are only a convenient way to obtain those two strings, and line 79 already adds one entry,
`("_Attribute", "System.Runtime.InteropServices", null)`, as a literal pair with no reference to the type. An
entry for `UnionAttribute` or `IUnion` follows that form and compiles on `netstandard2.0`.

The compile-time compilation cannot use these types in any case. `Metalama.Framework/docs/compile-time-target-frameworks.md:22-25`
states that the compile-time assembly is always compiled against the `netstandard2.0` reference assemblies,
regardless of the run-time target framework of the consuming project.

### Candidate 2. `AssemblyLoadContext.SetAssemblyLocationOverride`. Verdict: not worth it.

Metalama loads compile-time assemblies into a collectible `AssemblyLoadContext`
(`Metalama.Framework.Engine/CompileTime/UnloadableCompileTimeDomain.cs:38,51`), which is the kind of code the new
hook addresses. It solves a problem Metalama does not have: the compile-time assemblies are loaded from a file
path, so `Assembly.Location` is already populated, as the logging at
`Metalama.Framework.Engine/CompileTime/CompileTimeDomain.cs:90` and the resolution at
`Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:207,304,339` show. No comment or issue in
either repository records an empty `Assembly.Location` as a defect.

### Candidate 3. Cryptography, issues #1860 and #1864. Verdict: a .NET 11 test host, never a .NET 11 target.

Issue metalama/Metalama#1860 reports that `DSA.Create(DSAParameters)` throws `PlatformNotSupportedException` on
macOS with .NET 11, from `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/CryptographyHelper.cs:21`,
which is inside a `#if NET472 || NET5_0_OR_GREATER` block at line 20. The failure occurs while the licensing
services are registered, so every build of a Metalama Premium component fails on that platform.

Issue metalama/Metalama#1864 proposes the fix and settles the target framework question in its own text: it
states that "`ECDsa.Create(ECParameters)`, `SignHash` and `VerifyHash` are available on all three target
frameworks of `Metalama.Backstage`, which are `netstandard2.0`, `netframework4.7.2` and `net8.0`". The current
declaration is `netframework4.7.2;net10.0;netstandard2.0`
(`Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj:4`), which only strengthens the point. The
replacement algorithm therefore compiles into the existing assets and needs no new one.

What the two issues do need is a .NET 11 runtime on macOS to prove the fix. Issue #1860 records that this cell is
already covered outside this repository, by the platform matrix of `metalama/Metalama.Tests.DotNetSdk`, which is
where the failure was found and where it is the only failing cell of forty-one. That is a runtime host
requirement, not a target framework requirement and not a `net11.0` leg in this repository's test projects.

### Candidate 4. Performance interfaces. Verdict: nothing to gain, because the predecessors are unused.

A newer collection or search interface pays only where an older one is already on a hot path. Metalama uses
almost none of them. A search over `Metalama.Backstage/src`, `Metalama.Framework/src`, `Metalama.Patterns/src`
and `Metalama.Extensions` finds zero files naming `FrozenDictionary`, `FrozenSet`, `SearchValues` or
`AlternateLookup`. `ArrayPool` appears in one production file,
`Metalama.Framework.DesignTime/ProjectKeyFactory.cs:130,158`. `System.Threading.Lock` and `MemoryExtensions`
appear only in aspect test baselines
(`Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp13/LockType.t.cs:16`,
`Tests/Aspects/Misc/IndexAndRange.t.cs:24`). .NET 11 adds no member to any of these types in the first place; the
nearest additions are `ReadOnlySpan<T>.Min` and `Max` and `EqualityComparer<T>.Create` with a key selector, which
have no caller here.

### Candidate 5. Removing a polyfill or a compatibility shim. Verdict: nothing to remove.

There is no file in either repository whose name contains "polyfill". Counting the conditional compilation
symbols over the four production source trees gives 122 uses of `NET5_0_OR_GREATER`, 103 of `NET6_0_OR_GREATER`,
50 of `NET8_0_OR_GREATER`, 21 of `NET7_0_OR_GREATER` and 21 of `NETFRAMEWORK`. `NET9_0_OR_GREATER` appears nine
times and `NET10_0_OR_GREATER` once, and every one of those ten is in test code: six in aspect tests under
`Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp13/` and the rest in a hard-coded symbol list at
`Metalama.Framework/src/tests/Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:43-44`. No production source
file branches on .NET 9 or .NET 10 at all, which means no shim is waiting for a newer runtime.

The shims that do exist are written the other way round, as `#if !NET6_0_OR_GREATER` or `#if NET472`, and serve
the `netstandard2.0` and `net472` assets. Examples are
`Metalama.Framework.Engine/Collections/PortableEnumerableExtensions.cs:5`,
`Collections/DictionaryExtensions.cs:11`, `Collections/ConcurrentDictionaryExtensions.cs:16`,
`Collections/LinqExtensions.cs:519`, `Collections/KeyValuePairExtensions.cs:5` and
`Utilities/ObjectGraph/ObjectGraphTypeReader.cs:8`. Adding a `net11.0` asset removes none of them, because the
`netstandard2.0` asset must keep existing.

The two comments in the repositories that name a framework limitation confirm the same floor.
`Metalama.Backstage/src/Metalama.Backstage/Tools/BackstageToolsExecutor.cs:83-88` reimplements the argument
quoting of the .NET `PasteArguments` because `ProcessStartInfo.ArgumentList` "is not available on all target
frameworks", which means `netstandard2.0` and `net472`, not .NET 10.
`Metalama.Framework.Engine/Utilities/ObjectGraph/ObjectGraphTypeReader.cs:191` records that `IsByRefLike` "is not
available on .NET Framework". Neither is relieved by .NET 11.

### Which assemblies could even carry a `net11.0` asset

None usefully. Every packable project targets `netstandard2.0`, `net472` and `net10.0` in some combination:
`Metalama.Framework.Engine.csproj`, `Metalama.Framework.DesignTime.csproj`,
`Metalama.Framework.Introspection.csproj` and `Metalama.Testing.AspectTesting.csproj` are `net472;net10.0`;
`Metalama.Framework.csproj:4` and `Metalama.Extensions.Multicast.csproj` are `netstandard2.0;net10.0`;
`Metalama.Backstage.csproj:4` is `netframework4.7.2;net10.0;netstandard2.0`; every `Metalama.Patterns` package is
`net472;net10.0;netstandard2.0`; `Metalama.Framework.Sdk`, `Metalama.SourceTransformer`,
`Metalama.Framework.CompileTimeContracts` and the analyzer projects are `netstandard2.0` alone;
`Metalama.Framework.Workspaces.csproj:18` and `Metalama.Tool.csproj` are `net10.0`. In Metalama.Premium no
project mentions `net11` in any file, and the packable projects are still on `net8.0` pending pull request
metalama/Metalama.Premium#85.

Two mechanisms make a `net11.0` asset inert even if one were produced. First, the design-time payload loads into
hosts whose private runtime is .NET 10: `Metalama.Framework/docs/platform-support.md:144-157` derives the Core
flavour `net10.0` from Roslyn's own target framework strategy, so no host in PB-2027.0 runs a .NET runtime above
10 for that payload. Second, the extension assembly selection is a literal:
`Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs:20` and
`Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs:31` both compute the target framework as
`RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework") ? "net472" : "net10.0"`, so a
`MetalamaExtensionAssembly` item declared for `net11.0` is never selected on any host.

For a user project that targets `net11.0`, NuGet selects the `net10.0` asset, which is the intended behaviour and
is what `platform-support.md:211-213` and `:338-339` already record.

## What must be tested on .NET 11 regardless

Three things must be distinguished, and within the first the coordinator's split between what needs a real
toolchain and what does not is the deciding one.

### 1. The .NET 11 SDK as a build host

Two defects belong here, and neither needs an installed .NET 11 SDK, because each is a property of a version
comparison or an MSBuild condition and can be exercised by setting the input property directly.

The spurious `LAMA0601`. `Metalama.Framework.Package/build/Metalama.Framework.props` declares
`<MaximumSdkVersion>11.0</MaximumSdkVersion>` in the `MetalamaPlatformRequirement` item, and
`build/Metalama.Framework.targets:399` computes `_MetalamaSdkVersion` as
`$(NETCoreSdkVersion.Split('-')[0])`, that is the full three-part version with only the prerelease suffix
removed. The warning at lines 410 to 413 then fires when
`$([MSBuild]::VersionGreaterThan($(_MetalamaSdkVersion), 11.0))`. A released .NET 11 SDK reports `11.0.100`, which
is greater than `11.0`, so every build with a supported .NET 11 SDK reports that the SDK is newer than the
maximum. The condition takes `NETCoreSdkVersion` as its only input, so a test that sets that property to
`11.0.100` and asserts that no `LAMA0601` is raised proves the fix without any SDK being installed. The floor
comparison at line 407 is unaffected, because `VersionLessThan("10.0.400", "10.0")` is false.

The language version clamp. `build/Metalama.Framework.targets:118-120` rewrites `LangVersion` to `12.0` whenever
`LangVersionImplicitlySet` is `True` and `LangVersion` is not one of `12.0`, `13.0`, `14.0`, `default`, `latest`,
`latestMajor` or `preview`. A `net11.0` project whose implied version is `15.0` is therefore rewritten to `12.0`,
and `MetalamaCheckLangVersion` at lines 243 to 249 emits a warning whose text says the version was raised because
`12.0` is the lowest supported version, which is false in this direction. The condition reads only
`LangVersionImplicitlySet` and `LangVersion`, both ordinary properties, so a test that sets them to `True` and
`15.0` proves the fix without a .NET 11 SDK. Whether Metalama.Compiler actually marks a `net11.0` project as
implicitly set is a separate question that is not answerable from this repository (assumption A1 of
`analysis-reports/01-language-version-and-hosts.md:222`).

What genuinely requires an installed .NET 11 SDK is only the end-to-end confirmation: a standalone scenario that
runs `dotnet build` under it, and any test project or scenario project that declares `net11.0`, because the .NET
10 SDK rejects a higher target framework before restore. Decision 6b puts that work outside the release scope,
and since no .NET 11 interface is wanted, this report finds no evidence that would reopen it.

### 2. The .NET 11 runtime as a host of the Metalama tools and the design-time process

This is the only category that needs a real .NET 11 runtime, and the need is already met outside this repository.

The macOS licensing failure of #1860 is a runtime behaviour, not a compile-time one, and it is reproducible only
on a real .NET 11 runtime on macOS. Issue #1860 records that the platform matrix of
`metalama/Metalama.Tests.DotNetSdk` covers exactly that cell. The fix of #1864 has to be verified there, and
#1864 already says so. Nothing about it argues for a `net11.0` leg in this repository.

Two further host-runtime facts are worth recording, because they bear on which processes reach .NET 11 at all.
`Metalama.Framework/docs/platform-support.md:338-339` states that the `net10.0` compiler "declares
`rollForward=Major` and runs on .NET 11". The repository's own verification data contradicts the general form of
that statement: `analysis-reports/verification-verdicts.json` records that the `Major` policy rolls forward to a
higher major version only when no runtime of the requested major version is installed, so a `net10.0` application
runs on .NET 10 whenever a .NET 10 runtime is present. `Metalama.Tool.csproj:37`,
`Metalama.Backstage.DotNetTool.csproj:17`, `Metalama.Backstage.Worker.csproj:13,25` and
`Metalama.Backstage.Desktop.Windows.csproj:6` all declare `RollForward=Major`. The consequence is that the tools
reach the .NET 11 runtime only on a machine that has no .NET 10 runtime, which is rarer than the sentence in
`platform-support.md` implies. This is a documentation correction, not an argument for a test leg.

The design-time payload does not reach .NET 11 in a supported configuration at all, because its host runtime is
.NET 10 (`platform-support.md:144-157`).

### 3. `net11.0` as a user target framework served by the existing assets

This is a NuGet asset selection property and involves no Metalama code. A `net11.0` consumer resolves the
`net10.0` asset of every package, and `net11.0-windows` resolves the `net10.0-windows` asset of
`Metalama.Patterns.Wpf`. Proving it requires building a small consumer project, which needs a .NET 11 SDK and
targeting pack, and is again what `metalama/Metalama.Tests.DotNetSdk` exists for. It does not require this
repository's test projects to gain a leg, because the code under test is identical on both legs: the assembly
executed on `net11.0` is the `net10.0` assembly.

Two consequences of leaving the matrix alone should be stated so that they are accepted rather than overlooked.
The `net11.0` and `net11.0-windows` entries are already named and deliberately excluded in
`Metalama.Framework/src/tests/Standalone/SupportedPlatform.TestedTargetFrameworks/SupportedPlatform.TestedTargetFrameworks.csproj:8`,
so the exclusion is recorded where a reader will find it. And the aspect test baselines under
`obj/transformed/<tfm>` are shared across legs, so if a leg were ever added, any baseline whose content depends
on the base class library would have to be split with `@TargetFrameworks`. Not adding the leg avoids that cost
entirely.

## What could not be verified

- The .NET 11 what-is-new documentation at
  https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview could not be fetched. The network
  egress proxy blocks the whole of `learn.microsoft.com`, reported as `EGRESS_BLOCKED`. The same block prevents
  reading the DSA removal breaking change page at
  https://learn.microsoft.com/en-us/dotnet/core/compatibility/cryptography/11/dsa-removed-macos. The content of
  that page is taken from the body of metalama/Metalama#1860, which quotes it and gives the reason (the macOS
  `SecurityTransforms` library is obsolete and has no replacement, and only finite field DSA is affected).
- There is no cumulative `api-diff` for .NET 11 as a whole, only one per preview. The per-preview `libraries.md`
  files were used as the primary list and two preview 7 diff files were read in full (`System.Runtime` and
  `System.Runtime.Loader`). The remaining sixteen diff files of preview 7 and the diffs of previews 1 to 6 were
  not read one by one. A small addition to an assembly that no Metalama file references could therefore be
  missing from the list above, but it would not change the verdict, because the verdict rests on the internal
  finding that no Metalama code branches above `NET8_0_OR_GREATER` and that no shim is waiting for a newer
  runtime.
- Metalama.Compiler is not cloned (FACTS.md:6). Whether it marks a `net11.0` project with
  `LangVersionImplicitlySet` and what value it puts in `LangVersion` is an assumption, recorded as A1 in
  `analysis-reports/01-language-version-and-hosts.md:222`. It affects which of the two language version defects
  fires first, not whether a `net11.0` asset is needed.
- No .NET 11 SDK or runtime is installed in this environment, and no build or test was run, as instructed. Every
  internal claim is from source inspection.
