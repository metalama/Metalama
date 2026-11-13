// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Framework.Workspaces.Navigation.Results;
using Metalama.Mcp.Protocol;
using Metalama.Mcp.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Handles the requests of the supervisor: loading workspaces, running queries and opening links.
/// </summary>
internal sealed class RequestHandler
{
    private readonly CommandLineOptions _options;
    private readonly WorkspaceRegistry _registry = new();
    private readonly QueryCompiler _compiler = new();

    public RequestHandler( CommandLineOptions options )
    {
        this._options = options;
    }

    public async Task<JsonObject> HandleAsync( JsonObject request, CancellationToken cancellationToken )
    {
        var method = request[WorkerMethods.Method]?.GetValue<string>();
        var args = request[WorkerMethods.Args] as JsonObject ?? new JsonObject();

        try
        {
            return method switch
            {
                WorkerMethods.Load => await this.LoadAsync( args, cancellationToken ),
                WorkerMethods.Reload => await this.ReloadAsync( args, cancellationToken ),
                WorkerMethods.Unload => this.Unload( args ),
                WorkerMethods.Query => await this.QueryAsync( args, cancellationToken ),
                WorkerMethods.Open => this.Open( args ),
                _ => Failure( $"Unknown method '{method}'." )
            };
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
        {
            return Failure( "The request was cancelled on its soft timeout." );
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            return Failure( FormatException( e ) );
        }
    }

    private async Task<JsonObject> LoadAsync( JsonObject args, CancellationToken cancellationToken )
    {
        var paths = args["paths"]?.AsArray().Select( p => p!.GetValue<string>() ).ToList() ?? [];

        if ( paths.Count == 0 )
        {
            return Failure( "Pass at least one project or solution path." );
        }

        var properties = args["properties"] is JsonObject propertiesObject
            ? propertiesObject.ToImmutableDictionary( p => p.Key, p => p.Value?.GetValue<string>() ?? "" )
            : null;

        var stopwatch = Stopwatch.StartNew();
        var entry = await this._registry.LoadAsync( paths, properties, cancellationToken );

        return DescribeWorkspace( entry, "loaded", stopwatch.Elapsed, args );
    }

    private async Task<JsonObject> ReloadAsync( JsonObject args, CancellationToken cancellationToken )
    {
        if ( !this._registry.TryGet( args["workspace"]?.GetValue<string>(), out var entry, out var error ) )
        {
            return Failure( error );
        }

        var stopwatch = Stopwatch.StartNew();
        entry = await this._registry.ReloadAsync( entry, cancellationToken );

        return DescribeWorkspace( entry, "reloaded", stopwatch.Elapsed, args );
    }

    private JsonObject Unload( JsonObject args )
    {
        if ( !this._registry.TryGet( args["workspace"]?.GetValue<string>(), out var entry, out var error ) )
        {
            return Failure( error );
        }

        this._registry.Unload( entry );

        return new JsonObject { [WorkerMethods.Ok] = true, [WorkerMethods.Workspace] = entry.Key, [WorkerMethods.Text] = $"Unloaded {entry.Key}." };
    }

    private static JsonObject DescribeWorkspace( WorkspaceRegistry.Entry entry, string verb, TimeSpan elapsed, JsonObject args )
    {
        var workspace = entry.Workspace;
        var linkBase = entry.DefaultBase;
        var text = new StringBuilder();

        text.AppendLine( CultureInfo.InvariantCulture, $"workspace: {entry.Key} ({verb} in {elapsed.TotalSeconds.ToString( "0.0", CultureInfo.InvariantCulture )} s)" );
        text.AppendLine( CultureInfo.InvariantCulture, $"base: {linkBase}" );
        text.AppendLine( "projects:" );

        foreach ( var project in workspace.Projects.OrderBy( p => p.Name, StringComparer.OrdinalIgnoreCase ).ThenBy( p => p.TargetFramework, StringComparer.Ordinal ) )
        {
            var expression = $"workspace.GetProject(\"{project.Name}\", \"{project.TargetFramework}\")";
            text.AppendLine( CultureInfo.InvariantCulture, $"  {WorkspaceLinkContext.GetScope( project ),-40}  {expression}{(project.IsMetalamaEnabled ? "  Metalama enabled" : "")}" );
        }

        var diagnostics = workspace.WorkspaceDiagnostics;
        var errors = diagnostics.Count( d => d.Severity == Framework.Diagnostics.Severity.Error );
        var warnings = diagnostics.Count( d => d.Severity == Framework.Diagnostics.Severity.Warning );
        text.AppendLine( CultureInfo.InvariantCulture, $"load diagnostics: {errors} error(s), {warnings} warning(s){(diagnostics.Length > 0 ? "; query workspace.WorkspaceDiagnostics for all of them" : "")}" );

        foreach ( var diagnostic in diagnostics.Where( d => d.Severity >= Framework.Diagnostics.Severity.Warning ).Take( 10 ) )
        {
            text.AppendLine( "  " + FormatDiagnostic( diagnostic ) );
        }

        text.Append( CultureInfo.InvariantCulture, $"fingerprint: {entry.Fingerprint}" );

        var response = new JsonObject { [WorkerMethods.Ok] = true, [WorkerMethods.Workspace] = entry.Key, [WorkerMethods.Text] = text.ToString() };

        if ( args[WorkerMethods.IncludeStructured]?.GetValue<bool>() == true )
        {
            response[WorkerMethods.Structured] = new JsonObject
            {
                ["workspace"] = entry.Key,
                ["base"] = linkBase.ToString(),
                ["projects"] = new JsonArray( workspace.Projects.Select( p => (JsonNode) WorkspaceLinkContext.GetScope( p ) ).ToArray() )
            };
        }

        return response;
    }

    private async Task<JsonObject> QueryAsync( JsonObject args, CancellationToken cancellationToken )
    {
        var code = args["code"]?.GetValue<string>();

        if ( string.IsNullOrWhiteSpace( code ) )
        {
            return Failure( "Pass the C# code of the query." );
        }

        if ( !TryGetRenderOptions( args, out var renderOptions, out var formatError ) )
        {
            return Failure( formatError );
        }

        var workspaceKey = args["workspace"]?.GetValue<string>();
        WorkspaceRegistry.Entry? entry = null;

        if ( workspaceKey != null || this._registry.Entries.Count == 1 )
        {
            if ( !this._registry.TryGet( workspaceKey, out entry, out var error ) )
            {
                return Failure( error );
            }
        }

        var stopwatch = Stopwatch.StartNew();
        var compiled = this._compiler.Compile( code );

        if ( compiled.Assembly == null )
        {
            return Failure( "The query does not compile:\n" + string.Join( "\n", compiled.Diagnostics.Where( d => d.Severity == "error" ).Select( d => "  " + d ) ) );
        }

        var linkContext = this._registry.CreateLinkContext( entry?.Key );
        var linkBase = entry?.DefaultBase;
        var queryContext = new QueryContext(
            entry?.Workspace,
            this._registry.Entries.ToDictionary( e => e.Key, e => e.Workspace, StringComparer.Ordinal ),
            linkBase,
            new LinkResolver( linkContext ),
            cancellationToken );

        WorkerInitialization.TakeReportedDiagnostics();

        var console = new StringWriter( CultureInfo.InvariantCulture );
        var originalOut = Console.Out;
        Console.SetOut( console );

        RenderedResult rendered;

        try
        {
            // The tree is built while the query assembly is loaded, because building it enumerates lazy LINQ results.
            (rendered, _) = await QueryRunner.RunAsync(
                compiled.Assembly,
                queryContext,
                value => this.RenderResult( value, null, entry, linkContext, linkBase, renderOptions, code.Trim() ) );
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
        {
            throw;
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            Console.SetOut( originalOut );

            return Failure( "The query threw " + FormatException( e ) + FormatConsole( console.ToString() ) );
        }
        finally
        {
            Console.SetOut( originalOut );
        }

        var warnings = compiled.Diagnostics.Where( d => d.Severity == "warning" && d.Line != null ).ToList();

        return CreateResponse( rendered, console.ToString(), warnings, stopwatch.Elapsed, entry, renderOptions, args );
    }

    private JsonObject Open( JsonObject args )
    {
        var linkText = args["link"]?.GetValue<string>();

        if ( string.IsNullOrWhiteSpace( linkText ) )
        {
            return Failure( "Pass the link to open." );
        }

        if ( !TryGetRenderOptions( args, out var renderOptions, out var formatError ) )
        {
            return Failure( formatError );
        }

        var workspaceKey = args["workspace"]?.GetValue<string>();
        WorkspaceRegistry.Entry? entry = null;

        if ( workspaceKey != null || this._registry.Entries.Count == 1 )
        {
            if ( !this._registry.TryGet( workspaceKey, out entry, out var error ) )
            {
                return Failure( error );
            }
        }

        var linkBase = entry?.DefaultBase ?? new LinkBase( null, null );

        if ( !LinkParser.TryParse( linkText, linkBase, out var link, out var parseError ) )
        {
            return Failure( parseError );
        }

        // An absolute link names its own workspace.
        if ( link.Workspace != null && link.Workspace != entry?.Key )
        {
            if ( !this._registry.TryGet( link.Workspace, out entry, out var error ) )
            {
                return Failure( error );
            }

            linkBase = entry.DefaultBase;
        }

        var stopwatch = Stopwatch.StartNew();
        var linkContext = this._registry.CreateLinkContext( entry?.Key );

        if ( !new LinkResolver( linkContext ).TryResolve( link, out var value, out var resolveError ) )
        {
            return Failure( resolveError );
        }

        var rendered = this.RenderResult( value, link, entry, linkContext, linkBase, renderOptions, linkText );

        return CreateResponse( rendered, "", [], stopwatch.Elapsed, entry, renderOptions, args );
    }

    private RenderedResult RenderResult(
        object? value,
        Link? link,
        WorkspaceRegistry.Entry? entry,
        WorkspaceLinkContext linkContext,
        LinkBase? linkBase,
        RenderOptions options,
        string title )
    {
        var builder = new ResultTreeBuilder( new ResultTreeOptions( options.MaxDepth, options.MaxItems, entry?.RootDirectory ), new LinkBuilder( linkContext ) );
        var tree = builder.Build( value, link );

        var needsJson = options.Format != ResultFormat.Text || options.IncludeStructured;
        var json = needsJson ? new JsonRenderer( linkBase ).RenderDocument( tree, title ) : null;
        var text = options.Format == ResultFormat.Json ? json!.ToJsonString() : new TextRenderer( linkBase ).Render( tree );

        string? htmlPath = null;

        if ( options.Format == ResultFormat.Html )
        {
            htmlPath = HtmlWriter.WriteFile( json!, title.Length > 80 ? title.Substring( 0, 79 ) + "…" : title, this._options.OutputDirectory );

            // The agent gets a short text summary, so that the page never enters its context.
            var summary = new TextRenderer( linkBase ).Render( new ResultTreeBuilder( new ResultTreeOptions( 0, 10, entry?.RootDirectory ), new LinkBuilder( linkContext ) ).Build( value, link ) );
            text = $"html: {new Uri( htmlPath ).AbsoluteUri}\n{summary}";
        }

        return new RenderedResult( text, json, htmlPath, linkBase );
    }

    private static JsonObject CreateResponse(
        RenderedResult rendered,
        string console,
        IReadOnlyList<QueryDiagnostic> warnings,
        TimeSpan elapsed,
        WorkspaceRegistry.Entry? entry,
        RenderOptions options,
        JsonObject args )
    {
        var reported = WorkerInitialization.TakeReportedDiagnostics();
        var builder = new StringBuilder();

        if ( rendered.Base != null && options.Format != ResultFormat.Json )
        {
            builder.AppendLine( CultureInfo.InvariantCulture, $"base: {rendered.Base}" );
        }

        var maxCharacters = options.Format.GetMaxCharacters();
        var body = rendered.Text;
        var truncatedBody = false;

        if ( options.Format == ResultFormat.Text && body.Length > maxCharacters )
        {
            body = body.Substring( 0, maxCharacters );
            truncatedBody = true;
        }

        builder.AppendLine( body );

        if ( console.Length > 0 )
        {
            builder.AppendLine( "--- console ---" );
            builder.AppendLine( console.Length > 4000 ? console.Substring( 0, 4000 ) + "…" : console.TrimEnd() );
        }

        if ( reported.Count > 0 )
        {
            builder.AppendLine( "--- error list ---" );

            foreach ( var diagnostic in reported.Take( 50 ) )
            {
                builder.AppendLine( FormatDiagnostic( diagnostic ) );
            }
        }

        if ( warnings.Count > 0 )
        {
            builder.AppendLine( "--- query diagnostics ---" );

            foreach ( var warning in warnings )
            {
                builder.AppendLine( warning.ToString() );
            }
        }

        var footer = new List<string> { $"{elapsed.TotalMilliseconds.ToString( "0", CultureInfo.InvariantCulture )} ms" };

        if ( entry != null )
        {
            footer.Add( "fingerprint " + entry.Fingerprint );
        }

        if ( truncatedBody )
        {
            footer.Add( $"truncated at {maxCharacters.ToString( "N0", CultureInfo.InvariantCulture )} characters; narrow the query or lower maxItems or maxDepth" );
        }

        builder.Append( CultureInfo.InvariantCulture, $"--- {string.Join( " · ", footer )} ---" );

        var response = new JsonObject { [WorkerMethods.Ok] = true, [WorkerMethods.Text] = builder.ToString().Replace( "\r\n", "\n", StringComparison.Ordinal ) };

        if ( args[WorkerMethods.IncludeStructured]?.GetValue<bool>() == true && rendered.Json != null )
        {
            response[WorkerMethods.Structured] = rendered.Json;
        }

        return response;
    }

    private static bool TryGetRenderOptions( JsonObject args, out RenderOptions options, out string? error )
    {
        options = null!;

        if ( !ResultFormatExtensions.TryParse( args["format"]?.GetValue<string>(), out var format ) )
        {
            error = $"Unknown format '{args["format"]}'. Use text, json or html.";

            return false;
        }

        var includeStructured = args[WorkerMethods.IncludeStructured]?.GetValue<bool>() == true;

        // The MCP App renders the JSON form, so it gets the depth of the HTML snapshot.
        var defaultDepth = includeStructured ? ResultFormat.Html.GetDefaultMaxDepth() : format.GetDefaultMaxDepth();
        var maxDepth = Math.Clamp( args["maxDepth"]?.GetValue<int>() ?? defaultDepth, 0, 10 );
        var maxItems = Math.Clamp( args["maxItems"]?.GetValue<int>() ?? format.GetDefaultMaxItems(), 1, 10_000 );

        options = new RenderOptions( format, maxDepth, maxItems, includeStructured );
        error = null;

        return true;
    }

    private static string FormatDiagnostic( IIntrospectionDiagnostic diagnostic )
    {
        var location = diagnostic.FilePath != null ? $" {diagnostic.FilePath}{(diagnostic.Line != null ? ":" + diagnostic.Line.Value.ToString( CultureInfo.InvariantCulture ) : "")}" : "";

        return $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}{location} {diagnostic.Message}";
    }

    private static string FormatException( Exception e )
    {
        if ( e is AggregateException { InnerExceptions.Count: 1 } aggregate )
        {
            e = aggregate.InnerExceptions[0];
        }

        if ( e is MSBuildInitializationException )
        {
            return $"{e.GetType().Name}: {e.Message} The worker needs a .NET SDK whose major version is at most the runtime's ({Environment.Version.Major}).";
        }

        // Keep the frames of the query and drop those of the worker.
        var stackTrace = string.Join(
            "\n",
            (e.StackTrace ?? "").Split( '\n' )
            .Where( l => l.Contains( QueryCompiler.QueryFileName, StringComparison.Ordinal ) || l.Contains( QueryCompiler.QueryTypeName, StringComparison.Ordinal ) )
            .Take( 5 )
            .Select( l => "  " + l.Trim() ) );

        return $"{e.GetType().Name}: {e.Message}{(stackTrace.Length > 0 ? "\n" + stackTrace : "")}";
    }

    private static string FormatConsole( string console ) => console.Length > 0 ? "\n--- console ---\n" + console.TrimEnd() : "";

    private static JsonObject Failure( string? message ) => new() { [WorkerMethods.Ok] = false, [WorkerMethods.Error] = message, [WorkerMethods.Text] = "error: " + message };

    private sealed record RenderOptions( ResultFormat Format, int MaxDepth, int MaxItems, bool IncludeStructured );

    private sealed record RenderedResult( string Text, JsonObject? Json, string? HtmlPath, LinkBase? Base );
}
