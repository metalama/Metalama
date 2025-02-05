// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// Replaces the leftmost binding expression with its "access" version. E.g. it can turn <c>.b?.c</c> into <c>a.b?.c</c>.
/// Used in the process of removing a conditional access expression.
/// Can only be used once.
/// </summary>
internal sealed class RemoveConditionalAccessRewriter : SafeSyntaxRewriter
{
    private readonly ExpressionSyntax _expression;
    private bool _done;

    public RemoveConditionalAccessRewriter( ExpressionSyntax expression )
    {
        this._expression = expression;
    }

    protected override SyntaxNode? VisitCore( SyntaxNode? node )
    {
        if ( this._done )
        {
            return node;
        }

        return base.VisitCore( node );
    }

    public override SyntaxNode VisitMemberBindingExpression( MemberBindingExpressionSyntax node )
    {
        this._done = true;

        return SyntaxFactory.MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, this._expression, node.Name )
            .WithSymbolAnnotationsFrom( node );
    }

    public override SyntaxNode VisitElementBindingExpression( ElementBindingExpressionSyntax node )
    {
        this._done = true;

        return SyntaxFactory.ElementAccessExpression( this._expression, node.ArgumentList )
            .WithSymbolAnnotationsFrom( node );
    }
}