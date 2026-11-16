// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Mcp.Protocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// The MCP tools. Each forwards its call to the worker and converts the response to a tool result.
/// </summary>
internal sealed class McpTools
{
    public const string ViewerResourceUri = "ui://metalama/viewer";

    /// <summary>
    /// The key of the MCP Apps extension in the client capabilities.
    /// </summary>
    public const string AppsExtensionId = "io.modelcontextprotocol/ui";

    private static readonly bool _reportAssemblies = Environment.GetEnvironmentVariable( "METALAMA_MCP_REPORT_ASSEMBLIES" ) == "1";

    private readonly WorkerClient _worker;

    public McpTools( WorkerClient worker )
    {
        this._worker = worker;
    }

    public IEnumerable<McpServerTool> CreateTools()
    {
        yield return this.Create( nameof(this.LoadWorkspace), "load_workspace", "Load workspace", readOnly: false, withViewer: false );
        yield return this.Create( nameof(this.ReloadWorkspace), "reload_workspace", "Reload workspace", readOnly: false, withViewer: false );
        yield return this.Create( nameof(this.UnloadWorkspace), "unload_workspace", "Unload workspace", readOnly: false, withViewer: false );
        yield return this.Create( nameof(this.Query), "query", "Run a C# query", readOnly: false, withViewer: true );

        // Following a link runs no code today, because query anchors are not supported yet, but it will once they are.
        yield return this.Create( nameof(this.Open), "open", "Open a link", readOnly: false, withViewer: true );
    }

    private McpServerTool Create( string methodName, string name, string title, bool readOnly, bool withViewer )
    {
        var method = typeof(McpTools).GetMethod( methodName )!;
        var description = method.GetCustomAttributes( typeof(DescriptionAttribute), false ).OfType<DescriptionAttribute>().Single().Description;

        var options = new McpServerToolCreateOptions
        {
            Name = name,
            Title = title,
            Description = description,
            ReadOnly = readOnly,
            Destructive = false,
            OpenWorld = false
        };

        if ( withViewer )
        {
            // MCP Apps: hosts that support the extension render the result with the viewer, and the viewer may call the
            // tool itself to drill down. Other hosts ignore this metadata.
            options.Meta = new JsonObject { ["ui"] = new JsonObject { ["resourceUri"] = ViewerResourceUri, ["visibility"] = new JsonArray( "model", "app" ) } };
        }

        return McpServerTool.Create( method, this, options );
    }

    [UsedImplicitly]
    [Description(
        "Loads C# projects or solutions (.csproj, .sln, .slnx) into a workspace and keeps them loaded for the session. Returns the workspace key, "
        + "its projects with target frameworks, the default link base, and load diagnostics. Loading builds the projects' compile-time code, "
        + "which runs MSBuild targets and Metalama aspects of the target repository: only load code you trust." )]
    public Task<CallToolResult> LoadWorkspace(
        [Description( "Paths of the projects or solutions, absolute or relative to the server's working directory." )]
        string[] paths,
        [Description( "Optional MSBuild properties, such as Configuration." )]
        Dictionary<string, string>? properties = null,
        CancellationToken cancellationToken = default )
    {
        var args = new JsonObject { ["paths"] = new JsonArray( paths.Select( p => (JsonNode) p ).ToArray() ) };

        if ( properties is { Count: > 0 } )
        {
            args["properties"] = new JsonObject( properties.Select( p => KeyValuePair.Create( p.Key, (JsonNode?) p.Value ) ) );
        }

        return this.CallAsync( null, WorkerMethods.Load, args, null, cancellationToken );
    }

    [UsedImplicitly]
    [Description( "Reloads a workspace from disk, after its files were edited. Links to declarations stay valid; links with an index step become stale." )]
    public Task<CallToolResult> ReloadWorkspace(
        [Description( "The workspace key. Optional when a single workspace is loaded." )]
        string? workspace = null,
        CancellationToken cancellationToken = default )
        => this.CallAsync( null, WorkerMethods.Reload, new JsonObject { ["workspace"] = workspace }, null, cancellationToken );

