// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Helper methods for inliners.
/// </summary>
internal static class InlinerHelper
{
    /// <summary>
    /// Determines if the invocation is canonical, i.e. arguments match exactly parameters.
    /// </summary>
    public static bool IsCanonicalInvocation(
        SemanticModel semanticModel,
        IMethodSymbol contextMethod,
        InvocationExpressionSyntax invocationExpression )
    {
        if ( IsCanonicalInvocationWithStaticReceiver( semanticModel, contextMethod, invocationExpression ) )
        {
            return true;
        }

        if ( invocationExpression.ArgumentList.Arguments.Count != contextMethod.Parameters.Length )
        {
            return false;
        }

        return invocationExpression.ArgumentList.Arguments
            .Select( ( x, i ) => (Argument: x.Expression, Index: i) )
            .All( a => SymbolEqualityComparer.Default.Equals( semanticModel.GetSymbolInfo( a.Argument ).Symbol, contextMethod.Parameters[a.Index] ) );
    }

    private static bool IsCanonicalInvocationWithStaticReceiver(
        SemanticModel semanticModel,
        IMethodSymbol contextMethod,
        InvocationExpressionSyntax invocationExpression )
    {
        if ( invocationExpression is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.HelperTypeName }
                }
            } )
        {
            // Fast check before we query symbols.
            return false;
        }

        var targetSymbol = ModelExtensions.GetSymbolInfo( semanticModel, invocationExpression.Expression ).Symbol;

        if ( targetSymbol is null || OperatorData.GetByName( targetSymbol.AssertNotNull().Name ) is not { IsStatic: false } )
        {
            // We currently treat only non-static operators this way.
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
            .Any(
                a => !SymbolEqualityComparer.Default.Equals(
                    ModelExtensions.GetSymbolInfo( semanticModel, a.Argument ).Symbol,
                    contextMethod.Parameters[a.Index] ) ) )
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Walks up through parent nodes while they are parenthesized expressions.
    /// Returns the outermost node that is not wrapped in parentheses.
    /// </summary>
    public static SyntaxNode SkipParenthesizedExpressionAncestors( SyntaxNode node )
    {
        while ( node.Parent is ParenthesizedExpressionSyntax )
        {
            node = node.Parent;
        }

        return node;
    }

    /// <summary>
    /// Determines if an assignment expression is a discard assignment (e.g., _ = expr).
    /// </summary>
    public static bool IsDiscardAssignment( AssignmentExpressionSyntax assignmentExpression )
        => assignmentExpression.Kind() == SyntaxKind.SimpleAssignmentExpression
           && assignmentExpression.Left is IdentifierNameSyntax identifierName
           && string.Equals( identifierName.Identifier.ValueText, "_", StringComparison.Ordinal );
}
