# Gap 4 — Source-generator driver behaviour in the .NET 11 wave

Stage ordering, visibility, caching, on-disk output, analyzer discovery.

Research date: 2026-09-03. All statements below are verified against primary sources
(dotnet/roslyn source and `docs/features`, dotnet/sdk source, NuGet/NuGet.Client source,
learn.microsoft.com). Source snapshots were downloaded to
`C:\Users\GaelFraiteur\AppData\Local\Temp\claude\C--src-Metalama-2027-0-Metalama\86248111-7c7e-4f30-bf61-ae10afe3e5e4\scratchpad\net11\rosrc\`.

---

## 0. Version mapping (established, not assumed)

| Branch | `MajorVersion.MinorVersion` (eng/Versions.props) | `RegisterPreCompilationSourceOutput` present? |
|---|---|---|
| `dotnet/roslyn` `release/dev18.0` | 5.0 | no |
| `dotnet/roslyn` `release/dev18.3` | 5.3 | no |
| `dotnet/roslyn` `release/stable` | 5.10 | **yes** |
| `dotnet/roslyn` `release/insiders` | 5.11 | **yes** |
| `dotnet/roslyn` `main` | **5.12** | **yes** |

`dotnet/sdk` `release/11.0.1xx` (`eng/Version.Details.xml`) pins
`Microsoft.Net.Compilers.Toolset` **5.12.0-1.26451.112**; `dotnet/sdk` `main` pins
`5.12.0-1.26451.109`. So **the .NET 11 SDK ships Roslyn 5.12.x**, and the
pre-compilation feature is inside the .NET 11 GA compiler.

The API sits in `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt` (not `.Shipped.txt`) and
every member is annotated `[RSEXPERIMENTAL007]`:

```
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(Microsoft.CodeAnalysis.IncrementalValueProvider<TSource> source, System.Action<Microsoft.CodeAnalysis.PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(Microsoft.CodeAnalysis.IncrementalValuesProvider<TSource> source, System.Action<Microsoft.CodeAnalysis.PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.AddSource(string! hintName, Microsoft.CodeAnalysis.Text.SourceText! sourceText) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.AddSource(string! hintName, string! source) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.CancellationToken.get -> System.Threading.CancellationToken
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.PreCompilationSourceProductionContext() -> void
Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind.PreCompilation = 16 -> Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind
const Microsoft.CodeAnalysis.WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput" -> string!
```

Note that **`IncrementalGeneratorOutputKind.PreCompilation = 16` and
`WellKnownGeneratorOutputs.PreCompilationSourceOutput` are NOT marked experimental** —
only the registration methods and the production-context type are. A consumer can therefore
switch on the new enum value, or observe the new step name in `TrackedSteps`, without
suppressing `RSEXPERIMENTAL007`. Critically, this means **any generator driver host will see
the new output kind flow through `GeneratorDriverOptions.DisabledOutputs` and
`GeneratorRunResult.TrackedOutputSteps` regardless of whether it opted into the experiment.**

`RSEXPERIMENTAL007` is declared in
`src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs`:

```csharp
internal const string NullableDisabledSemanticModel = "RSEXPERIMENTAL001";
internal const string NullableDisabledSemanticModel_Url = "https://github.com/dotnet/roslyn/issues/70609";

internal const string GeneratorHostOutputs = "RSEXPERIMENTAL004";
internal const string GeneratorHostOutputs_Url = "https://github.com/dotnet/roslyn/issues/74753";

// The UrlFormat property is customized per-api to point at a public API tracking issue for the feature, not a single general issue.
internal const string PreviewLanguageFeatureApi = "RSEXPERIMENTAL006";

internal const string PreCompilationSourceOutput = "RSEXPERIMENTAL007";
internal const string PreCompilationSourceOutput_Url = "https://github.com/dotnet/roslyn/issues/83089";

// Previously taken: RSEXPERIMENTAL003 - https://github.com/dotnet/roslyn/issues/73002 (SyntaxTokenParser)
// Previously taken: RSEXPERIMENTAL005 - https://github.com/dotnet/roslyn/issues/77697
```

Implementation PR: <https://github.com/dotnet/roslyn/pull/83088> (merged 2026-05-20).
API review issue: <https://github.com/dotnet/roslyn/issues/83089>.
Design document: <https://github.com/dotnet/roslyn/blob/main/docs/features/pre-compilation-source-outputs.md>.

---

## 1. Execution order of the three output stages

### 1.1 The design document, quoted verbatim

From `docs/features/pre-compilation-source-outputs.md`, section **Execution Model**:

> The updated pipeline execution order is:
>
> ```
> 1. RegisterPostInitializationOutput
>    +-- Source added to initial compilation (takes no inputs)
>
> 2. RegisterPreCompilationSourceOutput          <- NEW
>    +-- Reads non-compilation inputs (additional files, parse options, etc.)
>    +-- Source added to initial compilation
>    +-- Compilation is rebuilt with new sources
>
> 3. RegisterSourceOutput / RegisterImplementationSourceOutput
>    +-- Reads full compilation (now includes post-init AND pre-compilation sources)
>    +-- Source is part of final output but not fed back into compilation
> ```
>
> Concretely, in the generator driver:
>
> 1. Post-initialization sources are collected as today.
> 2. A `DriverStateTable.Builder` is created **without** the compilation or syntax store - these are not yet available.
> 3. Pre-compilation output nodes are evaluated for all generators. Their sources are parsed into syntax trees.
> 4. The initial compilation is augmented: `compilation = compilation.AddSyntaxTrees(preCompilationTrees)`.
> 5. `DriverStateTable.Builder.SetCompilation` is called, which stores the compilation and creates the `SyntaxStore.Builder` internally.
> 6. Standard source output nodes execute against the augmented compilation.
>
> This means:
> - **Within a single generator**: A `RegisterSourceOutput` callback can query the semantic model and see types produced by that same generator's `RegisterPreCompilationSourceOutput`.
> - **Across generators**: Generator B's `RegisterSourceOutput` can see types produced by Generator A's `RegisterPreCompilationSourceOutput`.
> - **Incremental behavior**: Pre-compilation outputs participate in the standard incremental caching. If the inputs (e.g., additional files) haven't changed, the pre-compilation sources are cached and the compilation is not rebuilt unnecessarily.

And from the **Motivation** section:

> **Cross-Generator Type Visibility**: As a secondary benefit, pre-compilation sources are visible to *all* generators' standard phases, not just the generator that produced them. A generator that reads `.proto` files and emits C# types via `RegisterPreCompilationSourceOutput` makes those types available to other generators that perform binding in their `RegisterSourceOutput` phase. This enables cross-generator interoperability without requiring generators to know about each other.

### 1.2 The actual implementation

`src/Compilers/Core/Portable/SourceGeneration/GeneratorDriver.cs`, `RunGeneratorsCore`. The
real code is **three sequential passes, each a complete `for` loop over all generators**, not a
per-generator pipeline. The essential sequence:

```csharp
var inputCompilation = compilation;              // captured before any driver-side AddSyntaxTrees

// PASS 1 -- for i in 0..N-1
//   Initialize() if needed (catch -> CS8784, isInit: true)
//   UpdateOutputs(outputNodes, IncrementalGeneratorOutputKind.PostInit, new GeneratorRunStateTable.Builder(false), ImmutableHashSet<string>.Empty, ct)
//   ParseAdditionalSources(...)                 -> generatorState.PostInitTrees
//   RequiresInputTreeReparse(parseOptions) -> reparse PostInit AND PreCompilation trees
//   collect generatorState.InputNodes into syntaxInputNodes
//   collect generatorState.PostInitTrees into constantSourcesBuilder

if (constantSourcesBuilder.Count > 0)
    compilation = compilation.AddSyntaxTrees(constantSourcesBuilder);

var driverStateBuilder = new DriverStateTable.Builder(_state, compilation, syntaxInputNodes, ct);

// per-generator step-tracking builders, SHARED across the pre-comp and standard passes
var generatorRunStateBuilders = new GeneratorRunStateTable.Builder[N];
for (int i = 0; i < N; i++)
    generatorRunStateBuilders[i] = new GeneratorRunStateTable.Builder(state.TrackIncrementalSteps);

// PASS 2 (PRE-COMPILATION) -- for i in 0..N-1
    var preCompReserved = collectHintNames(generatorState.PostInitTrees);
    var preCompilationContext = UpdateOutputs(generatorState.OutputNodes,
        IncrementalGeneratorOutputKind.PreCompilation, generatorRunStateBuilders[i],
        preCompReserved, ct, driverStateBuilder);
    var (sources, _, _, _) = preCompilationContext.ToImmutableAndFree();
    var parsedSources = ReuseOrParsePreCompilationSources(state.Generators[i], sources, generatorState.PreCompilationTrees, ct);
    stateBuilder[i] = generatorState.WithPreCompilationTrees(parsedSources);
    // catch (UserFunctionException ufe) -> SetGeneratorException(..., phase: GeneratorRunPhase.PreCompilation, ...)

// COMPILATION CACHE
var cacheBuilder = state.CompilationCache.ToBuilder(inputCompilation, compilation);
//   for each generator: cacheBuilder.AddPostInitTree(tree.Tree) for each post-init tree
//   for each generator: cacheBuilder.AddPreCompTree(i, tree)    for each pre-comp tree
state = state.With(compilationCache: cacheBuilder.ToImmutableAndFree());
compilation = state.CompilationCache.Compilation;

driverStateBuilder.SetCompilation(compilation);

// PASS 3 (STANDARD) -- for i in 0..N-1
    if (... || generatorState.PreCompilationFailed) continue;
    var standardReserved = collectHintNames(generatorState.PostInitTrees, generatorState.PreCompilationTrees);
    var context = UpdateOutputs(generatorState.OutputNodes,
        IncrementalGeneratorOutputKind.Source | IncrementalGeneratorOutputKind.Implementation | IncrementalGeneratorOutputKind.Host,
        generatorRunStateBuilders[i], standardReserved, ct, driverStateBuilder);
    // catch (UserFunctionException ufe) -> SetGeneratorException(..., phase: GeneratorRunPhase.Standard, ...)
```

A new internal enum was added at the bottom of the same file:

```csharp
internal enum GeneratorRunPhase
{
    Init,
    PreCompilation,
    Standard,
}
```

### 1.3 Answers to the specific questions

**Is one generator's PreCompilation output visible to another generator's PreCompilation stage?**
**No.** Two independent reasons:

1. The pre-compilation pass runs to completion for *all* generators before any pre-compilation
   tree is added to a compilation (`AddSyntaxTrees` happens in the compilation-cache step,
   after the whole loop).
2. A pre-compilation callback has **no way to observe a compilation at all**. `CompilationProvider`
   and `SyntaxProvider` throw during this phase (see §1.5). The only inputs it can read are
   `ParseOptionsProvider`, `AdditionalTextsProvider`, `AnalyzerConfigOptionsProvider`,
   `CompilationOptionsProvider` and `MetadataReferencesProvider`.

**Is one generator's PreCompilation output visible to another generator's standard stage?**
**Yes, unconditionally, and independently of registration order.** Test
`PreCompilationSource_Is_Visible_To_Other_Generators_ReversedOrder` in
`src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests_PreCompilation.cs`
registers the *consuming* generator first, with the comment:

> // Register the consuming generator before the producing generator to verify
> // ordering of generator registration does not affect pre-compilation visibility.

**What is the ordering rule between generators?**
Index order in `GeneratorDriverState.Generators` — that is, the order the generators were handed
to the driver. There is no sorting, no priority, and no dependency graph. For the command-line
compiler that order is derived from `/analyzer:` reference order and, within one reference, from
the order `AnalyzerFileReference.GetGenerators` returns. This ordering determines only:
- the relative position of trees inside `Compilation.SyntaxTrees` (§2), and
- nothing about visibility, because all pre-comp output is committed at once.

**Is the result deterministic?**
Yes, for a fixed generator list, fixed inputs and generators that are themselves deterministic.
The driver performs no parallelism across generators in any of the three passes; each is a plain
sequential `for` loop. The design document does not use the word "deterministic"; this
conclusion is read off the implementation. The pre-existing determinism caveat still applies:
Roslyn assumes generators are deterministic and does not enforce it.

**A consequence the design document states only implicitly:** because a pre-compilation output
joins the initial compilation, **one generator can now change what another generator's
`SyntaxProvider` / `ForAttributeWithMetadataName` observes.** Post-init output already had this
property; pre-compilation output extends it to content computed from `AdditionalTexts` and
`.editorconfig`.

### 1.4 What runs, and what does not, when things are disabled or filtered

- `UpdateOutputs` filters with
  `if (outputKind.HasFlag(outputNode.Kind) && !_state.DisabledOutputs.HasFlag(outputNode.Kind))`.
  So `new GeneratorDriverOptions(IncrementalGeneratorOutputKind.PreCompilation)` disables the
  new stage entirely.
- `RunGenerators(compilation, generatorFilter, ct)` — a filtered-out generator skips *all three*
  passes, but its previously collected `PreCompilationTrees` are still fed into the compilation
  cache. The driver comment:

  > // Filtered generators don't run their pre-comp callback this pass, but their state still
  > // carries the previously collected PreCompilationTrees -- feeding those (and the just-produced
  > // trees from unfiltered generators) into the cache keeps the resulting compilation stable across runs.

  Test: `PreCompilation_Generator_Filtered_Other_Generator_Stays_Cached`.

### 1.5 Phase enforcement (runtime, not compile time)

`src/Compilers/Core/Portable/SourceGeneration/Nodes/SharedInputNodes.cs`:

```csharp
public static readonly InputNode<Compilation> Compilation = new InputNode<Compilation>(b => ImmutableArray.Create(GetCompilationOrThrow(b, nameof(IncrementalGeneratorInitializationContext.CompilationProvider))));
public static readonly InputNode<CompilationOptions> CompilationOptions = new InputNode<CompilationOptions>(b => ImmutableArray.Create(b.InitialCompilationOptions), ReferenceEqualityComparer.Instance);
public static readonly InputNode<ParseOptions> ParseOptions = new InputNode<ParseOptions>(b => ImmutableArray.Create(b.DriverState.ParseOptions));
public static readonly InputNode<AdditionalText> AdditionalTexts = new InputNode<AdditionalText>(b => b.DriverState.AdditionalTexts);
public static readonly InputNode<SyntaxTree> SyntaxTrees = new InputNode<SyntaxTree>(b => GetCompilationOrThrow(b, nameof(IncrementalGeneratorInitializationContext.SyntaxProvider)).SyntaxTrees.ToImmutableArray());
public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>(b => ImmutableArray.Create(b.DriverState.OptionsProvider));
public static readonly InputNode<MetadataReference> MetadataReferences = new InputNode<MetadataReference>(b => b.InitialMetadataReferences);

private static Compilation GetCompilationOrThrow(DriverStateTable.Builder b, string providerName)
{
    if (!b.IsCompilationAvailable)
    {
        // The full compilation (including syntax trees) is not available during the pre-compilation phase;
        // CompilationProvider and SyntaxProvider must wait for the standard phase. Note that
        // CompilationOptions and MetadataReferences ARE available pre-compilation, since they
        // are unaffected by source generation.
        throw new UserFunctionException(new InvalidOperationException(
            string.Format(CodeAnalysisResources.CompilationNotAvailableInPreCompilationPhase, providerName)));
    }
    return b.Compilation;
}
```

`SyntaxInputNode.UpdateStateTable` has its own guard:

```csharp
if (!graphState.IsCompilationAvailable)
{
    // Syntax-based providers cannot be used as inputs to a pre-compilation source output,
    // because the compilation (and thus syntax tree analysis) has not yet been built.
    // Wrap as a UserFunctionException so the driver reports a generator error instead of crashing.
    throw new UserFunctionException(new InvalidOperationException(
        CodeAnalysisResources.SyntaxProvidersNotAvailableInPreCompilationPhase));
}
```

Two new strings in `src/Compilers/Core/Portable/CodeAnalysisResources.resx`:

- `CompilationNotAvailableInPreCompilationPhase` =
  `"The compilation is not available during the pre-compilation phase, so {0} cannot be used as an input to a pre-compilation source output."`
- `SyntaxProvidersNotAvailableInPreCompilationPhase` =
  `"Syntax-based providers (e.g. SyntaxProvider, ForAttributeWithMetadataName) cannot be used as inputs to a pre-compilation source output, because the compilation has not yet been built."`

Note the divergence from the design document: it says the `DriverStateTable.Builder`
property getters throw `InvalidOperationException`. In the shipped code they carry only a
`Debug.Assert`; the throw lives in `SharedInputNodes.GetCompilationOrThrow` and
`SyntaxInputNode.UpdateStateTable`.

**Providers that DO work in the pre-compilation phase**, per `DriverStateTable.Builder`:

```csharp
/// <summary>
/// The compilation options from the user-supplied compilation. Available in all
/// phases, including pre-compilation, because options are unaffected by source
/// generation.
/// </summary>
internal CompilationOptions InitialCompilationOptions => _initialCompilation.Options;

/// <summary>
/// The metadata references from the user-supplied compilation. Available in all
/// phases, including pre-compilation, because references are unaffected by source
/// generation.
/// </summary>
internal ImmutableArray<MetadataReference> InitialMetadataReferences => _initialCompilation.ExternalReferences;
```

Tests: `PreCompilation_Can_Access_CompilationOptions`, `PreCompilation_Can_Access_MetadataReferences`.
This is itself a behaviour change for the standard phase too: `CompilationOptionsProvider` and
`MetadataReferencesProvider` now read from the **initial** compilation rather than from the
augmented one (semantically equivalent, since generation changes neither).

---

## 2. What the initial compilation contains, and `SyntaxTrees` order

### 2.1 Order inside the driver's own augmented compilation

From `CompilationCache.Builder.ToImmutableAndFree`
(`src/Compilers/Core/Portable/SourceGeneration/CompilationCache.cs`):

```csharp
var newCompilation = preCompTreesToAdd.IsEmpty
    ? _compilationWithPostInit
    : _compilationWithPostInit.AddSyntaxTrees(preCompTreesToAdd);
```

with `_compilationWithPostInit = inputCompilation.AddSyntaxTrees(constantSourcesBuilder)`.
Both builders are filled in generator-index order. Therefore, in the compilation the **standard
phase** sees:

```
[ user syntax trees (unchanged order) ]
[ generator 0 post-init trees ] [ generator 1 post-init trees ] ... [ generator N-1 post-init trees ]
[ generator 0 pre-comp trees  ] [ generator 1 pre-comp trees  ] ... [ generator N-1 pre-comp trees  ]
```

Within one generator, trees follow the order in which `AddSource` was called across that
generator's output nodes (nodes are visited in registration order by `UpdateOutputs`).

### 2.2 Order in the compilation returned to the host

`GeneratorDriver.RunGeneratorsAndUpdateCompilation` builds a *different* order — it appends
all three tree kinds per generator to the caller's original compilation:

```csharp
trees = ArrayBuilder<SyntaxTree>.GetInstance();
foreach (var generatorState in state.GeneratorStates)
{
    trees.AddRange(generatorState.PostInitTrees.Select(t => t.Tree));
    trees.AddRange(generatorState.PreCompilationTrees.Select(t => t.Tree));
    trees.AddRange(generatorState.GeneratedTrees.Select(t => t.Tree));
}

outputCompilation = compilation.AddSyntaxTrees(trees);
```

So `outputCompilation.SyntaxTrees` is:

```
[ user trees ]
[ gen0 post-init ][ gen0 pre-comp ][ gen0 standard ]
[ gen1 post-init ][ gen1 pre-comp ][ gen1 standard ]
...
```

**This is a real ordering difference between the compilation generators see and the compilation
that is emitted.** In the intermediate (generator-facing) compilation, all post-init trees of all
generators precede all pre-comp trees of all generators. In the output compilation, the three
kinds are interleaved per generator. Anything that indexes `SyntaxTrees` positionally must
know which compilation it holds.

Tests: `PreCompilationSource_Is_Added_To_Output_Compilation` (1 user tree + 1 pre-comp = 2
trees), `Multiple_PreCompilationSources_From_Same_Generator` (3), `Multiple_Generators_With_PreCompilationSources` (3),
`PreCompilation_RunResult_Trees_Are_In_Output_Compilation`.

### 2.3 Do analyzers and the semantic model see the pre-compilation trees?

**Yes, both.**

- **Semantic model**: the standard phase binds against the augmented compilation. The design
  document's canonical example is a `RegisterSourceOutput` callback calling
  `compilation.GetTypeByMetadataName("MyProto.MyMessage")` and getting a non-null symbol
  produced by the pre-compilation stage. Tests
  `PreCompilationSource_Is_Visible_To_RegisterSourceOutput`,
  `PreCompilationSource_Is_Visible_To_Other_Generators`.
- **Analyzers**: in `src/Compilers/Core/Portable/CommandLine/CommonCompiler.cs`,
  `RunGenerators` is called first and reassigns `compilation`; the analyzer driver is created
  afterwards from that same variable:

  ```csharp
  (compilation, generatorTimingInfo) = RunGenerators(compilation, baseDirectory, Arguments.ParseOptions, generators, analyzerConfigProvider, additionalTextFiles, diagnostics);
  ...
  AnalyzerOptions analyzerOptions = CreateAnalyzerOptions(additionalTextFiles, analyzerConfigProvider);
  if (!analyzers.IsEmpty)
  {
      (analyzerCts, analyzerExceptionDiagnostics, analyzerDriver) = initializeAnalyzerDriver(analyzerOptions, ref compilation);
  }
  ```

  Note this was already true for post-init and standard generated trees; pre-compilation trees
  simply join the same set.

- The compiler classifies generated trees purely positionally:

  ```csharp
  var generatedSyntaxTrees = compilation.SyntaxTrees.Skip(Arguments.SourceFiles.Length).ToList();
  ```

  All three kinds are therefore treated identically for `.editorconfig` option lookup
  (`analyzerConfigSet.GetOptionsForSourcePath(tree.FilePath)`), for `EmbeddedText.FromSource`,
  and for on-disk emission.

### 2.4 `SyntaxTree.FilePath` and hint names

`GeneratorDriver.ParseAdditionalSources` / `ReuseOrParsePreCompilationSources`:

```csharp
var prefix = GetFilePathPrefixForGenerator(this._state.BaseDirectory, generator);
var tree = ParseGeneratedSourceText(source, Path.Combine(prefix, source.HintName), cancellationToken);
```

```csharp
internal static string GetFilePathPrefixForGenerator(string? baseDirectory, ISourceGenerator generator)
{
    var type = generator.GetGeneratorType();
    return Path.Combine(baseDirectory ?? "", type.Assembly.GetName().Name ?? string.Empty, type.FullName!);
}
```

So, for every kind of generated tree (post-init, pre-compilation, standard — the rule is
identical):

```
<BaseDirectory>\<GeneratorAssemblySimpleName>\<GeneratorTypeFullName>\<hintName>
```

- `BaseDirectory` is `GeneratorDriverOptions.BaseDirectory`. In the command-line compiler it
  is `Arguments.GeneratedFilesOutputDirectory` when `/generatedfilesout:` was passed, else
  `Arguments.OutputDirectory` (see §5). When it is `null`, the path is relative.
- `GeneratorTypeFullName` is `Type.FullName`, so a nested generator type contributes a `+`
  segment (e.g. `My.Ns.Outer+Inner`).
- `hintName` is the *normalised* hint name from `AdditionalSourcesCollection`.

Hint-name normalisation (`src/Compilers/Core/Portable/SourceGeneration/AdditionalSourcesCollection.cs`):

- allowed characters: identifier-part characters plus `` . , - + ` _ <space> ( ) [ ] { } / \ ``;
  anything else throws `ArgumentException` with `HintNameInvalidChar`
  (`"The hintName '{0}' contains an invalid character '{1}' at position {2}."`);
- `hintName = hintName.Replace('\\', '/')` — backslashes become forward slashes;
- the regex `(\.{1,2}|/|^| )/` rejects leading `/`, `./`, `../`, `//` and `" /"`
  (`HintNameInvalidSegment`);
- `AppendExtensionIfRequired` appends the language file extension (`.cs` / `.vb`) when absent;
- uniqueness is `StringComparer.OrdinalIgnoreCase`.

Because `Path.Combine` uses the platform separator while the hint name keeps `/`, a hint name
containing directories produces a **mixed-separator path** on Windows, e.g.
`C:\p\obj\Debug\net11.0\generated\MyGen\My.Ns.MyGenerator\sub/dir/file.g.cs`.

**New in this wave: hint names must be unique across phases within one generator.**
`UpdateOutputs` now takes a `reservedHintNames` set:

- pre-compilation pass reserves `collectHintNames(PostInitTrees)`;
- standard pass reserves `collectHintNames(PostInitTrees, PreCompilationTrees)`;
- `AdditionalSourcesCollection.Contains` consults `_reservedHintNames` first;
- comparison is `StringComparer.OrdinalIgnoreCase`.

Driver comment:

> // Reserve hint names from prior phases (PostInit and PreCompilation) so that
> // standard-phase outputs cannot collide with them. Hint names must be unique
> // across all phases for a single generator.

Collision behaviour, from the tests:

| Test | Result |
|---|---|
| `PreCompilation_And_Standard_Output_Same_HintName` | standard phase throws `ArgumentException` → CS8785; pre-comp tree survives in the output compilation and in `GeneratedSources` (hint name `"shared.cs"`) |
| `PostInit_And_PreCompilation_Output_Same_HintName` | pre-comp phase throws; post-init tree survives, pre-comp tree not committed |
| `PostInit_And_Standard_Output_Same_HintName` | standard phase throws; post-init tree survives |
| `PreCompilation_HintName_Conflict_Within_PreCompilation_Phase_Throws` | pre-comp phase throws; neither type committed |

The reserved set is **per generator**, so two different generators may still use the same hint
name; their file paths differ by assembly name and generator type name.

### 2.5 The new compilation cache (reference stability)

New file `src/Compilers/Core/Portable/SourceGeneration/CompilationCache.cs`. Its purpose,
quoted from the type's own documentation:

> Caches the compilation produced by the pre-compilation phase, keyed on the inputs that
> determine its content. When a subsequent run produces the same inputs the cached
> `Compilation` reference is reused, which preserves reference-equality on
> `SharedInputNodes.Compilation` and therefore keeps every generator's
> `CompilationProvider`-derived caching valid across runs.

Cache key components:
- reference equality on the *input* compilation;
- reference-sequence equality on the flat list of post-init `SyntaxTree`s;
- sequence equality on `PreCompCacheKey` values, where

```csharp
public bool Equals(PreCompCacheKey other) =>
    GeneratorIndex == other.GeneratorIndex
    && string.Equals(HintName, other.HintName, StringComparison.OrdinalIgnoreCase)
    && ReferenceEquals(Text, other.Text)
    && ReferenceEquals(Options, other.Options);
```

When no generator produced any pre-compilation output, the cache is bypassed:

> // No pre-compilation contributions this run. Return a fresh cache holding
> // just compilationWithPostInit; we deliberately don't attempt to reuse
> // _previous here because that compilation reference is regenerated every
> // run (post-init AddSyntaxTrees) and standard-phase consumers expect to
> // see that fresh reference to preserve their own re-execution semantics.

**Consequence:** once *any* loaded generator registers a pre-compilation output, the
`Compilation` instance handed to *every* generator's `CompilationProvider` becomes
reference-stable across runs when nothing changed, which flips those generators'
`CompilationProvider` steps from re-executing to `Cached`. That is a behaviour change for
generators that never opted into the feature. Test:
`PreCompilation_Generator_Does_Not_Invalidate_Other_Generators_CompilationProvider`.

Reference stability of the trees themselves is handled by `ReuseOrParsePreCompilationSources`,
whose documentation states the failure mode it prevents:

> Like `ParseAdditionalSources`, but reuses a previously-parsed `GeneratedSyntaxTree` when the
> corresponding new `GeneratedSourceText` has the same `SourceText` reference and hint name at
> the same position -- indicating the upstream pre-compilation callback was cached.
>
> This serves two purposes: it skips wasted re-parsing of unchanged generator output, and it
> keeps the trees seen by the standard phase reference-stable across runs. The latter matters
> because the compilation cache reuses the previous run's `Compilation` (and the syntax trees it
> contains) on a hit; if we re-parsed pre-compilation sources, a cached standard-phase output's
> diagnostic could still hold a `Location` pointing at a tree that's no longer present in the
> run's output compilation.

Tests: `PreCompilation_Cached_SyntaxTree_Reference_Is_Stable_Across_Runs`,
`PreCompilation_Cached_Standard_Diagnostic_Tree_Is_In_Output_Compilation`.

---

## 3. Driver-mechanics API surface

### 3.1 Unchanged public types

Verified byte-for-byte against `main`:

- **`GeneratorDriverOptions`** — unchanged. Still `DisabledOutputs`, `TrackIncrementalGeneratorSteps`,
  `BaseDirectory`, internal `ChecksumAlgorithm`, and the same three constructors. No
  pre-compilation-specific option. The new stage is disabled by
  `new GeneratorDriverOptions(IncrementalGeneratorOutputKind.PreCompilation)` like any other kind.
- **`GeneratorDriver`** — no new public members. `RunGenerators`,
  `RunGeneratorsAndUpdateCompilation`, `AddGenerators`, `ReplaceGenerators`, `RemoveGenerators`,
  `AddAdditionalTexts`, `RemoveAdditionalTexts`, `ReplaceAdditionalText(s)`,
  `WithUpdatedParseOptions`, `WithUpdatedAnalyzerConfigOptions`, `GetRunResult`, `GetTimingInfo`.
- **`GeneratorDriverRunResult`** — unchanged (`Results`, `Diagnostics`, `GeneratedTrees`).
- **`GeneratorRunResult`** — unchanged (`Generator`, `GeneratedSources`, `Diagnostics`,
  `HostOutputs`, `Exception`, `TrackedSteps`, `TrackedOutputSteps`).
- **`GeneratedSourceResult`** — unchanged (`SyntaxTree`, `SourceText`, `HintName`).
- **`IncrementalGeneratorRunStep`** — unchanged (`Name`, `Inputs`, `Outputs`, `ElapsedTime`).
- **`IncrementalStepRunReason`** — unchanged (`New`, `Modified`, `Unchanged`, `Cached`, `Removed`).
  **No new reason was added for the pre-compilation phase.**
- **`IncrementalGeneratorOutputKind.Host = 0b1000`** and `RegisterHostOutput` /
  `HostOutputProductionContext` — untouched by this feature; still `RSEXPERIMENTAL004`
  (<https://github.com/dotnet/roslyn/issues/74753>). The only change is that the standard pass
  now requests `Source | Implementation | Host` in one `UpdateOutputs` call, exactly as before
  the split.

### 3.2 Behavioural changes to those unchanged types

- **`GeneratorRunResult.GeneratedSources`** now contains, in this order:
  post-init sources, then pre-compilation sources, then standard sources. From
  `GeneratorDriver.GetRunResult`:

  ```csharp
  ArrayBuilder<GeneratedSourceResult> sources = ArrayBuilder<GeneratedSourceResult>.GetInstance(
      generatorState.PostInitTrees.Length +
      generatorState.PreCompilationTrees.Length +
      generatorState.GeneratedTrees.Length);
  foreach (var tree in generatorState.PostInitTrees)        { sources.Add(new GeneratedSourceResult(tree.Tree, tree.Text, tree.HintName)); }
  foreach (var tree in generatorState.PreCompilationTrees)  { sources.Add(new GeneratedSourceResult(tree.Tree, tree.Text, tree.HintName)); }
  foreach (var tree in generatorState.GeneratedTrees)       { sources.Add(new GeneratedSourceResult(tree.Tree, tree.Text, tree.HintName)); }
  ```

  There is **no flag on `GeneratedSourceResult` saying which stage produced a source.** The only
  way to tell is `TrackedOutputSteps`, and only when `TrackIncrementalGeneratorSteps` is on.

- **`GeneratorRunResult.TrackedSteps` / `TrackedOutputSteps`** gain the key
  `"PreCompilationSourceOutput"`. `WellKnownGeneratorOutputs` now reads:

  ```csharp
  public static class WellKnownGeneratorOutputs
  {
      public const string SourceOutput = nameof(SourceOutput);
      public const string ImplementationSourceOutput = nameof(ImplementationSourceOutput);
      public const string PreCompilationSourceOutput = nameof(PreCompilationSourceOutput);
  }
  ```

  Test `PreCompilation_Has_Distinct_Step_Name` asserts both `PreCompilationSourceOutput` and
  `SourceOutput` appear in `TrackedSteps.Keys` and in `TrackedOutputSteps.Keys`.

- **`TrackIncrementalGeneratorSteps`** — same option, same semantics, but the driver now
  allocates one `GeneratorRunStateTable.Builder` per generator *before* the pre-compilation
  pass and reuses it for the standard pass. Design document:

  > Per-generator `GeneratorRunStateTable.Builder` instances are shared across both the
  > pre-compilation and standard passes, so all steps for a generator (both pre-compilation
  > and standard) appear in a unified view.

  Note the post-init pass still gets a throwaway `new GeneratorRunStateTable.Builder(false)`,
  so post-init steps are never tracked (unchanged behaviour).

- **`AbstractSourceOutputNode<TInput>`** is a new internal abstract base holding all shared
  `UpdateStateTable` / `AppendOutputs` logic, with abstract `Kind`, `StepName` and
  `InvokeUserAction`. `SourceOutputNode<TInput>` (Source + Implementation) and
  `PreCompilationSourceOutputNode<TInput>` derive from it. `PreCompilationSourceOutputNode`
  builds a `PreCompilationSourceProductionContext` with
  `graphState.DriverState.ChecksumAlgorithm` and **no diagnostic bag**.

- **`GeneratorState`** (internal) gained `PreCompilationTrees`, `PreCompilationFailed`,
  `WithPreCompilationTrees`, and a `GeneratorRunPhase` parameter on `WithError`.
  `RequiresConstantTreeReparse` was renamed/reshaped to `RequiresInputTreeReparse`, which now
  checks post-init *and* pre-compilation trees:

  ```csharp
  internal bool RequiresInputTreeReparse(ParseOptions parseOptions)
      => PostInitTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions)
      || PreCompilationTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions);
  ```

  Design document: *"Like post-initialization trees, pre-compilation trees are reparsed when
  parse options change between driver runs."*

---

## 4. Caching, cancellation, exceptions, diagnostics

### 4.1 Generator diagnostic identifiers — unchanged

No new diagnostic identifier was added. `GeneratorDriver.CreateGeneratorExceptionDiagnostic`:

```csharp
var errorCode = isInit ? provider.WRN_GeneratorFailedDuringInitialization : provider.WRN_GeneratorFailedDuringGeneration;
```

`src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`:

```csharp
WRN_GeneratorFailedDuringInitialization = 8784,
WRN_GeneratorFailedDuringGeneration = 8785,
```

Messages (`CSharpResources.resx`):

- **CS8784** — `"Generator '{0}' failed to initialize. It will not contribute to the output and compilation errors may occur as a result. Exception was of type '{1}' with message '{2}'.\n{3}"`, title `"Generator failed to initialize."`
- **CS8785** — `"Generator '{0}' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type '{1}' with message '{2}'.\n{3}"`, title `"Generator failed to generate source."`

Both are `DiagnosticSeverity.Warning`, category `"Compiler"`,
`customTags: WellKnownDiagnosticTags.AnalyzerException`, `Location.None`. Format arguments are
`generator.GetGeneratorType().Name`, `e.GetType().Name`, `e.Message`,
`e.CreateDiagnosticDescription()`.

Mapping to phase (via `GeneratorRunPhase`, `isInit: phase == GeneratorRunPhase.Init`):

| Phase | Diagnostic |
|---|---|
| `Init` (Initialize + post-init output) | **CS8784** |
| `PreCompilation` | **CS8785** |
| `Standard` | **CS8785** |

So a pre-compilation failure is indistinguishable from a standard-phase failure by
diagnostic identifier alone.

### 4.2 What a generator throwing now does

`SetGeneratorException`:

```csharp
var diagnostic = CreateGeneratorExceptionDiagnostic(provider, generator, e, isInit: phase == GeneratorRunPhase.Init);
var filtered = compilation.Options.FilterDiagnostic(diagnostic, cancellationToken);

// Build output respects the compilation's diagnostic options: a suppressed warning isn't
// added to the driver diagnostic bag.
if (filtered is not null)
{
    diagnosticBag?.Add(filtered);
}

// The per-generator run result always carries the failure diagnostic when an exception
// is recorded -- this preserves the documented invariant of GeneratorRunResult and
// ensures that a pre-compilation phase failure is recorded so the standard phase will
// skip this generator (otherwise standard-phase outputs would run with stale/missing
// pre-comp trees).
return generatorState.WithError(e, filtered ?? diagnostic, runTime ?? TimeSpan.Zero, phase);
```

Two behaviours worth calling out, both new in this wave:

1. **A suppressed CS8785 no longer hides the exception from `GeneratorRunResult`.**
   Even when `/nowarn:CS8785` or `.editorconfig` suppresses the warning, `GeneratorRunResult.Exception`
   and `GeneratorRunResult.Diagnostics` still carry it. Tests:
   `PreCompilation_Throws_With_Warning_Suppressed_Still_Stops_Generator`,
   `Standard_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable`,
   `Init_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable`.

2. **A pre-compilation failure skips that generator's standard phase entirely.**
   `GeneratorState.PreCompilationFailed` is set only when `phase == GeneratorRunPhase.PreCompilation`,
   and the standard loop does `if (... || generatorState.PreCompilationFailed) continue;`.
   Design document:

   > When a pre-compilation output fails (whether from accessing compilation-dependent inputs
   > or from any other exception), the generator is placed in **error state**: a diagnostic is
   > reported, and the generator's standard phase is **skipped entirely**. Other generators are
   > unaffected - their pre-compilation and standard phases continue to execute normally.

   Tests: `PreCompilation_Throws_Reports_Error_And_Stops_Generator`,
   `PreCompilation_Throws_Other_Generators_Unaffected`,
   `PreCompilation_Failure_Skips_Standard_But_Recovers_On_Next_Run`.

3. **Tree preservation depends on which phase failed** (`GeneratorState.WithError`):

   ```csharp
   // Preserve pre-comp trees only when the standard phase failed: those trees were
   // already added to the compilation other generators observed, so dropping them
   // would leave this generator's state inconsistent with what those generators saw.
   preCompilationTrees: phase == GeneratorRunPhase.Standard ? this.PreCompilationTrees : ImmutableArray<GeneratedSyntaxTree>.Empty,
   ```

   Test `PreCompilation_Trees_Preserved_When_Standard_Phase_Throws`.

4. **`CatchAnalyzerExceptions == false` still fail-fasts:**

   ```csharp
   if (!compilation.CatchAnalyzerExceptions)
   {
       Debug.Assert(false);
       Environment.FailFast(CreateGeneratorExceptionDiagnostic(messageProvider, sourceGenerator, e, isInit).ToString());
       return false;
   }
   ```

### 4.3 Diagnostic filtering (standard phase only)

`FilterDiagnostics` runs `DiagnosticAnalysisContextHelpers.VerifyArguments`, then
`compilation.Options.FilterDiagnostic` and `SuppressMessageAttributeState.ApplySourceSuppressions`.
An `ArgumentException` from `VerifyArguments` is rethrown as `UserFunctionException` → CS8785.
The pre-compilation phase never reaches this code, because
`PreCompilationSourceProductionContext` has no `ReportDiagnostic`. Design document:

> `PreCompilationSourceProductionContext` intentionally does **not** include `ReportDiagnostic`.
> Pre-compilation is an early phase focused purely on producing source; diagnostic reporting
> should be done in a separate analyzer.

Related change in the same wave: PR <https://github.com/dotnet/roslyn/pull/82113>
("Validate generator diagnostics after incremental updates", 2026-02-25) makes the driver
re-validate cached generator diagnostics on incremental runs.

### 4.4 Cancellation

- `PreCompilationSourceProductionContext.CancellationToken` is the driver's token, threaded
  through `InvokeUserAction(..., CancellationToken cancellationToken)`.
- PR <https://github.com/dotnet/roslyn/pull/83875> ("Free pooled objects across cancellation
  exceptions", 2026-06-05) wrapped pooled-object acquisition in the generator driver and the
  analyzer driver in `try`/`finally` so `ArrayBuilder<T>`, `DiagnosticBag` and `PooledHashSet<T>`
  instances are returned to their pools when an `OperationCanceledException` unwinds. Visible
  in `RunGeneratorsCore`'s `finally { stateBuilder?.Free(); constantSourcesBuilder?.Free(); syntaxInputNodes?.Free(); }`
  and in `UpdateOutputs`'s `catch { context.Free(); throw; }`. This is a leak fix, not a
  semantic change: cancellation still propagates out of `RunGenerators`.
- No new cancellation-related public API.

### 4.5 Other .NET 11-wave changes to `src/Compilers/Core/Portable/SourceGeneration`

Commits since 2025-10-01, newest first:

| Date | PR | Change |
|---|---|---|
| 2026-06-05 | [#83875](https://github.com/dotnet/roslyn/pull/83875) | Free pooled objects across cancellation exceptions |
| 2026-05-29 | [#83878](https://github.com/dotnet/roslyn/pull/83878) | **Fix input nodes incorrectly picking duplicate elements** |
| 2026-05-20 | [#83088](https://github.com/dotnet/roslyn/pull/83088) | Add `RegisterPreCompilationSourceOutput` |
| 2026-05-15 | [#82784](https://github.com/dotnet/roslyn/pull/82784) | Validate pooled objects in compiler tests |
| 2026-04-23 | [#79609](https://github.com/dotnet/roslyn/pull/79609) | `ForAttributeWithMetadataName` supports `[method: MyGenerator]` targeting primary constructors |
| 2026-02-25 | [#82113](https://github.com/dotnet/roslyn/pull/82113) | Validate generator diagnostics after incremental updates |
| 2026-01-29 | [#81992](https://github.com/dotnet/roslyn/pull/81992) | Follow-up to #81934 |
| 2026-01-12 | [#81934](https://github.com/dotnet/roslyn/pull/81934) | **Use command-line `checksumAlgorithm` in generator driver** |
| 2025-10-10 | [#80609](https://github.com/dotnet/roslyn/pull/80609) | XML doc comments on incremental-generator public API |

Two of these matter beyond the pre-compilation feature:

- **#83878 (correctness).** When an incremental generator's input set was simultaneously
  reordered and had items replaced while keeping the same element count, `InputNode` could
  select the same new item twice and silently drop a genuinely new one. The fix precomputes the
  set of replacement items (present in the new input, absent from the previous state table) and
  draws from that list rather than advancing a positional pointer. This affects every provider
  built on `InputNode`: `AdditionalTextsProvider`, `MetadataReferencesProvider`,
  `AnalyzerConfigOptionsProvider`, `ParseOptionsProvider`, `CompilationProvider`. Symptom before
  the fix: wrong generated output, no diagnostic.

- **#81934 / #81992 (checksums).** `GeneratorDriverOptions.ChecksumAlgorithm` (internal
  `init`-only property) now carries the command-line `/checksumalgorithm:` value into
  `SourceProductionContext.AddSource` and `PreCompilationSourceProductionContext.AddSource`:

  ```csharp
  public void AddSource(string hintName, string source) => AddSource(hintName, SourceText.From(source, Encoding.UTF8, checksumAlgorithm: ChecksumAlgorithm == SourceHashAlgorithm.None ? SourceHashAlgorithms.Default : ChecksumAlgorithm));
  public void AddSource(string hintName, SourceText sourceText) => Sources.Add(hintName, sourceText.WithChecksumAlgorithmIfAny(ChecksumAlgorithm));
  ```

  Generated `SourceText` instances now carry the project's checksum algorithm rather than the
  default, which changes the PDB checksums recorded for generated files. (`SourceHashAlgorithm`
  itself also gained `Sha384 = 3` and `Sha512 = 4` in `PublicAPI.Unshipped.txt`.)

---

## 5. `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath`

### 5.1 MSBuild layer — unchanged in this wave

`dotnet/roslyn` `src/Compilers/Core/MSBuildTask/Microsoft.Managed.Core.targets`, lines 320-352:

```xml
  <!--
    ========================
    CompilerGeneratedFilesOutputPath
    ========================

    Controls output of generated files.

    CompilerGeneratedFilesOutputPath controls the location the files will be output to.
    The compiler will not emit any generated files when the path is empty, and defaults to a /generated directory in $(IntermediateOutputPath) if $(IntermediateOutputPath) has a value.
    A relative path is considered relative to the project directory.

    EmitCompilerGeneratedFiles allows the user to control if anything is emitted by clearing the property when not true.
    When EmitCompilerGeneratedFiles is true, we ensure that CompilerGeneatedFilesOutputPath has a value and issue a warning if not.

    We will create CompilerGeneratedFilesOutputPath if it does not exist.
    -->
  <PropertyGroup>
    <EmitCompilerGeneratedFiles Condition="'$(EmitCompilerGeneratedFiles)' == ''">false</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath Condition="'$(EmitCompilerGeneratedFiles)' != 'true'"></CompilerGeneratedFilesOutputPath>
    <CompilerGeneratedFilesOutputPath Condition="'$(EmitCompilerGeneratedFiles)' == 'true' and '$(CompilerGeneratedFilesOutputPath)' == '' and '$(IntermediateOutputPath)' != ''">$(IntermediateOutputPath)/generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <Target Name="CreateCompilerGeneratedFilesOutputPath"
        BeforeTargets="CoreCompile"
        Condition="'$(EmitCompilerGeneratedFiles)' == 'true' and !('$(DesignTimeBuild)' == 'true' OR '$(BuildingProject)' != 'true')">

    <Warning Condition="'$(CompilerGeneratedFilesOutputPath)' == ''"
             Text="EmitCompilerGeneratedFiles was true, but no CompilerGeneratedFilesOutputPath was provided. CompilerGeneratedFilesOutputPath must be set in order to emit generated files." />

    <MakeDir Condition="'$(CompilerGeneratedFilesOutputPath)' != ''"
             Directories="$(CompilerGeneratedFilesOutputPath)"  />
  </Target>
```

`Microsoft.CSharp.Core.targets` line 127 passes it to the `Csc` task:

```xml
GeneratedFilesOutputPath="$(CompilerGeneratedFilesOutputPath)"
```

and `ManagedCompiler.cs` line 906 turns it into a command-line switch:

```csharp
commandLine.AppendSwitchIfNotNull("/generatedfilesout:", GeneratedFilesOutputPath);
```

### 5.2 Compiler layer

`CommonCompiler.CompileAndEmit`:

```csharp
var explicitGeneratedOutDir = Arguments.GeneratedFilesOutputDirectory;
var hasExplicitGeneratedOutDir = !string.IsNullOrWhiteSpace(explicitGeneratedOutDir);
var baseDirectory = hasExplicitGeneratedOutDir ? explicitGeneratedOutDir! : Arguments.OutputDirectory;
(compilation, generatorTimingInfo) = RunGenerators(compilation, baseDirectory, Arguments.ParseOptions, generators, analyzerConfigProvider, additionalTextFiles, diagnostics);

bool hasAnalyzerConfigs = !Arguments.AnalyzerConfigPaths.IsEmpty;
var generatedSyntaxTrees = compilation.SyntaxTrees.Skip(Arguments.SourceFiles.Length).ToList();
...
foreach (var tree in generatedSyntaxTrees)
{
    Debug.Assert(!string.IsNullOrWhiteSpace(tree.FilePath));
    ...
    var sourceText = tree.GetText(cancellationToken);
    embeddedTextBuilder.Add(EmbeddedText.FromSource(tree.FilePath, sourceText));
    if (analyzerOptionsBuilder is object)
        analyzerOptionsBuilder.Add(analyzerConfigSet!.GetOptionsForSourcePath(tree.FilePath));

    // write out the file if an output path was explicitly provided
    if (hasExplicitGeneratedOutDir)
    {
        var path = tree.FilePath;
        Debug.Assert(path.StartsWith(explicitGeneratedOutDir!));
        if (Directory.Exists(explicitGeneratedOutDir))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }

        var fileStream = OpenFile(path, diagnostics, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        ...
    }
}
```

Note the `if (Directory.Exists(explicitGeneratedOutDir))` guard on subdirectory creation: if
the base directory does not exist, subdirectories are not created and `OpenFile` fails. That is
why the `CreateCompilerGeneratedFilesOutputPath` target does the `MakeDir`.

### 5.3 The exact on-disk path pattern

```
$(CompilerGeneratedFilesOutputPath)\<GeneratorAssemblySimpleName>\<GeneratorTypeFullName>\<normalizedHintName>
```

With defaults, `$(CompilerGeneratedFilesOutputPath)` = `$(IntermediateOutputPath)/generated`,
i.e. `obj\Debug\net11.0\generated\`. A concrete example:

```
obj\Debug\net11.0\generated\Metalama.Framework.CompilerExtensions\Metalama.Framework.Engine.DesignTime.SourceGenerator\MyType.g.cs
```

- The `SyntaxTree.FilePath` for generated trees is exactly the path above, whether or not
  the file is written to disk. When `/generatedfilesout:` is absent, the base directory is
  `Arguments.OutputDirectory` (the `bin` directory) and **no file is written**, but
  `FilePath` still points into `bin`.
- The path is also the key used for `.editorconfig` option lookup
  (`analyzerConfigSet.GetOptionsForSourcePath(tree.FilePath)`) and for
  `EmbeddedText.FromSource`, so it is what `/embed` and `<EmbedAllSources>` record.

### 5.4 Is pre-compilation-stage output written there too?

**Yes.** The emission loop iterates `compilation.SyntaxTrees.Skip(Arguments.SourceFiles.Length)`
on the compilation returned by `RunGeneratorsAndUpdateCompilation`, which contains post-init,
pre-compilation and standard trees alike (§2.2). All three go through the same
`GetFilePathPrefixForGenerator` + hint-name path rule, so **nothing on disk distinguishes a
pre-compilation-stage file from a post-init or standard-stage file.** A tool that watches
`obj\...\generated` sees them identically.

The same holds for `EmbeddedText` and for `.editorconfig` resolution.

### 5.5 No new MSBuild property

There is no `EmitPreCompilationGeneratedFiles`, no separate output path, and no new switch on
`Csc`. `GeneratorDriverOptions.BaseDirectory` remains the single knob; it is still not settable
per output kind.

---

## 6. Analyzer/generator discovery, loading and isolation

### 6.1 `AnalyzerAssemblyLoader` — no change in the .NET 11 wave

The last structural rework was PR <https://github.com/dotnet/roslyn/pull/77004> ("Rework
analyzer assembly loading", merged **2025-03-24**, milestone VS 17.14 P3) — that is the
**.NET 10** wave, not .NET 11. It made `AnalyzerAssemblyLoader` a
`internal sealed partial class` and moved customisation to interfaces:

- `IAnalyzerPathResolver` — where an assembly (and its satellite assemblies) is loaded from.
  Implementations: `ProgramFilesAnalyzerPathResolver`, `ShadowCopyAnalyzerPathResolver`.
  Contract, quoted from `IAnalyzerPathResolver.cs`:

  > Instances of these types are considered in the order they are added to the `AnalyzerAssemblyLoader`.
  > The first instance to return true from `IsAnalyzerPathHandled(string)` will be considered to
  > be the owner of that path. From then on only that instance will be called for the other methods on this
  > interface.
  >
  > For example in a typical session: the `ProgramFilesAnalyzerPathResolver` will return true for
  > analyzer paths under C:\Program Files\dotnet. That means the `ShadowCopyAnalyzerPathResolver`,
  > which appears last on Windows, will never see these paths and hence won't shadow copy them.

- `IAnalyzerAssemblyResolver` (`#if NET` only) — which `Assembly` instance satisfies an
  `AssemblyName`. Quoted from `IAnalyzerAssemblyResolver.cs`:

  > The `AnalyzerAssemblyLoader` will partition analyzers into the directories they live
  > in and will create a separate `AssemblyLoadContext` for each directory. That instance
  > and the directory name represent `directoryContext` and `directory`.

  So the **isolation unit on .NET Core is one `AssemblyLoadContext` per directory** containing
  analyzer assemblies.

- `IAnalyzerAssemblyLoaderInternal : IAnalyzerAssemblyLoader, IDisposable` adds
  `bool IsHostAssembly(Assembly assembly)` and `string? GetOriginalDependencyLocation(AssemblyName assembly)`.

Comparer contract in `AnalyzerAssemblyLoader`:

```csharp
internal static readonly StringComparer OriginalPathComparer = StringComparer.Ordinal;
internal static readonly StringComparer GeneratedPathComparer = StringComparer.Ordinal;
internal static readonly (StringComparer Comparer, StringComparison Comparison) SimpleNameComparer = (StringComparer.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase);
```

The **only** commit touching
`src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerAssemblyLoader.cs` since 2025-11-01 is
`ShadowCopyAnalyzerPathResolver: Use cache to amortize cost of AV scans (#84765)`, 2026-08-13 —
a performance change, no contract change.

**Conclusion: nothing about analyzer/generator loading or isolation changes in the .NET 11 wave.**

### 6.2 The `analyzers` asset group in `project.assets.json` — .NET 12, NOT .NET 11

This is the most important correction in this section.

NuGet side (`NuGet/NuGet.Client`), `src/NuGet.Core/NuGet.ProjectModel`:

- `ProjectRestoreMetadata.RestoreEnableAnalyzerAssets` (bool), documented as backing
  *"the `RestoreEnableAnalyzerAssets` MSBuild property; when enabled, analyzer assets ..."*.
- `LockFileTargetLibrary.AnalyzerAssets` → `IList<LockFileItem>`.
- `LockFileFormat.AnalyzersProperty = "analyzers"` — the JSON property name inside each
  target library; entries are written `OrderBy(assembly => assembly.Path, StringComparer.Ordinal)`.
- `LockFileItem.CompilerApiVersionProperty = "compilerApiVersion"`.
- `LockFileContentFile.CodeLanguageProperty = "codeLanguage"`.

Presence by branch (grep for `RestoreEnableAnalyzerAssets` in `ProjectRestoreMetadata.cs`):

| NuGet.Client branch | occurrences |
|---|---|
| `dev` | 5 |
| `release-7.1.x` | 0 |
| `release-7.0.x` | 0 |
| `release-6.16.x` | 0 |

SDK side (`dotnet/sdk`), `src/Tasks/Microsoft.NET.Build.Tasks/ResolvePackageAssets.cs`,
PR <https://github.com/dotnet/sdk/pull/54646> (merged **2026-08-26**, milestone **12.0-preview1**):

| dotnet/sdk branch | `RestoreEnableAnalyzerAssets` in `ResolvePackageAssets.cs` |
|---|---|
| `main` (12.0) | present |
| `release/11.0.1xx` | **absent** |

**Therefore, in .NET 11 GA:**
- restore does **not** write an `analyzers` group into `project.assets.json`;
- the SDK still discovers analyzers by scanning every file in every package
  (`WriteAnalyzerPackageFiles` → `NuGetUtils.IsApplicableAnalyzer(file, ProjectLanguage)`);
- **`ExcludeAssets` / `PrivateAssets` / `IncludeAssets` are still not honoured for analyzers.**
  This is the long-standing [dotnet/sdk#1212](https://github.com/dotnet/sdk/issues/1212)
  behaviour. `PrivateAssets="all"` on a `PackageReference` still stops the reference flowing
  transitively, which is the only usable workaround, and the default `PrivateAssets` value is
  `contentfiles;build;analyzers`.
- The compiler itself never sees any of this: `csc` receives a flat list of `/analyzer:` paths
  from the `Csc` task's `Analyzers` item and applies no asset-group logic whatsoever.

**In .NET 12** (behind `RestoreEnableAnalyzerAssets`), per the PR:
- `ResolvePackageAssets` reads `LockFileTargetLibrary.AnalyzerAssets` instead of scanning files;
- the group *"already has `PrivateAssets` / `ExcludeAssets` / `IncludeAssets` applied by restore"*;
- selection uses two metadata values: `codeLanguage` must match the project language or be
  `"any"`, and among compiler-version-specific variants *"the highest applicable
  `compilerApiVersion` wins; if the project's compiler version is unknown, every variant is
  treated as version-agnostic"*;
- there is **no fallback** to legacy scanning when the feature is on and a package has no
  `analyzers` group;
- low-importance diagnostics naming each included/excluded analyzer are emitted, visible with
  `-v detailed`.

### 6.3 Folder layout under `analyzers/`

The NuGet convention (learn.microsoft.com, `nuget/guides/analyzers-conventions`, page updated
2026-02-02) is unchanged:

```
$/analyzers/{framework_name}{version}/{supported_architecture}/{supported_language}/{analyzer_name}.dll
```

- `framework_name` + `version` — optional; `dotnet` is the only valid value.
- `supported_language` — `cs`, `vb`, `fs`. Omitted means all languages.
- `supported_architecture` — present in the documented grammar but not used in practice.

**The `roslynX.Y` segment is not in that document.** It is an SDK convention, implemented in
`ResolvePackageAssets.AnalyzerResolver` and documented in that class's own `<remarks>`:

> This allows packages to ship multiple analyzers that target different versions
> of the compiler. For example, a package may include:
>
> "analyzers/dotnet/roslyn3.7/analyzer.dll"
> "analyzers/dotnet/roslyn3.8/analyzer.dll"
> "analyzers/dotnet/roslyn4.0/analyzer.dll"
>
> When the `compilerApiVersion` is 'roslyn3.9', only the assets
> in the folder with the highest applicable compiler version are picked.
> In this case,
>
> "analyzers/dotnet/roslyn3.8/analyzer.dll"
>
> will be picked, and the other analyzer assets will be excluded.

So the full effective layout is:

```
analyzers/dotnet/[roslyn<Major>.<Minor>/][cs|vb|fs/]<Analyzer>.dll
```

The `roslynX.Y` segment sits **after** `dotnet` and **before** the language segment. In practice
both of these are seen and both work:

```
analyzers/dotnet/roslyn4.14/cs/My.Analyzer.dll
analyzers/dotnet/cs/roslyn4.14/My.Analyzer.dll
```

because the version segment is located by a raw substring search rather than by position:

```csharp
private bool IsFileCompilerVersionSpecific(string file, out Version fileCompilerVersion)
{
    fileCompilerVersion = null;
    if (_compilerNameSearchString == null) { return false; }              // "/roslyn"
    int compilerNameStart = file.IndexOf(_compilerNameSearchString);
    if (compilerNameStart == -1) { return false; }
    int compilerVersionStart = compilerNameStart + _compilerNameSearchString.Length;
    int compilerVersionStop = file.IndexOf('/', compilerVersionStart);
    if (compilerVersionStop == -1) { return false; }
    return TryParseVersion(file, compilerVersionStart, compilerVersionStop - compilerVersionStart, out fileCompilerVersion);
}
```

Selection rules (`AnalyzerResolver.AddAnalyzer` / `CompleteLibraryAnalyzers`), both in the legacy
scanning path and in the new asset-group path:

- an asset with **no** compiler version is always included (version-agnostic);
- an asset whose version is **greater than** the project's `CompilerApiVersion` is dropped;
- among the remaining versioned assets **within one package**, only those equal to the maximum
  applicable version are included;
- state is reset per library (`CompleteLibraryAnalyzers`), so the choice is made per package,
  not globally.

The project's compiler version comes from the `ResolvePackageAssets` task property
`CompilerApiVersion`, documented in the task as *"Optional version of the compiler API (E.g.
'roslyn3.9', 'roslyn4.0'). Impacts applicability of analyzer assets."* It is parsed by
`ParseCompilerApiVersion` into a name (`"roslyn"`) and a `Version`. The project language comes
from `ProjectLanguage` via `NuGetUtils.GetLockFileLanguageName`.

For .NET 11, `CompilerApiVersion` will be `roslyn5.12` (Roslyn `MajorVersion`.`MinorVersion`
from `dotnet/roslyn` `eng/Versions.props`). **This is the first time the Roslyn major version
has moved since 4.x**, so a package shipping only `analyzers/dotnet/roslyn4.x/` folders will
still be selected (4.x < 5.12), but a package that wants a .NET 11-specific build must add a
`roslyn5.x` folder.

---

## 7. Design-time / IDE behaviour

- The Workspaces layer creates the driver with default options —
  `src/Workspaces/CSharp/Portable/Workspace/LanguageServices/CSharpCompilationFactoryService.cs`:

  ```csharp
  GeneratorDriver ICompilationFactoryService.CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts, string? generatedFilesBaseDirectory)
      => CSharpGeneratorDriver.Create(generators, additionalTexts, (CSharpParseOptions)parseOptions, optionsProvider, new GeneratorDriverOptions(baseDirectory: generatedFilesBaseDirectory));
  ```

  `DisabledOutputs` is `None` and `TrackIncrementalGeneratorSteps` is `false`. **The IDE therefore
  runs the pre-compilation stage.**

- `SolutionCompilationState.RegularCompilationTracker_Generators.cs` calls
  `generatorDriver.RunGenerators(compilationToRunGeneratorsOn, ShouldGeneratorRun, cancellationToken)`
  (or `GeneratorDriverInitializationCache.CreateAndRunGeneratorDriverAsync` on a first run), then
  materialises **every** `GeneratedSourceResult` — post-init, pre-compilation and standard alike —
  into a source-generated document keyed by `generatedSource.HintName` and carrying
  `generatedSource.SyntaxTree.FilePath`. Nothing in the workspace layer distinguishes a
  pre-compilation document.

- The IDE builds its final compilation itself by adding those documents to
  `compilationWithoutGeneratedFiles`, so the `SyntaxTrees` ordering rules of §2 apply only to the
  command-line path.

- `GeneratorRunResult.GeneratedSources` remains the way the IDE detects a generator that did not
  run:

  > // - GeneratedSources == default ImmutableArray: the generator was not invoked during that run (must run).
  > // - GeneratedSources == non-default empty array: the generator ran but produced no documents (may skip).
  > // - GeneratedSources == non-default non-empty array: the generator ran and produced documents (may skip).

---

## 8. Complete test inventory (behavioural contract)

`src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests_PreCompilation.cs`
(1658 lines, `[WorkItem("https://github.com/dotnet/roslyn/issues/83089")]`):

Basic functionality: `PreCompilationSource_Is_Added_To_Output_Compilation`,
`PreCompilationSource_Is_Visible_To_RegisterSourceOutput`,
`PreCompilationSource_Is_Visible_To_Other_Generators`,
`PreCompilationSource_Is_Visible_To_Other_Generators_ReversedOrder`,
`Multiple_PreCompilationSources_From_Same_Generator`,
`Multiple_Generators_With_PreCompilationSources`.

Input providers: `PreCompilation_With_AdditionalTextsProvider`,
`PreCompilation_With_AnalyzerConfigOptionsProvider`, `PreCompilation_With_Combined_Providers`,
`PreCompilation_With_Transformed_Provider`, `PreCompilation_Can_Access_CompilationOptions`,
`PreCompilation_Can_Access_MetadataReferences`.

Interaction with other stages: `PreCompilation_With_PostInit_In_Same_Generator`,
`PreCompilation_With_SourceOutput_In_Same_Generator`,
`PreCompilation_With_ImplementationSourceOutput_In_Same_Generator`.

Caching: `PreCompilation_Is_Cached_On_Second_Run`,
`PreCompilation_Reruns_When_AdditionalFile_Changes`,
`PreCompilation_Incremental_Step_Shows_Cached`,
`PreCompilation_And_Standard_Both_Cached_On_Second_Run`,
`PreCompilation_Generator_Does_Not_Invalidate_Other_Generators_CompilationProvider`,
`PreCompilation_Generator_Filtered_Other_Generator_Stays_Cached`,
`PreCompilation_RunResult_Trees_Are_In_Output_Compilation`,
`PreCompilation_Cached_Standard_Diagnostic_Tree_Is_In_Output_Compilation`,
`PreCompilation_Cached_SyntaxTree_Reference_Is_Stable_Across_Runs`.

Step tracking: `PreCompilation_Has_Distinct_Step_Name`.

Error handling: `PreCompilation_Throws_Reports_Error_And_Stops_Generator`,
`PreCompilation_Throws_Other_Generators_Unaffected`,
`PreCompilation_Trees_Preserved_When_Standard_Phase_Throws`,
`PreCompilation_Accessing_Compilation_Throws`, `PreCompilation_Using_SyntaxProvider_Throws`,
`PreCompilation_Failure_Skips_Standard_But_Recovers_On_Next_Run`,
`V1_Generator_Recovers_From_Standard_Phase_Exception_With_No_Input_Changes`,
`PreCompilation_Throws_With_Warning_Suppressed_Still_Stops_Generator`,
`Standard_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable`,
`Init_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable`.

Run results: `PreCompilationSources_Appear_In_GeneratedSources`,
`PreCompilation_And_Regular_Sources_Both_In_GeneratedSources`.

Edge cases: `No_PreCompilationOutputs_Registered_Is_Noop`, `PreCompilation_Empty_Callback_Is_Noop`,
`PreCompilation_Shared_Data_Flows_To_SourceOutput`,
`PreCompilation_IncrementalValuesProvider_Overload`.

Hint names: `PreCompilation_And_Standard_Output_Same_HintName`,
`PostInit_And_PreCompilation_Output_Same_HintName`, `PostInit_And_Standard_Output_Same_HintName`,
`PreCompilation_HintName_Conflict_Within_PreCompilation_Phase_Throws`.

---

## 9. Open questions

1. Will `RSEXPERIMENTAL007` be lifted before .NET 11 GA in November 2026? The API is in
   `PublicAPI.Unshipped.txt`, and the design document says *"Given that this is an experimental
   API, runtime enforcement with clear error messages is sufficient for the initial release."*
   The API review issue <https://github.com/dotnet/roslyn/issues/83089> is the tracking item.

2. Will the Razor source generator actually adopt `RegisterPreCompilationSourceOutput` in the
   .NET 11 SDK? The design document reports *"roughly 50% performance improvement"* from early
   experiments, but I did not verify whether
   `Microsoft.NET.Sdk.Razor.SourceGenerators` on `release/11.0.1xx` calls the new API. If it
   does, every project with `.razor` files gets pre-compilation trees in its initial compilation
   by default.

3. Will `dotnet/sdk#54646` and NuGet's `RestoreEnableAnalyzerAssets` be backported to
   `release/11.0.1xx` before GA? Both are currently main/dev only (milestone 12.0-preview1).
   As of 2026-09-03 the answer is no.

4. Does the design document's `RegisterDeclarationOutput` proposal
   ([dotnet/roslyn#81395](https://github.com/dotnet/roslyn/issues/81395)) land in a later wave?
   The design document explicitly says the two proposals are *"Complementary, Not Competing"*
   and sketches a five-stage pipeline. Nothing of `RegisterDeclarationOutput` is in `main` today.

5. The design document names the parse-options reparse helper `RequiresConstantTreeReparse`;
   the shipped code calls it `RequiresInputTreeReparse`. Cosmetic, but worth noting when
   cross-reading the document against the source.

6. Whether any host other than the command-line compiler and the Roslyn Workspaces layer
   (Rider, VS Code C# Dev Kit) passes `IncrementalGeneratorOutputKind.PreCompilation` in
   `DisabledOutputs`. Not investigated.

---

## 10. Source URLs

Design and specification
- <https://github.com/dotnet/roslyn/blob/main/docs/features/pre-compilation-source-outputs.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md>
- <https://github.com/dotnet/roslyn/pull/83088> (implementation)
- <https://github.com/dotnet/roslyn/issues/83089> (API review)
- <https://github.com/dotnet/roslyn/issues/81395> (two-phase alternative)
- <https://github.com/dotnet/roslyn/issues/53632> (configurable post-initialization)

Roslyn source (all `main` unless noted)
- `src/Compilers/Core/Portable/SourceGeneration/GeneratorDriver.cs`
- `src/Compilers/Core/Portable/SourceGeneration/CompilationCache.cs`
- `src/Compilers/Core/Portable/SourceGeneration/GeneratorState.cs`
- `src/Compilers/Core/Portable/SourceGeneration/GeneratorDriverOptions.cs`
- `src/Compilers/Core/Portable/SourceGeneration/IncrementalContexts.cs`
- `src/Compilers/Core/Portable/SourceGeneration/AdditionalSourcesCollection.cs`
- `src/Compilers/Core/Portable/SourceGeneration/RunResults.cs`
- `src/Compilers/Core/Portable/SourceGeneration/WellKnownGeneratorOutputs.cs`
- `src/Compilers/Core/Portable/SourceGeneration/IncrementalGeneratorRunStep.cs`
- `src/Compilers/Core/Portable/SourceGeneration/IncrementalStepRunReason.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/IIncrementalGeneratorOutputNode.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/AbstractSourceOutputNode.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/PreCompilationSourceOutputNode.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/DriverStateTable.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/SharedInputNodes.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/SyntaxInputNode.cs`
- `src/Compilers/Core/Portable/SourceGeneration/Nodes/GeneratorRunStateTable.cs`
- `src/Compilers/Core/Portable/CommandLine/CommonCompiler.cs`
- `src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs`
- `src/Compilers/Core/Portable/CodeAnalysisResources.resx`
- `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`
- `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`
- `src/Compilers/CSharp/Portable/CSharpResources.resx`
- `src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests_PreCompilation.cs`
- `src/Compilers/Core/MSBuildTask/Microsoft.Managed.Core.targets`
- `src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets`
- `src/Compilers/Core/MSBuildTask/ManagedCompiler.cs`
- `src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerAssemblyLoader.cs`
- `src/Compilers/Core/Portable/DiagnosticAnalyzer/IAnalyzerPathResolver.cs`
- `src/Compilers/Core/Portable/DiagnosticAnalyzer/IAnalyzerAssemblyResolver.cs`
- `src/Workspaces/CSharp/Portable/Workspace/LanguageServices/CSharpCompilationFactoryService.cs`
- `src/Workspaces/Core/Portable/Workspace/Solution/SolutionCompilationState.RegularCompilationTracker_Generators.cs`
- `eng/Versions.props`

SDK and NuGet
- <https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/ResolvePackageAssets.cs>
- <https://github.com/dotnet/sdk/pull/54646>
- <https://github.com/dotnet/sdk/issues/1212>
- `dotnet/sdk` `release/11.0.1xx` and `main`: `eng/Version.Details.xml`
- <https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.ProjectModel/ProjectRestoreMetadata.cs>
- <https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.ProjectModel/LockFile/LockFileFormat.cs>
- <https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.ProjectModel/LockFile/LockFileItem.cs>
- <https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.ProjectModel/LockFile/LockFileTargetLibrary.cs>
- <https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions>
- <https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files>

Related Roslyn pull requests
- <https://github.com/dotnet/roslyn/pull/77004> (analyzer assembly loading rework, .NET 10 wave)
- <https://github.com/dotnet/roslyn/pull/83875> (pooled objects across cancellation)
- <https://github.com/dotnet/roslyn/pull/83878> (input node duplicate elements)
- <https://github.com/dotnet/roslyn/pull/82113> (validate generator diagnostics after incremental updates)
- <https://github.com/dotnet/roslyn/pull/81934>, <https://github.com/dotnet/roslyn/pull/81992> (checksum algorithm)
- <https://github.com/dotnet/roslyn/pull/79609> (FAWMN primary constructors)
- <https://github.com/dotnet/roslyn/pull/84765> (shadow copy AV scan cache)
