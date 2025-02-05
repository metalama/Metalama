// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// If, due to some manipulation of syntax tree, we end up with preprocessor directives that are not preceded by a newline,
/// the compiler will report CS1040. This class fixes that.
/// </summary>
/// <remarks>
/// For example, this can happen when we transform this code:
/// <code>
/// #pragma warning disable CS8618
/// Foo P { get; }
/// </code>
/// Into:
/// <code>
/// private readonly #pragma warning disable CS8618
/// Foo _p;
/// </code>
/// </remarks>
internal static class PreprocessorFixer
{
    public static T Fix<T>( T node, SyntaxGenerationContext context )
        where T : SyntaxNode
    {
        if ( !node.ContainsDirectives )
        {
            return node;
        }

        var walker = new Walker();
        walker.Visit( node );

        if ( walker.TokensToFix == null )
        {
            return node;
        }

        return node.ReplaceTokens( walker.TokensToFix, ( _, token ) => token.WithTrailingTrivia( token.TrailingTrivia.AddOptionalLineFeed( context ) ) );
    }

    private sealed class Walker() : SafeSyntaxWalker( SyntaxWalkerDepth.Trivia )
    {
        private SyntaxToken _lastToken;
        private bool _justAfterNewline;

        public List<SyntaxToken>? TokensToFix { get; private set; }

        public override void VisitToken( SyntaxToken token )
        {
            this.VisitLeadingTrivia( token );

            this._lastToken = token;

            if ( token.Span.Length > 0 )
            {
                this._justAfterNewline = false;
            }

            this.VisitTrailingTrivia( token );
        }

        public override void VisitTrivia( SyntaxTrivia trivia )
        {
            if ( SyntaxFacts.IsPreprocessorDirective( trivia.Kind() ) )
            {
                if ( !this._justAfterNewline )
                {
                    this.TokensToFix ??= [];
                    this.TokensToFix.Add( this._lastToken );
                }

                this._justAfterNewline = true;
            }
            else if ( trivia.ToString() is [.., var ch] && SyntaxFacts.IsNewLine( ch ) )
            {
                // This can be EndOfLineTrivia, DisabledTextTrivia or possibly other trivia.
                this._justAfterNewline = true;
            }
            else if ( trivia.IsKind( SyntaxKind.WhitespaceTrivia ) )
            {
                // Do nothing, whitespace is allowed before preprocessor directives.
            }
            else
            {
                this._justAfterNewline = false;
            }
        }
    }
}