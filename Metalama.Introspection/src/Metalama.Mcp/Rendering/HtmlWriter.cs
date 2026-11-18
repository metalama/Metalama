// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// Builds the HTML viewer page: a page with the result embedded, served by the result server or saved as a snapshot, or the
/// page of the MCP App, which receives its data from the host.
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

    /// <summary>
    /// The property of the embedded document that tells the viewer that the result server serves it, so that it fetches what
    /// its cells expand from <c>../open</c>.
    /// </summary>
    public const string ServedProperty = "served";

    /// <summary>
    /// The property of the embedded document of a served page that holds the URL of the files of the query editor, relative
    /// to the page.
    /// </summary>
    public const string EditorAssetsProperty = "editorAssets";

    // The default encoder escapes <, >, & and quotes as \uXXXX, so a value that contains </script> cannot close the element.
    private static readonly JsonSerializerOptions _embeddingOptions = new() { Encoder = JavaScriptEncoder.Default };

    /// <summary>
    /// Creates the page of the MCP App, without data.
    /// </summary>
    public static string CreateAppPage() => CreatePage( null, "Metalama" );

    /// <summary>
    /// Gets the content security policy of the page. The page loads nothing and runs only its own script, allowed by its hash.
    /// A served page may also fetch from its own origin, which is the result server.
    /// </summary>
    public static string GetContentSecurityPolicy( bool served )
    {
        // SHA-256 is what the content security policy requires for script hashes.
        var scriptHash = Convert.ToBase64String( SHA256.HashData( Encoding.UTF8.GetBytes( ReadScript() ) ) );
        // A served page also loads the query editor from the server: its script, style sheet, font and worker.
        return served
            ? $"default-src 'none'; script-src 'sha256-{scriptHash}' 'self'; style-src 'self' 'unsafe-inline'; img-src data:; font-src 'self'; worker-src 'self'; base-uri 'none'; form-action 'none'; connect-src 'self'"
            : $"default-src 'none'; script-src 'sha256-{scriptHash}'; style-src 'unsafe-inline'; img-src data:; base-uri 'none'; form-action 'none'";
    }

    /// <summary>
    /// Creates a page. When <paramref name="document"/> is given, it is embedded. When <paramref name="served"/> is <c>true</c>,
    /// the page fetches what its cells expand from the result server; otherwise it needs nothing else.
    /// </summary>
    public static string CreatePage( JsonObject? document, string title, bool served = false )
    {
        // The HTML parser normalizes line endings to LF before the browser hashes an inline script, and the resources have
        // CRLF line endings in a Windows working tree, so they are normalized before they are hashed and embedded.
        var template = EmbeddedResources.Read( "Viewer.viewer.html" ).ReplaceLineEndings( "\n" );
        var script = ReadScript();
        var css = EmbeddedResources.Read( "Viewer.viewer.css" ).ReplaceLineEndings( "\n" );
        var csp = GetContentSecurityPolicy( served );

        if ( document != null && served )
        {
            document = (JsonObject) document.DeepClone();
            document[ServedProperty] = true;
            document[EditorAssetsProperty] = "../assets/" + EditorAssets.Version + "/";
        }

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
    /// Gets a short hash of the files of the viewer, which changes whenever the page of the MCP App changes.
    /// </summary>
    public static string GetAppPageVersion()
    {
        var content = string.Join(
            "\0",
            EmbeddedResources.Read( "Viewer.viewer.html" ).ReplaceLineEndings( "\n" ),
            ReadScript(),
            EmbeddedResources.Read( "Viewer.viewer.css" ).ReplaceLineEndings( "\n" ) );

        return Convert.ToHexStringLower( SHA256.HashData( Encoding.UTF8.GetBytes( content ) ) ).Substring( 0, 12 );
    }

    private static string ReadScript() => EmbeddedResources.Read( "Viewer.viewer.js" ).ReplaceLineEndings( "\n" );
}
