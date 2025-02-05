// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Fabrics;

public sealed class QueryExecutionContext
{
    private static readonly WeakCache<CompilationModel, ConcurrentDictionary<IQuery<IDeclaration>, Node>> _staticCache = new();
    private ConcurrentDictionary<IQuery<IDeclaration>, Node>? _selectionCache;

    internal CancellationToken CancellationToken { get; }

    internal CompilationModel Compilation { get; }

    internal IDiagnosticAdder DiagnosticSink { get; }

    public UserCodeExecutionContext UserCodeExecutionContext { get; }

    public UserCodeInvoker UserCodeInvoker { get; }

    internal QueryExecutionContext(
        CompilationModel compilation,
        IDiagnosticAdder diagnosticSink,
        UserCodeInvoker userCodeInvoker,
        UserCodeExecutionContext userCodeExecutionContext,
        CancellationToken cancellationToken )
    {
        this.CancellationToken = cancellationToken;
        this.UserCodeInvoker = userCodeInvoker;
        this.Compilation = compilation;
        this.DiagnosticSink = diagnosticSink;
        this.UserCodeExecutionContext = userCodeExecutionContext;
    }

    internal async ValueTask<T?> GetFromCacheAsync<T>( IQuery<IDeclaration> receiver, CancellationToken cancellationToken )
        where T : class
    {
        this._selectionCache ??= _staticCache.GetOrAdd( this.Compilation, _ => new ConcurrentDictionary<IQuery<IDeclaration>, Node>() );

        var node = this._selectionCache.GetOrAdd( receiver, _ => new Node() );

        if ( node.Payload != null )
        {
            return (T) node.Payload;
        }
        else
        {
            await node.Semaphore.WaitAsync( cancellationToken );

            return null;
        }
    }

    internal void AddToCache( IQuery<IDeclaration> receiver, object payload )
    {
        if ( !this._selectionCache.AssertNotNull().TryGetValue( receiver, out var node ) )
        {
            throw new AssertionFailedException();
        }

        node.Payload = payload;
        node.Semaphore.Release();
    }

    private sealed class Node
    {
        public SemaphoreSlim Semaphore { get; } = new( 1 );

        public object? Payload { get; set; }
    }
}