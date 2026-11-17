// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Drives the server over stdio without a workspace: the protocol surface, the process split and its guarantees.
/// </summary>
public sealed class ServerEndToEndTests
{
    [Fact]
    public async Task ToolsHaveAnnotationsAndTheViewer()
    {
        await using var server = await ServerProcess.StartAsync();
        var tools = await server.Client.ListToolsAsync( cancellationToken: TestContext.Current.CancellationToken );

        Assert.Equal( ["load_workspace", "open", "query", "reload_workspace", "unload_workspace"], tools.Select( t => t.Name ).Order( StringComparer.Ordinal ).ToArray() );

        foreach ( var tool in tools )
        {
            Assert.False( tool.ProtocolTool.Annotations?.ReadOnlyHint, tool.Name );
        }

        var query = tools.Single( t => t.Name == "query" ).ProtocolTool;
        Assert.Equal( "ui://metalama/viewer", query.Meta?["ui"]?["resourceUri"]?.GetValue<string>() );

        // The description tells the agent what `workspace` is and where the object model is documented.
        Assert.Contains( "Metalama.Framework.Workspaces.Workspace", query.Description, StringComparison.Ordinal );
        Assert.Contains( "https://doc.postsharp.net/metalama/api/introspection-api", query.Description, StringComparison.Ordinal );
        Assert.Contains( "https://doc.postsharp.net/metalama/api/introspection-api", server.Client.ServerInstructions, StringComparison.Ordinal );
        Assert.NotNull( server.Client.ServerInstructions );
        Assert.Contains( "Open(link)", server.Client.ServerInstructions, StringComparison.Ordinal );
    }

    [Theory]
    [InlineData( "load_workspace" )]
    [InlineData( "reload_workspace" )]
    [InlineData( "query" )]
    [InlineData( "open" )]
    public async Task ToolsThatRunCodeAreNotDeclaredHarmless( string name )
    {
        await using var server = await ServerProcess.StartAsync();
        var tools = await server.Client.ListToolsAsync( cancellationToken: TestContext.Current.CancellationToken );
        var annotations = tools.Single( t => t.Name == name ).ProtocolTool.Annotations;

        // The defaults of the specification are true; a client may ask permission only for destructive or open-world tools.
        Assert.NotEqual( false, annotations?.DestructiveHint );
        Assert.NotEqual( false, annotations?.OpenWorldHint );
    }

    [Fact]
    public async Task OnlyOpenIsCallableFromTheView()
    {
        await using var server = await ServerProcess.StartAsync();
        var tools = await server.Client.ListToolsAsync( cancellationToken: TestContext.Current.CancellationToken );

        static string[]? GetVisibility( McpClientTool tool ) => tool.ProtocolTool.Meta?["ui"]?["visibility"]?.AsArray().Select( v => v!.GetValue<string>() ).ToArray();

        Assert.Equal( ["model"], GetVisibility( tools.Single( t => t.Name == "query" ) ) );
        Assert.Equal( ["model", "app"], GetVisibility( tools.Single( t => t.Name == "open" ) ) );
    }

    [Fact]
    public async Task TextIsTheSameWithAndWithoutMcpApps()
    {
        const string code = "new { A = new { B = new { C = 1 } } }";

        await using var plain = await ServerProcess.StartAsync();
        var plainText = await plain.QueryAsync( code );

        await using var apps = await ServerProcess.StartAsync( withAppsCapability: true );
        var appsText = await apps.QueryAsync( code );

        static string WithoutFooter( string text ) => text.Substring( 0, text.LastIndexOf( "\n--- ", StringComparison.Ordinal ) );

        Assert.Equal( WithoutFooter( plainText ), WithoutFooter( appsText ) );
    }

    [Fact]
    public async Task HtmlEnumeratesTheResultOnce()
    {
        await using var server = await ServerProcess.StartAsync();

        var text = await server.QueryAsync( "Enumerable.Range( 0, 3 ).Select( i => { Console.WriteLine( \"enumerated\" ); return i; } )", "html" );

        Assert.Equal( 3, text.Split( "enumerated" ).Length - 1 );
    }

    [Fact]
    public async Task HtmlReturnsAnHttpsLinkAndWritesNoFile()
    {
        await using var server = await ServerProcess.StartAsync();

        var link = await server.QueryHtmlLinkAsync( "42" );

        Assert.Equal( "https", link.Scheme );
        Assert.Equal( "localhost", link.Host );

        using var client = ServerProcess.CreateHttpClient();
        var html = await client.GetStringAsync( link, TestContext.Current.CancellationToken );
        Assert.Equal( "42", ServerProcess.GetEmbeddedDocument( html ).GetProperty( "result" ).GetProperty( "value" ).GetString() );
    }

