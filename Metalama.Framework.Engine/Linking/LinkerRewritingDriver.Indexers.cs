// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// Properties with overrides have the following structure:
//  * Final semantic. 
//  * Override n
//  * ...
//  * Override 1
//  * Default semantic.
//  * Base semantic (if the property was introduced).

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        private IReadOnlyList<MemberDeclarationSyntax> RewriteIndexer(
            IndexerDeclarationSyntax indexerDeclaration,
            IPropertySymbol symbol,
            SyntaxGenerationContext context )
        {
            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
#if ROSLYN_4_12_0_OR_GREATER
                if ( symbol is { IsPartialDefinition: true, PartialImplementationPart: { } } )
                {
                    // This is a partial indexer declaration that is not to be transformed.
                    return [indexerDeclaration];
                }
#endif

                var members = new List<MemberDeclarationSyntax>();

#if ROSLYN_4_12_0_OR_GREATER
                if ( symbol is { IsPartialDefinition: true, PartialImplementationPart: null } )
                {
                    // This is a partial indexer declaration that did not have any body.
                    // Keep it as is and add a new declaration that will contain the override.
                    members.Add( indexerDeclaration );
                }
#endif

                var lastOverride = (IPropertySymbol) this.InjectionRegistry.GetLastOverride( symbol );

                if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
                }
                else
                {
                    members.Add( this.GetTrampolineForIndexer( indexerDeclaration, lastOverride, context ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && this.ShouldGenerateSourceMember( symbol ) )
                {
                    members.Add(
                        this.GetOriginalImplIndexer(
                            indexerDeclaration,
                            symbol,
                            indexerDeclaration.Type,
                            indexerDeclaration.ParameterList,
                            indexerDeclaration.ExpressionBody,
                            context ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add(
                        this.GetEmptyImplIndexer(
                            symbol,
                            indexerDeclaration.Type,
                            indexerDeclaration.ParameterList,
                            indexerDeclaration.AccessorList.AssertNotNull(),
                            context ) );
                }

                return members;
            }
            else if ( this.InjectionRegistry.IsOverride( symbol ) )
            {
                if ( !this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     || this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    return Array.Empty<MemberDeclarationSyntax>();
                }

                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default )];
            }
            else
            {
                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default )];
            }

            MemberDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
            {
                var transformedAccessors = new List<AccessorDeclarationSyntax>();

                if ( symbol.GetMethod != null )
                {
                    if ( indexerDeclaration.AccessorList?.Accessors.SingleOrDefault( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) ) is
                        { } getAccessorDeclaration )
                    {
                        transformedAccessors.Add( GetLinkedAccessor( semanticKind, getAccessorDeclaration, symbol.GetMethod ) );
                    }
                    else if ( indexerDeclaration.ExpressionBody != null )
                    {
                        var linkedBody = this.GetSubstitutedBody(
                            symbol.GetMethod.ToSemantic( semanticKind ),
                            new SubstitutionContext(
                                this,
                                context,
                                new InliningContextIdentifier( symbol.GetMethod.ToSemantic( semanticKind ) ) ) );

                        transformedAccessors.Add(
                            AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                List<AttributeListSyntax>(),
                                TokenList(),
                                Token( TriviaList( ElasticMarker ), SyntaxKind.GetKeyword, TriviaList( ElasticMarker ) ),
                                Block(
                                    Token(
                                        context.ElasticEndOfLineTriviaList,
                                        SyntaxKind.OpenBraceToken,
                                        context.ElasticEndOfLineTriviaList ),
                                    SingletonList<StatementSyntax>( linkedBody ),
                                    Token( context.ElasticEndOfLineTriviaList, SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) ),
                                null,
                                default ) );
                    }
                }

                if ( symbol.SetMethod != null )
                {
                    var setAccessorDeclaration = indexerDeclaration.AccessorList!.Accessors.Single(
                        a => a.IsKind( SyntaxKind.SetAccessorDeclaration ) || a.IsKind( SyntaxKind.InitAccessorDeclaration ) );

                    transformedAccessors.Add( GetLinkedAccessor( semanticKind, setAccessorDeclaration, symbol.SetMethod ) );
                }

                // Trivia processing for the property declaration:
                //   * Properties with accessor lists are not changed (accessors are expected to be correct, accessor lists inherit the same trivia).
                //   * Properties with expression body are transformed in the following way (trivia of the <expr> are already processed):
                //      int Property <trivia_leading_equals_value> => <trivia_trailing_equals_value> <expr> <trivia_leading_semicolon>; <trivia_trailing_semicolon>
                //      int Property <trivia_leading_equals_value> { <trivia_after_equals_value> get { ... } <trivia_before_semicolon> } <trivia_trailing_semicolon>

                var (accessorListLeadingTrivia, accessorStartingTrivia, accessorEndingTrivia, accessorListTrailingTrivia) = indexerDeclaration switch
                {
                    { AccessorList: not null and var accessorList } => (
                        accessorList.OpenBraceToken.LeadingTrivia,
                        accessorList.OpenBraceToken.TrailingTrivia,
                        accessorList.CloseBraceToken.LeadingTrivia,
                        accessorList.CloseBraceToken.TrailingTrivia),
                    { ExpressionBody: not null and var expressionBody } => (
                        expressionBody.ArrowToken.LeadingTrivia,
                        expressionBody.ArrowToken.TrailingTrivia,
                        indexerDeclaration.SemicolonToken.LeadingTrivia,
                        indexerDeclaration.SemicolonToken.TrailingTrivia),
                    _ => throw new AssertionFailedException( $"Unexpected property declaration: {indexerDeclaration}" )
                };

                accessorListLeadingTrivia =
                    indexerDeclaration.ThisKeyword switch
                    {
                        var thisKeyword when thisKeyword.TrailingTrivia.HasAnyNewLine() => accessorListLeadingTrivia,
                        _ => context.ElasticEndOfLineTriviaList.AddRange( accessorListLeadingTrivia )
                    };

                var result = indexerDeclaration.PartialUpdate(
                    accessorList: AccessorList(
                            Token( accessorListLeadingTrivia, SyntaxKind.OpenBraceToken, accessorStartingTrivia ),
                            List( transformedAccessors ),
                            Token( accessorEndingTrivia, SyntaxKind.CloseBraceToken, accessorListTrailingTrivia ) )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                    expressionBody: null,
                    semicolonToken: default(SyntaxToken) );

