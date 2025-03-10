// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes accesses to event field delegate, i.e. the backing field.
/// </summary>
internal sealed class EventFieldRaiseSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IEventSymbol _targetEvent;

    public EventFieldRaiseSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IEventSymbol targetEvent ) : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._targetEvent = targetEvent;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var targetName =
            this._targetEvent.IsEventFieldIntroduction()
                ? LinkerRewritingDriver.GetBackingFieldName( this._targetEvent )
                : LinkerRewritingDriver.GetOriginalImplMemberName( this._targetEvent );

        switch ( currentNode )
        {
            case IdentifierNameSyntax:
                return IdentifierName( targetName );

            case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } memberAccess:
                return
                    memberAccess.WithName( IdentifierName( targetName ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}