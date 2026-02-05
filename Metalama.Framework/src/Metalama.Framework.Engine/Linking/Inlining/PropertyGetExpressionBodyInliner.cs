// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

// ReSharper disable once UnusedType.Global
internal sealed class PropertyGetExpressionBodyInliner : PropertyGetInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax needs to be in form: return <annotated_property_expression>;
        if ( aspectReference.ResolvedSemantic.Symbol.Kind != SymbolKind.Property
             && (aspectReference.ResolvedSemantic.Symbol.Kind != SymbolKind.Method
                 || aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol
                 || (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol?.Kind != SymbolKind.Property
                 || (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol is not IPropertySymbol) )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        var propertySymbol =
            aspectReference.ResolvedSemantic.Symbol.Kind == SymbolKind.Property && aspectReference.ResolvedSemantic.Symbol is IPropertySymbol property
                ? property
                : (IPropertySymbol) ((aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol).AssertNotNull();

        if ( !SymbolEqualityComparer.Default.Equals(
                propertySymbol.Type,
                aspectReference.ContainingSemantic.Symbol.ReturnType ) )
        {
            return false;
        }

        // The property access (possibly through parentheses or null-forgiving) should be inside an arrow expression clause.
        var expressionOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( aspectReference.RootExpression.AssertNotNull() );

        if ( !expressionOrWrapped.Parent.IsKind( SyntaxKind.ArrowExpressionClause ) || expressionOrWrapped.Parent is not ArrowExpressionClauseSyntax )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        // Navigate through parentheses and null-forgiving to find the arrow expression clause.
        var expressionOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( aspectReference.RootExpression.AssertNotNull() );
        var arrowExpressionClause = (ArrowExpressionClauseSyntax) expressionOrWrapped.Parent.AssertNotNull();

        return new InliningAnalysisInfo( arrowExpressionClause, null );
    }
}