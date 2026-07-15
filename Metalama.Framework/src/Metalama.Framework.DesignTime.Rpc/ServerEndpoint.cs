// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Abstract base class for RPC server endpoints. Server endpoints listen for client connections on named pipes,
/// register RPC services, and can broadcast events to connected clients. Derive from this class to create
/// a server that exposes specific RPC services.
/// </summary>
/// <remarks>
/// <para>A server endpoint can accept multiple client connections simultaneously. Each client connection
/// is handled independently, and services are registered on each connection.</para>
/// <para>Services can be registered either at startup (via <see cref="CreateServices"/>) or dynamically
/// (via <see cref="AddServices"/>). Dynamically added services create additional named pipes with the
/// format <c>{PipeName}-{index}</c>.</para>
/// </remarks>
public abstract class ServerEndpoint : BaseEndpoint
{
    private readonly ConcurrentDictionary<JsonRpc, NamedPipeServerStream> _pipes = new();

    private int _nextPipeId = 1;
    private ImmutableArray<RpcService> _services = ImmutableArray<RpcService>.Empty;
    private int _started;

    protected ServerEndpoint(
        IServiceProvider serviceProvider,
        string pipeName ) : base(
        serviceProvider,
        pipeName ) { }

    /// <summary>
    /// Gets a registered service of the specified type after waiting for the endpoint to be initialized.
    /// </summary>
    /// <typeparam name="TService">The type of service to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified service type is not registered.</exception>
    public async ValueTask<TService> GetRequiredServiceAsync<TService>( CancellationToken cancellationToken )
        where TService : RpcService
    {
        await this.WaitUntilInitializedAsync( cancellationToken );

        return this._services.OfType<TService>().SingleOrDefault()
               ?? throw new InvalidOperationException( $"Service '{typeof(TService).Name}' is not registered." );
    }

    /// <summary>
    /// Creates the initial set of RPC services to register on the endpoint. Override this method to provide
    /// the services that will be available to clients immediately after calling <see cref="Start"/>.
    /// </summary>
    /// <returns>An enumerable of <see cref="RpcService"/> instances to register.</returns>
    protected abstract IEnumerable<RpcService> CreateServices();

    /// <summary>
    /// Gets the number of currently connected clients.
    /// </summary>
    public int ClientCount => this._pipes.Count;

#pragma warning disable VSTHRD100 // Avoid "async void".
    /// <summary>
    /// Starts the server endpoint and begins listening for client connections. This method does not block
    /// waiting for clients; connection handling is performed asynchronously in background tasks.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the endpoint has already been started.</exception>
    public void Start()
    {
        try
        {
            // State transitions: 0 (default) -> 1 (starting) -> 2 (started)
            if ( Interlocked.CompareExchange( ref this._started, 1, 0 ) != 0 )
            {
                throw new InvalidOperationException( $"The '{this}' endpoint has already been started." );
            }

            var services = this.CreateServices().ToImmutableArray();
            this._services = this._services.AddRange( services );
            this.ExecuteBackgroundTask( ct => this.AcceptNewClientAsync( this.PipeName, services, ct ), nameof(this.AcceptNewClientAsync), false );

            Interlocked.Exchange( ref this._started, 2 );
            this.InitializedTask.SetResult( true );
        }
        catch ( Exception ex )
        {
            this.InitializedTask.SetException( ex );

            throw;
        }
    }
#pragma warning restore VSTHRD100 // Avoid "async void".

