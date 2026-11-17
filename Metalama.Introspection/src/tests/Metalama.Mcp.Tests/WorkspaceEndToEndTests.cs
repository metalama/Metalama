// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Drives the server over stdio with the FrogHattery sample loaded.
/// </summary>
public sealed class WorkspaceEndToEndTests : IClassFixture<FrogHatteryFixture>
{
    private readonly FrogHatteryFixture _fixture;

    public WorkspaceEndToEndTests( FrogHatteryFixture fixture )
    {
        this._fixture = fixture;
    }

    private ServerProcess Server => this._fixture.Server;

    [Fact]
    public void LoadReportsProjectsAndBase()
    {
        Assert.Contains( "workspace: FrogHattery.csproj", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "FrogHattery@net10.0", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "base: metalama://FrogHattery.csproj/FrogHattery@net10.0/source/", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "Metalama enabled", this._fixture.LoadText, StringComparison.Ordinal );
    }

    [Fact]
    public async Task QueryListsDeclarationsWithLinksAndLocations()
    {
        var text = await this.Server.QueryAsync( "workspace.SourceCode.Types.Where( t => t.Name.EndsWith( \"Frog\" ) ).OrderBy( t => t.Name )" );

        Assert.Contains( "T:FrogHattery.BullFrog", text, StringComparison.Ordinal );
        Assert.Contains( "BullFrog.cs:", text, StringComparison.Ordinal );
        Assert.Contains( "fingerprint ", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task LongLinksInATableAreNotCut()
    {
        // Table cells are cut at a width, but a cut link cannot be opened. These method links are longer than that width.
        var text = await this.Server.QueryAsync(
            "workspace.SourceCode.Methods.Where( m => m.Parameters.Count > 1 ).Select( m => new { Method = m, Parameters = m.Parameters.Count } )" );

        var rows = text.Split( '\n' ).Where( l => l.StartsWith( "M:", StringComparison.Ordinal ) ).ToList();
        Assert.NotEmpty( rows );
        Assert.Contains( rows, r => r.Split( "  " )[0].Length > 60 );

        foreach ( var row in rows )
        {
            var link = row.Split( "  " )[0];
            Assert.DoesNotContain( "…", link, StringComparison.Ordinal );

            var (isError, opened, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = link } );
            Assert.False( isError, $"{link}: {opened}" );
        }
    }

    [Fact]
    public async Task DeclarationProjectedInARowKeepsItsLink()
    {
        // The primer tells agents to project the declaration rather than its name, so that each row links to it.
        const string query =
            "workspace.SourceCode.Methods.Select( m => new { Method = m, Nodes = m.Metrics().Get<SyntaxNodesCount>().Value } ).OrderByDescending( x => x.Nodes ).Take( 5 )";

        var text = await this.Server.QueryAsync( query );
        Assert.Contains( "M:FrogHattery.", text, StringComparison.Ordinal );

        // The JSON, from which the html page is built, also carries the location of each declaration.
        var json = await this.Server.QueryAsync( query, "json" );
        Assert.Contains( "M:FrogHattery.", json, StringComparison.Ordinal );
        Assert.Contains( ".cs:", json, StringComparison.Ordinal );
    }

    [Fact]
    public async Task HtmlPageCanLinkLocationsToTheEditor()
    {
        // Locations are relative to the workspace root, so the page needs the root, and the template of the editor link, to
        // turn each location into a link that opens the file at the line.
        var document = await this.QueryHtmlDocumentAsync( "workspace.SourceCode.Types.Take( 3 )" );

        Assert.Equal( Path.GetDirectoryName( ServerProcess.FrogHatteryPath ), document.GetProperty( "root" ).GetString() );
        Assert.Equal( CommandLineOptions.DefaultEditorLink, document.GetProperty( "editor" ).GetString() );
    }

    [Theory]
    [InlineData( null )]

    // An agent passed maxDepth 1 with html, and the page could not expand its cells. The page never enters the context of the
    // agent, so a lower depth has no use there.
    [InlineData( 1 )]
    public async Task DeclarationInAnHtmlTableCellCanBeExpanded( int? maxDepth )
    {
        // The page expands a cell in place from the data it embeds, so a declaration projected into a row must carry its
        // properties, as a method does in a LINQPad grid.
        var document = await this.QueryHtmlDocumentAsync(
            "workspace.SourceCode.Methods.Select( m => new { Method = m, Nodes = m.Metrics().Get<SyntaxNodesCount>().Value } ).OrderByDescending( x => x.Nodes ).Take( 5 )",
            maxDepth );

        var row = document.GetProperty( "result" ).GetProperty( "items" )[0];
        var method = row.GetProperty( "properties" ).EnumerateArray().Single( p => p.GetProperty( "name" ).GetString() == "Method" ).GetProperty( "value" );

        Assert.StartsWith( "M:FrogHattery.", method.GetProperty( "link" ).GetString(), StringComparison.Ordinal );
        Assert.True( method.TryGetProperty( "properties", out var properties ), "The declaration in the cell has no properties." );
        var propertyNames = properties.EnumerateArray().Select( p => p.GetProperty( "name" ).GetString() ).ToList();
        Assert.Contains( "Parameters", propertyNames );
        Assert.Contains( "ReturnType", propertyNames );
    }

    private async Task<JsonElement> QueryHtmlDocumentAsync( string query, int? maxDepth = null )
    {
        var link = await this.Server.QueryHtmlLinkAsync( query, maxDepth );
        using var client = ServerProcess.CreateHttpClient();

        return ServerProcess.GetEmbeddedDocument( await client.GetStringAsync( link, TestContext.Current.CancellationToken ) );
    }

    [Fact]
    public async Task ServedPageIsMarkedAndHasSecurityHeaders()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync( link, TestContext.Current.CancellationToken );
        Assert.Equal( HttpStatusCode.OK, response.StatusCode );
        Assert.Equal( "text/html", response.Content.Headers.ContentType?.MediaType );

        var csp = string.Join( ";", response.Headers.GetValues( "Content-Security-Policy" ) );
        Assert.Contains( "default-src 'none'", csp, StringComparison.Ordinal );
        Assert.Contains( "connect-src 'self'", csp, StringComparison.Ordinal );
        Assert.Equal( "nosniff", response.Headers.GetValues( "X-Content-Type-Options" ).Single() );
        Assert.Equal( "no-referrer", response.Headers.GetValues( "Referrer-Policy" ).Single() );
        Assert.True( response.Headers.CacheControl?.NoStore );
        Assert.False( response.Headers.Contains( "Access-Control-Allow-Origin" ) );

        // The page knows it is served, so that its cells fetch what they expand from the server.
        var document = ServerProcess.GetEmbeddedDocument( await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken ) );
        Assert.True( document.GetProperty( "served" ).GetBoolean() );
        Assert.Equal( "T:FrogHattery.BullFrog", document.GetProperty( "result" ).GetProperty( "link" ).GetString() );
    }

    [Fact]
    public async Task OpenEndpointRendersALinkOnDemand()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        var json = await client.GetStringAsync( new Uri( link, "../open?link=" + Uri.EscapeDataString( "T:FrogHattery.BullFrog/Methods" ) ), TestContext.Current.CancellationToken );

        var document = JsonDocument.Parse( json ).RootElement;
        Assert.Equal( 1, document.GetProperty( "schema" ).GetInt32() );
        Assert.Equal( "collection", document.GetProperty( "result" ).GetProperty( "kind" ).GetString() );
        Assert.Contains(
            document.GetProperty( "result" ).GetProperty( "items" ).EnumerateArray(),
            i => i.GetProperty( "link" ).GetString()!.StartsWith( "M:FrogHattery.BullFrog.", StringComparison.Ordinal ) );
    }

    [Fact]
    public async Task OpenEndpointNeverRunsAQuery()
    {
        // A query anchor would run code. The HTTP server only follows links, as the MCP App view does.
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync(
            new Uri( link, "../open?link=" + Uri.EscapeDataString( "metalama://FrogHattery.csproj/FrogHattery@net10.0/source/query/d29ya3NwYWNl" ) ),
            TestContext.Current.CancellationToken );

        Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
        Assert.Contains( "not supported", await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task RequestsWithoutTheTokenAreNotFound()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();
        var origin = new Uri( link.GetLeftPart( UriPartial.Authority ) );
        var token = link.Segments[1].TrimEnd( '/' );

        foreach ( var path in new[] { "/", "/r/" + link.Segments[^1], "/" + new string( 'A', token.Length ) + "/r/" + link.Segments[^1], "/open?link=T:FrogHattery.BullFrog" } )
        {
            using var response = await client.GetAsync( new Uri( origin, path ), TestContext.Current.CancellationToken );
            Assert.Equal( HttpStatusCode.NotFound, response.StatusCode );
        }
    }

    [Fact]
    public async Task RequestsForAnotherHostAreRejected()
    {
        // A page of another site that rebinds its DNS name to 127.0.0.1 sends its own name as Host.
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var request = new HttpRequestMessage( HttpMethod.Get, link );
        request.Headers.Host = "attacker.example:" + link.Port.ToString( CultureInfo.InvariantCulture );
        using var response = await client.SendAsync( request, TestContext.Current.CancellationToken );

        Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
    }

    [Fact]
    public async Task OnlyGetIsAccepted()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.PostAsync( link, new StringContent( "" ), TestContext.Current.CancellationToken );

        Assert.False( response.IsSuccessStatusCode );
    }

    [Fact]
    public async Task UnknownResultExplainsWhatToDo()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "42" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync( new Uri( link, "unknown" ), TestContext.Current.CancellationToken );

        Assert.Equal( HttpStatusCode.NotFound, response.StatusCode );
        Assert.Contains( "no longer available", await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task EveryLinkInTheOutputOpens()
    {
        var text = await this.Server.QueryAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )" );
        Assert.Contains( "D<INamedType>(\"T:FrogHattery.BullFrog\")", text, StringComparison.Ordinal );

        // Every value with a link is printed as the link itself or after an arrow.
        var links = text.Split( '\n' )
            .Select( l => l.Contains( "→ ", StringComparison.Ordinal ) ? l.Substring( l.IndexOf( "→ ", StringComparison.Ordinal ) + 2 ).Trim() : l.Trim().Split( "  " ).Last() )
            .Where( l => l.StartsWith( "T:", StringComparison.Ordinal ) || l.StartsWith( "M:", StringComparison.Ordinal ) || l.StartsWith( "P:", StringComparison.Ordinal ) )
            .Distinct()
            .ToList();

        Assert.NotEmpty( links );

        foreach ( var link in links )
        {
            var (isError, opened, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = link } );
            Assert.False( isError, $"{link}: {opened}" );
        }
    }

    [Fact]
    public async Task OpenFollowsPropertySteps()
    {
        var text = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );

        Assert.Contains( "M:FrogHattery.BullFrog.", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task OpenReportsHiddenPropertiesAndUnknownDeclarations()
    {
        var (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Depth" } );
        Assert.True( isError );
        Assert.Contains( "no displayed property 'Depth'", text, StringComparison.Ordinal );

        (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.Nope" } );
        Assert.True( isError );
        Assert.Contains( "not found", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task IndexStepsCarryTheFingerprint()
    {
        var text = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );

        // Items of a collection reached by a link get index links only when they have no identifier of their own, so open
        // one through an index explicitly.
        Assert.DoesNotContain( "error", text.Split( '\n' )[0], StringComparison.Ordinal );

        var opened = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods/0/Parameters" } );
        Assert.Contains( "IParameter", opened, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ProjectionIsATable()
    {
        var text = await this.Server.QueryAsync( "workspace.SourceCode.Types.OrderBy( t => t.Name ).Select( t => new { t.Name, Methods = t.Methods.Count } )" );

        Assert.Contains( "Name", text.Split( '\n' ).First( l => l.StartsWith( "Name", StringComparison.Ordinal ) ), StringComparison.Ordinal );
        Assert.Contains( "Methods", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task StatementsAndDeclarationHelpers()
    {
        var text = await this.Server.QueryAsync( "var type = D<INamedType>( \"T:FrogHattery.BullFrog\" );\nreturn type.Name.Length;" );

        Assert.StartsWith( "base: ", text, StringComparison.Ordinal );
        Assert.Contains( "\n8\n", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileErrorsPointAtTheQuery()
    {
        var (isError, text, _) = await this.Server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "workspace.Nope" } );

        Assert.True( isError );
        Assert.Contains( "CS1061 (1,11)", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ExceptionsAreReported()
    {
        var (isError, text, _) = await this.Server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "D( \"T:FrogHattery.Nope\" )" } );

        Assert.True( isError );
        Assert.Contains( "not found", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ConsoleOutputIsCapturedAndTheStreamStaysValid()
    {
        var text = await this.Server.QueryAsync( "Console.WriteLine( \"hello from the query\" );\nreturn 1;" );

        Assert.Contains( "--- console ---\nhello from the query", text, StringComparison.Ordinal );

        // The MCP stream is intact.
        Assert.Contains( "\n2\n", await this.Server.QueryAsync( "1 + 1" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task DownloadIsASelfContainedSnapshot()
    {
        var link = await this.Server.QueryHtmlLinkAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )" );
        using var client = ServerProcess.CreateHttpClient();

        using var response = await client.GetAsync( new Uri( link + "?download=1" ), TestContext.Current.CancellationToken );
        Assert.Equal( "attachment", response.Content.Headers.ContentDisposition?.DispositionType );
        Assert.EndsWith( ".html", response.Content.Headers.ContentDisposition?.FileName?.Trim( '"' ), StringComparison.Ordinal );

        var html = await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken );

        // The snapshot does not call the server, which is gone when the file is opened later.
        Assert.False( ServerProcess.GetEmbeddedDocument( html ).TryGetProperty( "served", out _ ) );
        Assert.Contains( "Content-Security-Policy", html, StringComparison.Ordinal );
        Assert.Contains( "id=\"metalama-data\"", html, StringComparison.Ordinal );
        Assert.Contains( "T:FrogHattery.BullFrog", html, StringComparison.Ordinal );
        Assert.DoesNotContain( "http://", html, StringComparison.Ordinal );
        Assert.DoesNotContain( "https://", html, StringComparison.Ordinal );
    }

    [Fact]
    public async Task JsonFormatFollowsTheSchema()
    {
        var text = await this.Server.QueryAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )", "json" );

        var json = text.Substring( 0, text.LastIndexOf( "\n--- ", StringComparison.Ordinal ) );
        using var document = System.Text.Json.JsonDocument.Parse( json );
        var root = document.RootElement;

        Assert.Equal( 1, root.GetProperty( "schema" ).GetInt32() );
        var result = root.GetProperty( "result" );
        Assert.Equal( "object", result.GetProperty( "kind" ).GetString() );
        Assert.True( result.GetProperty( "declaration" ).GetBoolean() );
        Assert.Equal( "T:FrogHattery.BullFrog", result.GetProperty( "link" ).GetString() );
        Assert.StartsWith( "metalama://", result.GetProperty( "href" ).GetString(), StringComparison.Ordinal );
        Assert.True( result.GetProperty( "properties" ).GetArrayLength() > 0 );
    }

    [Fact]
    public async Task EnumerationOrderIsStableWithinALoad()
    {
        const string query = "string.Join( \",\", workspace.SourceCode.Methods.Select( m => m.ToSerializableId().Id ) )";

        Assert.Equal( Body( await this.Server.QueryAsync( query ) ), Body( await this.Server.QueryAsync( query ) ) );
    }

    [Fact]
    public async Task IndexLinksAreStaleAfterAReloadAndDeclarationLinksAreNot()
    {
        // The order of collections such as SourceCode.Methods can differ between two loads of identical sources, so an
        // index link is valid only for the load that produced it.
        var fingerprint = GetFingerprint( await this.Server.QueryAsync( "0" ) );
        var indexLink = $"T:FrogHattery.BullFrog/Methods/0?v={fingerprint}";
        await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = indexLink } );

        await this.Server.CallSuccessfullyAsync( "reload_workspace", new Dictionary<string, object?>() );

        var (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = indexLink } );
        Assert.True( isError );
        Assert.Contains( "stale:", text, StringComparison.Ordinal );

        await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );
        Assert.NotEqual( fingerprint, GetFingerprint( await this.Server.QueryAsync( "0" ) ) );
    }

    private static string Body( string text ) => text.Split( '\n' )[1];

    private static string GetFingerprint( string text )
    {
        var footer = text.Split( '\n' ).Last();
        var start = footer.IndexOf( "fingerprint ", StringComparison.Ordinal ) + "fingerprint ".Length;

        return new string( footer.Skip( start ).TakeWhile( char.IsAsciiHexDigit ).ToArray() );
    }
}
