// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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