    [UsedImplicitly]
    [Description( "Unloads a workspace to free memory." )]
    public Task<CallToolResult> UnloadWorkspace(
        [Description( "The workspace key." )] string workspace,
        CancellationToken cancellationToken = default )
        => this.CallAsync( null, WorkerMethods.Unload, new JsonObject { ["workspace"] = workspace }, null, cancellationToken );

    [UsedImplicitly]
    [Description(
        "Runs a C# query: an expression, whose value is rendered, or statements ending with `return`. Globals: `workspace` (the loaded workspace), "
        + "`workspaces`, `D(id)` and `D<T>(id)` to resolve a declaration ID, `Open(link)` and `Open<T>(link)` to resolve a link, and `ct`, a cancellation token. "
        + "Every rendered object starts with a link that `open` or `Open(…)` accepts. This runs arbitrary code with the user's privileges." )]
    public Task<CallToolResult> Query(
        McpServer server,
        [Description( "The C# expression or statements." )]
        string code,
        [Description( "The workspace key. Optional when a single workspace is loaded." )]
        string? workspace = null,
        [Description( "Items shown per collection. Default: 50 (500 for html)." )]
        int? maxItems = null,
        [Description( "Levels of properties expanded below the result. Default: 1 (3 for html)." )]
        int? maxDepth = null,
        [Description( "Output format: text (default), json, or html (writes a self-contained file and returns its path)." )]
        string? format = null,
        [Description( "Soft timeout in seconds; the worker is stopped after twice this time. Default: 60." )]
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default )
    {
        var args = new JsonObject
        {
            ["code"] = code,
            ["workspace"] = workspace,
            ["maxItems"] = maxItems,
            ["maxDepth"] = maxDepth,
            ["format"] = format
        };

        return this.CallAsync( server, WorkerMethods.Query, args, timeoutSeconds is > 0 ? TimeSpan.FromSeconds( timeoutSeconds.Value ) : null, cancellationToken );
    }

    [UsedImplicitly]
    [Description(
        "Opens a link printed by a previous result, such as `T:MyApp.Customer`, `T:MyApp.Customer/Methods` or `metalama://…`, and renders the object it names. "
        + "Relative links are resolved against the base of the workspace." )]
    public Task<CallToolResult> Open(
        McpServer server,
        [Description( "The link." )] string link,
        [Description( "The workspace key. Optional when a single workspace is loaded or the link is absolute." )]
        string? workspace = null,
        [Description( "Items shown per collection. Default: 50 (500 for html)." )]
        int? maxItems = null,
        [Description( "Levels of properties expanded below the result. Default: 1 (3 for html)." )]
        int? maxDepth = null,
        [Description( "Output format: text (default), json, or html." )]
        string? format = null,
        CancellationToken cancellationToken = default )
    {
        var args = new JsonObject
        {
            ["link"] = link,
            ["workspace"] = workspace,
            ["maxItems"] = maxItems,
            ["maxDepth"] = maxDepth,
            ["format"] = format
        };

        return this.CallAsync( server, WorkerMethods.Open, args, null, cancellationToken );
    }

    private async Task<CallToolResult> CallAsync( McpServer? server, string method, JsonObject args, TimeSpan? timeout, CancellationToken cancellationToken )
    {
        // The JSON form is only produced for hosts that render the MCP App view, so that other hosts do not receive the
        // result twice.
        var includeStructured = server?.ClientCapabilities?.Extensions?.ContainsKey( AppsExtensionId ) == true;
        args[WorkerMethods.IncludeStructured] = includeStructured;

        var response = await this._worker.CallAsync( method, args, timeout, cancellationToken );

        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = response[WorkerMethods.Text]?.GetValue<string>() ?? response[WorkerMethods.Error]?.GetValue<string>() ?? "" }],
            IsError = response[WorkerMethods.Ok]?.GetValue<bool>() != true
        };

        if ( includeStructured && response[WorkerMethods.Structured] is { } structured )
        {
            result.StructuredContent = JsonSerializer.SerializeToElement( structured );
        }

        if ( _reportAssemblies )
        {
            // Lets tests check that the supervisor never loads the code model, Roslyn or MSBuild.
            await Console.Error.WriteLineAsync(
                "metalama-mcp: supervisor assemblies=" + string.Join( ";", AppDomain.CurrentDomain.GetAssemblies().Select( a => a.GetName().Name ) ) );
        }

        return result;
    }
}
