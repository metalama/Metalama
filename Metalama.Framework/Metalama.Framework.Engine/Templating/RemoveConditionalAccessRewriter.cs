// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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