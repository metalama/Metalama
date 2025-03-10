// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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