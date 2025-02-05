// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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