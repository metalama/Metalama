// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Pipes;

namespace Metalama.Framework.DesignTime.Rpc;

public abstract partial class ClientEndpoint : BaseEndpoint
{
    private record struct Connection( NamedPipeClientStream Stream, JsonRpc Rpc, ImmutableArray<RpcClient> Clients );

    private readonly Callback _callback;
    private readonly NullCallback _nullCallback;

    private ImmutableDictionary<JsonRpc, Connection> _connectionByStream = ImmutableDictionary<JsonRpc, Connection>.Empty;

    // Not immutable because speed is important here.
    private Dictionary<string, RpcClient> _clientdByInterfaceName = new( StringComparer.Ordinal );

    private volatile int _connecting;

    private readonly ConcurrentDictionary<Type, TaskCompletionSource<RpcClient>> _clientAwaiters = new();
    private readonly object _addClientLock = new();

    protected ClientEndpoint(
        IServiceProvider serviceProvider,
        string pipeName ) :
        base( serviceProvider, pipeName )
    {
        this._callback = new Callback( this );
        this._nullCallback = new NullCallback( this );
    }

    protected abstract IEnumerable<RpcClient> CreateServiceClients();

    [PublicAPI]
    public TClient? GetClient<TClient>()
        where TClient : RpcClient
    {
        foreach ( var connection in this._connectionByStream )
        {
            foreach ( var client in connection.Value.Clients )
            {
                if ( client is TClient typedClient )
                {
                    return typedClient;
                }
            }
        }

        return null;
    }

    [PublicAPI]
    public TClient GetRequiredClient<TClient>()
        where TClient : RpcClient
        => this.GetClient<TClient>() ??
           throw new InvalidOperationException( $"The client '{typeof(TClient)}' is not registered." );

    public async ValueTask<TClient> GetOrWaitForClientAsync<TClient>( CancellationToken cancellationToken ) where TClient : RpcClient
    {
        TaskCompletionSource<RpcClient> awaiter;

        lock ( this._addClientLock )
        {
            var client = this.GetClient<TClient>();

            if ( client != null )
            {
                return client;
            }

            awaiter = this._clientAwaiters.GetOrAdd( typeof(TClient), _ => new TaskCompletionSource<RpcClient>() );
        }

        await this.EnsureInitialServicesRetrievedAsync( cancellationToken );

        return (TClient) await awaiter.Task.WarnIfLongAsync( this.Logger, $"waiting for client '{typeof(TClient)}'", cancellationToken );
    }

    protected virtual Task EnsureInitialServicesRetrievedAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    protected Task AddServiceClientsAsync( string pipeName, ImmutableArray<RpcClient> clients, CancellationToken cancellationToken )
    {
#if DEBUG
        var duplicates = clients
            .GroupBy( c => c.InterfaceName )
            .Where( g => g.Count() > 1 )
            .Select( x => x.Key )
            .ToList();

        if ( duplicates.Count > 0 )
        {
            throw new ArgumentOutOfRangeException( $"More than one client named {string.Join( ",", duplicates )}." );
        }
#endif

        if ( clients.IsEmpty )
        {
            return Task.CompletedTask;
        }
        else
        {
            // We need a new connection to serve the new service clients.
            return this.ConnectCoreAsync( pipeName, clients, firstConnection: false, cancellationToken );
        }
    }

    protected Task OnConnectedAsync( IEnumerable<RpcClient> clients, CancellationToken cancellationToken )
        => Task.WhenAll( clients.Select( i => i.OnRpcConnectedAsync( cancellationToken ) ) );

    [Obsolete( "Provide a CancellationToken." )]
    public Task<bool> ConnectAsync() => this.ConnectAsync( CancellationToken.None );

    public async Task<bool> ConnectAsync( CancellationToken cancellationToken )
    {
        if ( Interlocked.CompareExchange( ref this._connecting, 1, 0 ) != 0 )
        {
            this.Logger.Trace?.Log( $"The race to connect to the endpoint '{this.PipeName}' was lost." );

            await this.WaitUntilInitializedAsync( cancellationToken );

            return false;
        }

        try
        {
            var value = await this.ConnectCoreAsync( this.PipeName, this.CreateServiceClients().ToImmutableArray(), firstConnection: true, cancellationToken );
            this.InitializedTask.SetResult( value );

            return value;
        }
        catch ( Exception ex )
        {
            this.InitializedTask.SetException( ex );

            throw;
        }
    }

