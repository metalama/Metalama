// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of return statement which invokes an annotated expression.
/// </summary>
internal sealed class MethodDiscardInliner : MethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: _ = <annotated_method_expression( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        if ( invocationExpression.Parent is not AssignmentExpressionSyntax assignmentExpression )
        {
            return false;
        }

        // Invocation should be on the right.
        if ( assignmentExpression.Right != invocationExpression )
        {
            // Only incorrect code can get here.
            throw new AssertionFailedException( Justifications.CoverageMissing );

            // return false;
        }

        // Assignment should have a discard identifier on the left (TODO: ref returns).
        if ( assignmentExpression.Kind() != SyntaxKind.SimpleAssignmentExpression
             || assignmentExpression.Left is not IdentifierNameSyntax identifierName
             || !string.Equals( identifierName.Identifier.ValueText, "_", StringComparison.Ordinal ) )
        {
            return false;
        }

        // The assignment should be part of expression statement.
        if ( assignmentExpression.Parent is not ExpressionStatementSyntax )
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
        var assignmentExpression = (AssignmentExpressionSyntax) invocationExpression.Parent.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) assignmentExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, null );
    }
}