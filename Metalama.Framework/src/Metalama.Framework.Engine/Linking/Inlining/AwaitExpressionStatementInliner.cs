// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of await expression as a statement: await M();
/// Used for async methods returning Task/ValueTask (void-like).
/// </summary>
internal sealed class AwaitExpressionStatementInliner : AsyncMethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: await <annotated_method_expression>( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol.Kind != SymbolKind.Method || aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            return false;
        }

        if ( !aspectReference.RootExpression.AssertNotNull().Parent.IsKind( SyntaxKind.InvocationExpression )
             || aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression, possibly through parentheses.
        // Note: await (M()); is valid, but (await M()); is not valid C# as a statement.
        var possibleAwait = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent;

        if ( !possibleAwait.IsKind( SyntaxKind.AwaitExpression ) || possibleAwait is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression should be directly inside an expression statement.
        // Note: (await M()); is not valid C# - a parenthesized expression cannot be a statement.
        if ( !awaitExpression.Parent.IsKind( SyntaxKind.ExpressionStatement ) || awaitExpression.Parent is not ExpressionStatementSyntax )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !InlinerHelper.IsCanonicalInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();

        // Navigate through parentheses to find the await expression.
        var awaitExpression = (AwaitExpressionSyntax) InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) awaitExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, null );
    }
}