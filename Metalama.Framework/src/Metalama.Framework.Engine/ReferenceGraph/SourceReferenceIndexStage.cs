// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// The index of the references of the source compilation for one pipeline execution, shared by all extensions.
/// </summary>
/// <remarks>
/// The pipeline creates one instance in the source stage, which is the first high-level stage, and disposes it at the end of that stage. The
/// stages that follow a low-level weaver have no index. The object references a compilation, so it must not outlive the pipeline execution. An
/// instance is created by <see cref="SourceReferenceIndexService"/> and passed to the extensions through the context of their transforming hook.
/// </remarks>
[PublicAPI]
public sealed class SourceReferenceIndexStage : IDisposable
{
    /// <summary>
    /// The lock that protects <see cref="_sourceCompilation"/>, <see cref="_index"/> and <see cref="_isDisposed"/>.
    /// </summary>
    private readonly object _sync = new();

    /// <summary>
    /// The service provider of the project.
    /// </summary>
    private readonly ProjectServiceProvider _serviceProvider;

    /// <summary>
    /// The declaration roots to index, grouped by syntax tree, or <c>null</c> when every syntax tree is indexed.
    /// </summary>
    private readonly IReadOnlyDictionary<SyntaxTree, ImmutableArray<SyntaxNode>>? _rootsByTree;

    /// <summary>
    /// The source compilation, or <c>null</c> after <see cref="Dispose"/>.
    /// </summary>
    private CompilationModel? _sourceCompilation;

    /// <summary>
    /// The task that builds the index, or <c>null</c> when no build has started or after <see cref="Dispose"/>.
    /// </summary>
    private Task<InboundReferenceIndex>? _index;

    /// <summary>
    /// Indicates whether <see cref="Dispose"/> has been called.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceReferenceIndexStage"/> class.
    /// </summary>
    internal SourceReferenceIndexStage(
        ProjectServiceProvider serviceProvider,
        CompilationModel sourceCompilation,
        ReferenceIndexerOptions options,
        bool hasRequirements,
        IReadOnlyDictionary<SyntaxTree, ImmutableArray<SyntaxNode>>? rootsByTree )
    {
        this._serviceProvider = serviceProvider;
        this._sourceCompilation = sourceCompilation;
        this.Options = options;
        this.HasRequirements = hasRequirements;
        this._rootsByTree = rootsByTree;
    }

    /// <summary>
    /// Gets the merged options of the extensions.
    /// </summary>
    public ReferenceIndexerOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether at least one extension returned requirements. When it is <c>false</c>, the index is empty.
    /// </summary>
    public bool HasRequirements { get; }

    /// <summary>
    /// Gets a value indicating whether the index covers only the declaration roots that the extensions returned, and not every syntax tree.
    /// </summary>
    public bool IsRestrictedToDeclarationRoots => this._rootsByTree != null;

    /// <summary>
    /// Returns the index. The index is built on the first call, with the semantic models of the source compilation, one concurrent
    /// task per syntax tree.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token, which also cancels the build of the index when this call starts it.</param>
    /// <exception cref="ObjectDisposedException">The source stage has ended.</exception>
    /// <remarks>
    /// <para>
    /// The build runs outside of the lock of the instance. A build runs synchronously until its first incomplete task, which can be the whole build
    /// for a single syntax tree, and the other callers and <see cref="Dispose"/> must not wait for it on the lock.
    /// </para>
    /// <para>
    /// The syntax trees are indexed concurrently, so the order of the referenced symbols and of their references in the index is not
    /// deterministic. A consumer that reports diagnostics or creates transformations from the index must sort what it reads.
    /// </para>
    /// </remarks>
    public Task<InboundReferenceIndex> GetIndexAsync( CancellationToken cancellationToken )
    {
        TaskCompletionSource<InboundReferenceIndex> completionSource;
        CompilationModel sourceCompilation;

        lock ( this._sync )
        {
            if ( this._isDisposed )
            {
                throw new ObjectDisposedException( nameof(SourceReferenceIndexStage), "The stage of the index of source references has ended." );
            }

            // A build that was canceled or that failed is not cached, so a later call can start it again.
            if ( this._index != null && !this._index.IsCanceled && !this._index.IsFaulted )
            {
                return this._index;
            }

            completionSource = new TaskCompletionSource<InboundReferenceIndex>( TaskCreationOptions.RunContinuationsAsynchronously );
            this._index = completionSource.Task;
            sourceCompilation = this._sourceCompilation!;
        }

        return this.BuildAndPublishIndexAsync( sourceCompilation, completionSource, cancellationToken );
    }

