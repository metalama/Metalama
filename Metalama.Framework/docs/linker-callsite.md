# Linker Call-Site Advice

This document describes the **call-site advice** mechanism in the Metalama linker — the system responsible for rewriting *consumer* sites of a type or member when an aspect requires action at the place of use rather than at the place of declaration.

The current concrete instance of this mechanism is `[OnInitialized]` (`IInitializable` wrapping), but the infrastructure is designed to host additional call-site advice kinds without churning the linker plumbing.

## Why call-site advice?

Most Metalama advice rewrites the *declaration* it targets: `[Override]` rewrites the method body, `[Introduce]` adds new members, `[OnFieldChanged]` rewrites property setters. The linker's substitution pipeline is optimized for that shape: each transformation is anchored to a single declaration, and the injection step rewrites the declaration's syntax tree directly.

`[OnInitialized]` is different. The aspect is declared on a *type*, but to honor the contract the linker must wrap **every `new T()` and `with`-expression call site** in `InitializableExtensions.WithInitialize(...)` so that the registered initializers run at the moment the instance is observed by user code. The rewrite happens at the consumer side, anywhere in the compilation, including:

- Source files that declare no aspect at all.
- Aspect-introduced syntax trees.
- Constructor arguments, collection initializers, expression-bodied members, lambda bodies — any expression position.

There is no single declaration the linker can hook. The only viable strategy is to **walk every syntax tree in the intermediate compilation, find the matching expressions, and rewrite them**. That walker is what `OnInitializedCallSiteFinder` does, and the rest of this document is about how the linker orchestrates and short-circuits it.

## Architecture overview

The pipeline for call-site advice has three phases inside the linker, plus a propagation path that lets a project tell its downstream consumers whether it produces any matching types:

```
                       ┌──────────────────────────────────────────┐
                       │ AspectPipeline (compile-time)            │
                       │  ─ runs aspects                          │
                       │  ─ computes ContainsInitializableTypes   │
                       │    via GetDerivedTypes(IInitializable)   │
                       │  ─ writes the flag into                  │
                       │    TransitiveAspectsManifest             │
                       └──────────────────┬───────────────────────┘
                                          │ (PE resource: Metalama.InheritableAspects.bin)
                                          ▼
                       ┌──────────────────────────────────────────┐
                       │ TransitivePipelineContributorSource       │
                       │  ─ reads each PE reference's manifest    │
                       │  ─ aggregates                            │
                       │    ReferencesContainInitializableTypes    │
                       └──────────────────┬───────────────────────┘
                                          │
                                          ▼
       PipelineContributorSources ──▶ LinkerPipelineStage
                                          │
                                          │  packages the bool into
                                          │  CallSiteAdviceInfo
                                          ▼
       AspectLinkerInput ──▶ LinkerInjectionStep ──▶ LinkerInjectionStepOutput
                                                          │
                                                          ▼
                                                  LinkerAnalysisStep
                                                  ─ closure check
                                                  ─ if needed: run
                                                    OnInitializedCallSiteFinder
                                                  ─ build SubstitutionGenerator
                                                          │
                                                          ▼
                                                 OnInitializedObjectCreationSubstitution
                                                 (per call-site rewriter)
```

The three phases inside the linker are:

1. **Closure check** (`LinkerAnalysisStep.ExecuteAsync`) — decide whether the walker needs to run at all.
2. **Walker** (`OnInitializedCallSiteFinder`) — visit every syntax tree of the intermediate compilation, resolve symbols, and produce a list of `ObjectCreationCallSiteReference`s describing the rewrites.
3. **Substitution** (`OnInitializedObjectCreationSubstitution`, registered with `SubstitutionGenerator`) — perform the per-call-site rewrite during the linker's substitution pass.

This document focuses on phases 1 and the propagation path that feeds it, because that is where the optimization and the cross-project plumbing live. Phases 2 and 3 are local rewriters with no cross-project state.

## The closure check

