// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Utilities;

internal static class UrlHelper
{
    /// <summary>
    /// Determines whether the given <paramref name="url"/> is a safe absolute URL, i.e. one that uses the <c>http</c> or
    /// <c>https</c> scheme. This prevents a hijacked or MITM'd feed from supplying a dangerous scheme (e.g. <c>ms-msdt:</c>,
    /// <c>search-ms:</c>, <c>file://</c>) that would be passed to Windows protocol activation. See issue #1647.
    /// </summary>
    public static bool IsSafe( [NotNullWhen( true )] string? url, [NotNullWhen( true )] out Uri? uri )
    {
        if ( !Uri.TryCreate( url, UriKind.Absolute, out uri )
             || uri.Scheme is not ("http" or "https") )
        {
            uri = null;

            return false;
        }

        return true;
    }
}
