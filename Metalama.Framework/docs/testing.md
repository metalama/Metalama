# Testing Architecture

This document describes how `Metalama.Framework` is tested: the different test suites, the strategies behind them, and how to author and run each kind of test. It covers the whole spectrum, from fast in-memory unit tests to full `dotnet build` / MSBuild builds of real projects and simulated design-time (IDE) sessions.

Tests are orchestrated by PostSharp.Engineering through `Build.ps1 test`. The authoritative inventory of what is built and tested is the `Solutions` collection in [`eng/src/Program.cs`](../../eng/src/Program.cs).

## Test-suite inventory

| Suite | Location (`Metalama.Framework/src/tests/…`) | Strategy | Runner |
|---|---|---|---|
| **Unit tests** | `Metalama.Framework.Tests.UnitTests` | In-memory: build a `CompilationModel` / run pieces of the engine and assert. | xUnit |
| **Aspect tests** | `Metalama.Framework.Tests.AspectTests` (`.Internals`, `.PublicPipeline`) | File-based golden-file: `X.cs` input, `X.t.cs` expected transformed output. Many scenarios. | Custom xUnit framework (`Metalama.Testing.AspectTesting`) |
| **Template tests** | `Metalama.Framework.Tests.TemplateTests` | Same framework, template compilation + expansion in isolation. | Custom (own `TestRunnerFactory`) |
| **Linker tests** | `Metalama.Framework.Tests.LinkerTests` | Same framework, the aspect linker in isolation. | Custom (own `TestRunnerFactory`) |
| **Standalone tests** | `Standalone/` | Full `dotnet build`/`dotnet test`/MSBuild of real projects; assert via `test.json`. | `ManyDotNetSolutions` |
| **Design-time standalone** | `DesignTimeStandalone/` | Real projects driven through a simulated IDE design-time host. | `ManyDesignTimeSolutions` + `Metalama.DesignTime.HostSimulator` |
| **Analyzer tests** | `Metalama.Framework.Engine.Analyzers.Tests` | Roslyn analyzers that police Metalama's own source. | xUnit |
| **Workspace tests** | `Metalama.Framework.Tests.Workspaces` | `Metalama.Framework.Workspaces` against a real `MSBuildWorkspace`. | xUnit |
| **Benchmarks** | `Metalama.Framework.Tests.Benchmarks` | Performance, BenchmarkDotNet. | Exe |
| **Test app** | `Metalama.Framework.TestApp` | "It builds": exercises the real MSBuild targets end to end. | build-only |
| **Docker tests** | `docker/` | Container end-to-end (Windows and WSL/Linux). | CI-only PowerShell |

Two supporting projects are not test suites themselves but underpin the above: [`Metalama.Testing.UnitTesting`](../src/Metalama.Testing.UnitTesting) (the shared, packable unit-testing framework) and [`Metalama.Testing.AspectTesting`](../src/Metalama.Testing.AspectTesting) (the shared aspect-testing framework and custom xUnit runner). [`Metalama.AspectWorkbench`](../src/tests/Metalama.AspectWorkbench) is a WPF developer tool for authoring and debugging aspect tests.

## How tests run: `Build.ps1 test`

`Build.ps1` bootstraps `eng/src/Program.cs` (the `BuildMetalama` project), which constructs a PostSharp.Engineering `Product` and runs its engineering commands. `Build.ps1 test` clean-rebuilds each registered solution and runs its tests according to per-solution flags. The `Solutions` list (`eng/src/Program.cs`) is the source of truth. Key entries and flags:

- `Metalama.Framework/Metalama.Framework.sln` — the full solution is **built but its tests are not run** (`TestMethod = BuildMethod.None`, "too slow and redundant"). It carries `SupportsTestCoverage = true`.
- `Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf` (`IsTestOnly = true`) — this **solution filter is what actually runs the Framework tests**, against the latest Roslyn only. It excludes the `.4.12.0` variant projects for speed.
- `Metalama.Framework.TestApp.sln` — registered **twice**, once as a `DotNetSolution` and once as a `MsbuildSolution`, both `TestMethod = BuildMethod.Build` (build-only). The MSBuild duplicate exists "because there can be different errors".
- `ManyDotNetSolutions("…/Standalone")` and `ManyDesignTimeSolutions("…/DesignTimeStandalone")` — expand each scenario directory at run time (see below).
- Other product solutions (`Metalama.Backstage`, `Metalama.Patterns`, `Metalama.Extensions`, `Metalama.Migration`, `Metalama.LinqPad`) each carry their own tests.

