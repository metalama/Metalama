// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of return statement which invokes an annotated expression.
/// </summary>
internal sealed class MethodCastReturnStatementInliner : MethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: return (<type>)<annotated_method_expression( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation (possibly through parentheses or null-forgiving) should be inside a cast expression.
        var invocationOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression );

        if ( invocationOrWrapped.Parent is not CastExpressionSyntax castExpression )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals(
                semanticModel.GetSymbolInfo( castExpression.Type ).Symbol,
                aspectReference.ContainingSemantic.Symbol.ReturnType ) )
        {
            return false;
        }

        // The cast expression (possibly through parentheses or null-forgiving) should be inside a return statement.
        var castOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( castExpression );

        if ( castOrWrapped.Parent is not ReturnStatementSyntax )
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

        // Navigate through parentheses and null-forgiving to find the cast expression.
        var invocationOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression );
        var castExpression = (CastExpressionSyntax) invocationOrWrapped.Parent.AssertNotNull();

        // Navigate through parentheses and null-forgiving to find the return statement.
        var castOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( castExpression );
        var returnStatement = (ReturnStatementSyntax) castOrWrapped.Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }
}