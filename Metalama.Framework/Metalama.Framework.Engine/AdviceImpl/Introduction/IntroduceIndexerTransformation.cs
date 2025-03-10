// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
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
                    ? ExplicitInterfaceSpecifier( (NameSyntax) syntaxGenerator.TypeSyntax( finalIndexer.ExplicitInterfaceImplementations.Single().DeclaringType ) )
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
                    return AccessorList( List( [GenerateGetAccessor(), GenerateSetAccessor()] ) );

                // Indexers with only get accessor.
                case (_, not null, null):
                    return AccessorList( List( [GenerateGetAccessor()] ) );

                // Indexers with only set accessor.
                case (_, null, not null):
                    return AccessorList( List( [GenerateSetAccessor()] ) );

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
                                Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) ),
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

            return
                AccessorDeclaration(
                    this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalIndexer.SetMethod, context ),
                    TokenList( tokens ),
                    this.BuilderData.HasInitOnlySetter
                        ? Token( TriviaList(), SyntaxKind.InitKeyword, TriviaList( ElasticSpace ) )
                        : Token( TriviaList(), SyntaxKind.SetKeyword, TriviaList( ElasticSpace ) ),
                    hasNoBody
                        ? null
                        : context.SyntaxGenerator.FormattedBlock(),
                    null,
                    hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );
        }
    }
}