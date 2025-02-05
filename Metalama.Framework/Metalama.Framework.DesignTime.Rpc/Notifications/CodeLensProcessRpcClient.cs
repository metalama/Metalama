// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

public sealed class CodeLensProcessRpcClient : RpcClient<IEventHubRpcApi>
{
    private readonly ILogger _logger;

    internal CodeLensProcessRpcClient( ClientEndpoint endpoint, IServiceProvider serviceProvider ) : base( endpoint )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(CodeLensProcessRpcClient) );
    }

    protected internal override async Task OnRpcConnectedAsync( CancellationToken cancellationToken )
    {
        this._logger.Trace?.Log( "Registering for notifications." );
        await this.GetApiDangerous().SubscribeAsync( [nameof(CompilationResultChangedEventData), nameof(EndpointChangedEventData)], cancellationToken );
        this._logger.Trace?.Log( "Registering for notifications: completed." );
    }
}