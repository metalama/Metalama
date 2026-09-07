// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceIndexerTransformation : IntroduceMemberTransformation<IndexerBuilderData>
{
    public IntroduceIndexerTransformation( AspectLayerInstance aspectLayerInstance, IndexerBuilderData introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration ) { }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var finalIndexer = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

        var indexerSyntax =
            IndexerDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( finalIndexer, context ),
                finalIndexer.GetSyntaxModifierList(),
                syntaxGenerator.TypeSyntax( finalIndexer.Type ).WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                finalIndexer.ExplicitInterfaceImplementations.Count > 0
                    ? ExplicitInterfaceSpecifier(
                        (NameSyntax) syntaxGenerator.TypeSyntax( finalIndexer.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                    : null,
                Token( SyntaxKind.ThisKeyword ),
                context.SyntaxGenerator.ParameterList( finalIndexer, context.FinalCompilation ),
                GenerateAccessorList(),
                null,
                default );

        var injectedIndexer = new InjectedMember(
            this,
            indexerSyntax,
            this.AspectLayerId,
            InjectedMemberSemantic.Introduction,
            this.BuilderData.ToRef() );

        return [injectedIndexer];

        AccessorListSyntax GenerateAccessorList()
        {
            switch (finalIndexer.Writeability, finalIndexer.GetMethod, finalIndexer.SetMethod)
            {
                // Indexers with both accessors.
                case (_, not null, not null):
                    return SyntaxFactoryEx.FormattedAccessorList(
                        [GenerateGetAccessor(), GenerateSetAccessor()],
                        context.SyntaxGenerationContext );

                // Indexers with only get accessor.
                case (_, not null, null):
                    return SyntaxFactoryEx.FormattedAccessorList( [GenerateGetAccessor()], context.SyntaxGenerationContext );

                // Indexers with only set accessor.
                case (_, null, not null):
                    return SyntaxFactoryEx.FormattedAccessorList( [GenerateSetAccessor()], context.SyntaxGenerationContext );

                default:
                    throw new AssertionFailedException( "Both the getter and the setter are undefined." );
            }
        }

        AccessorDeclarationSyntax GenerateGetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( finalIndexer.GetMethod!.Accessibility != finalIndexer.Accessibility )
            {
                finalIndexer.GetMethod.Accessibility.AddTokens( tokens );
            }

            var hasNoBody = finalIndexer.IsAbstract;

            return
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalIndexer.GetMethod, context ),
                    TokenList( tokens ),
                    Token( SyntaxKind.GetKeyword ),
                    hasNoBody
                        ? null
                        : syntaxGenerator.FormattedBlock(
                            ReturnStatement(
                                Token( TriviaList(), SyntaxKind.ReturnKeyword, TriviaList( ElasticSpace ) ),
                                DefaultExpression( syntaxGenerator.TypeSyntax( finalIndexer.Type ) ),
                                Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) ) ),
                    null,
                    hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );
        }

        AccessorDeclarationSyntax GenerateSetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( finalIndexer.SetMethod!.Accessibility != finalIndexer.Accessibility )
            {
                finalIndexer.SetMethod.Accessibility.AddTokens( tokens );
            }

            var hasNoBody = finalIndexer.IsAbstract;

            // The keyword needs a trailing space only when followed by a body block ("set { ... }").
            // For bodyless accessors ("set;"), no trailing trivia is emitted, so we don't get "set ;".
            var keywordKind = this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword;
            var keyword = hasNoBody
                ? Token( keywordKind )
                : Token( default, keywordKind, TriviaList( ElasticSpace ) );

            return
                AccessorDeclaration(
                    this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalIndexer.SetMethod, context ),
                    TokenList( tokens ),
                    keyword,
                    hasNoBody
                        ? null
                        : context.SyntaxGenerator.FormattedBlock(),
                    null,
                    hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );
        }
    }

    /// <inheritdoc />
    public override IEnumerable<DeclarationBuilderData> GetImplicitDeclarations()
    {
        // An indexer introduced into an extension block needs the static implementation methods that the compiler
        // creates in the enclosing static class, as an introduced method or property does. Each one takes the
        // receiver first, then the index parameters, and, for the setter, the assigned value last.
        var containingDeclaration = this.BuilderData.ContainingDeclaration.GetTarget( this.InitialCompilation );

        if ( containingDeclaration is not IExtensionBlock extensionBlock )
        {
            return [];
        }

        var result = new List<DeclarationBuilderData>( 2 );
        var indexerType = this.BuilderData.Type.GetTarget( this.InitialCompilation );
        var metadataName = this.GetIndexerMetadataName();

        if ( this.BuilderData.GetMethod != null )
        {
            result.Add(
                ExtensionImplementationHelper.CreateImplicitAccessorMethod(
                    this.AspectLayerInstance,
                    extensionBlock,
                    metadataName,
                    isSetter: false,
                    this.BuilderData.GetMethod.Accessibility,
                    this.BuilderData.IsStatic,
                    indexerType,
                    this.BuilderData.RefKind,
                    this.InitialCompilation,
                    this.BuilderData.GetMethod.Attributes,
                    this.BuilderData.GetMethod.ReturnParameter.Attributes,
                    this.BuilderData.Parameters ) );
        }

        if ( this.BuilderData.SetMethod != null )
        {
            result.Add(
                ExtensionImplementationHelper.CreateImplicitAccessorMethod(
                    this.AspectLayerInstance,
                    extensionBlock,
                    metadataName,
                    isSetter: true,
                    this.BuilderData.SetMethod.Accessibility,
                    this.BuilderData.IsStatic,
                    indexerType,
                    this.BuilderData.RefKind,
                    this.InitialCompilation,
                    this.BuilderData.SetMethod.Attributes,
                    this.BuilderData.SetMethod.ReturnParameter.Attributes,
                    this.BuilderData.Parameters ) );
        }

        return result;
    }

    /// <summary>
    /// Gets the name that the indexer has in metadata, which is also the name from which the names of its accessors
    /// are formed. The name is <c>Item</c> unless the indexer carries <see cref="IndexerNameAttribute"/>.
    /// </summary>
    private string GetIndexerMetadataName()
    {
        const string defaultIndexerName = "Item";

        foreach ( var attribute in this.BuilderData.Attributes )
        {
            if ( attribute.ConstructorArguments.Length != 1
                 || attribute.Type.GetTarget( this.InitialCompilation ).FullName != typeof(IndexerNameAttribute).FullName )
            {
                continue;
            }

            if ( attribute.ConstructorArguments[0].ToTypedConstant( this.InitialCompilation ).Value is string indexerName )
            {
                return indexerName;
            }
        }

        return defaultIndexerName;
    }
}