// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Formatting
{
    internal static class EndOfLineHelper
    {
        /// <summary>
        /// Returns the first EOL string without allocating memory.
        /// </summary>
        public static string DetermineEndOfLineStyleFast( SyntaxTree syntaxTree )
            => DetermineEndOfLineStyleFast( syntaxTree.GetRoot() )
               ?? "\r\n";

        private static string? DetermineEndOfLineStyleFast( SyntaxTriviaList list )
        {
            foreach ( var trivia in list )
            {
                if ( trivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
                {
                    // The whole point of this game is to avoid memory allocation.
                    if ( trivia.SyntaxTree?.TryGetText( out var text ) != true || text == null )
                    {
                        return trivia.ToString();
                    }
                    else
                    {
                        switch ( trivia.Span.Length )
                        {
                            case 1:
                                switch ( text[trivia.SpanStart] )
                                {
                                    case '\r':
                                        return "\r";

                                    case '\n':
                                        return "\n";
                                }

                                break;

                            case 2:
                                switch (text[trivia.SpanStart], text[trivia.SpanStart + 1])
                                {
                                    case ('\r', '\n'):
                                        return "\r\n";
                                }

                                break;
                        }

                        return trivia.ToString();
                    }
                }
            }

            return null;
        }

        private static string? DetermineEndOfLineStyleFast( SyntaxNode node )
        {
            foreach ( var child in node.ChildNodesAndTokens() )
            {
                if ( child.IsNode )
                {
                    var eol = DetermineEndOfLineStyleFast( child.AsNode().AssertNotNull() );

                    if ( eol != null )
                    {
                        return eol;
                    }
                }
                else
                {
                    var token = child.AsToken();
                    var eol = DetermineEndOfLineStyleFast( token.LeadingTrivia ) ?? DetermineEndOfLineStyleFast( token.TrailingTrivia );

                    if ( eol != null )
                    {
                        return eol;
                    }
                }
            }

            return null;
        }
    }
}