Flag semantics: `IsTestOnly` = built only in the test phase (not packaged); `TestMethod = None` = build but do not run tests; `TestMethod = Build` = the test *is* that it builds; `SupportsTestCoverage` = collect coverage instrumentation for that solution.

**CI** is TeamCity, generated into `.teamcity/settings.kts`; each build type (`DebugBuild`/`ReleaseBuild`/`PublicBuild`) simply runs `Build.ps1 … test` in a Docker image and publishes `artifacts/testResults/**`. Two extra CI configurations (`DockerTestsWinX64`, `DockerTestsWslX64`) run `src/tests/docker/DockerTests.ps1`. Coverage is handled inside the engineering `test` command (coverlet), gated by `SupportsTestCoverage` and the `eng/Coverage.props` a project imports; there is no separate dotCover step.

## Roslyn-version variants (the `.4.12.0` pattern)

Metalama is loaded *inside* host processes (Visual Studio, the compiler) that provide a specific Roslyn version, so the engine ships per Roslyn API generation and its tests must run against each. Most test projects therefore have a `.4.12.0` sibling (`UnitTests`, `AspectTests`, `TemplateTests`, `LinkerTests`, `Benchmarks`, `UnitTestHelpers`) that recompiles the **same source** against Roslyn 4.12.0, while the un-suffixed project targets the latest (currently 5.0.0).

The variant shares source by globbing, not copying or a shared project. The entire `.4.12.0` csproj is a three-line shim:

```xml
<Project ToolsVersion="Current">
  <ItemGroup>
    <Compile Include="../Metalama.Framework.Tests.UnitTests/**/*.cs"
             Exclude="../Metalama.Framework.Tests.UnitTests/bin/**/*.cs;../Metalama.Framework.Tests.UnitTests/obj/**/*.cs" />
  </ItemGroup>
  <Import Project="../../../../eng/RoslynVersions/Roslyn.4.12.0.props" />
  <Import Project="../Metalama.Framework.Tests.UnitTests/Metalama.Framework.Tests.UnitTests.csproj" />
</Project>
```

`eng/RoslynVersions/Roslyn.4.12.0.props` sets `ThisRoslynVersion = 4.12.0`, `ThisRoslynVersionProjectSuffix = .4.12.0` (the leading period is required), and the `DefineConstants` `ROSLYN_4_12_0;ROSLYN_4_4_0_OR_GREATER;ROSLYN_4_8_0_OR_GREATER;ROSLYN_4_12_0_OR_GREATER;ROSLYN_4_12_0_OR_EARLIER`. The base csproj imports `eng/RoslynVersions/Latest.props` (which resolves to `Roslyn.5.0.0.props`, empty suffix) and is written entirely in terms of those variables: its `AssemblyName`, its `VersionOverride` on Roslyn packages, and its `ProjectReference`s (`Metalama.Framework.Engine$(ThisRoslynVersionProjectSuffix)`, etc.) all pick up the suffix. So importing the base csproj *from* the `.4.12.0` shim wires up the 4.12.0 engine; building it directly wires up the latest.

Source that must differ across Roslyn API levels uses the cumulative `ROSLYN_*_OR_GREATER` / `ROSLYN_4_12_0_OR_EARLIER` constants (hundreds of uses across the engine). Unit-test projects deliberately do **not** override `LangVersion` per variant, so assertions can use the latest language and tests branch on `ROSLYN_*_OR_GREATER` instead. The process for adding a new Roslyn version is documented in [updating-roslyn.md](updating-roslyn.md).

## Unit tests