    private async Task<bool> ConnectCoreAsync(
        string pipeName,
        ImmutableArray<RpcClient> clients,
        bool firstConnection,
        CancellationToken cancellationToken = default )
    {
        ImmutableArray<RpcClient> newClients;

        try
        {
            lock ( this._addClientLock )
            {
                // Avoid duplicate clients.
                newClients = clients.Where( c => !this._clientdByInterfaceName.ContainsKey( c.InterfaceName ) ).ToImmutableArray();

                if ( newClients.IsEmpty )
                {
                    return true;
                }

                this.UpdateClientdByInterfaceName(
                    clientsByInterfaceName =>
                    {
                        foreach ( var client in newClients )
                        {
                            clientsByInterfaceName.Add( client.InterfaceName, client );
                        }
                    } );
            }

            this.Logger.Trace?.Log( $"Connecting to the endpoint '{pipeName}'." );

            var pipeStream = new NamedPipeClientStream( ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous );
            await pipeStream.ConnectAsync( cancellationToken ).WarnIfLongAsync( this.Logger, "Connect to pipe stream.", cancellationToken );

            var rpc = this.CreateRpc( pipeStream );

            if ( firstConnection )
            {
                rpc.AddLocalRpcTarget<IRpcCallback>( this._callback, null );
            }
            else
            {
                // We have to register a callback (otherwise we get RemoteMethodNotFoundException on the server side).
                // But we don't want to register the regular callback again, because that would cause duplicate events.
                // So register a dummy callback that does nothing.
                rpc.AddLocalRpcTarget<IRpcCallback>( this._nullCallback, null );
            }

            foreach ( var client in newClients )
            {
                client.ConfigureRpc( rpc );
            }

            rpc.StartListening();
            rpc.Disconnected += this.OnRpcDisconnected;

            // Update collections.
            InterlockedHelper.Update( ref this._connectionByStream, x => x.Add( rpc, new Connection( pipeStream, rpc, newClients ) ) );

            // Signal waiters.
            // We must do this _after_ updating the client collections.
            foreach ( var client in newClients )
            {
                if ( this._clientAwaiters.TryGetValue( client.GetType(), out var awaiter ) )
                {
                    awaiter.SetResult( client );
                }
            }

            this.Logger.Trace?.Log( $"The client is connected to the endpoint '{pipeName}'." );

            await this.OnConnectedAsync( newClients, cancellationToken )
                .WarnIfLongAsync( this.Logger, nameof(this.OnConnectedAsync), cancellationToken );

            return true;
        }
        catch ( Exception e )
        {
            this.Logger.Error?.Log( $"Cannot connect to the endpoint '{pipeName}': " + e.Message );

            this.ExceptionHandler?.OnException( e, this.Logger, this.DisposeCancellationToken.IsCancellationRequested );

            foreach ( var client in newClients )
            {
                client.SetFailure( e );
            }

            throw;
        }
    }

    private void OnRpcDisconnected( object sender, JsonRpcDisconnectedEventArgs e )
    {
        var rpc = (JsonRpc) sender;

        if ( this._connectionByStream.TryGetValue( rpc, out var connection ) )
        {
            // Update collections.
            this.UpdateClientdByInterfaceName(
                clientdByInterfaceName =>
                {
                    foreach ( var client in connection.Clients )
                    {
                        clientdByInterfaceName.Remove( client.InterfaceName );
                    }
                } );

            this._connectionByStream = this._connectionByStream.Remove( rpc );
        }
    }

    protected override void Dispose( bool disposing )
    {
        if ( disposing )
        {
            lock ( this._connectionByStream )
            {
                foreach ( var rpc in this._connectionByStream.Values )
                {
                    rpc.Rpc.Dispose();
                    rpc.Stream.Dispose();
                }
            }
        }
    }

    private void UpdateClientdByInterfaceName( Action<Dictionary<string, RpcClient>> action )
    {
        var copy = new Dictionary<string, RpcClient>( this._clientdByInterfaceName );
        action( copy );
        this._clientdByInterfaceName = copy;
    }

    protected virtual Task OnEventReceivedAsync( RpcEventEnvelope envelope, CancellationToken cancellationToken )
    {
        if ( this._clientdByInterfaceName.TryGetValue( envelope.OriginatingApi, out var client ) )
        {
            this.Logger.Trace?.Log( $"OnEventReceivedAsync: Passing message {envelope.Data.Category} to the client {client}." );

            return client.OnEventReceivedAsync( envelope.Data, cancellationToken );
        }
        else
        {
            this.Logger.Warning?.Log(
                $"OnEventReceivedAsync: Dropping a message originating from '{envelope.OriginatingApi}': the client interface is not registered." );

            return Task.CompletedTask;
        }
    }
}