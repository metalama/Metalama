// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Starts <c>metalama-mcp</c> as an MCP client would, and talks to it over stdio.
/// </summary>
internal sealed partial class ServerProcess : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _standardError;

    private ServerProcess( McpClient client, ConcurrentQueue<string> standardError, string outputDirectory )
    {
        this.Client = client;
        this._standardError = standardError;
        this.OutputDirectory = outputDirectory;
    }

    public McpClient Client { get; }

    public string OutputDirectory { get; }

    public static string ServerPath => Path.GetFullPath( GetMetadata( "ServerPath" ) );

    public static string FrogHatteryPath => Path.GetFullPath( GetMetadata( "FrogHatteryPath" ) );

    public IReadOnlyList<string> StandardError => [..this._standardError];

    [GeneratedRegex( @"supervisor pid=(?<s>\d+) started worker pid=(?<w>\d+)" )]
    private static partial Regex PidRegex();

    public (int Supervisor, int Worker)? ProcessIds
    {
        get
        {
            var match = this._standardError.Select( l => PidRegex().Match( l ) ).LastOrDefault( m => m.Success );

            return match == null
                ? null
                : (int.Parse( match.Groups["s"].Value, CultureInfo.InvariantCulture ), int.Parse( match.Groups["w"].Value, CultureInfo.InvariantCulture ));
        }
    }

    /// <summary>
    /// Waits until standard error reports a worker, other than <paramref name="previousWorker"/> when given. Standard error
    /// is delivered asynchronously, so it can lag behind the tool result.
    /// </summary>
    public async Task<(int Supervisor, int Worker)> WaitForProcessIdsAsync( int? previousWorker = null )
    {
        for ( var i = 0; i < 100; i++ )
        {
            if ( this.ProcessIds is { } ids && ids.Worker != previousWorker )
            {
                return ids;
            }

            await Task.Delay( 100 );
        }

        throw new TimeoutException( "The server did not report its process identifiers on standard error." );
    }

    public static async Task<ServerProcess> StartAsync( bool withAppsCapability = false, int? timeoutSeconds = null, bool reportAssemblies = false )
    {
        var standardError = new ConcurrentQueue<string>();
        var outputDirectory = Path.Combine( Path.GetTempPath(), "metalama-mcp-tests", Guid.NewGuid().ToString( "N" ) );

        List<string> arguments = [ServerPath, "--output-dir", outputDirectory];

        if ( timeoutSeconds != null )
        {
            arguments.Add( "--timeout" );
            arguments.Add( timeoutSeconds.Value.ToString( CultureInfo.InvariantCulture ) );
        }

        var transport = new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Name = "metalama-mcp",
                Command = "dotnet",
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName( FrogHatteryPath ),
                StandardErrorLines = standardError.Enqueue,
                EnvironmentVariables = reportAssemblies ? new Dictionary<string, string?> { ["METALAMA_MCP_REPORT_ASSEMBLIES"] = "1" } : null
            } );

        var options = new McpClientOptions { ClientInfo = new Implementation { Name = "Metalama.Mcp.Tests", Version = "1.0" } };

        if ( withAppsCapability )
        {
            options.Capabilities = new ClientCapabilities
            {
                Extensions = new Dictionary<string, object>
                {
                    ["io.modelcontextprotocol/ui"] = JsonDocument.Parse( """{ "mimeTypes": [ "text/html;profile=mcp-app" ] }""" ).RootElement.Clone()
                }
            };
        }

        var client = await McpClient.CreateAsync( transport, options );

        return new ServerProcess( client, standardError, outputDirectory );
    }

    public async Task<(bool IsError, string Text, JsonElement? Structured)> CallAsync( string tool, Dictionary<string, object?> arguments )
    {
        var result = await this.Client.CallToolAsync( tool, arguments );
        var text = string.Join( "\n", result.Content.OfType<TextContentBlock>().Select( t => t.Text ) );

        return (result.IsError == true, text, result.StructuredContent);
    }

    public async Task<string> CallSuccessfullyAsync( string tool, Dictionary<string, object?> arguments )
    {
        var (isError, text, _) = await this.CallAsync( tool, arguments );

        if ( isError )
        {
            throw new InvalidOperationException( $"{tool} failed:\n{text}\n--- stderr ---\n{string.Join( "\n", this.StandardError.TakeLast( 30 ) )}" );
        }

        return text;
    }

    public Task<string> QueryAsync( string code, string? format = null )
        => this.CallSuccessfullyAsync( "query", new Dictionary<string, object?> { ["code"] = code, ["format"] = format } );

    private static string GetMetadata( string key )
        => typeof(ServerProcess).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single( a => a.Key == key ).Value!;

    public async ValueTask DisposeAsync()
    {
        await this.Client.DisposeAsync();

        try
        {
            Directory.Delete( this.OutputDirectory, true );
        }
        catch ( IOException )
        {
            // Best effort.
        }
        catch ( UnauthorizedAccessException )
        {
            // Best effort.
        }
    }
}
