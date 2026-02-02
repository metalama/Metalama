// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of return statement with cast of await expression: return (T)await M(); or return (T)(await M());
/// </summary>
internal sealed class AwaitCastReturnStatementInliner : AsyncMethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: return (<type>)await <annotated_method_expression>( <arguments> );
        // or: return (<type>)(await <annotated_method_expression>( <arguments> ));
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression, possibly through parentheses.
        var possibleAwait = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent;

        if ( possibleAwait is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression (possibly parenthesized) should be inside a cast expression.
        var awaitOrParenthesized = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        if ( awaitOrParenthesized.Parent is not CastExpressionSyntax castExpression )
        {
            return false;
        }

        // The cast type should match the return type of the containing method.
        // For async methods, we need to get the awaited type (the T in Task<T>/ValueTask<T>/custom awaitable).
        var containingMethodReturnType = aspectReference.ContainingSemantic.Symbol.ReturnType;

        if ( !AsyncHelper.TryGetAsyncInfo( containingMethodReturnType, out var awaitedReturnType, out _ ) )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals( semanticModel.GetSymbolInfo( castExpression.Type ).Symbol, awaitedReturnType ) )
        {
            return false;
        }

        // The cast expression (possibly wrapped in additional casts) should be inside a return statement.
        // Skip through any outer cast expressions (e.g., when template has (int) and framework adds (global::System.Int32)).
        SyntaxNode castOrOuter = castExpression;

        while ( castOrOuter.Parent is CastExpressionSyntax )
        {
            castOrOuter = castOrOuter.Parent;
        }

        if ( castOrOuter.Parent is not ReturnStatementSyntax )
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

        // Navigate through parentheses to find the cast expression.
        var current = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        var castExpression = (CastExpressionSyntax) current.Parent.AssertNotNull();

        // Skip through any outer cast expressions.
        SyntaxNode castOrOuter = castExpression;

        while ( castOrOuter.Parent is CastExpressionSyntax )
        {
            castOrOuter = castOrOuter.Parent;
        }

        var returnStatement = (ReturnStatementSyntax) castOrOuter.Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }
}