**Projects.** [`Metalama.Framework.Tests.UnitTests`](../src/tests/Metalama.Framework.Tests.UnitTests) is the main xUnit assembly for the engine (hundreds of files mirroring the engine's areas: `CodeModel`, `CompileTime`, `DesignTime`, `Collections`, `Aspects`, …). [`Metalama.Framework.Tests.UnitTestHelpers`](../src/tests/Metalama.Framework.Tests.UnitTestHelpers) is a packable helper library of shared base classes (`DesignTimeTestBase`, `DiagnosticAnalyzerTestsBase`, `PreviewTestsBase`, `SerializationTestsBase`, …) and mocks (`TestWorkspaceProvider`, `TestDesignTimeAspectPipelineFactory`, …); it contains no `[Fact]`s. TFMs: `net48;net8.0` (framework `Metalama.Testing.UnitTesting`: `net472;net8.0`).

**Framework.** Unit tests inherit `UnitTestClass` ([`Metalama.Testing.UnitTesting/UnitTestClass.cs`](../src/Metalama.Testing.UnitTesting/UnitTestClass.cs)). It bootstraps Backstage once with a test license, routes xUnit output, and exposes `CreateTestContext()` (named from `[CallerFilePath]`/`[CallerMemberName]`). A [`TestContext`](../src/Metalama.Testing.UnitTesting/TestContext.cs) provides:

- `ServiceProvider` — a `ProjectServiceProvider` built over the **immutable** DI (not MS DI). Test doubles are threaded in through an `AdditionalServiceCollection` ([`Metalama.Framework.Engine/Services/AdditionalServiceCollection.cs`](../src/Metalama.Framework.Engine/Services/AdditionalServiceCollection.cs)) — override `ConfigureServices(IAdditionalServiceCollection)` to register mocks.
- `CreateCompilation(code)` / `CreateCompilationModel(code)` — build a Roslyn `CSharpCompilation` and wrap it as an `ICompilation` / internal `CompilationModel`.
- A per-test timeout (default 240 s, disabled under a debugger) and **enforced disposal** (the finalizer throws if a context was not disposed) — hence `using var testContext = …`.

Behavior is customized with the immutable `TestContextOptions` record (`Timeout`, `AdditionalMetadataReferences`, `RequireOrderedAspects`, `CodeFormattingOptions`, `ExtensionTypes`, …).

Minimal pattern:

```csharp
public sealed class InvokerTests : UnitTestClass
{
    [Fact]
    public void Methods()
    {
        const string code = "class TargetCode { void ToString(string format) {} }";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );
        var method = compilation.Types.Single().Methods.OfName( "ToString" ).Single();
        // assert with Xunit.Assert / the AssertEx helpers
    }
}
```

Some unit tests do run the real pipeline — e.g. `Aspects/AspectTestBase.cs` runs the `CompileTimeAspectPipeline` in memory. Common idioms (see `InvokerTests.cs`, `ExpressionFactoryTests.cs` as references):

- For template-expansion tests, use a `SyntaxSerializationContext` with `TemplateExpansionContext.WithTestingContext(ctx, serviceProvider)`.
- `using Microsoft.CodeAnalysis;` is needed for the `NormalizeWhitespace()` extension on `SyntaxNode`; without it only the `SyntaxToken` overload resolves.
- `TypedExpressionSyntaxImpl.Convert()` wraps casts in `ParenthesizedExpression()`, so the output is `((Type)expr)`, not `(Type)expr`.
- `AssertEx.DynamicEquals()` compares via the `IExpression.ToExpressionSyntax()` chain.
- Resolve types with `compilation.Factory.GetTypeByReflectionType(typeof(int))` (built-in) or `compilation.Types.OfName("A").Single()` (user-defined); compare with `compilation.Comparers.Default.Equals(a, b)`.

**Analyzer tests.** [`Metalama.Framework.Engine.Analyzers.Tests`](../src/tests/Metalama.Framework.Engine.Analyzers.Tests) (net8.0 only, no Roslyn variant) tests the internal Roslyn analyzers that police Metalama's own source (e.g. `KindCheckOptimizationAnalyzer`/`LAMA0860`). It does not use `UnitTestClass`; it builds raw `CSharpCompilation`s and runs `compilation.WithAnalyzers(...)`.

**Memory-retention tests.** `DesignTime/Pipeline/MemoryLeaks` holds the suite that asserts that a design-time editing session releases the versions of the project it has superseded. These tests simulate an editing session and assert on the liveness of weak references after a forced collection, so they follow conventions of their own: no compilation may reach a local of the test method, the collection must be forced in several rounds, and a failure reports the chain of fields that retains the object. The rules they enforce, and the reasons for those conventions, are in [`design-time-memory.md`](design-time-memory.md). Read it before extending the suite.

## Aspect tests

Aspect tests are the primary way the transformation behavior of the framework is verified. Each test is a **single C# file** placed anywhere under a project's `Tests/` tree; the framework compiles it through Metalama and compares the transformed output to a checked-in golden file.

### The `.cs` / `.t.cs` convention

The input file (`X.cs`) contains the aspect and a `TargetCode` class, with the member(s) under test marked `// <target>`. The expected transformed output lives in the sibling `X.t.cs`, which holds only the transformed target member(s), with diagnostics rendered as comments:

```csharp
// X.cs (input)                         // X.t.cs (expected output)
[Log]                                    private int Method(int a)
private int Method(int a)                {
{  // <target>                             global::System.Console.WriteLine("Entering");
    return a;                              return a;
}                                        }
```

Golden-file mechanics ([`Metalama.Testing.AspectTesting/BaseTestRunner.cs`](../src/Metalama.Testing.AspectTesting/BaseTestRunner.cs), `FileExtensions.cs`):

- The actual normalized output is written to `obj/transformed/<tfm>/…/X.t.cs` and compared with `Assert.Equal` against the checked-in `X.t.cs`. The expected file is never overwritten during a test run.
- If `X.t.cs` is **missing, it is auto-created with a placeholder** and the test fails — so the first run of a new test always fails and drops the file next to the input.
- To accept new output, run the `AcceptTestOutput` MSBuild target (it copies `obj/transformed/<tfm>/**/*.{cs,txt}` back over the source `.t.cs`/`.t.txt`). If `Metalama.Extensions.DiffEngine` is configured, a diff tool launches on a mismatch.
- Related snapshot extensions: `.i.cs` (introduced/generated trees, used by the design-time scenario), `.t.txt` (expected `Program.Main` console output), `.ct.cs` (compiled-template snapshot), `.cs.html` / `.t.cs.html` (syntax-highlighted renders). These are excluded from compilation and nested under the input file in the IDE by [`Metalama.Testing.AspectTesting.targets`](../src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets).

### Discovery and test naming

The aspect-test projects replace the default xUnit framework with `Metalama.Testing.AspectTesting.AspectTestFramework` (the `[assembly: TestFramework]` attribute and the metadata attributes that point at the source directory are injected by MSBuild at build time, not written in source). Discovery **walks the file system** rather than using reflection ([`XunitFramework/TestDiscoverer.cs`](../src/Metalama.Testing.AspectTesting/XunitFramework/TestDiscoverer.cs)): every `.cs` under the source dir becomes a test, except files under `bin`/`obj`, files starting with `_` (e.g. `_Runner.cs`), and directories excluded via `metalamaTests.json`.

**The test name is the file name without extension** (`TestCase.DisplayName`), and the synthetic xUnit "class" is the containing directory. This is why:

```bash
dotnet test <project> -f net8.0 --filter "ReplaceParameter_Covariant"
```

works with the bare file name, while `--filter "Name~…"` partial matches are unreliable. Use `--list-tests` to confirm discovery, and rebuild after adding a new `.cs` test file.

### Test directives

Per-test configuration is expressed with `// @Directive(args)` comments; directory-wide defaults live in `metalamaTests.json` files merged up the tree. Directives are parsed in [`TestOptions.cs`](../src/Metalama.Testing.AspectTesting/TestOptions.cs) and **must appear inside an `#if` block**; an unknown `@name` fails the test. Frequently used directives:

| Category | Directive | Effect |
|---|---|---|
| Scenario | `@TestScenario(Default\|CodeFix\|LiveTemplate\|LiveTemplatePreview\|DesignTime\|Preview)` | Select the runner/scenario (see below). |
| Scenario | `@AppliedCodeFixIndex(n)`, `@TargetSyntaxTreeSuffix(…)` | Pick the code fix / the preview tree. |
| Gating | `@Skipped(reason)` | Skip the test. |
| Gating | `@RequiredConstant(c)`, `@ForbiddenConstant(c)`, `@TargetFrameworks(net8.0;net472)` | Run only when a preprocessor symbol / TFM matches. |
| Diagnostics | `@IncludeAllSeverities` | Include hidden/info diagnostics, not just warnings and above. |
| Diagnostics | `@IgnoredDiagnostic(id)`, `@ClearIgnoredDiagnostics`, `@ExpectedException(type)` | Suppress specific IDs; expect the pipeline to throw. |
| Compilation | `@Include(path)`, `@AssemblyReference(name)`, `@DefinedConstant(c)` | Add another input file / reference / preprocessor symbol. |
| Compilation | `@LanguageVersion(v)`, `@LanguageFeature(f[=v])`, `@NullabilityDisabled`, `@OutputAssemblyType(Dll\|Exe)`, `@MainMethod(name)` | Shape the compilation. |
| Compilation | `@AllowCompileTimeDynamicCode`, `@RequireOrderedAspects` | Loosen compile-time checks / require ordered aspects. |
| Output | `@FormatOutput`, `@PreserveWhitespace`, `@OutputCompilationDisabled` | Format / compare whitespace / skip emitting the binary. |
| Output | `@WriteInputHtml`, `@WriteOutputHtml`, `@WriteCompiledTemplate` | Emit HTML / compiled-template snapshots (HTML needs the `HtmlWriter` plugin). |
| Execution | `@DisableExecuteProgram`, `@DisableCompareProgramOutput` | Control running/comparing `Program.Main`. |
| Debug | `@LaunchDebugger`, `@EnableLogging`, `@CheckMemoryLeaks`, `@Repeat(n)`, `@RandomSeed(n)` | Developer aids. |

`metalamaTests.json` additionally carries options not settable per file: `TestRunnerFactoryType` (plug a custom runner — this is how template/linker tests work), `Exclude`/`IsRoot` (stop the directory walk), `ReportErrorMessage`, `IgnoredDiagnostics`, `LicenseKeyProviderType`. The root config of `AspectTests` sets a broad `IgnoredDiagnostics` list (nullable/unused warnings), `MainMethod: "TestMain"`, `CheckMemoryLeaks: true`, and the test license provider.

### Scenarios

`@TestScenario` (and a project's own `TestRunnerFactoryType`) selects a runner ([`TestRunnerFactory.cs`](../src/Metalama.Testing.AspectTesting/TestRunnerFactory.cs)):

| Scenario | What it verifies |
|---|---|
| **Default** (`AspectTestRunner`) | Full compile-time pipeline: emits the binary, verifies syntax-tree structure, and on .NET 5+ executes `Program.Main`/`TestMain` and snapshots console output to `.t.txt`. Output compared as `.t.cs`. |
| **Diagnostics** | Same runner; the `.t.cs` also contains diagnostics as comments. Tune with `@IncludeAllSeverities` / `@IgnoredDiagnostic` / `@ExpectedException`. |
| **CodeFix** | Applies the Nth suggested code fix (`@AppliedCodeFixIndex`) and snapshots the result. |
| **DesignTime** | Runs the design-time pipeline and snapshots the introduced partial trees (`0.i.cs`, `1.i.cs`, …) plus design-time diagnostics/suppressions. |
| **Preview** | Snapshots what the IDE "preview transformed code" command shows (`@TargetSyntaxTreeSuffix` selects the tree). |
| **LiveTemplate / LiveTemplatePreview** | Applies an aspect as a live template to a single member (target marked with `TestLiveTemplateAttribute`). |
| **HTML** | `@WriteInputHtml`/`@WriteOutputHtml` produce syntax-highlighted `.cs.html` snapshots (needs `Metalama.Extensions.HtmlWriter`). |

Two cross-cutting capabilities apply across scenarios:

- **Cross-project / dependency tests**: a companion file `X.Dependency.cs` next to the input is compiled *through Metalama* into a separate assembly and referenced by the main compilation (recursion supported). `@DependencyDefinedConstant` / `@DependencyLanguageVersion` shape it.
- **Program-execution tests**: the transformed program's `Main`/`TestMain` is run and its output compared to `.t.txt` (toggle with `@DisableExecuteProgram` / `@DisableCompareProgramOutput`).

### AspectTests vs `.Internals` vs `.PublicPipeline`

All three share the same runner and directives; they differ in what they may reference:

- `Metalama.Framework.Tests.AspectTests` — tests that do **not** need `InternalsVisibleTo` from the engine, so they can run against the public/obfuscated build.
- `Metalama.Framework.Tests.AspectTests.Internals` — tests that require engine internals.
- `Metalama.Framework.Tests.PublicPipeline` — tests that exercise the public (non-internal) pipeline API path.

### AspectWorkbench

[`Metalama.AspectWorkbench`](../src/tests/Metalama.AspectWorkbench) is a WPF developer tool (not a test project) for interactively authoring and debugging a single `.cs`/`.t.cs` pair. It runs the same runners/pipeline and visualizes each stage side by side: colorized source, the annotated and compiled template, the transformed and intermediate-linker output, diagnostics, and program output.

## Template and linker tests

Both reuse the aspect-test framework (file-based discovery, `// @` directives, `.t.cs` snapshots) but supply their own runner via `TestRunnerFactoryType`:

- **Template tests** ([`Metalama.Framework.Tests.TemplateTests`](../src/tests/Metalama.Framework.Tests.TemplateTests)) test **template compilation and expansion in isolation**, not the full pipeline. A test file has a type `Aspect` with a `Template` method and a `TargetCode.Method`; the runner compiles and loads the compile-time template, expands it against the target, and snapshots the expanded body. It also asserts that the *annotated* template is textually identical to the input (the highlighting invariant).
- **Linker tests** ([`Metalama.Framework.Tests.LinkerTests`](../src/tests/Metalama.Framework.Tests.LinkerTests)) test **the aspect linker only**. The runner rewrites the input to synthesize linker transformations from special test constructs, runs `AspectLinker.ExecuteAsync`, and verifies inline linker assertions. See [linker-overview.md](linker-overview.md).

## Standalone tests

Standalone tests are **full builds of real projects** (`dotnet build`/`dotnet test`, or desktop MSBuild), catching defects that only manifest through the actual MSBuild targets, package references, compiler-hosted pipeline, and extension loading. Each immediate subdirectory of [`Standalone/`](../src/tests/Standalone) is one scenario; conventions are documented in [`Standalone/CLAUDE.md`](../src/tests/Standalone/CLAUDE.md). Scenarios must reference Metalama via `PackageReference`.

**Discovery** (`ManySolutions.ProcessDirectory` in PostSharp.Engineering) picks the first matching entry point per directory and stops (no recursion once found):

1. `*.proj` → `BuildMethod.Build` (custom orchestration; built via its `Build` target, never "run")
2. `*.sln` / `*.slnx` → `BuildMethod.Test`
3. `*.csproj` → `BuildMethod.Test`
4. `Program.cs` → `BuildMethod.Test` (a single-file program is `dotnet run`)

Every scenario is built; those with `BuildMethod.Test` are additionally run. Scenarios run concurrently (a semaphore of `ProcessorCount`); any failure fails the set. Examples: `SingleFile` (single-file program, run), `CompileTimeContract` (the SDK-extension pattern: `[CompileTime]` contracts + `MetalamaExtensionAssembly` + `MetalamaCompileTimeAssembly`), `TestWeaver` (a custom weaver plug-in), `CodeCoverage` (a real xUnit project, run via `dotnet test`), `BlazorApp`, and regression scenarios like `Issue1743` / `Issue1749` / `Issue1741`.

### `test.json`

Place a `test.json` next to the scenario entry point to assert an outcome other than "builds and runs cleanly". The schema is `TestOptions` in PostSharp.Engineering; the diagnostic matching is in `TestableSolution.EvaluateOutput`. Fields:

- `IgnoreExitCode` (bool) — do not fail on a non-zero exit code (required when the build is *expected* to fail).
- `ExpectedDiagnosticsRegexes` (string[]) — each regex must match at least one diagnostic, else fail.
- `ForbiddenDiagnosticsRegexes` (string[]) — each regex must match no diagnostic. Prefer this over `FailOnUnexpectedDiagnostics` for "must not appear".
- `FailOnUnexpectedDiagnostics` (bool) — fail if any diagnostic is not matched by an expected regex. Brittle (fires on incidental warnings such as `NU1902`); usually leave off.
- `ErrorRegexes` (string[]) — matched against the whole output, but only when the build **succeeded**; a match is a failure (catches a build that passed but should not have).
- `BuildOnly` (bool) — build but do not run.
- `Target` (string) — MSBuild target; honored only by the MSBuild engine, ignored by `dotnet`.
- `Properties` (dictionary) and `Matrix` (array) — see below.

Diagnostic matching keeps only output lines containing the literal `: error ` or `: warning ` (the canonical MSBuild format, portable across `dotnet` and MSBuild.exe) and applies each regex case-insensitively against the **whole line**. A regex may therefore match a code (`LAMA0077`) or message text; a code-less MSBuild `<Error>` is matched on its message. The `Matrix` (`TestMatrixEntry[]`) builds the scenario once per entry, asserting each independently (each field falls back to the top-level value; log files are suffixed per entry). Example — a build expected to fail with a specific error:

```json
{
    "IgnoreExitCode": true,
    "ExpectedDiagnosticsRegexes": [ "does not support the legacy Razor build" ],
    "BuildOnly": true
}
```

When `dotnet build` cannot express the orchestration (e.g. a `dotnet`-built library consumed by a desktop-MSBuild app, or a cache-busting salt), a scenario uses a `*.proj` wrapper with a `<Target Name="Build">` instead (e.g. `Issue31024.proj`). Prefer `test.json` where possible, and add a `README.md` explaining why a failure outcome is expected.

## Design-time standalone tests and the host simulator

[`DesignTimeStandalone/`](../src/tests/DesignTimeStandalone) reproduces defects that only appear at **design time** (in the IDE), which a batch build cannot reach: cross-project pipeline-cache reuse, per-document analyzer requests, and loading multiple Metalama versions into one process.

These scenarios are driven by a Metalama-repo-local engine, `ManyDesignTimeSolutions` ([`eng/src/ManyDesignTimeSolutions.cs`](../../eng/src/ManyDesignTimeSolutions.cs)), which subclasses `ManySolutions` (same discovery/scheduling) but, instead of asserting on a `dotnet build`, invokes the **host simulator**. Its `DesignTimeSolution` restores the scenario, runs a throwaway `dotnet build -p:UseSharedCompilation=false` only so file-referenced assemblies exist, then runs `Metalama.DesignTime.HostSimulator` over the solution. Assertions come from `test.json` **identically** to standalone tests, because the simulator writes diagnostics in canonical MSBuild format.

[`Metalama.DesignTime.HostSimulator`](../src/tests/Metalama.DesignTime.HostSimulator) is a command-line program that simulates what an IDE does at design time (documented thoroughly in its `README.md`). It deliberately **does not reference Metalama**: it hosts Metalama the way an editor does, through each project's restored analyzer references loaded from disk (via a per-directory `AssemblyLoadContext`, mimicking Roslyn's `DirectoryLoadContext`), so it exercises whatever Metalama version each project references — including two versions in one solution. It opens the solution in an `MSBuildWorkspace` configured like a real design-time build (`DesignTimeBuild=true`, `BuildingInsideVisualStudio=true`), runs source generators, and issues per-document syntax and semantic analyzer requests as an open editor would. It fails on an analyzer that threw (`AD0001`), an unhandled Metalama exception (`LAMA0001`), any Metalama error, or an analyzer assembly that failed to load (exit `0` = ok, `1` = failure, `2` = usage). Options include traversal order, `--permutations` (each order in a fresh child process, since analyzer load contexts are non-collectible), `--property`, and `--timeout`. It does **not** simulate Visual Studio's two-process RPC split (it loads analyzers in-process, like Rider/OmniSharp), incremental editing, or Roslyn's concurrent scheduling. See [cross-process-communication.md](cross-process-communication.md) for the real design-time architecture.

## Other test projects

- [`Metalama.Framework.TestApp`](../src/tests/Metalama.Framework.TestApp) — a small solution (an `Exe`, its aspects, a library, a test-runner) whose whole test is that **it builds** through the real MSBuild targets (with `MetalamaEmitCompilerTransformedFiles`/`MetalamaDebugTransformedCode` on). Registered twice, once per build engine.
- [`Metalama.Framework.Tests.Workspaces`](../src/tests/Metalama.Framework.Tests.Workspaces) — xUnit tests for `Metalama.Framework.Workspaces` against a real `MSBuildWorkspace` (hence a .NET SDK is required at runtime). Uses the *public* `Microsoft.CodeAnalysis.Workspaces.MSBuild` packages, pinned separately from the Metalama.Compiler fork.
- [`Metalama.Framework.Tests.Benchmarks`](../src/tests/Metalama.Framework.Tests.Benchmarks) — BenchmarkDotNet performance suite. Also pins *public* Roslyn packages (`BenchmarkRoslynVersion`).
- `src/tests/docker/` — container end-to-end tests: `DockerTests.ps1` iterates subdirectories under a target (`win-x64`, `linux-x64`), each a `Dockerfile` + `test.ps1`, run via `DockerBuild.ps1` (optionally WSL). Scenarios include `CompilerLogs`, `MetalamaKill`, `NonStandardDotNetRoot`. Run only in the two docker CI configurations.
- `src/tests/RunManually/` (manually-run multi-Roslyn-version repros), `src/tests/Deprecated/` (the deprecated reactive layer), `src/tests/Utilities/` (dev tools such as `SyntaxCover`) — not part of the automated run.

## Tests in other product solutions

Each product solution ships its own tests, discovered by the same conventions:

- **Metalama.Backstage** — `Metalama.Backstage.Tests`, `…Commands.Tests`, `…Worker.Tests`, plus the packable `Metalama.Backstage.Testing` helper. Coverage enabled at the solution level.
- **Metalama.Patterns** and **Metalama.Extensions** — a mix of `*.UnitTests` (xUnit) and `*.AspectTests` that reuse the core `Metalama.Testing.AspectTesting` framework and directives (which is why those solutions carry `FormatExclusions` for the test payloads).

## Shared configuration

- [`src/tests/Directory.Build.props`](../src/tests/Directory.Build.props) — sets `IsPackable=False`, `Deterministic=False` (avoids the deterministic-source-paths error), `AddAssemblyMetadataAttributes=False` for the whole test tree.
- [`src/tests/Directory.Build.targets`](../src/tests/Directory.Build.targets) — copies the shared `xunit.runner.json` next to every test assembly.
- [`src/tests/xunit.runner.json`](../src/tests/xunit.runner.json) — `parallelizeAssembly: true`, `shadowCopy: false` (shadow copying is incompatible with the public-signed test assemblies), `diagnosticMessages: true`.
- `eng/Coverage.props` — a project imports this to opt into coverlet coverage (with assembly/namespace exclusions), collected when the engineering `test` command runs with coverage enabled.

## Choosing a test suite

| You are testing… | Use |
|---|---|
| A code-model API, a serializer, an internal algorithm | **Unit test** (`Metalama.Framework.Tests.UnitTests`) |
| That an aspect produces the expected transformed code / diagnostics / runtime output | **Aspect test** (`Metalama.Framework.Tests.AspectTests`, default scenario) |
| Design-time behavior (introduced trees, previews, code fixes, live templates) | **Aspect test** with the matching `@TestScenario` |
| Template compilation/expansion or the linker in isolation | **Template test** / **Linker test** |
| MSBuild targets, packaging, real package references, cross-engine behavior | **Standalone test** (`+ test.json`) |
| A defect that only appears in the IDE / design-time host | **Design-time standalone test** (host simulator) |
| A performance regression | **Benchmark** |

See also [pipeline.md](pipeline.md), [compilation-model.md](compilation-model.md), [linker-overview.md](linker-overview.md), and [updating-roslyn.md](updating-roslyn.md).
