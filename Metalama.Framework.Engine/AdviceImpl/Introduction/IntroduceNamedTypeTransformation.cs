// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceNamedTypeTransformation : IntroduceDeclarationTransformation<NamedTypeBuilderData>
{
    public IntroduceNamedTypeTransformation( AspectLayerInstance aspectLayerInstance, NamedTypeBuilderData introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration ) { }

    public override TransformationObservability Observability => TransformationObservability.Always;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var typeBuilder = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        BaseListSyntax? baseList;

        if ( typeBuilder.BaseType != null && typeBuilder.BaseType.SpecialType != SpecialType.Object )
        {
            baseList = BaseList(
                SingletonSeparatedList<BaseTypeSyntax>( SimpleBaseType( context.SyntaxGenerator.TypeSyntax( typeBuilder.BaseType.ToNonNullable() ) ) ) );
        }
        else
        {
            baseList = null;
        }

        var type =
            ClassDeclaration(
                    AdviceSyntaxGenerator.GetAttributeLists( typeBuilder, context ),
                    typeBuilder.GetSyntaxModifierList(),
                    Identifier( typeBuilder.Name ),
                    typeBuilder.TypeParameters.Count == 0
                        ? null
                        : TypeParameterList( SeparatedList( typeBuilder.TypeParameters.SelectAsReadOnlyList( tp => TypeParameter( Identifier( tp.Name ) ) ) ) ),
                    baseList,
                    List<TypeParameterConstraintClauseSyntax>(),
                    List<MemberDeclarationSyntax>() )
                .NormalizeWhitespaceIfNecessary( context.SyntaxGenerationContext );

        switch ( typeBuilder.ContainingDeclaration )
        {
            case INamedType:
            case INamespace { IsGlobalNamespace: true }:
                return [new InjectedMember( this, type, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];

            case INamespace:
                var namespaceDeclaration =
                    NamespaceDeclaration(
                        Token( TriviaList(), SyntaxKind.NamespaceKeyword, TriviaList( ElasticSpace ) ),
                        ParseName( typeBuilder.ContainingNamespace.FullName ),
                        Token( TriviaList(), SyntaxKind.OpenBraceToken, TriviaList( context.SyntaxGenerationContext.ElasticEndOfLineTrivia ) ),
                        List<ExternAliasDirectiveSyntax>(),
                        List<UsingDirectiveSyntax>(),
                        SingletonList<MemberDeclarationSyntax>( type ),
                        Token( TriviaList( context.SyntaxGenerationContext.ElasticEndOfLineTrivia ), SyntaxKind.CloseBraceToken, TriviaList() ),
                        default );

                return
                [
                    new InjectedMember(
                        this,
                        namespaceDeclaration,
                        this.AspectLayerId,
                        InjectedMemberSemantic.Introduction,
                        this.BuilderData.ToRef() )
                ];

            default:
                throw new AssertionFailedException(
                    $"Unsupported containing declaration type '{typeBuilder.ContainingDeclaration.AssertNotNull().GetType()}'." );
        }
    }
}