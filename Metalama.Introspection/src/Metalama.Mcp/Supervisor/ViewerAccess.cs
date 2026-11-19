// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// What the MCP App view can do with a tool.
/// </summary>
internal enum ViewerAccess
{
    /// <summary>
    /// The result is not shown in the view.
    /// </summary>
    None,

    /// <summary>
    /// The view renders the result, but only the model can call the tool.
    /// </summary>
    Render,

    /// <summary>
    /// The view renders the result and can call the tool itself.
    /// </summary>
    RenderAndCall
}
