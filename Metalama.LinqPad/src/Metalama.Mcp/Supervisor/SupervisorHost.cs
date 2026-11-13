// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Runs the MCP server on stdio. Stdio is the only transport: the process that started the server is the only one that can
/// talk to it.
/// </summary>
internal static class SupervisorHost
{
    public static async Task<int> RunAsync( CommandLineOptions options )
    {
        await using var worker = new WorkerClient( options );
        var tools = new McpTools( worker );

        var serverOptions = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "metalama-mcp",
                Title = "Metalama",
                Version = typeof(SupervisorHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0"
            },
            ServerInstructions = EmbeddedResources.Read( "Primer.md" ),
            ToolCollection = [],
            ResourceCollection = []
        };

        foreach ( var tool in tools.CreateTools() )
        {
            serverOptions.ToolCollection.Add( tool );
        }

        serverOptions.ResourceCollection.Add(
            McpServerResource.Create(
                () => new TextResourceContents { Uri = McpTools.ViewerResourceUri, MimeType = HtmlWriter.AppMimeType, Text = HtmlWriter.CreateAppPage() },
                new McpServerResourceCreateOptions
                {
                    UriTemplate = McpTools.ViewerResourceUri,
                    Name = "Metalama result viewer",
                    Description = "Shows query results as property tables and grids, LINQPad-style, inside hosts that support MCP Apps.",
                    MimeType = HtmlWriter.AppMimeType
                } ) );

        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += ( _, e ) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        await using var transport = new StdioServerTransport( serverOptions, NullLoggerFactory.Instance );
        await using var server = McpServer.Create( transport, serverOptions, NullLoggerFactory.Instance );

        try
        {
            await server.RunAsync( shutdown.Token );
        }
        catch ( OperationCanceledException ) when ( shutdown.IsCancellationRequested )
        {
            // Ctrl+C.
        }

        return 0;
    }
}
