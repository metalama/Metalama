// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Base class for RPC services implementations, i.e. on the server side.
/// </summary>
public abstract class RpcService
{
    private static int _nextId;
    private readonly TaskCompletionSource<bool> _initialized = new();
    private readonly int _id = Interlocked.Increment( ref _nextId );

    protected ILogger Logger { get; }

    protected RpcService( ServerEndpoint serverEndpoint )
    {
        this.Logger = serverEndpoint.LoggerFactory.GetLogger( this.GetType().Name ).WithPrefix( this._id.ToString() );
        this.Logger.Trace?.Log( $"Instantiating." );
    }

    protected Task WaitUntilInitializedAsync( CancellationToken cancellationToken = default )
        => this._initialized.Task.WarnIfLongAsync( this.Logger, nameof(this.WaitUntilInitializedAsync), cancellationToken );

    internal abstract void ConfigureRpc( JsonRpc rpc );

    /// <summary>
    /// Called by the server endpoint after <see cref="JsonRpc.StartListening"/> has been called on the rpc.
    /// Subclasses register the rpc with their callback bookkeeping at this point so that event broadcasting
    /// (such as <c>RaiseEventAsync</c>) never targets a rpc whose <c>StartListening</c> has not yet been
    /// called, which would throw <c>"This operation is not allowed before listening for messages has started."</c>
    /// </summary>
    internal virtual void OnRpcStarted( JsonRpc rpc ) { }

    protected internal void OnRpcConnected()
    {
        this.Logger.Trace?.Log( nameof(this.OnRpcConnected) );
        this._initialized.TrySetResult( true );
    }

    internal virtual void OnRpcDisconnected( JsonRpc rpc )
    {
        this.Logger.Trace?.Log( nameof(this.OnRpcDisconnected) );
    }

    public override string ToString() => $"{{{this.GetType().Name}, Id={this._id}}}";
}

/// <summary>
/// Base class for an RPC service that has a callback.
/// </summary>
public abstract class RpcService<TApi> : RpcService, IDisposable where TApi : IRpcApi
{
    private readonly ConcurrentDictionary<JsonRpc, IRpcCallback> _clients = new();
    private readonly ConcurrentDictionary<JsonRpc, TApi> _apis = new();

    // Holds (callback proxy, local API) pairs created in ConfigureRpc until OnRpcStarted promotes them
    // to _clients/_apis. Promoting only after rpc.StartListening() prevents RaiseEventAsync from
    // invoking the callback proxy on a rpc that is not yet listening.
    private readonly ConcurrentDictionary<JsonRpc, PendingClient> _pendingClients = new();

    protected RpcService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

    public virtual void Dispose() { }

    internal override void ConfigureRpc( JsonRpc rpc )
    {
        var client = rpc.Attach<IRpcCallback>();
        var implementation = this.CreateApi( new EventSender( client ) );
        rpc.AddLocalRpcTarget( implementation, null );
        this._pendingClients.TryAdd( rpc, new PendingClient( client, implementation ) );
    }

    internal override void OnRpcStarted( JsonRpc rpc )
    {
        if ( this._pendingClients.TryRemove( rpc, out var pending ) )
        {
            this._clients.TryAdd( rpc, pending.Callback );
            this._apis.TryAdd( rpc, pending.Api );
        }
    }

    internal override void OnRpcDisconnected( JsonRpc rpc )
    {
        // Cover disposal between ConfigureRpc and OnRpcStarted as well as the normal post-Started path.
        this._pendingClients.TryRemove( rpc, out _ );
        this._clients.TryRemove( rpc, out _ );
    }

    protected IEnumerable<TApi> Apis => this._apis.Values;

    protected abstract TApi CreateApi( IRpcEventSender eventSender );

    protected async Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        await this.WaitUntilInitializedAsync( cancellationToken );

        if ( this._clients.Count == 0 )
        {
            this.Logger.Trace?.Log( $"RaiseEventAsync: No clients, nothing to do for event {eventData.Category}." );

            return;
        }

        this.Logger.Trace?.Log( $"RaiseEventAsync: Notifying {this._clients.Count} clients with event {eventData.Category}." );

        var envelope = new RpcEventEnvelope( typeof(TApi).Name, eventData );

        await Task.WhenAll( this._clients.Values.Select( c => c.RaiseEventAsync( envelope, cancellationToken ) ) ).WithCancellation( cancellationToken );
    }

    protected void RaiseEvent( RpcEventData eventData, CancellationToken cancellationToken = default )
    {
        // TODO: Exception handling.
        _ = this.RaiseEventAsync( eventData, cancellationToken );
    }

    private sealed class EventSender : IRpcEventSender
    {
        private readonly IRpcCallback _client;

        public EventSender( IRpcCallback client )
        {
            this._client = client;
        }

        public Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
            => this._client.RaiseEventAsync( new RpcEventEnvelope( typeof(TApi).Name, eventData ), cancellationToken );
    }

    private readonly record struct PendingClient( IRpcCallback Callback, TApi Api );
}