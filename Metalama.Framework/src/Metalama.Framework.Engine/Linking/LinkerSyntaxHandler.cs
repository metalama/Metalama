// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            switch ( declaration?.Kind() )
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var methodDecl = (MethodDeclarationSyntax) declaration!;

                        // Partial methods without declared body have the whole declaration as body.
                        return methodDecl.Body ?? (SyntaxNode?) methodDecl.ExpressionBody ?? methodDecl;
                    }

                case SyntaxKind.ConstructorDeclaration:
                    {
                        var constructorDecl = (ConstructorDeclarationSyntax) declaration!;

                        return constructorDecl.Body ?? (SyntaxNode?) constructorDecl.ExpressionBody ?? constructorDecl;
                    }

                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        var otherMethodDecl = (BaseMethodDeclarationSyntax) declaration!;

                        return (SyntaxNode?) otherMethodDecl.Body
                               ?? otherMethodDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );
                    }

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    {
                        var accessorDecl = (AccessorDeclarationSyntax) declaration!;

                        // Accessors with no body are auto-properties or partial properties, in which case we have a substitution for the whole accessor declaration.
                        Invariant.Assert( !symbol.IsAbstract );

                        return accessorDecl.Body ?? (SyntaxNode?) accessorDecl.ExpressionBody ?? accessorDecl;
                    }

                case SyntaxKind.ArrowExpressionClause:
                    {
                        var arrowExpressionClause = (ArrowExpressionClauseSyntax) declaration!;

                        // Expression-bodied property.
                        return arrowExpressionClause;
                    }

                case SyntaxKind.VariableDeclarator:
                    {
                        if ( declaration is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: EventFieldDeclarationSyntax } } variableDecl )
                        {
                            // Event field accessors start replacement as variableDecls.
                            return variableDecl;
                        }

                        goto default;
                    }

                case SyntaxKind.Parameter:
                    {
                        if ( declaration is ParameterSyntax { Parent.Parent: RecordDeclarationSyntax } positionalProperty )
                        {
                            // Record positional property.
                            return positionalProperty;
                        }

                        goto default;
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected override target symbol: '{symbol}'." );
            }
        }

        if ( injectionRegistry.IsOverride( symbol ) )
        {
            switch ( declaration?.Kind() )
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    {
                        var methodDecl = (BaseMethodDeclarationSyntax) declaration!;
                        Invariant.Assert( methodDecl is MethodDeclarationSyntax or ConstructorDeclarationSyntax or DestructorDeclarationSyntax );

                        return (SyntaxNode?) methodDecl.Body
                               ?? methodDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );
                    }

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    {
                        var accessorDecl = (AccessorDeclarationSyntax) declaration!;

                        return (SyntaxNode?) accessorDecl.Body
                               ?? accessorDecl.ExpressionBody ?? throw new AssertionFailedException( $"'{symbol}' has no implementation." );
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected symbol: {symbol}" );
            }
        }

        return null;
    }
}