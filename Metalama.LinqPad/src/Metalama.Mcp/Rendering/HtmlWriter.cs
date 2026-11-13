// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// Builds the HTML viewer page: either a self-contained file with the result embedded, or the page of the MCP App, which
/// receives its data from the host.
/// </summary>
/// <remarks>
/// This class works on JSON only, so the supervisor can use it without loading the code model.
/// </remarks>
internal static partial class HtmlWriter
{
    [GeneratedRegex( @"\{\{(?<name>[A-Z]+)\}\}" )]
    private static partial Regex PlaceholderRegex();

    public const string AppMimeType = "text/html;profile=mcp-app";
    public const string DataElementId = "metalama-data";

    // The default encoder escapes <, >, & and quotes as \uXXXX, so a value that contains </script> cannot close the element.
    private static readonly JsonSerializerOptions _embeddingOptions = new() { Encoder = JavaScriptEncoder.Default };

    /// <summary>
    /// Creates the page of the MCP App, without data.
    /// </summary>
    public static string CreateAppPage() => CreatePage( null, "Metalama" );

    /// <summary>
    /// Creates a page. When <paramref name="document"/> is given, it is embedded and the page needs nothing else.
    /// </summary>
    public static string CreatePage( JsonObject? document, string title )
    {
        var template = EmbeddedResources.Read( "Viewer.viewer.html" );
        var script = EmbeddedResources.Read( "Viewer.viewer.js" );
        var css = EmbeddedResources.Read( "Viewer.viewer.css" );

        var scriptHash = Convert.ToBase64String( SHA256.HashData( Encoding.UTF8.GetBytes( script ) ) );

        // The page loads nothing: no network, no frames, and only its own script, allowed by its hash.
        var csp = $"default-src 'none'; script-src 'sha256-{scriptHash}'; style-src 'unsafe-inline'; img-src data:; base-uri 'none'; form-action 'none'";

        var data = document != null
            ? $"<script type=\"application/json\" id=\"{DataElementId}\">{document.ToJsonString( _embeddingOptions )}</script>"
            : "";

        // A single pass over the template, so that a value that contains a placeholder, in the title or the data, is never substituted.
        return PlaceholderRegex()
            .Replace(
                template,
                match => match.Groups["name"].Value switch
                {
                    "CSP" => WebUtility.HtmlEncode( csp ),
                    "TITLE" => WebUtility.HtmlEncode( title ),
                    "CSS" => css,
                    "SCRIPT" => script,
                    "DATA" => data,
                    _ => match.Value
                } );
    }

    /// <summary>
    /// Writes a self-contained page to the output directory and returns its path.
    /// </summary>
    public static string WriteFile( JsonObject document, string title, string outputDirectory )
    {
        Directory.CreateDirectory( outputDirectory );

        var hash = Convert.ToHexStringLower( SHA256.HashData( Encoding.UTF8.GetBytes( document.ToJsonString() ) ) ).Substring( 0, 8 );
        var fileName = $"result-{DateTime.Now.ToString( "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture )}-{hash}.html";
        var path = Path.Combine( outputDirectory, fileName );

        File.WriteAllText( path, CreatePage( document, title ), new UTF8Encoding( false ) );

        return path;
    }
}
