// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.Rpc;

[JsonObject]
public class RpcEventEnvelope
{
    /// <summary>
    /// Gets the name of the originating <see cref="IRpcApi"/> interface name. Used for routing.
    /// </summary>
    public string OriginatingApi { get; }

    public RpcEventData Data { get; }

    public RpcEventEnvelope( string originatingApi, RpcEventData data )
    {
        this.OriginatingApi = originatingApi;
        this.Data = data;
    }
}