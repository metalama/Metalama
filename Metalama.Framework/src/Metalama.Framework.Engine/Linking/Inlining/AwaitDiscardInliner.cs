// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of discard assignment with await expression: _ = await M(); or _ = (await M());
/// </summary>
internal sealed class AwaitDiscardInliner : AsyncMethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: _ = await <annotated_method_expression>( <arguments> );
        // or: _ = (await <annotated_method_expression>( <arguments> ));
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression, possibly through parentheses.
        var possibleAwait = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent;

        if ( possibleAwait is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression (possibly parenthesized) should be the right side of an assignment.
        var awaitOrParenthesized = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        if ( awaitOrParenthesized.Parent is not AssignmentExpressionSyntax assignmentExpression )
        {
            return false;
        }

        // Invocation should be on the right.
        if ( assignmentExpression.Right != awaitOrParenthesized )
        {
            throw new AssertionFailedException( Justifications.CoverageMissing );
        }

        // Assignment should have a discard identifier on the left.
        if ( !InlinerHelper.IsDiscardAssignment( assignmentExpression ) )
        {
            return false;
        }

        // The assignment should be part of expression statement.
        if ( assignmentExpression.Parent is not ExpressionStatementSyntax )
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

        // Navigate through parentheses to find the await expression.
        var awaitExpression = (AwaitExpressionSyntax) InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent.AssertNotNull();

        // Navigate through parentheses to find the assignment.
        var current = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        var assignmentExpression = (AssignmentExpressionSyntax) current.Parent.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) assignmentExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, null );
    }
}
