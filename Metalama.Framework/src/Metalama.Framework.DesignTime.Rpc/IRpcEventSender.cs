// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Interface for sending events from server services to connected clients.
/// Implementations are provided by <see cref="RpcService{TApi}"/> when creating API instances.
/// </summary>
public interface IRpcEventSender
{
    /// <summary>
    /// Raises an event to be sent to the connected client.
    /// </summary>
    /// <param name="eventData">The event data to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken );
}