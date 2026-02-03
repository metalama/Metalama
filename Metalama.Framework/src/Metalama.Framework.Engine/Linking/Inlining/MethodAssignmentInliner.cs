// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class MethodAssignmentInliner : MethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: <local> = <annotated_method_expression( <arguments> );
        if ( aspectReference.ResolvedSemantic.Symbol.Kind != SymbolKind.Method || aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol )
        {
            // Coverage: ignore (hit only when the check in base class is incorrect).
            return false;
        }

        if ( !aspectReference.RootExpression.AssertNotNull().Parent.IsKind( SyntaxKind.InvocationExpression ) || aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation (possibly through parentheses or null-forgiving) should be the right side of an assignment.
        var invocationOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression );

        if ( !invocationOrWrapped.Parent.IsKind( SyntaxKind.SimpleAssignmentExpression ) || invocationOrWrapped.Parent is not AssignmentExpressionSyntax assignmentExpression )
        {
            return false;
        }

        // Assignment should be simple and Invocation should be on the right.
        if ( assignmentExpression.Kind() != SyntaxKind.SimpleAssignmentExpression
             || assignmentExpression.Right != invocationOrWrapped )
        {
            return false;
        }

        // Assignment should have a local on the left.
        var leftSymbol = semanticModel.GetSymbolInfo( assignmentExpression.Left ).Symbol;
        if ( !assignmentExpression.Left.IsKind( SyntaxKind.IdentifierName ) || assignmentExpression.Left is not IdentifierNameSyntax || leftSymbol?.Kind != SymbolKind.Local || leftSymbol is not ILocalSymbol )
        {
            return false;
        }

        // The assignment should be part of expression statement.
        if ( !assignmentExpression.Parent.IsKind( SyntaxKind.ExpressionStatement ) || assignmentExpression.Parent is not ExpressionStatementSyntax )
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

        // Navigate through parentheses and null-forgiving to find the assignment.
        var invocationOrWrapped = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression );

        var assignmentExpression = (AssignmentExpressionSyntax) invocationOrWrapped.Parent.AssertNotNull();
        var localVariable = (IdentifierNameSyntax) assignmentExpression.Left.AssertNotNull();
        var expressionStatement = (ExpressionStatementSyntax) assignmentExpression.Parent.AssertNotNull();

        return new InliningAnalysisInfo( expressionStatement, localVariable.Identifier.Text );
    }
}