# Open-source extension points: adviser bridge, pipeline hook and factory

> Part of the [call-site interceptors design](README.md). Previous: [09-premium-engine.md](09-premium-engine.md) | Next: [10b-oss-linker-and-templates.md](10b-oss-linker-and-templates.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 10. Open-source extension points

All changes of this section target the 2027.0 line (`develop/2027.0`). The 2026.1 line receives only the fixes listed in section [11.2](11-delivery-plan.md#112-fix-track). No proposed public name, comment or diagnostic mentions interceptors (R4, PO13).

### 10.1 Overview

| Id | Public surface (PROPOSED) | Namespace | Purpose |
|---|---|---|---|
| A1 | `static class AdviserExtensibility` with `GetExtensionContext(this IAdviser)` | `Metalama.Framework.Engine.Advising` | Bridge from any `IAdviser` to the engine state. |
| A2 | `sealed class AdviserExtensionContext` | same | Owner, aspect target, template provider, disposal check, query creation, origin capture. |
| A3 | `sealed class ExtensionContributionOrigin` with `Capture(IQueryOwner)` | `Metalama.Framework.Engine.Extensibility` | Opaque attribution of a contribution, consumed by the transformation factory. |
| B1 | `PipelineExtension.ExecuteTransformingContributorsAsync` | same | Stage-end hook before the linker. |
| B2 | `sealed class ExtensionTransformationContext` | same | Inputs of the hook. |
| T1 | `sealed class ExtensionTransformationFactory` and its request and handle types | `Metalama.Framework.Engine.Extensibility.Transformations` | Synthesized classes, methods and local functions; redirections of invocations, awaits, method references (`RedirectMethodReference`, RC44) and accessor uses (`RedirectAccessor`, RC48, RC50), which the factory stores in a dictionary keyed by syntax tree and by source node (section [10.4.5](#1045-call-site-redirections), RC39). |
| T2 | `sealed class ProceedBinding`, `enum ProceedMultiplicity` | same | Custom `meta.Proceed()` of synthesized methods. A binding whose method is an accessor emits the property or event access instead of a call (section [10.4.4](#1044-synthesized-methods-and-proceed-bindings)). |
| M1 | `interface IMetaExtension`; `meta.GetExtension<T>()`; `meta.TryGetExtension<T>(out T?)` | `Metalama.Framework.Aspects` (assembly `Metalama.Framework`) | Compile-time objects that an extension attaches to a template expansion, inherited by called templates (section [10.6.6](10b-oss-linker-and-templates.md#1066-meta-extensions-per-expansion-extension-data), RC40). |
| M2 | `SynthesizedMethodTemplate.MetaExtensions` | `Metalama.Framework.Engine.Extensibility.Transformations` | The meta extensions of the expansion of a synthesized method. It replaces `ExpansionServices`. |
| X1 | `static class SourceExpressionFactory` with `CreateInspectionOnly( ExpressionSyntax, IType )` | `Metalama.Framework.Engine.Templating` | An `ISourceExpression` over source syntax that compile-time code can inspect but not emit (section [10.6.8](10b-oss-linker-and-templates.md#1068-inspection-only-source-expressions), LAMA0297). |
| X2 | `static class SourceExpressionExtensions` with `GetSourceSyntax( this IExpression )` | `Metalama.Framework.Engine.CodeModel` (assembly `Metalama.Framework.Sdk`) | The Roslyn syntax of an expression that wraps source syntax (section [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g)). |
| C1 | `ContributorKind.IsProjectLocal` (init) | `Metalama.Framework.Engine.Extensibility` | Design-time results that are never exported. |
| C2 | `DesignTimeAspectPipelineResultExtensionCollection.HasExportedContent` | same | Manifest gate that ignores project-local results. |
| D1 | `ReferenceKinds.Await = 1L << 28` | `Metalama.Framework.Code` | Reference kind for `await` expressions. |
| D2 | `sealed class SourceReferenceIndexService` (project service); `PipelineExtension.GetSourceIndexRequirements( SourceIndexRequirementsContext )` with the records `SourceIndexRequirementsContext` and `SourceIndexRequirements`; `ExtensionTransformationContext.SourceReferenceIndex`; `interface IDesignTimeReferenceIndexRequirementsProvider` | `Metalama.Framework.Engine.ReferenceGraph` and `Metalama.Framework.Engine.Extensibility` | One index of source references per high-level stage, built lazily from the merged requirements of all extensions, restricted to declaration roots when every consumer returned roots, and a per-`SemanticModel` index at design time (section [10.7.5](10c-oss-reference-graph-design-time.md#1075-shared-index-of-source-references)). It contains no interceptor concept (R4). `OutboundReferenceIndexBuilder` stays internal: an earlier version of this design made it public, which is no longer needed. |
| D3 | `OperatorKind.NullCoalescingAssignment`, appended as the last member | `Metalama.Framework.Code` | The kind of `??=`, which extensions report for accessor sites (RC51). The enumeration already contains kinds that C# cannot overload, such as `Concatenate` and `Like` (FW27 `Code\OperatorKind.cs:264, 279`). Appending the member keeps the values of the existing members, which have no explicit values. |
| E1 | `SymbolDictionaryKey.CreateLookupKey` becomes public | `Metalama.Framework.Engine.Utilities.Roslyn` | Lookups that compute no identifier string when the hash codes differ. |
| E2 | `[Durable]` on the existing `MethodTemplateSelector` | `Metalama.Framework.Advising` | A template selector can be stored in a durable registration. The struct contains only strings and Boolean values (FW27 `Advising\MethodTemplateSelector.cs:57-58`). |
| E3 | `DesignTimeHierarchicalOptions.CreateManager` (section [10.8.6](10c-oss-reference-graph-design-time.md#1086-hierarchical-options-at-design-time)) | `Metalama.Framework.Engine.HierarchicalOptions` | An initialized hierarchical options manager for a design-time compilation. |

Internal changes support these types: owner propagation in `AdviceFactory<T>`, a stage index on `HighLevelPipelineStage`, linker changes for call-site identity and injection, template-engine changes, walker gap fixes, the `ReducedFrom` normalization, the merge of indexer options per reference kind, and the fix of the design-time default bucket.

### 10.2 Adviser bridge (B2a)

#### 10.2.1 EXISTING behavior that the bridge must cover

`IAdviser` has six implementations. Every advice verb reaches the engine by casting to the internal `IAdviserInternal` (FW27 `Aspects\AdviserExtensions.cs:1912`; `Advising\IAdviserInternal.cs:7-10`).

| Implementation | Where | What `IAdviserInternal.AdviceFactory` returns | Public owner today |
|---|---|---|---|
| `AspectBuilder<T>` | ENG27 `Aspects\AspectBuilder.cs:25, 63` | its root `AdviceFactory<T>` | itself (`IQueryOwner`) |
| `AdviceFactory<T>` | ENG27 `Advising\AdviceFactory.cs:40, 2434` | itself | none |
| `TypeFabricDriver.Amender` | ENG27 `Fabrics\TypeFabricDriver.cs:119, 143, 183` | a factory created by `WithTemplateClassInstance` from the aggregate aspect's factory | itself (through `BaseAmender<T>`, ENG27 `Fabrics\FabricDriver.BaseAmender.cs:25`) |
| `IntroductionAdviceResult<T>` | ENG27 `AdviceImpl\Introduction\IntroductionAdviceResult.cs:16, 54` | the factory that executed the advice | none |
| `AdviserExtensions.Adviser<T>` (private) | FW27 `Aspects\AdviserExtensions.cs:2052-2075, 2084-2113` | the factory returned by `WithTemplateProvider` | none |
| `AddAttributeAdviceResult` | ENG27 `AdviceImpl\Attributes\AddAttributeAdviceResult.cs:17, 38-48` | none: it does not implement `IAdviserInternal`, and its `Target` and `With` throw `NotSupportedException` | none |

`ImplementInterfaceAdvice.ImplementationResult` implements `IAdviserInternal`, but not `IAdviser`, because `IInterfaceImplementationAdviser` does not derive from `IAdviser` (FW27 `Advising\IInterfaceImplementationAdviser.cs:30`). The bridge cannot receive it. The result of an introduction whose outcome is Error or Ignore is an `IAdviser` whose `Target` throws `InvalidOperationException` (ENG27 `Advising\AdviceResult.cs:47-60`).

Constraints:

1. `Adviser<T>.With` creates a new `Adviser<T>` around the same factory, so the factory's own target can differ from `adviser.Target`. The bridge takes the target from `IAdviser.Target`.
2. `AdviceFactory<T>` has no owner. For a type fabric, its aspect instance is the aggregate `FabricAspect<T>` instance (ENG27 `Fabrics\TypeFabricDriver.cs:143`), while the amender reports the fabric predecessor (ENG27 `Fabrics\FabricDriver.BaseAmender.cs:104`).
3. The template provider is private state (`_templateClassInstance`, ENG27 `Advising\AdviceFactory.cs:43, 211`).
4. Disposal is tracked by `AdviceFactoryState.IsDisposed` (ENG27 `Advising\AdviceFactoryState.cs:104-109`).

The engine can see `IAdviserInternal`, because `Metalama.Framework` grants `InternalsVisibleTo` to the engine assemblies (FW27 `Metalama.Framework.csproj:15-36`). No premium assembly needs `InternalsVisibleTo`.

#### 10.2.2 PROPOSED public API

```csharp
// NEW FILE: ENG27 Advising/AdviserExtensibility.cs
namespace Metalama.Framework.Engine.Advising;

/// <summary>Gives extensions access to the engine state behind an <see cref="IAdviser"/>.</summary>
[PublicAPI]
public static class AdviserExtensibility
{
    /// <summary>Gets the extension context of an adviser.</summary>
    /// <param name="adviser">An adviser created by the engine, of any kind.</param>
    /// <returns>A new context. The context is valid only during the execution of the aspect or fabric that received the adviser.</returns>
    /// <exception cref="ArgumentException">The adviser was not created by the engine.</exception>
    /// <exception cref="NotSupportedException">The adviser is the result of an attribute introduction.</exception>
    /// <exception cref="InvalidOperationException">The adviser is the result of an introduction whose outcome is Error or Ignore, so it has no target.</exception>
    public static AdviserExtensionContext GetExtensionContext( this IAdviser adviser )
    {
        if ( adviser is AddAttributeAdviceResult )
        {
            throw new NotSupportedException( "The result of an attribute introduction cannot be used as an adviser." );
        }

        if ( adviser is not IAdviserInternal internalAdviser )
        {
            throw new ArgumentException( "The adviser was not created by the Metalama engine.", nameof(adviser) );
        }

        var factory = (IAdviceFactoryImpl) internalAdviser.AdviceFactory;

        // An adviser that is itself an owner (AspectBuilder<T>, TypeFabricDriver.Amender) is the owner.
        // Otherwise, the owner is the one that the factory carries.
        return factory.CreateExtensionContext( adviser.Target, adviser as IQueryOwner );
    }
}
```

```csharp
// NEW FILE: ENG27 Advising/AdviserExtensionContext.cs
namespace Metalama.Framework.Engine.Advising;

/// <summary>Exposes to an extension the state of the engine behind an <see cref="IAdviser"/>.</summary>
/// <remarks>
/// An instance references the compilation of the aspect layer, so it must not be stored in an object that outlives the
/// pipeline run.
/// </remarks>
[PublicAPI]
public sealed class AdviserExtensionContext
{
    internal AdviserExtensionContext(
        IDeclaration target,
        IQueryOwner owner,
        AdviceFactoryState state,
        TemplateClassInstance? templateClassInstance,
        IDeclaration aspectTarget );

    /// <summary>
    /// Gets the owner of the contributions made through the adviser: the aspect builder for an aspect, the amender for a
    /// type fabric. The predecessor of the contributions is <c>Owner.AspectPredecessor</c>.
    /// </summary>
    public IQueryOwner Owner { get; }

    /// <summary>
    /// Gets the target declaration of the aspect instance whose <c>BuildAspect</c> method is executing, in the compilation
    /// of the adviser. For a type fabric, this is the target type of the fabric.
    /// </summary>
    public IDeclaration AspectTarget { get; }

    /// <summary>Gets the template provider against which template names are resolved, or <c>null</c>. It honors <c>WithTemplateProvider</c>.</summary>
    public TemplateProvider? TemplateProvider { get; }

    /// <summary>Throws an <see cref="ObjectDisposedException"/> when the aspect or fabric has finished executing.</summary>
    public void ThrowIfDisposed();

    /// <summary>Creates a query that selects a single declaration and whose owner is <see cref="Owner"/>.</summary>
    public IQuery<T> CreateQuery<T>( T declaration )
        where T : class, IDeclaration;

    /// <summary>Captures the attribution information of a contribution made now through this adviser.</summary>
    public ExtensionContributionOrigin CaptureOrigin();
}
```

The context has no `Target`, `AspectPredecessor`, `IsDisposed` or `AspectInstance` property. The target is `IAdviser.Target`, the predecessor is `Owner.AspectPredecessor`, the extension needs only `ThrowIfDisposed`, and the aspect instance is carried by the origin that `CaptureOrigin` returns.

`CreateQuery` uses the same arguments as `AspectBuilder.Outbound` (ENG27 `Aspects\AspectBuilder.cs:86-91`): `new RootQuery<T>( declaration.ToRef(), this.Owner, CompilationModelVersion.Current )` (ENG27 `Queries\RootQuery.cs:12-22`).

#### 10.2.3 Contribution origin

```csharp
// NEW FILE: ENG27 Extensibility/ExtensionContributionOrigin.cs
namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents the aspect or fabric that made a contribution to an extension, together with the default template provider
/// and the information that the engine needs to attribute and order the code that the contribution produces.
/// </summary>
/// <remarks>
/// An origin captured from an aspect references the aspect instance of the current pipeline run. A contributor that holds
/// it must not outlive the run, and the design-time form of a contributor must not hold it. An origin captured from a
/// project or namespace fabric is stored in the pipeline configuration, which is long-lived at design time. Such an origin
/// therefore holds only the predecessor, the description, the default template provider and the identifier of the aspect
/// layer. It holds no aspect instance and no template class instance. The transformation factory creates the synthetic
/// aspect instance of the top-level fabric aggregate aspect class, and the template class instance of the fabric, once per
/// stage from the compilation of the stage.
/// </remarks>
[PublicAPI]
public sealed class ExtensionContributionOrigin
{
    /// <remarks>
    /// Both <paramref name="templateClassInstance"/> and <paramref name="aspectInstance"/> are <c>null</c> for an origin
    /// captured from a static fabric.
    /// </remarks>
    internal ExtensionContributionOrigin(
        AspectPredecessor predecessor,
        string diagnosticSourceDescription,
        TemplateProvider? defaultTemplateProvider,
        TemplateClassInstance? templateClassInstance,
        AspectLayerId aspectLayerId,
        IAspectInstanceInternal? aspectInstance,
        int pipelineStepIndex );

    public AspectPredecessor Predecessor { get; }

    public string DiagnosticSourceDescription { get; }

    /// <summary>Gets the default template provider of the contribution, or <c>null</c>.</summary>
    public TemplateProvider? DefaultTemplateProvider { get; }

    /// <summary>Captures the origin of a contribution made through a query, for example a fabric amender or <c>IAspectBuilder.Outbound</c>.</summary>
    /// <exception cref="ArgumentException">The owner was not created by the engine.</exception>
    /// <exception cref="ObjectDisposedException">The owner is an aspect builder whose aspect has finished executing.</exception>
    public static ExtensionContributionOrigin Capture( IQueryOwner owner )
        => owner is IExtensionContributionOriginSource source
            ? source.CaptureContributionOrigin()
            : throw new ArgumentException( "The owner was not created by the Metalama engine.", nameof(owner) );

    // Consumed by ExtensionTransformationFactory.
    internal TemplateClassInstance? TemplateClassInstance { get; }

    internal AspectLayerId AspectLayerId { get; }        // Always one of the ordered layers of the pipeline.

    internal IAspectInstanceInternal? AspectInstance { get; }     // Null for a static fabric.

    internal int PipelineStepIndex { get; }              // OrderWithinPipeline of the registering layer.
}

// NEW FILE: ENG27 Extensibility/IExtensionContributionOriginSource.cs
internal interface IExtensionContributionOriginSource
{
    ExtensionContributionOrigin CaptureContributionOrigin();
}
```

#### 10.2.4 PROPOSED internal changes

| File | Change |
|---|---|
| ENG27 `Advising\AdviceFactoryState.cs` | Add `public IQueryOwner? Owner { get; set; }`, the default owner of every factory that shares the state. Add `internal static void ThrowIfDisposed( AdviceFactoryState state )`, called by `AdviceFactory.ThrowIfDisposed` (ENG27 `Advising\AdviceFactory.cs:394-402`). |
| ENG27 `Aspects\AspectDriver.cs:225-227` | After `new AspectBuilder<T>(...)`, add `adviceFactoryState.Owner = aspectBuilder;`. |
| ENG27 `Advising\AdviceFactory.cs:56-80` | Add a required constructor parameter `IQueryOwner? ownerOverride` stored in `_ownerOverride`, and `internal IQueryOwner? Owner => this._ownerOverride ?? this._state.Owner;`. The parameter is required, not optional, so that a construction site that forgets it fails to compile. The four construction sites are ENG27 `Aspects\AspectDriver.cs:206` and `Advising\AdviceFactory.cs:186, 203, 382`. |
| ENG27 `Advising\AdviceFactory.cs:185-208, 375-389` | Pass `this._ownerOverride` in `WithTemplateClassInstance`, `WithExplicitInterfaceImplementation` and `WithDeclaration`. |
| ENG27 `Advising\IAdviceFactoryImpl.cs:13-27` | Add `IAdviceFactoryImpl WithOwner( IQueryOwner owner );` and `AdviserExtensionContext CreateExtensionContext( IDeclaration target, IQueryOwner? adviserOwner );`. |
| ENG27 `Fabrics\TypeFabricDriver.cs:143` | `this.Advice = ((IAdviceFactoryImpl) aspectBuilder.AdviceFactory).WithTemplateClassInstance( templateClassInstance ).WithOwner( this );` |
| ENG27 `Aspects\AspectBuilder.cs:25` | Implement `IExtensionContributionOriginSource`. The template provider falls back to `TemplateProvider.FromInstance( this.AspectInstance.Aspect )`, the provider that `AspectDriver` already uses for declarative advice. |
| ENG27 `Fabrics\FabricDriver.BaseAmender.cs:25` | Implement `IExtensionContributionOriginSource` for project and namespace fabrics, with the fabric predecessor, the description of line 130, `TemplateProvider.FromInstance( this._fabricInstance.Fabric )`, and the identifier of the layer of the top-level fabric aggregate aspect class (ENG27 `Pipeline\AspectPipeline.cs:335`, G2). The origin holds no aspect instance, because the `AspectInstance` constructors that take an `IDeclaration` store a reference bound to the compilation (ENG27 `Aspects\AspectInstance.cs:70, 100`), and a static-fabric origin lives in the configuration (the defect class of issue #1799). The transformation factory creates the synthetic aspect instance per stage (section [10.4.7](#1047-attribution-and-ordering)). |
| ENG27 `Fabrics\TypeFabricDriver.cs:119` | Override `CaptureContributionOrigin` in `Amender`: `((IAdviceFactoryImpl) this.Advice).CreateExtensionContext( this.Type, this ).CaptureOrigin()`. This origin carries the aggregate aspect layer (for linker ordering) and the fabric predecessor (for attribution). |

| Adviser | `Target` | `Owner` | `AspectPredecessor.Kind` | `TemplateProvider` |
|---|---|---|---|---|
| `builder` in `BuildAspect` | aspect target | the builder | `ChildAspect` | the aspect |
| `builder.With(x)` | `x` | the new `AspectBuilder<TNew>` (ENG27 `Aspects\AspectBuilder.cs:166-170`) | `ChildAspect` | same |
| `builder.IntroduceMethod(...)` result | introduced method | root builder, through `AdviceFactoryState.Owner` | `ChildAspect` | same |
| `builder.WithTemplateProvider(p)` | aspect target | root builder | `ChildAspect` | `p` |
| `amender` in a type fabric | fabric target type | the amender | `Fabric` | the fabric |
| `amender.With(x)` | `x` | the amender, through `_ownerOverride` | `Fabric` | the fabric |

The last row fixes the attribution of type-fabric contributions for extensions. Changing `AdviceFactory.AddAspect` and `RequireAspect` (ENG27 `Advising\AdviceFactory.cs:2325, 2374`) to use the owner's predecessor is a separate behavior change for existing child aspects, proposed as the optional fix F13.

### 10.3 Transforming pipeline hook (B2b)

#### 10.3.1 The hook

```csharp
// ENG27 Extensibility/PipelineExtension.cs (MODIFIED, new member after line 69)

/// <summary>
/// Executes the contributors that produce code transformations. The method is invoked once at the end of every high-level
/// pipeline stage that runs the linker, after <see cref="ExecutePipelineContributorsAsync"/> has been invoked for all
/// extensions and before the linker runs.
/// </summary>
/// <remarks>
/// <para>
/// The method is invoked at compile time and in the preview, live-template and introspection scenarios. It is not invoked at
/// design time or in the WPF precompilation scenario, because these scenarios run no linker.
/// </para>
/// <para>
/// Transformations are requested through <see cref="ExtensionTransformationContext.TransformationFactory"/>. They are not
/// observable: aspects do not see them, and the design-time generated code does not contain them.
/// </para>
/// <para>
/// Use <see cref="ExtensionTransformationContext.IsSourceStage"/> and
/// <see cref="ExtensionTransformationContext.HighLevelStageIndex"/> to decide what to do in a given stage.
/// </para>
/// </remarks>
public virtual Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    => Task.CompletedTask;
```

The hook returns `Task`, not a result object (RC2). The factory accumulates the requests of all extensions, and the stage completes it after the last extension.

#### 10.3.2 The context

```csharp
// NEW FILE: ENG27 Extensibility/ExtensionTransformationContext.cs
namespace Metalama.Framework.Engine.Extensibility;

/// <summary>Exposes the inputs of <see cref="PipelineExtension.ExecuteTransformingContributorsAsync"/>.</summary>
/// <remarks>One instance is shared by all extensions of a stage. It references compilations and must not outlive the call.</remarks>
[PublicAPI]
public sealed class ExtensionTransformationContext
{
    internal ExtensionTransformationContext(
        AspectPipelineConfiguration pipelineConfiguration,
        IReadOnlyCollection<IExtensionPipelineContributor> contributors,
        IReadOnlyCollection<IExtensionPipelineContributor> contributorsAddedInStage,
        CompilationModel sourceCompilation,
        CompilationModel stageInitialCompilation,
        CompilationModel stageFinalCompilation,
        int highLevelStageIndex,
        UserDiagnosticSink diagnostics,
        ExtensionTransformationFactory transformationFactory,
        SourceReferenceIndexStage sourceReferenceIndex );

    public AspectPipelineConfiguration PipelineConfiguration { get; }

    public ProjectServiceProvider ServiceProvider => this.PipelineConfiguration.ServiceProvider;

    /// <summary>Gets the execution scenario: compile time, preview, live template or introspection.</summary>
    [Memo]
    public ExecutionScenario ExecutionScenario => this.ServiceProvider.GetRequiredService<ExecutionScenario>();

    /// <summary>
    /// Gets all extension contributors of the stage: those replayed from the contributor sources of the pipeline
    /// (fabrics and referenced assemblies) and those added by the aspects of the stage. The order is not deterministic.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>Gets the extension contributors added by the aspects that executed in this stage. The order is not deterministic.</summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> ContributorsAddedInStage { get; }

    /// <summary>Gets the model of the source compilation of the pipeline, which is the input of the first stage.</summary>
    public CompilationModel SourceCompilation { get; }

    /// <summary>Gets the model that the aspects of this stage started from.</summary>
    public CompilationModel StageInitialCompilation { get; }

    /// <summary>Gets the model after all aspects of this stage.</summary>
    public CompilationModel StageFinalCompilation { get; }

    /// <summary>Gets <see cref="SourceCompilation"/> bound to the aspect repository of <see cref="StageFinalCompilation"/>.</summary>
    [Memo]
    public CompilationModel SourceCompilationWithFinalAspects
        => this.SourceCompilation.WithAspectRepository( this.StageFinalCompilation.AspectRepository, "Source with final aspects" );

    /// <summary>Gets the zero-based index of the stage among the high-level stages that this pipeline executes.</summary>
    public int HighLevelStageIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the aspects of this stage started from the source compilation. The linker of such a
    /// stage receives the syntax trees of the source compilation. Exactly one stage has this property: the first high-level
    /// stage.
    /// </summary>
    public bool IsSourceStage
        => ReferenceEquals( this.StageInitialCompilation.PartialCompilation, this.SourceCompilation.PartialCompilation );

    /// <summary>Gets the sink for the diagnostics and suppressions of the extensions.</summary>
    public UserDiagnosticSink Diagnostics { get; }

    /// <summary>Gets the factory of transformations for this stage.</summary>
    public ExtensionTransformationFactory TransformationFactory { get; }

    /// <summary>
    /// Gets the index of the references of the source compilation for this stage, shared by all extensions
    /// (section 10.7.5).
    /// </summary>
    public SourceReferenceIndexStage SourceReferenceIndex { get; }
}
```

`IsSourceStage` is exactly the condition under which the linker of the stage receives the source trees. It compares the partial compilations, not the models, so that it stays true if a later change derives a new `CompilationModel` before the pipeline steps. All code-model versions of a stage share one `PartialCompilation` (ENG26 `CodeModel\CompilationModel.cs:333`), and the linker stage drops the model (ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:90`). `AspectPipeline` inserts the high-level system aspect layers before all user layers, so the first stage is always high-level and always receives the pipeline input (ENG27 `Pipeline\AspectPipeline.cs:333-358`; section [9.5.1](09-premium-engine.md#951-entry-point-and-guards)). `CompilationModel.WithAspectRepository` is public (ENG27 `CodeModel\CompilationModel.cs:393`).

`ContributorsAddedInStage` needs a small internal change. `PipelineStepsState` adds both replayed contributors (ENG27 `Pipeline\PipelineStepsState.cs:99`) and contributors added by aspects (`Pipeline\ExecuteAspectLayerPipelineStep.cs:147`) through the same `AddExtendedContributors` method (`Pipeline\PipelineStepsState.cs:492-498`). It keeps a second list for the contributors that `ExecuteAspectLayerPipelineStep` adds. The replays that the constructor adds are not in that list. `PipelineStepsResult` exposes the list as `ExtensionContributorsAddedInStage`, and `LinkerPipelineStage` passes it to the context.

#### 10.3.3 Stage index

```csharp
// ENG27 Pipeline/HighLevelPipelineStage.cs (MODIFIED)
/// <summary>Gets or sets the zero-based index of this stage among the high-level stages of the current pipeline execution.</summary>
internal int HighLevelStageIndex { get; set; }

// ENG27 Pipeline/AspectPipeline.cs:757-768 (MODIFIED): in the stage loop, before ExecuteAsync.
if ( stage is HighLevelPipelineStage highLevelStage )
{
    highLevelStage.HighLevelStageIndex = highLevelStageIndex;
    highLevelStageIndex++;
}
```

The stage object is created per execution (ENG27 `Pipeline\AspectPipeline.cs:759`), so a mutable property is safe.

#### 10.3.4 Call site in LinkerPipelineStage

```csharp
// ENG27 Pipeline/CompileTime/LinkerPipelineStage.cs, inserted before line 42 (before the validator loop).

// Collect the requirements of the shared index of source references (section 10.7.5).
var sourceIndexService = pipelineConfiguration.ServiceProvider.GetRequiredService<SourceReferenceIndexService>();
var sourceIndexRequirementsContext = new SourceIndexRequirementsContext( pipelineStepsResult.ExtensionContributors, this.HighLevelStageIndex );
var sourceIndexRequirements = extensions.Select( e => (e, e.GetSourceIndexRequirements( sourceIndexRequirementsContext )) );
using var sourceIndexStage = sourceIndexService.BeginStage( input.FirstCompilationModel.AssertNotNull(), sourceIndexRequirements );

// Lines 42-56 run the validators unchanged. ExecutePipelineContributorsAsync reads the index through
// sourceIndexService.Current, because its signature does not change.

// ENG27 Pipeline/CompileTime/LinkerPipelineStage.cs, inserted after line 56 (end of the validator loop)
// and before line 58 ("// Run the linker.").

// Run the transforming extensions.
var namingServices = new LinkerNamingServices( finalCompilation );
var extensionDiagnostics = new UserDiagnosticSink( pipelineConfiguration.ServiceProvider );

var transformationFactory = new ExtensionTransformationFactory(
    new ExtensionTransformationFactoryContext( pipelineConfiguration.ServiceProvider, finalCompilation, input.AspectLayers, namingServices ) );

var extensionTransformationContext = new ExtensionTransformationContext(
    pipelineConfiguration,
    pipelineStepsResult.ExtensionContributors,
    pipelineStepsResult.ExtensionContributorsAddedInStage,
    input.FirstCompilationModel.AssertNotNull(),
    initialCompilation,
    finalCompilation,
    this.HighLevelStageIndex,
    extensionDiagnostics,
    transformationFactory,
    sourceIndexStage );

foreach ( var extension in extensions )
{
    await extension.ExecuteTransformingContributorsAsync( extensionTransformationContext, cancellationToken );
}

var extensionLinkerInput = transformationFactory.Complete();

// Run the linker (lines 59-66, MODIFIED).
var linker = new AspectLinker(
    pipelineConfiguration.ServiceProvider,
    new AspectLinkerInput(
        input.FirstCompilationModel.AssertNotNull(),
        pipelineStepsResult.LastCompilation,
        pipelineStepsResult.Transformations.Concat( extensionLinkerInput.Transformations ).ToReadOnlyList(),
        input.AspectLayers,
        new CallSiteAdviceInfo( input.ContributorSources.ReferencesContainInitializableTypes ),   // Unchanged.
        extensionLinkerInput ) );

// Result (lines 92-94, MODIFIED): append extensionDiagnostics.ToImmutable() to the stage diagnostics.
```

The code above shows the data flow. The implementation creates `LinkerNamingServices`, the factory context and the factory lazily, on the first access to `ExtensionTransformationContext.TransformationFactory`, and passes `pipelineStepsResult.Transformations` unchanged to `AspectLinkerInput` when the extension input is empty. A project that uses no transforming extension then pays one context allocation per stage and no copy of the transformation list.

`GenerateAdditionalCompilationOutputFilesAsync` builds design-time trees from `pipelineStepsResult.Transformations` only (ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:117-124`), so extension transformations never reach design-time generated code.

#### 10.3.5 Behavior per pipeline and scenario

| Pipeline | High-level stage | Hook invoked | `IsSourceStage` in stage 0 |
|---|---|---|---|
| `CompileTimeAspectPipeline` | `LinkerPipelineStage` (ENG27 `Pipeline\CompileTime\CompileTimeAspectPipeline.cs:408-411`) | yes, in every high-level stage | yes (section [9.5.1](09-premium-engine.md#951-entry-point-and-guards)) |
| `WpfPrecompileAspectPipeline` | `WpfPrecompilePipelineStage` | no | not applicable |
| `DesignTimeAspectPipeline`, `TestDesignTimeAspectPipeline` | `DesignTimePipelineStage` (ENG27 `Pipeline\DesignTime\BaseDesignTimeAspectPipeline.cs:21-26`) | no | not applicable |
| `PreviewAspectPipeline` | `LinkerPipelineStage` | yes | same rule as compile time |
| `LiveTemplateAspectPipeline` | `LinkerPipelineStage` | yes | yes; the contributor sources contain only the live-template aspect |
| `IntrospectionAspectPipeline` | `LinkerPipelineStage` (ENG27 `Introspection\IntrospectionAspectPipeline.cs:32-33`) | yes | yes |

The new member of `PipelineExtension` is virtual with a default implementation, so extensions compiled without it load and behave as before. `ExtensionPipelineContributorsResult` is unchanged, and the Validation engine needs no change. No `IsTransforming` flag is added to `ContributorKind`: it would save only one context allocation per stage.

### 10.4 Extension transformation factory (B2c)

All public types live in the new namespace `Metalama.Framework.Engine.Extensibility.Transformations` of `Metalama.Framework.Engine`.

#### 10.4.1 Factory

```csharp
namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>Creates linker transformations on behalf of a <see cref="PipelineExtension"/>.</summary>
/// <remarks>
/// <para>
/// The engine creates one instance per high-level stage and passes it to the transforming hook of every extension. The
/// instance is not thread-safe. Names are allocated in call order, so the caller must call the factory in a deterministic
/// order.
/// </para>
/// <para>
/// The factory does not change the code model. The declarations that it creates are not visible to aspects and do not
/// appear in design-time generated code.
/// </para>
/// </remarks>
public sealed class ExtensionTransformationFactory
{
    internal ExtensionTransformationFactory( ExtensionTransformationFactoryContext context );

    /// <summary>Gets the compilation that results from all aspects of the stage. In the source stage, its syntax trees are the source syntax trees.</summary>
    public ICompilation Compilation { get; }

    /// <summary>Declares a top-level, non-generic static class in a new syntax tree.</summary>
    public SynthesizedTypeHandle DeclareStaticClass( ExtensionContributionOrigin origin, SynthesizedStaticClassRequest request );

    /// <summary>
    /// Declares a method whose body is expanded from a template. The method is either a member of a type or a local
    /// function appended to the root block of the body of a source member.
    /// </summary>
    /// <exception cref="InvalidTemplateSignatureException">The template cannot implement the declared signature.</exception>
    /// <exception cref="DiagnosticException">The template cannot be resolved.</exception>
    public SynthesizedMethodHandle DeclareMethod( ExtensionContributionOrigin origin, SynthesizedMethodRequest request );

    /// <summary>
    /// Reserves an identifier that does not conflict with any identifier visible in the body of a source member, including
    /// identifiers reserved by previous calls and by templates expanded for the same member.
    /// </summary>
    public string ReserveLocalIdentifier( IMethodBase hostMember, string hint );

    /// <summary>Requests that a source invocation be replaced by an invocation of another method.</summary>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same call site.</exception>
    public void RedirectInvocation( ExtensionContributionOrigin origin, InvocationRedirectionRequest request );

    /// <summary>Requests that the operand of a source <c>await</c> expression be passed to another method whose result is awaited.</summary>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same call site.</exception>
    public void RedirectAwait( ExtensionContributionOrigin origin, AwaitRedirectionRequest request );

    /// <summary>
    /// Requests that a source method group converted to a delegate or to a function pointer be replaced by a method group
    /// of another method, or by an expression that creates a delegate of the same type that calls the other method.
    /// </summary>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same method group.</exception>
    public void RedirectMethodReference( ExtensionContributionOrigin origin, MethodReferenceRedirectionRequest request );

    /// <summary>
    /// Requests that one accessor use of a source property or event access be replaced by an invocation of another method.
    /// The factory merges the requests for the get use and the set use of one compound site into one rewrite.
    /// </summary>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same accessor use of the
    /// site, or the site is already redirected by another kind of request.</exception>
    public void RedirectAccessor( ExtensionContributionOrigin origin, AccessorRedirectionRequest request );

    /// <summary>Determines whether a redirection was already requested for a call site.</summary>
    public bool IsRedirected( ExpressionSyntax callSite );

    /// <summary>Freezes the factory and returns the linker input. Called by the pipeline stage after all extensions.</summary>
    internal ExtensionLinkerInput Complete();
}
```

#### 10.4.2 Why a factory

All linker and template types are internal (ENG26 `Transformations\ITransformation.cs`; `Linking\Substitution\SyntaxNodeSubstitution.cs:13`; `Templating\TemplateExpansionContext.cs:39`), and `InternalsVisibleTo` is reserved for tests. A factory keeps the public surface small, generic and versionable, and lets the engine validate every request. Making `ITransformation`, `InjectedMember` and `SyntaxNodeSubstitution` public was rejected, because it exposes a large and unstable surface.

#### 10.4.3 Synthesized static classes

```csharp
/// <summary>Describes a top-level, non-generic static class that the linker creates in a new syntax tree.</summary>
public sealed class SynthesizedStaticClassRequest
{
    public SynthesizedStaticClassRequest( string nameHint );

    public string NameHint { get; }

    /// <summary>Gets the namespace of the class. <c>null</c> means the global namespace.</summary>
    public INamespace? Namespace { get; init; }

    public Accessibility Accessibility { get; init; } = Accessibility.Internal;
}

public sealed class SynthesizedTypeHandle
{
    /// <summary>Gets the declared type, resolved in <see cref="ExtensionTransformationFactory.Compilation"/>.</summary>
    public INamedType Type { get; }

    public string Name { get; }
}
```

A static class in the global namespace makes its extension methods visible from every file. This is the placement that the premium engine uses for `GeneratedStaticClass()` and for conditional access. The class reuses `IntroduceNamedTypeTransformation` with a new constructor parameter `TransformationObservability observability = TransformationObservability.Always` (ENG26 `AdviceImpl\Introduction\IntroduceNamedTypeTransformation.cs:27`); the factory passes `None`.

#### 10.4.4 Synthesized methods and proceed bindings

```csharp
/// <summary>Describes where a synthesized method is declared.</summary>
public abstract class SynthesizedMethodPlacement
{
    private protected SynthesizedMethodPlacement() { }

    /// <summary>
    /// Declares the method as a member of a class, struct or record of the current compilation, which can be a source type or
    /// a type introduced by an aspect. The method is injected into the primary declaration part of the type.
    /// </summary>
    public static SynthesizedMethodPlacement InType( INamedType containingType );

    /// <summary>Declares the method as a member of a class declared by <see cref="ExtensionTransformationFactory.DeclareStaticClass"/>.</summary>
    public static SynthesizedMethodPlacement InType( SynthesizedTypeHandle containingType );

    /// <summary>
    /// Declares the method as a local function appended to the root block of the body of a source member. The host is a
    /// method, a constructor, an operator, a finalizer or an accessor that has a body or an expression body.
    /// </summary>
    public static SynthesizedMethodPlacement AsLocalFunction( IMethodBase hostMember );
}

/// <summary>Describes a method that the linker synthesizes and implements with a template.</summary>
public sealed class SynthesizedMethodRequest
{
    public SynthesizedMethodRequest(
        SynthesizedMethodPlacement placement,
        string nameHint,
        Action<IMethodBuilder> buildSignature,
        SynthesizedMethodTemplate template,
        Func<IMethod, ProceedBinding> createProceedBinding );

    public SynthesizedMethodPlacement Placement { get; }

    /// <summary>Gets the preferred name. The linker allocates the final unique name.</summary>
    public string NameHint { get; }

    /// <summary>
    /// Gets the action that defines the signature: accessibility, static-ness, return type, parameters, type parameters and
    /// attributes. The action must not set the name or the async flag. It can set the accessibility, <c>IsStatic</c>,
    /// <c>IsReadOnly</c> for a method in a struct, and the members of parameters and type parameters.
    /// </summary>
    public Action<IMethodBuilder> BuildSignature { get; }

    public SynthesizedMethodTemplate Template { get; }

    /// <summary>
    /// Gets the function that creates the binding of <c>meta.Proceed()</c>. It receives the frozen signature, so the binding
    /// can refer to the type parameters and parameters of the synthesized method.
    /// </summary>
    public Func<IMethod, ProceedBinding> CreateProceedBinding { get; }

    /// <summary>
    /// Gets the nullable annotation context in which the method is emitted. <c>null</c> means the context of the insertion
    /// point. The signature and the body are generated in this context, and the linker wraps the member in
    /// <c>#nullable enable</c> or <c>#nullable disable</c> when it differs from the insertion point (section 10.6.4).
    /// </summary>
    public bool? NullableAnnotationsEnabled { get; init; }

    /// <summary>
    /// Gets a function that the name allocator calls for each candidate name, after the checks of the placement, or
    /// <c>null</c>. A candidate is used only when the function returns <c>true</c>. An extension uses it to verify that a
    /// name is free at the places from which the method is called.
    /// </summary>
    public Func<string, bool>? IsNameAvailable { get; init; }
}

/// <summary>Determines how many times the proceed expression can be emitted in the expanded body.</summary>
public enum ProceedMultiplicity
{
    /// <summary>No restriction.</summary>
    Any,

    /// <summary>
    /// The proceed expression can be emitted at most once, and not inside a loop, a lambda or a local function of the expanded
    /// body. Violations are reported as errors.
    /// </summary>
    AtMostOnce
}

/// <summary>Describes the template that implements a synthesized method.</summary>
public sealed class SynthesizedMethodTemplate
{
    public SynthesizedMethodTemplate( MethodTemplateSelector selector );

    /// <summary>
    /// Gets the template selector. Set <see cref="MethodTemplateSelector.UseAsyncTemplateForAnyAwaitable"/> to make the method
    /// <c>async</c> when its return type has a method builder.
    /// </summary>
    public MethodTemplateSelector Selector { get; }

    /// <summary>Gets the template provider. When it is null, the default provider of the origin is used.</summary>
    public TemplateProvider TemplateProvider { get; init; }

    /// <summary>Gets the compile-time template arguments, with the same meaning as <c>args</c> in advice methods.</summary>
    public object? Arguments { get; init; }

    /// <summary>Gets the tags exposed as <c>meta.Tags</c>.</summary>
    public object? Tags { get; init; }

    /// <summary>
    /// Gets the number of leading parameters of the synthesized method that are excluded from the ordinal binding of
    /// run-time template parameters. A receiver parameter is typically excluded.
    /// </summary>
    public int HiddenLeadingParameterCount { get; init; }

    public ProceedMultiplicity ProceedMultiplicity { get; init; } = ProceedMultiplicity.Any;

    /// <summary>
    /// Gets the compile-time objects that templates can read with <c>meta.GetExtension&lt;T&gt;()</c> while this template is
    /// expanded, including the templates that it calls. Two elements must not have the same type.
    /// </summary>
    public ImmutableArray<IMetaExtension> MetaExtensions { get; init; } = ImmutableArray<IMetaExtension>.Empty;
}

/// <summary>Represents a synthesized method.</summary>
public sealed class SynthesizedMethodHandle
{
    /// <summary>Gets the frozen signature, resolved in <see cref="ExtensionTransformationFactory.Compilation"/>.</summary>
    public IMethod Method { get; }

    /// <summary>Gets the final unique name.</summary>
    public string Name { get; }

    public bool IsLocalFunction { get; }

    /// <summary>Gets the host member of a local function, or <c>null</c>.</summary>
    public IMethodBase? HostMember { get; }

    /// <summary>
    /// Gets the parameters whose name was changed to avoid a conflict in the host scope, from the requested name to the final
    /// name. The dictionary is empty for type members.
    /// </summary>
    public IReadOnlyDictionary<string, string> RenamedParameters { get; }
}

public enum ProceedBindingKind
{
    /// <summary><c>meta.Proceed()</c> is not available.</summary>
    None,

    /// <summary><c>meta.Proceed()</c> invokes a method.</summary>
    Invoke,

    /// <summary><c>meta.Proceed()</c> awaits a parameter of the synthesized method.</summary>
    Await,

    /// <summary><c>meta.Proceed()</c> returns a parameter of the synthesized method without awaiting it.</summary>
    Parameter
}

public enum ProceedReceiverKind
{
    /// <summary><c>ReceiverType.M(args)</c>. Also used for the static form of classic extension methods.</summary>
    Static,

    /// <summary><c>parameter.M(args)</c>.</summary>
    Parameter,

    /// <summary>
    /// <c>this.M(args)</c>. The synthesized method must be an instance member, or a local function of an instance member of a
    /// class. The caller must ensure that <c>this.M(args)</c> binds to <c>M</c> in the synthesized method.
    /// </summary>
    This,

    /// <summary><c>base.M(args)</c>. The synthesized method must be an instance member of the calling type.</summary>
    Base
}

/// <summary>Describes the expression that <c>meta.Proceed()</c> produces in a synthesized method.</summary>
public sealed class ProceedBinding
{
    public static ProceedBinding None { get; }

    public static ProceedBinding InvokeStatic(
        IMethod method,
        IType? receiverType = null,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default );

    public static ProceedBinding InvokeOnParameter(
        IMethod method,
        int receiverParameterIndex,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default );

    /// <summary>
    /// Creates a binding where <c>meta.Proceed()</c> invokes <paramref name="method"/> on <c>this</c> with a plain call, which
    /// keeps virtual and interface dispatch.
    /// </summary>
    public static ProceedBinding InvokeOnThis(
        IMethod method,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default );

    public static ProceedBinding InvokeOnBase(
        IMethod method,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default );

    /// <summary>
    /// Creates a binding where <c>meta.Proceed()</c> awaits a parameter. The result type is given explicitly because it comes
    /// from the await-expression information of the source call site, which also covers extension <c>GetAwaiter</c> methods.
    /// </summary>
    public static ProceedBinding AwaitParameter( int parameterIndex, IType resultType );

    /// <summary>Creates a binding where <c>meta.Proceed()</c> returns a parameter without awaiting it.</summary>
    public static ProceedBinding ReturnParameter( int parameterIndex );

    /// <summary>
    /// Returns a copy in which the arguments at the given parameter indices are cast to the given types. A cast to
    /// <c>object</c> keeps a <c>dynamic</c> parameter statically bound. A cast to the parameter type of the invoked method
    /// passes a parameter whose type is wider than the parameter of the invoked method.
    /// </summary>
    public ProceedBinding WithArgumentCasts( ImmutableArray<(int ParameterIndex, IType Type)> casts );

    public ProceedBindingKind Kind { get; }

    public IMethod? Method { get; }

    public ProceedReceiverKind ReceiverKind { get; }

    public IType? ReceiverType { get; }

    public int ParameterIndex { get; }

    public ImmutableArray<IType> TypeArguments { get; }

    /// <summary>Gets the indices of the synthesized-method parameters passed as arguments, in order. The default is every parameter except the receiver.</summary>
    public ImmutableArray<int> ArgumentParameterIndices { get; }

    public ImmutableArray<(int ParameterIndex, IType Type)> ArgumentCasts { get; }

    public IType? AwaitResultType { get; }
}
```

The premium package uses the same word: a premium `InterceptorPlacement` resolves into one `SynthesizedMethodPlacement` (section [5.6.3](05b-api-providers-contexts-results.md#563-interceptorplacement)). The factory has no concept of interceptors or of the origin of a site. For `AsLocalFunction`, it receives only a host member.

Accessors. The factories `InvokeStatic`, `InvokeOnParameter`, `InvokeOnThis` and `InvokeOnBase` accept an accessor as `method` (`MethodKind.PropertyGet`, `PropertySet`, `EventAdd` or `EventRemove`). The proceed expression is then the access, not a call: `R.P`, `p.P`, `this.P` or `base.P` for a getter; the same access assigned with the last argument parameter for a setter, `p.P = value`, whose value is the assigned value; and `p.E += handler` or `p.E -= handler` for an add or remove accessor. For the accessor of a C# 14 extension property, the proceed expression is the static implementation form, `global::E.get_P( receiver )` or `global::E.set_P( receiver, value )`, as the existing invoker writes it (ENG27 `CodeModel\Invokers\FieldOrPropertyInvoker.cs:35-38, 70-101`). The implementation method of a setter returns `void`, so the expansion of `return meta.Proceed();` in a setter template emits the call as a statement followed by `return value;`. No new factory is needed: the kind of the method selects the form.

The signature is described with the public `IMethodBuilder` through a callback. No parallel descriptor type is introduced. The premium `IInterceptorBuilder` (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)) is a restricted and validated view that the premium engine translates into this callback, so it needs no change of the factory. Its open-source consequences are the generalization of the argument casts of `ProceedBinding`, from casts to `object` to casts to any type, and the argument lists of redirections (`RedirectedArgument`, section [10.4.5](#1045-call-site-redirections)), which carry its bindings. `IMethodBuilder` already expresses accessibility, static-ness, type parameters with constraints, `AllowsRefStruct` and variance, parameters with `RefKind`, `DefaultValue`, `IsParams` and `IsThis`, the return type, and attributes (FW26 `Code\DeclarationBuilders\IParameterBuilder.cs:24-52`; `IMethodBuilder.cs:36`; `IDeclarationBuilder.cs:33`). The resulting `MethodBuilderData` resolves to an `IMethod` in any compilation, even when no transformation added it (ENG26 `CodeModel\Factories\DeclarationFactory.Builders.cs:99-105, 206-230`), so `meta.Target.Method` and syntax generation work unchanged.

`DeclareMethod` runs these steps inside `UserCodeExecutionContext.WithContext( serviceProvider, finalCompilation, ... )` (ENG26 `Utilities\UserCode\UserCodeExecutionContext.cs:130`):

1. Allocate the name (section [10.4.6](#1046-names-and-lexical-scopes)) and create `new MethodBuilder( layerInstance, declaringType, name, methodKind )` (ENG27 `CodeModel\Introductions\Builders\MethodBuilder.cs:43-75`), with `MethodKind.LocalFunction` for a local function. For a local function, the declaring type is the declaring type of the host, and `IsStatic` is set to the host's `IsStatic` after the callback; the local-function syntax never emits `static`.
2. Invoke `BuildSignature( builder )`.
3. Validate (section [10.4.8](#1048-validation-performed-by-the-factory)). Restore the name if the callback changed it.
4. For a local function, rename every parameter and type parameter whose name is not unique in the host scope, with `TemplateLexicalScope.GetUniqueIdentifier`, and record the renames.
5. Resolve and select the template (section [10.6.1](10b-oss-linker-and-templates.md#1061-selection-at-declaration-time)). Set `builder.IsAsync = template.MustInterpretAsAsyncTemplate()` (ENG26 `Advising\TemplateExtensions.cs:113-115`) and `builder.SetIsIteratorMethod( template.IsIteratorMethod )`. This differs from `IntroduceMethodAdvice`, which copies the `async` modifier of the template method (ENG26 `AdviceImpl\Introduction\IntroduceMethodAdvice.cs:56`). A Default template interpreted as async needs an `async` method, as for overrides, so that the proceed expression can contain `await`.
6. Freeze the builder and resolve `method = builder.BuilderData.ToRef().GetTarget( finalCompilation )`.
7. Invoke `CreateProceedBinding( method )`, translate the referenced declarations into the final compilation, and validate the binding.
8. Bind the template with `ForSynthesizedMethod` (section [10.6.2](10b-oss-linker-and-templates.md#1062-binder-with-hidden-leading-parameters)).
9. Create the transformation and return the handle.

| Element | Mapping | Version 1 behavior |
|---|---|---|
| Return by `ref` | `ParameterBuilder.RefKind` throws on the return parameter (ENG26 `CodeModel\Introductions\Builders\ParameterBuilder.cs:44-61`), and syntax generation does not emit `ref` on return types (ENG26 `SyntaxGeneration\ContextualSyntaxGenerator.cs:260`). | Not supported. The premium engine presents such call sites with `RefReturn`. |
| `scoped`, `[UnscopedRef]` | Not representable in the code model. | Not supported (`ScopedParameter`). |
| Pointer types | No `unsafe` flag on builders. | Rejected by the factory. |
| `async` | Not settable through `IMethodBuilder`. | Decided by template selection (step 5). |
| Name | Allocated by the linker. | The callback must not set it. |

#### 10.4.5 Call-site redirections

```csharp
/// <summary>Describes how the receiver of the source invocation is passed to the new target.</summary>
public enum CallSiteReceiverMode
{
    /// <summary>
    /// The receiver is not passed. Valid when the source receiver is a type name, absent, or <c>base</c> with a virtual
    /// target, which the new target, an instance method of the calling type, calls through <c>base</c>.
    /// </summary>
    Drop,

    /// <summary>
    /// The receiver is passed by value as the first argument. An implicit receiver and a <c>base</c> receiver with a
    /// non-virtual target are written <c>this</c>, and <c>p-&gt;M()</c> gives <c>*p</c>.
    /// </summary>
    FirstArgument,

    /// <summary>The receiver is passed with <c>ref</c> as the first argument.</summary>
    FirstArgumentByRef,

    /// <summary>The receiver is passed with <c>in</c> as the first argument, for a <c>ref readonly</c> receiver parameter.</summary>
    FirstArgumentByIn,

    /// <summary>
    /// The receiver syntax is kept and the target is invoked as an extension method. With <see cref="MemberOfReceiver"/>, this
    /// is one of the two modes that preserve the short-circuit of a conditional access such as <c>a?.M(x)</c>.
    /// </summary>
    ExtensionReceiver,

    /// <summary>
    /// The receiver syntax is kept and only the member name is replaced, so the target is invoked as an instance member of the
    /// receiver: <c>r.M(x)</c> becomes <c>r.I(x)</c>, <c>a?.B.M(x)</c> becomes <c>a?.B.I(x)</c>, and an implicit receiver
    /// <c>M(x)</c> becomes <c>this.I(x)</c>. The target must be an instance method declared in the static type of the receiver
    /// or in one of its base types.
    /// </summary>
    MemberOfReceiver
}

/// <summary>Represents an argument appended to the rewritten call as a named argument, for example a caller-information value computed from the source call site.</summary>
public readonly record struct CallSiteExtraArgument( string ParameterName, ExpressionSyntax Value )
{
    public static CallSiteExtraArgument FromConstant( string parameterName, TypedConstant value );
}

/// <summary>
/// Describes one argument of a rewritten call whose argument list differs from the source: a value of the source site,
/// or an expression evaluated at the site.
/// </summary>
/// <remarks>
/// The values of the source site are evaluated once, from left to right, before the expressions. When the argument list
/// changes their order, or omits one, the linker evaluates them into temporaries first, and evaluates an omitted value
/// that can have side effects into a discard.
/// </remarks>
public sealed class RedirectedArgument
{
    /// <summary>Gets an argument that passes the receiver of the source site.</summary>
    public static RedirectedArgument SourceReceiver { get; }

    /// <summary>Gets an argument that passes the argument written at the source site for a parameter of the source method.</summary>
    public static RedirectedArgument SourceArgument( int parameterOrdinal );

    /// <summary>
    /// Gets an argument that passes the elements of an expanded <c>params</c> argument of the source site as one
    /// collection: a collection expression, or an array creation before C# 12.
    /// </summary>
    public static RedirectedArgument PackedSourceArgument( int parameterOrdinal, IType collectionType );

    /// <summary>Gets an argument that passes a parameter of the delegate, in the lambda that replaces a method group.</summary>
    public static RedirectedArgument DelegateParameter( int index );

    /// <summary>Gets an argument that passes an expression, which is emitted in the context of the source site.</summary>
    public static RedirectedArgument Expression( IExpression expression );

    /// <summary>Gets a copy of this argument that is written as a named argument.</summary>
    public RedirectedArgument WithName( string parameterName );
}

/// <summary>Represents the method that replaces a call site.</summary>
public sealed class CallSiteRedirectionTarget
{
    /// <param name="containingTypeAtCallSite">The containing type as it must be written at the call site, for example a
    /// constructed generic type. The default is the containing type definition.</param>
    public static CallSiteRedirectionTarget Synthesized( SynthesizedMethodHandle method, INamedType? containingTypeAtCallSite = null );

    public static CallSiteRedirectionTarget Existing( IMethod method, INamedType? containingTypeAtCallSite = null );
}

public sealed class InvocationRedirectionRequest
{
    public InvocationRedirectionRequest( InvocationExpressionSyntax callSite, CallSiteRedirectionTarget target, CallSiteReceiverMode receiverMode );

    /// <summary>Gets the invocation, which must be a node of a syntax tree of the stage's partial compilation.</summary>
    public InvocationExpressionSyntax CallSite { get; }

    public CallSiteRedirectionTarget Target { get; }

    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>Gets explicit type arguments for the new target, written in the context of the call site.</summary>
    public ImmutableArray<IType> TypeArguments { get; init; }

    public ImmutableArray<CallSiteExtraArgument> ExtraArguments { get; init; }

    /// <summary>
    /// Gets the complete argument list of the new call, after the receiver when <see cref="ReceiverMode"/> passes it, or
    /// the default value when the new call passes the arguments of the source site in their order, followed by
    /// <see cref="ExtraArguments"/>.
    /// </summary>
    public ImmutableArray<RedirectedArgument> Arguments { get; init; }

    /// <summary>
    /// Gets a type to which the new call is cast, when its return type differs from the original one. The caller sets it
    /// only when the value of the call is used and the call site is not inside a conditional access.
    /// </summary>
    public IType? ResultCast { get; init; }

    /// <summary>Gets a description used in the completeness diagnostics of the linker.</summary>
    public string? Description { get; init; }
}

public sealed class AwaitRedirectionRequest
{
    public AwaitRedirectionRequest( AwaitExpressionSyntax callSite, CallSiteRedirectionTarget target );

    public AwaitExpressionSyntax CallSite { get; }

    public CallSiteRedirectionTarget Target { get; }

    public ImmutableArray<IType> TypeArguments { get; init; }

    /// <summary>Gets arguments appended after the awaited operand.</summary>
    public ImmutableArray<CallSiteExtraArgument> ExtraArguments { get; init; }

    /// <summary>Gets a value indicating whether <c>.ConfigureAwait(false)</c> is appended to the new call.</summary>
    public bool AppendConfigureAwaitFalse { get; init; }

    public IType? ResultCast { get; init; }

    public string? Description { get; init; }
}

public sealed class MethodReferenceRedirectionRequest
{
    /// <param name="methodReference">The method-group expression: a simple name, a generic name or a member access whose
    /// operation is an <c>IMethodReferenceOperation</c> converted to a delegate or to a function pointer.</param>
    /// <param name="receiverMode">
    /// <see cref="CallSiteReceiverMode.Drop"/>: the new target is static, or an instance method called on <c>this</c>, and
    /// the method group becomes <c>X.I</c>, <c>this.I</c> or <c>I</c>.
    /// <see cref="CallSiteReceiverMode.MemberOfReceiver"/>: the method group becomes <c>r.I</c>.
    /// <see cref="CallSiteReceiverMode.ExtensionReceiver"/>: the method group becomes <c>r.I</c>, with <c>I</c> an extension
    /// method whose <c>this</c> parameter has a reference type.
    /// <see cref="CallSiteReceiverMode.FirstArgument"/> and <see cref="CallSiteReceiverMode.FirstArgumentByRef"/>: the method
    /// group becomes a wrapper that evaluates the receiver once and creates a delegate of <see cref="WrapperDelegateType"/>
    /// that passes the receiver as the first argument of the new target.
    /// </param>
    public MethodReferenceRedirectionRequest( ExpressionSyntax methodReference, CallSiteRedirectionTarget target, CallSiteReceiverMode receiverMode );

    public ExpressionSyntax MethodReference { get; }

    public CallSiteRedirectionTarget Target { get; }

    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>Gets explicit type arguments for the new target, written in the context of the site.</summary>
    public ImmutableArray<IType> TypeArguments { get; init; }

    /// <summary>
    /// Gets the delegate type of the wrapper. It is required with the <c>FirstArgument</c> modes and with
    /// <see cref="WrapperArguments"/>, and ignored otherwise.
    /// </summary>
    public IType? WrapperDelegateType { get; init; }

    /// <summary>
    /// Gets the arguments that the lambda of the wrapper passes to the new target, or the default value for the parameters
    /// of the delegate in order. When it is set, the method group becomes a lambda even with the <c>Drop</c> mode, and the
    /// expressions are evaluated at each invocation of the delegate.
    /// </summary>
    public ImmutableArray<RedirectedArgument> WrapperArguments { get; init; }

    public string? Description { get; init; }
}
```

```csharp
/// <summary>The accessor use that an <see cref="AccessorRedirectionRequest"/> redirects.</summary>
public enum AccessorRole
{
    Get,
    Set,
    Add,
    Remove
}

/// <summary>The syntactic context of an accessor site, which decides the form of the rewrite.</summary>
public enum AccessorSiteContext
{
    /// <summary>The site is the expression of an expression statement. A site that needs a temporary becomes a block.</summary>
    Statement,

    /// <summary>The value of the site is not used, and the position accepts only a statement expression, for example a <c>for</c> incrementor.</summary>
    DiscardedExpression,

    /// <summary>The value of the site is used. A site that needs a temporary uses pattern variables.</summary>
    ValueExpression
}

public sealed class AccessorRedirectionRequest
{
    /// <param name="site">The node of the site: the member access of a read; the assignment expression of a write, of a
    /// compound assignment, of a <c>??=</c> or of an event subscription; or the increment or decrement expression.</param>
    /// <param name="role">The accessor use that is redirected.</param>
    public AccessorRedirectionRequest( ExpressionSyntax site, AccessorRole role, CallSiteRedirectionTarget target, CallSiteReceiverMode receiverMode );

    public ExpressionSyntax Site { get; }

    public AccessorRole Role { get; }

    public CallSiteRedirectionTarget Target { get; }

    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>Gets the context of the site. The requests of one site must give the same value.</summary>
    public AccessorSiteContext Context { get; init; }

    /// <summary>
    /// Gets a value indicating whether the receiver must be stored in a temporary because it is used twice. The requests
    /// of one site must give the same value.
    /// </summary>
    public bool ReceiverRequiresTemporary { get; init; }

    public ImmutableArray<IType> TypeArguments { get; init; }

    public ImmutableArray<CallSiteExtraArgument> ExtraArguments { get; init; }

    /// <summary>Gets a type to which the result of the new call is cast, when the value is used and the return type differs.</summary>
    public IType? ResultCast { get; init; }

    public string? Description { get; init; }
}
```

`RedirectAccessor` is generic (R4): it replaces the accessor calls of a property or event access with calls of other methods, and keeps the evaluation order and the single evaluation of the receiver. It knows nothing about interceptors. The factory reads the shape of the site from the syntax and the semantic model of the site: the operator of a compound assignment and its explicit conversion, the prefix or postfix form, and the `??=` test. The requests for the get use and the set use of one site are merged into one `CallSiteRedirection` with two targets. The factory throws when a second request redirects a use that is already redirected, which is the guard of R7 per accessor use. It allocates the names of the temporaries with the lexical scope of the host (section [10.4.6](#1046-names-and-lexical-scopes)). The syntax of the rewrite is given in section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call).

`RedirectMethodReference` is generic (R4): it replaces a method group with a reference to another method, or with a delegate-creating wrapper, whose lambda can pass other arguments than the parameters of the delegate (`WrapperArguments`). It knows nothing about interceptors, templates or groups.

`RedirectedArgument` lets an extension describe the complete argument list of a rewritten call (RC63): the values of the source site in another order, a packed `params` argument, and expressions such as pulled values, caller information and `this`. The premium engine resolves the binding of a site into this list, with the expressions as `IExpression` values, and passes it in `Arguments`. The factory validates that each source value is used at most once and that a `ref`, `out` or `in` argument keeps its modifier. The premium engine has already checked that each expression binds at the site (E16). The linker writes the temporaries and the discards with the statement and pattern-variable forms of the compound accessor sites (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)), so no new syntax form is needed.

Redirections that keep the original target (RC68). The factory accepts a redirection whose target is the original method of the site, `CallSiteRedirectionTarget.Existing( originalMethod )`, with `ExtraArguments` that pass omitted optional parameters as named arguments. It must not refuse this case: no rule of the factory compares the target with the original method. The premium rules E2 (`InterceptorIsTarget`) and E17 (LAMA1018) concern an interceptor, which would call itself, and they are not rules of the factory. The caller-side argument providers of section [16.10](16-future-directions.md#1610-caller-side-argument-providers) need this primitive. The site model of the premium engine keeps exposing omitted arguments (`InvocationArgumentKind.DefaultValue`) and computed arguments (`InvocationArgumentKind.Computed`), which those providers read. It moves from the future work of an earlier version to version 1 (RC44). The syntax of the replacement is given in section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call).

The call-site rewrite separates the callee form from the receiver passing (challenge to B2c, adopted). At the call site, a `base` call to a virtual method becomes `this.I(args)` with the receiver dropped. The `base` call exists only inside the interceptor, through `ProceedReceiverKind.Base`. When the premium engine passes the receiver as a parameter (rules R1, R1x and R3 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)), an implicit or explicit `this` receiver, and the `base` receiver of a non-virtual target, are passed as the first argument, so that the proceed call binds on a parameter typed as the target's containing type. When the interceptor is an instance member of the receiver's type hierarchy (rule R2), `MemberOfReceiver` keeps the receiver, and the proceed call is `this.M(args)` through `ProceedReceiverKind.This`. The factory cannot know whether `this.M(args)` binds to the target in the placement: the premium engine guarantees it with the speculative binding of condition R2a. A conditional-access receiver `.B` in `a?.B.M(x)` exists only inside the conditional access. Moving it into an argument, as in `I(a.B, x)`, evaluates `x` and dereferences a null `a`. The local rewrites that keep the short-circuit are `a?.B.I(x)` with `I` an extension method visible by lookup (`ExtensionReceiver`), and `a?.B.I(x)` with `I` an instance member of the type of `.B` or of a base type of it (`MemberOfReceiver`).

For a synthesized target, the factory builds the `ArgumentNameMap` of the redirection by position: a named argument that names a parameter of the source method is renamed to the parameter of the synthesized method at the same position, after the receiver parameter when the mode is `FirstArgument*`. This covers the renames of the local-function scope (section [10.4.6](#1046-names-and-lexical-scopes)) and the renames of a premium signature builder, because neither can reorder parameters. For an existing target without `Arguments`, named arguments are kept, and the premium engine checks them (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9), E12). With `Arguments`, the names are those that the arguments carry.

The factory stores each validated redirection as data: an internal `CallSiteRedirection` that holds only syntax, keyed by syntax tree and then by the source node, with reference equality on nodes. `Complete()` returns the dictionary in `ExtensionLinkerInput.CallSiteRedirections`, of type `IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>>` (section [10.5.1](10b-oss-linker-and-templates.md#1051-internal-model-and-threading)). The dictionary is the whole linker input of call-site redirections: the linker needs no annotation, no collector and no substitution to apply it (RC39). The public request types stay the only way for an extension to add an entry, so the factory validates every entry. A public read-only view of the dictionary is not needed, because no extension reads it.

#### 10.4.6 Names and lexical scopes

Names are allocated at declaration time by the linker's own naming services, which the pipeline stage creates once and shares with the factory and with the linker (RC15):

```csharp
/// <summary>
/// Holds the naming services that the linker uses to allocate member names and local identifiers. One instance is shared by
/// the extension transformation factory and the linker, so that names allocated by extensions are known to the linker.
/// </summary>
internal sealed class LinkerNamingServices
{
    public LinkerNamingServices( CompilationModel finalCompilation );

    public LinkerInjectionHelperProvider HelperProvider { get; }

    public LinkerInjectionNameProvider NameProvider { get; }

    public LexicalScopeFactory LexicalScopeFactory { get; }
}
```

| Name | Allocator | Checks |
|---|---|---|
| Synthesized class | new `LinkerInjectionNameProvider.GetSynthesizedTypeName( INamespace ns, string hint )` | types of the namespace in the final compilation, types already synthesized, and `GetTypeByMetadataName` of the full name |
| Type-member method | new `LinkerInjectionNameProvider.GetSynthesizedMemberName( INamedType containingType, string hint, Func<string, bool>? isNameAvailable )` | declared members, inherited members, nested types, members of types derived from the placement type, the name of the placement type itself (CS0542), names already injected, and `SynthesizedMethodRequest.IsNameAvailable` |
| Method with a `this` parameter in a static class | same | the checks of the previous row. The premium engine passes an `IsNameAvailable` function that runs the lookup of check C5 (section [6.5.2](06b-signatures-and-validation.md#652-checks)) at every call site of the group. That lookup finds every extension method with the candidate name that is visible at a call site, including the extension methods of an internal class of a referenced assembly that `InternalsVisibleTo` makes visible. Extension methods of a class in the global namespace are candidates at every call site of the compilation and of every friend assembly, so two friend assemblies that generate the same interceptor would otherwise make the rewritten call sites ambiguous (CS0121). |
| Local function, its parameters and type parameters, reserved identifiers | `LexicalScopeFactory.GetLexicalScope( host.ToFullRef() ).GetUniqueIdentifier( hint )` | every identifier visible in the host body and every identifier already allocated in that scope (ENG26 `Linking\LexicalScopeFactory.cs:103-223`) |

Override templates of the host member later use the same `TemplateLexicalScope` object, keyed by the host (ENG26 `AdviceImpl\Override\OverrideMethodTransformation.cs:56-62`), so their locals avoid the local function and its parameters. The injection step allocates its own names later and sees the synthesized names in `_injectedMemberNames`.

Two defects are fixed on the way (section [10.10](10c-oss-reference-graph-design-time.md#1010-other-open-source-fixes-found-on-the-way)): `FindAndUpdate` of the name provider, and the empty lexical scope of builders in types without syntax.

#### 10.4.7 Attribution and ordering

Each extension transformation implements `ITransformation`:

- `AspectLayerInstance`: `new AspectLayerInstance( aspectInstance, layerName, finalCompilation )`. For an aspect or a type fabric, `aspectInstance` is `origin.AspectInstance`. For a project or namespace fabric, the origin holds no aspect instance (section [10.2.3](#1023-contribution-origin)), and the factory creates, once per stage, a synthetic instance of the top-level fabric aggregate aspect class whose target is the compilation of the stage. That instance lives only as long as the stage. `AspectReferenceResolver` indexes `_layerIndex[AspectLayerId]` (ENG26 `Linking\AspectReferenceResolver.cs:88-96, 564-569`), so the factory rejects origins whose layer is not in the ordered layers of the stage.
- `AdviceOrderingIndices`: `new AdviceOrderingIndices( origin.PipelineStepIndex, int.MaxValue - 1, sequence++ )`. The second index places the transformation after every advice of the same layer; it is `int.MaxValue - 1` because `GetMemberLayerIndex` adds 1. The third index is a factory-wide sequence number, so two extension transformations never compare equal in `TransformationLinkerOrderComparer` (ENG26 `Linking\TransformationLinkerOrderComparer.cs:33-42`).
- `Observability`: `TransformationObservability.None` (ENG26 `Transformations\TransformationObservability.cs`). The factory never calls `CompilationModel.AddTransformation`.
- `GeneratedCodeAnnotation`: the injected member uses `AspectInstance.AspectClass.GeneratedCodeAnnotation` (ENG26 `Linking\LinkerInjectionStep.Rewriter.cs:554-558`). The redirection uses the same annotation on the callee and on extra arguments. The original arguments keep their source annotation.

#### 10.4.8 Validation performed by the factory

The factory rejects programming errors of the caller with exceptions. User errors that come from the template are thrown as the existing public `DiagnosticException` (ENG26 `Diagnostics\DiagnosticException.cs:24`) or `InvalidTemplateSignatureException` (FW26 `Aspects\InvalidTemplateSignatureException.cs:36`). The premium engine catches them and reports them at the representative call site.

| Operation | Condition | Exception |
|---|---|---|
| any | The origin's layer is not in the ordered layers of the stage. | `ArgumentException` |
| any | The factory was completed. | `InvalidOperationException` |
| `DeclareStaticClass` | The namespace does not belong to the compilation. | `ArgumentException` |
| `DeclareMethod` (type member) | The type is not a class, struct or record of the current compilation. | `ArgumentException` |
| `DeclareMethod` | The signature is abstract, virtual, override, extern, partial or sealed, is an operator, implements an interface explicitly, or contains pointer types. | `ArgumentException` |
| `DeclareMethod` | The method is not static but the placement is a static class. | `ArgumentException` |
| `DeclareMethod` | `IsReadOnly` is set and the placement is not a struct, or the method is static. | `ArgumentException` |
| `DeclareMethod` | The first parameter has `IsThis` and the placement is not a non-generic, non-nested static class. | `ArgumentException` |
| `DeclareMethod` (local function) | The host is not a source member, has no body and no expression body, or is a partial definition without implementation. | `ArgumentException` |
| `DeclareMethod` (local function) | The signature has attributes and the host syntax tree uses C# earlier than 9. | `ArgumentException` |
| `DeclareMethod` (local function) | The host is a member of a C# 14 extension block (its declaring type is an `IExtensionBlock`, FW27 `Code\IExtensionBlock.cs:11`). `this` is not available in the body of an extension member. | `ArgumentException` |
| `DeclareMethod` | The selected template is interpreted as async and the return type is `void`. | `InvalidTemplateSignatureException` |
| `DeclareMethod` | The proceed binding is `Await` and the selected template is not interpreted as async. | `InvalidTemplateSignatureException` |
| `DeclareMethod` | A parameter index of the proceed binding is out of range, the type-argument count does not match, or `Base` or `This` is used on a static method or on a local function of a static host. | `ArgumentException` |
| `DeclareMethod` | `This` is used and the invoked method is not declared in the containing type of the synthesized method or in one of its base types. | `ArgumentException` |
| `Redirect*` | The node does not belong to a syntax tree of the stage's partial compilation. | `ArgumentException` |
| `Redirect*` | A redirection already exists for the node (guard for R7), except a request of `RedirectAccessor` for the other accessor use of the same site, which the factory merges. | `InvalidOperationException` |
| `RedirectInvocation` | The invocation is not bound to an ordinary, reduced-extension or C# 14 extension method. | `ArgumentException` |
| `RedirectInvocation` | `Drop` with a receiver that is an instance expression other than `base` with a virtual target. | `ArgumentException` |
| `RedirectInvocation` | `FirstArgument*` with a `base` receiver and a virtual target, a member-binding receiver (`?.`), or a static method. | `ArgumentException` |
| `RedirectInvocation` | `ExtensionReceiver` with a target that is not an extension method, or with a synthesized target whose class is not in the global namespace. | `ArgumentException` |
| `RedirectInvocation` | `MemberOfReceiver` with a static target, with a `base` receiver, or with a target whose declaring type is neither the static type of the receiver nor one of its base types. | `ArgumentException` |
| `RedirectInvocation`, `RedirectAwait` | `ResultCast` is set and the call site is an invocation whose expression is a `MemberBindingExpressionSyntax`, or is inside the receiver chain of such an invocation. A cast cannot be written inside a conditional access. | `ArgumentException` |
| `Redirect*` | The target is a local function and the call site is not inside the host body. | `ArgumentException` |
| `Redirect*` | The target is an instance method, the mode is not `MemberOfReceiver`, and the call site is not in a type that is or derives from its declaring type. | `ArgumentException` |
| `RedirectMethodReference` | The node is not the syntax of an `IMethodReferenceOperation` whose parent is an `IDelegateCreationOperation` or an `IAddressOfOperation`. | `ArgumentException` |
| `RedirectMethodReference` | A function-pointer conversion with a target that is not a static method of a type, or with a mode other than `Drop`. | `ArgumentException` |
| `RedirectMethodReference` | `ExtensionReceiver` with a target whose `this` parameter does not have a reference type (CS1113). | `ArgumentException` |
| `RedirectMethodReference` | A `FirstArgument` mode without `WrapperDelegateType`, with a delegate type that is not the converted type of the site, with a site that is the handler of an event assignment, or with a receiver of a ref-like type. | `ArgumentException` |
| `RedirectInvocation` | `Arguments` uses a value of the source site twice, passes a `ref`, `out` or `in` argument without its modifier, or omits a `ref` or `out` argument, which cannot be evaluated into a discard. | `ArgumentException` |
| `RedirectMethodReference` | `WrapperArguments` at a function-pointer site, at the handler of an event assignment, or without `WrapperDelegateType`. | `ArgumentException` |
| `RedirectAccessor` | The node is not a property or event access, or an assignment, compound assignment, `??=`, increment or decrement whose target is one; or the role is not an accessor use of the site (for example `Set` on a read). | `ArgumentException` |
| `RedirectAccessor` | Two requests of one site give different values of `Context` or `ReceiverRequiresTemporary`. | `ArgumentException` |
| `RedirectAccessor` | `ReceiverRequiresTemporary` with a struct receiver that is a variable in the context `ValueExpression` or `DiscardedExpression`, or in a method where C# does not allow a `ref` local. | `ArgumentException` |
| `RedirectAccessor` | The site is in an object or `with` initializer, in a deconstruction, or its setter is an `init` accessor. | `ArgumentException` |

Semantic rules that need the full interception context stay in the premium engine: accessibility, static contexts, struct receivers, expression trees, `[Conditional]` methods, `scoped`, ref returns.
