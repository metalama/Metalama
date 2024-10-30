// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        private IReadOnlyList<MemberDeclarationSyntax> RewriteProperty(
            PropertyDeclarationSyntax propertyDeclaration,
            IPropertySymbol symbol,
            SyntaxGenerationContext generationContext )
        {
            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
#if ROSLYN_4_12_0_OR_GREATER
                if ( symbol is { IsPartialDefinition: true, PartialImplementationPart: { } } )
                {
                    // This is a partial property declaration that is not to be transformed.
                    return [propertyDeclaration];
                }
#endif

                var members = new List<MemberDeclarationSyntax>();

#if ROSLYN_4_12_0_OR_GREATER
                if ( symbol is { IsPartialDefinition: true, PartialImplementationPart: null } )
                {
                    // This is a partial property declaration that did not have any body.
                    // Keep it as is and add a new declaration that will contain the override.
                    members.Add( propertyDeclaration );
                }
#endif

                var lastOverride = (IPropertySymbol) this.InjectionRegistry.GetLastOverride( symbol );

                if ( propertyDeclaration.IsAutoPropertyDeclaration()
                     && this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    // Backing field for auto property.
                    members.Add(
                        this.GetPropertyBackingField(
                            propertyDeclaration.Type,
                            propertyDeclaration.Initializer,
                            FilterAttributeListsForTarget( propertyDeclaration.AttributeLists, SyntaxKind.FieldKeyword, false, false ),
                            symbol,
                            generationContext ) );
                }

                if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final, true ) );
                }
                else
                {
                    members.Add(
                        this.GetTrampolineForProperty(
                            propertyDeclaration,
                            lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            generationContext ) );
                }

                if ( !propertyDeclaration.IsAutoPropertyDeclaration()
                     && this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && this.ShouldGenerateSourceMember( symbol ) )
                {
                    members.Add(
                        this.GetOriginalImplProperty(
                            symbol,
                            FilterAttributeListsForTarget( propertyDeclaration.AttributeLists, SyntaxKind.FieldKeyword, false, true ),
                            propertyDeclaration.Type,
                            propertyDeclaration.Initializer,
                            propertyDeclaration.AccessorList,
                            propertyDeclaration.ExpressionBody,
                            generationContext ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add(
                        this.GetEmptyImplProperty(
                            symbol,
                            List<AttributeListSyntax>(),
                            propertyDeclaration.Type,
                            generationContext ) );
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
            else if ( this.AnalysisRegistry.HasBaseSemanticReferences( symbol ) )
            {
                Invariant.Assert( symbol is { IsOverride: true, IsSealed: false } or { IsVirtual: true } );

                return
                [
                    this.GetTrampolineForProperty( propertyDeclaration, symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ), generationContext ),
                    this.GetOriginalImplProperty(
                        symbol,
                        FilterAttributeListsForTarget( propertyDeclaration.AttributeLists, SyntaxKind.FieldKeyword, false, true ),
                        propertyDeclaration.Type,
                        propertyDeclaration.Initializer,
                        propertyDeclaration.AccessorList,
                        propertyDeclaration.ExpressionBody,
                        generationContext )
                ];
            }
            else if ( this.AnalysisRegistry.HasAnySubstitutions( symbol ) )
            {
                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default, false )];
            }
            else
            {
                if ( this.LateTransformationRegistry.IsPrimaryConstructorInitializedMember( symbol ) )
                {
                    propertyDeclaration = propertyDeclaration.PartialUpdate(
                        initializer: default(EqualsValueClauseSyntax),
                        semicolonToken: default(SyntaxToken) );
                }

                return [propertyDeclaration];
            }

            MemberDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind, bool isOverrideOrOverrideTarget )
            {
                var transformedAccessors = new List<AccessorDeclarationSyntax>();

                if ( symbol.GetMethod != null )
                {
                    if ( propertyDeclaration.AccessorList?.Accessors.SingleOrDefault( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) ) is
                        { } getAccessorDeclaration )
                    {
                        transformedAccessors.Add( GetLinkedAccessor( semanticKind, getAccessorDeclaration, symbol.GetMethod, isOverrideOrOverrideTarget ) );
                    }
                    else if ( propertyDeclaration.ExpressionBody != null )
                    {
                        var linkedBody = this.GetSubstitutedBody(
                            symbol.GetMethod.ToSemantic( semanticKind ),
                            new SubstitutionContext(
                                this,
                                generationContext,
                                new InliningContextIdentifier( symbol.GetMethod.ToSemantic( semanticKind ) ) ) );

                        transformedAccessors.Add(
                            AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                FilterAttributeListsForTarget( propertyDeclaration.AttributeLists, SyntaxKind.MethodKeyword, false, false ),
                                TokenList(),
                                Token( TriviaList( ElasticMarker ), SyntaxKind.GetKeyword, TriviaList( ElasticMarker ) ),
                                Block(
                                    Token(
                                        generationContext.ElasticEndOfLineTriviaList,
                                        SyntaxKind.OpenBraceToken,
                                        generationContext.ElasticEndOfLineTriviaList ),
                                    SingletonList<StatementSyntax>( linkedBody ),
                                    Token( generationContext.ElasticEndOfLineTriviaList, SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) ),
                                null,
                                default ) );
                    }
                }

                if ( symbol.SetMethod != null )
                {
                    var setAccessorDeclaration = propertyDeclaration.AccessorList!.Accessors.Single(
                        a => a.IsKind( SyntaxKind.SetAccessorDeclaration ) || a.IsKind( SyntaxKind.InitAccessorDeclaration ) );

                    transformedAccessors.Add( GetLinkedAccessor( semanticKind, setAccessorDeclaration, symbol.SetMethod, isOverrideOrOverrideTarget ) );
                }

                // Trivia processing for the property declaration:
                //   * Properties with accessor lists are not changed (accessors are expected to be correct, accessor lists inherit the same trivia).
                //   * Properties with expression body are transformed in the following way (trivia of the <expr> are already processed):
                //      int Property <trivia_leading_equals_value> => <trivia_trailing_equals_value> <expr> <trivia_leading_semicolon>; <trivia_trailing_semicolon>
                //      int Property <trivia_leading_equals_value> { <trivia_after_equals_value> get { ... } <trivia_before_semicolon> } <trivia_trailing_semicolon>

                var (accessorListLeadingTrivia, accessorStartingTrivia, accessorEndingTrivia, accessorListTrailingTrivia) = propertyDeclaration switch
                {
                    { AccessorList: not null and var accessorList } => (
                        accessorList.OpenBraceToken.LeadingTrivia,
                        accessorList.OpenBraceToken.TrailingTrivia,
                        accessorList.CloseBraceToken.LeadingTrivia,
                        accessorList.CloseBraceToken.TrailingTrivia),
                    { ExpressionBody: not null and var expressionBody } => (
                        expressionBody.ArrowToken.LeadingTrivia,
                        expressionBody.ArrowToken.TrailingTrivia,
                        propertyDeclaration.SemicolonToken.LeadingTrivia,
                        propertyDeclaration.SemicolonToken.TrailingTrivia),
                    _ => throw new AssertionFailedException( $"Unexpected property declaration: '{propertyDeclaration}'." )
                };

                accessorListLeadingTrivia =
                    propertyDeclaration.Identifier switch
                    {
                        var identifier when identifier.TrailingTrivia.HasAnyNewLine() => accessorListLeadingTrivia,
                        _ => generationContext.ElasticEndOfLineTriviaList.AddRange( accessorListLeadingTrivia )
                    };

                var result = propertyDeclaration.PartialUpdate(
                    attributeLists: FilterAttributeListsForTarget( propertyDeclaration.AttributeLists, SyntaxKind.PropertyKeyword, true, true ),
                    accessorList: AccessorList(
                            Token( accessorListLeadingTrivia, SyntaxKind.OpenBraceToken, accessorStartingTrivia ),
                            List( transformedAccessors ),
                            Token( accessorEndingTrivia, SyntaxKind.CloseBraceToken, accessorListTrailingTrivia ) )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                    expressionBody: null,
                    initializer: null,
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
                        generationContext,
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
                            (arrowToken.LeadingTrivia.AddOptionalLineFeed( generationContext ),
                             arrowToken.TrailingTrivia.AddOptionalLineFeed( generationContext ),
                             semicolonToken.LeadingTrivia.AddOptionalLineFeed( generationContext ), semicolonToken.TrailingTrivia),
                        { SemicolonToken: var semicolonToken } => (
                            semicolonToken.LeadingTrivia.AddOptionalLineFeed( generationContext ),
                            semicolonToken.TrailingTrivia.AddOptionalLineFeed( generationContext ),
                            generationContext.ElasticEndOfLineTriviaList, generationContext.ElasticEndOfLineTriviaList),
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

#if ROSLYN_4_12_0_OR_GREATER
        private static PropertyDeclarationSyntax RemoveAttributesForPartialImplementation( PropertyDeclarationSyntax declaration )
        {
            return
                declaration.PartialUpdate(
                    attributeLists: List<AttributeListSyntax>(),
                    accessorList: declaration.AccessorList?.PartialUpdate(
                        accessors: List(
                            declaration.AccessorList.Accessors.SelectAsArray( a => a.PartialUpdate( attributeLists: List<AttributeListSyntax>() ) ) ) ) );
        }
#endif

        private FieldDeclarationSyntax GetPropertyBackingField(
            TypeSyntax type,
            EqualsValueClauseSyntax? initializer,
            SyntaxList<AttributeListSyntax> attributes,
            IPropertySymbol symbol,
            SyntaxGenerationContext context )
        {
            var modifiers = new List<SyntaxToken> { SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) };

            if ( symbol.SetMethod == null || symbol.SetMethod.IsInitOnly )
            {
                modifiers.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReadOnlyKeyword ) );
            }

            if ( symbol.IsStatic )
            {
                modifiers.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) );
            }

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

            return FieldDeclaration(
                    attributes,
                    TokenList( modifiers ),
                    VariableDeclaration(
                        type.WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier( GetBackingFieldName( symbol ) ),
                                null,
                                initializer ) ) ) )
                .WithOptionalTrivia(
                    context.TwoElasticEndOfLinesTriviaList,
                    context.ElasticEndOfLineTriviaList,
                    this.SyntaxGenerationOptions )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private static BlockSyntax GetImplicitGetterBody( IMethodSymbol symbol, SyntaxGenerationContext generationContext )
            => generationContext.SyntaxGenerator.FormattedBlock(
                    ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                symbol.IsStatic
                                    ? generationContext.SyntaxGenerator.Type( symbol.ContainingType )
                                    : ThisExpression(),
                                IdentifierName( GetBackingFieldName( (IPropertySymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                            .WithSimplifierAnnotationIfNecessary( generationContext ),
                        Token( SyntaxKind.SemicolonToken ) ) )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

        private static BlockSyntax GetImplicitSetterBody( IMethodSymbol symbol, SyntaxGenerationContext generationContext )
            => generationContext.SyntaxGenerator.FormattedBlock(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    symbol.IsStatic
                                        ? generationContext.SyntaxGenerator.Type( symbol.ContainingType )
                                        : ThisExpression(),
                                    IdentifierName( GetBackingFieldName( (IPropertySymbol) symbol.AssociatedSymbol.AssertNotNull() ) ) )
                                .WithSimplifierAnnotationIfNecessary( generationContext ),
                            IdentifierName( "value" ) ) ) )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

        private MemberDeclarationSyntax GetOriginalImplProperty(
            IPropertySymbol symbol,
            SyntaxList<AttributeListSyntax> attributes,
            TypeSyntax type,
            EqualsValueClauseSyntax? initializer,
            AccessorListSyntax? existingAccessorList,
            ArrowExpressionClauseSyntax? existingExpressionBody,
            SyntaxGenerationContext generationContext )
        {
            var accessorList =
                existingAccessorList
                    ?.WithAccessors(
                        List(
                            existingAccessorList.Accessors.SelectAsArray(
                                a =>
                                    TransformAccessor(
                                        a,
                                        a.Kind() switch
                                        {
                                            SyntaxKind.GetAccessorDeclaration => symbol.GetMethod.AssertNotNull(),
                                            SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration => symbol.SetMethod.AssertNotNull(),
                                            _ => throw new AssertionFailedException( $"Unexpected accessor: {a}" )
                                        } ) ) ) )
                    .WithSourceCodeAnnotation();

            var transformedExpressionBody =
                existingExpressionBody != null
                    ? TransformExpressionBody( existingExpressionBody, symbol.GetMethod.AssertNotNull() )
                    : null;

            return this.GetSpecialImplProperty(
                attributes,
                type,
                accessorList,
                transformedExpressionBody,
                initializer.WithSourceCodeAnnotation(),
                symbol,
                GetOriginalImplMemberName( symbol ),
                generationContext );

            AccessorDeclarationSyntax TransformAccessor( AccessorDeclarationSyntax accessorDeclaration, IMethodSymbol accessorSymbol )
            {
                var semantic = accessorSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var context = new InliningContextIdentifier( semantic );

                var substitutedBody =
                    accessorDeclaration.Body != null
                        ? (BlockSyntax) RewriteBody(
                            accessorDeclaration.Body,
                            accessorSymbol,
                            new SubstitutionContext( this, generationContext, context ) )
                        : null;

                var substitutedExpressionBody =
                    accessorDeclaration.ExpressionBody != null
                        ? (ArrowExpressionClauseSyntax) RewriteBody(
                            accessorDeclaration.ExpressionBody,
                            accessorSymbol,
                            new SubstitutionContext( this, generationContext, context ) )
                        : null;

                return accessorDeclaration.PartialUpdate(
                    modifiers: TokenList( accessorDeclaration.Modifiers.Where( t => !t.IsAccessModifierKeyword() ) ),
                    body: substitutedBody,
                    expressionBody: substitutedExpressionBody );
            }

            ArrowExpressionClauseSyntax TransformExpressionBody( ArrowExpressionClauseSyntax expressionBody, IMethodSymbol accessorSymbol )
            {
                var semantic = accessorSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var context = new InliningContextIdentifier( semantic );

                var substitutedExpressionBody =
                    (ArrowExpressionClauseSyntax) RewriteBody(
                        expressionBody,
                        accessorSymbol,
                        new SubstitutionContext( this, generationContext, context ) );

                return substitutedExpressionBody;
            }
        }

        private MemberDeclarationSyntax GetEmptyImplProperty(
            IPropertySymbol symbol,
            SyntaxList<AttributeListSyntax> attributes,
            TypeSyntax type,
            SyntaxGenerationContext context )
        {
            var setAccessorKind =
                symbol switch
                {
                    { SetMethod.IsInitOnly: false } => SyntaxKind.SetAccessorDeclaration,
                    { SetMethod.IsInitOnly: true } => SyntaxKind.InitAccessorDeclaration,
                    { SetMethod: null, OverriddenProperty: not null } => SyntaxKind.InitAccessorDeclaration,
                    _ => (SyntaxKind?) null
                };

            var accessorList =
                AccessorList(
                    List(
                        new[]
                            {
                                symbol.GetMethod != null
                                    ? AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        List<AttributeListSyntax>(),
                                        TokenList(),
                                        Token( SyntaxKind.GetKeyword ),
                                        null,
                                        ArrowExpressionClause( DefaultExpression( type ) ),
                                        Token( SyntaxKind.SemicolonToken ) )
                                    : null,
                                setAccessorKind != null
                                    ? AccessorDeclaration(
                                        setAccessorKind.Value,
                                        context.SyntaxGenerator.FormattedBlock() )
                                    : null
                            }.Where( a => a != null )
                            .AssertNoneNull() ) );

            return this.GetSpecialImplProperty( attributes, type, accessorList, null, null, symbol, GetEmptyImplMemberName( symbol ), context );
        }

        private MemberDeclarationSyntax GetSpecialImplProperty(
            SyntaxList<AttributeListSyntax> attributes,
            TypeSyntax propertyType,
            AccessorListSyntax? accessorList,
            ArrowExpressionClauseSyntax? expressionBody,
            EqualsValueClauseSyntax? initializer,
            IPropertySymbol symbol,
            string name,
            SyntaxGenerationContext context )
        {
            var cleanAccessorList =
                accessorList?.WithAccessors(
                    List(
                        accessorList.Accessors.SelectAsReadOnlyList(
                            a =>
                                a.Kind() switch
                                {
                                    SyntaxKind.GetAccessorDeclaration => this.FilterAttributesOnSpecialImpl( symbol.GetMethod.AssertNotNull(), a ),
                                    SyntaxKind.SetAccessorDeclaration => symbol.SetMethod != null
                                        ? this.FilterAttributesOnSpecialImpl( symbol.SetMethod, a )
                                        : a,
                                    SyntaxKind.InitAccessorDeclaration => symbol.SetMethod != null
                                        ? this.FilterAttributesOnSpecialImpl( symbol.SetMethod, a )
                                        : a,
                                    _ => throw new AssertionFailedException( $"Unexpected accessor: {a}" )
                                } ) ) );

            return
                PropertyDeclaration(
                        attributes,
                        symbol.IsStatic
                            ? TokenList(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        propertyType,
                        null,
                        Identifier( name ),
                        cleanAccessorList?.WithOptionalTrailingLineFeed( context ),
                        expressionBody,
                        initializer.WithSourceCodeAnnotation(),
                        semicolonToken: expressionBody != null || initializer != null
                            ? Token( default, SyntaxKind.SemicolonToken, context.ElasticEndOfLineTriviaList )
                            : default )
                    .WithOptionalLeadingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private PropertyDeclarationSyntax GetTrampolineForProperty(
            PropertyDeclarationSyntax property,
            IntermediateSymbolSemantic<IPropertySymbol> targetSymbol,
            SyntaxGenerationContext context )
        {
            var getAccessor = property.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.GetAccessorDeclaration );
            var setAccessor = property.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.SetAccessorDeclaration );

            return property
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
                    initializer: null,
                    semicolonToken: default(SyntaxToken) )
                .WithTriviaFromIfNecessary( property, this.SyntaxGenerationOptions );

            ExpressionSyntax GetInvocationTarget()
            {
                if ( targetSymbol.Symbol.IsStatic )
                {
                    return IdentifierName( targetSymbol.Symbol.Name );
                }
                else
                {
                    return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( targetSymbol.Symbol.Name ) )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }
        }
    }
}