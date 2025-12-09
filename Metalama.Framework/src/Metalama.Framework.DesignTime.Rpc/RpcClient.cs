// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Abstract base class for RPC client proxies. Client proxies provide access to remote services
/// exposed by the server and receive events from those services. Derive from <see cref="RpcClient{TApi}"/>
/// to create a typed client proxy for a specific API interface.
/// </summary>
public abstract class RpcClient
{
    private readonly TaskCompletionSource<bool> _initialized = new();

    internal abstract Type InterfaceType { get; }

    internal string InterfaceName => this.InterfaceType.Name;

    protected ILogger Logger { get; }

    protected RpcClient( ClientEndpoint endpoint )
    {
        // The endpoint is unused but pulling it back if removed is a lot of work, so we keep it flowing.
        _ = endpoint;

        this.Logger = endpoint.LoggerFactory.GetLogger( this.GetType().Name );
    }

    protected internal abstract void ConfigureRpc( JsonRpc rpc );

    protected internal virtual Task OnRpcConnectedAsync( CancellationToken cancellationToken )
    {
        this._initialized.SetResult( true );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Occurs when an event is received from the server for this client's API.
    /// </summary>
    public event Action<RpcEventData>? EventReceived;

    /// <summary>
    /// Called when an event is received from the server. Override to process events before raising <see cref="EventReceived"/>.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected internal virtual Task OnEventReceivedAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.EventReceived?.Invoke( eventData );

        return Task.CompletedTask;
    }

    protected Task WaitUntilInitializedAsync( CancellationToken cancellationToken ) => this._initialized.Task.WithCancellation( cancellationToken );

    internal void SetFailure( Exception exception ) => this._initialized.TrySetException( exception );
}

/// <summary>
/// A typed RPC client proxy that provides access to a remote API of type <typeparamref name="TApi"/>.
/// </summary>
/// <typeparam name="TApi">The API interface type. Must implement <see cref="IRpcApi"/>.</typeparam>
public abstract class RpcClient<TApi> : RpcClient
    where TApi : class, IRpcApi
{
    private TApi? _remoteApi;

    protected RpcClient( ClientEndpoint endpoint ) : base( endpoint ) { }

    internal override Type InterfaceType => typeof(TApi);

    /// <summary>
    /// Gets the remote API or throws an <see cref="InvalidOperationException"/> if it is not yet available.
    /// For safety, use <see cref="GetApiAsync"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TApi GetApiDangerous() => this._remoteApi ?? throw new InvalidOperationException();

    /// <summary>
    /// Gets the remote API after waiting for the client to be initialized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The remote API proxy.</returns>
    /// <exception cref="InvalidOperationException">Thrown if initialization failed.</exception>
    public async ValueTask<TApi> GetApiAsync( CancellationToken cancellationToken = default )
    {
        await this.WaitUntilInitializedAsync( cancellationToken );

        return this._remoteApi ?? throw new InvalidOperationException();
    }

    protected internal override void ConfigureRpc( JsonRpc rpc )
    {
        this._remoteApi = rpc.Attach<TApi>();
    }
}