// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class PropertyGetReturnInliner : PropertyGetInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax needs to be in form: return <annotated_property_expression>;
        if ( aspectReference.ResolvedSemantic.Symbol is not IPropertySymbol
             && (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol is not IPropertySymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        var propertySymbol =
            aspectReference.ResolvedSemantic.Symbol as IPropertySymbol
            ?? (IPropertySymbol) ((aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol).AssertNotNull();

        if ( !SymbolEqualityComparer.Default.Equals(
                propertySymbol.Type,
                aspectReference.ContainingSemantic.Symbol.ReturnType ) )
        {
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not ReturnStatementSyntax )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var returnStatement = (ReturnStatementSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();

        return new InliningAnalysisInfo( returnStatement, null );
    }
}