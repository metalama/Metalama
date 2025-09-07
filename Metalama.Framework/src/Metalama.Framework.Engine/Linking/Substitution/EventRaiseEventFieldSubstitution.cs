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
/// Substitutes event raise references and event field invocations where event field's backing field is targeted.
/// </summary>
internal sealed class EventRaiseEventFieldSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IEventSymbol _targetEvent;

    public EventRaiseEventFieldSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IEventSymbol targetEvent ) : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._targetEvent = targetEvent;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        switch ( currentNode )
        {
            case IdentifierNameSyntax identifierName:
                // Replacing the direct invocation.
                return IdentifierName( Identifier( TriviaList( identifierName.Identifier.LeadingTrivia ), this._targetEvent.Name, TriviaList( identifierName.Identifier.TrailingTrivia ) ) );

            case MemberAccessExpressionSyntax { Expression: { }, Name: IdentifierNameSyntax identifierName } simpleMemberAccess:
                // Replacing the this expression invocation.
                return
                    simpleMemberAccess.WithName(
                        IdentifierName(
                            Identifier(
                                TriviaList( identifierName.Identifier.LeadingTrivia ),
                                this._targetEvent.Name,
                                TriviaList( identifierName.Identifier.TrailingTrivia ) ) ) );

            case InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax 
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.HelperTypeName, Identifier.LeadingTrivia: var leadingTrivia },
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
                ] arguments,
                ArgumentList.CloseParenToken.TrailingTrivia: var trailingTrivia
            }:
                // Replacing the linker expression.
                var backingFieldAccess = 
                    eventMemberAccess.WithName( IdentifierName( this._targetEvent.Name ) )
                    .WithRequiredLeadingTrivia( leadingTrivia );

                var invokeArguments = EventRaiseArgumentsHelper.ExtractInvokeArguments( arguments );

                // backingField?.Invoke()
                return ConditionalAccessExpression(
                    backingFieldAccess,
                    InvocationExpression(
                        MemberBindingExpression( IdentifierName( "Invoke" ) ),
                        ArgumentList(
                            Token( SyntaxKind.OpenParenToken ),
                            invokeArguments,
                            Token( TriviaList(), SyntaxKind.CloseParenToken, trailingTrivia ) ) ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}