// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of return statement with await expression: return await M(); or return (await M());
/// </summary>
internal sealed class AwaitReturnStatementInliner : AsyncMethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: return await <annotated_method_expression>( <arguments> );
        // or: return (await <annotated_method_expression>( <arguments> ));
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol methodSymbol )
        {
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression.
        if ( invocationExpression.Parent is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression should be inside a return statement, possibly through parentheses.
        var possibleReturn = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression ).Parent;

        if ( possibleReturn is not ReturnStatementSyntax )
        {
            return false;
        }

        // Return types must be compatible (the inner type of Task<T>/ValueTask<T>).
        var containingMethodReturnType = aspectReference.ContainingSemantic.Symbol.ReturnType;
        var targetMethodReturnType = methodSymbol.ReturnType;

        if ( !SignatureTypeComparer.Instance.Equals( containingMethodReturnType, targetMethodReturnType ) )
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
        var awaitExpression = (AwaitExpressionSyntax) invocationExpression.Parent.AssertNotNull();

        // Navigate through parentheses to find the return statement.
        var returnStatement = (ReturnStatementSyntax) InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression ).Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }
}
