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
using System.Net.Http.Headers;
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
/// See https://github.com/metalama/Metalama/issues/1769.
/// </remarks>
public sealed class WebServerAuthenticationTests : TestsBase
{
    /// <summary>
    /// The token the server under test is configured to require.
    /// </summary>
    private const string _expectedToken = "the-expected-token";

    /// <summary>
    /// The name of the session cookie set by the server once a valid token has been presented.
    /// </summary>
    private const string _cookieName = "metalama-setup-token";

    public WebServerAuthenticationTests( ITestOutputHelper logger )
        : base( logger ) { }

    /// <summary>
    /// Creates the content root of the server under test.
    /// </summary>
    /// <remarks>
    /// The static-file middleware requires a <c>wwwroot</c> directory under the content root, otherwise it falls back
    /// to probing the binary directory. We provide an empty one so the server starts cleanly under the test host.
    /// </remarks>
    private static string CreateContentRoot()
    {
        var contentRoot = Path.Combine( Path.GetTempPath(), "MetalamaWorkerTest_" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( Path.Combine( contentRoot, "wwwroot" ) );

        return contentRoot;
    }

    /// <summary>
    /// Deletes the content root created by <see cref="CreateContentRoot"/> on a best-effort basis.
    /// </summary>
    private static void DeleteContentRoot( string contentRoot )
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

    /// <summary>
    /// Builds the setup web server, hosted by the ASP.NET Core test host and requiring <see cref="_expectedToken"/>.
    /// </summary>
    private WebApplication CreateApplication( string contentRoot )
    {
        var appData = new AppData( (ServiceCollection) this.CloneServiceCollection(), this.ServiceProvider );

        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions() { ApplicationName = "Metalama.Backstage.Worker", ContentRootPath = contentRoot } );

        builder.WebHost.UseTestServer();

        return WebServerCommand.BuildWebApplication( builder, appData, () => { }, _expectedToken );
    }

    /// <summary>
    /// Starts the web server and sends a <c>GET</c> request to <paramref name="path"/>, passing <paramref name="token"/>
    /// as the authentication token in the query string when it is not <see langword="null"/>.
    /// </summary>
    /// <param name="acceptHtml">
    /// Whether the request advertises <c>text/html</c>, i.e. whether it looks like a browser navigation. A navigation
    /// presenting a valid token is redirected so that the token leaves the address bar, whereas a programmatic call
    /// such as the readiness probe of the parent process is served directly.
    /// </param>
    /// <param name="tokenParameterName">
    /// The name of the query-string parameter carrying the token. The lookup of a query parameter is case-insensitive,
    /// so a test can present the token under a different casing than the canonical one.
    /// </param>
    private async Task<HttpResponseMessage> SendRequestAsync(
        string path,
        string? token,
        bool acceptHtml = false,
        string tokenParameterName = "t" )
    {
        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
        var cancellationToken = cancellationTokenSource.Token;

        var contentRoot = CreateContentRoot();

        try
        {
            var app = this.CreateApplication( contentRoot );

            await using ( app.ConfigureAwait( false ) )
            {
                await app.StartAsync( cancellationToken );

                // The test-host client does not follow redirects, so the redirect that the server issues after a valid
                // token is observable.
                using var client = app.GetTestClient();

                var requestUri = token == null ? path : $"{path}?{tokenParameterName}={Uri.EscapeDataString( token )}";

                using var request = new HttpRequestMessage( HttpMethod.Get, requestUri );
                request.Headers.Host = "localhost";

                if ( acceptHtml )
                {
                    request.Headers.Accept.Add( new MediaTypeWithQualityHeaderValue( "text/html" ) );
                }

                var response = await client.SendAsync( request, cancellationToken );

                await app.StopAsync( cancellationToken );

                return response;
            }
        }
        finally
        {
            DeleteContentRoot( contentRoot );
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

    /// <summary>
    /// Verifies that a prefix of the expected token is not accepted.
    /// </summary>
    [Fact]
    public async Task RequestWithTruncatedTokenIsRejected()
    {
        using var response = await this.SendRequestAsync( "/ping", token: _expectedToken.Substring( 0, _expectedToken.Length - 1 ) );

        Assert.Equal( HttpStatusCode.Unauthorized, response.StatusCode );
    }

    [Theory]
    [InlineData( "/ping" )]
    [InlineData( "/LicenseKey" )]
    [InlineData( "/Consents" )]
    public async Task ProgrammaticRequestWithValidTokenIsAccepted( string path )
    {
        using var response = await this.SendRequestAsync( path, token: _expectedToken );

        Assert.Equal( HttpStatusCode.OK, response.StatusCode );
    }

    /// <summary>
    /// Verifies that a browser navigation presenting a valid token receives the session cookie and is redirected to the
    /// same page without the token, so that the token does not remain in the address bar or in the browsing history.
    /// </summary>
    [Fact]
    public async Task BrowserNavigationWithValidTokenSetsCookieAndStripsTokenFromUrl()
    {
        using var response = await this.SendRequestAsync( "/LicenseKey", token: _expectedToken, acceptHtml: true );

        Assert.Equal( HttpStatusCode.Found, response.StatusCode );
        Assert.Equal( "/LicenseKey", response.Headers.Location?.ToString() );

        var setCookie = Assert.Single( response.Headers.GetValues( "Set-Cookie" ) );

        Assert.Contains( $"{_cookieName}={_expectedToken}", setCookie, StringComparison.Ordinal );
        Assert.Contains( "httponly", setCookie, StringComparison.OrdinalIgnoreCase );
        Assert.Contains( "samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase );
    }

    /// <summary>
    /// Verifies that a token presented under a different casing of the query-parameter name is stripped from the URL of
    /// the redirect as well.
    /// </summary>
    /// <remarks>
    /// The lookup of a query parameter is case-insensitive, so such a request authenticates. Were the parameter name
    /// compared case-sensitively when the query string is rebuilt, the token would survive the redirect and remain in
    /// the address bar and in the browsing history, which is precisely what the redirect exists to prevent.
    /// </remarks>
    [Fact]
    public async Task BrowserNavigationWithDifferentlyCasedTokenParameterStripsTokenFromUrl()
    {
        using var response = await this.SendRequestAsync( "/LicenseKey", token: _expectedToken, acceptHtml: true, tokenParameterName: "T" );

        Assert.Equal( HttpStatusCode.Found, response.StatusCode );
        Assert.Equal( "/LicenseKey", response.Headers.Location?.ToString() );
    }

    /// <summary>
    /// Verifies that the cookie set on the first request authenticates the subsequent requests, which is what allows the
    /// pages to link to each other and to post forms without carrying the token in every URL.
    /// </summary>
    [Fact]
    public async Task RequestWithCookieIsAccepted()
    {
        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
        var cancellationToken = cancellationTokenSource.Token;

        var contentRoot = CreateContentRoot();

        try
        {
            var app = this.CreateApplication( contentRoot );

            await using ( app.ConfigureAwait( false ) )
            {
                await app.StartAsync( cancellationToken );

                using var client = app.GetTestClient();

                using var request = new HttpRequestMessage( HttpMethod.Get, "/LicenseKey" );
                request.Headers.Host = "localhost";
                request.Headers.Add( "Cookie", $"{_cookieName}={_expectedToken}" );

                using var response = await client.SendAsync( request, cancellationToken );

                await app.StopAsync( cancellationToken );

                Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            }
        }
        finally
        {
            DeleteContentRoot( contentRoot );
        }
    }
}
