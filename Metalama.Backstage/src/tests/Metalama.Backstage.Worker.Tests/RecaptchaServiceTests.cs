// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Services;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Backstage.Worker.Tests;

// Regression tests for #1649, at the level of the service that fetches the reCAPTCHA site key from the backend
// server. A hijacked/MITM'd key endpoint could return a malicious value; the service must validate the format and
// treat a malformed key the same as a missing one (null), so it is never reflected into the setup page.
public sealed class RecaptchaServiceTests
{
    private static async Task<string?> GetSiteKeyFromServerResponseAsync( string serverResponse )
    {
        var webLinks = new WebLinks();
        var httpClientFactory = new TestHttpClientFactory();

        httpClientFactory.AddHook(
            request => request.RequestUri!.ToString() == webLinks.NewsletterGetCaptchaSiteKeyApi,
            ( _, _ ) => Task.FromResult( new HttpResponseMessage( HttpStatusCode.OK ) { Content = new StringContent( serverResponse ) } ) );

        var service = new RecaptchaService( httpClientFactory, webLinks );
        service.Initialize();

        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );

        return await service.GetRecaptchaSiteKeyAsync().WaitAsync( cancellationTokenSource.Token );
    }

    [Theory]
    [InlineData( "\"><script>alert(1)</script>" )]
    [InlineData( "key\" onmouseover=\"alert(1)" )]
    [InlineData( "javascript:alert(1)" )]
    [InlineData( "key with spaces" )]
    [InlineData( "" )]
    public async Task MalformedSiteKeyFromServerIsRejected( string serverResponse )
    {
        var siteKey = await GetSiteKeyFromServerResponseAsync( serverResponse );

        Assert.Null( siteKey );
    }

    [Fact]
    public async Task WellFormedSiteKeyFromServerIsAccepted()
    {
        const string validKey = "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe";

        var siteKey = await GetSiteKeyFromServerResponseAsync( validKey );

        Assert.Equal( validKey, siteKey );
    }

    [Fact]
    public async Task SurroundingWhitespaceFromServerIsTrimmed()
    {
        const string validKey = "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe";

        // A legitimate endpoint may return the key with a trailing newline; that must still be accepted.
        var siteKey = await GetSiteKeyFromServerResponseAsync( "\n" + validKey + "\r\n" );

        Assert.Equal( validKey, siteKey );
    }
}
