// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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