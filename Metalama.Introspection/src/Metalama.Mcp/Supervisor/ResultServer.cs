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
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Serves html results to the browser of the user, on <c>https://localhost</c> with a random port, and renders on demand what
/// the page expands. It never runs a query: it shows the documents of <see cref="ResultCache"/>, and follows links with the
/// <c>open</c> method of the worker, as the MCP App view does.
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

    private ResultServer( WebApplication application, WorkerClient worker, ResultCache cache, byte[] token, int port )
    {
        this._application = application;
        this._worker = worker;
        this._cache = cache;
        this._token = token;
        this._host = "localhost:" + port.ToString( CultureInfo.InvariantCulture );
        var encodedToken = ResultCache.Base64Url( token );
        this._tokenLength = encodedToken.Length;
        this.BaseUri = new Uri( $"https://{this._host}/{encodedToken}/" );
    }

    /// <summary>
    /// Gets the URL under which every page is served, with the token.
    /// </summary>
    public Uri BaseUri { get; }

    public Uri GetResultUri( string id ) => new( this.BaseUri, "r/" + id );

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

        if ( !HttpMethods.IsGet( context.Request.Method ) )
        {
            response.Headers.Allow = "GET";
            await WriteTextAsync( context, StatusCodes.Status405MethodNotAllowed, "Only GET is accepted." );

            return;
        }

        var segments = (context.Request.Path.Value ?? "").Trim( '/' ).Split( '/' );

        if ( segments.Length < 2 || !this.IsToken( segments[0] ) )
        {
            await WriteTextAsync( context, StatusCodes.Status404NotFound, "Not found." );

            return;
        }

        switch ( segments[1] )
        {
            case "r" when segments.Length == 3:
                await this.ServeResultAsync( context, segments[2] );

                break;

            case "open" when segments.Length == 2:
                await this.ServeOpenAsync( context );

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
