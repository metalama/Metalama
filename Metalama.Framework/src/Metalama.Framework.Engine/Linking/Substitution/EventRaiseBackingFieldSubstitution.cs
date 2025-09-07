// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes event raise references and event field invocations where event field's backing field is targeted.
/// </summary>
internal sealed class EventRaiseBackingFieldSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IEventSymbol _targetEvent;

    public EventRaiseBackingFieldSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IEventSymbol targetEvent ) : base( compilationContext )
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
                // Replacing the direct invocation.
                return IdentifierName( targetName );

            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax } simpleMemberAccess:
                // Replacing the this expression invocation.
                return
                    simpleMemberAccess.WithName( IdentifierName( targetName ) );

            case InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax 
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.HelperTypeName },
                    Name: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.EventRaiseMemberName }
                },
                ArgumentList.Arguments: 
                [
                    {                        
                        Expression: ParenthesizedLambdaExpressionSyntax 
                        {
                            ExpressionBody: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax eventMemberAccess } 
                        } 
                    },
                    ..
                ] arguments
            }:
                // Replacing the linker expression.
                var backingFieldAccess = eventMemberAccess.WithName( IdentifierName( targetName ) );

                // backingField?.Invoke()
                return ConditionalAccessExpression(
                    backingFieldAccess,
                    InvocationExpression(
                        MemberBindingExpression( IdentifierName( "Invoke" ) ),
                        ArgumentList(
                            SeparatedList(
                                arguments.Skip( 1 ).Select(
                                    a => Argument( a.Expression ) ) ) ) ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}