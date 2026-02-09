// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes property backing field references ('field' keyword).
/// </summary>
internal sealed class PropertyImplicitAccessorSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IPropertySymbol _targetProperty;

    public PropertyImplicitAccessorSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IPropertySymbol targetProperty ) : base(
        compilationContext )
    {
        this._rootNode = rootNode;
        this._targetProperty = targetProperty;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var targetName = substitutionContext.RewritingDriver.GetBackingFieldName( this._targetProperty );

        switch ( currentNode.Kind() )
        {
            case SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                when currentNode is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } accessorDeclaration:
                // Replacing a body-less set/init accessor (auto property).
                return
                    Block(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactoryEx.WellKnownIdentifierName( targetName ),
                                    SyntaxFactoryEx.WellKnownIdentifierName( "value" ) ) ) )
                        .WithTriviaFromIfNecessary( accessorDeclaration, substitutionContext.SyntaxGenerationContext.Options );

            case SyntaxKind.GetAccessorDeclaration
                when currentNode is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } accessorDeclaration:
                // Replacing a body-less get accessor (auto property).
                return
                    Block(
                            ReturnStatement(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                SyntaxFactoryEx.WellKnownIdentifierName( targetName ),
                                Token( SyntaxKind.SemicolonToken ) ) )
                        .WithTriviaFromIfNecessary( accessorDeclaration, substitutionContext.SyntaxGenerationContext.Options );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}