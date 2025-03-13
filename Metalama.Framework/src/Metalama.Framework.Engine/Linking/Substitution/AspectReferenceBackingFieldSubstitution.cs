// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference that points to the source semantic of auto property for an access to the generated backing field.
/// </summary>
internal sealed class AspectReferenceBackingFieldSubstitution : SyntaxNodeSubstitution
{
    private readonly ResolvedAspectReference _aspectReference;

    public override SyntaxNode ReplacedNode => this._aspectReference.RootNode;

    public AspectReferenceBackingFieldSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base(
        compilationContext )
    {
        this._aspectReference = aspectReference;
    }

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        if ( this._aspectReference.RootNode != this._aspectReference.SymbolSourceNode )
        {
            // Root node is different that symbol source node - this is introduction in form:
            // <helper_type>.<helper_member>(<symbol_source_node>);
            // We need to get to symbol source node.

            currentNode = this._aspectReference.RootNode switch
            {
                InvocationExpressionSyntax { ArgumentList: { Arguments.Count: 1 } argumentList } =>
                    argumentList.Arguments[0].Expression,
                _ => throw new AssertionFailedException( $"Unsupported form: {this._aspectReference.RootNode}" )
            };
        }

        switch ( currentNode )
        {
            case MemberAccessExpressionSyntax { Name: not null } memberAccessExpression:
                var backingFieldName = LinkerRewritingDriver.GetBackingFieldName( this._aspectReference.ResolvedSemantic.Symbol );

                if ( this._aspectReference.OriginalSymbol.IsExplicitInterfaceMemberImplementation() )
                {
                    return memberAccessExpression.PartialUpdate( ThisExpression(), name: IdentifierName( backingFieldName ) );
                }
                else
                {
                    return memberAccessExpression.WithName( IdentifierName( backingFieldName ) );
                }

            default:
                throw new AssertionFailedException( $"Unexpected syntax: {currentNode}" );
        }
    }
}