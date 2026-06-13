// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Services;
using Xunit;

namespace Metalama.Backstage.Worker.Tests;

// Regression tests for #1649. The reCAPTCHA site key is provided by the backend server and reflected into the
// setup page as 'data-sitekey="@Model.RecaptchaSiteKey"'. A hijacked/MITM'd key endpoint could return a value
// crafted to break out of that attribute (or a future raw/JS context). As belt-and-suspenders hardening on top
// of Razor's auto-encoding, the site key must be validated against the documented reCAPTCHA key format
// ([A-Za-z0-9_-]+) before being reflected; anything else must be rejected.
public sealed class RecaptchaSiteKeyValidationTests
{
    [Theory]

    // Well-formed reCAPTCHA keys consist only of letters, digits, '-' and '_'.
    [InlineData( "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe" )]
    [InlineData( "6Lc_aXkUAAAAANxValid_site-Key" )]
    [InlineData( "abcDEF0123456789-_" )]
    public void ValidSiteKeyIsAccepted( string siteKey )
    {
        Assert.True( RecaptchaSiteKeyValidator.IsValid( siteKey ) );
    }

    [Theory]

    // Null or empty: nothing to reflect.
    [InlineData( null )]
    [InlineData( "" )]
    [InlineData( " " )]

    // Attribute break-out / XSS payloads a hijacked endpoint might return.
    [InlineData( "\"><script>alert(1)</script>" )]
    [InlineData( "key\" onmouseover=\"alert(1)" )]
    [InlineData( "javascript:alert(1)" )]
    [InlineData( "key<tag>" )]

    // Other characters outside the allowed set.
    [InlineData( "key with spaces" )]
    [InlineData( "key.with.dots" )]
    [InlineData( "key/with/slashes" )]
    [InlineData( "key=value" )]
    public void InvalidSiteKeyIsRejected( string? siteKey )
    {
        Assert.False( RecaptchaSiteKeyValidator.IsValid( siteKey ) );
    }
}
