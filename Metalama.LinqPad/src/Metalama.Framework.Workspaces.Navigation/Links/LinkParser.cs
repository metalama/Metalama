// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// Parses absolute and relative links. Decoding is lenient: percent-encoded and raw segments are both accepted, because
/// agents type <c>#ctor</c> without encoding it.
/// </summary>
internal static partial class LinkParser
{
    [GeneratedRegex( @"\?v=(?<v>[0-9a-fA-F]+)$" )]
    private static partial Regex FingerprintRegex();

    public static bool TryParse( string text, LinkBase? linkBase, [NotNullWhen( true )] out Link? link, [NotNullWhen( false )] out string? error )
    {
        link = null;
        text = text.Trim();

        if ( text.Length == 0 )
        {
            error = "The link is empty.";

            return false;
        }

        string? fingerprint = null;
        var fingerprintMatch = FingerprintRegex().Match( text );

        if ( fingerprintMatch.Success )
        {
            fingerprint = fingerprintMatch.Groups["v"].Value.ToLowerInvariant();
            text = text.Substring( 0, fingerprintMatch.Index );
        }

        string? workspace;
        string[] segments;
        int index;

        if ( text.StartsWith( Link.Scheme, StringComparison.OrdinalIgnoreCase ) )
        {
            segments = text.Substring( Link.Scheme.Length ).Split( '/' );
            workspace = Unescape( segments[0] );

            if ( workspace.Length == 0 )
            {
                workspace = linkBase?.Workspace;
            }

            index = 1;

            if ( segments.Length < 4 )
            {
                error = $"The link '{text}' must have the form metalama://<workspace>/<scope>/<source|transformed>/<anchor>[/<step>]*.";

                return false;
            }
        }
        else
        {
            segments = text.Split( '/' );
            workspace = linkBase?.Workspace;
            index = 0;
        }

        string scope;
        bool isTransformed;

        if ( segments.Length >= index + 3 && IsCodeVersion( segments[index + 1] ) )
        {
            scope = Unescape( segments[index] );
            isTransformed = segments[index + 1] == "transformed";
            index += 2;
        }
        else if ( index == 0 && linkBase?.Scope != null )
        {
            scope = linkBase.Scope;
            isTransformed = linkBase.IsTransformed;
        }
        else
        {
            error =
                $"The link '{text}' has no scope, and the workspace has no default scope because it has several projects or target frameworks. Use the form <project>@<tfm>/source/<anchor>.";

            return false;
        }

        if ( index >= segments.Length || segments[index].Length == 0 )
        {
            error = $"The link '{text}' has no anchor.";

            return false;
        }

        string anchor;

        if ( segments[index] == "query" )
        {
            if ( index + 1 >= segments.Length )
            {
                error = $"The query anchor of the link '{text}' has no code.";

                return false;
            }

            anchor = Link.QueryAnchorPrefix + segments[index + 1];
            index += 2;
        }
        else
        {
            anchor = Unescape( segments[index] );
            index++;
        }

        var steps = segments.Skip( index ).Where( s => s.Length > 0 ).Select( Unescape ).ToImmutableArray();

        link = new Link( workspace, scope, isTransformed, anchor, steps, fingerprint );
        error = null;

        return true;
    }

    private static bool IsCodeVersion( string segment ) => segment is "source" or "transformed";

    private static string Unescape( string segment )
    {
        try
        {
            return Uri.UnescapeDataString( segment );
        }
        catch ( UriFormatException )
        {
            return segment;
        }
    }
}
