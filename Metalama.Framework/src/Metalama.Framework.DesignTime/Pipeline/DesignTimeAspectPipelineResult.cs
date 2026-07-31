// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Caches the pipeline results for each syntax tree.
/// </summary>
public sealed partial class DesignTimeAspectPipelineResult
{
    private static readonly ImmutableDictionary<string, SyntaxTreePipelineResult> _emptySyntaxTreeResults =
        ImmutableDictionary.Create<string, SyntaxTreePipelineResult>( StringComparer.Ordinal );

    private static readonly ImmutableDictionary<string, IntroducedSyntaxTree> _emptyIntroducedSyntaxTrees =
        ImmutableDictionary.Create<string, IntroducedSyntaxTree>( StringComparer.Ordinal );

    private static readonly ImmutableDictionaryOfHashSet<string, InheritableAspectInstance> _emptyInheritableAspects =
        ImmutableDictionaryOfHashSet<string, InheritableAspectInstance>.Create(
            StringComparer.Ordinal,
            InheritableAspectInstance.ByTargetComparer.Instance );

    private static readonly ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> _emptyInheritableOptions
        = ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>.Empty;

    private static readonly ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> _emptyAnnotations =
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Empty;

    private static long _nextId;
    private readonly long _id = Interlocked.Increment( ref _nextId );

    private bool IsEmpty => this.SyntaxTreeResults.IsEmpty && this.IntroducedSyntaxTrees.IsEmpty && this.Extensions.IsEmpty && this._inheritableAspects.IsEmpty;

    public DesignTimeAspectPipelineResultExtensionCollection Extensions { get; } = DesignTimeAspectPipelineResultExtensionCollection.Empty;

    internal ImmutableDictionary<string, IntroducedSyntaxTree> IntroducedSyntaxTrees { get; } = _emptyIntroducedSyntaxTrees;

    /// <summary>
    /// Gets a maps if the syntax tree name to the pipeline result for this syntax tree.
    /// </summary>
    internal ImmutableDictionary<string, SyntaxTreePipelineResult> SyntaxTreeResults { get; } = _emptySyntaxTreeResults;

    /// <summary>
    /// List of SyntaxTreeResult that have been invalidated.
    /// </summary>
    private readonly ImmutableDictionary<string, SyntaxTreePipelineResult> _invalidSyntaxTreeResults = _emptySyntaxTreeResults;

    private readonly ImmutableDictionaryOfHashSet<string, InheritableAspectInstance> _inheritableAspects = _emptyInheritableAspects;

    public ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> InheritableOptions { get; } = _emptyInheritableOptions;

    public ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> Annotations { get; } = _emptyAnnotations;

    internal ulong AspectInstancesHashCode { get; }

    private DesignTimeAspectPipelineResult(
        AspectPipelineConfiguration? configuration,
        ImmutableDictionary<string, SyntaxTreePipelineResult> syntaxTreeResults,
        ImmutableDictionary<string, SyntaxTreePipelineResult> invalidSyntaxTreeResults,
        ImmutableDictionary<string, IntroducedSyntaxTree> introducedSyntaxTrees,
        ImmutableDictionaryOfHashSet<string, InheritableAspectInstance> inheritableAspects,
        DesignTimeAspectPipelineResultExtensionCollection extensions,
        ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> inheritableOptions,
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> annotations,
        ulong aspectInstancesHashCode )
    {
        this.SyntaxTreeResults = syntaxTreeResults;
        this._invalidSyntaxTreeResults = invalidSyntaxTreeResults;
        this.IntroducedSyntaxTrees = introducedSyntaxTrees;
        this._inheritableAspects = inheritableAspects;
        this.InheritableOptions = inheritableOptions;
        this.Extensions = extensions;
        this.Configuration = configuration;
        this.Annotations = annotations;
        this.AspectInstancesHashCode = aspectInstancesHashCode;

        Logger.DesignTime.Trace?.Log(
            $"CompilationPipelineResult {this._id} created with {this.SyntaxTreeResults.Count} syntax trees and {this.IntroducedSyntaxTrees.Count} introduced syntax trees." );

        if ( !this.IsEmpty && configuration == null )
        {
            throw new AssertionFailedException();
        }
    }

