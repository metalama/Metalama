// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// Server-side RPC service that acts as an event hub. Receives events and forwards them to subscribed clients
/// based on their event type subscriptions.
/// </summary>
[PublicAPI]
public sealed partial class EventHubRpcService : RpcService<IEventHubRpcApi>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubRpcService"/> class.
    /// </summary>
    /// <param name="serverEndpoint">The server endpoint this service belongs to.</param>
    public EventHubRpcService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

    /// <inheritdoc />
    protected override IEventHubRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( eventSender );

    /// <summary>
    /// Forwards an event to all connected clients that have subscribed to the event's type.
    /// </summary>
    /// <param name="eventData">The event data to forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ForwardEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
        => Task.WhenAll( this.Apis.Select( api => ((Api) api).RaiseEventAsync( eventData, cancellationToken ) ) );
}