# Aspect Pipeline Architecture

This document describes the Metalama aspect pipeline — how aspects are ordered and executed, and how the `CompilationModel` evolves through the pipeline. For the compilation model's internal architecture (declarations, builders, references, updatable collections), see [compilation-model.md](compilation-model.md).

## Pipeline Hierarchy

The pipeline has a four-level hierarchy:

```
Pipeline
  └── Stages (sequential)
        └── Aspect Layers (sequential, ordered by topological sort)
              └── Declaration Depths (sequential, base before derived)
                    └── Types at same depth (PARALLEL)
```

Each level produces an updated `CompilationModel` that flows to the next.

## Level 1: Stages

A **stage** (`PipelineStage`) groups a set of transformations that do not require the Roslyn compilation to be updated between them.

**Stage types:**

| Type | Description |
|---|---|
| `HighLevelPipelineStage` | Groups adjacent aspect layers that use the high-level API (`AspectDriver`). Contains multiple aspect layers processed by `PipelineStepsState`. |
| `LowLevelPipelineStage` | Wraps a single aspect backed by a low-level `IAspectWeaver`. Cannot be grouped with other aspects. |

**Stage creation** (in `AspectPipeline.TryInitialize`): All `OrderedAspectLayer` objects are grouped by `AspectDriver` using `GroupAdjacent` — consecutive layers sharing the same driver type become one stage. A low-level weaver always gets its own stage.

**Execution** (in `AspectPipeline.ExecuteAsync`): Stages execute sequentially. Each stage receives the previous stage's `AspectPipelineResult` (including the latest `CompilationModel` and Roslyn compilation) and produces a new result.

```
AspectPipeline.ExecuteAsync():
  for each stage in pipelineConfiguration.Stages:
    stage.ExecuteAsync(pipelineConfiguration, previousResult, ...)
      → AspectPipelineResult (feeds into next stage)
```

## Level 2: Aspect Layers

Within a `HighLevelPipelineStage`, processing is orchestrated by `PipelineStepsState`, which manages a `SkipListDictionary<PipelineStepId, PipelineStep>`.

### What Is an Aspect Layer?

An aspect class has one or more **layers**. Most aspects have exactly one layer — the default layer (with a `null` name). Exceptionally, an aspect class may define additional layers via the `[Layers("Layer1", "Layer2")]` attribute. When an aspect has multiple layers, `BuildAspect` is called once per layer.

Layers are identified by `AspectLayerId` (aspect class + layer name) and ordered by `OrderedAspectLayer.Order` — a topological sort based on aspect dependencies, with name as tiebreaker.

### Aspect Layer Ordering

Aspect layers are the primary ordering dimension within a stage. All depths of layer A are processed before any depth of layer B (assuming A is ordered before B).

## Level 3: Declaration Depth

Within a single aspect layer, aspect instances are further grouped by the **declaration depth** of their target.

### How Depth Is Computed

Declaration depth (`CompilationModel.GetDepth()`) is a value derived from the inheritance hierarchy:

| Declaration | Depth |
|---|---|
| External assemblies | 0 |
| Compilation | 0 |
| Assembly references | 1 |
| Global namespace | 2 |
| Namespaces | 3 + number of dots in full name |
| Named types | `max(baseType.Depth, interfaces.Depth, declaringType.Depth, namespace.Depth) + 1` |
| Members (methods, properties, etc.) | `containingDeclaration.Depth + 1` |
| Parameters | `containingDeclaration.Depth + 1` |

This guarantees that base types always have lower depth than derived types.

### Pipeline Step Identity

Each pipeline step is identified by a `PipelineStepId`, a 4-tuple:

```
PipelineStepId(AspectLayerId, AspectTargetTypeDepth, AspectTargetDepth, AdviceTargetDepth)
```

`PipelineStepIdComparer` orders steps by:
1. **Aspect layer order** — topological distance, then name as tiebreaker
2. **AspectTargetTypeDepth** — depth of the target's closest named type in the inheritance hierarchy
3. **AspectTargetDepth** — absolute depth of the aspect target (encodes type=0, member=+1, parameter=+2 relative to type)
4. **AdviceTargetDepth** — depth of the advice's target declaration

Steps are iterated **sequentially** by `PipelineStepsState.ExecuteAsync()`. Each step produces an immutable `CompilationModel` that becomes the input to the next step:

