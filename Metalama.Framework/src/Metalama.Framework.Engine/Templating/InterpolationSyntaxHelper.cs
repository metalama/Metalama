// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating;

internal static class InterpolationSyntaxHelper
{
    public static InterpolationSyntax Fix( InterpolationSyntax interpolation )
    {
        // NormalizeWhitespace adds the spaces that are missing between the tokens of a generated expression. It is recursive
        // and can overflow the stack on a deep expression (see #2083), so we only add the missing spaces in this case.
        var normalizedInterpolation = interpolation.CanNormalizeWhitespace()
            ? interpolation.NormalizeWhitespace()
            : interpolation.AddMissingTokenSeparators();

        // Interpolations cannot contain EOL, so we need to remove them.
        var fixedInterpolation = (InterpolationSyntax) new RemoveEndOfLinesRewriter().Visit( normalizedInterpolation )!;

        // If the interpolation expression contains an alias-prefixed identifier (for instance global::System) that is not
        // in a parenthesis or a square bracket, we need to parenthesize the expression.
        if ( RequiresParentheses( fixedInterpolation ) )
        {
            return fixedInterpolation.WithExpression( SyntaxFactory.ParenthesizedExpression( fixedInterpolation.Expression ) );
        }
        else
        {
            return fixedInterpolation;
        }
    }

    /// <summary>
    /// Determines whether the given interpolation contains an alias-qualified name or a string literal that is not in a
    /// parenthesis, in an argument list, or in the brackets of an element access.
    /// </summary>
    /// <remarks>
    /// The method uses an explicit stack instead of a recursion, so that it can process a deep expression (see #2083).
    /// </remarks>
    private static bool RequiresParentheses( InterpolationSyntax interpolation )
    {
        var stack = new Stack<SyntaxNode>();
        stack.Push( interpolation );

        while ( stack.Count > 0 )
        {
            var node = stack.Pop();

            switch ( node.Kind() )
            {
                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.StringLiteralExpression:
                    return true;

                case SyntaxKind.InvocationExpression:
                    stack.Push( ((InvocationExpressionSyntax) node).Expression );

                    break;

                case SyntaxKind.ElementAccessExpression:
                    stack.Push( ((ElementAccessExpressionSyntax) node).Expression );

                    break;

                case SyntaxKind.ParenthesizedExpression:
                case SyntaxKind.ParenthesizedPattern:
                case SyntaxKind.TypeOfExpression:
                case SyntaxKind.DefaultExpression:
                    break;

                default:
                    foreach ( var child in node.ChildNodes() )
                    {
                        stack.Push( child );
                    }

                    break;
            }
        }

        return false;
    }

    private sealed class RemoveEndOfLinesRewriter : SafeSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia( SyntaxTrivia trivia )
            => trivia.Kind() switch
            {
                SyntaxKind.EndOfLineTrivia => SyntaxFactory.ElasticSpace,
                _ => trivia
            };
    }
}
