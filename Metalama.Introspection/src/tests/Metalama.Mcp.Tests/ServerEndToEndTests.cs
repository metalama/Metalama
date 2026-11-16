// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using ModelContextProtocol.Protocol;
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
