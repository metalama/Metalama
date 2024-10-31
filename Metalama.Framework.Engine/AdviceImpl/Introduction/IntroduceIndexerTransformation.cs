// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        var indexer = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

        var indexerSyntax =
            IndexerDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( indexer, context ),
                indexer.GetSyntaxModifierList(),
                syntaxGenerator.TypeSyntax( indexer.Type ).WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                indexer.ExplicitInterfaceImplementations.Count > 0
                    ? ExplicitInterfaceSpecifier( (NameSyntax) syntaxGenerator.TypeSyntax( indexer.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                    : null,
                Token( SyntaxKind.ThisKeyword ),
                context.SyntaxGenerator.ParameterList( indexer, context.FinalCompilation ),
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
            switch (indexer.Writeability, indexer.GetMethod, indexer.SetMethod)
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

            if ( indexer.GetMethod!.Accessibility != indexer.Accessibility )
            {
                indexer.GetMethod.Accessibility.AddTokens( tokens );
            }

            return
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( indexer.GetMethod, context ),
                    TokenList( tokens ),
                    Token( SyntaxKind.GetKeyword ),
                    syntaxGenerator.FormattedBlock(
                        ReturnStatement(
                            Token( TriviaList(), SyntaxKind.ReturnKeyword, TriviaList( ElasticSpace ) ),
                            DefaultExpression( syntaxGenerator.TypeSyntax( indexer.Type ) ),
                            Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) ),
                    null,
                    default );
        }

        AccessorDeclarationSyntax GenerateSetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( indexer.SetMethod!.Accessibility != indexer.Accessibility )
            {
                indexer.SetMethod.Accessibility.AddTokens( tokens );
            }

            return
                AccessorDeclaration(
                    this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( indexer.SetMethod, context ),
                    TokenList( tokens ),
                    this.BuilderData.HasInitOnlySetter
                        ? Token( TriviaList(), SyntaxKind.InitKeyword, TriviaList( ElasticSpace ) )
                        : Token( TriviaList(), SyntaxKind.SetKeyword, TriviaList( ElasticSpace ) ),
                    context.SyntaxGenerator.FormattedBlock(),
                    null,
                    default );
        }
    }
}