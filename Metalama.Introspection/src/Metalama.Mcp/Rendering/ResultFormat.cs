// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Mcp.Rendering;

internal enum ResultFormat
{
    Text,
    Json,
    Html
}

internal static class ResultFormatExtensions
{
    public static bool TryParse( string? text, out ResultFormat format )
    {
        switch ( text?.Trim().ToLowerInvariant() )
        {
            case null or "" or "text":
                format = ResultFormat.Text;

                return true;

            case "json":
                format = ResultFormat.Json;

                return true;

            case "html":
                format = ResultFormat.Html;

                return true;

            default:
                format = default;

                return false;
        }
    }

    /// <summary>
    /// Gets the default depth: the HTML viewer has no round trip to the server, so its snapshot goes deeper.
    /// </summary>
    public static int GetDefaultMaxDepth( this ResultFormat format ) => format == ResultFormat.Html ? 3 : 1;

    public static int GetDefaultMaxItems( this ResultFormat format ) => format == ResultFormat.Html ? 500 : 50;

    public static int GetMaxCharacters( this ResultFormat format ) => format == ResultFormat.Html ? 10 * 1024 * 1024 : 20_000;

    public static string ToDisplayString( this ResultFormat format ) => format.ToString().ToLowerInvariant() ?? throw new InvalidOperationException();
}
