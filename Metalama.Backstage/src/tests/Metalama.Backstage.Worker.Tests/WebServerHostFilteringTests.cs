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

public sealed class WebServerHostFilteringTests : TestsBase
{
    public WebServerHostFilteringTests( ITestOutputHelper logger )
        : base( logger ) { }

    private async Task<HttpResponseMessage> SendPingWithHostAsync( string hostHeader )
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

                using var request = new HttpRequestMessage( HttpMethod.Get, "/ping" );
                request.Headers.Host = hostHeader;

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
            catch ( IOException )
            {
                // Best-effort cleanup.
            }
        }
    }

    [Theory]
    [InlineData( "localhost" )]
    [InlineData( "localhost:5000" )]
    [InlineData( "127.0.0.1" )]
    [InlineData( "127.0.0.1:5000" )]
    public async Task LoopbackHostIsAllowed( string hostHeader )
    {
        using var response = await this.SendPingWithHostAsync( hostHeader );

        Assert.Equal( HttpStatusCode.OK, response.StatusCode );
    }

    [Theory]
    [InlineData( "evil.example.com" )]
    [InlineData( "attacker.test" )]
    [InlineData( "metalama.net" )]
    public async Task NonLoopbackHostIsRejected( string hostHeader )
    {
        using var response = await this.SendPingWithHostAsync( hostHeader );

        // A request whose 'Host' header does not target the loopback interface must be rejected by the host-filtering
        // middleware. This protects the short-lived local setup server against DNS-rebinding from local websites.
        Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
    }
}
