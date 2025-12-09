// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// RPC client for the CodeLens process that subscribes to compilation and endpoint change events
/// from the event hub.
/// </summary>
public sealed class CodeLensProcessRpcClient : RpcClient<IEventHubRpcApi>
{
    private readonly ILogger _logger;

    internal CodeLensProcessRpcClient( ClientEndpoint endpoint, IServiceProvider serviceProvider ) : base( endpoint )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(CodeLensProcessRpcClient) );
    }

    /// <summary>
    /// Called when the RPC connection is established. Subscribes to compilation and endpoint change events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected internal override async Task OnRpcConnectedAsync( CancellationToken cancellationToken )
    {
        this._logger.Trace?.Log( "Registering for notifications." );
        await this.GetApiDangerous().SubscribeAsync( [nameof(CompilationResultChangedEventData), nameof(EndpointChangedEventData)], cancellationToken );
        this._logger.Trace?.Log( "Registering for notifications: completed." );
    }
}