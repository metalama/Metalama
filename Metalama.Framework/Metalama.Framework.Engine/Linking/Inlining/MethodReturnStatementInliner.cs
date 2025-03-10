// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of return statement which invokes an annotated expression.
/// </summary>
internal sealed class MethodReturnStatementInliner : MethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: return <annotated_method_expression( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol methodSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals(
                methodSymbol.ReturnType,
                aspectReference.ContainingSemantic.Symbol.ReturnType ) )
        {
            return false;
        }

        if ( invocationExpression.Parent is not ReturnStatementSyntax )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !IsInlineableInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();
        var returnStatement = (ReturnStatementSyntax) invocationExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }
}