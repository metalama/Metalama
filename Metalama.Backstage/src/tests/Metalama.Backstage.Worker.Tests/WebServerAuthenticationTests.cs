// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.Worker.WebServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Worker.Tests;

/// <summary>
/// Tests that the local setup web server rejects requests that do not carry the per-session authentication token.
/// </summary>
/// <remarks>
/// The server binds to the loopback interface only, but loopback is not a security boundary between local accounts:
/// on a shared machine, any other local user can reach it and drive the current user's Backstage configuration.
/// The per-session token is what makes the server usable only by the session that started it.
/// </remarks>
public sealed class WebServerAuthenticationTests : TestsBase
{
    public WebServerAuthenticationTests( ITestOutputHelper logger )
        : base( logger ) { }

    /// <summary>
    /// Starts the web server under the ASP.NET Core test host and sends a <c>GET</c> request to the given path,
    /// optionally passing <paramref name="token"/> as the authentication token in the query string.
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync( string path, string? token )
    {
        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
        var cancellationToken = cancellationTokenSource.Token;

        // The static-file middleware requires a 'wwwroot' directory under the content root, otherwise it falls back to
        // probing the binary directory. We provide an empty one so the server starts cleanly under the test host.
        var contentRoot = Path.Combine( Path.GetTempPath(), "MetalamaWorkerTest_" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( Path.Combine( contentRoot, "wwwroot" ) );

        try
        {
            var appData = new AppData( (ServiceCollection) this.CloneServiceCollection(), this.ServiceProvider );

            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions() { ApplicationName = "Metalama.Backstage.Worker", ContentRootPath = contentRoot } );

            builder.WebHost.UseTestServer();

            var app = WebServerCommand.BuildWebApplication( builder, appData, () => { } );

            await using ( app.ConfigureAwait( false ) )
            {
                await app.StartAsync( cancellationToken );

                using var client = app.GetTestClient();

                var requestUri = token == null ? path : $"{path}?t={Uri.EscapeDataString( token )}";

                using var request = new HttpRequestMessage( HttpMethod.Get, requestUri );
                request.Headers.Host = "localhost";

                var response = await client.SendAsync( request, cancellationToken );

                await app.StopAsync( cancellationToken );

                return response;
            }
        }
        finally
        {
            try
            {
                Directory.Delete( contentRoot, recursive: true );
            }
            catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
            {
                // Best-effort cleanup: the temp directory may still hold a handle (e.g. on Windows), in which case
                // deletion can fail with either IOException or UnauthorizedAccessException. Neither should fail the test.
            }
        }
    }

    [Theory]
    [InlineData( "/ping" )]
    [InlineData( "/LicenseKey" )]
    [InlineData( "/Consents" )]
    public async Task RequestWithoutTokenIsRejected( string path )
    {
        using var response = await this.SendRequestAsync( path, token: null );

        Assert.Equal( HttpStatusCode.Unauthorized, response.StatusCode );
    }

    [Theory]
    [InlineData( "/ping" )]
    [InlineData( "/LicenseKey" )]
    [InlineData( "/Consents" )]
    public async Task RequestWithWrongTokenIsRejected( string path )
    {
        using var response = await this.SendRequestAsync( path, token: "not-the-right-token" );

        Assert.Equal( HttpStatusCode.Unauthorized, response.StatusCode );
    }
}