    /// <summary>
    /// Adds additional services to the endpoint dynamically after startup. Creates a new named pipe
    /// with the format <c>{PipeName}-{index}</c> for the new services.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>The name of the newly created pipe for these services.</returns>
    protected string AddServices( ImmutableArray<RpcService> services )
    {
        // State must be 2 (started). States 0 (default) and 1 (starting) are not allowed.
        if ( this._started != 2 )
        {
            throw new InvalidOperationException(
                $"Cannot add services to endpoint '{this}' because Start() has not completed yet. " +
                $"Ensure Start() is called before any code that may trigger extension discovery." );
        }

        var pipeId = Interlocked.Increment( ref this._nextPipeId );
        var pipeName = this.PipeName + "-" + pipeId;
        this._services = this._services.AddRange( services );

        this.ExecuteBackgroundTask( ct => this.AcceptNewClientAsync( pipeName, services, ct ), nameof(this.AcceptNewClientAsync), false );

        return pipeName;
    }

    /// <summary>
    /// Occurs when a client has connected and services have been registered on the connection.
    /// </summary>
    public event Action? ClientConnected;

    /// <summary>
    /// Occurs when a client has disconnected.
    /// </summary>
    public event Action? ClientDisconnected;

    /// <summary>
    /// Called after the server pipe has been created but before waiting for client connections.
    /// Override this method to perform additional setup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnServerPipeCreatedAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    private void OnConnected( IEnumerable<RpcService> services )
    {
        foreach ( var service in services )
        {
            service.OnRpcConnected();
        }

        try
        {
            this.ClientConnected?.Invoke();
        }
        catch ( Exception ex )
        {
            this.Logger.LogException( ex );
        }
    }

    private async Task AcceptNewClientAsync( string pipeName, ImmutableArray<RpcService> services, CancellationToken cancellationToken )
    {
        NamedPipeServerStream pipe;

        // Accept loop with recovery. A newly-created NamedPipeServerStream is connectable the moment it
        // exists — before we reach WaitForConnectionAsync. If a client connects into that window and then
        // disconnects before we accept it (e.g. a ClientEndpoint that is disposed mid-connect), that pipe
        // instance is broken and WaitForConnectionAsync throws IOException ("The pipe is being closed").
        // We must discard the broken instance and create a fresh one, otherwise the exception would tear
        // down this accept task and the endpoint would silently stop accepting ANY further client on this
        // pipe for the rest of the session (leaving design-time features permanently unresponsive).
        while ( true )
        {
            // Bail out promptly if the endpoint is being disposed, rather than creating a fresh pipe instance
            // and running OnServerPipeCreatedAsync only to unwind at the next await.
            cancellationToken.ThrowIfCancellationRequested();

            pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous );

            await this.OnServerPipeCreatedAsync( cancellationToken ).WarnIfLongAsync( this.Logger, nameof(this.OnServerPipeCreatedAsync), cancellationToken );

            this.Logger.Trace?.Log( $"Endpoint '{pipeName}': wait for a client (currently has {this.ClientCount})." );

            if ( this.TestSyncProvider != null )
            {
                // Sync point placed AFTER the server pipe is created (so a client can already connect to it)
                // but BEFORE WaitForConnectionAsync. Tests use this to force the interleaving where a client
                // connects and disconnects while the server is still parked here.
                await this.TestSyncProvider.SyncPointAsync( $"ServerEndpoint.BeforeWaitForConnection:{pipeName}", cancellationToken );
            }

            try
            {
                await pipe.WaitForConnectionAsync( cancellationToken );

                break;
            }
            catch ( IOException e )
            {
                // A client connected to this instance and disconnected before we accepted it. The instance is
                // now unusable; dispose it and loop to create a fresh one and keep accepting. Cancellation
                // (endpoint disposal) surfaces as OperationCanceledException, not IOException, and is handled
                // by the ThrowIfCancellationRequested at the top of the loop.
                this.Logger.Trace?.Log(
                    $"Endpoint '{pipeName}': a client disconnected before it could be accepted ('{e.Message}'); discarding the pipe instance and retrying." );

                pipe.Dispose();
            }
        }

        if ( this.TestSyncProvider != null )
        {
            await this.TestSyncProvider.SyncPointAsync( $"ServerEndpoint.AfterGetsClient:{pipeName}", cancellationToken );
        }

