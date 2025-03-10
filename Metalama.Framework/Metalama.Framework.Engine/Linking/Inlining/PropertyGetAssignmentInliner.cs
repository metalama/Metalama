// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class PropertyGetAssignmentInliner : PropertyGetInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax needs to be in form: <variable> = <annotated_property_expression>;
        if ( aspectReference.ResolvedSemantic.Symbol is not IPropertySymbol
             && (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol is not IPropertySymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.Parent is not AssignmentExpressionSyntax assignmentExpression )
        {
            return false;
        }

        // The assignment should be part of expression statement.
        if ( assignmentExpression.Parent is not ExpressionStatementSyntax )
        {
            return false;
        }

        // Assignment should be simple and property access should be on the right.
        if ( assignmentExpression.Kind() != SyntaxKind.SimpleAssignmentExpression
             || assignmentExpression.Right != aspectReference.RootExpression )
        {
            return false;
        }

        // Assignment should have a local on the left (TODO: ref returns).
        if ( assignmentExpression.Left is not IdentifierNameSyntax || semanticModel.GetSymbolInfo( assignmentExpression.Left ).Symbol is not ILocalSymbol )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var assignmentExpression = (AssignmentExpressionSyntax) aspectReference.RootExpression.Parent.AssertNotNull();
        var localVariable = (IdentifierNameSyntax) assignmentExpression.Left.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) assignmentExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, localVariable.Identifier.Text );
    }
}