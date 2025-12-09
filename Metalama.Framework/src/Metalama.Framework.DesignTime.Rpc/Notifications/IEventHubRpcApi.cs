// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// RPC API interface for the event hub service. Clients use this to subscribe to specific event types.
/// </summary>
[PublicAPI]
public interface IEventHubRpcApi : IRpcApi
{
    /// <summary>
    /// Subscribes to receive events of the specified types.
    /// </summary>
    /// <param name="eventTypeNames">The fully qualified type names of the events to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync( string[] eventTypeNames, [UsedImplicitly] CancellationToken cancellationToken );
}