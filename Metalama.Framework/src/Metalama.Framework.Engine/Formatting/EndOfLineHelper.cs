// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Formatting
{
    internal static class EndOfLineHelper
    {
        /// <summary>
        /// Normalizes all <see cref="SyntaxKind.EndOfLineTrivia"/> in a syntax tree to use the specified EOL string.
        /// </summary>
        public static SyntaxNode NormalizeEndOfLines( SyntaxNode root, string endOfLine ) => new EndOfLineNormalizingRewriter( endOfLine ).Visit( root )!;

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

        private sealed class EndOfLineNormalizingRewriter : CSharpSyntaxRewriter
        {
            private readonly string _endOfLine;
            private readonly SyntaxTrivia _endOfLineTrivia;

            public EndOfLineNormalizingRewriter( string endOfLine ) : base( visitIntoStructuredTrivia: true )
            {
                this._endOfLine = endOfLine;
                this._endOfLineTrivia = SyntaxFactory.EndOfLine( endOfLine );
            }

            public override SyntaxTrivia VisitTrivia( SyntaxTrivia trivia )
            {
                if ( !trivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
                {
                    return base.VisitTrivia( trivia );
                }

                // If the trivia already has the target content, return it unchanged
                // to avoid allocating a new object and forcing reallocation of the tree branch.
                if ( this.IsAlreadyNormalized( trivia ) )
                {
                    return trivia;
                }

                return this._endOfLineTrivia;
            }

            private bool IsAlreadyNormalized( SyntaxTrivia trivia )
            {
                if ( trivia.Span.Length != this._endOfLine.Length )
                {
                    return false;
                }

                // Length 2 is unambiguously \r\n for EndOfLineTrivia. For length 1, check the character.
                if ( this._endOfLine.Length == 1
                     && trivia.SyntaxTree?.TryGetText( out var text ) == true )
                {
                    return text[trivia.SpanStart] == this._endOfLine[0];
                }

                return true;
            }
        }
    }
}