The walker is expensive: it visits every syntax node of every syntax tree in the intermediate compilation, performs semantic resolution on each `ObjectCreationExpressionSyntax` / `ImplicitObjectCreationExpressionSyntax` / `WithExpressionSyntax`, and constructs an `InitializableTypeRegistry`. On a project that contains no `IInitializable` implementer in its closure the walker is provably a no-op, so we short-circuit it.

In `LinkerAnalysisStep.cs` (around line 279):

```csharp
var closureContainsInitializable =
    input.IntermediateCompilation.IsPartial
    || input.CallSiteAdviceInfo.ReferencesContainInitializableTypes
    || input.InputCompilationModel.GetDerivedTypes( typeof(IInitializable) ).Any();

if ( closureContainsInitializable )
{
    var initializableTypeRegistry = new InitializableTypeRegistry( input.IntermediateCompilation.CompilationContext );
    onInitializedCallSites = await new OnInitializedCallSiteFinder( … ).FindCallSitesAsync( cancellationToken );
}
else
{
    onInitializedCallSites = Array.Empty<ObjectCreationCallSiteReference>();
}
```

The OR has three terms covering disjoint scopes plus a correctness escape hatch:

### Term 2: `CallSiteAdviceInfo.ReferencesContainInitializableTypes` — referenced assemblies

Aggregated by `TransitivePipelineContributorSource` from each referenced assembly's `TransitiveAspectsManifest.ContainsInitializableTypes`. This is the **only** way to discover whether a referenced library declares an `IInitializable` implementer, because the next term — `GetDerivedTypes` — explicitly excludes referenced-assembly types.

### Term 3: `GetDerivedTypes(IInitializable).Any()` — current compilation

`CompilationModel.GetDerivedTypes(Type)` is an O(1) `DerivedTypeIndex` dictionary lookup. The index is built once per `CompilationModel` and includes:

- Source types declared in the current compilation.
- Aspect-introduced types (the index is rebuilt on the post-aspect `CompilationModel`).

It explicitly does **not** include types declared in referenced assemblies — the underlying `DerivedTypeIndex.GetDerivedTypesInCurrentCompilation` filters them out via `IsContainedInCurrentCompilation`. This is why term 2 is required: a project with zero local implementers can still construct a type from a referenced library that has one, and we must run the walker for that case.

### Why both terms are required

The two terms cover disjoint scopes by design. Removing either one would introduce a correctness bug:

- **Without term 2**: a project that declares no `IInitializable` of its own but does `new SomeReferencedLib.Foo()` (where `Foo : IInitializable`) would skip the walker and emit an unwrapped `new`, breaking the contract.
- **Without term 3**: a project with no Metalama references but containing its own `IInitializable` implementer would skip the walker for the same reason.

### Term 1: `IsPartial` — design-time correctness escape hatch

The compile-time pipeline always works on a *complete* `PartialCompilation` (`CompleteImpl`), so terms 2 and 3 are exact: the `DerivedTypeIndex` covers every tree in the project.

The design-time pipeline (today: preview via `PreviewAspectPipeline`) works on a **partial** `PartialCompilation` (`PartialImpl`) that contains only the previewed tree plus its tracked dependencies. Its `DerivedTypeIndex` is built from that subset. But `OnInitializedCallSiteFinder` iterates `compilationContext.Compilation.SyntaxTrees` — the **full** Roslyn `Compilation.SyntaxTrees`, not the partial subset — so an `IInitializable` implementer declared in a tree outside the partial closure would be missed by `GetDerivedTypes` even though the walker still needs to wrap its call sites.

Preview is a rare interactive operation, so the walker cost is irrelevant compared to correctness. We simply force the walker to run whenever the compilation is partial. This also has a useful consequence: the design-time pipeline does not need to track the flag at all. `DesignTimeAspectPipelineResult.ContainsInitializableTypes` returns a conservative `true` for any cross-project design-time consumer, and there is no per-tree scan, no incremental aggregation, and no propagation through `DesignTimeAspectPipelineResult.Update`.

