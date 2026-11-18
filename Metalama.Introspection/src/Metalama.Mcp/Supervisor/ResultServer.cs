// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This file is the only place where the server listens on a port. See BannedSymbols.txt and section 4.1 of the design.
#pragma warning disable RS0030

using Metalama.Mcp.Protocol;
using Metalama.Mcp.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Serves html results to the browser of the user, on <c>https://localhost</c> with a random port, and renders on demand what
/// the page expands. It shows the documents of <see cref="ResultCache"/>, follows links with the <c>open</c> method of the
/// worker, as the MCP App view does, and runs the query that the user edits in a page (<see cref="ServeQueryAsync"/>).
/// </summary>
/// <remarks>
/// <para>Every URL starts with a random token that only the agent and the user receive, so other local processes and web pages
/// cannot reach it. The <c>Host</c> header must be <c>localhost</c>, which defeats DNS rebinding, and only GET is accepted.</para>
/// <para>The server is built with <see cref="WebApplication.CreateEmptyBuilder"/>: it reads no configuration file and no
/// environment variable, so nothing in the working directory can change where it listens, and it has no logging, so nothing
/// reaches standard output, which carries MCP.</para>
/// </remarks>
internal sealed class ResultServer : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly WorkerClient _worker;
    private readonly ResultCache _cache;
    private readonly byte[] _token;
    private readonly int _tokenLength;
    private readonly string _host;
    private readonly string _origin;

    private ResultServer( WebApplication application, WorkerClient worker, ResultCache cache, byte[] token, int port )
    {
        this._application = application;
        this._worker = worker;
        this._cache = cache;
        this._token = token;
        this._host = "localhost:" + port.ToString( CultureInfo.InvariantCulture );
        this._origin = "https://" + this._host;
        var encodedToken = ResultCache.Base64Url( token );
        this._tokenLength = encodedToken.Length;
        this.BaseUri = new Uri( $"https://{this._host}/{encodedToken}/" );
    }

    /// <summary>
    /// Gets the URL under which every page is served, with the token.
    /// </summary>
    public Uri BaseUri { get; }

    public Uri GetResultUri( string id ) => new( this.BaseUri, "r/" + id );

    /// <summary>
    /// The property of a page that holds the code of its query.
    /// </summary>
    public const string QueryProperty = "query";

    private const int _maxQueryRequestLength = 1024 * 1024;

    private static readonly TimeSpan _intelliSenseTimeout = TimeSpan.FromSeconds( 60 );

    /// <summary>
    /// Stores a page and returns its URL. The page of a query keeps the code, so that the user can edit it and run it again.
    /// </summary>
    public Uri Store( JsonObject page, string? query )
    {
        if ( query != null )
        {
            // The worker already gives the page its query, laid out on several lines.
            page[QueryProperty] ??= query;
        }

        return this.GetResultUri( this._cache.Add( page ) );
    }

    public static async Task<ResultServer> StartAsync( WorkerClient worker, ResultCache cache, X509Certificate2 certificate, CancellationToken cancellationToken )
    {
        var token = RandomNumberGenerator.GetBytes( 32 );

        // Kestrel cannot listen on the same random port for IPv4 and IPv6, so a port is drawn from the dynamic range, and
        // drawn again when it is taken.
        for ( var attempt = 0; ; attempt++ )
        {
            var port = RandomNumberGenerator.GetInt32( 49152, 65536 );
            var builder = WebApplication.CreateEmptyBuilder( new WebApplicationOptions() );

            builder.WebHost.UseKestrelCore()
                .ConfigureKestrel(
                    options =>
                    {
                        options.AddServerHeader = false;

                        // 127.0.0.1 and ::1 only.
                        options.ListenLocalhost( port, listen => listen.UseHttps( certificate ) );
                    } );

            var application = builder.Build();
            var server = new ResultServer( application, worker, cache, token, port );
            application.Run( server.HandleAsync );

            try
            {
                await application.StartAsync( cancellationToken );

                return server;
            }
            catch ( IOException ) when ( attempt < 10 )
            {
                await application.DisposeAsync();
            }
        }
    }

    private async Task HandleAsync( HttpContext context )
    {
        var response = context.Response;
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers.CacheControl = "no-store";

        if ( !string.Equals( context.Request.Host.Value, this._host, StringComparison.OrdinalIgnoreCase ) )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "Bad request." );

            return;
        }

        var segments = (context.Request.Path.Value ?? "").Trim( '/' ).Split( '/' );

        if ( segments.Length < 2 || !this.IsToken( segments[0] ) )
        {
            await WriteTextAsync( context, StatusCodes.Status404NotFound, "Not found." );

            return;
        }

        // Running a query and the IntelliSense of the query editor are POST, accepted only from a page of this server
        // (ReadPostAsync). Everything else is GET.
        var postMethod = segments is [_, var name] ? GetPostMethod( name ) : null;
        var isPost = segments is [_, "query"] || postMethod != null;

        if ( isPost ? !HttpMethods.IsPost( context.Request.Method ) : !HttpMethods.IsGet( context.Request.Method ) )
        {
            response.Headers.Allow = isPost ? "POST" : "GET";
            await WriteTextAsync( context, StatusCodes.Status405MethodNotAllowed, isPost ? "Only POST is accepted." : "Only GET is accepted." );

            return;
        }

        if ( postMethod != null )
        {
            await this.ServeIntelliSenseAsync( context, postMethod );

            return;
        }

        switch ( segments[1] )
        {
            case "query":
                await this.ServeQueryAsync( context );

                break;

            case "assets" when segments.Length == 4:
                await ServeAssetAsync( context, segments[2], segments[3] );

                break;

            case "r" when segments.Length == 3:
                await this.ServeResultAsync( context, segments[2] );

                break;

            case "open" when segments.Length == 2:
                await this.ServeOpenAsync( context );

                break;

            case "view" when segments.Length == 2:
                await this.ServeViewAsync( context );

                break;

            default:
                await WriteTextAsync( context, StatusCodes.Status404NotFound, "Not found." );

                break;
        }
    }

    private bool IsToken( string segment )
    {
        // The length of the token is not a secret. Decoding throws on characters outside the alphabet.
        if ( segment.Length != this._tokenLength )
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[64];

        try
        {
            return System.Buffers.Text.Base64Url.TryDecodeFromChars( segment, buffer, out var length )
                   && CryptographicOperations.FixedTimeEquals( buffer.Slice( 0, length ), this._token );
        }
        catch ( FormatException )
        {
            return false;
        }
    }

    private async Task ServeResultAsync( HttpContext context, string id )
    {
        if ( !this._cache.TryGet( id, out var document ) )
        {
            await WriteTextAsync( context, StatusCodes.Status404NotFound, "This result is no longer available: the session that produced it ended, or newer results replaced it. Run the query again." );

            return;
        }

        var title = document["title"]?.GetValue<string>() ?? "Metalama";
        title = title.Length > 80 ? title.Substring( 0, 79 ) + "…" : title;

        // A download is a snapshot that works without the server, which is gone when the file is opened later.
        var download = context.Request.Query["download"] == "1";

        if ( download )
        {
            context.Response.Headers.ContentDisposition = $"attachment; filename=\"metalama-{DateTime.Now.ToString( "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture )}.html\"";
        }

        context.Response.Headers.ContentSecurityPolicy = HtmlWriter.GetContentSecurityPolicy( served: !download );
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync( HtmlWriter.CreatePage( document, title, served: !download ), Encoding.UTF8, context.RequestAborted );
    }

    private async Task ServeOpenAsync( HttpContext context )
    {
        var link = context.Request.Query["link"].ToString();

        if ( link.Length == 0 )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The link parameter is missing." );

            return;
        }

        // The same call as the drill-down of the MCP App view. Query anchors, which would run code, are rejected by the worker.
        var args = new JsonObject { ["link"] = link, ["format"] = "json", [WorkerMethods.IncludeStructured] = true };

        if ( int.TryParse( context.Request.Query["maxItems"], NumberStyles.None, CultureInfo.InvariantCulture, out var maxItems ) )
        {
            args["maxItems"] = maxItems;
        }

        var result = await this._worker.CallAsync( WorkerMethods.Open, args, null, context.RequestAborted );

        if ( result[WorkerMethods.Ok]?.GetValue<bool>() != true || result[WorkerMethods.Structured] is not JsonObject document )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, result[WorkerMethods.Error]?.GetValue<string>() ?? "The link cannot be opened." );

            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync( document.ToJsonString(), Encoding.UTF8, context.RequestAborted );
    }

    /// <summary>
    /// Opens a link on a page of its own: the worker renders it with the html format, as the <c>open</c> tool does, and the
    /// browser is sent to the stored page. A page uses it to open a declaration in a new window. Query anchors, which would
    /// run code, are rejected by the worker.
    /// </summary>
    private async Task ServeViewAsync( HttpContext context )
    {
        var link = context.Request.Query["link"].ToString();

        if ( link.Length == 0 )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The link parameter is missing." );

            return;
        }

        var args = new JsonObject { ["link"] = link, ["format"] = "html", [WorkerMethods.IncludeStructured] = false };
        var result = await this._worker.CallAsync( WorkerMethods.Open, args, null, context.RequestAborted );

        if ( result[WorkerMethods.Ok]?.GetValue<bool>() != true || result[WorkerMethods.Page] is not JsonObject page )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, result[WorkerMethods.Error]?.GetValue<string>() ?? "The link cannot be opened." );

            return;
        }

        // A relative redirect, from <token>/view to <token>/r/<id>, so that the Host checked above is kept.
        context.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = "r/" + this.Store( page, null ).Segments[^1];
    }

    /// <summary>
    /// Runs the query that the user edited in a page, and returns the URL of the page of its result.
    /// </summary>
    /// <remarks>
    /// This runs code, so beyond the token, the request must come from a page of this server: its <c>Origin</c> must be the
    /// origin of the server, and the browser must not mark it as cross-site. The body must be JSON, which a page of another
    /// origin cannot send without a CORS preflight, and the server answers none.
    /// </remarks>
    private async Task ServeQueryAsync( HttpContext context )
    {
        if ( await this.ReadPostAsync( context ) is not { } body )
        {
            return;
        }

        string? code;
        string? workspace;

        try
        {
            code = body["code"]?.GetValue<string>();
            workspace = body["workspace"]?.GetValue<string>();
        }
        catch ( Exception e ) when ( e is InvalidOperationException or FormatException )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The request is not valid." );

            return;
        }

        if ( string.IsNullOrWhiteSpace( code ) )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The query is empty." );

            return;
        }

        var args = new JsonObject { ["code"] = code, ["workspace"] = workspace, ["format"] = "html", [WorkerMethods.IncludeStructured] = false };
        var result = await this._worker.CallAsync( WorkerMethods.Query, args, null, context.RequestAborted );

        if ( result[WorkerMethods.Ok]?.GetValue<bool>() != true || result[WorkerMethods.Page] is not JsonObject page )
        {
            await WriteTextAsync(
                context,
                StatusCodes.Status400BadRequest,
                result[WorkerMethods.Error]?.GetValue<string>() ?? result[WorkerMethods.Text]?.GetValue<string>() ?? "The query failed." );

            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        var url = this.Store( page, code ).ToString();
        await context.Response.WriteAsync( new JsonObject { ["url"] = url }.ToJsonString(), Encoding.UTF8, context.RequestAborted );
    }

    /// <summary>
    /// Checks a POST request and reads its JSON body. It writes the error response and returns <c>null</c> when the request
    /// is rejected. Every POST route goes through this method, so that none can miss a check.
    /// </summary>
    /// <remarks>
    /// A POST runs a query or reads the code model, so beyond the token, the request must come from a page of this server:
    /// its <c>Origin</c> must be the origin of the server, and the browser must not mark it as cross-site. The body must be
    /// JSON, which a page of another origin cannot send without a CORS preflight, and the server answers none.
    /// </remarks>
    private async Task<JsonObject?> ReadPostAsync( HttpContext context )
    {
        var request = context.Request;

        if ( request.Headers.Origin.ToString() != this._origin
             || (request.Headers.TryGetValue( "Sec-Fetch-Site", out var site ) && site.ToString() != "same-origin") )
        {
            await WriteTextAsync( context, StatusCodes.Status403Forbidden, "Requests are accepted only from the pages of this server." );

            return null;
        }

        if ( request.ContentType?.StartsWith( "application/json", StringComparison.OrdinalIgnoreCase ) != true )
        {
            await WriteTextAsync( context, StatusCodes.Status415UnsupportedMediaType, "The request must be JSON." );

            return null;
        }

        if ( request.ContentLength is null or > _maxQueryRequestLength )
        {
            await WriteTextAsync( context, StatusCodes.Status413PayloadTooLarge, "The query is too long." );

            return null;
        }

        try
        {
            if ( await JsonNode.ParseAsync( request.Body, cancellationToken: context.RequestAborted ) is JsonObject body )
            {
                return body;
            }
        }
        catch ( JsonException ) { }

        await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The request is not valid." );

        return null;
    }

    /// <summary>
    /// Maps a route of the query editor to the method of the worker that answers it.
    /// </summary>
    private static string? GetPostMethod( string route )
        => route switch
        {
            "complete" => WorkerMethods.Complete,
            "describe" => WorkerMethods.DescribeCompletion,
            "hover" => WorkerMethods.Hover,
            "signature" => WorkerMethods.Signature,
            "diagnostics" => WorkerMethods.Diagnostics,
            _ => null
        };

    /// <summary>
    /// Answers a request of the query editor: completion, completion details, quick info, signature help or diagnostics.
    /// The worker analyzes the code and does not run it.
    /// </summary>
    private async Task ServeIntelliSenseAsync( HttpContext context, string method )
    {
        if ( await this.ReadPostAsync( context ) is not { } body )
        {
            return;
        }

        var args = new JsonObject();

        try
        {
            args["code"] = body["code"]?.GetValue<string>() ?? "";
            args["position"] = body["position"]?.GetValue<int>() ?? 0;
            args["index"] = body["index"]?.GetValue<int>() ?? -1;
        }
        catch ( Exception e ) when ( e is InvalidOperationException or FormatException )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, "The request is not valid." );

            return;
        }

        // The page drops answers that come late, but the worker is not stopped for them: a soft timeout that is too short
        // would kill the worker, and with it the loaded workspaces, on the first call, which loads the language services.
        var result = await this._worker.CallAsync( method, args, _intelliSenseTimeout, context.RequestAborted );

        if ( result[WorkerMethods.Ok]?.GetValue<bool>() != true || result[WorkerMethods.Structured] is not JsonObject answer )
        {
            await WriteTextAsync( context, StatusCodes.Status400BadRequest, result[WorkerMethods.Error]?.GetValue<string>() ?? "The request failed." );

            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync( answer.ToJsonString(), Encoding.UTF8, context.RequestAborted );
    }

    /// <summary>
    /// Serves a file of the query editor. The version in the path changes with the files, so they can be cached.
    /// </summary>
    private static async Task ServeAssetAsync( HttpContext context, string version, string name )
    {
        if ( version != EditorAssets.Version || !EditorAssets.TryGet( name, out var asset ) )
        {
            await WriteTextAsync( context, StatusCodes.Status404NotFound, "Not found." );

            return;
        }

        context.Response.Headers.CacheControl = "private, max-age=86400, immutable";
        context.Response.ContentType = asset.ContentType;
        context.Response.ContentLength = asset.Content.Length;
        await context.Response.Body.WriteAsync( asset.Content, context.RequestAborted );
    }

    private static async Task WriteTextAsync( HttpContext context, int statusCode, string text )
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync( text, Encoding.UTF8, context.RequestAborted );
    }

    public async ValueTask DisposeAsync()
    {
        await this._application.StopAsync();
        await this._application.DisposeAsync();
    }
}