```csharp
while ( enumerator.MoveNext() )
{
    this._currentStep = enumerator.Current.Value;
    var compilation = this.LastCompilation;
    this.LastCompilation = await this._currentStep.ExecuteAsync( compilation, ... );
}
```

### Why Depth Matters

Processing base types before derived types ensures that when a derived type's aspect executes, it sees all transformations applied to its base type. This is essential for:

- **Inheritable aspects**: An aspect on `Base` may introduce members that the aspect on `Derived` needs to see
- **Constructor parameter pulling**: Parameters introduced on `Base` must exist before `Derived` can add `: base(...)` arguments for them

## Level 4: Parallel Type Processing

Within a single pipeline step (one layer at one depth), aspect instances on **different types** execute in parallel via `RunConcurrentlyAsync`.

### Why Parallelism Is Safe

1. **Types at the same depth are independent**: Inheritance guarantees that types at the same depth do not derive from each other. Transformations on one type cannot affect another at the same depth.

2. **Each type gets its own mutable clone**: `CreateMutableClone()` is called per type. The underlying `ImmutableDictionary` collections are shared read-only; only the mutable clone's local modifications are thread-local.

3. **Results are merged after completion**: Observable transformations from all types are collected in a `ConcurrentQueue<ITransformation>` and merged into a single new immutable compilation after all types finish.

### Processing Structure

```
ExecuteAspectLayerPipelineStep.ExecuteAsync():
  (one step = one aspect layer at one declaration depth)

  Immutable compilation v(n) (includes all changes from earlier layers and lower depths)
    │
    ├── Group aspect instances by GetClosestNamedType()
    │
    ├── RunConcurrentlyAsync(instancesByType):  ← PARALLEL across types at same depth
    │   │
    │   ├── Type A: aspect1 → aspect2 → aspect3  (SEQUENTIAL within type)
    │   │   Each produces observable transformations → ConcurrentQueue
    │   │
    │   ├── Type B: aspect1 → aspect2             (SEQUENTIAL within type)
    │   │   Each produces observable transformations → ConcurrentQueue
    │   │
    │   └── Type C: aspect1                       (SEQUENTIAL within type)
    │       Produces observable transformations → ConcurrentQueue
    │
    └── compilation.WithTransformationsAndAspectInstances(all observable transformations)
        → Immutable compilation v(n+1) (output → input to next step)
```

Within a single type, if there are multiple aspect instances (e.g., from different aspect sources), they execute **sequentially** — each aspect sees the immutable snapshot produced by the previous aspect on that type.

### Example: Inheritance-Based Ordering

Consider an inheritable aspect applied to `Base → Derived → MostDerived`:

```
Step 1: Layer=MyAspect, Depth=5 (where Base lives)
  → Process Base in isolation → CompilationModel v1

Step 2: Layer=MyAspect, Depth=6 (where Derived lives)
  → Derived sees v1 (including Base's transformations) → CompilationModel v2

Step 3: Layer=MyAspect, Depth=7 (where MostDerived lives)
  → MostDerived sees v2 (including Base + Derived transformations) → CompilationModel v3
```

If `Sibling1` and `Sibling2` are both at depth 6, they run in parallel in the same step.

## Mutable Compilations and Aspect Execution

### The Role of Mutable Compilations

When an aspect executes its `BuildAspect` method, it receives an `IAspectBuilder` which provides advice methods (e.g., `IntroduceMethod`, `Override`, `IntroduceParameter`). Each advice call produces one or more **transformations**. These transformations need to be:

1. **Visible to the same aspect** — so subsequent advice calls can see earlier introductions (e.g., an aspect introduces a field, then overrides a method that references it).
2. **Collected for replay** — so the pipeline can create the next immutable compilation version after the aspect finishes.

This is achieved through a **mutable compilation** that exists only during aspect execution.

### Lifecycle of a Mutable Compilation

