// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal partial class AspectReferenceRenamingSubstitution
{
    private sealed class ConditionalAccessRewriter : SafeSyntaxRewriter
    {
        private readonly string _replacingIdentifier;
        private ConditionalAccessExpressionSyntax? _context;

        public ConditionalAccessRewriter( string replacingIdentifier )
        {
            this._replacingIdentifier = replacingIdentifier;
        }

        public override SyntaxNode VisitConditionalAccessExpression( ConditionalAccessExpressionSyntax node )
        {
            if ( this._context == null )
            {
                this._context = node;

                var result = node.WithWhenNotNull( (ExpressionSyntax) this.Visit( node.WhenNotNull )! );

                this._context = null;

                return result;
            }
            else
            {
                // Template compiler currently does not generate expressions that would chain x?.y?.z without parentheses.
                throw new AssertionFailedException( Justifications.CoverageMissing );

                // return node;
            }
        }

        public override SyntaxNode VisitMemberBindingExpression( MemberBindingExpressionSyntax node )
        {
            if ( this._context != null )
            {
                return node.WithName( IdentifierName( this._replacingIdentifier ) );
            }
            else
            {
                // Template compiler currently does not generate expressions that would chain x?.y?.z without parentheses.
                throw new AssertionFailedException( Justifications.CoverageMissing );

                // return node;
            }
        }
    }
}