    /// <summary>
    /// Builds the index and completes the task that the other callers of <see cref="GetIndexAsync"/> receive.
    /// </summary>
    private async Task<InboundReferenceIndex> BuildAndPublishIndexAsync(
        CompilationModel sourceCompilation,
        TaskCompletionSource<InboundReferenceIndex> completionSource,
        CancellationToken cancellationToken )
    {
        try
        {
            var index = await this.BuildIndexAsync( sourceCompilation, cancellationToken );
            completionSource.TrySetResult( index );

            return index;
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
        {
            completionSource.TrySetCanceled( cancellationToken );

            throw;
        }
        catch ( Exception e )
        {
            completionSource.TrySetException( e );

            throw;
        }
    }

    /// <summary>
    /// Builds the index from the declaration roots when the index is restricted to them, or else from every syntax tree of the source compilation.
    /// The index is empty when no extension returned requirements.
    /// </summary>
    private async Task<InboundReferenceIndex> BuildIndexAsync( CompilationModel sourceCompilation, CancellationToken cancellationToken )
    {
        var builder = new InboundReferenceIndexBuilder( this._serviceProvider, this.Options, SymbolEqualityComparer.Default );

        if ( this.HasRequirements )
        {
            var semanticModelProvider = sourceCompilation.CompilationContext.SemanticModelProvider;
            var concurrentTaskRunner = this._serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            if ( this._rootsByTree != null )
            {
                await concurrentTaskRunner.RunConcurrentlyAsync(
                    this._rootsByTree,
                    pair => builder.IndexDeclarationRoots( pair.Key, pair.Value, semanticModelProvider, cancellationToken ),
                    cancellationToken );
            }
            else
            {
                await concurrentTaskRunner.RunConcurrentlyAsync(
                    sourceCompilation.PartialCompilation.SyntaxTreeCollection,
                    syntaxTree => builder.IndexSyntaxTree( syntaxTree, semanticModelProvider, cancellationToken ),
                    cancellationToken );
            }
        }

        return builder.ToReadOnly();
    }

    /// <summary>
    /// Ends the lifetime of the index and releases the compilation and the index.
    /// </summary>
    public void Dispose()
    {
        lock ( this._sync )
        {
            this._isDisposed = true;
            this._sourceCompilation = null;
            this._index = null;
        }
    }

    /// <summary>
    /// Groups the declaration roots by syntax tree and removes the roots that are contained in another root.
    /// </summary>
    internal static IReadOnlyDictionary<SyntaxTree, ImmutableArray<SyntaxNode>> MergeRoots( IEnumerable<SyntaxNode> roots )
    {
        var result = new Dictionary<SyntaxTree, ImmutableArray<SyntaxNode>>();

        foreach ( var group in roots.GroupBy( r => r.SyntaxTree ) )
        {
            var distinctRoots = new HashSet<SyntaxNode>( group );

            var outermostRoots = distinctRoots
                .Where( root => !root.Ancestors().Any( distinctRoots.Contains ) )
                .OrderBy( root => root.SpanStart )
                .ToImmutableArray();

            result.Add( group.Key, outermostRoots );
        }

        return result;
    }
}
