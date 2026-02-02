// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
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

        // The invocation should be inside an await expression.
        if ( invocationExpression.Parent is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression (possibly parenthesized) should be inside a cast expression.
        SyntaxNode awaitOrParenthesized = awaitExpression;

        while ( awaitOrParenthesized.Parent is ParenthesizedExpressionSyntax )
        {
            awaitOrParenthesized = awaitOrParenthesized.Parent;
        }

        if ( awaitOrParenthesized.Parent is not CastExpressionSyntax castExpression )
        {
            return false;
        }

        // The cast type should match the return type of the containing method.
        // For async methods, we need to get the awaited type (the T in Task<T>).
        var containingMethodReturnType = aspectReference.ContainingSemantic.Symbol.ReturnType;
        var awaitedReturnType = GetAwaitedType( containingMethodReturnType );

        if ( awaitedReturnType is null )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals( semanticModel.GetSymbolInfo( castExpression.Type ).Symbol, awaitedReturnType ) )
        {
            return false;
        }

        // The cast expression should be inside a return statement.
        if ( castExpression.Parent is not ReturnStatementSyntax )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !IsCanonicalInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();
        var awaitExpression = (AwaitExpressionSyntax) invocationExpression.Parent.AssertNotNull();

        // Navigate through parentheses.
        SyntaxNode current = awaitExpression;

        while ( current.Parent is ParenthesizedExpressionSyntax )
        {
            current = current.Parent;
        }

        var castExpression = (CastExpressionSyntax) current.Parent.AssertNotNull();
        var returnStatement = (ReturnStatementSyntax) castExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }

    private static ITypeSymbol? GetAwaitedType( ITypeSymbol returnType )
    {
        // For Task<T>, ValueTask<T>, get T.
        // For Task, ValueTask, return null (void).
        if ( returnType is INamedTypeSymbol namedType )
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString();

            if ( fullName is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>" )
            {
                return namedType.TypeArguments[0];
            }
        }

        return null;
    }
}