## Cross-project propagation path

A project tells its downstream consumers "I contain `IInitializable` implementers" through a single boolean stamped on the manifest it embeds in its PE output. The full path:

1. **Compile-time pipeline computes the flag.** In `CompileTimeAspectPipeline.ExecuteCoreAsync`:

   ```csharp
   var containsInitializableTypes = result.Value.LastCompilationModel
       .GetDerivedTypes( typeof(IInitializable) )
       .Any();
   ```

   This is an O(1) `DerivedTypeIndex` lookup on the post-aspect compilation model, so it sees both source-declared and aspect-introduced implementers.

2. **Manifest emission is gated to also fire on the flag.** The condition that decides whether to write `Metalama.InheritableAspects.bin` into the PE was extended to include `|| containsInitializableTypes`. Without this, a project whose only "interesting" content is the new flag would silently fail to persist it.

3. **`TransitiveAspectsManifest` carries the flag.** A new `bool ContainsInitializableTypes { get; }` was added to both the interface (`ITransitiveAspectsManifest`) and the concrete class. The serializer writes it under `nameof(ContainsInitializableTypes)`, and the deserializer reads it via `TryGetValue` defaulting to `false` — preserving backward compatibility with manifests written by older Metalama versions.

4. **`TransitivePipelineContributorSource` aggregates across references.** This is the single loader of transitively-referenced manifests: it iterates `compilationContext.Compilation.References`, reads `Metalama.InheritableAspects.bin` from each PE via `MetadataReader.TryGetMetadata`, deserializes, and stores the results in a per-assembly dictionary. A new property is computed once at construction time:

   ```csharp
   public bool ReferencesContainInitializableTypes { get; }
       = manifests.Values.Any( m => m.ContainsInitializableTypes );
   ```

5. **`PipelineContributorSources` carries the bool forward** through the pipeline alongside the existing optional `IExternalAnnotationProvider?` / `IExternalHierarchicalOptionsProvider?` slots. `Add(...)` OR-merges it.

6. **`LinkerPipelineStage` packages the bool into `CallSiteAdviceInfo`** at the linker entry boundary:

   ```csharp
   new AspectLinkerInput(
       …,
       new CallSiteAdviceInfo( input.ContributorSources.ReferencesContainInitializableTypes ) )
   ```

   From this point on, the linker's internal plumbing carries the struct, not the bare bool.

7. **`AspectLinkerInput` → `LinkerInjectionStep` → `LinkerInjectionStepOutput` → `LinkerAnalysisStep`.** Each step holds a `CallSiteAdviceInfo CallSiteAdviceInfo { get; }` property and copies it through. The closure check in `LinkerAnalysisStep` reads `input.CallSiteAdviceInfo.ReferencesContainInitializableTypes`.

## The design-time path (preview)

`DesignTimeAspectPipelineResult` implements `ITransitiveAspectsManifest`, so it must answer `ContainsInitializableTypes` for any design-time consumer (e.g. another design-time pipeline reading a sibling project via `TransitiveCompilationService` for cross-Metalama-version references).

Because the design-time linker always force-runs the walker via the `IsPartial` term, the design-time pipeline has no reason to track this flag. We therefore return the **safe default `true`** from both:

- `DesignTimeAspectPipelineResult.ITransitiveAspectsManifest.ContainsInitializableTypes`
- The `TransitiveAspectsManifest.Create` call in `GetSerializedTransitiveAspectManifest`

`true` is the correct mapping of "unknown": it can only cause an unnecessary walker run (performance pessimization), while `false` would risk skipping required `WithInitialize` wrapping (correctness bug).

The earlier design tracked the flag incrementally inside `DesignTimeAspectPipelineResult` (per-tree scan, `ImmutableHashSet` of trees containing implementers, index/un-index logic in `Update`). All of that was removed once we recognized that the partial-compilation problem made the per-tree information unreliable for the linker anyway. The current design-time path has zero tracking overhead for this feature.

