# Open-source extension points: reference graph, design time, helpers and file list

> Part of the [call-site interceptors design](README.md). Previous: [10b-oss-linker-and-templates.md](10b-oss-linker-and-templates.md) | Next: [11-delivery-plan.md](11-delivery-plan.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 10.7 Reference graph (B2e)

#### 10.7.1 EXISTING walker gaps

All line numbers refer to ENG27 `ReferenceGraph\ReferenceIndexWalker.cs`.

| Gap | Evidence | Consequence |
|---|---|---|
| No `VisitGenericName`. `M<int>()` and `x.M<int>()` index only the type arguments. | only `VisitIdentifierName` at 103 | Explicit generic invocations are missed. |
| Collection-expression elements are not visited, and the method returns early when the type is null. | 681-703 | Calls in `[Foo(), ..Bar()]` are missed. |
| Constructor-initializer arguments are not visited. | 532-535 | Calls in `: base(Foo())` are missed. |
| Array rank sizes are not visited. | 674-679 | Calls in `new int[Foo()]` are missed. |
| Receivers of assignment targets are visited twice, and element-access receivers are indexed with the `Assignment` kind. | 110-135 with 715-724 and 730-737 | Duplicate call sites. |
| A reduced-form classic extension call is keyed by `ReducedExtensionMethodSymbol`. | ENG27 `ReferenceGraph\ReferenceIndexBuilder.cs:20-21` | `x.Ext()` and `E.Ext(x)` have different keys. |
| No `await` indexing. | no `VisitAwaitExpression` | Await call sites cannot be found through the index. |
| A walk that starts at a `VariableDeclaratorSyntax` or an `ArrowExpressionClauseSyntax` indexes nothing. | 800-809; ENG27 `ReferenceGraph\OutboundReferenceIndexBuilder.cs:52-66` | A field or expression-bodied getter scope indexes nothing. |

#### 10.7.2 PROPOSED walker code

```csharp
// VisitGenericName (NEW). The stored node is the GenericNameSyntax, as the IdentifierNameSyntax is for a simple name.
public override void VisitGenericName( GenericNameSyntax node )
{
    this.IndexReference( node, node.Identifier );
    this.Visit( node.TypeArgumentList );
}

// VisitAssignmentExpression (lines 110-135, REPLACED).
public override void VisitAssignmentExpression( AssignmentExpressionSyntax node )
{
    this.Visit( node.Right );

    if ( node.Left is ElementAccessExpressionSyntax elementAccess )
    {
        // The indexer is assigned. The receiver and the arguments are read, so they keep the enclosing kind.
        this.IndexReference( elementAccess, elementAccess.ArgumentList, default, ReferenceKinds.Assignment );
        this.Visit( elementAccess.Expression );
        this.Visit( elementAccess.ArgumentList );
    }
    else
    {
        // VisitMemberAccessExpression visits the receiver with the member-access kind, so it is visited once.
        this.VisitWithReferenceKinds( node.Left, ReferenceKinds.Assignment );
    }
}

// VisitConstructorDeclaration (after line 535, NEW statements).
if ( node.Initializer != null && this._options.MustDescendIntoImplementation() )
{
    this.Visit( node.Initializer.ArgumentList );
}

// VisitArrayCreationExpression (lines 674-679, MODIFIED): visit the rank sizes, then the initializer.
foreach ( var rankSpecifier in node.Type.RankSpecifiers )
{
    foreach ( var size in rankSpecifier.Sizes )
    {
        this.Visit( size );
    }
}

// VisitCollectionExpression (lines 681-703, MODIFIED): the early return becomes a conditional block,
// and the elements are always visited afterwards.
foreach ( var element in node.Elements )
{
    this.Visit( element );
}

// VisitAwaitExpression (NEW).
public override void VisitAwaitExpression( AwaitExpressionSyntax node )
{
    this.Visit( node.Expression );

    if ( this._currentDeclarationNode == null || !this._options.MustIndexReferenceKind( ReferenceKinds.Await ) )
    {
        return;
    }

    if ( this.SemanticModel.GetAwaitExpressionInfo( node ).IsDynamic )
    {
        return;
    }

    var awaitedType = this.SemanticModel.GetTypeInfo( node.Expression, this._cancellationToken ).Type;

    if ( awaitedType is null or { TypeKind: TypeKind.Error or TypeKind.Dynamic } )
    {
        return;
    }

    this._observer?.OnSymbolResolved( awaitedType );

    if ( !this.IsCurrentDeclarationInRunTimeCode() )
    {
        return;
    }

    // The key is the awaited type, which may be a type parameter. No identifier filtering is possible for this kind.
    this._referenceIndexBuilder.AddReference( awaitedType, this.CurrentDeclarationSymbol, node, ReferenceKinds.Await );
}

// VisitDeclarationRoot (NEW, internal). Entry point for a walk that starts at the syntax of a declaration.
internal void VisitDeclarationRoot( SyntaxNode node )
{
    this._syntaxTree ??= node.SyntaxTree;

    switch ( node )
    {
        case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax fieldDeclaration } variable:
            using ( this.EnterDeclaration( variable ) )
            {
                this.VisitTypeReference( fieldDeclaration.Declaration.Type, ReferenceKinds.MemberType );

                if ( variable.Initializer != null && this._options.MustDescendIntoImplementation() )
                {
                    this.Visit( variable.Initializer );
                }
            }

            break;

        case ArrowExpressionClauseSyntax { Parent: BasePropertyDeclarationSyntax propertyDeclaration } arrow:
            if ( this.SemanticModel.GetDeclaredSymbol( propertyDeclaration ) is IPropertySymbol { GetMethod: { } getter } )
            {
                using ( this.EnterDeclaration( arrow, getter ) )
                {
                    this.Visit( arrow );
                }
            }

            break;

        default:
            this.Visit( node );

            break;
    }
}
```

`CanIndexSymbol` (lines 899-930) is refactored so that its compile-time check moves to `IsCurrentDeclarationInRunTimeCode`, shared with `VisitAwaitExpression`. `SemanticModel.GetAwaitExpressionInfo` and `AwaitExpressionInfo.IsDynamic` are public (RC `Compilation\AwaitExpressionInfo.cs:15-39`).

#### 10.7.3 ReducedFrom normalization

```csharp
// ENG27 ReferenceGraph/ReferenceIndexBuilder.cs:13-21 (MODIFIED)
if ( referencedSymbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension, ReducedFrom: { } reducedFrom } )
{
    // A classic extension method called in the reduced form binds to a symbol whose OriginalDefinition is the symbol itself.
    // The static definition is the declaration that the code model exposes.
    referencedSymbol = reducedFrom;
}

referencedSymbol = referencedSymbol.OriginalDefinition;
```

C# 14 extension-block members are not affected, because their method kind is not `ReducedExtension`. The existing test `InvocationOfExtensionMethodValidatedThroughExtensionBlock` (TST27 `Metalama.Framework.Tests.UnitTests\ReferenceIndex\InboundReferenceIndexTests.cs:331-348`) guards this.

#### 10.7.4 ReferenceKinds.Await

```csharp
// FW27 Code/ReferenceKinds.cs (MODIFIED, after UnionCaseType = 1 << 27)

/// <summary>
/// An explicit <c>await</c> expression. The referenced declaration is the type of the awaited expression, and the
/// referencing node is the <c>AwaitExpressionSyntax</c>.
/// </summary>
/// <remarks>
/// <para>
/// This kind covers only the <c>await</c> operator. The awaits that the compiler generates for <c>await foreach</c> and
/// <c>await using</c> are not references of this kind. Awaits of a <c>dynamic</c> value are not reported.
/// </para>
/// <para>A request whose value derives from <see cref="All"/> does not include this kind. It must be requested explicitly.</para>
/// </remarks>
Await = 1L << 28
```

| Question | Decision | Evidence |
|---|---|---|
| Bit | 28. Bit 27 is `UnionCaseType` in 2027.0. Bit 6 is unused but its history is unknown. | FW27 `Code\ReferenceKinds.cs:59, 176` |
| Excluded from `All` requests | Yes. `ReferenceKinds.All` is -1 (FW27 `Code\ReferenceKinds.cs:23`), so a value derived from it, such as `ReferenceKinds.All & ~ReferenceKinds.UsingNamespace`, also contains bit 28. `ReferenceIndexerRequirements.Create` therefore removes `Await` from any value that contains a bit that no named kind defines: `if ( (referenceKinds & ~namedKinds) != 0 ) { referenceKinds &= namedKinds & ~ReferenceKinds.Await; }`, where `namedKinds` is the bitwise OR of all named kinds except `All`. `ReferenceIndexerOptions.All` excludes it as well. Existing validators, Architecture rules and introspection receive no new reference (PO32). A unit test registers a type validator with `ReferenceKinds.All & ~ReferenceKinds.UsingNamespace` and asserts that it receives no `Await` reference. | ENG27 `ReferenceGraph\ReferenceIndexerRequirements.cs:21-74`; `ReferenceIndexerOptions.cs:175-185`; P27 `Metalama.Extensions.Validation.Engine\ReferenceValidatorRunner.cs:155` |
| Key | The type of the awaited expression, normalized to its original definition. Type parameters are kept. | `GetAwaiterMethod` is null under runtime async. |
| Node | The `AwaitExpressionSyntax`, which is the node that the linker rewrites. | |
| Identifier filtering | None. The await requirement uses `DeclarationKind.Compilation`, which removes the kind from the filtered kinds. | ENG27 `ReferenceGraph\ReferenceIndexerOptions.cs:80-84, 191-209` |

#### 10.7.5 Shared index of source references

PROPOSED. A generic project service builds one index of the references of the source compilation per high-level stage, an `InboundReferenceIndex` keyed by referenced symbol, and shares it between all extensions. It contains no interceptor concept (R4). It replaces the public `OutboundReferenceIndexBuilder` of an earlier version of this design as the entry point of the premium engines. `OutboundReferenceIndexBuilder` stays internal and is used only by introspection, as today (ENG27 `ReferenceGraph\OutboundReferenceIndexBuilder.cs:18`).

EXISTING situation. The Validation engine builds its own `InboundReferenceIndexBuilder` over the trees of the initial compilation of the stage, with the `SemanticModelProvider` of that compilation (P27 `Metalama.Extensions.Validation.Engine\ReferenceValidatorRunner.cs:66-79`). It runs in `ExecutePipelineContributorsAsync`, which `LinkerPipelineStage` calls just before the linker (ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:39-66`). An interceptor engine with its own scan would bind the same bodies a second time.

```csharp
// ENG27 Extensibility/PipelineExtension.cs (MODIFIED, new member)

/// <summary>
/// Returns the requirements of the extension for the index of the references of the source compilation of the current
/// high-level stage. The method is invoked once per stage, on every extension, before
/// <see cref="ExecutePipelineContributorsAsync"/>. The default implementation returns
/// <see cref="SourceIndexRequirements.None"/>.
/// </summary>
/// <remarks>
/// The requirements of all extensions are merged, and the index is built at most once per stage, when an extension first
/// reads it. An extension that returns no requirement can still read the index, but the index then contains only the
/// references that other extensions requested.
/// </remarks>
public virtual SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context )
    => SourceIndexRequirements.None;
```

```csharp
// NEW FILE: ENG27 ReferenceGraph/SourceIndexRequirements.cs
namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>The inputs of <see cref="PipelineExtension.GetSourceIndexRequirements"/>.</summary>
[PublicAPI]
public sealed record SourceIndexRequirementsContext(
    IReadOnlyCollection<IPipelineContributor> Contributors,
    int HighLevelStageIndex );

/// <summary>The requirements of one extension for the index of the source references of one stage.</summary>
[PublicAPI]
public sealed record SourceIndexRequirements( ImmutableArray<ReferenceIndexerRequirements> Requirements )
{
    public static SourceIndexRequirements None { get; } = new( ImmutableArray<ReferenceIndexerRequirements>.Empty );

    /// <summary>
    /// Gets the syntax nodes of the declarations that contain every reference that the extension needs, or <c>null</c>
    /// when the extension needs the references of every syntax tree.
    /// </summary>
    public ImmutableArray<SyntaxNode>? DeclarationRoots { get; init; }
}
```

```csharp
// NEW FILE: ENG27 ReferenceGraph/SourceReferenceIndexService.cs
namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// Builds the index of the references of the source compilation once per high-level stage, from the merged requirements
/// of all extensions, and shares it between them.
/// </summary>
[PublicAPI]
public sealed class SourceReferenceIndexService : IProjectService
{
    /// <summary>
    /// Gets the index of the current stage, or <c>null</c> outside of a stage. Code that runs in
    /// <see cref="PipelineExtension.ExecutePipelineContributorsAsync"/> reads the index through this property, because the
    /// signature of that method does not change.
    /// </summary>
    public SourceReferenceIndexStage? Current { get; }

    /// <summary>Starts a stage. Called by <c>LinkerPipelineStage</c> before the first extension hook.</summary>
    internal SourceReferenceIndexStage BeginStage(
        CompilationModel sourceCompilation,
        IEnumerable<(PipelineExtension Consumer, SourceIndexRequirements Requirements)> requirements );

    /// <summary>
    /// Returns the index of the references of one semantic model at design time. The index is built once per
    /// <see cref="SemanticModel"/> object, from <see cref="DesignTimeAspectPipelineResultExtensionCollection.Options"/>, and
    /// cached in a static <c>ConditionalWeakTable</c>, so every extension that analyzes the same semantic model reads the
    /// same index.
    /// </summary>
    public InboundReferenceIndex GetDesignTimeIndex(
        SemanticModel semanticModel,
        DesignTimeAspectPipelineResultExtensionCollection extensions,
        CancellationToken cancellationToken );
}

/// <summary>The index of the references of the source compilation for one high-level stage.</summary>
/// <remarks>The object references a compilation. It must not outlive the stage.</remarks>
[PublicAPI]
public sealed class SourceReferenceIndexStage : IDisposable
{
    /// <summary>Gets the merged options of the stage.</summary>
    public ReferenceIndexerOptions Options { get; }

    /// <summary>
    /// Returns the index of the stage. The index is built on the first call, with the
    /// <see cref="SemanticModelProvider"/> of the source compilation, one concurrent task per tree. It covers only the
    /// union of the declaration roots when every extension that returned requirements for the stage also returned roots,
    /// and every syntax tree of the source compilation otherwise.
    /// </summary>
    public Task<InboundReferenceIndex> GetIndexAsync( CancellationToken cancellationToken );

    /// <summary>Ends the stage and releases the index.</summary>
    public void Dispose();
}

/// <summary>
/// Implemented by a design-time pipeline result extension that needs references of the analyzed file. The requirements
/// are added to the options of the collection of the project, and are not merged into the options of referencing projects.
/// </summary>
[PublicAPI]
public interface IDesignTimeReferenceIndexRequirementsProvider
{
    IEnumerable<ReferenceIndexerRequirements> ReferenceIndexerRequirements { get; }
}
```

Rules:

| Topic | Rule | Reason |
|---|---|---|
| Consumers | Validators read the index in `ExecutePipelineContributorsAsync` through `SourceReferenceIndexService.Current`. Transforming extensions read it through `ExtensionTransformationContext.SourceReferenceIndex` (section [10.3.2](10a-oss-bridge-hook-factory.md#1032-the-context)). | The signature of the existing hook does not change. |
| One build per stage | The index is built lazily, at most once per stage, from the merged requirements of all extensions. | Validators and interceptors share the syntax walk and the binding. |
| Shared binding | The index uses the `SemanticModelProvider` of the source compilation, as the Validation engine does today. `GetOperation` calls of the interceptor engine on the same trees use the same provider. | A body bound for one consumer is free for the other (section [9.6](09-premium-engine.md#96-cost-model-and-shared-binding)). |
| Merge of name filters | EXISTING: the merge of options intersects the masks of the kinds that are filtered by identifier and unions the identifiers into one set for all kinds (ENG27 `ReferenceGraph\ReferenceIndexerOptions.cs:131-150`). PROPOSED: one identifier set per reference kind, which is the union of the names that the consumers request for that kind. Filtering is disabled only for a kind for which some consumer is unconstrained. | A name requested for one kind no longer admits references of other kinds, and a consumer without names on one kind does not disable filtering of the other kinds. A requirement whose kinds derive from `ReferenceKinds.All` still disables filtering of every kind that it contains. |
| Scope-restricted walking | The stage walks only the union of the declaration roots when every consumer of the stage returned roots in its `SourceIndexRequirements`. When one consumer returned requirements without roots, the stage walks every tree. The roots are walked with the internal entry point `VisitDeclarationRoot` of section [10.7.2](#1072-proposed-walker-code), a root nested in another root is removed, and the references are recorded in an `InboundReferenceIndexBuilder` (RC52). | A consumer that needs whole trees would otherwise need a second walk. The rule names no consumer, so validators can restrict the walk when all their scopes are known declarations. It replaces the earlier rule, which restricted the walk only when one consumer, the interceptor engine, was the only one. |
| Stage index | `SourceIndexRequirementsContext.HighLevelStageIndex` lets an extension return `SourceIndexRequirements.None` in a stage where it reads nothing. The interceptor engine returns requirements only in the source stage (section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)). | A later stage then builds no index for the replayed registrations of static fabrics. |
| `ReferenceKinds.Await` | Keyed by the awaited type (section [10.7.4](#1074-referencekindsawait)). Only interceptors request it. It is excluded from the masks that derive from `ReferenceKinds.All`. | Existing validators receive no new reference. |
| Design time | `GetDesignTimeIndex` builds one index per `SemanticModel` object for Phase B of all extensions. The options come from `DesignTimeAspectPipelineResultExtensionCollection.Options`, which include the requirements of `IDesignTimeReferenceIndexRequirementsProvider` extensions of the project. The collection keeps these requirements out of the options that referencing projects merge (ENG27 `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs:58-59`). | `TheDiagnosticAnalyzer` passes the same `SemanticModel` to every extension (DT27 `DiagnosticAnalysis\TheDiagnosticAnalyzer.cs:199-205`). The cache is a `ConditionalWeakTable`, whose entries die with the semantic model (DOCS27 `design-time-memory.md:214-228`). |
| Compatibility | Until the Validation engine migrates (F20), it returns no requirement and builds its own index as today. Its results do not change. | The migration is a separate change. |

Size: M, about 330 lines with documentation, for `SourceReferenceIndexService`, `SourceReferenceIndexStage`, `GetSourceIndexRequirements` with its context and result records, the per-kind merge, the union of declaration roots and the design-time cache.

#### 10.7.6 Gating

The walker fixes are not gated. They ship in 2027.0, a feature release, with release notes: explicit generic invocations, generic attributes and qualified generic type names, calls in collection expressions, constructor initializers and array sizes, reduced-form extension calls, and removal of duplicate references (PO31). They are not backported to 2026.1. If a backport is ever needed, the fixes must be gated behind a `ReferenceIndexerOptions` flag that only the interceptor engine sets, because validator options are shared across projects (ENG27 `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs:58-59`).

### 10.8 Design-time plumbing (B2f)

#### 10.8.1 ContributorKind.IsProjectLocal

```csharp
// ENG27 Extensibility/ContributorKind.cs (MODIFIED, after line 25)

/// <summary>Gets a value indicating whether the design-time results of this kind remain in the project that produced them.</summary>
/// <remarks>
/// <para>
/// The design-time pipeline stores the design-time form of every contributor that an extension returns. By default, it also
/// exports this form to the projects that reference the project: it writes it to the design-time transitive manifest, and
/// the presence of such a form makes the pipeline produce this manifest.
/// </para>
/// <para>
/// The design-time forms of a project-local kind are excluded from both. Only <see cref="PipelineExtension.AnalyzeSemanticModel"/>
/// of the producing project sees them, and <see cref="IDesignTimePipelineResultExtension.ToTransitiveAspectManifestExtension"/>
/// is never called for them. The flag is read from the kind of the design-time form, which
/// <c>ITransitivePipelineContributor.ToDesignTime</c> returns. The compile-time pipeline does not read it.
/// </para>
/// <para>
/// A kind cannot be both project-local and a design-time validator. The <c>init</c> accessors of this property and of
/// <see cref="IsDesignTimeValidator"/> throw <see cref="InvalidOperationException"/> for this combination, so that an
/// invalid kind fails when it is declared.
/// </para>
/// </remarks>
public bool IsProjectLocal { get; init; }
```

#### 10.8.2 Consumers of the flag

```csharp
// ENG27 Extensibility/DesignTimeAspectPipelineResultExtensionCollection.cs:82-85 (MODIFIED)
public ImmutableArray<ITransitiveAspectsManifestExtension> ToTransitiveValidatorInstances( bool includeValidators )
    => this.Extensions
        .Where( e => !e.ContributorKind.IsProjectLocal && (includeValidators || !e.ContributorKind.IsDesignTimeValidator) )
        .Select( e => e.ToTransitiveAspectManifestExtension() )
        .ToImmutableArray();

/// <summary>Gets a value indicating whether referencing projects receive anything from this collection.</summary>
[Memo]
public bool HasExportedContent
    => !this._allValidators.IsEmpty || this.Extensions.Any( e => !e.ContributorKind.IsProjectLocal );
```

```csharp
// DT27 Pipeline/DesignTimeAspectPipelineResult.cs:801-805 (MODIFIED)
internal bool HasTransitiveAspectManifestContent
    => !this._inheritableAspects.IsEmpty
       || !this.InheritableOptions.IsEmpty
       || !this.Annotations.IsEmpty
       || this.Extensions.HasExportedContent;
```

The compile-time pipeline needs no change. At compile time, the premium engine returns no transitive contributor (section [9.1.3](09-premium-engine.md#913-extension-export)), and the compile-time kind `InterceptorRegistration` is not project-local anyway.

The alternative, a nullable `ToTransitiveAspectManifestExtension`, was rejected: it is per instance rather than per kind, and the extension would still count in `HasTransitiveAspectManifestContent`.

With the new gate, a project can drop its design-time manifest while its `Extensions` collection is not empty, because the collection can hold only project-local results. Two existing comments state the opposite invariant, and they change as follows: the remarks of `HasTransitiveAspectManifestContent` (DT27 `Pipeline\DesignTimeAspectPipelineResult.cs:788-800`) and the comment at DT27 `Pipeline\DesignTimeAspectPipeline.cs:615-619`. When the manifest is dropped, the collection holds no exported result, although it can hold project-local results. A referencing project does not need project-local results, so `DesignTimeProjectVersion.ReferencedExtensions` still loses nothing. The term `!_allValidators.IsEmpty` keeps the manifest of a project whose referenced projects export validators.

#### 10.8.3 Fix of the default-bucket overwrite

EXISTING: `SplitResultsByTree` creates a separate builder for the default key when a contributor or an aspect instance has no document (DT27 `Pipeline\DesignTimeAspectPipelineResult.cs:549-552, 619-622`). It also creates `emptySyntaxTreeResult` for diagnostics, introductions and transformations without a document (lines 406, 434, 504, 672, 714, 741). At the end, it replaces the default entry with `emptySyntaxTreeResult` (lines 771-774). When a run produces both a default-key contributor and, for example, a diagnostic without a path, the contributor is silently lost. The same code exists in 2026.1.

PROPOSED: use one builder for the default key.

```csharp
// DT27 Pipeline/DesignTimeAspectPipelineResult.cs:549-552 and 619-622 (MODIFIED)
if ( documentKey.IsDefault && !resultBuilders.ContainsKey( default ) )
{
    resultBuilders.Add( default, emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null ) );
}
// Lines 771-774 stay: they assign the same instance.
```

#### 10.8.4 No scope-keyed lookup

No new lookup is added to the open-source collection (RC21). The existing index serves inbound validators: it is keyed by the validated declaration and merged from referenced projects (ENG27 `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs:53-56, 66-78, 120-128`). Interceptors need scope and target keys and no merge. The premium engine builds its own index (section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)). `AnalyzeSemanticModel` keeps its signature (ENG27 `Extensibility\PipelineExtension.cs:74-80`).

#### 10.8.5 Change S1: multi-stage design-time accumulation

EXISTING: both design-time and linker stages publish only the transitive contributors of their own stage (ENG27 `Pipeline\DesignTime\DesignTimePipelineStage.cs:74`; `Pipeline\CompileTime\LinkerPipelineStage.cs:99`), and the design-time pipeline keeps only the result of the last stage (DT27 `Pipeline\DesignTimeAspectPipeline.PipelineState.cs:547`). When a weaver splits the stages, registrations made by aspects of the first stage are lost at design time, and registrations of later-stage aspects are analyzed as valid (section [9.7.1](09-premium-engine.md#971-phase-a-design-time-pipeline)). PROPOSED (separate pull request, required by the interceptor engine, PO33): in later stages, pass only `ContributorsAddedInStage` to the existing hooks, accumulate the transitive contributors across stages, and pass the high-level stage index to `ExecuteDesignTimePipelineContributorsAsync`. The stage list of the configuration is internal (ENG27 `Pipeline\AspectPipelineConfiguration.cs:32`), so the extension cannot compute the index itself. The change is named S1 so that it cannot be confused with milestone M1.

#### 10.8.6 Hierarchical options at design time

EXISTING: `CompilationModel.CreateInitialInstance` without a hierarchical options manager falls back to `NullHierarchicalOptionsManager` (ENG27 `CodeModel\CompilationModel.cs:75-91, 143-144`), and `HierarchicalOptionsManager.InitializeAsync` is internal (ENG27 `HierarchicalOptions\HierarchicalOptionsManager.cs:56`). An analyzer extension therefore cannot create a model in which `DeclarationEnhancements.GetOptions<T>()` returns the options set by fabrics and attributes. The Validation runner has the same gap (P27 `DesignTimeReferenceValidatorRunner.cs:37-42`).

PROPOSED (E3): a public helper `DesignTimeHierarchicalOptions.CreateManager( AspectPipelineConfiguration configuration, Compilation compilation, CancellationToken cancellationToken )` that returns an initialized manager built from the option sources of the configuration, and an overload of `CompilationModel.CreateInitialInstance` that accepts it. Options that aspects provide through `IHierarchicalOptionsProvider` are not available, because Phase B runs no aspect. The premium engine uses the helper in Phase B (section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)). The Validation runner can adopt it in a separate change.

### 10.9 Small public helpers (B2g)

| Candidate | Decision | Reason |
|---|---|---|
| Public method-signature comparer based on `SignatureTypeComparer` | Not proposed (RC22). | The comparer is internal and described as probably incorrect in its own comments (ENG27 `CodeModel\Comparers\SignatureTypeComparer.cs:16-21`). Its `GetHashCode` for arrays calls itself with the same argument on both paths (lines 104 and 207), which recurses without end (fix F10). The premium engine uses Roslyn's `SymbolEqualityComparer` and its own `SignatureType`. |
| Awaitable information | Not proposed. | `SemanticModel.GetAwaitExpressionInfo` and `IAwaitOperation` are public and complete. |
| Writable-variable classification | Not proposed. | Roslyn exposes what is needed: `ILocalSymbol.IsForEach`, `IsUsing`, `IsFixed`, `RefKind`, `ScopedKind`, `IParameterSymbol.RefKind`, `IFieldSymbol.IsReadOnly`. |
| `SymbolDictionaryKey.CreateLookupKey` | Proposed (E1): `internal` becomes `public` (ENG27 `Utilities\Roslyn\SymbolDictionaryKey.cs:49`), with a remark that the key references the symbol and must never be stored. | It avoids an identifier string for each lookup that misses. A lookup that hits still computes the identifier of the looked-up symbol once, because `SymbolDictionaryKey.Equals` compares identifier strings when the hash codes match (ENG27 `Utilities\Roslyn\SymbolDictionaryKey.cs:36-57`). Most Phase B lookups of scope keys miss. |
| `PartialCompilation.IsSyntaxTreeObserved` | Optional. | It would let the engine scan only the observed tree in preview (ENG27 `CodeModel\PartialCompilation.cs:62`). |
| `ExtensionTemplateServices` | Proposed (section [10.6.1](10b-oss-linker-and-templates.md#1061-selection-at-declaration-time)). | Registration and design-time checks of templates without expansion, and the shape of an await template (`TryGetMethodTemplateShape`, RC60). |
| SDK accessor for the syntax of a source expression | Proposed (X2), in `Metalama.Framework.Sdk`. | The code model deliberately exposes no expression tree. A provider that needs the structure of an expression of an interception context uses the Roslyn syntax. See below. |
| Source-expression factory | Proposed (X1, section [10.6.8](10b-oss-linker-and-templates.md#1068-inspection-only-source-expressions)), in `Metalama.Framework.Engine`. | An extension cannot create the internal `SourceUserExpression`. |
| Kind and parameter of a `PullAction` | Proposed (E3), in `Metalama.Framework`: the enumeration `PullActionKind` and the property `PullAction.Kind` become public, and `PullAction.UseExistingParameter` records a durable reference to its parameter in a new public property. | The kind is internal today (FW27 `Advising\PullAction.cs:42`, `Advising\PullActionKind.cs`), and `default( PullAction )` cannot be told apart from `IntroduceParameterAndPull` through the public `Expression`, which is `null` for both. `UseExistingParameter` stores only the expression `ExpressionFactory.Parse( parameter.Name )` (line 119), so an extension cannot tell a pulled parameter from a parsed name. The interceptor engine needs both facts: it refuses `IntroduceParameterAndPull`, and it applies the guard of section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values) to pulled parameters only. The premium engine has no `InternalsVisibleTo` access to `Metalama.Framework` (FW27 `Metalama.Framework.csproj:15-36`). |

EXISTING state of the SDK and the engine for expressions:

- The public `ISourceExpression.AsSyntaxNode` already returns the source node of a source expression, typed as `object` (FW27 `Code\ISourceExpression.cs:15`). `Metalama.Framework` does not reference Roslyn, which explains the type.
- The engine's `ExpressionExtensions.ToExpressionSyntax` (ENG27 `Templating\Expressions\ExpressionExtensions.cs:50`) is not public: the class is `internal static` (line 16). It also generates syntax through a `SyntaxSerializationContext`, which can add a cast (ENG27 `Templating\Expressions\SourceUserExpression.cs:31-41`), so it does not return the source node.
- The SDK already bridges the code model to Roslyn with `SymbolExtensions.GetSymbol` and `SourceReferenceExtensions.SyntaxNodeOrToken`, in the namespace `Metalama.Framework.Engine.CodeModel` (SDK27 `CodeModel\SymbolExtensions.cs:36, 55`; `CodeModel\SourceReferenceExtensions.cs:17-31`). It has no member for expressions.

PROPOSED minimal addition, next to `SourceReferenceExtensions`:

```csharp
namespace Metalama.Framework.Engine.CodeModel;

