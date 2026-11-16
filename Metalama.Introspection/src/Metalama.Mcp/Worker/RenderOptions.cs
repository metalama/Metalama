// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Results;
using Metalama.Mcp.Rendering;
using System;

namespace Metalama.Mcp.Worker;

/// <summary>
/// How a result is rendered.
/// </summary>
/// <param name="Format">The format requested by the call.</param>
/// <param name="MaxDepth">The depth of the requested format.</param>
/// <param name="MaxItems">The number of items per collection of the requested format.</param>
/// <param name="IncludeStructured">Whether the JSON form is also returned for the MCP App view.</param>
internal sealed record RenderOptions( ResultFormat Format, int MaxDepth, int MaxItems, bool IncludeStructured )
{
    /// <summary>
    /// Gets the limits of the JSON form returned for the MCP App view, which has the depth of the HTML snapshot, or
    /// <c>null</c> when no JSON form is returned.
    /// </summary>
    public ResultTreeOptions? GetStructuredLimits( string? rootDirectory )
        => this.IncludeStructured
            ? new ResultTreeOptions(
                Math.Max( this.MaxDepth, ResultFormat.Html.GetDefaultMaxDepth() ),
                Math.Max( this.MaxItems, ResultFormat.Html.GetDefaultMaxItems() ),
                rootDirectory )
            : null;
}
