// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class EventRemoveAssignmentInliner : EventInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax needs to be in form: <annotated_property_expression> -= value;
        if ( aspectReference.ResolvedSemantic.Symbol is not IEventSymbol
             && (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol is not IEventSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.Parent is not AssignmentExpressionSyntax assignmentExpression )
        {
            return false;
        }

        // Property access should be on the left.
        if ( assignmentExpression.Left != aspectReference.RootExpression )
        {
            return false;
        }

        // Assignment should have a "value" identifier on the right.
        if ( assignmentExpression.Right is not IdentifierNameSyntax rightIdentifier ||
             !string.Equals( rightIdentifier.Identifier.ValueText, "value", StringComparison.Ordinal ) )
        {
            return false;
        }

        // Assignment should be simple.
        if ( assignmentExpression.Kind() != SyntaxKind.SubtractAssignmentExpression )
        {
            return false;
        }

        // The assignment should be part of expression statement.
        if ( assignmentExpression.Parent is not ExpressionStatementSyntax )
        {
            // Only incorrect code can get here.
            throw new AssertionFailedException( Justifications.CoverageMissing );

            // return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var assignmentExpression = (AssignmentExpressionSyntax) aspectReference.RootExpression.Parent.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) assignmentExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, null );
    }
}