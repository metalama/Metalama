// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// The query editor of a served page: its files, and the IntelliSense that the worker computes for it.
/// </summary>
public sealed class QueryEditorEndToEndTests : IClassFixture<FrogHatteryFixture>
{
    private readonly FrogHatteryFixture _fixture;

    public QueryEditorEndToEndTests( FrogHatteryFixture fixture )
    {
        this._fixture = fixture;
    }

    private ServerProcess Server => this._fixture.Server;

    [Fact]
    public async Task CompletionListsTheMembersOfTheCodeModel()
    {
        var answer = await this.PostAsync( "complete", "workspace.", 10 );

        var labels = Labels( answer );
        Assert.Contains( "SourceCode", labels );
        Assert.Contains( "TransformedCode", labels );
        Assert.Equal( 10, answer["start"]!.GetValue<int>() );
    }

    [Fact]
    public async Task CompletionOfAnElementOfACollectionListsItsMembers()
    {
        const string code = "workspace.SourceCode.Types.Select( t => t.";
        var answer = await this.PostAsync( "complete", code, code.Length );

        Assert.Contains( "Methods", Labels( answer ) );
    }

    [Fact]
    public async Task CompletionShowsTheGlobalsButNotTheGeneratedClass()
    {
        var labels = Labels( await this.PostAsync( "complete", "", 0 ) );

        Assert.Contains( "workspace", labels );
        Assert.Contains( "workspaces", labels );
        Assert.Contains( "ct", labels );
        Assert.DoesNotContain( "ExecuteAsync", labels );
        Assert.DoesNotContain( "__MetalamaQuery", labels );
    }

