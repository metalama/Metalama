// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal static class RoslynHelper
{
    /// <summary>
    /// Determines if the current <see cref="SyntaxNode"/> is part of an expression that will be written to and/or read from.
    /// </summary>
    /// <param name="node">
    /// The current node. If the node is not part of an expression to which the concept of read/write applies 
    /// (for example, a variable declaration), this method may return <see cref="AccessKind.Read"/> instead of 
    /// <see cref="AccessKind.Undefined"/>.
    /// </param>
    /// <returns></returns>
    public static AccessKind GetAccessKind( this SyntaxNode? node )
    {
        if ( node == null )
        {
            return AccessKind.Undefined;
        }

        var parent = node.Parent;

        if ( parent == null )
        {
            return AccessKind.Undefined;
        }

        var parentKind = parent.Kind();

        switch ( parentKind )
        {
            case SyntaxKind.PostIncrementExpression:
            case SyntaxKind.PostDecrementExpression:
            case SyntaxKind.PreIncrementExpression:
            case SyntaxKind.PreDecrementExpression:
                return AccessKind.ReadWrite;

            case SyntaxKind.MemberBindingExpression:
                return GetAccessKind( parent );
        }

        switch ( parent )
        {
            case AssignmentExpressionSyntax assignmentExpression:
                return assignmentExpression.Left == node
                    ? parentKind == SyntaxKind.SimpleAssignmentExpression ? AccessKind.Write : AccessKind.ReadWrite
                    : AccessKind.Read;

            case MemberAccessExpressionSyntax memberAccessExpression:
                return memberAccessExpression.Name == node
                    ? GetAccessKind( parent )
                    : AccessKind.Read;

            case ConditionalAccessExpressionSyntax conditionalAccessExpression:
                return conditionalAccessExpression.WhenNotNull == node
                    ? GetAccessKind( parent )
                    : AccessKind.Read;

            case ParenthesizedExpressionSyntax:
                return GetAccessKind( parent );
        }

        // NB: Returns Read even for nodes where calling this method makes no sense, for example a variable declaration.
        // In current use cases there's no benefit to having accurate Undefined returns, so we don't bother to work it out.
        return AccessKind.Read;
    }
}