    [Fact]
    public async Task HtmlWithoutHttpExplainsWhy()
    {
        await using var server = await ServerProcess.StartAsync( noHttp: true );

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "42", ["format"] = "html" } );

        Assert.True( isError );
        Assert.Contains( "--no-http", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task HtmlWithoutADevelopmentCertificateExplainsHowToGetOne()
    {
        await using var server = await ServerProcess.StartAsync( testCertificate: "missing" );

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "42", ["format"] = "html" } );

        Assert.True( isError );
        Assert.Contains( "dotnet dev-certs https --trust", text, StringComparison.Ordinal );

        // Text results still work.
        Assert.Equal( "42", (await server.QueryAsync( "42" )).Split( '\n' )[0] );
    }

    [Fact]
    public async Task AppResultLinksToTheServedPage()
    {
        // A host may render the MCP App view without forwarding its tools/call to the server, as Claude desktop does. The view
        // then loads what its cells expand from the result server, and offers to open the served page in the browser.
        await using var server = await ServerProcess.StartAsync( withAppsCapability: true );

        var (_, _, structured) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "3" } );
        var pageUrl = new Uri( structured!.Value.GetProperty( "pageUrl" ).GetString()! );

        using var client = ServerProcess.CreateHttpClient();
        var page = ServerProcess.GetEmbeddedDocument( await client.GetStringAsync( pageUrl, TestContext.Current.CancellationToken ) );
        Assert.Equal( "3", page.GetProperty( "result" ).GetProperty( "value" ).GetString() );

        var (_, htmlText, html) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "2", ["format"] = "html" } );
        Assert.Contains( "html: " + html!.Value.GetProperty( "pageUrl" ).GetString(), htmlText, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AppViewCanFetchLinksFromAnotherOrigin()
    {
        // The view runs in a frame of the host, on the origin of the host, and fetches the open endpoint of the result server.
        await using var server = await ServerProcess.StartAsync( withAppsCapability: true );
        var (_, _, structured) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "3" } );
        var openUrl = new Uri( new Uri( structured!.Value.GetProperty( "pageUrl" ).GetString()! ), "../open?link=" + Uri.EscapeDataString( "T:System.Object" ) );

        using var client = ServerProcess.CreateHttpClient();

        using var preflight = new HttpRequestMessage( HttpMethod.Options, openUrl );
        preflight.Headers.Add( "Origin", "https://host.example" );
        preflight.Headers.Add( "Access-Control-Request-Method", "GET" );
        preflight.Headers.Add( "Access-Control-Request-Private-Network", "true" );
        using var preflightResponse = await client.SendAsync( preflight, TestContext.Current.CancellationToken );
        Assert.Equal( HttpStatusCode.NoContent, preflightResponse.StatusCode );
        Assert.Equal( "*", Assert.Single( preflightResponse.Headers.GetValues( "Access-Control-Allow-Origin" ) ) );
        Assert.Equal( "true", Assert.Single( preflightResponse.Headers.GetValues( "Access-Control-Allow-Private-Network" ) ) );

        using var get = new HttpRequestMessage( HttpMethod.Get, openUrl );
        get.Headers.Add( "Origin", "https://host.example" );
        using var response = await client.SendAsync( get, TestContext.Current.CancellationToken );
        Assert.Equal( "*", Assert.Single( response.Headers.GetValues( "Access-Control-Allow-Origin" ) ) );

        // Pages are not readable from another origin: only the open endpoint is.
        using var pageRequest = new HttpRequestMessage( HttpMethod.Get, structured.Value.GetProperty( "pageUrl" ).GetString() );
        pageRequest.Headers.Add( "Origin", "https://host.example" );
        using var pageResponse = await client.SendAsync( pageRequest, TestContext.Current.CancellationToken );
        Assert.False( pageResponse.Headers.Contains( "Access-Control-Allow-Origin" ) );
    }

    [Fact]
    public async Task AppResultHasNoPageWithNoHttp()
    {
        await using var server = await ServerProcess.StartAsync( withAppsCapability: true, noHttp: true );
        var (_, _, structured) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "3" } );

        Assert.False( structured!.Value.TryGetProperty( "pageUrl", out _ ) );
    }

    [Fact]
    public async Task ResultsHaveNoPageWithoutMcpApps()
    {
        // The model gets the link in the text of an html result; the JSON form only exists for the MCP App view.
        await using var server = await ServerProcess.StartAsync();
        await server.QueryHtmlLinkAsync( "2" );

        var (_, text, structured) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "3" } );

        Assert.Null( structured );
        Assert.DoesNotContain( "https://", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task QueryOnWorkspaceWithoutWorkspaceExplainsWhy()
    {
        await using var server = await ServerProcess.StartAsync();

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "workspace.SourceCode.Types" } );

        Assert.True( isError );
        Assert.Contains( "No workspace is loaded", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ViewerIsAnMcpAppPageThatLoadsNothing()
    {
        await using var server = await ServerProcess.StartAsync();
        var resource = await server.Client.ReadResourceAsync( "ui://metalama/viewer", cancellationToken: TestContext.Current.CancellationToken );
        var contents = Assert.IsType<TextResourceContents>( Assert.Single( resource.Contents ) );

        Assert.Equal( "text/html;profile=mcp-app", contents.MimeType );
        Assert.Contains( "default-src &#39;none&#39;", contents.Text, StringComparison.Ordinal );
        Assert.DoesNotContain( "metalama-data\">", contents.Text, StringComparison.Ordinal );

        // The only address in the page is where it may connect: the result server, on any port until it runs.
        Assert.Contains( "connect-src https://localhost:*", contents.Text, StringComparison.Ordinal );
        Assert.Single( Regex.Matches( contents.Text, "https://" ) );
        Assert.Equal( "https://localhost:*", contents.Meta!["ui"]!["csp"]!["connectDomains"]![0]!.GetValue<string>() );
    }

    [Fact]
    public async Task QueryWithoutWorkspace()
    {
        await using var server = await ServerProcess.StartAsync();
        var text = await server.QueryAsync( "Enumerable.Range( 1, 3 ).Select( i => new { i, Square = i * i } )" );

        Assert.Contains( "i  Square\n1  1\n2  4\n3  9", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task StructuredContentOnlyForHostsWithMcpApps()
    {
        await using ( var plain = await ServerProcess.StartAsync() )
        {
            var (_, _, structured) = await plain.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "new { A = 1 }" } );
            Assert.Null( structured );
        }

        await using var apps = await ServerProcess.StartAsync( withAppsCapability: true );
        var (isError, text, appStructured) = await apps.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "new { A = 1 }" } );

        Assert.False( isError, text );
        Assert.NotNull( appStructured );
        Assert.Equal( 1, appStructured.Value.GetProperty( "schema" ).GetInt32() );
        Assert.Equal( "object", appStructured.Value.GetProperty( "result" ).GetProperty( "kind" ).GetString() );
    }

    [Fact]
    public async Task HardTimeoutStopsTheWorkerAndTheNextCallWorks()
    {
        await using var server = await ServerProcess.StartAsync();
        var firstWorker = await GetWorkerAsync( server );

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "while ( true ) { }", ["timeoutSeconds"] = 1 } );

        Assert.True( isError );
        Assert.Contains( "hard timeout", text, StringComparison.Ordinal );

        Assert.StartsWith( "2\n", await server.QueryAsync( "1 + 1" ), StringComparison.Ordinal );
        Assert.NotEqual( firstWorker, (await server.WaitForProcessIdsAsync( firstWorker )).Worker );
    }

    [Fact]
    public async Task SoftTimeoutCancelsAQueryThatObservesItsToken()
    {
        await using var server = await ServerProcess.StartAsync();
        var worker = await GetWorkerAsync( server );

        var (isError, text, _) = await server.CallAsync(
            "query",
            new Dictionary<string, object?> { ["code"] = "await Task.Delay( 60_000, ct ); return 1;", ["timeoutSeconds"] = 1 } );

        Assert.True( isError );
        Assert.Contains( "cancelled", text, StringComparison.Ordinal );
        Assert.Equal( worker, server.ProcessIds!.Value.Worker );
    }

    [Fact]
    public async Task OnlyTheSupervisorListensAndOnlyOnLoopbackAfterAnHtmlResult()
    {
        await using var server = await ServerProcess.StartAsync();
        var worker = await GetWorkerAsync( server );
        var supervisor = server.ProcessIds!.Value.Supervisor;

        // Until a result is requested in the html format, nothing listens.
        var before = ListeningSockets.GetOwningProcessIds();
        Assert.DoesNotContain( supervisor, before );
        Assert.DoesNotContain( worker, before );

        var link = await server.QueryHtmlLinkAsync( "42" );

        // The worker, which runs the queries, never listens. The supervisor listens on the port of the link, on loopback only.
        var after = ListeningSockets.GetListeners();
        Assert.DoesNotContain( after, l => l.ProcessId == worker );

        var supervisorListeners = after.Where( l => l.ProcessId == supervisor ).ToList();
        Assert.NotEmpty( supervisorListeners );
        Assert.All( supervisorListeners, l => Assert.True( l.IsLoopback, l.LocalAddress ) );
        Assert.All( supervisorListeners, l => Assert.EndsWith( ":" + link.Port.ToString( CultureInfo.InvariantCulture ), l.LocalAddress, StringComparison.Ordinal ) );
    }

    [Fact]
    public async Task NoProcessListensWithNoHttp()
    {
        await using var server = await ServerProcess.StartAsync( noHttp: true );
        var worker = await GetWorkerAsync( server );
        var supervisor = server.ProcessIds!.Value.Supervisor;

        await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "42", ["format"] = "html" } );

        var listeners = ListeningSockets.GetOwningProcessIds();
        Assert.DoesNotContain( supervisor, listeners );
        Assert.DoesNotContain( worker, listeners );
    }

    [Fact]
    public async Task SupervisorDoesNotLoadTheCodeModel()
    {
        await using var server = await ServerProcess.StartAsync( reportAssemblies: true );

        // A query makes the worker load and compile against the code model; the supervisor only forwards it.
        await server.QueryAsync( "typeof(IDeclaration).Assembly.GetName().Name" );

        var line = server.StandardError.Last( l => l.StartsWith( "metalama-mcp: supervisor assemblies=", StringComparison.Ordinal ) );
        var assemblies = line.Substring( line.IndexOf( '=', StringComparison.Ordinal ) + 1 ).Split( ';' );

        Assert.Contains( "metalama-mcp", assemblies );
        Assert.Contains( "ModelContextProtocol.Core", assemblies );
        Assert.DoesNotContain( assemblies, a => a.StartsWith( "Microsoft.CodeAnalysis", StringComparison.Ordinal ) );
        Assert.DoesNotContain( assemblies, a => a.StartsWith( "Microsoft.Build", StringComparison.Ordinal ) );
        Assert.DoesNotContain( assemblies, a => a.StartsWith( "Metalama.Framework", StringComparison.Ordinal ) );
    }

    [Fact]
    public async Task WorkerDoesNotLoadTheHttpServer()
    {
        await using var server = await ServerProcess.StartAsync( reportAssemblies: true );

        // The html result starts the HTTP server in the supervisor, and the next query runs in the worker.
        await server.QueryHtmlLinkAsync( "42" );
        await server.QueryAsync( "1" );

        // The supervisor forwards the standard error of the worker asynchronously, with a "worker: " prefix.
        const string marker = "metalama-mcp: worker assemblies=";
        string? line = null;

        for ( var i = 0; i < 50 && line == null; i++ )
        {
            line = server.StandardError.LastOrDefault( l => l.Contains( marker, StringComparison.Ordinal ) );
            await Task.Delay( line == null ? 100 : 0, TestContext.Current.CancellationToken );
        }

        Assert.NotNull( line );
        var assemblies = line.Substring( line.IndexOf( marker, StringComparison.Ordinal ) + marker.Length ).Split( ';' );

        Assert.Contains( "Microsoft.CodeAnalysis", assemblies );
        Assert.DoesNotContain( assemblies, a => a.StartsWith( "Microsoft.AspNetCore", StringComparison.Ordinal ) );
    }

    [Fact]
    public void ServerHasNoHttpTransportForMcp()
    {
        // The HTTP server only serves results to the browser; MCP itself stays on stdio.
        var depsJson = File.ReadAllText( Path.ChangeExtension( ServerProcess.ServerPath, ".deps.json" ) );
        using var document = JsonDocument.Parse( depsJson );
        var libraries = document.RootElement.GetProperty( "libraries" ).EnumerateObject().Select( p => p.Name ).ToList();

        Assert.Contains( libraries, l => l.StartsWith( "ModelContextProtocol.Core/", StringComparison.Ordinal ) );
        Assert.DoesNotContain( libraries, l => l.StartsWith( "ModelContextProtocol.AspNetCore", StringComparison.OrdinalIgnoreCase ) );
    }

    [Fact]
    public async Task NetworkOptionsAreRejected()
    {
        using var process = Process.Start(
            new ProcessStartInfo( "dotnet" ) { ArgumentList = { ServerProcess.ServerPath, "--port", "8080" }, RedirectStandardError = true, RedirectStandardInput = true } )!;

        var error = await process.StandardError.ReadToEndAsync( TestContext.Current.CancellationToken );
        await process.WaitForExitAsync( TestContext.Current.CancellationToken );

        Assert.Equal( 2, process.ExitCode );
        Assert.Contains( "no network option", error, StringComparison.Ordinal );
    }

    /// <summary>
    /// Runs a query, which starts the worker, and returns its process identifier.
    /// </summary>
    private static async Task<int> GetWorkerAsync( ServerProcess server )
    {
        await server.QueryAsync( "0" );

        return (await server.WaitForProcessIdsAsync()).Worker;
    }
}