    [Fact]
    public async Task CompletionDetailsDescribeTheSelectedItem()
    {
        var answer = await this.PostAsync( "complete", "workspace.", 10 );
        var item = answer["items"]!.AsArray().Single( i => i!["label"]!.GetValue<string>() == "SourceCode" )!;

        var details = await this.PostAsync( "describe", "workspace.", 10, item["index"]!.GetValue<int>() );

        Assert.Contains( "SourceCode", details["detail"]!.GetValue<string>(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task HoverShowsTheTypeOfAMember()
    {
        var answer = await this.PostAsync( "hover", "workspace.SourceCode", 12 );

        Assert.Contains( "ICompilationSet", answer["markdown"]!.GetValue<string>(), StringComparison.Ordinal );
        Assert.Equal( 10, answer["start"]!.GetValue<int>() );
        Assert.Equal( 20, answer["end"]!.GetValue<int>() );
    }

    [Fact]
    public async Task SignatureHelpNamesTheParameterAtTheCaret()
    {
        const string code = "workspace.SourceCode.Types.Take( ";
        var answer = await this.PostAsync( "signature", code, code.Length );

        var signatures = answer["signatures"]!.AsArray();
        Assert.NotEmpty( signatures );
        var active = signatures[answer["activeSignature"]!.GetValue<int>()]!;
        Assert.Contains( "count", active["label"]!.GetValue<string>(), StringComparison.Ordinal );
        Assert.Equal( 0, answer["activeParameter"]!.GetValue<int>() );
    }

    [Fact]
    public async Task SignatureHelpFollowsTheArgument()
    {
        const string code = "string.Join( \", \", ";
        var answer = await this.PostAsync( "signature", code, code.Length );

        Assert.Equal( 1, answer["activeParameter"]!.GetValue<int>() );
    }

    [Fact]
    public async Task DiagnosticsReportAnErrorAtItsPosition()
    {
        var answer = await this.PostAsync( "diagnostics", "workspace.Typs", 0 );

        var marker = Assert.Single( answer["markers"]!.AsArray() )!;
        Assert.Equal( "CS1061", marker["id"]!.GetValue<string>() );
        Assert.Equal( "error", marker["severity"]!.GetValue<string>() );
        Assert.Equal( 1, marker["startLine"]!.GetValue<int>() );
        Assert.Equal( 11, marker["startColumn"]!.GetValue<int>() );
    }

    [Theory]
    [InlineData( "workspace.SourceCode.Types.Count()" )]
    [InlineData( "Console.WriteLine( 1 )" )]
    [InlineData( "var n = workspace.SourceCode.Types.Count();\nreturn n;" )]
    public async Task ValidQueriesHaveNoDiagnostics( string code )
    {
        var answer = await this.PostAsync( "diagnostics", code, 0 );

        Assert.Empty( answer["markers"]!.AsArray() );
    }

    [Fact]
    public async Task IntelliSenseRunsNoCode()
    {
        var path = Path.Combine( Path.GetTempPath(), "metalama-mcp-intellisense-" + Guid.NewGuid().ToString( "N" ) );
        var code = $"System.IO.File.WriteAllText( @\"{path}\", \"x\" )";

        foreach ( var route in new[] { "complete", "hover", "signature", "diagnostics" } )
        {
            await this.PostAsync( route, code, code.Length - 2 );
        }

        Assert.False( File.Exists( path ) );
    }

    [Theory]
    [InlineData( "complete" )]
    [InlineData( "describe" )]
    [InlineData( "hover" )]
    [InlineData( "signature" )]
    [InlineData( "diagnostics" )]
    public async Task IntelliSenseIsAcceptedOnlyFromThePagesOfTheServer( string route )
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();
        var body = "{\"code\":\"workspace.\",\"position\":10}";

        // Another origin, a cross-site fetch, a form body, no token, and GET are all rejected.
        using ( var response = await SendAsync( client, link, route, body, "https://evil.example", null, "application/json" ) )
        {
            Assert.Equal( HttpStatusCode.Forbidden, response.StatusCode );
        }

        using ( var response = await SendAsync( client, link, route, body, "self", "cross-site", "application/json" ) )
        {
            Assert.Equal( HttpStatusCode.Forbidden, response.StatusCode );
        }

        using ( var response = await SendAsync( client, link, route, body, "self", "same-origin", "application/x-www-form-urlencoded" ) )
        {
            Assert.Equal( HttpStatusCode.UnsupportedMediaType, response.StatusCode );
        }

        var origin = link.GetLeftPart( UriPartial.Authority );
        var withoutToken = new Uri( origin + "/" + new string( 'A', link.Segments[1].TrimEnd( '/' ).Length ) + "/r/x" );

        using ( var response = await SendAsync( client, withoutToken, route, body, "self", "same-origin", "application/json" ) )
        {
            Assert.Equal( HttpStatusCode.NotFound, response.StatusCode );
        }

        using ( var response = await client.GetAsync( new Uri( link, "../" + route ), TestContext.Current.CancellationToken ) )
        {
            Assert.Equal( HttpStatusCode.MethodNotAllowed, response.StatusCode );
        }

        using ( var response = await SendAsync( client, link, route, body, "self", "same-origin", "application/json" ) )
        {
            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
        }
    }

    [Fact]
    public async Task ServedPageLoadsTheEditorFromTheServer()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync( link, TestContext.Current.CancellationToken );
        var csp = string.Join( ";", response.Headers.GetValues( "Content-Security-Policy" ) );

        Assert.Matches( "script-src 'sha256-[^']+' 'self'", csp );
        Assert.Contains( "style-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal );
        Assert.Contains( "font-src 'self'", csp, StringComparison.Ordinal );
        Assert.Contains( "worker-src 'self'", csp, StringComparison.Ordinal );

        var assets = ServerProcess.GetEmbeddedDocument( await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken ) )
            .GetProperty( "editorAssets" )
            .GetString()!;

        Assert.Matches( "^\\.\\./assets/[0-9a-f]{12}/$", assets );

        foreach ( var (name, type) in new[]
                 {
                     ("editor.js", "text/javascript"), ("editor.css", "text/css"), ("editor.worker.js", "text/javascript"), ("codicon.ttf", "font/ttf")
                 } )
        {
            using var asset = await client.GetAsync( new Uri( link, assets + name ), TestContext.Current.CancellationToken );
            Assert.Equal( HttpStatusCode.OK, asset.StatusCode );
            Assert.Equal( type, asset.Content.Headers.ContentType?.MediaType );
            Assert.Contains( "immutable", asset.Headers.CacheControl?.ToString(), StringComparison.Ordinal );
        }

        // Only the files of the editor, and only under the current version.
        using ( var unknown = await client.GetAsync( new Uri( link, assets + "viewer.js" ), TestContext.Current.CancellationToken ) )
        {
            Assert.Equal( HttpStatusCode.NotFound, unknown.StatusCode );
        }

        using ( var stale = await client.GetAsync( new Uri( link, "../assets/000000000000/editor.js" ), TestContext.Current.CancellationToken ) )
        {
            Assert.Equal( HttpStatusCode.NotFound, stale.StatusCode );
        }
    }

    [Fact]
    public async Task SnapshotHasNoEditor()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync( new Uri( link + "?download=1" ), TestContext.Current.CancellationToken );
        var html = await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken );

        Assert.False( ServerProcess.GetEmbeddedDocument( html ).TryGetProperty( "editorAssets", out _ ) );
        Assert.DoesNotContain( "'self'", html, StringComparison.Ordinal );
    }

    private static string[] Labels( JsonObject answer ) => answer["items"]!.AsArray().Select( i => i!["label"]!.GetValue<string>() ).ToArray();

    private async Task<JsonObject> PostAsync( string route, string code, int position, int index = -1 )
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();
        var body = new JsonObject { ["code"] = code, ["position"] = position, ["index"] = index }.ToJsonString();

        using var response = await SendAsync( client, link, route, body, "self", "same-origin", "application/json" );
        var text = await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken );
        Assert.True( response.IsSuccessStatusCode, text );

        return JsonNode.Parse( text )!.AsObject();
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Uri link,
        string route,
        string body,
        string? origin,
        string? fetchSite,
        string contentType )
    {
        using var request = new HttpRequestMessage( HttpMethod.Post, new Uri( link, "../" + route ) )
        {
            Content = new StringContent( body, Encoding.UTF8, contentType )
        };

        if ( origin != null )
        {
            request.Headers.Add( "Origin", origin == "self" ? link.GetLeftPart( UriPartial.Authority ) : origin );
        }

        if ( fetchSite != null )
        {
            request.Headers.Add( "Sec-Fetch-Site", fetchSite );
        }

        return await client.SendAsync( request, TestContext.Current.CancellationToken );
    }
}