#if ROSLYN_4_12_0_OR_GREATER
                if ( symbol is { IsPartialDefinition: true, PartialImplementationPart: null } )
                {
                    result = RemoveAttributesForPartialImplementation( result );
                }
#endif

                return result;
            }

            AccessorDeclarationSyntax GetLinkedAccessor(
                IntermediateSymbolSemanticKind semanticKind,
                AccessorDeclarationSyntax accessorDeclaration,
                IMethodSymbol methodSymbol )
            {
                var linkedBody = this.GetSubstitutedBody(
                    methodSymbol.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this,
                        context,
                        new InliningContextIdentifier( methodSymbol.ToSemantic( semanticKind ) ) ) );

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
                        { SemicolonToken: var semicolonToken } => (
                            semicolonToken.LeadingTrivia.AddOptionalLineFeed( context ),
                            semicolonToken.TrailingTrivia.AddOptionalLineFeed( context ),
                            context.ElasticEndOfLineTriviaList, context.ElasticEndOfLineTriviaList),
                        _ => throw new AssertionFailedException( $"Unexpected accessor declaration: {accessorDeclaration}" )
                    };

                return accessorDeclaration.PartialUpdate(
                    expressionBody: null,
                    body: linkedBody
                        .PartialUpdate(
                            openBraceToken: Token( openBraceLeadingTrivia, SyntaxKind.OpenBraceToken, openBraceTrailingTrivia ),
                            closeBraceToken: Token( closeBraceLeadingTrivia, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                    semicolonToken: default(SyntaxToken) );
            }
        }

#if ROSLYN_4_12_0_OR_GREATER
        private static IndexerDeclarationSyntax RemoveAttributesForPartialImplementation( IndexerDeclarationSyntax declaration )
        {
            return
                declaration.PartialUpdate(
                    attributeLists: List<AttributeListSyntax>(),
                    parameterList: declaration.ParameterList.PartialUpdate(
                        parameters: SeparatedList(
                            declaration.ParameterList.Parameters.SelectAsArray( p => p.PartialUpdate( attributeLists: List<AttributeListSyntax>() ) ) ) ),
                    accessorList: declaration.AccessorList?.PartialUpdate(
                        accessors: List(
                            declaration.AccessorList.Accessors.SelectAsArray( a => a.PartialUpdate( attributeLists: List<AttributeListSyntax>() ) ) ) ) );
        }
