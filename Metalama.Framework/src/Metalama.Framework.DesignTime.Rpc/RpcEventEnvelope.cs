// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Wraps event data with routing information for delivery from server to client.
/// The envelope contains the originating API name so the client can route the event to the correct handler.
/// </summary>
[JsonObject]
public class RpcEventEnvelope
{
    /// <summary>
    /// Gets the name of the originating <see cref="IRpcApi"/> interface. Used for routing events to the correct client handler.
    /// </summary>
    public string OriginatingApi { get; }

    /// <summary>
    /// Gets the event data payload.
    /// </summary>
    public RpcEventData Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcEventEnvelope"/> class.
    /// </summary>
    /// <param name="originatingApi">The name of the originating API interface.</param>
    /// <param name="data">The event data payload.</param>
    public RpcEventEnvelope( string originatingApi, RpcEventData data )
    {
        this.OriginatingApi = originatingApi;
        this.Data = data;
    }
}