// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class RedirectionSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _referencingNode;
    private readonly IntermediateSymbolSemantic _targetSemantic;

    public RedirectionSubstitution( CompilationContext compilationContext, SyntaxNode referencingNode, IntermediateSymbolSemantic targetSemantic )
        : base( compilationContext )
    {
        this._referencingNode = referencingNode;
        this._targetSemantic = targetSemantic;
    }

    public override SyntaxNode ReplacedNode => this._referencingNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        // We currently need to support all name syntaxes that may reference a property of the current object.

        switch ( currentNode )
        {
            case SimpleNameSyntax name:
                return name.WithIdentifier( Identifier( this._targetSemantic.Symbol.Name ) );

            case MemberAccessExpressionSyntax { RawKind: (int) SyntaxKind.SimpleMemberAccessExpression }:
                return currentNode;

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}