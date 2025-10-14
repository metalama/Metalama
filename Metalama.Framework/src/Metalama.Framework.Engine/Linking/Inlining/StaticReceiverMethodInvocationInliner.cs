// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles the inlining of the invocation of an instance method subsituted with a static method where the
/// first parameter is the receiver.
/// </summary>
internal sealed class StaticReceiverMethodInvocationInliner : MethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: <annotated_method_expression( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        if ( invocationExpression.Parent is not ExpressionStatementSyntax )
        {
            return false;
        }

        if ( !IsCanonicalInvocationWithStaticReceiver( invocationExpression, aspectReference.ContainingSemantic.Symbol, semanticModel ) )
        {
            return false;
        }

        return true;
    }

    private static bool IsCanonicalInvocationWithStaticReceiver(
        InvocationExpressionSyntax invocationExpression,
        IMethodSymbol contextMethod,
        SemanticModel semanticModel )
    {
        if ( !invocationExpression.HasAnnotation( LinkerInjectionHelperProvider.HasStaticReceiverArgumentAnnotation ) )
        {
            return false;
        }
        
        if ( invocationExpression.ArgumentList.Arguments.Count != contextMethod.Parameters.Length + 1 )
        {
            return false;
        }

        // Check receiver argument.
        if ( !invocationExpression.ArgumentList.Arguments[0].Expression.IsKind( SyntaxKind.ThisExpression ) )
        {
            return false;
        }

        // Check other arguments.
        if ( invocationExpression.ArgumentList.Arguments
            .Skip( 1 )
            .Select( ( x, i ) => (Argument: x.Expression, Index: i) )
            .Any( a => !SymbolEqualityComparer.Default.Equals(
                      ModelExtensions.GetSymbolInfo( semanticModel, a.Argument ).Symbol,
                      contextMethod.Parameters[a.Index] ) ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) invocationExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, null );
    }
}