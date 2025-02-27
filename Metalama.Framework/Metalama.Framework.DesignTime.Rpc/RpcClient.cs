// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace Metalama.Framework.DesignTime.Rpc;

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

    public event Action<RpcEventData>? EventReceived;

    protected internal virtual Task OnEventReceivedAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.EventReceived?.Invoke( eventData );

        return Task.CompletedTask;
    }

    protected Task WaitUntilInitializedAsync( CancellationToken cancellationToken ) => this._initialized.Task.WithCancellation( cancellationToken );

    internal void SetFailure( Exception exception ) => this._initialized.TrySetException( exception );
}

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