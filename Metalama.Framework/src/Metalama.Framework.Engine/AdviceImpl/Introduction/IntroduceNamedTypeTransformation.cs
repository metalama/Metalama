// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        var introducedType = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        BaseListSyntax? baseList;

        if ( introducedType.BaseType != null && introducedType.BaseType.SpecialType != SpecialType.Object )
        {
            baseList = BaseList(
                SingletonSeparatedList<BaseTypeSyntax>( SimpleBaseType( context.SyntaxGenerator.TypeSyntax( introducedType.BaseType.ToNonNullable() ) ) ) );
        }
        else
        {
            baseList = null;
        }

        var typeArgs =
            introducedType.TypeParameters.Count == 0
                ? null
                : TypeParameterList(
                    SeparatedList(
                        introducedType.TypeParameters.SelectAsReadOnlyList(
                            tp => TypeParameter(
                                List<AttributeListSyntax>(),
                                tp.Variance switch
                                {
                                    VarianceKind.In => Token( SyntaxKind.InKeyword ),
                                    VarianceKind.Out => Token( SyntaxKind.OutKeyword ),
                                    _ => default
                                },
                                Identifier( tp.Name ) ) ) ) );

        var type =
            (this.BuilderData.TypeKind switch
            {
                TypeKind.Class =>
                    (TypeDeclarationSyntax) ClassDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        Identifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),
                TypeKind.Struct =>
                    StructDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        Identifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),
                TypeKind.Interface =>
                    InterfaceDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        Identifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),
                _ => throw new AssertionFailedException( $"Unsupported type kind '{introducedType.TypeKind}'." )
            }).NormalizeWhitespaceIfNecessary( context.SyntaxGenerationContext );

        switch ( introducedType.ContainingDeclaration )
        {
            case INamedType:
            case INamespace { IsGlobalNamespace: true }:
                return [new InjectedMember( this, type, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];

            case INamespace:
                var namespaceDeclaration =
                    NamespaceDeclaration(
                        Token( TriviaList(), SyntaxKind.NamespaceKeyword, TriviaList( ElasticSpace ) ),
                        ParseName( introducedType.ContainingNamespace.FullName ),
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
                    $"Unsupported containing declaration type '{introducedType.ContainingDeclaration.AssertNotNull().GetType()}'." );
        }
    }
}