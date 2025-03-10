// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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