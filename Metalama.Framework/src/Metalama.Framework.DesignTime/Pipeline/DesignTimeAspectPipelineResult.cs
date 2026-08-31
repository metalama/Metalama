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
using Metalama.Framework.Engine.Utilities.Roslyn;
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
    private static readonly ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult> _emptySyntaxTreeResults =
        ImmutableDictionary.Create<DocumentKey, SyntaxTreePipelineResult>();

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
    internal ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult> SyntaxTreeResults { get; } = _emptySyntaxTreeResults;

    /// <summary>
    /// List of SyntaxTreeResult that have been invalidated.
    /// </summary>
    private readonly ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult> _invalidSyntaxTreeResults = _emptySyntaxTreeResults;

    private readonly ImmutableDictionaryOfHashSet<string, InheritableAspectInstance> _inheritableAspects = _emptyInheritableAspects;

    /// <summary>
    /// The contributors of the last run that belong to a syntax tree of another compilation, typically the validators
    /// that a fabric of this project applies to declarations of a referenced project.
    /// </summary>
    /// <remarks>
    /// No <see cref="SyntaxTreePipelineResult"/> of this project is keyed by the path of such a tree, so these
    /// contributors cannot be indexed and un-indexed per tree like the others. They are kept here instead, and
    /// <see cref="Update"/> replaces the whole set on every run rather than adding to it. See issue #1796.
    /// </remarks>
    private readonly ImmutableArray<IDesignTimePipelineResultExtension> _foreignExtensions =
        ImmutableArray<IDesignTimePipelineResultExtension>.Empty;

    public ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> InheritableOptions { get; } = _emptyInheritableOptions;

    public ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> Annotations { get; } = _emptyAnnotations;

    internal ulong AspectInstancesHashCode { get; }

    private DesignTimeAspectPipelineResult(
        AspectPipelineConfiguration? configuration,
        ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult> syntaxTreeResults,
        ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult> invalidSyntaxTreeResults,
        ImmutableDictionary<string, IntroducedSyntaxTree> introducedSyntaxTrees,
        ImmutableDictionaryOfHashSet<string, InheritableAspectInstance> inheritableAspects,
        DesignTimeAspectPipelineResultExtensionCollection extensions,
        ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> inheritableOptions,
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> annotations,
        ulong aspectInstancesHashCode,
        ImmutableArray<IDesignTimePipelineResultExtension> foreignExtensions )
    {
        this._foreignExtensions = foreignExtensions;
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

        var (lazyResultsByTree, foreignExtensions) = SplitResultsByTree( compilation, pipelineResults );

        // Materialized because it is enumerated twice below and the projection allocates a new result on each
        // enumeration.
        var resultsByTree = lazyResultsByTree.ToReadOnlyList();

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

        // Every old result is un-indexed before any new one is indexed. Interleaving the two, which this loop used to
        // do, lets the un-indexing of one tree remove an entry that the indexing of another tree has already added,
        // whenever the two are keyed alike. That happens with the introduced syntax trees, which are keyed by a name
        // rendered from the target type while the results they belong to are keyed by a source path: when the primary
        // declaration of a partial type moves from one file to another, both files are in this batch, and processing
        // the new owner first made the old owner delete the entry the new one had just written. See issue #1742.
        foreach ( var result in resultsByTree )
        {
            var filePath = result.SyntaxTreePath;

            if ( syntaxTreeResultBuilder.TryGetValue( filePath, out var oldSyntaxTreeResult ) )
            {
                UnindexOldTree( filePath, oldSyntaxTreeResult );
            }
        }

        foreach ( var result in resultsByTree )
        {
            var filePath = result.SyntaxTreePath;

            IndexNewTree( filePath, result );

            syntaxTreeResultBuilder[filePath] = result;
        }

        void UnindexOldTree( DocumentKey filePath, SyntaxTreePipelineResult oldSyntaxTreeResult )
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

        void IndexNewTree( DocumentKey filePath, SyntaxTreePipelineResult newSyntaxTreeResult )
        {
            if ( !newSyntaxTreeResult.Introductions.IsEmpty )
            {
                introducedSyntaxTreeBuilder ??= this.IntroducedSyntaxTrees.ToBuilder();

                foreach ( var introducedTree in newSyntaxTreeResult.Introductions )
                {
                    Logger.DesignTime.Trace?.Log(
                        $"CompilationPipelineResult.Update( id = {this._id} ): adding introduced syntax tree '{introducedTree.Name}'." );

                    // The last one wins. This index is keyed by the introduced tree name, which
                    // DesignTimeSyntaxTreeGenerator.GetUniqueFilenameForType renders from the target type and never from
                    // a path, while the un-indexing pass above is keyed by the source path. The two keys are therefore
                    // independent, and the pass is not a transaction: when the primary declaration of a partial type
                    // moves from one file to another, this update un-indexes the new file only and the entry the old
                    // file left behind is still present. It names a tree of an earlier run, so the new one replaces it,
                    // and the stale result of the old file is corrected when that file is next analysed. See issue
                    // #1742.
                    if ( introducedSyntaxTreeBuilder.TryGetValue( introducedTree.Name, out var existingIntroducedTree )
                         && existingIntroducedTree.SourceDocumentKey != introducedTree.SourceDocumentKey )
                    {
                        Logger.DesignTime.Trace?.Log(
                            $"CompilationPipelineResult.Update( id = {this._id} ): the introduced syntax tree '{introducedTree.Name}' moves from "
                            + $"'{existingIntroducedTree.SourceDocumentKey}' to '{introducedTree.SourceDocumentKey}'." );
                    }

                    introducedSyntaxTreeBuilder[introducedTree.Name] = introducedTree;
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

        // Replace the contributors that belong to another compilation. They cannot be indexed per tree, because no
        // result of this project is keyed by the path of such a tree, so the set of the previous run is removed and
        // the set of this run is added.
        //
        // Only when this run produced some. A run that produced none has not established that there are none: the
        // design-time stage invokes the extensions only when the run yielded at least one contributor of extension
        // kind, so a validator source registered by an aspect contributes nothing on a run in which that aspect's file
        // is clean, and a run whose pipeline failed carries none either. Replacing unconditionally would then drop the
        // rules of a referenced project silently, which is the regression this whole branch of the method exists to
        // avoid. Keeping the previous set instead is symmetric with the per-tree results, which are likewise replaced
        // when their tree is re-analysed and kept otherwise.
        //
        // The residual is that a rule genuinely removed survives until the next run that produces any foreign
        // contributor. Removing a rule is an edit to compile-time code, which discards the pipeline configuration and
        // with it this whole result, so in practice that window does not outlive the edit. See issue #1796.
        if ( !foreignExtensions.IsEmpty )
        {
            extensionsBuilder ??= this.Extensions.ToBuilder();

            foreach ( var oldForeignExtension in this._foreignExtensions )
            {
                extensionsBuilder.Remove( oldForeignExtension );
            }

            foreach ( var newForeignExtension in foreignExtensions )
            {
                extensionsBuilder.Add( newForeignExtension );
            }
        }

        // Make immutable and return.
        var introducedTrees = introducedSyntaxTreeBuilder?.ToImmutable() ?? this.IntroducedSyntaxTrees;
        var inheritableAspects = inheritableAspectsBuilder?.ToImmutable() ?? this._inheritableAspects;

        var extensions = extensionsBuilder?.ToImmutable( projectVersion.ReferencedExtensions )
                         ?? this.Extensions.WithChildCollections( projectVersion.ReferencedExtensions );

        var inheritableOptions = inheritableOptionsBuilder?.ToImmutable() ?? this.InheritableOptions;
        var annotations = annotationsBuilder?.ToImmutable() ?? this.Annotations;

        return new DesignTimeAspectPipelineResult(
            configuration,
            syntaxTreeResultBuilder.ToImmutable(),
            ImmutableDictionary<DocumentKey, SyntaxTreePipelineResult>.Empty,
            introducedTrees,
            inheritableAspects,
            extensions,
            inheritableOptions,
            annotations,
            aspectInstancesHashCode,
            foreignExtensions.IsEmpty ? this._foreignExtensions : foreignExtensions );
    }

    /// <summary>
    /// Splits a <see cref="DesignTimePipelineExecutionResult"/>, which includes data for several syntax trees, into
    /// a list of <see cref="SyntaxTreePipelineResult"/> which each have information related to a single syntax tree.
    /// </summary>
    /// <param name="compilation">The partial compilation the pipeline ran on.</param>
    /// <param name="pipelineResults">The results to split.</param>
    /// <returns>
    /// The results for each syntax tree of <paramref name="compilation"/>, and the contributors that belong to a
    /// syntax tree of another compilation, which no result of this project can carry.
    /// </returns>
    private static (IEnumerable<SyntaxTreePipelineResult> Results, ImmutableArray<IDesignTimePipelineResultExtension> ForeignExtensions) SplitResultsByTree(
        PartialCompilation compilation,
        DesignTimePipelineExecutionResult pipelineResults )
    {
        SyntaxTreePipelineResult.Builder? emptySyntaxTreeResult = null;

        // Keyed by DocumentKey, with the default key holding the results that belong to the compilation rather than to
        // any document. The default key cannot collide with a document, because it wraps a null path while every key of
        // a document wraps SyntaxTree.FilePath, which Roslyn never returns as null. The empty path, which this method
        // used as its sentinel, has no such guarantee: a syntax tree may carry it.
        var resultBuilders = pipelineResults
            .InputSyntaxTrees
            .ToDictionary( syntaxTree => syntaxTree.GetDocumentKey(), syntaxTree => new SyntaxTreePipelineResult.Builder( syntaxTree ) );

        ImmutableArray<IDesignTimePipelineResultExtension>.Builder? foreignExtensions = null;

        // Split diagnostic by syntax tree.
        foreach ( var diagnostic in pipelineResults.Diagnostics.ReportedDiagnostics )
        {
            SyntaxTreePipelineResult.Builder? builder;

            // GetLineSpan() works even for "external" locations (i.e. not tree-based), which we use for exceptions.
            if ( diagnostic.Location.GetLineSpan().Path is { } filePath )
            {
                if ( !resultBuilders.TryGetValue( DocumentKey.FromPath( filePath ), out builder ) )
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
                    if ( resultBuilders.TryGetValue( DocumentKey.FromPath( path! ), out var builder ) )
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

            if ( !introduction.SourceDocumentKey.IsDefault )
            {
                var documentKey = introduction.SourceDocumentKey;

                if ( !resultBuilders.TryGetValue( documentKey, out builder ) )
                {
                    // This happens when the source tree is not dirty, so it's not part of the PartialCompilation. The
                    // tree is resolved against the compilation the builder will take a semantic model from, rather
                    // than carried here from the run that produced the introduction, so it cannot be a tree of an
                    // earlier compilation.
                    compilation.Compilation.GetIndexedSyntaxTrees().TryGetValue( documentKey, out var syntaxTree );

                    builder = resultBuilders[documentKey] = new SyntaxTreePipelineResult.Builder( syntaxTree );
                }
            }
            else
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }

            builder.Introductions ??= ImmutableArray.CreateBuilder<IntroducedSyntaxTree>();
            builder.Introductions.Add( introduction );
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

            var documentKey = syntaxTree.GetDocumentKey();

            if ( !resultBuilders.TryGetValue( documentKey, out var builder ) )
            {
                // An inheritable aspect instance is not bound to the tree the pipeline ran on, because an aspect can add one to
                // a declaration it did not itself target, e.g. through RequireAspect or through the transitive instance exported
                // by PullStrategy.IntroduceParameterAndPull. The target declaration can therefore be in a tree that is not dirty,
                // hence not a part of the PartialCompilation. The instance is skipped instead of being filed under that tree,
                // because the tree keeps the result of the run that did include it, and overwriting that result would drop the
                // diagnostics and introductions it holds.
                Logger.DesignTime.Trace?.Log(
                    $"SplitResultsByTree: skipping the inheritable aspect of type '{inheritableAspectInstance.AspectClass.ShortName}' because its target "
                    + $"declaration is in syntax tree '{documentKey}', which is not a part of the partial compilation." );

                continue;
            }

            builder.InheritableAspects ??= ImmutableArray.CreateBuilder<InheritableAspectInstance>();
            builder.InheritableAspects.Add( inheritableAspectInstance );
        }

        // Split extensions by syntax tree.
        foreach ( var extension in pipelineResults.Extensions )
        {
            var documentKey = extension.DocumentKey;

            if ( documentKey.IsDefault && !resultBuilders.ContainsKey( default ) )
            {
                resultBuilders.Add( default, new SyntaxTreePipelineResult.Builder( null ) );
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

                if ( resultBuilders.TryGetValue( documentKey, out var builder ) )
                {
                    builder.Extensions ??= ImmutableArray.CreateBuilder<IDesignTimePipelineResultExtension>();
                    builder.Extensions.Add( designTimeExtension );
                }
                else if ( !documentKey.IsDefault && compilation.Compilation.GetIndexedSyntaxTrees().ContainsKey( documentKey ) )
                {
                    // The document belongs to this project but is not dirty, so it is not part of the partial
                    // compilation.
                    // This is the same situation as for an inheritable aspect instance above: an aspect can export a
                    // contributor onto a declaration it did not itself target, such as the declaring type of a base
                    // constructor. The contributor is skipped rather than filed under that tree, because the tree
                    // keeps the result of the run that did include it and that result already carries this
                    // contributor. Filing it under the tree would overwrite that result and drop the diagnostics and
                    // introductions it holds. See issue #1768.
                    Logger.DesignTime.Trace?.Log(
                        $"SplitResultsByTree: skipping the transitive contributor of kind '{designTimeExtension.ContributorKind}' because it belongs "
                        + $"to syntax tree '{documentKey}' of this project, which is not a part of the partial compilation." );
                }
                else
                {
                    // The document belongs to another compilation, or the contributor reports none and the result keyed by
                    // the default DocumentKey was not created, which the branch above normally guarantees. The case
                    // that matters is the first: with cross-project validators the syntax tree a reference validator
                    // reports is that of the validated declaration, and a fabric of this project can validate
                    // references to declarations of a referenced project. No result of this project is keyed by that
                    // document, and none ever will be, so skipping the contributor would lose it and the rules it
                    // enforces would silently stop being applied.
                    //
                    // Such a contributor is therefore kept, in a collection that is replaced on every run rather than
                    // appended to. Replacing is correct because a run re-produces the complete set: the extension that
                    // creates reference validators runs every validator source over the whole compilation, not over
                    // the partial one. Appending, which is what this method used to do, gave the collection an entry
                    // per run that nothing could ever remove. See issue #1796.
                    foreignExtensions ??= ImmutableArray.CreateBuilder<IDesignTimePipelineResultExtension>();
                    foreignExtensions.Add( designTimeExtension );
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
            if ( syntaxTree == null && !resultBuilders.ContainsKey( default ) )
            {
                resultBuilders.Add( default, new SyntaxTreePipelineResult.Builder( null ) );
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

            var documentKey = syntaxTree?.GetDocumentKey() ?? default;

            if ( resultBuilders.TryGetValue( documentKey, out var builder ) )
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
            var documentKey = (transformation as ISyntaxTreeTransformationBase)?.TransformedSyntaxTree.GetDocumentKey();

            if ( documentKey == null || !resultBuilders.TryGetValue( documentKey.Value, out var builder ) )
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
            SyntaxTreePipelineResult.Builder? builder;
            var syntaxTreePath = optionItem.Key.SyntaxTreePath;

            if ( syntaxTreePath != null )
            {
                if ( !resultBuilders.TryGetValue( DocumentKey.FromPath( syntaxTreePath ), out builder ) )
                {
                    // An inheritable option is not bound to the trees the pipeline ran on. It is exported for the declaration that carries the
                    // options, which is wherever the fabric or the attribute put them, and which is generally not the file being edited. That
                    // declaration can therefore be in a tree that is not dirty, hence not a part of the PartialCompilation. The option is skipped
                    // instead of being filed under that tree, because the tree keeps the result of the run that did include it, and that result
                    // already carries the option: overwriting it would drop the diagnostics and introductions it holds. This is the treatment that
                    // issue #1768 gave the inheritable aspects above. See issue #1848.
                    Logger.DesignTime.Trace?.Log(
                        $"SplitResultsByTree: skipping the inheritable option of type '{optionItem.Key.OptionType}' on '{optionItem.Key.DeclarationId}' "
                        + $"because it belongs to syntax tree '{syntaxTreePath}', which is not a part of the partial compilation." );

                    continue;
                }
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

            SyntaxTreePipelineResult.Builder? builder;

            if ( syntaxTree == null )
            {
                builder = emptySyntaxTreeResult ??= new SyntaxTreePipelineResult.Builder( null );
            }
            else if ( !resultBuilders.TryGetValue( syntaxTree.GetDocumentKey(), out builder ) )
            {
                // An annotation is not bound to the trees the pipeline ran on either, because an aspect annotates the declaration it is given,
                // which is generally not in the file being edited. The annotation is skipped for the reason given for the inheritable options
                // above: the tree keeps the result of the run that did include it, and that result already carries the annotation. See issue #1848.
                Logger.DesignTime.Trace?.Log(
                    $"SplitResultsByTree: skipping {exportedAnnotations.Length} annotation(s) on '{annotationsOnDeclaration.Key}' because the "
                    + $"declaration is in syntax tree '{syntaxTree.FilePath}', which "
                    + (compilation.Compilation.ContainsSyntaxTree( syntaxTree )
                        ? "is a tree of this project that is not a part of the partial compilation."
                        : "belongs to another compilation.") );

                continue;
            }

            builder.Annotations ??= ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.CreateBuilder();
            builder.Annotations.Add( annotationsOnDeclaration.Key.ToSerializableId(), exportedAnnotations );
        }

        // Add syntax trees with empty output so they get cached too.
        foreach ( var syntaxTree in compilation.SyntaxTreeCollection )
        {
            if ( !resultBuilders.ContainsKey( syntaxTree.GetDocumentKey() ) )
            {
                resultBuilders.Add( syntaxTree.GetDocumentKey(), new SyntaxTreePipelineResult.Builder( syntaxTree ) );
            }
        }

        if ( emptySyntaxTreeResult != null )
        {
            resultBuilders[default] = emptySyntaxTreeResult;
        }

        return (resultBuilders.SelectAsReadOnlyCollection( b => b.Value.ToImmutable( compilation.Compilation ) ),
                foreignExtensions?.ToImmutable() ?? ImmutableArray<IDesignTimePipelineResultExtension>.Empty);
    }

    internal Invalidator ToInvalidator() => new( this );

    internal bool IsSyntaxTreeDirty( SyntaxTree syntaxTree ) => !this.SyntaxTreeResults.ContainsKey( syntaxTree.GetDocumentKey() );

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