// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes event raise references and event field invocations where event broker is targeted.
/// </summary>
internal sealed class EventRaiseBrokerCallSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IntermediateSymbolSemantic<IEventSymbol> _targetSemantic;

    public EventRaiseBrokerCallSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        Invariant.Assert( aspectReference.TargetKind == AspectReferenceTargetKind.EventRaiseAccessor );
        Invariant.Assert( aspectReference.ResolvedSemantic.Symbol.Kind == SymbolKind.Event && aspectReference.ResolvedSemantic.Symbol is IEventSymbol );

        Invariant.Implies(
            aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Base,
            ((IEventSymbol) aspectReference.ResolvedSemantic.Symbol).IsEventField() == true );

        this._rootNode = aspectReference.RootNode;
        this._targetSemantic = aspectReference.ResolvedSemantic.ToTyped<IEventSymbol>();
    }

    public EventRaiseBrokerCallSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IntermediateSymbolSemantic targetSemantic ) : base(
        compilationContext )
    {
        Invariant.Assert( targetSemantic.Symbol.Kind == SymbolKind.Event && targetSemantic.Symbol is IEventSymbol );
        Invariant.Implies( targetSemantic.Kind == IntermediateSymbolSemanticKind.Base, ((IEventSymbol) targetSemantic.Symbol).IsEventField() == true );

        this._rootNode = rootNode;
        this._targetSemantic = targetSemantic.ToTyped<IEventSymbol>();
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var currentEventBrokerInfo = substitutionContext.RewritingDriver.AnalysisRegistry.GetVisibleEventBrokerForSemantic( this._targetSemantic )
            .AssertNotNull();

        var leadingTrivia = currentNode.GetLeadingTrivia();

        ExpressionSyntax eventBrokerExpression =
            this._targetSemantic.Symbol.IsStatic
                ? SyntaxFactoryEx.SafeIdentifierName( currentEventBrokerInfo.EventBrokerFieldName )
                    .WithOptionalLeadingTrivia( leadingTrivia, substitutionContext.SyntaxGenerationContext )
                : MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        SyntaxFactoryEx.SafeIdentifierName( currentEventBrokerInfo.EventBrokerFieldName ) )
                    .WithOptionalLeadingTrivia( leadingTrivia, substitutionContext.SyntaxGenerationContext );

        switch ( currentNode.Kind() )
        {
            case SyntaxKind.InvocationExpression when currentNode is InvocationExpressionSyntax
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
                            ExpressionBody: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax }
                        }
                    },
                    ..
                ] arguments,
                ArgumentList.CloseParenToken.TrailingTrivia: var trailingTrivia
            }:
                var invokeArguments = EventRaiseArgumentsHelper.ExtractInvokeArguments( arguments );

                return
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            eventBrokerExpression,
                            SyntaxFactoryEx.SafeIdentifierName( nameof(EventBroker<,,>.Invoke) ) ),
                        ArgumentList(
                            Token( SyntaxKind.OpenParenToken ),
                            SingletonSeparatedList( Argument( TupleExpression( invokeArguments ) ) ),
                            Token( SyntaxKind.CloseParenToken )
                                .WithOptionalTrailingTrivia( trailingTrivia, substitutionContext.SyntaxGenerationContext.Options ) ) );

            case SyntaxKind.InvocationExpression when currentNode is InvocationExpressionSyntax
            {
                Expression: { },
                ArgumentList.Arguments: var arguments,
                ArgumentList.CloseParenToken.TrailingTrivia: var trailingTrivia
            }:
                return
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            eventBrokerExpression,
                            SyntaxFactoryEx.SafeIdentifierName( nameof(EventBroker<,,>.Invoke) ) ),
                        ArgumentList(
                            Token( SyntaxKind.OpenParenToken ),
                            SingletonSeparatedList( Argument( TupleExpression( SeparatedList( arguments ) ) ) ),
                            Token( SyntaxKind.CloseParenToken )
                                .WithOptionalTrailingTrivia( trailingTrivia, substitutionContext.SyntaxGenerationContext.Options ) ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}