    internal DesignTimeAspectPipelineResult() { }

    /// <summary>
    /// Gets the pipeline configuration, or potentially <c>null</c>  if the current <see cref="DesignTimeAspectPipelineResult"/> is empty.
    /// </summary>
    internal AspectPipelineConfiguration? Configuration { get; }

    /// <summary>
    /// Updates cache with a <see cref="DesignTimePipelineExecutionResult"/> that includes results for several syntax trees.
    /// </summary>
    internal DesignTimeAspectPipelineResult Update(
        PartialCompilation compilation,
        DesignTimeProjectVersion projectVersion,
        DesignTimePipelineExecutionResult pipelineResults,
        AspectPipelineConfiguration configuration )
    {
        Logger.DesignTime.Trace?.Log( $"CompilationPipelineResult.Update( id = {this._id} )" );

        var (resultsByTree, externalExtensions) = SplitResultsByTree( compilation, pipelineResults );

        var syntaxTreeResultBuilder = this.SyntaxTreeResults.ToBuilder();

        ImmutableDictionary<string, IntroducedSyntaxTree>.Builder? introducedSyntaxTreeBuilder = null;
        ImmutableDictionaryOfHashSet<string, InheritableAspectInstance>.Builder? inheritableAspectsBuilder = null;
        DesignTimeAspectPipelineResultExtensionCollection.Builder? extensionsBuilder = null;
        ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>.Builder? inheritableOptionsBuilder = null;
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Builder? annotationsBuilder = null;
        var aspectInstancesHashCode = this.AspectInstancesHashCode;

        foreach ( var (filePath, oldResult) in this._invalidSyntaxTreeResults )
        {
            UnindexOldTree( filePath, oldResult );
        }

        foreach ( var result in resultsByTree )
        {
            var filePath = result.SyntaxTreePath ?? "";

            // Un-index the old tree.
            if ( syntaxTreeResultBuilder.TryGetValue( filePath, out var oldSyntaxTreeResult ) )
            {
                UnindexOldTree( filePath, oldSyntaxTreeResult );
            }

            // Index the new tree.
            IndexNewTree( filePath, result );

            syntaxTreeResultBuilder[filePath] = result;
        }

        void UnindexOldTree( string filePath, SyntaxTreePipelineResult oldSyntaxTreeResult )
        {
            if ( !oldSyntaxTreeResult.Introductions.IsEmpty )
            {
                introducedSyntaxTreeBuilder ??= this.IntroducedSyntaxTrees.ToBuilder();

                foreach ( var introducedTree in oldSyntaxTreeResult.Introductions )
                {
                    Logger.DesignTime.Trace?.Log( $"CompilationPipelineResult.Update( id = {this._id} ): removing introduced tree '{introducedTree.Name}'." );

                    introducedSyntaxTreeBuilder.Remove( introducedTree.Name );
                }
            }

            if ( !oldSyntaxTreeResult.InheritableAspects.IsEmpty )
            {
                inheritableAspectsBuilder ??= this._inheritableAspects.ToBuilder();

                foreach ( var x in oldSyntaxTreeResult.InheritableAspects )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): removing inheritable aspect of type '{x.AspectClass.ShortName}'." );

                    inheritableAspectsBuilder.Remove( x.AspectClass.FullName, x );
                }
            }

