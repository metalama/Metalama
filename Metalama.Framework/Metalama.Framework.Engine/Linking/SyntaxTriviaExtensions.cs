// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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