        this.Logger.Trace?.Log( $"Endpoint '{pipeName}': got a client (now has {this.ClientCount + 1})." );

        JsonRpc? rpc = null;
        var ownershipTransferred = false;

        try
        {
            rpc = CreateRpc( pipe );

            this.Logger.Trace?.Log( $"Endpoint '{pipeName}': adding services {string.Join( ", ", services.Select( s => s.GetType().Name ) )}." );

            foreach ( var i in services )
            {
                i.ConfigureRpc( rpc );
            }

            rpc.Disconnected += this.OnRpcDisconnected;

            // From this point the pipe and rpc are owned by _pipes: Dispose() and OnRpcDisconnected release them.
            this._pipes.TryAdd( rpc, pipe );
            ownershipTransferred = true;

            if ( this.TestSyncProvider != null )
            {
                await this.TestSyncProvider.SyncPointAsync( $"ServerEndpoint.AfterConfiguresRpc:{pipeName}", cancellationToken );
            }

            this.Logger.Trace?.Log( $"Endpoint '{pipeName}': start listening." );
            rpc.StartListening();

            if ( this.TestSyncProvider != null )
            {
                await this.TestSyncProvider.SyncPointAsync( $"ServerEndpoint.AfterStartsListening:{pipeName}", cancellationToken );
            }

            // Promote the rpc into each service's callback bookkeeping AFTER StartListening so that any
            // concurrent broadcast (RaiseEventAsync) cannot pick up a rpc that would throw
            // "This operation is not allowed before listening for messages has started."
            foreach ( var i in services )
            {
                i.OnRpcStarted( rpc );
            }

            this.Logger.Trace?.Log( $"The server endpoint '{pipeName}' is ready." );

            // Listen to another client.
            this.ExecuteBackgroundTask( ct => this.AcceptNewClientAsync( pipeName, services, ct ), nameof(this.AcceptNewClientAsync), false );

            this.OnConnected( services );
        }
        catch
        {
            // If ownership was never transferred to _pipes (e.g. CreateRpc/ConfigureRpc threw, or the accepted
            // client disconnected before we finished setting it up), nothing else will dispose the pipe or rpc,
            // so release them here to avoid leaking the OS pipe handle. Once in _pipes, Dispose() handles them.
            if ( !ownershipTransferred )
            {
                try
                {
                    rpc?.Dispose();
                }
                catch ( Exception disposeException )
                {
                    this.Logger.Warning?.Log( $"Disposing rpc after a failed accept threw: {disposeException}" );
                }

                try
                {
                    pipe.Dispose();
                }
                catch ( Exception disposeException )
                {
                    this.Logger.Warning?.Log( $"Disposing pipe after a failed accept threw: {disposeException}" );
                }
            }

            throw;
        }
    }

    private void OnRpcDisconnected( object? sender, JsonRpcDisconnectedEventArgs e )
    {
        this.Logger.Trace?.Log( $"Endpoint '{this.PipeName}': a client got disconnected." );

        this.OnRpcDisconnected( (JsonRpc) sender! );
    }

    private void OnRpcDisconnected( JsonRpc rpc )
    {
        foreach ( var i in this._services )
        {
            i.OnRpcDisconnected( rpc );
        }

        if ( this._pipes.TryRemove( rpc, out var pipe ) )
        {
            pipe.Dispose();
        }

        try
        {
            this.ClientDisconnected?.Invoke();
        }
        catch ( Exception ex )
        {
            this.Logger.LogException( ex );
        }
    }

    protected override void Dispose( bool disposing )
    {
        base.Dispose( disposing );

        foreach ( var pipe in this._pipes )
        {
            try
            {
                if ( !pipe.Key.IsDisposed )
                {
                    pipe.Key.Dispose();
                }

                pipe.Value.Dispose();
            }
            catch ( Exception e )
            {
                this.ExceptionHandler?.OnException( e, this.Logger, true );
            }
        }
    }
}