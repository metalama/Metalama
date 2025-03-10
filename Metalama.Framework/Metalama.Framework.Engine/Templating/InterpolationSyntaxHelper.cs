// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

internal sealed class InterpolationSyntaxHelper : SafeSyntaxVisitor<bool>
{
    private static readonly InterpolationSyntaxHelper _instance = new();

    private InterpolationSyntaxHelper() { }

    public override bool DefaultVisit( SyntaxNode node ) => node.ChildNodes().Any( this.Visit );

    public override bool VisitAliasQualifiedName( AliasQualifiedNameSyntax node ) => true;

    public override bool VisitInvocationExpression( InvocationExpressionSyntax node ) => this.Visit( node.Expression );

    public override bool VisitElementAccessExpression( ElementAccessExpressionSyntax node ) => this.Visit( node.Expression );

    public override bool VisitParenthesizedExpression( ParenthesizedExpressionSyntax node ) => false;

    public override bool VisitParenthesizedPattern( ParenthesizedPatternSyntax node ) => false;

    public override bool VisitTypeOfExpression( TypeOfExpressionSyntax node ) => false;

    public override bool VisitDefaultExpression( DefaultExpressionSyntax node ) => false;

    public override bool VisitLiteralExpression( LiteralExpressionSyntax node ) => node.IsKind( SyntaxKind.StringLiteralExpression );

    public static InterpolationSyntax Fix( InterpolationSyntax interpolation )
    {
        // Interpolations cannot contain EOL, so we need to remove them.
        var fixedInterpolation = (InterpolationSyntax) new RemoveEndOfLinesRewriter().Visit( interpolation.NormalizeWhitespace() )!;

        // If the interpolation expression contains an alias-prefixed identifier (for instance global::System) that is not
        // in a parenthesis or a square bracket, we need to parenthesize the expression.
        if ( _instance.Visit( fixedInterpolation ) )
        {
            return fixedInterpolation.WithExpression( SyntaxFactory.ParenthesizedExpression( fixedInterpolation.Expression ) );
        }
        else
        {
            return fixedInterpolation;
        }
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