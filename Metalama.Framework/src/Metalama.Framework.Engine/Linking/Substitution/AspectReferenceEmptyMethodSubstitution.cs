// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference to an empty method (introduced method with no base implementation)
/// by replacing the invocation with a default value (for expression context) or removing the statement
/// (for statement context), instead of generating a separate empty method.
/// </summary>
internal sealed class AspectReferenceEmptyMethodSubstitution : SyntaxNodeSubstitution
{
    private readonly bool _isStatementContext;
    private readonly ITypeSymbol _resultType;

    public override SyntaxNode ReplacedNode { get; }

    public AspectReferenceEmptyMethodSubstitution(
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        // Support only base semantics for introduced methods with no base.
        Invariant.Assert( aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Base );

        Invariant.Assert(
            !aspectReference.ResolvedSemantic.Symbol.IsOverride
            && !aspectReference.ResolvedSemantic.Symbol.TryGetHiddenSymbol( compilationContext.Compilation, out _ ) );

        // Determine the method symbol and its return type.
        var methodSymbol = (IMethodSymbol) aspectReference.ResolvedSemantic.Symbol;

        if ( !AsyncHelper.TryGetAsyncInfo( methodSymbol.ReturnType, out var resultType, out _ ) )
        {
            resultType = methodSymbol.ReturnType;
        }

        this._resultType = resultType;

        // Determine the invocation expression (parent of the root node).
        var invocationNode = FindInvocationExpression( aspectReference.RootNode );

        // Determine whether the invocation is used as a statement or as an expression.
        if ( invocationNode.Parent.IsKind( SyntaxKind.ExpressionStatement ) && invocationNode.Parent is ExpressionStatementSyntax expressionStatement )
        {
            this._isStatementContext = true;
            this.ReplacedNode = expressionStatement;
        }
        else
        {
            this._isStatementContext = false;
            this.ReplacedNode = invocationNode;
        }
    }

    private static SyntaxNode FindInvocationExpression( SyntaxNode rootNode )
    {
        // The RootNode is typically the MemberAccessExpressionSyntax (e.g., this.Foo).
        // Its parent should be the InvocationExpressionSyntax (e.g., this.Foo()).
        // For the Link(This.Foo, Base)() pattern in linker tests, the RootNode is the inner
        // InvocationExpressionSyntax, and the outer InvocationExpressionSyntax is the parent.

        var current = rootNode;

        // Walk up to find the invocation expression that calls the referenced method.
        if ( current.Parent != null )
        {
            if ( current.Parent.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent is InvocationExpressionSyntax invocation
                 && invocation.Expression == current )
            {
                return invocation;
            }

            // For cases where rootNode itself is an invocation (e.g., Link(This.Foo, Base)),
            // and the parent is the outer invocation: Link(This.Foo, Base)().
            if ( current.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent is InvocationExpressionSyntax outerInvocation
                 && outerInvocation.Expression == current )
            {
                return outerInvocation;
            }
        }

        // Fallback: if rootNode itself is an invocation.
        if ( rootNode.IsKind( SyntaxKind.InvocationExpression ) )
        {
            return rootNode;
        }

        throw new AssertionFailedException( $"Could not find invocation expression for root node: {rootNode}" );
    }

    public override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        if ( this._isStatementContext )
        {
            // For statement context (void or non-void used as statement), remove the statement entirely.
            return null;
        }
        else
        {
            // For expression context, replace the invocation with default(ReturnType).
            return DefaultExpression( substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( this._resultType ) );
        }
    }
}
