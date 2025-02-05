// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;

namespace Metalama.Framework.Engine.Formatting;

public sealed partial class CodeFormatter
{
    /// <summary>
    /// Fixes the output of <see cref="Simplifier"/>.
    /// </summary>
    /// <remarks>
    /// It seems that <see cref="Simplifier"/> can remove an EOL trivia after single-line comment. We detect and remediate this situation.
    /// </remarks>
    private sealed class SimplifierFixer : SafeSyntaxRewriter
    {
        private SyntaxTrivia _lastEndOfLineTrivia = SyntaxFactory.EndOfLine( "\n" );

        public override SyntaxToken VisitToken( SyntaxToken token )
        {
            if ( token.HasLeadingTrivia )
            {
                SyntaxKind lastTrivia = default;

                foreach ( var trivia in token.LeadingTrivia )
                {
                    var kind = trivia.Kind();

                    switch ( kind )
                    {
                        case SyntaxKind.EndOfLineTrivia:
                            // We remember the last EOL trivia so we can know the EOL style.
                            this._lastEndOfLineTrivia = trivia;
                            lastTrivia = kind;

                            break;

                        case SyntaxKind.SingleLineCommentTrivia:
                            lastTrivia = kind;

                            break;
                    }
                }

                if ( lastTrivia == SyntaxKind.SingleLineCommentTrivia )
                {
#pragma warning disable LAMA0832
                    return token.WithLeadingTrivia( token.LeadingTrivia.Add( this._lastEndOfLineTrivia ) );
#pragma warning restore LAMA0832
                }
            }

            return token;
        }
    }
}