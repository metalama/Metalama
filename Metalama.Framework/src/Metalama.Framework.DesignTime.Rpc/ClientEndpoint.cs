// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Pipes;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Abstract base class for RPC client endpoints. Client endpoints connect to server endpoints via named pipes,
/// create client proxies for remote services, and receive events from the server. Derive from this class
/// to create a client that consumes specific RPC services.
/// </summary>
/// <remarks>
/// <para>A client endpoint can connect to multiple pipes (when the server adds services dynamically).
/// The first connection registers the primary callback for events; subsequent connections use a null callback
/// to avoid duplicate event delivery.</para>
/// <para>Clients are registered either at connection time (via <see cref="CreateServiceClients"/>) or dynamically
/// (via <see cref="AddServiceClientsAsync"/>). Duplicate clients (by interface name) are automatically ignored.</para>
/// </remarks>
public abstract partial class ClientEndpoint : BaseEndpoint
{
    private record struct Connection( NamedPipeClientStream Stream, JsonRpc Rpc, ImmutableArray<RpcClient> Clients );

    private readonly Callback _callback;
    private readonly NullCallback _nullCallback;

    private ImmutableDictionary<JsonRpc, Connection> _connectionByStream = ImmutableDictionary<JsonRpc, Connection>.Empty;

    // Not immutable because speed is important here.
    private Dictionary<string, RpcClient> _clientsByInterfaceName = new( StringComparer.Ordinal );

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

    /// <summary>
    /// Creates the initial set of RPC clients to register on the endpoint. Override this method to provide
    /// the clients that will connect to server services when <see cref="ConnectAsync"/> is called.
    /// </summary>
    /// <returns>An enumerable of <see cref="RpcClient"/> instances to register.</returns>
    protected abstract IEnumerable<RpcClient> CreateServiceClients();

    /// <summary>
    /// Gets a registered client of the specified type, or <c>null</c> if not registered.
    /// </summary>
    /// <typeparam name="TClient">The type of client to retrieve.</typeparam>
    /// <returns>The registered client instance, or <c>null</c> if not found.</returns>
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

    /// <summary>
    /// Gets a registered client of the specified type.
    /// </summary>
    /// <typeparam name="TClient">The type of client to retrieve.</typeparam>
    /// <returns>The registered client instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified client type is not registered.</exception>
    [PublicAPI]
    public TClient GetRequiredClient<TClient>()
        where TClient : RpcClient
        => this.GetClient<TClient>() ??
           throw new InvalidOperationException( $"The client '{typeof(TClient)}' is not registered." );

    /// <summary>
    /// Gets a registered client of the specified type, waiting for it to be registered if necessary.
    /// </summary>
    /// <typeparam name="TClient">The type of client to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered client instance.</returns>
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

    /// <summary>
    /// Determines whether a client of the specified type is registered.
    /// </summary>
    /// <typeparam name="TClient">The type of client to check for.</typeparam>
    /// <returns><c>true</c> if the client is registered; otherwise, <c>false</c>.</returns>
    public bool IsClientAvailable<TClient>() where TClient : RpcClient
    {
        return this.GetClient<TClient>() != null;
    }

    /// <summary>
    /// Ensures that the initial set of services has been retrieved from the server.
    /// Called by <see cref="GetOrWaitForClientAsync{TClient}"/> when the requested client is not yet available,
    /// before blocking to wait for dynamically added clients. Override this method to query the server for
    /// available services and register corresponding clients. The base implementation does nothing.
    /// </summary>
    /// <remarks>
    /// <para>This method is called lazily - only when code first tries to get a client that doesn't exist yet.
    /// Typical implementations query the server for registered services and call <see cref="AddServiceClientsAsync"/>
    /// to register clients for each service.</para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task EnsureInitialServicesRetrievedAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    /// <summary>
    /// Adds additional service clients by connecting to a new pipe. Used when the server dynamically adds services.
    /// </summary>
    /// <param name="pipeName">The name of the pipe to connect to.</param>
    /// <param name="clients">The clients to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Called after clients have been connected. Override to perform additional initialization.
    /// </summary>
    /// <param name="clients">The clients that were connected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task OnConnectedAsync( IEnumerable<RpcClient> clients, CancellationToken cancellationToken )
        => Task.WhenAll( clients.Select( i => i.OnRpcConnectedAsync( cancellationToken ) ) );

