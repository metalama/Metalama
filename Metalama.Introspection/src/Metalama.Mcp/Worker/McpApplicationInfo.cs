// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage;
using SharpCrafters.Backstage.Application;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The Backstage application information of the worker.
/// </summary>
internal sealed class McpApplicationInfo : ApplicationInfoBase
{
    public McpApplicationInfo() : base( typeof(McpApplicationInfo).Assembly, MetalamaProduct.Profile ) { }

    public override string Name => "Metalama.Mcp";
}
