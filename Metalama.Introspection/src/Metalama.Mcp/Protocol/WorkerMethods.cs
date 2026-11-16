// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Mcp.Protocol;

/// <summary>
/// The methods of the supervisor-to-worker protocol, and the property names of its messages.
/// </summary>
/// <remarks>
/// A request is <c>{ "id", "method", "args" }</c>. A response is <c>{ "id", "ok", "text", "error", "structured", "workspace" }</c>.
/// <see cref="Cancel"/> has no response: it cancels the request in progress.
/// </remarks>
internal static class WorkerMethods
{
    public const string Load = "load";
    public const string Reload = "reload";
    public const string Unload = "unload";
    public const string Query = "query";
    public const string Open = "open";
    public const string Cancel = "cancel";

    public const string Id = "id";
    public const string Method = "method";
    public const string Args = "args";
    public const string Ok = "ok";
    public const string Text = "text";
    public const string Error = "error";
    public const string Structured = "structured";
    public const string Workspace = "workspace";

    /// <summary>
    /// The argument that asks the worker to return the JSON form of the result in <see cref="Structured"/>, for the MCP App view.
    /// </summary>
    public const string IncludeStructured = "includeStructured";
}