## Why `CallSiteAdviceInfo` is a struct, not a bool

Inside the linker, the flag is wrapped in a `record struct CallSiteAdviceInfo` (`Linking/CallSiteAdviceInfo.cs`):

```csharp
internal readonly record struct CallSiteAdviceInfo( bool ReferencesContainInitializableTypes );
```

The struct is the linker's vocabulary for "facts that decide whether call-site advice walkers need to run". A future second call-site advice kind (e.g. some other interface that triggers consumer-side rewriting) would need to:

1. Add a new property to `CallSiteAdviceInfo`.
2. Update the single `new CallSiteAdviceInfo(...)` construction in `LinkerPipelineStage`.
3. Consume the new property in `LinkerAnalysisStep`.

No other linker file needs to change — the struct is already threaded through `AspectLinkerInput` → `LinkerInjectionStep` → `LinkerInjectionStepOutput` → `LinkerAnalysisStep`. Upstream of the linker, the cross-project propagation path stays bare-bool because each kind of advice has its own, independent aggregation logic in `TransitivePipelineContributorSource`.

## Cost summary

| Scenario | Walker runs? | Reason |
|---|---|---|
| Compile-time, current project has `IInitializable` implementer | Yes | Term 3 (`GetDerivedTypes`, O(1)) |
| Compile-time, only a referenced library has implementers | Yes | Term 2 (`ReferencesContainInitializableTypes`) |
| Compile-time, no implementer anywhere in closure | **No** | All three terms `false` — walker skipped entirely, including `InitializableTypeRegistry` construction and symbol resolution |
| Design-time preview (any project) | Yes | Term 1 (`IsPartial`), unconditionally |

The closure check itself is three boolean reads plus an O(1) dictionary lookup — negligible compared to even the cheapest walker run.

## Files

| File | Role |
|---|---|
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.cs` | Closure check, walker invocation |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.OnInitializedCallSiteFinder.cs` | The walker itself |
| `Metalama.Framework.Engine/Linking/InitializableTypeRegistry.cs` | Symbol cache used by the walker |
| `Metalama.Framework.Engine/Linking/Substitution/OnInitializedObjectCreationSubstitution.cs` | Per-call-site rewriter |
| `Metalama.Framework.Engine/Linking/CallSiteAdviceInfo.cs` | The vocabulary struct |
| `Metalama.Framework.Engine/Linking/AspectLinkerInput.cs` | Linker entry point — holds `CallSiteAdviceInfo` |
| `Metalama.Framework.Engine/Linking/LinkerInjectionStep.cs` | Threads the struct through |
| `Metalama.Framework.Engine/Linking/LinkerInjectionStepOutput.cs` | Holds the struct for the analysis step |
| `Metalama.Framework.Engine/Pipeline/CompileTime/LinkerPipelineStage.cs` | Packages the bool into the struct at the linker boundary |
| `Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs` | Computes the per-project flag, gates manifest emission, calls `TransitiveAspectsManifest.Create` with it |
| `Metalama.Framework.Engine/Aspects/TransitiveAspectsManifest.cs` | Serializes/deserializes the flag (with backward-compat default `false`) |
| `Metalama.Framework.Engine/Aspects/ITransitiveAspectsManifest.cs` | Interface property |
| `Metalama.Framework.Engine/Aspects/TransitivePipelineContributorSource.cs` | Aggregates the flag across PE references |
| `Metalama.Framework.Engine/Pipeline/PipelineContributorSources.cs` | Carries the bool through the pipeline |
| `Metalama.Framework.Engine/Pipeline/AspectPipeline.cs` | Constructs `PipelineContributorSources` with the aggregated bool |
| `Metalama.Framework.DesignTime/Pipeline/DesignTimeAspectPipelineResult.cs` | Returns conservative `true` for design-time consumers |
