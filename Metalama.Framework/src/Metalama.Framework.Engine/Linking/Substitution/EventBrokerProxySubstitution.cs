// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes access from event raise override to the override's helper declaration.
/// </summary>
internal sealed class EventBrokerProxySubstitution : SyntaxNodeSubstitution
{
    private readonly ResolvedAspectReference _aspectReference;
    private readonly string _brokerProxyName;

    public EventBrokerProxySubstitution(
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference,
        string brokerProxyName ) : base( compilationContext )
    {
        this._aspectReference = aspectReference;
        this._brokerProxyName = brokerProxyName;
    }

    public override SyntaxNode ReplacedNode => this._aspectReference.RootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        // Transform calls to the helper declaration instead of the actual broker call
        switch ( currentNode )
        {
            case MemberAccessExpressionSyntax memberAccess:
                // Replace member access with access to the broker proxy
                var newMemberAccess = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName( this._brokerProxyName ) );

                // Preserve trivia from the original member access
                return newMemberAccess.WithTriviaFromIfNecessary( memberAccess, substitutionContext.SyntaxGenerationContext.Options );

            case IdentifierNameSyntax identifierName:
                // Replace identifier with access to the broker proxy
                var memberAccessExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName( this._brokerProxyName ) );

                // Preserve trivia from the original identifier
                return memberAccessExpression.WithTriviaFromIfNecessary( identifierName, substitutionContext.SyntaxGenerationContext.Options );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}