#endif

        private static BlockSyntax GetImplicitIndexerGetterBody( IMethodSymbol symbol, SyntaxGenerationContext context )
            => context.SyntaxGenerator.FormattedBlock(
                    ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                symbol.IsStatic
                                    ? context.SyntaxGenerator.Type( symbol.ContainingType )
                                    : ThisExpression(),
                                IdentifierName( GetBackingFieldName( (IPropertySymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                            .WithSimplifierAnnotationIfNecessary( context ),
                        Token( SyntaxKind.SemicolonToken ) ) )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

        private static BlockSyntax GetImplicitIndexerSetterBody( IMethodSymbol symbol, SyntaxGenerationContext context )
            => context.SyntaxGenerator.FormattedBlock(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    symbol.IsStatic
                                        ? context.SyntaxGenerator.Type( symbol.ContainingType )
                                        : ThisExpression(),
                                    IdentifierName( GetBackingFieldName( (IPropertySymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                                .WithSimplifierAnnotationIfNecessary( context ),
                            IdentifierName( "value" ) ) ) )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

        private MemberDeclarationSyntax GetOriginalImplIndexer(
            IndexerDeclarationSyntax indexer,
            IPropertySymbol symbol,
            TypeSyntax type,
            BracketedParameterListSyntax parameterList,
            ArrowExpressionClauseSyntax? existingExpressionBody,
            SyntaxGenerationContext context )
        {
            var existingAccessorList = indexer.AccessorList.AssertNotNull();

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
                                            SyntaxKind.GetAccessorDeclaration => symbol.GetMethod.AssertNotNull(),
                                            SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration => symbol.SetMethod.AssertNotNull(),
                                            _ => throw new AssertionFailedException( $"Unexpected kind: {a}" )
                                        } ) ) ) )
                    .WithSourceCodeAnnotation();

            return this.GetSpecialImplIndexer(
                type,
                parameterList,
                transformedAccessorList,
                existingExpressionBody,
                symbol,
                GetOriginalImplParameterType(),
                context );

            AccessorDeclarationSyntax TransformAccessor( AccessorDeclarationSyntax accessorDeclaration, IMethodSymbol accessorSymbol )
            {
                var semantic = accessorSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var inliningContextId = new InliningContextIdentifier( semantic );

                var substitutedBody =
                    accessorDeclaration.Body != null
                        ? (BlockSyntax) RewriteBody(
                            accessorDeclaration.Body,
                            accessorSymbol,
                            new SubstitutionContext( this, context, inliningContextId ) )
                        : null;

                var substitutedExpressionBody =
                    accessorDeclaration.ExpressionBody != null
                        ? (ArrowExpressionClauseSyntax) RewriteBody(
                            accessorDeclaration.ExpressionBody,
                            accessorSymbol,
                            new SubstitutionContext( this, context, inliningContextId ) )
                        : null;

                return accessorDeclaration.PartialUpdate( body: substitutedBody, expressionBody: substitutedExpressionBody );
            }
        }

        private MemberDeclarationSyntax GetEmptyImplIndexer(
            IPropertySymbol symbol,
            TypeSyntax type,
            BracketedParameterListSyntax parameterList,
            AccessorListSyntax existingAccessorList,
            SyntaxGenerationContext context )
        {
            return this.GetSpecialImplIndexer( type, parameterList, existingAccessorList, null, symbol, GetEmptyImplParameterType(), context );
        }

        private MemberDeclarationSyntax GetSpecialImplIndexer(
            TypeSyntax indexerType,
            BracketedParameterListSyntax indexerParameters,
            AccessorListSyntax? accessorList,
            ArrowExpressionClauseSyntax? expressionBody,
            IPropertySymbol symbol,
            TypeSyntax specialImplType,
            SyntaxGenerationContext context )
        {
            return
                IndexerDeclaration(
                        this.FilterAttributesOnSpecialImpl( symbol ),
                        symbol.IsStatic
                            ? TokenList(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        indexerType,
                        null,
                        Token( SyntaxKind.ThisKeyword ),
                        this.FilterAttributesOnSpecialImpl(
                            symbol.Parameters,
                            indexerParameters
                                .WithOptionalTrailingTrivia( default(SyntaxTriviaList), this.SyntaxGenerationOptions )
                                .WithAdditionalParameters( (specialImplType, AspectReferenceSyntaxProvider.LinkerOverrideParamName) ) ),
                        accessorList,
                        expressionBody,
                        expressionBody != null ? Token( SyntaxKind.SemicolonToken ) : default )
                    .WithOptionalLeadingAndTrailingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private IndexerDeclarationSyntax GetTrampolineForIndexer(
            IndexerDeclarationSyntax indexer,
            IPropertySymbol targetSymbol,
            SyntaxGenerationContext context )
        {
            var getAccessor = indexer.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.GetAccessorDeclaration );
            var setAccessor = indexer.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.SetAccessorDeclaration );

            return indexer
                .PartialUpdate(
                    accessorList: AccessorList(
                        List(
                            new[]
                                {
                                    getAccessor != null
                                        ? AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            context.SyntaxGenerator.FormattedBlock(
                                                ReturnStatement(
                                                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                                    GetInvocationTarget(),
                                                    Token( SyntaxKind.SemicolonToken ) ) ) )
                                        : null,
                                    setAccessor != null
                                        ? AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration,
                                            context.SyntaxGenerator.FormattedBlock(
                                                ExpressionStatement(
                                                    AssignmentExpression(
                                                        SyntaxKind.SimpleAssignmentExpression,
                                                        GetInvocationTarget(),
                                                        IdentifierName( "value" ) ) ) ) )
                                        : null
                                }.Where( a => a != null )
                                .AssertNoneNull() ) ),
                    expressionBody: null,
                    semicolonToken: default(SyntaxToken) )
                .WithTriviaFromIfNecessary( indexer, this.SyntaxGenerationOptions );

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