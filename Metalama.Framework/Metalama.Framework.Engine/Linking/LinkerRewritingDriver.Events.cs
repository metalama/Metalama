// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        private IReadOnlyList<MemberDeclarationSyntax> RewriteEvent( EventDeclarationSyntax eventDeclaration, IEventSymbol symbol )
        {
            var context = this.IntermediateCompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, eventDeclaration );

            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
                var members = new List<MemberDeclarationSyntax>();
                var lastOverride = (IEventSymbol) this.InjectionRegistry.GetLastOverride( symbol );

                if ( eventDeclaration.GetLinkerDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.EventField )
                     && this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    // Backing field for event field.
                    members.Add( this.GetEventBackingField( eventDeclaration, symbol, context ) );
                }

                if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final, true ) );
                }
                else
                {
                    members.Add( this.GetTrampolineForEvent( eventDeclaration, lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ), context ) );
                }

                if ( !eventDeclaration.GetLinkerDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.EventField )
                     && this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && this.ShouldGenerateSourceMember( symbol ) )
                {
                    members.Add( this.GetOriginalImplEvent( eventDeclaration, symbol, context ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add( this.GetEmptyImplEvent( eventDeclaration, symbol, context ) );
                }

                return members;
            }
            else if ( this.InjectionRegistry.IsOverride( symbol ) )
            {
                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default, true )];
                }
                else
                {
                    return Array.Empty<MemberDeclarationSyntax>();
                }
            }
            else if ( eventDeclaration.GetLinkerDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.EventField ) )
            {
                // Event field indicates explicit interface implementation with event field template.

                return
                [
                    this.GetEventBackingField( eventDeclaration, symbol, context ), GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default, true )
                ];
            }
            else if ( this.AnalysisRegistry.HasBaseSemanticReferences( symbol ) )
            {
                Invariant.Assert( symbol is { IsOverride: true, IsSealed: false } or { IsVirtual: true } );

                return
                [
                    this.GetTrampolineForEvent( eventDeclaration, symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ), context ),
                    this.GetOriginalImplEvent( eventDeclaration, symbol, context )
                ];
            }
            else if ( this.AnalysisRegistry.HasAnySubstitutions( symbol ) )
            {
                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default, false )];
            }
            else
            {
                return [eventDeclaration];
            }

            EventDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind, bool isOverrideOrOverrideTarget )
            {
                var addAccessorDeclaration = (AccessorDeclarationSyntax) symbol.AddMethod.AssertNotNull().GetPrimaryDeclarationSyntax().AssertNotNull();
                var removeAccessorDeclaration = (AccessorDeclarationSyntax) symbol.RemoveMethod.AssertNotNull().GetPrimaryDeclarationSyntax().AssertNotNull();

                var transformedAdd = GetLinkedAccessor( semanticKind, addAccessorDeclaration, symbol.AddMethod.AssertNotNull(), isOverrideOrOverrideTarget );

                var transformedRemove = GetLinkedAccessor(
                    semanticKind,
                    removeAccessorDeclaration,
                    symbol.RemoveMethod.AssertNotNull(),
                    isOverrideOrOverrideTarget );

                var (accessorListLeadingTrivia, accessorStartingTrivia, accessorEndingTrivia, accessorListTrailingTrivia) = eventDeclaration switch
                {
                    { AccessorList: not null and var accessorList } => (
                        accessorList.OpenBraceToken.LeadingTrivia,
                        accessorList.OpenBraceToken.TrailingTrivia,
                        accessorList.CloseBraceToken.LeadingTrivia,
                        accessorList.CloseBraceToken.TrailingTrivia),
                    _ => throw new AssertionFailedException( $"Invalid accessor list at '{eventDeclaration}'." )
                };

                return eventDeclaration
                    .WithAccessorList(
                        AccessorList(
                            Token( accessorListLeadingTrivia, SyntaxKind.OpenBraceToken, accessorStartingTrivia ),
                            List( [transformedAdd, transformedRemove] ),
                            Token( accessorEndingTrivia, SyntaxKind.CloseBraceToken, accessorListTrailingTrivia ) ) );
            }

            AccessorDeclarationSyntax GetLinkedAccessor(
                IntermediateSymbolSemanticKind semanticKind,
                AccessorDeclarationSyntax accessorDeclaration,
                IMethodSymbol methodSymbol,
                bool isOverrideOrOverrideTarget )
            {
                if ( !isOverrideOrOverrideTarget && !this.AnalysisRegistry.HasAnySubstitutions( methodSymbol ) )
                {
                    return accessorDeclaration;
                }

                var semantic = methodSymbol.ToSemantic( semanticKind );

                var linkedBody = this.GetSubstitutedBody(
                    semantic,
                    new SubstitutionContext(
                        this,
                        context,
                        new InliningContextIdentifier( semantic ) ) );

                // Trivia processing:
                //   * For block bodies methods, we preserve trivia of the opening/closing brace.
                //   * For expression bodied methods:
                //       int Foo() <trivia_leading_equals_value> => <trivia_trailing_equals_value> <expression> <trivia_leading_semicolon> ; <trivia_trailing_semicolon>
                //       int Foo() <trivia_leading_equals_value> { <trivia_trailing_equals_value> <linked_body> <trivia_leading_semicolon> } <trivia_trailing_semicolon>

                var (openBraceLeadingTrivia, openBraceTrailingTrivia, closeBraceLeadingTrivia, closeBraceTrailingTrivia) =
                    accessorDeclaration switch
                    {
                        { Body: { OpenBraceToken: var openBraceToken, CloseBraceToken: var closeBraceToken } } =>
                            (openBraceToken.LeadingTrivia, openBraceToken.TrailingTrivia, closeBraceToken.LeadingTrivia, closeBraceToken.TrailingTrivia),
                        { ExpressionBody.ArrowToken: var arrowToken, SemicolonToken: var semicolonToken } =>
                            (arrowToken.LeadingTrivia.AddOptionalLineFeed( context ),
                             arrowToken.TrailingTrivia.AddOptionalLineFeed( context ),
                             semicolonToken.LeadingTrivia.AddOptionalLineFeed( context ), semicolonToken.TrailingTrivia),
                        _ => throw new AssertionFailedException( $"Unexpected accessor declaration: {accessorDeclaration}" )
                    };

                return accessorDeclaration.PartialUpdate(
                    expressionBody: null,
                    body: Block(
                            Token( openBraceLeadingTrivia, SyntaxKind.OpenBraceToken, openBraceTrailingTrivia ),
                            SingletonList<StatementSyntax>( linkedBody ),
                            Token( closeBraceLeadingTrivia, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                    semicolonToken: default(SyntaxToken) );
            }
        }

        private static BlockSyntax GetImplicitAdderBody( IMethodSymbol symbol, SyntaxGenerationContext context )
            => context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                symbol.IsStatic
                                    ? context.SyntaxGenerator.TypeSyntax( symbol.ContainingType )
                                    : ThisExpression(),
                                IdentifierName( GetBackingFieldName( (IEventSymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                            .WithSimplifierAnnotationIfNecessary( context ),
                        IdentifierName( "value" ) ),
                    Token( default, SyntaxKind.SemicolonToken, new SyntaxTriviaList( context.ElasticEndOfLineTrivia ) ) ) );

        private static BlockSyntax GetImplicitRemoverBody( IMethodSymbol symbol, SyntaxGenerationContext context )
            => context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SubtractAssignmentExpression,
                        MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                symbol.IsStatic
                                    ? context.SyntaxGenerator.TypeSyntax( symbol.ContainingType )
                                    : ThisExpression(),
                                IdentifierName( GetBackingFieldName( (IEventSymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                            .WithSimplifierAnnotationIfNecessary( context ),
                        IdentifierName( "value" ) ),
                    Token( default, SyntaxKind.SemicolonToken, context.ElasticEndOfLineTriviaList ) ) );

        private EventFieldDeclarationSyntax GetEventBackingField(
            EventDeclarationSyntax eventDeclaration,
            IEventSymbol symbol,
            SyntaxGenerationContext context )
        {
            EqualsValueClauseSyntax? initializerExpression;

            switch ( eventDeclaration.GetLinkerDeclarationFlags() & AspectLinkerDeclarationFlags.HasInitializerExpressionMask )
            {
                case AspectLinkerDeclarationFlags.HasDefaultInitializerExpression:
                    initializerExpression =
                        EqualsValueClause(
                            LiteralExpression(
                                SyntaxKind.DefaultLiteralExpression,
                                Token( SyntaxKind.DefaultKeyword ) ) );

                    break;

                case AspectLinkerDeclarationFlags.HasHiddenInitializerExpression:
                    var firstStatement =
                        eventDeclaration.AccessorList.AssertNotNull()
                            .Accessors.First()
                            .Body.AssertNotNull()
                            .Statements.Single();

                    var expression = ((InvocationExpressionSyntax) ((ExpressionStatementSyntax) firstStatement).Expression).ArgumentList.Arguments[0]
                        .Expression;

                    initializerExpression = EqualsValueClause( expression );

                    break;

                default:
                    initializerExpression = null;

                    break;
            }

            return this.GetEventBackingField( eventDeclaration.Type, initializerExpression, symbol, context );
        }

        // Event backing field is intentionally an event field to handle thread-safety.
        private EventFieldDeclarationSyntax GetEventBackingField(
            TypeSyntax eventType,
            EqualsValueClauseSyntax? initializer,
            IEventSymbol symbol,
            SyntaxGenerationContext context )
        {
            if ( initializer == null && symbol.Type is { IsValueType: false, NullableAnnotation: NullableAnnotation.NotAnnotated } )
            {
                initializer =
                    EqualsValueClause(
                        PostfixUnaryExpression(
                            SyntaxKind.SuppressNullableWarningExpression,
                            LiteralExpression(
                                SyntaxKind.DefaultLiteralExpression,
                                Token( SyntaxKind.DefaultKeyword ) ) ) );
            }

            return
                EventFieldDeclaration(
                        List<AttributeListSyntax>(),
                        symbol.IsStatic
                            ? TokenList(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        VariableDeclaration(
                            eventType.WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier( GetBackingFieldName( symbol ) ),
                                    null,
                                    initializer ) ) ) )
                    .NormalizeWhitespaceIfNecessary( context )
                    .WithOptionalTrivia(
                        context.ElasticEndOfLineTriviaList,
                        context.TwoElasticEndOfLinesTriviaList,
                        this.SyntaxGenerationOptions )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private MemberDeclarationSyntax GetOriginalImplEvent(
            EventDeclarationSyntax @event,
            IEventSymbol symbol,
            SyntaxGenerationContext context )
        {
            var existingAccessorList = @event.AccessorList.AssertNotNull();

            var transformedAccessorList =
                existingAccessorList
                    .WithAccessors(
                        List(
                            existingAccessorList.Accessors.SelectAsArray(
                                a =>
                                    TransformAccessor(
                                        a,
                                        a.Kind() switch
                                        {
                                            SyntaxKind.AddAccessorDeclaration => symbol.AddMethod.AssertNotNull(),
                                            SyntaxKind.RemoveAccessorDeclaration => symbol.RemoveMethod.AssertNotNull(),
                                            _ => throw new AssertionFailedException( $"Unexpected kind: {a}" )
                                        } ) ) ) )
                    .WithSourceCodeAnnotation();

            return this.GetSpecialImplEvent(
                @event.Type,
                transformedAccessorList.WithSourceCodeAnnotation(),
                symbol,
                GetOriginalImplMemberName( symbol ),
                context );

            AccessorDeclarationSyntax TransformAccessor( AccessorDeclarationSyntax accessorDeclaration, IMethodSymbol accessorSymbol )
            {
                var semantic = accessorSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var inliningContext = new InliningContextIdentifier( semantic );

                var substitutedBody =
                    accessorDeclaration.Body != null
                        ? (BlockSyntax) RewriteBody( accessorDeclaration.Body, accessorSymbol, new SubstitutionContext( this, context, inliningContext ) )
                        : null;

                var substitutedExpressionBody =
                    accessorDeclaration.ExpressionBody != null
                        ? (ArrowExpressionClauseSyntax) RewriteBody(
                            accessorDeclaration.ExpressionBody,
                            accessorSymbol,
                            new SubstitutionContext( this, context, inliningContext ) )
                        : null;

                return accessorDeclaration.PartialUpdate( body: substitutedBody, expressionBody: substitutedExpressionBody );
            }
        }

        private MemberDeclarationSyntax GetEmptyImplEvent( EventDeclarationSyntax @event, IEventSymbol symbol, SyntaxGenerationContext context )
        {
            return this.GetSpecialImplEvent( @event.Type, @event.AccessorList.AssertNotNull(), symbol, GetEmptyImplMemberName( symbol ), context );
        }

        private MemberDeclarationSyntax GetSpecialImplEvent(
            TypeSyntax eventType,
            AccessorListSyntax accessorList,
            IEventSymbol symbol,
            string name,
            SyntaxGenerationContext context )
        {
            var cleanAccessorList =
                accessorList.WithAccessors(
                    List(
                        accessorList.Accessors.SelectAsReadOnlyList(
                            a =>
                                a.Kind() switch
                                {
                                    SyntaxKind.AddAccessorDeclaration => this.FilterAttributesOnSpecialImpl( symbol.AddMethod.AssertNotNull(), a ),
                                    SyntaxKind.RemoveAccessorDeclaration => this.FilterAttributesOnSpecialImpl( symbol.RemoveMethod.AssertNotNull(), a ),
                                    _ => throw new AssertionFailedException( $"Unexpected kind: {a}" )
                                } ) ) );

            return
                EventDeclaration(
                        this.FilterAttributesOnSpecialImpl( symbol ),
                        symbol.IsStatic
                            ? TokenList(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        eventType,
                        null,
                        Identifier( name ),
                        cleanAccessorList )
                    .NormalizeWhitespaceIfNecessary( context )
                    .WithOptionalLeadingAndTrailingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private EventDeclarationSyntax GetTrampolineForEvent(
            EventDeclarationSyntax @event,
            IntermediateSymbolSemantic<IEventSymbol> targetSemantic,
            SyntaxGenerationContext context )
        {
            Invariant.Assert( targetSemantic.Kind is IntermediateSymbolSemanticKind.Base or IntermediateSymbolSemanticKind.Default );

            Invariant.Implies(
                targetSemantic.Kind is IntermediateSymbolSemanticKind.Base,
                targetSemantic.Symbol is { IsOverride: true } or { IsVirtual: true } );

            var addAccessor = @event.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.AddAccessorDeclaration );
            var removeAccessor = @event.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.RemoveAccessorDeclaration );

            return @event
                .WithAccessorList(
                    AccessorList(
                        List(
                            new[]
                                {
                                    addAccessor != null
                                        ? AccessorDeclaration(
                                            SyntaxKind.AddAccessorDeclaration,
                                            context.SyntaxGenerator.FormattedBlock(
                                                ExpressionStatement(
                                                    AssignmentExpression(
                                                        SyntaxKind.AddAssignmentExpression,
                                                        GetInvocationTarget(),
                                                        IdentifierName( "value" ) ) ) ) )
                                        : null,
                                    removeAccessor != null
                                        ? AccessorDeclaration(
                                            SyntaxKind.RemoveAccessorDeclaration,
                                            context.SyntaxGenerator.FormattedBlock(
                                                ExpressionStatement(
                                                    AssignmentExpression(
                                                        SyntaxKind.SubtractAssignmentExpression,
                                                        GetInvocationTarget(),
                                                        IdentifierName( "value" ) ) ) ) )
                                        : null
                                }.Where( a => a != null )
                                .AssertNoneNull() ) ) )
                .WithTriviaFromIfNecessary( @event, this.SyntaxGenerationOptions );

            ExpressionSyntax GetInvocationTarget()
            {
                if ( targetSemantic.Symbol.IsStatic )
                {
                    return GetTargetName();
                }
                else
                {
                    return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), GetTargetName() )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }

            IdentifierNameSyntax GetTargetName()
            {
                if ( targetSemantic.Kind is IntermediateSymbolSemanticKind.Base )
                {
                    return IdentifierName( GetOriginalImplMemberName( targetSemantic.Symbol ) );
                }
                else
                {
                    return IdentifierName( targetSemantic.Symbol.Name );
                }
            }
        }
    }
}