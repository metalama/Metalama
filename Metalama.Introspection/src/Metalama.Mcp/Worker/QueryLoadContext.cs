// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;
using System.Runtime.Loader;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The collectible load context of a compiled query, unloaded after the query.
/// </summary>
internal sealed class QueryLoadContext : AssemblyLoadContext
{
    public QueryLoadContext() : base( "MetalamaQuery", isCollectible: true ) { }

    // Every dependency of a query, including Metalama and the workspace, resolves from the default context.
    protected override Assembly? Load( AssemblyName assemblyName ) => null;
}