/// <summary>Extension methods that give access to the Roslyn syntax of expressions of the code model.</summary>
[PublicAPI]
public static class SourceExpressionExtensions
{
    /// <summary>
    /// Gets the source <see cref="ExpressionSyntax"/> of an expression that wraps source syntax, or <c>null</c> when the
    /// expression does not wrap source syntax, for example a generated expression or a parameter.
    /// </summary>
    public static ExpressionSyntax? GetSourceSyntax( this IExpression expression )
        => expression is ISourceExpression { AsSyntaxNode: ExpressionSyntax syntax } ? syntax : null;
}
```

The method is a typed view over `ISourceExpression.AsSyntaxNode`. It needs no engine access, so it fits the SDK. It works for the expressions of the interception contexts and for the initializers of source fields, properties and events.

### 10.10 Other open-source fixes found on the way

| Id | Defect | Evidence | Fix |
|---|---|---|---|
| F12 | A contributor added after `BuildAspect` through `builder.Outbound` is silently lost. | ENG27 `Aspects\AspectBuilderState.cs:78-101`; `Aspects\AspectBuilder.cs:199` | `AspectBuilderState.AddContributor` throws `InvalidOperationException` after `ToResult`. It also benefits validators (PO21). |
| F14 | `LinkerInjectionNameProvider.FindAndUpdate` tests `hint` instead of `candidate`: the loop runs forever when the hint collides with an existing member, and a candidate such as `Foo1` is never checked. | ENG26 `Linking\LinkerInjectionNameProvider.cs:220-240` (line 228); same in 2027.0 | Test `candidate`. |
| F9 (part) | `LexicalScopeFactory` returns an empty scope without the method's parameters for builders in types without syntax. | ENG27 `Linking\LexicalScopeFactory.cs:115-119` | Include parameter and type-parameter names, and `value` for setters. |
| F6 | `Adviser<T>.With` compares the declaration with itself. | FW27 `Aspects\AdviserExtensions.cs:2109` | Compare with `this.Target`. |
| F13 (optional) | Child aspects added through a type-fabric adviser are attributed to the aggregate aspect. | ENG27 `Advising\AdviceFactory.cs:2325, 2374` | Use the owner's predecessor. Separate pull request; changes predecessor chains. |
| F15 | The injection rewriter discards rewrites of root and namespace members when injections exist. | ENG26 `Linking\LinkerInjectionStep.Rewriter.cs:1878, 1898, 1918-1919` | Use the visited members (section [10.5.3](10b-oss-linker-and-templates.md#1053-injection-step-and-rewriter)). |
| F11 | A tree that Metalama rewrites invalidates the `[InterceptsLocation]` attributes that target it, and the user sees only CS9234. | ENG26 `Linking\LinkerInjectionStep.cs:352-358` | After linking, for each `[InterceptsLocation]` attribute of the input compilation whose target tree was replaced, report LAMA0662 at the attribute (section [9.11.3](09-premium-engine.md#9113-open-source-diagnostics)). |
| F18 | `AsyncHelper.TryGetAsyncInfoFromCodeModel` does not treat `Task` and `Task<TResult>` as having a method builder. A Default template on a synthesized method whose return type `Task<T>` mentions an introduced type parameter is not interpreted as async. | ENG27 `CodeModel\Helpers\AsyncHelper.cs:47-54, 74-111`, compared with lines 151-155 | Add the `Task` check of `GetAwaitableResultTypeCore` (section [7.9.4](07-await-interception.md#794-interaction-with-the-existing-async-machinery)). |
| F19 | The linker drops the initializer of a property that is not an override target when an accessor body has substitutions, which is possible for a C# 14 semi-automatic property. The existing `OnInitialized` advice is affected. | ENG27 `Linking\LinkerRewritingDriver.Properties.cs:186-189, 298` | Keep the initializer when `isOverrideOrOverrideTarget` is false, and apply the substitutions of the property to it (section [10.5.7](10b-oss-linker-and-templates.md#1057-linking-step)). |

### 10.11 Open-source file list and size

| Id | File (ENG27 unless stated) | Kind | Estimated lines with documentation |
|---|---|---|---|
| A | `Advising\AdviserExtensibility.cs`, `Advising\AdviserExtensionContext.cs`, `Extensibility\ExtensionContributionOrigin.cs`, `Extensibility\IExtensionContributionOriginSource.cs` | new | 330 |
| A | `Advising\AdviceFactory.cs`, `IAdviceFactoryImpl.cs`, `AdviceFactoryState.cs`, `Aspects\AspectDriver.cs`, `Aspects\AspectBuilder.cs`, `Fabrics\FabricDriver.BaseAmender.cs`, `Fabrics\TypeFabricDriver.cs` | modified | 130 |
| B | `Extensibility\PipelineExtension.cs`, `Pipeline\HighLevelPipelineStage.cs`, `Pipeline\AspectPipeline.cs`, `Pipeline\CompileTime\LinkerPipelineStage.cs`, `Pipeline\PipelineStepsState.cs`, `Pipeline\PipelineStepsResult.cs`, `Pipeline\ExecuteAspectLayerPipelineStep.cs` | modified | 100 |
| B | `Extensibility\ExtensionTransformationContext.cs` | new | 170 |
| T | `Extensibility\Transformations\*.cs` (factory, requests, handles, placement, template, proceed binding with accessors, redirections, including `MethodReferenceRedirectionRequest` and `AccessorRedirectionRequest`) | new | 1,420 |
| T | `Extensibility\Transformations\ExtensionTransformationFactoryContext.cs`, `ExtensionLinkerInput.cs`, `ExtensionExpansionState.cs`, `ExtensionTemplateServices.cs` | new | 220 |
| T | `Transformations\Extensions\SynthesizedMethodTransformation.cs`, `IInjectLocalFunctionTransformation.cs`, `SynthesizedLocalFunctionTransformation.cs`, `ExtensionProceedBinding.cs`, `ExtensionProceedExpressionFactory.cs` | new | 700 |
| T | `Advising\MethodTemplateSelection.cs` (moved code), `Advising\TemplateBindingHelper.cs`, `Advising\AdviceFactory.cs`, `Transformations\ProceedHelper.cs`, `Templating\TemplateExpansionContext.ConfigureAwaitUserExpression.cs`, `AdviceImpl\Introduction\IntroduceNamedTypeTransformation.cs` | new and modified | 240 |
| L | `Linking\LinkerNamingServices.cs`, `CallSiteRedirection.cs`, `CallSiteRewriter.cs` (calls, awaits, method groups, the wrapper, and the accessor and compound rewrites with their temporaries) | new | 820 |
| L | `Linking\AspectLinkerInput.cs`, `LinkerInjectionStepOutput.cs`, `LinkerInjectionStep.cs`, `LinkerInjectionStep.Rewriter.cs` (invocation, await, method-group, assignment, unary and expression-statement visitors, declarator and base-list descent, applied record, fix F15), `LinkerInjectionStep.TransformationCollection.cs`, `LinkerInjectionRegistry.cs`, `LinkerAnalysisStep.cs` and `LinkerAnalysisStep.AspectReferenceCollector.cs` (local-function hosts only), `LinkerRewritingDriver.Properties.cs` (fix F19), `LinkerInjectionNameProvider.cs`, `LexicalScopeFactory.cs`, `AspectLinkerDiagnosticDescriptors.cs` | modified | 640 |
| M | FW27 `Aspects\IMetaExtension.cs` (new), `Aspects\meta.cs`, `Aspects\IMetaApi.cs`; ENG27 `Templating\MetaModel\MetaApiProperties.cs`, `Templating\MetaModel\MetaApi.cs` | new and modified | 120 |
| X | ENG27 `Templating\SourceExpressionFactory.cs` (new), `Templating\Expressions\SourceUserExpression.cs`, `Templating\TemplatingDiagnosticDescriptors.cs` (LAMA0297); SDK27 `CodeModel\SourceExpressionExtensions.cs` (new) | new and modified | 90 |
| C | `Extensibility\ContributorKind.cs`, `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs`, DT27 `Pipeline\DesignTimeAspectPipelineResult.cs`, DT27 `Pipeline\DesignTimeAspectPipeline.cs`, new `HierarchicalOptions\DesignTimeHierarchicalOptions.cs`, `CodeModel\CompilationModel.cs` | new and modified | 110 |
| D | FW27 `Code\ReferenceKinds.cs`, `ReferenceGraph\ReferenceIndexWalker.cs`, `ReferenceIndexBuilder.cs`, `ReferenceIndexerRequirements.cs`, `OutboundReferenceIndexBuilder.cs` (stays internal; calls `VisitDeclarationRoot`) | modified | 200 |
| D | New `ReferenceGraph\SourceReferenceIndexService.cs`, `SourceReferenceIndexStage.cs`, `SourceIndexRequirements.cs`, `IDesignTimeReferenceIndexRequirementsProvider.cs`; `Extensibility\PipelineExtension.cs` (`GetSourceIndexRequirements`); `ReferenceGraph\ReferenceIndexerOptions.cs` (merge per reference kind); `ReferenceGraph\InboundReferenceIndexBuilder.cs` (declaration roots); `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs` (project-local requirements) | new and modified (size M) | 330 |
| E | `Utilities\Roslyn\SymbolDictionaryKey.cs`, `Aspects\AspectBuilderState.cs`, `CodeModel\Helpers\AsyncHelper.cs`, FW27 `Advising\MethodTemplateSelector.cs`, FW27 `Code\OperatorKind.cs` (`NullCoalescingAssignment`) | modified | 35 |
| E | FW27 `Advising\PullAction.cs`, `Advising\PullActionKind.cs` (public kind and parameter, E3) | modified | 30 |
| Y | FW27 `Aspects\AnyAwaitable.cs`, `Aspects\AnyAwaitableMethodBuilder.cs` (new); ENG27 `CompileTime\SymbolClassifier.cs`, `Templating\TemplatingCodeValidator.Visitor.cs`, `Templating\TemplatingDiagnosticDescriptors.cs` (LAMA0298), `Advising\TemplateBindingHelper.cs`, `Templating\TemplateExpansionContext.ConfigureAwaitUserExpression.cs` | new and modified | 260 |
| T | `Extensibility\Transformations\RedirectedArgument.cs` (new), the argument lists of `InvocationRedirectionRequest` and `MethodReferenceRedirectionRequest`, and the temporaries of reordered arguments in `CallSiteRewriter.cs` | new and modified | 250 |
| Docs | DOCS27 `extensibility.md`, `linker-callsite.md`, new `extension-transformations.md` (first in `docs/future/`), `pipeline.md`, `design-time-memory.md`, `testing.md`; ENG27 `Diagnostics\Ranges.md` | new and modified | 550 |

Total: about 6,150 lines of production code and documentation in the open-source repository, plus about 7,700 lines of tests, baselines and test-only proof-of-concept code. The accessor sites of the third product-owner batch add about 400 lines of production code (the accessor redirection, the accessor proceed bindings, the compound rewrites and their visitors, and `OperatorKind.NullCoalescingAssignment`) and about 900 lines of tests. The injection-time rewrite (RC39) removes about 900 lines of linker code and about 400 lines of linker tests compared with the annotation design. The meta extensions, the source-expression factory and the SDK accessor add about 210 lines and 200 lines of tests. `RedirectMethodReference` and the method-group rewrite (RC44) add about 250 lines and 400 lines of tests. The proof of concept of section [12.5](12-test-plan.md#125-in-repository-proof-of-concept-of-interceptors) (RC45) adds about 1,300 lines of test-only code and about 1,400 lines of tests and baselines beyond the earlier test-only extension. The sixth product-owner batch adds about 540 lines of production code: `AnyAwaitable` and its template-compiler rules, the public kind and parameter of `PullAction`, and the argument lists of redirections, with about 700 lines of tests.
