// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Transformations;

internal static class TransformationHelper
{
    public static BlockSyntax CreateIdentityAccessorBody(
        SyntaxKind accessorDeclarationKind,
        ExpressionSyntax proceedExpression,
        SyntaxGenerationContext context )
    {
        switch ( accessorDeclarationKind )
        {
            case SyntaxKind.GetAccessorDeclaration:
                return context.SyntaxGenerator.FormattedBlock(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        proceedExpression,
                        SyntaxFactory.Token( SyntaxKind.SemicolonToken ) ) );

            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.InitAccessorDeclaration:
                return context.SyntaxGenerator.FormattedBlock( SyntaxFactory.ExpressionStatement( proceedExpression ) );

            default:
                throw new AssertionFailedException( $"Unexpected SyntaxKind: {accessorDeclarationKind}." );
        }
    }

    public static ExpressionSyntax CreatePropertyProceedGetExpression(
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        SyntaxGenerationContext syntaxGenerationContext,
        IProperty targetProperty,
        AspectLayerId aspectLayer )
        => aspectReferenceSyntaxProvider.GetPropertyReference(
            aspectLayer,
            targetProperty,
            AspectReferenceTargetKind.PropertyGetAccessor,
            syntaxGenerationContext.SyntaxGenerator );

    public static ExpressionSyntax CreatePropertyProceedSetExpression(
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        SyntaxGenerationContext syntaxGenerationContext,
        IProperty targetProperty,
        AspectLayerId aspectLayer )
        => SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            aspectReferenceSyntaxProvider.GetPropertyReference(
                aspectLayer,
                targetProperty,
                AspectReferenceTargetKind.PropertySetAccessor,
                syntaxGenerationContext.SyntaxGenerator ),
            SyntaxFactory.IdentifierName( "value" ) );

    public static ExpressionSyntax CreateIndexerProceedGetExpression(
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        SyntaxGenerationContext syntaxGenerationContext,
        IIndexer targetIndexer,
        AspectLayerId aspectLayer )
        => aspectReferenceSyntaxProvider.GetIndexerReference(
            aspectLayer,
            targetIndexer,
            AspectReferenceTargetKind.PropertyGetAccessor,
            syntaxGenerationContext.SyntaxGenerator );

    public static ExpressionSyntax CreateIndexerProceedSetExpression(
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        SyntaxGenerationContext syntaxGenerationContext,
        IIndexer targetIndexer,
        AspectLayerId aspectLayer )
        => SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            aspectReferenceSyntaxProvider.GetIndexerReference(
                aspectLayer,
                targetIndexer,
                AspectReferenceTargetKind.PropertySetAccessor,
                syntaxGenerationContext.SyntaxGenerator ),
            SyntaxFactory.IdentifierName( "value" ) );

    public static BracketedParameterListSyntax GetIndexerOverrideParameterList(
        CompilationModel compilation,
        SyntaxGenerationContext syntaxGenerationContext,
        IIndexer indexer,
        TypeSyntax additionalParameterType )
    {
        var originalParameterList = syntaxGenerationContext.SyntaxGenerator.ParameterList( indexer, compilation, true );
        var overriddenByParameterType = additionalParameterType;

        return originalParameterList.WithAdditionalParameters( (overriddenByParameterType, AspectReferenceSyntaxProvider.LinkerOverrideParamName) );
    }

    public static SyntaxGenerationContext GetSyntaxGenerationContext(
        this CompilationContext compilationContext,
        SyntaxGenerationOptions options,
        IDeclaration declaration )
    {
        switch ( declaration )
        {
            case SymbolBasedDeclaration symbolBasedDeclaration:
                var primaryDeclaration = symbolBasedDeclaration.GetClosestPrimaryDeclarationSyntax().AssertNotNull();

                return compilationContext.GetSyntaxGenerationContext( options, primaryDeclaration );

            case IntroducedDeclaration builtDeclaration:
                return GetSyntaxGenerationContext( compilationContext, options, builtDeclaration.BuilderData.InsertPosition );

            default:
                throw new AssertionFailedException( $"Unexpected declaration: {declaration}" );
        }
    }

    public static SyntaxGenerationContext GetSyntaxGenerationContext(
        this CompilationContext compilationContext,
        SyntaxGenerationOptions options,
        InsertPosition insertPosition )
    {
        if ( insertPosition is { Relation: InsertPositionRelation.Within, BuilderData: { } containingBuilder } )
        {
            return GetSyntaxGenerationContext( compilationContext, options, containingBuilder.InsertPosition );
        }

        if ( insertPosition is { Relation: InsertPositionRelation.Root } )
        {
            // TODO: This is temporary.
            return compilationContext.GetSyntaxGenerationContext( options, false, false, "\n" );
        }

        var insertOffset = insertPosition switch
        {
            { Relation: InsertPositionRelation.After, SyntaxNode: { } node } => node.Span.End + 1,
            { Relation: InsertPositionRelation.Within, SyntaxNode: BaseTypeDeclarationSyntax node } => node.CloseBraceToken.Span.Start - 1,
            { Relation: InsertPositionRelation.Within, SyntaxNode: NamespaceDeclarationSyntax node } => node.CloseBraceToken.Span.Start - 1,
            { Relation: InsertPositionRelation.Within, SyntaxNode: FileScopedNamespaceDeclarationSyntax node } => node.Name.Span.End,
            _ => throw new AssertionFailedException( $"Unsupported {insertPosition}." )
        };

        return compilationContext.GetSyntaxGenerationContext( options, insertPosition.SyntaxTree, insertOffset );
    }
}