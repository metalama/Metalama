// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

internal static class SyntaxTriviaExtensions
{
    public static SyntaxTriviaList StripFirstTrailingNewLine( this SyntaxTriviaList list )
    {
        var newTrivias = new List<SyntaxTrivia>();
        var firstNewLine = true;

        for ( var i = 0; i < list.Count; i++ )
        {
            if ( list[i].IsKind( SyntaxKind.EndOfLineTrivia ) && firstNewLine )
            {
                firstNewLine = false;
            }
            else
            {
                newTrivias.Add( list[i] );
            }
        }

        return SyntaxFactory.TriviaList( newTrivias );
    }

    public static bool HasAnyNewLine( this SyntaxTriviaList list ) => list.Any( x => x.IsKind( SyntaxKind.EndOfLineTrivia ) );
}