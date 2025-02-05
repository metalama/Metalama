// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

internal partial class MetaSyntaxRewriter
{
    private sealed class IndentRewriter : SafeSyntaxRewriter
    {
        private const int _limit = 80;
        private readonly MetaSyntaxRewriter _parent;

        public IndentRewriter( MetaSyntaxRewriter parent )
        {
            this._parent = parent;
        }

        public override SyntaxNode? VisitArgumentList( ArgumentListSyntax node )
        {
            if ( node.Arguments.Count > 1 || node.Arguments.Span.Length > _limit )
            {
                this._parent.Indent();

                try
                {
                    var indentedArguments =
                        node.Arguments.SelectAsReadOnlyList( a => ((ArgumentSyntax) this.Visit( a )!).WithLeadingTrivia( this._parent.GetIndentation() ) );

                    return SyntaxFactory.ArgumentList(
                        node.OpenParenToken.WithTrailingTrivia( this._parent.GetIndentation( false ) ),
                        SyntaxFactory.SeparatedList( indentedArguments ),
                        node.CloseParenToken );
                }
                finally
                {
                    this._parent.Unindent();
                }
            }

            return base.VisitArgumentList( node );
        }

        public override SyntaxNode? VisitInitializerExpression( InitializerExpressionSyntax node )
        {
            if ( node.Expressions.Count > 1 || node.Span.Length > _limit )
            {
                this._parent.Indent();

                try
                {
                    var indentedExpressions =
                        node.Expressions.SelectAsReadOnlyList( a => ((ExpressionSyntax) this.Visit( a )!).WithLeadingTrivia( this._parent.GetIndentation() ) );

                    return SyntaxFactory.InitializerExpression(
                        node.Kind(),
                        node.OpenBraceToken.WithTrailingTrivia( this._parent.GetIndentation( false ) ),
                        SyntaxFactory.SeparatedList( indentedExpressions ),
                        node.CloseBraceToken );
                }
                finally
                {
                    this._parent.Unindent();
                }
            }

            return base.VisitInitializerExpression( node );
        }

        protected override SyntaxNode? VisitCore( SyntaxNode? node )
        {
            if ( node == null || node.Span.Length < _limit || node.HasNoDeepIndentAnnotation() )
            {
                return node;
            }

            return base.VisitCore( node );
        }

        // Never indent within a string interpolation because CRLF are forbidden there.
        public override SyntaxNode VisitInterpolation( InterpolationSyntax node ) => node;
    }
}