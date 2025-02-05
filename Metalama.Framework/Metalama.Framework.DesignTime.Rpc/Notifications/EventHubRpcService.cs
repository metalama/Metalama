// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

[PublicAPI]
public sealed partial class EventHubRpcService : RpcService<IEventHubRpcApi>
{
    public EventHubRpcService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

    protected override IEventHubRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( eventSender );

    public Task ForwardEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
        => Task.WhenAll( this.Apis.Select( api => ((Api) api).RaiseEventAsync( eventData, cancellationToken ) ) );
}