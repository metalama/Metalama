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
public sealed partial class DesignTimeAspectPipelineResult : ITransitiveAspectsManifest
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

    private byte[]? _serializedTransitiveAspectManifest;

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
            $"CompilationPipelineResult {this._id} created with {this.SyntaxTreeResults.Count} syntax trees and {this._invalidSyntaxTreeResults.Count} introduced syntax trees." );

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

                foreach ( var validator in oldSyntaxTreeResult.Extensions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): removing validator `{validator}` from syntax tree '{filePath}'." );

                    extensionsBuilder.Remove( validator );
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
                    Logger.DesignTime.Trace?.Log( $"CompilationPipelineResult.Update( id = {this._id} ): adding validator `{extension}` to '{filePath}'." );
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
    private static (IEnumerable<SyntaxTreePipelineResult> Results, IReadOnlyList<IDesignTimeAspectPipelineResultExtension>? ExternalValidators)
        SplitResultsByTree(
            PartialCompilation compilation,
            DesignTimePipelineExecutionResult pipelineResults )
    {
        SyntaxTreePipelineResult.Builder? emptySyntaxTreeResult = null;

        var resultBuilders = pipelineResults
            .InputSyntaxTrees
            .ToDictionary( r => r.Key, syntaxTree => new SyntaxTreePipelineResult.Builder( syntaxTree.Value ) );

        List<IDesignTimeAspectPipelineResultExtension>? externalValidators = null;

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
            var syntaxTree = inheritableAspectInstance.TargetDeclaration.GetPrimarySyntaxTree();

            if ( syntaxTree == null )
            {
                continue;
            }

            var filePath = syntaxTree.FilePath;
            var builder = resultBuilders[filePath];
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
                    builder.Extensions ??= ImmutableArray.CreateBuilder<IDesignTimeAspectPipelineResultExtension>();
                    builder.Extensions.Add( designTimeExtension );
                }
                else
                {
                    // This happens with cross-project validators
                    externalValidators ??= new List<IDesignTimeAspectPipelineResultExtension>();
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
            var syntaxTree = aspectInstance.TargetDeclaration.GetPrimarySyntaxTree();

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
            var builder = resultBuilders[filePath];
            builder.AspectInstances ??= ImmutableArray.CreateBuilder<DesignTimeAspectInstance>();

            builder.AspectInstances.Add(
                new DesignTimeAspectInstance(
                    targetDeclarationId,
                    predecessorDeclarationId,
                    aspectInstance.AspectClass.FullName,
                    aspectInstance.IsSkipped ) );
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

            var syntaxTree = annotationsOnDeclaration.Key.GetPrimarySyntaxTree();

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

    // At design time, cross-project reference validators are not added to the main pipeline. Instead, the validator provider recursively includes
    // the providers of referenced projects. However cross-project references are still used for PE references.
    ImmutableArray<ITransitiveAspectsManifestExtension> ITransitiveAspectsManifest.Extensions => ImmutableArray<ITransitiveAspectsManifestExtension>.Empty;

    internal byte[] GetSerializedTransitiveAspectManifest( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        if ( this._serializedTransitiveAspectManifest == null )
        {
            var manifest = TransitiveAspectsManifest.Create(
                this._inheritableAspects.SelectMany( g => g ).ToImmutableArray(),
                this.Extensions.ToTransitiveValidatorInstances(),
                this.InheritableOptions,
                this.Annotations );

            this._serializedTransitiveAspectManifest = manifest.ToBytes( serviceProvider, compilationContext );
        }

        return this._serializedTransitiveAspectManifest;
    }
}