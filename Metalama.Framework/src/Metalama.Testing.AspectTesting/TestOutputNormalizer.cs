// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;

namespace Metalama.Testing.AspectTesting;

[PublicAPI]
public static class TestOutputNormalizer
{
    private static readonly Regex _spaceRegex = new( "\\s+", RegexOptions.Compiled );
    private static readonly Regex _newLineRegex = new( "(\\s*(\r\n|\r|\n))", RegexOptions.Compiled | RegexOptions.Multiline );

    internal static string NormalizeEndOfLines( string? s, bool replaceWithSpace = false )
        => string.IsNullOrWhiteSpace( s ) ? "" : _newLineRegex.Replace( s, replaceWithSpace ? " " : "\r\n" ).Trim();

    public static string? NormalizeTestOutput( string? s, bool preserveFormatting, bool forComparison )
        => s == null ? null : NormalizeTestOutput( CSharpSyntaxTree.ParseText( s ).GetRoot(), preserveFormatting, forComparison );

    private static string NormalizeTestOutput( SyntaxNode syntaxNode, bool preserveFormatting, bool forComparison )
    {
        if ( preserveFormatting )
        {
            return NormalizeEndOfLines( syntaxNode.ToFullString() );
        }
        else
        {
            // The following line might remove linebreaks between a } and a //.
            var s = syntaxNode.NormalizeWhitespace( "  " ).ToFullString();

            s = NormalizeEndOfLines( s, forComparison );

            if ( forComparison )
            {
                s = _spaceRegex.Replace( s, " " );
            }

            return s;
        }
    }
}