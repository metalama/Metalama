// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes property backing field references ('field' keyword).
/// </summary>
internal sealed class PropertyBackingFieldReferenceSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IPropertySymbol _targetProperty;

    public PropertyBackingFieldReferenceSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IPropertySymbol targetProperty ) : base(
        compilationContext )
    {
        this._rootNode = rootNode;
        this._targetProperty = targetProperty;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var targetName = LinkerRewritingDriver.GetBackingFieldName( this._targetProperty );

        switch ( currentNode.Kind() )
        {
            case SyntaxKind.FieldExpression when currentNode is FieldExpressionSyntax fieldExpression:
                // Replacing the direct invocation.
                return SyntaxFactoryEx.WellKnownIdentifierName(
                    TriviaList( fieldExpression.Token.LeadingTrivia ),
                    targetName,
                    TriviaList( fieldExpression.Token.TrailingTrivia ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}

#endif