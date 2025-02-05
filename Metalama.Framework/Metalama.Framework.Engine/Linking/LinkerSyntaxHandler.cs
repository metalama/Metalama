// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerSyntaxHandler
{
    public static SyntaxNode GetCanonicalRootNode( IMethodSymbol symbol, LinkerInjectionRegistry injectionRegistry )
        => GetCanonicalRootNodeOrNull( symbol, injectionRegistry ) ?? throw new AssertionFailedException( $"'{symbol}' is not an override target." );

    public static SyntaxNode? GetCanonicalRootNodeOrNull( IMethodSymbol symbol, LinkerInjectionRegistry injectionRegistry )
    {
        var declaration = symbol.GetPrimaryDeclarationSyntax();

        if ( injectionRegistry.IsOverrideTarget( symbol ) )
        {
            switch ( declaration )
            {
                case MethodDeclarationSyntax methodDecl:
                    // Partial methods without declared body have the whole declaration as body.
                    return methodDecl.Body ?? (SyntaxNode?) methodDecl.ExpressionBody ?? methodDecl;

                case BaseMethodDeclarationSyntax otherMethodDecl:
                    return (SyntaxNode?) otherMethodDecl.Body
                           ?? otherMethodDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );

                case AccessorDeclarationSyntax accessorDecl:
                    // Accessors with no body are auto-properties or partial properties, in which case we have a substitution for the whole accessor declaration.
                    Invariant.Assert( !symbol.IsAbstract );

                    return accessorDecl.Body ?? (SyntaxNode?) accessorDecl.ExpressionBody ?? accessorDecl;

                case ArrowExpressionClauseSyntax arrowExpressionClause:
                    // Expression-bodied property.
                    return arrowExpressionClause;

                case VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: EventFieldDeclarationSyntax } } variableDecl:
                    // Event field accessors start replacement as variableDecls.
                    return variableDecl;

                case ParameterSyntax { Parent.Parent: RecordDeclarationSyntax } positionalProperty:
                    // Record positional property.
                    return positionalProperty;

                default:
                    throw new AssertionFailedException( $"Unexpected override target symbol: '{symbol}'." );
            }
        }

        if ( injectionRegistry.IsOverride( symbol ) )
        {
            switch ( declaration )
            {
                case BaseMethodDeclarationSyntax methodDecl:
                    Invariant.Assert( methodDecl is MethodDeclarationSyntax or ConstructorDeclarationSyntax or DestructorDeclarationSyntax );

                    return (SyntaxNode?) methodDecl.Body
                           ?? methodDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );

                case AccessorDeclarationSyntax accessorDecl:
                    return (SyntaxNode?) accessorDecl.Body
                           ?? accessorDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );

                default:
                    throw new AssertionFailedException( $"Unexpected symbol: {symbol}" );
            }
        }

        return null;
    }
}