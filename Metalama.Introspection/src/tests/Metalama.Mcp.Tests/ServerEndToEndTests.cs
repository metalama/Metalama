// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SharpCrafters.Backstage.Maintenance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public async Task HtmlGoesToTheTemporaryDirectoryOfMetalama()
    {
        // Without --output-dir, the page goes to a temporary directory managed by Backstage, which is private to the user and
        // cleaned up. The test redirects the temporary directory of Metalama with METALAMA_TEMP.
        await using var server = await ServerProcess.StartAsync( defaultOutputDirectory: true );

        var text = await server.QueryAsync( "42", "html" );

        var uri = text.Split( '\n' ).Single( l => l.StartsWith( "html: ", StringComparison.Ordinal ) ).Substring( "html: ".Length );
        var path = new Uri( uri ).LocalPath;

        Assert.True( File.Exists( path ), path );
        Assert.StartsWith( Path.GetFullPath( server.OutputDirectory ), path, StringComparison.OrdinalIgnoreCase );
        Assert.Contains( "McpResults", path, StringComparison.Ordinal );

        // Backstage records the clean-up policy of the directory, which deletes each page a month after it was written.
        var policies = Directory.EnumerateFiles( server.OutputDirectory, "cleanup.json", SearchOption.AllDirectories ).Select( File.ReadAllText ).ToArray();
        var strategy = (int) CleanUpStrategy.FileOneMonthAfterCreation;
        Assert.Contains( policies, p => JsonDocument.Parse( p ).RootElement.GetProperty( "Strategy" ).GetInt32() == strategy );
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
        Assert.DoesNotContain( "https://", contents.Text, StringComparison.Ordinal );
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
    public async Task NoProcessListensOnASocket()
    {
        await using var server = await ServerProcess.StartAsync();
        var worker = await GetWorkerAsync( server );
        var supervisor = server.ProcessIds!.Value.Supervisor;

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
    public void ServerHasNoHttpDependency()
    {
        var depsJson = File.ReadAllText( Path.ChangeExtension( ServerProcess.ServerPath, ".deps.json" ) );
        using var document = JsonDocument.Parse( depsJson );
        var libraries = document.RootElement.GetProperty( "libraries" ).EnumerateObject().Select( p => p.Name ).ToList();

        Assert.Contains( libraries, l => l.StartsWith( "ModelContextProtocol.Core/", StringComparison.Ordinal ) );
        Assert.DoesNotContain( libraries, l => l.StartsWith( "Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase ) );
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
