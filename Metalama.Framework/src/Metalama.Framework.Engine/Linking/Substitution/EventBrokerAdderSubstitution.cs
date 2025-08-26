// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes event broker adder body for event accessors.
/// </summary>
internal sealed class EventBrokerAdderSubstitution : SyntaxNodeSubstitution
{
    private readonly ResolvedAspectReference _aspectReference;

    public EventBrokerAdderSubstitution( 
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        this._aspectReference = aspectReference;
    }

    public override SyntaxNode ReplacedNode => this._aspectReference.RootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var context = substitutionContext.SyntaxGenerationContext;
        var eventOverride = this._aspectReference.ResolvedSemantic.Symbol;
        var @event = (IEventSymbol) substitutionContext.RewritingDriver.InjectionRegistry.GetOverrideTarget(eventOverride ).AssertNotNull();
        var eventBrokerTypeInfo = substitutionContext.RewritingDriver.AnalysisRegistry.GetEventBrokerTypeInfo( @event ).AssertNotNull();

        return context.SyntaxGenerator.FormattedBlock(
            IfStatement(
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName( eventBrokerTypeInfo.EventBrokerFieldName ) ),
                    LiteralExpression(
                        SyntaxKind.NullLiteralExpression ) ),
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(
                                Identifier(
                                    TriviaList(),
                                    SyntaxKind.VarKeyword,
                                    "var",
                                    "var",
                                    TriviaList( ElasticSpace ) ) ),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier( "newBroker" ) )
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            Token( TriviaList(), SyntaxKind.NewKeyword, "new", "new", TriviaList( ElasticSpace ) ),
                                            context.SyntaxGenerator.TypeSyntax( eventBrokerTypeInfo.EventBrokerType ),
                                            ArgumentList(
                                                 SeparatedList(
                                                    [
                                                        Argument( ThisExpression() ),
                                                        Argument( IdentifierName( eventBrokerTypeInfo.InvokerDelegate.FieldName ) ),
                                                        Argument( IdentifierName( eventBrokerTypeInfo.CastDelegate.FieldName ) )
                                                    ] ) ),
                                            null ) ) ) ) ) ),
                    WhileStatement(
                        BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            LiteralExpression(
                                SyntaxKind.NullLiteralExpression ),
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName( "System" ),
                                            IdentifierName( "Threading" ) ),
                                        IdentifierName( "Interlocked" ) ),
                                    IdentifierName( "CompareExchange" ) ) )
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList<ArgumentSyntax>(
                                        [
                                            Argument(
                                                null,
                                                Token( TriviaList(), SyntaxKind.RefKeyword, TriviaList( ElasticSpace) ),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName(eventBrokerTypeInfo.EventBrokerFieldName))),
                                            Token(SyntaxKind.CommaToken),
                                            Argument(
                                                IdentifierName("newBroker")),
                                            Token(SyntaxKind.CommaToken),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NullLiteralExpression))
                                        ] ) ) ) ),
                        EmptyStatement() ) ) ),
            IfStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName( eventBrokerTypeInfo.EventBrokerFieldName ) ), 
                        IdentifierName( "AddHandler" ) ),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                IdentifierName( "value" ) ) ) ) ),
                Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(this._aspectReference.ResolvedSemantic.ToTyped<IEventSymbol>().Symbol.Name) ),
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName( eventBrokerTypeInfo.EventBrokerFieldName ) ),
                                IdentifierName( "InvocationDelegate" ) ) ) ) ) ) );
    }
}