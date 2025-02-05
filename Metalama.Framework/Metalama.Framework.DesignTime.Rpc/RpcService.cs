// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using StreamJsonRpc;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Base class for RPC services implementations, i.e. on the server side.
/// </summary>
public abstract class RpcService
{
    private readonly ServerEndpoint _serverEndpoint;

    protected ILogger Logger { get; }

    protected RpcService( ServerEndpoint serverEndpoint )
    {
        this.Logger = serverEndpoint.LoggerFactory.GetLogger( this.GetType().Name );
        this._serverEndpoint = serverEndpoint;
    }

    protected ValueTask WaitUntilInitializedAsync( CancellationToken cancellationToken = default )
        => this._serverEndpoint.WaitUntilInitializedAsync( cancellationToken );

    internal abstract void ConfigureRpc( JsonRpc rpc );

    internal virtual void OnRpcDisconnected( JsonRpc rpc ) { }
}

/// <summary>
/// Base class for an RPC service that has a callback.
/// </summary>
public abstract class RpcService<TApi> : RpcService, IDisposable where TApi : IRpcApi
{
    private readonly ConcurrentDictionary<JsonRpc, IRpcCallback> _clients = new();
    private readonly ConcurrentDictionary<JsonRpc, TApi> _apis = new();

    protected RpcService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

    public virtual void Dispose() { }

    internal override void ConfigureRpc( JsonRpc rpc )
    {
        var client = rpc.AttachSafe<IRpcCallback>( this.Logger );
        var implementation = this.CreateApi( new EventSender( client ) );
        rpc.AddLocalRpcTarget( implementation, null );
        this._clients.TryAdd( rpc, client );
        this._apis.TryAdd( rpc, implementation );
    }

    internal override void OnRpcDisconnected( JsonRpc rpc )
    {
        this._clients.TryRemove( rpc, out _ );
    }

    protected IEnumerable<TApi> Apis => this._apis.Values;

    protected abstract TApi CreateApi( IRpcEventSender eventSender );

    protected Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        if ( this._clients.Count == 0 )
        {
            return Task.CompletedTask;
        }

        var envelope = new RpcEventEnvelope( typeof(TApi).Name, eventData );

        return Task.WhenAll( this._clients.Values.Select( c => c.RaiseEventAsync( envelope, cancellationToken ) ) );
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
}