// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.Rpc;

[JsonObject]
public abstract class RpcEventData
{
    /// <summary>
    /// Gets the name event category. Allows for filtering.
    /// </summary>
    public abstract string Category { get; }
}