```
ProcessTypeAsync():

  currentCompilation (immutable, from previous aspect or lower depth)
    │
    ├── CreateMutableClone() → mutableCompilation (IsMutable = true)
    │
    ├── AspectDriver.ExecuteAspectAsync(initialCompilation, mutableCompilation)
    │   │
    │   ├── Creates AdviceFactoryState(mutableCompilation)
    │   │
    │   ├── Aspect calls builder.IntroduceMethod(...), builder.Override(...), etc.
    │   │   │
    │   │   └── AdviceFactoryState.AddTransformations(transformations):
    │   │       ├── Collect in Transformations list (for later replay)
    │   │       └── If observable: apply to mutableCompilation via AddTransformation()
    │   │           → Subsequent advice calls see the introduced declarations
    │   │
    │   └── Returns AspectInstanceResult with all collected transformations
    │
    ├── mutableCompilation.CreateImmutableClone() → newCompilation (IsMutable = false)
    │   └── Becomes currentCompilation for next aspect on same type
    │
    └── Observable transformations enqueued to ConcurrentQueue (for cross-type merge)
```

### What the Aspect Sees

The `IAspectBuilder` exposes two compilation views:

- **`builder.Target`** — the target declaration in the **initial** (read-only) compilation, before any advice from this aspect
- **`builder.AdvisedTarget`** — the target declaration in the **mutable** compilation, reflecting modifications from earlier advice calls within the same aspect

### Observable vs Non-Observable Transformations

Not all transformations are applied to the mutable compilation. `TransformationObservability` controls visibility:

| Observability | Applied to mutable model? | Replayed to next immutable? | Examples |
|---|---|---|---|
| `Always` | Yes | Yes | `IntroduceParameterTransformation`, `IntroduceMethodTransformation`, type introductions |
| `CompileTimeOnly` | Yes | Yes | `SetHasImplementationTransformation`, `AddAnnotationTransformation`, attribute introductions |
| `None` | No | No (linker only) | Override transformations, contract transformations, template statement insertions |

**Why `None`?** Override and template transformations affect **syntax generation** (the linker's job), not the code model's declaration structure. These transformations are collected in `AspectInstanceResult.Transformations` and passed directly to the linker.

### Committing: From Mutable to Immutable

When an aspect finishes successfully (`AdviceOutcome.Default`), the mutable compilation is frozen:

1. `CreateImmutableClone()` copies the mutable model with `IsMutable = false`
2. This becomes the baseline for the next aspect on the same type

If the aspect fails (`AdviceOutcome.Error` or `Ignore`), the mutable compilation is **discarded** — its changes are not committed.

### The Replay Step

After all types at a given depth complete, `WithTransformationsAndAspectInstances()` creates the next immutable compilation by **replaying** all observable transformations on the prototype:

1. Creates a new `CompilationModel` with `IsMutable = true`
2. Calls `AddTransformation()` for each observable transformation
3. Sets `IsMutable = false`
4. Rebuilds the `DerivedTypeIndex` and attribute index

This replay is necessary because the mutable compilations used during execution were independent clones — the new immutable must incorporate all changes into a single coherent snapshot.

## Key Files

| File | Role |
|---|---|
| `AspectPipeline.cs` | Main pipeline orchestrator; stage creation and sequential execution |
| `PipelineStage.cs` | Abstract base class for stages |
| `HighLevelPipelineStage.cs` | Groups adjacent high-level aspect layers; creates `PipelineStepsState` |
| `LowLevelPipelineStage.cs` | Wraps a single `IAspectWeaver` |
| `PipelineStageConfiguration.cs` | Stage config record: kind, aspect layers, optional weaver |
| `PipelineStepsState.cs` | Sequential step iteration, step creation with depth computation |
| `PipelineStepId.cs` | 4-tuple identifier: `(AspectLayerId, TypeDepth, TargetDepth, AdviceTargetDepth)` |
| `PipelineStepIdComparer.cs` | Ordering: layer → TypeDepth → TargetDepth → AdviceTargetDepth |
| `ExecuteAspectLayerPipelineStep.cs` | Parallel type processing within a step, mutable→immutable lifecycle |
| `Aspects/AspectClass.cs` | Layer initialization from `[Layers(...)]` attribute |
| `AspectOrdering/OrderedAspectLayer.cs` | Topologically-sorted layer with `Order` property |
| `Advising/AdviceFactoryState.cs` | Collects transformations AND applies observable ones to mutable model |
| `Aspects/AspectDriver.cs` | Executes single aspect: creates AdviceFactoryState |
| `Aspects/AspectBuilder.cs` | `IAspectBuilder`: exposes `Target` (initial) and `AdvisedTarget` (mutable) |
| `Transformations/TransformationObservability.cs` | `Always`, `CompileTimeOnly`, `None` |
