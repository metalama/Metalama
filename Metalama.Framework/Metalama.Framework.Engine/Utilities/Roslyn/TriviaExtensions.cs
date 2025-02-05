// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class TriviaExtensions
{
    private static int FindLastNonWhitespaceTrivia( SyntaxTriviaList list )
    {
        for ( var i = list.Count - 1; i >= 0; i-- )
        {
            var trivia = list[i];

            if ( !trivia.IsKind( SyntaxKind.WhitespaceTrivia ) && !trivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFirstNonWhitespaceTrivia( SyntaxTriviaList list )
    {
        for ( var i = 0; i < list.Count; i++ )
        {
            var trivia = list[i];

            if ( !trivia.IsKind( SyntaxKind.WhitespaceTrivia ) && !trivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
            {
                return i;
            }
        }

        return -1;
    }

    public static SyntaxTriviaList InsertAfterFirstNonWhitespaceTrivia( this SyntaxTriviaList list, params SyntaxTrivia[] trivia )
    {
        var index = FindLastNonWhitespaceTrivia( list );

        if ( index < 0 || index >= list.Count )
        {
            return list.AddRange( trivia );
        }
        else
        {
            return list.InsertRange( index + 1, trivia );
        }
    }

    public static SyntaxTriviaList InsertBeforeLastNonWhitespaceTrivia( this SyntaxTriviaList list, params SyntaxTrivia[] trivia )
    {
        var index = FindFirstNonWhitespaceTrivia( list );

        if ( index < 0 )
        {
            return list.InsertRange( 0, trivia );
        }
        else if ( index < list.Count - 2 )
        {
            return list.InsertRange( index + 1, trivia );
        }
        else
        {
            return list.AddRange( list );
        }
    }
}