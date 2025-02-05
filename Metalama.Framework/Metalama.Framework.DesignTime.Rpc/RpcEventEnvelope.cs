// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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