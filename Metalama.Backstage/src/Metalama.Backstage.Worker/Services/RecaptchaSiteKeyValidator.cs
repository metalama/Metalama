// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text.RegularExpressions;

namespace Metalama.Backstage.Services;

/// <summary>
/// Validates the format of a reCAPTCHA site key received from the backend server before it is reflected into the
/// setup page. The site key is outside our trust boundary (a hijacked or MITM'd key endpoint could return a
/// malicious value), so we accept it only if it matches the documented reCAPTCHA key format, i.e. a non-empty
/// string of ASCII letters, digits, '-' and '_'. This is belt-and-suspenders hardening on top of Razor's
/// HTML-attribute auto-encoding (see #1649).
/// </summary>
internal static class RecaptchaSiteKeyValidator
{
    private static readonly Regex _siteKeyRegex = new( "^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant );

    public static bool IsValid( string? siteKey ) => !string.IsNullOrEmpty( siteKey ) && _siteKeyRegex.IsMatch( siteKey );
}
