// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        private IReadOnlyList<MemberDeclarationSyntax> RewriteEventField( EventFieldDeclarationSyntax eventFieldDeclaration, IEventSymbol symbol )
        {
            var context = this.IntermediateCompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, eventFieldDeclaration );

            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
                var members = new List<MemberDeclarationSyntax>();
                var lastOverride = (IEventSymbol) this.InjectionRegistry.GetLastOverride( symbol );

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( this.GetEventBackingField( eventFieldDeclaration, symbol, context ) );
                }

                if ( this.InjectionRegistry.HasEventRaiseOverride( symbol ) )
                {
                    // If there is an event raise override, we will generate the event broker field.
                    members.Add( this.GetEventBrokerField( symbol, context ) );

                    // And link the final declaration, which will use broker substitution.
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
                }
                else
                {
                    if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                    {
                        members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
                    }
                    else
                    {
                        members.Add( this.GetTrampolineForEventField( eventFieldDeclaration, lastOverride, context ) );
                    }
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add( this.GetEmptyImplEventField( eventFieldDeclaration.Declaration.Type, symbol, context ) );
                }

                return members;
            }
            else if ( this.InjectionRegistry.IsOverride( symbol ) )
            {
                throw new AssertionFailedException( $"Event field cannot be an override: {symbol}" );
            }
            else
            {
                var members = new List<MemberDeclarationSyntax>();

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add(
                        this.GetEmptyImplEventField(
                            eventFieldDeclaration.Declaration.Type,
                            symbol,
                            context ) );
                }

                if ( this.LateTransformationRegistry.IsPrimaryConstructorInitializedMember( symbol ) )
                {
                    eventFieldDeclaration =
                        eventFieldDeclaration.WithDeclaration(
                            eventFieldDeclaration.Declaration.WithVariables(
                                SeparatedList( eventFieldDeclaration.Declaration.Variables.SelectAsArray( v => v.WithInitializer( default ) ) ) ) );
                }

                members.Add( eventFieldDeclaration );

                return members;
            }

            MemberDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
            {
                var transformedAdd = GetLinkedAccessor(
                    semanticKind,
                    SyntaxKind.AddAccessorDeclaration,
                    SyntaxKind.AddKeyword,
                    symbol.AddMethod.AssertNotNull() );

                var transformedRemove = GetLinkedAccessor(
                    semanticKind,
                    SyntaxKind.RemoveAccessorDeclaration,
                    SyntaxKind.RemoveKeyword,
                    symbol.RemoveMethod.AssertNotNull() );

                return
                    EventDeclaration(
                        FilterAttributeListsForTarget( eventFieldDeclaration.AttributeLists, SyntaxKind.EventKeyword, true, true ),
                        eventFieldDeclaration.Modifiers,
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.EventKeyword ),
                        eventFieldDeclaration.Declaration.Type.WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                        null,
                        Identifier( symbol.Name ),
                        AccessorList(
                            Token( context.ElasticEndOfLineTriviaList, SyntaxKind.OpenBraceToken, context.ElasticEndOfLineTriviaList ),
                            List( [transformedAdd, transformedRemove] ),
                            Token( context.ElasticEndOfLineTriviaList, SyntaxKind.CloseBraceToken, context.ElasticEndOfLineTriviaList ) ),
                        default );
            }

            AccessorDeclarationSyntax GetLinkedAccessor(
                IntermediateSymbolSemanticKind semanticKind,
                SyntaxKind accessorKind,
                SyntaxKind accessorKeyword,
                IMethodSymbol methodSymbol )
            {
                var linkedBody = this.GetSubstitutedBody(
                    methodSymbol.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this,
                        context,
                        new InliningContextIdentifier( methodSymbol.ToSemantic( semanticKind ) ) ) );

                var (openBraceLeadingTrivia, openBraceTrailingTrivia, closeBraceLeadingTrivia, closeBraceTrailingTrivia) =
                    (TriviaList(), context.ElasticEndOfLineTriviaList, context.ElasticEndOfLineTriviaList,
                     context.ElasticEndOfLineTriviaList);

                return
                    AccessorDeclaration(
                        accessorKind,
                        FilterAttributeListsForTarget( eventFieldDeclaration.AttributeLists, SyntaxKind.MethodKeyword, false, false )
                            .AddRange( FilterAttributeListsForTarget( eventFieldDeclaration.AttributeLists, SyntaxKind.Parameter, false, true ) ),
                        TokenList(),
                        Token( TriviaList(), accessorKeyword, context.ElasticEndOfLineTriviaList ),
                        Block(
                                Token( openBraceLeadingTrivia, SyntaxKind.OpenBraceToken, openBraceTrailingTrivia ),
                                SingletonList<StatementSyntax>( linkedBody ),
                                Token( closeBraceLeadingTrivia, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                        null,
                        default );
            }
        }

        private EventFieldDeclarationSyntax GetEventBackingField(
            EventFieldDeclarationSyntax eventFieldDeclaration,
            IEventSymbol symbol,
            SyntaxGenerationContext context )
        {
            var declarator = (VariableDeclaratorSyntax) symbol.GetPrimaryDeclarationSyntax().AssertNotNull();

            return
                this.GetEventBackingField(
                    eventFieldDeclaration.Declaration.Type,
                    declarator.Initializer,
                    symbol,
                    context );
        }

        private MemberDeclarationSyntax GetEmptyImplEventField( TypeSyntax eventType, IEventSymbol symbol, SyntaxGenerationContext context )
        {
            var accessorList =
                AccessorList(
                    List(
                    [
                        AccessorDeclaration( SyntaxKind.AddAccessorDeclaration, context.SyntaxGenerator.FormattedBlock() ),
                        AccessorDeclaration( SyntaxKind.RemoveAccessorDeclaration, context.SyntaxGenerator.FormattedBlock() )
                    ] ) );

            return this.GetSpecialImplEvent( eventType, accessorList, symbol, GetEmptyImplMemberName( symbol ), context );
        }

        private EventDeclarationSyntax GetTrampolineForEventField(
            EventFieldDeclarationSyntax eventField,
            IEventSymbol targetSymbol,
            SyntaxGenerationContext context )
        {
            // TODO: Do not copy leading/trailing trivia to all declarations.

            return
                EventDeclaration(
                        List<AttributeListSyntax>(),
                        eventField.Modifiers,
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.EventKeyword ),
                        eventField.Declaration.Type.WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                        null,
                        eventField.Declaration.Variables.Single().Identifier,
                        AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(
                                        SyntaxKind.AddAccessorDeclaration,
                                        context.SyntaxGenerator.FormattedBlock(
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.AddAssignmentExpression,
                                                    GetInvocationTarget(),
                                                    IdentifierName( "value" ) ) ) ) ),
                                    AccessorDeclaration(
                                        SyntaxKind.RemoveAccessorDeclaration,
                                        context.SyntaxGenerator.FormattedBlock(
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SubtractAssignmentExpression,
                                                    GetInvocationTarget(),
                                                    IdentifierName( "value" ) ) ) ) )
                                }.WhereNotNull() ) ),
                        default )
                    .WithTriviaFromIfNecessary( eventField, this.SyntaxGenerationOptions );

            ExpressionSyntax GetInvocationTarget()
            {
                if ( targetSymbol.IsStatic )
                {
                    return IdentifierName( targetSymbol.Name );
                }
                else
                {
                    return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( targetSymbol.Name ) )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }
        }
    }
}