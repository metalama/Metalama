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
    /// <remarks>
    /// The method iterates <see cref="RpcService{TApi}.RegisteredApis"/> and not <see cref="RpcService{TApi}.Apis"/>. A client
    /// subscribes during its connection, and the server handles the subscription before it adds the connection to
    /// <see cref="RpcService{TApi}.Apis"/>. An event forwarded between these two steps would otherwise be lost, although the
    /// connection of the client has already completed (issue #2364). Iterating the connections that have not yet been promoted
    /// is safe because the implementation of <see cref="IEventHubRpcApi"/> sends an event only to a client that has subscribed to its type, and the server has
    /// started listening on any connection that has received a subscription.
    /// </remarks>
    public Task ForwardEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
        => Task.WhenAll(
            this.RegisteredApis.Select(
                api => this.SafeBroadcastInvokeAsync( ct => ((Api) api).RaiseEventAsync( eventData, ct ), cancellationToken ) ) );
}