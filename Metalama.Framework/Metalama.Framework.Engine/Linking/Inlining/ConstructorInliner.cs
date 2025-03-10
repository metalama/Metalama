// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Constructor inliner.
/// </summary>
internal sealed class ConstructorInliner : Inliner
{
    private static bool IsInlineableObjectCreation(
        SemanticModel semanticModel,
        IMethodSymbol contextConstructor,
        ObjectCreationExpressionSyntax objectCreationExpression )
    {
        var (expectedNumberOfParameters, argumentMapFunc) =
            contextConstructor.Parameters switch
            {
                [.., { Name: AspectReferenceSyntaxProvider.LinkerOverrideParamName }, { IsParams: true }] =>
                    (contextConstructor.Parameters.Length - 1, i => i < contextConstructor.Parameters.Length - 2 ? i : i + 1),
                [.., { Name: AspectReferenceSyntaxProvider.LinkerOverrideParamName }] =>
                    (contextConstructor.Parameters.Length - 1, i => i),
                _ =>
                    (contextConstructor.Parameters.Length, (Func<int, int>) (i => i)),
            };

        return
            expectedNumberOfParameters == (objectCreationExpression.ArgumentList?.Arguments.Count ?? 0)
            && (objectCreationExpression.ArgumentList?.Arguments
                    .Select( ( x, i ) => (Argument: x.Expression, Index: i) )
                    .All(
                        a => SymbolEqualityComparer.Default.Equals(
                            semanticModel.GetSymbolInfo( a.Argument ).Symbol,
                            contextConstructor.Parameters[argumentMapFunc( a.Index )] ) )
                ?? false);
    }

    public override bool IsValidForTargetSymbol( ISymbol symbol ) => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };

    public override bool IsValidForContainingSymbol( ISymbol symbol ) => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };

    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: <annotated_constructor_expression>( new <type>(<arguments>) );
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } )
        {
            // Coverage: ignore (hit only when the check IsValidForTargetSymbol check is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        if ( invocationExpression.Parent is not ExpressionStatementSyntax )
        {
            return false;
        }

        if ( invocationExpression.ArgumentList is not { Arguments: [{ Expression: ObjectCreationExpressionSyntax objectCreationExpression }] } )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !IsInlineableObjectCreation( semanticModel, aspectReference.ContainingSemantic.Symbol, objectCreationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
        => new( aspectReference.RootExpression.Parent.AssertNotNull(), null );
}