    /// <summary>
    /// Connects to the server endpoint and registers the initial set of clients.
    /// If called concurrently, only one caller will perform the connection; others will wait for initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if this call performed the connection; <c>false</c> if another call won the race.</returns>
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
                newClients = clients.Where( c => !this._clientsByInterfaceName.ContainsKey( c.InterfaceName ) ).ToImmutableArray();

                if ( newClients.IsEmpty )
                {
                    return true;
                }

                this.UpdateClientsByInterfaceName(
                    clientsByInterfaceName =>
                    {
                        foreach ( var client in newClients )
                        {
                            this.Logger.Trace?.Log( $"Registering interface {client.InterfaceName}. First connection: {firstConnection}." );
                            clientsByInterfaceName.Add( client.InterfaceName, client );
                        }
                    } );
            }

            this.Logger.Trace?.Log( $"Connecting to the named pipe '{pipeName}'." );

            var pipeStream = new NamedPipeClientStream( ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous );
            await pipeStream.ConnectAsync( cancellationToken ).WarnIfLongAsync( this.Logger, "Connect to pipe stream.", cancellationToken );

            if ( this.TestSyncProvider != null )
            {
                await this.TestSyncProvider.SyncPointAsync( $"ClientEndpoint.AfterGetsServer:{pipeName}", cancellationToken );
            }

            this.Logger.Trace?.Log( $"Connected to the named pipe '{pipeName}'." );

            // Create the callback channel.
            var rpc = this.CreateRpc( pipeStream );
            rpc.Disconnected += this.OnRpcDisconnected;

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

            this.Logger.Trace?.Log( $"Start listening to callback channel of the named pipe '{pipeName}'." );
            rpc.StartListening();

            if ( this.TestSyncProvider != null )
            {
                await this.TestSyncProvider.SyncPointAsync( $"ClientEndpoint.AfterStartsListening:{pipeName}", cancellationToken );
            }

            // Update collections.
            InterlockedHelper.Update( ref this._connectionByStream, x => x.Add( rpc, new Connection( pipeStream, rpc, newClients ) ) );

            if ( this.TestSyncProvider != null )
            {
                await this.TestSyncProvider.SyncPointAsync( $"ClientEndpoint.BeforeSignalingAwaiters:{pipeName}", cancellationToken );
            }

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
            this.Logger.LogException( e, $"Cannot connect to the endpoint '{pipeName}'" );

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
        this.Logger.Warning?.Log( $"RPC disconnected: '{e.Description}'." );

        var rpc = (JsonRpc) sender;

        if ( this._connectionByStream.TryGetValue( rpc, out var connection ) )
        {
            // Update collections.
            this.UpdateClientsByInterfaceName(
                clientsByInterfaceName =>
                {
                    foreach ( var client in connection.Clients )
                    {
                        clientsByInterfaceName.Remove( client.InterfaceName );
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

    private void UpdateClientsByInterfaceName( Action<Dictionary<string, RpcClient>> action )
    {
        var copy = new Dictionary<string, RpcClient>( this._clientsByInterfaceName );
        action( copy );
        this._clientsByInterfaceName = copy;
    }

    /// <summary>
    /// Called when an event is received from the server. Routes the event to the appropriate client
    /// based on the originating API interface name.
    /// </summary>
    /// <param name="envelope">The event envelope containing the event data and originating API name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnEventReceivedAsync( RpcEventEnvelope envelope, CancellationToken cancellationToken )
    {
        if ( this._clientsByInterfaceName.TryGetValue( envelope.OriginatingApi, out var client ) )
        {
            this.Logger.Trace?.Log( $"OnEventReceivedAsync: Passing message {envelope.Data.GetType().Name} to the client {client}." );

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