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
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

    private ServerProcess( McpClient client, ConcurrentQueue<string> standardError )
    {
        this.Client = client;
        this._standardError = standardError;
    }

    public McpClient Client { get; }

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

    /// <summary>
    /// Gets a self-signed certificate for <c>localhost</c>, which the server uses instead of the ASP.NET Core development
    /// certificate, so that the tests do not depend on the machine.
    /// </summary>
    public static X509Certificate2 TestCertificate => _testCertificate.Value;

    private static readonly Lazy<X509Certificate2> _testCertificate = new( CreateTestCertificate );

    private static readonly Lazy<string> _testCertificatePath = new(
        () =>
        {
            var path = Path.Combine( Path.GetTempPath(), "metalama-mcp-tests", $"localhost-{Environment.ProcessId}.pfx" );
            Directory.CreateDirectory( Path.GetDirectoryName( path )! );
            File.WriteAllBytes( path, TestCertificate.Export( X509ContentType.Pfx ) );

            return path;
        } );

    private static X509Certificate2 CreateTestCertificate()
    {
        using var key = RSA.Create( 2048 );
        var request = new CertificateRequest( "CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName( "localhost" );
        request.CertificateExtensions.Add( names.Build() );
        request.CertificateExtensions.Add( new X509EnhancedKeyUsageExtension( [new Oid( "1.3.6.1.5.5.7.3.1" )], false ) );

        using var certificate = request.CreateSelfSigned( DateTimeOffset.UtcNow.AddDays( -1 ), DateTimeOffset.UtcNow.AddDays( 7 ) );

        // Round-trip through PFX, because Schannel on Windows needs a key that is not ephemeral.
        return X509CertificateLoader.LoadPkcs12( certificate.Export( X509ContentType.Pfx ), null, X509KeyStorageFlags.Exportable );
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that trusts <see cref="TestCertificate"/> only.
    /// </summary>
    public static HttpClient CreateHttpClient()
        => new(
            new HttpClientHandler
            {
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = ( _, certificate, _, _ ) => certificate?.Thumbprint == TestCertificate.Thumbprint
            } );

    /// <param name="testCertificate">The value of <c>METALAMA_MCP_TEST_CERTIFICATE</c>: the path of <see cref="TestCertificate"/> by
    /// default, or <c>"missing"</c> to act as if there were no development certificate.</param>
    public static async Task<ServerProcess> StartAsync(
        bool withAppsCapability = false,
        int? timeoutSeconds = null,
        bool reportAssemblies = false,
        bool noHttp = false,
        string? testCertificate = null )
    {
        var standardError = new ConcurrentQueue<string>();

        List<string> arguments = [ServerPath];
        var environment = new Dictionary<string, string?> { ["METALAMA_MCP_TEST_CERTIFICATE"] = testCertificate ?? _testCertificatePath.Value };

        if ( noHttp )
        {
            arguments.Add( "--no-http" );
        }

        if ( reportAssemblies )
        {
            environment["METALAMA_MCP_REPORT_ASSEMBLIES"] = "1";
        }

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
                EnvironmentVariables = environment
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

        return new ServerProcess( client, standardError );
    }

    [GeneratedRegex( @"^html: (?<url>https://localhost:\d+/\S+)$", RegexOptions.Multiline )]
    private static partial Regex HtmlLinkRegex();

    /// <summary>
    /// Runs a query in the html format and returns the https link of the result.
    /// </summary>
    public async Task<Uri> QueryHtmlLinkAsync( string code, int? maxDepth = null )
    {
        var text = await this.CallSuccessfullyAsync(
            "query",
            new Dictionary<string, object?> { ["code"] = code, ["format"] = "html", ["maxDepth"] = maxDepth } );

        var match = HtmlLinkRegex().Match( text );

        return match.Success ? new Uri( match.Groups["url"].Value ) : throw new InvalidOperationException( $"No https link in:\n{text}" );
    }

    /// <summary>
    /// Gets the JSON document that a served page embeds.
    /// </summary>
    public static JsonElement GetEmbeddedDocument( string html )
    {
        var data = Regex.Match( html, "<script type=\"application/json\" id=\"metalama-data\">(?<json>.*?)</script>", RegexOptions.Singleline ).Groups["json"].Value;

        return JsonDocument.Parse( data ).RootElement.Clone();
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

    public async ValueTask DisposeAsync() => await this.Client.DisposeAsync();
}
