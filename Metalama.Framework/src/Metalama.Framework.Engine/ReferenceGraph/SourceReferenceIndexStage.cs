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
/// The index of the references of the source compilation for one high-level stage, shared by all extensions of the stage.
/// </summary>
/// <remarks>
/// The object references a compilation. It must not outlive the stage. An instance is created by <see cref="SourceReferenceIndexService"/> and
/// passed to the extensions through the contexts of their hooks.
/// </remarks>
[PublicAPI]
public sealed class SourceReferenceIndexStage : IDisposable
{
    private readonly object _sync = new();
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<SyntaxTree, ImmutableArray<SyntaxNode>>? _rootsByTree;
    private CompilationModel? _sourceCompilation;
    private Task<InboundReferenceIndex>? _index;
    private bool _isDisposed;

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
    /// Gets the merged options of the stage.
    /// </summary>
    public ReferenceIndexerOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether at least one extension returned requirements for the stage. When it is <c>false</c>, the index is empty.
    /// </summary>
    public bool HasRequirements { get; }

    /// <summary>
    /// Gets a value indicating whether the index covers only the declaration roots that the extensions returned, and not every syntax tree.
    /// </summary>
    public bool IsRestrictedToDeclarationRoots => this._rootsByTree != null;

    /// <summary>
    /// Returns the index of the stage. The index is built on the first call, with the semantic models of the source compilation, one concurrent
    /// task per syntax tree.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token, which also cancels the build of the index when this call starts it.</param>
    /// <exception cref="ObjectDisposedException">The stage has ended.</exception>
    /// <remarks>
    /// The build runs outside of the lock of the stage. A build runs synchronously until its first incomplete task, which can be the whole build
    /// for a single syntax tree, and the other callers and <see cref="Dispose"/> must not wait for it on the lock.
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
    /// Ends the stage and releases the compilation and the index.
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