            if ( !oldSyntaxTreeResult.Extensions.IsEmpty )
            {
                extensionsBuilder ??= this.Extensions.ToBuilder();

                foreach ( var extension in oldSyntaxTreeResult.Extensions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): removing extension `{extension}` from syntax tree '{filePath}'." );

                    extensionsBuilder.Remove( extension );
                }
            }

            if ( !oldSyntaxTreeResult.InheritableOptions.IsDefault )
            {
                inheritableOptionsBuilder ??= this.InheritableOptions.ToBuilder();

                foreach ( var optionItem in oldSyntaxTreeResult.InheritableOptions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): removing inheritable option of type `{optionItem.Key.OptionType}` on `{optionItem.Key.DeclarationId}` from syntax tree '{filePath}'." );

                    inheritableOptionsBuilder.Remove( optionItem.Key );
                }
            }

            if ( !oldSyntaxTreeResult.Annotations.IsEmpty )
            {
                annotationsBuilder ??= this.Annotations.ToBuilder();

                foreach ( var annotation in oldSyntaxTreeResult.Annotations )
                {
                    annotationsBuilder.Remove( annotation.Key, annotation );
                }
            }

            aspectInstancesHashCode ^= oldSyntaxTreeResult.AspectInstancesHashCode;
        }

        void IndexNewTree( string filePath, SyntaxTreePipelineResult newSyntaxTreeResult )
        {
            if ( !newSyntaxTreeResult.Introductions.IsEmpty )
            {
                introducedSyntaxTreeBuilder ??= this.IntroducedSyntaxTrees.ToBuilder();

                foreach ( var introducedTree in newSyntaxTreeResult.Introductions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): adding introduced syntax tree '{introducedTree.Name}'." );

                    if ( !introducedSyntaxTreeBuilder.TryAdd( introducedTree.Name, introducedTree ) )
                    {
                        // This can happen when the introduced syntax tree name is not deterministic.
                        throw new AssertionFailedException(
                            $"CompilationPipelineResult.Update( id = {this._id} ): Attempting to add duplicate syntax tree '{introducedTree.Name}'." );
                    }
                }
            }

            if ( !newSyntaxTreeResult.InheritableAspects.IsEmpty )
            {
                inheritableAspectsBuilder ??= this._inheritableAspects.ToBuilder();

                foreach ( var x in newSyntaxTreeResult.InheritableAspects )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): adding inheritable aspect of type '{x.AspectClass.ShortName}'." );

                    inheritableAspectsBuilder.Add( x.AspectClass.FullName, x );
                }
            }

            if ( !newSyntaxTreeResult.Extensions.IsDefaultOrEmpty )
            {
                extensionsBuilder ??= this.Extensions.ToBuilder();

                foreach ( var extension in newSyntaxTreeResult.Extensions )
                {
                    Logger.DesignTime.Trace?.Log( $"CompilationPipelineResult.Update( id = {this._id} ): adding extension `{extension}` to '{filePath}'." );
                    extensionsBuilder.Add( extension );
                }
            }

            if ( !newSyntaxTreeResult.InheritableOptions.IsDefaultOrEmpty )
            {
                inheritableOptionsBuilder ??= this.InheritableOptions.ToBuilder();

                foreach ( var optionItem in newSyntaxTreeResult.InheritableOptions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): adding inheritable options of type `{optionItem.Key.OptionType}`." );

                    if ( !inheritableOptionsBuilder.TryAdd( optionItem.Key, optionItem.Options ) )
                    {
                        // This seems theoretically possible, but reproducing it was not successful.
                        throw new AssertionFailedException(
                            $"Attempting to add duplicate inheritable options of type " +
                            $"'{optionItem.Key.OptionType}' on '{optionItem.Key.DeclarationId}' in '{optionItem.Key.SyntaxTreePath}'." );
                    }
                }
            }

            if ( !newSyntaxTreeResult.Annotations.IsEmpty )
            {
                annotationsBuilder ??= this.Annotations.ToBuilder();

                foreach ( var annotationGroup in newSyntaxTreeResult.Annotations )
                {
                    annotationsBuilder.Add( annotationGroup.Key, annotationGroup );
                }
            }

            aspectInstancesHashCode ^= newSyntaxTreeResult.AspectInstancesHashCode;
        }

        // Make immutable and return.
        var introducedTrees = introducedSyntaxTreeBuilder?.ToImmutable() ?? this.IntroducedSyntaxTrees;
        var inheritableAspects = inheritableAspectsBuilder?.ToImmutable() ?? this._inheritableAspects;

        if ( externalExtensions != null )
        {
            extensionsBuilder ??= this.Extensions.ToBuilder();

            foreach ( var externalExtension in externalExtensions )
            {
                extensionsBuilder.Add( externalExtension );
            }
        }

        var extensions = extensionsBuilder?.ToImmutable( projectVersion.ReferencedExtensions )
                         ?? this.Extensions.WithChildCollections( projectVersion.ReferencedExtensions );

        var inheritableOptions = inheritableOptionsBuilder?.ToImmutable() ?? this.InheritableOptions;
        var annotations = annotationsBuilder?.ToImmutable() ?? this.Annotations;

        return new DesignTimeAspectPipelineResult(
            configuration,
            syntaxTreeResultBuilder.ToImmutable(),
            ImmutableDictionary<string, SyntaxTreePipelineResult>.Empty,
            introducedTrees,
            inheritableAspects,
            extensions,
            inheritableOptions,
            annotations,
            aspectInstancesHashCode );
    }

    /// <summary>
    /// Splits a <see cref="DesignTimePipelineExecutionResult"/>, which includes data for several syntax trees, into
    /// a list of <see cref="SyntaxTreePipelineResult"/> which each have information related to a single syntax tree.
    /// </summary>
    private static (IEnumerable<SyntaxTreePipelineResult> Results, IReadOnlyList<IDesignTimePipelineResultExtension>? ExternalValidators)
        SplitResultsByTree(
            PartialCompilation compilation,
            DesignTimePipelineExecutionResult pipelineResults )
    {
        SyntaxTreePipelineResult.Builder? emptySyntaxTreeResult = null;

        var resultBuilders = pipelineResults
            .InputSyntaxTrees
            .ToDictionary( r => r.Key, syntaxTree => new SyntaxTreePipelineResult.Builder( syntaxTree.Value ) );

        List<IDesignTimePipelineResultExtension>? externalValidators = null;

        // Split diagnostic by syntax tree.
        foreach ( var diagnostic in pipelineResults.Diagnostics.ReportedDiagnostics )
        {
            SyntaxTreePipelineResult.Builder? builder;

            // GetLineSpan() works even for "external" locations (i.e. not tree-based), which we use for exceptions.
            if ( diagnostic.Location.GetLineSpan().Path is { } filePath )
            {
                if ( !resultBuilders.TryGetValue( filePath, out builder ) )
                {
                    // This can happen when a CS error is reported in the aspect. These errors can be ignored.
                    continue;
                }
            }
            else
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }

            builder.Diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>();
            builder.Diagnostics.Add( diagnostic );
        }

        // Split suppressions by syntax tree.
        foreach ( var suppression in pipelineResults.Diagnostics.DiagnosticSuppressions )
        {
            void AddSuppression( string? path )
            {
                if ( !string.IsNullOrEmpty( path ) )
                {
                    if ( resultBuilders.TryGetValue( path, out var builder ) )
                    {
                        builder.Suppressions ??= ImmutableArray.CreateBuilder<CacheableScopedSuppression>();
                        builder.Suppressions.Add( new CacheableScopedSuppression( suppression ) );
                    }
                    else
                    {
                        // This can happen when a suppression is applied to an aspect that is in a different compilation, e.g. with [IntroduceDependency].
                    }
                }
            }

            var declaringSyntaxes = suppression.ScopeSymbol.DeclaringSyntaxReferences;

            switch ( declaringSyntaxes.Length )
            {
                case 0:
                    continue;

                case 1:
                    AddSuppression( declaringSyntaxes[0].SyntaxTree.FilePath );

                    break;

                default:
                    foreach ( var filePath in declaringSyntaxes.Select( p => p.SyntaxTree.FilePath ).Distinct() )
                    {
                        AddSuppression( filePath );
                    }

                    break;
            }
        }

        // Split introductions by original syntax tree.
        foreach ( var introduction in pipelineResults.IntroducedSyntaxTrees )
        {
            SyntaxTreePipelineResult.Builder? builder;

            if ( introduction.SourceSyntaxTree is { } syntaxTree )
            {
                var filePath = syntaxTree.FilePath;

                if ( !resultBuilders.TryGetValue( filePath, out builder ) )
                {
                    // This happens when the source tree is not dirty, so it's not part of the PartialCompilation.
                    builder = resultBuilders[filePath] = new SyntaxTreePipelineResult.Builder( syntaxTree );
                }
            }
            else
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }

            builder.Introductions ??= ImmutableArray.CreateBuilder<IntroducedSyntaxTree>();

            builder.Introductions.Add(
                introduction.SourceSyntaxTree == null ? new IntroducedSyntaxTree( introduction.Name, null, introduction.GeneratedSyntaxTree ) : introduction );
        }

        var compilationContext = compilation.CompilationContext;

        // Split inheritable aspects by syntax tree.
        foreach ( var inheritableAspectInstance in pipelineResults.InheritableAspects )
        {
            var syntaxTree = inheritableAspectInstance.TargetDeclaration.GetPrimarySyntaxTree( compilationContext );

            if ( syntaxTree == null )
            {
                continue;
            }

            var filePath = syntaxTree.FilePath;

            if ( !resultBuilders.TryGetValue( filePath, out var builder ) )
            {
                // An inheritable aspect instance is not bound to the tree the pipeline ran on, because an aspect can add one to
                // a declaration it did not itself target, e.g. through RequireAspect or through the transitive instance exported
                // by PullStrategy.IntroduceParameterAndPull. The target declaration can therefore be in a tree that is not dirty,
                // hence not a part of the PartialCompilation. The instance is skipped instead of being filed under that tree,
                // because the tree keeps the result of the run that did include it, and overwriting that result would drop the
                // diagnostics and introductions it holds.
                Logger.DesignTime.Trace?.Log(
                    $"SplitResultsByTree: skipping the inheritable aspect of type '{inheritableAspectInstance.AspectClass.ShortName}' because its target "
                    + $"declaration is in syntax tree '{filePath}', which is not a part of the partial compilation." );

                continue;
            }

            builder.InheritableAspects ??= ImmutableArray.CreateBuilder<InheritableAspectInstance>();
            builder.InheritableAspects.Add( inheritableAspectInstance );
        }

        // Split extensions by syntax tree.
        foreach ( var extension in pipelineResults.Extensions )
        {
            var syntaxTree = extension.SyntaxTree;

            if ( syntaxTree == null && !resultBuilders.ContainsKey( string.Empty ) )
            {
                resultBuilders.Add( string.Empty, new SyntaxTreePipelineResult.Builder( null ) );
            }

            var designTimeExtension = extension.ToDesignTime();

            if ( designTimeExtension != null )
            {
                /*
                new DesignTimeReferenceValidatorInstance(
                validatedDeclarationSymbol,
                extension.Properties.ReferenceKinds,
                extension.Properties.IncludeDerivedTypes,
                extension.Driver,
                extension.Implementation,
                extension.DiagnosticSourceDescription,
                extension.Granularity,
                compilation.CompilationContext ); */

                var filePath = syntaxTree?.FilePath ?? string.Empty;

                if ( resultBuilders.TryGetValue( filePath, out var builder ) )
                {
                    builder.Extensions ??= ImmutableArray.CreateBuilder<IDesignTimePipelineResultExtension>();
                    builder.Extensions.Add( designTimeExtension );
                }
                else
                {
                    // This happens with cross-project validators
                    externalValidators ??= new List<IDesignTimePipelineResultExtension>();
                    externalValidators.Add( designTimeExtension );
                }
            }
            else
            {
                // TODO: validating a declaration that is not backed by a symbol is not supported at design time at the moment.
            }
        }

        // Split aspect instances by syntax tree.
        foreach ( var aspectInstance in pipelineResults.AspectInstances )
        {
            var syntaxTree = aspectInstance.TargetDeclaration.GetPrimarySyntaxTree( compilationContext );

            // No continue here to handle even aspect instances without a syntax tree.
            if ( syntaxTree == null && !resultBuilders.ContainsKey( string.Empty ) )
            {
                resultBuilders.Add( string.Empty, new SyntaxTreePipelineResult.Builder( null ) );
            }

            var targetDeclarationId = aspectInstance.TargetDeclaration.ToSerializableId();
            SerializableDeclarationId? predecessorDeclarationId = null;

            if ( aspectInstance.Predecessors is [var predecessor, ..] )
            {
                var reflectionMapper = ((ICompilationServices) compilationContext).ReflectionMapper;

                var predecessorDeclarationSymbol = predecessor.Instance switch
                {
                    IAspectInstance predecessorAspect => reflectionMapper.GetTypeSymbol( predecessorAspect.Aspect.GetType() ),

                    // Can't use fabricInstance.Fabric.GetType() here, because for type fabrics,
                    // we need the original type (e.g. C.Fabric), not the rewritten type (e.g. C_Fabric).
                    IFabricInstance fabricInstance => compilationContext.Compilation.GetTypeByMetadataName(
                        ((IFabricInstanceInternal) fabricInstance).FabricTypeFullName ),
                    _ => null
                };

                predecessorDeclarationId = predecessorDeclarationSymbol?.GetSerializableId();
            }

            var filePath = syntaxTree?.FilePath ?? string.Empty;

            if ( resultBuilders.TryGetValue( filePath, out var builder ) )
            {
                builder.AspectInstances ??= ImmutableArray.CreateBuilder<DesignTimeAspectInstance>();

                builder.AspectInstances.Add(
                    new DesignTimeAspectInstance(
                        targetDeclarationId,
                        predecessorDeclarationId,
                        aspectInstance.AspectClass.FullName,
                        aspectInstance.IsSkipped ) );
            }
            else
            {
                // This is a transitive aspect. 
                // TODO: integrate transitive aspects with the aspect explorer.
            }
        }

        // Split transformations by syntax tree.
        foreach ( var transformation in pipelineResults.Transformations )
        {
            var filePath = (transformation as ISyntaxTreeTransformationBase)?.TransformedSyntaxTree.FilePath;

            if ( filePath == null || !resultBuilders.TryGetValue( filePath, out var builder ) )
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }

            builder.Transformations ??= ImmutableArray.CreateBuilder<DesignTimeTransformation>();

            var formattable = transformation.ToDisplayString();

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            var description = formattable != null ? MetalamaStringFormatter.Format( formattable ) : transformation.ToString()!;

            builder.Transformations.Add(
                new DesignTimeTransformation(
                    transformation.TargetDeclaration.ToSerializableId(),
                    transformation.AspectClass.FullName,
                    description ) );
        }

        // Split options by syntax tree.
        foreach ( var optionItem in pipelineResults.InheritableOptions )
        {
            SyntaxTreePipelineResult.Builder builder;
            var syntaxTreePath = optionItem.Key.SyntaxTreePath;

            if ( syntaxTreePath != null )
            {
                builder = resultBuilders[syntaxTreePath];
            }
            else
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }

            builder.InheritableOptions ??= ImmutableArray.CreateBuilder<InheritableOptionsInstance>();
            builder.InheritableOptions.Add( new InheritableOptionsInstance( optionItem.Key, optionItem.Value ) );
        }

        // Split annotations by syntax tree.
        foreach ( var annotationsOnDeclaration in pipelineResults.Annotations )
        {
            // Annotations in AspectPipelineResults are only used for the cross-project scenario, so we only index exported annotations.
            var exportedAnnotations = annotationsOnDeclaration
                .Where( x => x.Export )
                .Select( x => x.Annotation )
                .ToImmutableArray();

            if ( exportedAnnotations.IsEmpty )
            {
                continue;
            }

            var syntaxTree = annotationsOnDeclaration.Key.GetPrimarySyntaxTree( compilationContext );

            SyntaxTreePipelineResult.Builder builder;

            if ( syntaxTree == null )
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }
            else
            {
                var filePath = syntaxTree.FilePath;
                builder = resultBuilders[filePath];
            }

            builder.Annotations ??= ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.CreateBuilder();
            builder.Annotations.Add( annotationsOnDeclaration.Key.ToSerializableId(), exportedAnnotations );
        }

        // Add syntax trees with empty output so they get cached too.
        var inputTreesWithoutOutput = compilation.SyntaxTrees.ToBuilder();

        foreach ( var path in resultBuilders.Keys )
        {
            inputTreesWithoutOutput.Remove( path );
        }

        foreach ( var empty in inputTreesWithoutOutput )
        {
            resultBuilders.Add( empty.Key, new SyntaxTreePipelineResult.Builder( empty.Value ) );
        }

        if ( emptySyntaxTreeResult != null )
        {
            resultBuilders[""] = emptySyntaxTreeResult;
        }

        return (resultBuilders.SelectAsReadOnlyCollection( b => b.Value.ToImmutable( compilation.Compilation ) ), externalValidators);
    }

    internal Invalidator ToInvalidator() => new( this );

    internal bool IsSyntaxTreeDirty( SyntaxTree syntaxTree ) => !this.SyntaxTreeResults.ContainsKey( syntaxTree.FilePath );

    public IEnumerable<string> InheritableAspectTypes => this._inheritableAspects.Keys;

    public IEnumerable<InheritableAspectInstance> GetInheritableAspects( string aspectType ) => this._inheritableAspects[aspectType];

    /// <summary>
    /// Gets a value indicating whether this project exports something a referencing project could inherit: an
    /// inheritable aspect, an inheritable option, an exported annotation, or a transitive validator (carried by
    /// <see cref="Extensions"/>). When it is <c>false</c> there is nothing to serialize and nothing for a consumer
    /// to merge, so the manifest is not produced at all. See <see cref="SerializedTransitiveAspectManifestWithoutValidators"/> and
    /// the reference-construction site in <c>DesignTimeAspectPipeline</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Extensions"/> is the design-time collection, a superset of the transitive validators; testing it
    /// (rather than the narrower transitive projection) keeps this conservative and keeps
    /// <c>DesignTimeProjectVersion.ReferencedExtensions</c> correct: whenever the live manifest is dropped, its
    /// <see cref="Extensions"/> collection is necessarily empty here.
    /// </remarks>
    internal bool HasTransitiveAspectManifestContent
        => !this._inheritableAspects.IsEmpty
           || !this.InheritableOptions.IsEmpty
           || !this.Annotations.IsEmpty
           || !this.Extensions.IsEmpty;

    /// <summary>
    /// Creates the transitive manifest. <paramref name="includeValidators"/> selects whether the design-time
    /// validators are included in the manifest, and must match whether the consumer has the other channel that carries
    /// them, <see cref="DesignTimeProjectVersion.ReferencedExtensions"/>:
    /// <list type="bullet">
    /// <item>
    /// <c>false</c> for a consumer built against the same version of Metalama. Its reference carries this very result as its live
    /// <c>TransitiveAspectsManifest</c>, so <c>ReferencedExtensions</c> reads the validators out of the design-time
    /// extension collection, which deduplicates diamond-shaped reference graphs. Putting them in the manifest as
    /// well would deliver each validator twice (once per channel), and more than twice across a diamond, because
    /// the manifest channel is walked once per direct reference and is not deduplicated.
    /// </item>
    /// <item>
    /// <c>true</c> for a consumer built against a different version of Metalama. Such a reference carries no live
    /// result (the producer's result is an object of the other version's <c>Metalama.Framework.Engine</c> and cannot
    /// cross), so <c>ReferencedExtensions</c> contributes nothing for it and the manifest is the only channel
    /// available. See <see cref="SerializedTransitiveAspectManifestWithValidators"/>.
    /// </item>
    /// </list>
    /// </summary>
    private TransitiveAspectsManifest CreateTransitiveManifest( bool includeValidators )
        =>

            // ContainsInitializableTypes is set to the safe default `true`: DesignTimeAspectPipelineResult does not
            // track the flag (the tracking would be useless at design time, where LinkerAnalysisStep force-runs the
            // OnInitialized walker because the partial compilation may exclude trees declaring implementers).
            // LinkerAnalysisStep consumes the flag to skip that walker, so `true` only makes a consumer run it
            // unnecessarily, a performance pessimization at worst. `false` would be unsafe: a consumer reading it at
            // compile time could miss required WithInitialize wrapping.
            TransitiveAspectsManifest.Create(
                this._inheritableAspects.SelectMany( g => g ).ToImmutableArray(),
                this.Extensions.ToTransitiveValidatorInstances( includeValidators ),
                this.InheritableOptions,
                this.Annotations,
                containsInitializableTypes: true );

    /// <summary>
    /// Gets the transitive manifest serialized for the design-time pipeline of a referencing project running the
    /// same version of Metalama in this same process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A manifest is serialized in exactly three places, and they differ only in which boundary the bytes have to
    /// cross. <see cref="SerializedTransitiveAspectManifestWithValidators"/> crosses a Metalama version.
    /// <c>TransitiveAspectsManifest.ToResource</c> crosses a process and an arbitrary amount of time, being embedded
    /// in the PE binary and read by whichever version later compiles against that assembly. This one crosses
    /// neither: it is produced and consumed by this build, in this process, and then discarded.
    /// </para>
    /// <para>
    /// It exists because a boundary remains even so. The consuming project has to bind the manifest to <em>its own</em>
    /// compile-time copy of each type, and the round-trip through run-time type names is what rebinds them (issue
    /// #1710). Two projects of the same Metalama version can hold different compile-time copies, typically when a
    /// shared assembly is multi-targeted, so what this crosses is a compile-time copy, not a version. When the copies
    /// do match, the consumer skips it and reuses <see cref="LiveTransitiveAspectManifest"/> instead.
    /// </para>
    /// <para>
    /// Uncompressed, as is <see cref="SerializedTransitiveAspectManifestWithValidators"/>: nothing that reads either
    /// of them predates the uncompressed marker. Only <c>TransitiveAspectsManifest.ToResource</c> still compresses,
    /// and for a reason that does not apply here, namely keeping the PE binary small.
    /// </para>
    /// <para>
    /// Serializes unconditionally. Whether it is worth serializing at all is the caller's question, answered by
    /// <see cref="HasTransitiveAspectManifestContent"/>: a project exporting nothing to inherit, which is the common
    /// case, carries no manifest on its reference, so nothing is deserialized or merged on the other side either.
    /// </para>
    /// </remarks>
    [Memo]
    internal SerializedTransitiveAspectManifest SerializedTransitiveAspectManifestWithoutValidators
        => SerializedTransitiveAspectManifest.Create(
            this.LiveTransitiveAspectManifest
                .ToImmutableBytes( this.Configuration.AssertNotNull().ServiceProvider, compress: false ) );

    /// <summary>
    /// Gets the transitive manifest serialized for a consumer built against a <em>different</em> version of Metalama:
    /// two projects of the same solution, one referencing the other, each referencing its own Metalama version.
    /// Both versions are loaded in the same process and reach each other through the version-neutral
    /// <c>Metalama.Framework.DesignTime.Contracts</c> assembly, but their <c>Metalama.Framework.Engine</c> types have
    /// distinct identities, so no manifest object can be passed across. Serializing here and deserializing on the
    /// other side is what converts the manifest into the consuming version's object model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uncompressed, like <see cref="SerializedTransitiveAspectManifestWithoutValidators"/>, even though the reader is a different
    /// build than the one that wrote these bytes. The uncompressed form is recognized by peeking a marker byte
    /// introduced in 2026.1 (issue #1710), so a reader older than that would fail on it, but no such reader can
    /// receive these bytes: a project only consumes this manifest if it has a project reference to this one, and a
    /// referencing project's Metalama version is never older than the referenced project's. Every possible reader is
    /// therefore at least this version.
    /// </para>
    /// <para>
    /// That bound applies to what we <em>write</em>, not to what we read. The converse case is ordinary: this
    /// project may reference an older one, whose manifest arrives in the legacy compressed form. That is why
    /// <c>TransitiveAspectsManifest.Deserialize</c> keeps accepting both formats even though nothing writes the
    /// compressed one any more except <c>ToResource</c>, which compresses to keep the PE binary small.
    /// </para>
    /// <para>
    /// Serializes unconditionally, as <see cref="SerializedTransitiveAspectManifestWithoutValidators"/> does, and for the same
    /// reason: whether there is anything worth sending is <see cref="HasTransitiveAspectManifestContent"/>, which
    /// belongs to the caller.
    /// </para>
    /// </remarks>
    [Memo]
    internal SerializedTransitiveAspectManifest SerializedTransitiveAspectManifestWithValidators
        => SerializedTransitiveAspectManifest.Create(
            this.CreateTransitiveManifest( includeValidators: true )
                .ToImmutableBytes( this.Configuration.AssertNotNull().ServiceProvider, compress: false ) );

    /// <summary>
    /// Gets the live, in-memory manifest. A same-version consumer whose compile-time
    /// copies match this producer's can consume this object directly and skip the serialize/deserialize round-trip
    /// (issue #1710 fast path); see <c>TransitivePipelineContributorSource</c>. 
    /// </summary>
    [Memo]
    internal TransitiveAspectsManifest LiveTransitiveAspectManifest => this.CreateTransitiveManifest( includeValidators: false );
}