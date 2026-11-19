// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Mcp.Protocol;

/// <summary>
/// The methods of the supervisor-to-worker protocol, and the property names of its messages.
/// </summary>
/// <remarks>
/// A request is <c>{ "id", "method", "args" }</c>. A response is <c>{ "id", "ok", "text", "error", "structured", "page", "workspace" }</c>.
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

    // IntelliSense for the query editor of a served page. They analyze the code and never run it. Their answer is in
    // Structured. The arguments are "code" and, except for Diagnostics, "position", an offset in the code.
    public const string Complete = "complete";
    public const string DescribeCompletion = "describeCompletion";
    public const string Hover = "hover";
    public const string Signature = "signature";
    public const string Diagnostics = "diagnostics";

    public const string Id = "id";
    public const string Method = "method";
    public const string Args = "args";
    public const string Ok = "ok";
    public const string Text = "text";
    public const string Error = "error";
    public const string Structured = "structured";

    /// <summary>
    /// For the html format, the JSON document of the page that the supervisor serves.
    /// </summary>
    public const string Page = "page";
    public const string Workspace = "workspace";

    /// <summary>
    /// The argument that asks the worker to return the JSON form of the result in <see cref="Structured"/>, for the MCP App view.
    /// </summary>
    public const string IncludeStructured = "includeStructured";

    /// <summary>
    /// The argument of <see cref="Load"/> that gives the MCP client, as an object with the properties
    /// <see cref="ClientName"/> and <see cref="ClientVersion"/>. The worker reports it in the usage telemetry.
    /// </summary>
    public const string Client = "client";

    public const string ClientName = "name";
    public const string ClientVersion = "version";
}
