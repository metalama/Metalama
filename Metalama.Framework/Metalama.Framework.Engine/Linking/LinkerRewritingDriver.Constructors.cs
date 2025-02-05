// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Metalama.Framework.Engine.SyntaxGeneration.SyntaxFactoryEx;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerRewritingDriver
{
    private IReadOnlyList<MemberDeclarationSyntax> RewriteConstructor(
        ConstructorDeclarationSyntax constructorDeclaration,
        IMethodSymbol symbol,
        SyntaxGenerationContext context )
    {
        var members = new List<MemberDeclarationSyntax>();

        // If deconstructing primary constructor, add all fields defined by primary constructor parameters.
        if ( this.InjectionRegistry.IsAuxiliarySourceSymbol( symbol )
             && this.LateTransformationRegistry.HasRemovedPrimaryConstructor( symbol.ContainingType ) )
        {
            foreach ( var primaryConstructorField in this.LateTransformationRegistry.GetPrimaryConstructorFields( symbol.ContainingType ) )
            {
                members.Add(
                    FieldDeclaration(
                        List<AttributeListSyntax>(),
                        TokenList( TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ), TokenWithTrailingSpace( SyntaxKind.ReadOnlyKeyword ) ),
                        VariableDeclaration(
                            context.SyntaxGenerator
                                .TypeSyntax( primaryConstructorField.Type )
                                .WithOptionalTrailingTrivia( ElasticSpace, context.Options ),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier( TriviaList( ElasticSpace ), GetCleanPrimaryConstructorFieldName( primaryConstructorField ), default ) ) ) ),
                        Token( SyntaxKind.SemicolonToken ).WithOptionalTrailingLineFeed( context ) ) );
            }

            foreach ( var primaryConstructorProperty in this.LateTransformationRegistry.GetPrimaryConstructorProperties( symbol.ContainingType ) )
            {
                members.Add(
                    PropertyDeclaration(
                        List<AttributeListSyntax>(),
                        TokenList( TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) ),
                        context.SyntaxGenerator.TypeSyntax( primaryConstructorProperty.Type ),
                        null,
                        Identifier( TriviaList( ElasticSpace ), primaryConstructorProperty.Name, default ),
                        AccessorList(
                            List(
                            [
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    List<AttributeListSyntax>(),
                                    TokenList(),
                                    Token( SyntaxKind.GetKeyword ),
                                    null,
                                    null,
                                    Token( SyntaxKind.SemicolonToken ) ),
                                AccessorDeclaration(
                                    SyntaxKind.InitAccessorDeclaration,
                                    List<AttributeListSyntax>(),
                                    TokenList(),
                                    Token( SyntaxKind.InitKeyword ),
                                    null,
                                    null,
                                    Token( SyntaxKind.SemicolonToken ) )
                            ] ) ),
                        null,
                        null,
                        default ) );
            }

            if ( constructorDeclaration.Parent is RecordDeclarationSyntax { ParameterList.Parameters.Count: > 0 } recordDeclaration )
            {
                members.Add(
                    MethodDeclaration(
                        List<AttributeListSyntax>(),
                        TokenList( TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) ),
                        PredefinedType( TokenWithTrailingSpace( SyntaxKind.VoidKeyword ) ),
                        null,
                        Identifier( "Deconstruct" ),
                        null,
                        ParameterList(
                            SeparatedList(
                                recordDeclaration.ParameterList.Parameters.SelectAsArray(
                                    p =>
                                        Parameter(
                                            List<AttributeListSyntax>(),
                                            TokenList( TokenWithTrailingSpace( SyntaxKind.OutKeyword ) ),
                                            p.Type,
                                            p.Identifier,
                                            null ) ) ) ),
                        List<TypeParameterConstraintClauseSyntax>(),
                        Block(
                            recordDeclaration.ParameterList.Parameters.SelectAsArray(
                                p =>
                                    (StatementSyntax) ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName( p.Identifier ),
                                            MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName( p.Identifier ) )
                                                .WithSimplifierAnnotationIfNecessary( context ) ) ) ) ),
                        null,
                        default ) );
            }
        }

        if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
        {
            var lastOverride = this.InjectionRegistry.GetLastOverride( symbol );

            if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
            {
                members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
            }
            else
            {
                throw new AssertionFailedException( $"Non-inlined constructors are not supported: {lastOverride}" );
            }

            if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                 && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                 && this.ShouldGenerateSourceMember( symbol ) )
            {
                throw new AssertionFailedException( $"Non-inlined constructors are not supported: {lastOverride}" );
            }

            if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                 && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                 && this.ShouldGenerateEmptyMember( symbol ) )
            {
                throw new AssertionFailedException( $"Non-inlined constructors are not supported: {lastOverride}" );
            }
        }
        else if ( this.InjectionRegistry.IsOverride( symbol ) )
        {
            if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                 && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
            {
                members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default ) );
            }
        }
        else
        {
            members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default ) );
        }

        return members;

        ConstructorDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
        {
            var linkedBody =
                this.InjectionRegistry.IsOverrideTarget( symbol ) || this.InjectionRegistry.IsOverride( symbol )
                                                                  || this.AnalysisRegistry.HasAnySubstitutions( symbol )
                    ? this.GetSubstitutedBody(
                        symbol.ToSemantic( semanticKind ),
                        new SubstitutionContext(
                            this,
                            context,
                            new InliningContextIdentifier( symbol.ToSemantic( semanticKind ) ) ) )
                    : null;

            var (openBraceLeadingTrivia, openBraceTrailingTrivia, closeBraceLeadingTrivia, closeBraceTrailingTrivia) =
                constructorDeclaration switch
                {
                    { Body: { OpenBraceToken: var openBraceToken, CloseBraceToken: var closeBraceToken } } =>
                        (openBraceToken.LeadingTrivia, openBraceToken.TrailingTrivia, closeBraceToken.LeadingTrivia, closeBraceToken.TrailingTrivia),
                    { ExpressionBody.ArrowToken: var arrowToken, SemicolonToken: var semicolonToken } =>
                        (arrowToken.LeadingTrivia.AddOptionalLineFeed( context ),
                         arrowToken.TrailingTrivia.AddOptionalLineFeed( context ),
                         semicolonToken.LeadingTrivia.AddOptionalLineFeed( context ), semicolonToken.TrailingTrivia),
                    _ => throw new AssertionFailedException( $"Unsupported form of constructor declaration for {symbol}." )
                };

            var isAuxiliaryForPrimaryConstructor = this.InjectionRegistry.IsAuxiliarySourceSymbol( symbol );

            if ( isAuxiliaryForPrimaryConstructor )
            {
                List<StatementSyntax> primaryConstructorFieldAssignments = [];

                foreach ( var primaryConstructorField in this.LateTransformationRegistry.GetPrimaryConstructorFields( symbol.ContainingType ) )
                {
                    var cleanName = GetCleanPrimaryConstructorFieldName( primaryConstructorField );

                    primaryConstructorFieldAssignments.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName( cleanName ) ),
                                    IdentifierName( cleanName ) )
                                .WithSimplifierAnnotationIfNecessary( context ) ) );
                }

                foreach ( var member in symbol.ContainingType.GetMembers() )
                {
                    if ( !this.LateTransformationRegistry.IsPrimaryConstructorInitializedMember( member ) )
                    {
                        continue;
                    }

                    string name;
                    ExpressionSyntax expression;

                    switch ( member )
                    {
                        case IFieldSymbol field:
                            var fieldDeclaration = (VariableDeclaratorSyntax) field.GetPrimaryDeclarationSyntax().AssertNotNull();

                            name = field.Name;
                            expression = fieldDeclaration.Initializer.AssertNotNull().Value;

                            break;

                        case IEventSymbol eventField:
                            var eventFieldDeclaration = (VariableDeclaratorSyntax) eventField.GetPrimaryDeclarationSyntax().AssertNotNull();

                            name = eventField.Name;
                            expression = eventFieldDeclaration.Initializer.AssertNotNull().Value;

                            break;

                        case IPropertySymbol property:
                            var primaryDeclaration = property.GetPrimaryDeclarationSyntax().AssertNotNull();

                            switch ( primaryDeclaration )
                            {
                                case PropertyDeclarationSyntax propertyDeclaration:
                                    name = propertyDeclaration.Identifier.ValueText;
                                    expression = propertyDeclaration.Initializer.AssertNotNull().Value;

                                    break;

                                case ParameterSyntax parameterDeclaration:
                                    name = parameterDeclaration.Identifier.ValueText;
                                    expression = IdentifierName( parameterDeclaration.Identifier.ValueText );

                                    break;

                                default:
                                    throw new AssertionFailedException( $"Unsupported: {primaryDeclaration}" );
                            }

                            break;

                        default:
                            throw new AssertionFailedException( $"Unsupported: {member}" );
                    }

                    primaryConstructorFieldAssignments.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName( name ) )
                                    .WithSimplifierAnnotationIfNecessary( context ),
                                expression ) ) );
                }

                if ( primaryConstructorFieldAssignments.Count > 0 )
                {
                    if ( linkedBody != null )
                    {
                        linkedBody =
                            Block(
                                    Block( primaryConstructorFieldAssignments ).WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    linkedBody )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                    else if ( constructorDeclaration.ExpressionBody != null )
                    {
                        linkedBody =
                            Block(
                                    Block( primaryConstructorFieldAssignments ).WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    ExpressionStatement( constructorDeclaration.ExpressionBody.Expression ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                    else
                    {
                        linkedBody =
                            Block(
                                    Block( primaryConstructorFieldAssignments ).WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    constructorDeclaration.Body.AssertNotNull().WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                }
            }

            var ret = constructorDeclaration.PartialUpdate(
                isAuxiliaryForPrimaryConstructor
                    ? GetPrimaryConstructorAttributes( constructorDeclaration )
                    : constructorDeclaration.AttributeLists,
                isAuxiliaryForPrimaryConstructor
                    ? TokenList(
                        constructorDeclaration.Modifiers.SelectAsArray(
                            x => x.IsKind( SyntaxKind.PrivateKeyword ) ? Token( x.LeadingTrivia, SyntaxKind.PublicKeyword, x.TrailingTrivia ) : x ) )
                    : constructorDeclaration.Modifiers,
                expressionBody:
                linkedBody != null
                    ? null
                    : constructorDeclaration.ExpressionBody,
                body:
                linkedBody != null
                    ? Block(
                            Token( openBraceLeadingTrivia, SyntaxKind.OpenBraceToken, openBraceTrailingTrivia ),
                            SingletonList<StatementSyntax>( linkedBody ),
                            Token( closeBraceLeadingTrivia, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation )
                    : constructorDeclaration.Body,
                parameterList:
                isAuxiliaryForPrimaryConstructor
                    ? constructorDeclaration.ParameterList.WithParameters(
                        constructorDeclaration.ParameterList.Parameters.RemoveAt(
                            constructorDeclaration.ParameterList.Parameters switch
                            {
                                [.., { Identifier.ValueText: AspectReferenceSyntaxProvider.LinkerOverrideParamName }, _] 
                                    => constructorDeclaration.ParameterList.Parameters.Count - 2,
                                _ => constructorDeclaration.ParameterList.Parameters.Count - 1,
                            } ) )
                    : constructorDeclaration.ParameterList,
                initializer:
                isAuxiliaryForPrimaryConstructor
                    ? this.LateTransformationRegistry.GetPrimaryConstructorBaseArgumentList( symbol ) switch
                    {
                        { } arguments => ConstructorInitializer( SyntaxKind.BaseConstructorInitializer, arguments ),
                        null => default
                    }
                    : constructorDeclaration.Initializer,
                semicolonToken:
                linkedBody != null
                    ? default
                    : constructorDeclaration.SemicolonToken );

            return ret;
        }
    }

    private static SyntaxList<AttributeListSyntax> GetPrimaryConstructorAttributes( ConstructorDeclarationSyntax constructorDeclaration )
    {
        var typeDeclaration = (TypeDeclarationSyntax) constructorDeclaration.Parent.AssertNotNull();

        return
            List(
                typeDeclaration.AttributeLists
                    .Where( al => al.Target?.Identifier.IsKind( SyntaxKind.MethodKeyword ) == true )
                    .Select( al => al.WithTarget( null ) ) );
    }

    private static string GetCleanPrimaryConstructorFieldName( IFieldSymbol field ) => field.Name[1..^2];
}