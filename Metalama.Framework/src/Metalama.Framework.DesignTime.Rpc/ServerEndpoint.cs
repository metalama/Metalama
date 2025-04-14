// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Pipes;

namespace Metalama.Framework.DesignTime.Rpc;

public abstract class ServerEndpoint : BaseEndpoint
{
    private readonly ConcurrentDictionary<JsonRpc, NamedPipeServerStream> _pipes = new();
    private readonly TaskCompletionSource<bool> _startedTask = new();

    private int _nextPipeId = 1;
    private ImmutableArray<RpcService> _services;

    protected ServerEndpoint(
        IServiceProvider serviceProvider,
        string pipeName ) : base(
        serviceProvider,
        pipeName ) { }

    public async ValueTask<TService> GetRequiredServiceAsync<TService>( CancellationToken cancellationToken )
        where TService : RpcService
    {
        // Services are not availabable until the Start method has completed.
        // However, they are available before the server got a client, so we can't use WhenInitializedAsync.
        await this._startedTask.Task.WithCancellation( cancellationToken );

        return this._services.OfType<TService>().SingleOrDefault()
               ?? throw new InvalidOperationException( $"Service '{typeof(TService).Name}' is not registered." );
    }

    protected abstract IEnumerable<RpcService> CreateServices();

    public int ClientCount => this._pipes.Count;

#pragma warning disable VSTHRD100 // Avoid "async void".
    /// <summary>
    /// Starts the RPC connection, but does not wait until the service is fully started.
    /// </summary>
    public void Start()
    {
        if ( !this._services.IsDefault )
        {
            throw new InvalidOperationException( $"The '{this}' endpoint has already been started." );
        }

        var services = this.CreateServices().ToImmutableArray();
        this._services = services;
        _ = this.StartCoreAsync( this.PipeName, services, this.DisposeCancellationToken );
        this._startedTask.SetResult( true );
    }
#pragma warning restore VSTHRD100 // Avoid "async void".

    protected string AddServices( ImmutableArray<RpcService> services )
    {
        var pipeId = Interlocked.Increment( ref this._nextPipeId );
        var pipeName = this.PipeName + "-" + pipeId;
        this._services = this._services.AddRange( services );

        this.ExecuteBackgroundTask( ct => this.AcceptNewClientAsync( pipeName, services, ct ), nameof(this.AcceptNewClientAsync), false );

        return pipeName;
    }

    protected virtual Task OnServerPipeCreatedAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    private async Task StartCoreAsync( string pipeName, ImmutableArray<RpcService> services, CancellationToken cancellationToken )
    {
        this.Logger.Trace?.Log( $"Starting the server endpoint '{pipeName}'." );

        try
        {
            await this.AcceptNewClientAsync( pipeName, services, cancellationToken );

            this.Logger.Trace?.Log( $"The server endpoint '{pipeName}' is ready." );

            this.InitializedTask.SetResult( true );
        }
        catch ( Exception e )
        {
            this.InitializedTask.SetException( e );
            this.ExceptionHandler?.OnException( e, this.Logger, this.DisposeCancellationToken.IsCancellationRequested );
        }
    }

    private static void OnConnected( IEnumerable<RpcService> services )
    {
        foreach ( var service in services )
        {
            service.OnRpcConnected();
        }
    }

    private async Task AcceptNewClientAsync( string pipeName, ImmutableArray<RpcService> services, CancellationToken cancellationToken )
    {
        var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous );

        await this.OnServerPipeCreatedAsync( cancellationToken ).WarnIfLongAsync( this.Logger, nameof(this.OnServerPipeCreatedAsync), cancellationToken );

        this.Logger.Trace?.Log( $"Endpoint '{pipeName}': wait for a client (currently has {this.ClientCount})." );

        await pipe.WaitForConnectionAsync( cancellationToken );

        this.Logger.Trace?.Log( $"Endpoint '{pipeName}': got a client (now has {this.ClientCount + 1})." );

        var rpc = this.CreateRpc( pipe );

        this.Logger.Trace?.Log( $"Endpoint '{pipeName}': adding services {string.Join( ", ", services.Select( s => s.GetType().Name ) )}." );

        foreach ( var i in services )
        {
            i.ConfigureRpc( rpc );
        }

        rpc.Disconnected += this.OnRpcDisconnected;
        this._pipes.TryAdd( rpc, pipe );

        this.Logger.Trace?.Log( $"Endpoint '{pipeName}': start listening." );
        rpc.StartListening();

        this.Logger.Trace?.Log( $"The server endpoint '{pipeName}' is ready." );

        // Listen to another client.
        this.ExecuteBackgroundTask( ct => this.AcceptNewClientAsync( pipeName, services, ct ), nameof(this.AcceptNewClientAsync), false );

        OnConnected( services );
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