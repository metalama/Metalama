// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Mcp.Protocol;
using Metalama.Mcp.Rendering;
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
internal sealed class McpTools : IAsyncDisposable
{
    public const string ViewerResourceUri = "ui://metalama/viewer";

    /// <summary>
    /// The key of the MCP Apps extension in the client capabilities.
    /// </summary>
    public const string AppsExtensionId = "io.modelcontextprotocol/ui";

    /// <summary>
    /// The property of the JSON form, in the MCP App view only, that holds the link of the same result on the result server.
    /// </summary>
    public const string PageUrlProperty = "pageUrl";

    private static readonly bool _reportAssemblies = Environment.GetEnvironmentVariable( "METALAMA_MCP_REPORT_ASSEMBLIES" ) == "1";

    private readonly WorkerClient _worker;
    private readonly CommandLineOptions _options;
    private readonly ResultCache _results = new();
    private readonly SemaphoreSlim _serverLock = new( 1, 1 );
    private ResultServer? _server;

    public McpTools( WorkerClient worker, CommandLineOptions options )
    {
        this._worker = worker;
        this._options = options;
    }

    /// <summary>
    /// Gets the result server, which is started by the first html result, so that a session that never asks for html never
    /// listens on a port. A missing certificate is not remembered, so that the user can install one and retry.
    /// </summary>
    private async Task<(ResultServer? Server, string? Error)> GetServerAsync( CancellationToken cancellationToken )
    {
        if ( this._options.NoHttp )
        {
            return (null, $"The html format is disabled by the {CommandLineOptions.NoHttpOption} option of the server. Use the text or json format.");
        }

        await this._serverLock.WaitAsync( cancellationToken );

        try
        {
            if ( this._server == null )
            {
                if ( !DevelopmentCertificate.TryGet( out var certificate, out var error ) )
                {
                    return (null, error);
                }

                this._server = await ResultServer.StartAsync( this._worker, this._results, certificate, cancellationToken );
            }

            return (this._server, null);
        }
        finally
        {
            this._serverLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if ( this._server != null )
        {
            await this._server.DisposeAsync();
        }

        this._serverLock.Dispose();
    }

    public IEnumerable<McpServerTool> CreateTools()
    {
        // Loading runs MSBuild targets, aspects and a NuGet restore; a query runs arbitrary code. Their destructive and
        // open-world hints are left to the default of the specification, which is true, so that clients ask for permission.
        yield return this.Create( nameof(this.LoadWorkspace), "load_workspace", "Load workspace", runsCode: true, ViewerAccess.None );
        yield return this.Create( nameof(this.ReloadWorkspace), "reload_workspace", "Reload workspace", runsCode: true, ViewerAccess.None );
        yield return this.Create( nameof(this.UnloadWorkspace), "unload_workspace", "Unload workspace", runsCode: false, ViewerAccess.None );

        // The viewer renders the result of a query but cannot run a query: only the model can.
        yield return this.Create( nameof(this.Query), "query", "Run a C# query", runsCode: true, ViewerAccess.Render );

        // The viewer calls open to drill down. Following a link runs no code today, because query anchors are not
        // supported yet, but it will once they are.
        yield return this.Create( nameof(this.Open), "open", "Open a link", runsCode: true, ViewerAccess.RenderAndCall );
    }

    private McpServerTool Create( string methodName, string name, string title, bool runsCode, ViewerAccess viewerAccess )
    {
        var method = typeof(McpTools).GetMethod( methodName )!;
        var description = method.GetCustomAttributes( typeof(DescriptionAttribute), false ).OfType<DescriptionAttribute>().Single().Description;

        var options = new McpServerToolCreateOptions { Name = name, Title = title, Description = description, ReadOnly = false };

        if ( !runsCode )
        {
            options.Destructive = false;
            options.OpenWorld = false;
        }

        if ( viewerAccess != ViewerAccess.None )
        {
            // MCP Apps: hosts that support the extension render the result with the viewer. A tool is callable from the
            // viewer only when its visibility includes "app". Other hosts ignore this metadata.
            var visibility = viewerAccess == ViewerAccess.RenderAndCall ? new JsonArray( "model", "app" ) : new JsonArray( "model" );
            options.Meta = new JsonObject { ["ui"] = new JsonObject { ["resourceUri"] = ViewerResourceUri, ["visibility"] = visibility } };
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
        "Runs a C# query: an expression, whose value is rendered, or statements ending with `return`. Globals: `workspace`, a "
        + "`Metalama.Framework.Workspaces.Workspace` with `SourceCode` and `TransformedCode` (`ICompilationSet`) and the introspection API, "
        + "`workspaces`, `D(id)` and `D<T>(id)` to resolve a declaration ID, `Open(link)` and `Open<T>(link)` to resolve a link, and `ct`, a cancellation token. "
        + "Declarations are the Metalama code model of `Metalama.Framework.Code` (`INamedType`, `IMethod`, …). "
        + "For the object model, use the Metalama skill if you have it, or read https://doc.postsharp.net/metalama/api/introspection-api "
        + "and https://doc.postsharp.net/metalama/api/metalama-framework-workspaces-workspace. "
        + "Every rendered object starts with a link that `open` or `Open(…)` accepts. "
        + "In projections, select the declaration itself (`new { Method = m, … }`), never a string built from its name: only a declaration keeps its link and location, "
        + "which is what a person follows in an html page. This runs arbitrary code with the user's privileges." )]
    public Task<CallToolResult> Query(
        McpServer server,
        [Description( "The C# expression or statements." )]
        string code,
        [Description( "The workspace key. Optional when a single workspace is loaded." )]
        string? workspace = null,
        [Description( "Items shown per collection. Default: 50. For html, only a value above 500 has an effect; omit it." )]
        int? maxItems = null,
        [Description( "Levels of properties expanded below the result. Default: 1. For html, only a value above 3 has an effect; omit it." )]
        int? maxDepth = null,
        [Description( "Output format: text (default), json, or html (returns an https://localhost link to a LINQPad-style page for the user to open; you get a short summary)." )]
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
        [Description( "Items shown per collection. Default: 50. For html, only a value above 500 has an effect; omit it." )]
        int? maxItems = null,
        [Description( "Levels of properties expanded below the result. Default: 1. For html, only a value above 3 has an effect; omit it." )]
        int? maxDepth = null,
        [Description( "Output format: text (default), json, or html (returns an https://localhost link for the user to open)." )]
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

        // An html result is served, so the server must be able to serve it before the query runs.
        ResultServer? resultServer = null;

        if ( ResultFormatExtensions.TryParse( args["format"]?.GetValue<string>(), out var format ) && format == ResultFormat.Html )
        {
            (resultServer, var error) = await this.GetServerAsync( cancellationToken );

            if ( resultServer == null )
            {
                return new CallToolResult { Content = [new TextContentBlock { Text = error! }], IsError = true };
            }
        }

        var response = await this._worker.CallAsync( method, args, timeout, cancellationToken );
        var text = response[WorkerMethods.Text]?.GetValue<string>() ?? response[WorkerMethods.Error]?.GetValue<string>() ?? "";
        Uri? pageUrl = null;

        if ( resultServer != null && response[WorkerMethods.Page] is JsonObject page )
        {
            // The agent gets the link and a short summary; the person opens the link.
            pageUrl = resultServer.GetResultUri( this._results.Add( page ) );
            text = $"html: {pageUrl}\n{text}";
        }

        var result = new CallToolResult { Content = [new TextContentBlock { Text = text }], IsError = response[WorkerMethods.Ok]?.GetValue<bool>() != true };

        if ( includeStructured && response[WorkerMethods.Structured] is JsonObject structured )
        {
            // Some hosts render the MCP App view but do not forward its tools/call to the server, so the view cannot drill
            // down. Once the result server runs, every result shown in the view also gets a served page, which the view
            // offers to open in the browser. The server is not started for this: only an html result starts it.
            if ( pageUrl == null && this._server is { } runningServer )
            {
                pageUrl = runningServer.GetResultUri( this._results.Add( (JsonObject) structured.DeepClone() ) );
            }

            if ( pageUrl != null )
            {
                structured[PageUrlProperty] = pageUrl.ToString();
            }

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
