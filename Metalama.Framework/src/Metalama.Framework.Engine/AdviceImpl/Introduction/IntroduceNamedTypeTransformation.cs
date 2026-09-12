// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.SyntaxGeneration;
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

        // A struct, an enum and a delegate emit no base list. The base that InitializeBaseType gives them, which is
        // System.ValueType, System.Enum and System.MulticastDelegate, is the semantic base that the code model
        // reports, and writing it in a base list is CS0527: the compiler derives it from the declaration instead.
        // An enum writes its underlying integral type in that position, which the arm for that kind supplies.
        BaseListSyntax? baseList;

        if ( this.BuilderData.TypeKind is TypeKind.Struct or TypeKind.Enum or TypeKind.Delegate )
        {
            baseList = null;
        }
        else if ( introducedType.BaseType != null && introducedType.BaseType.SpecialType != SpecialType.Object )
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
                                SyntaxFactoryEx.SafeIdentifier( tp.Name ) ) ) ) );

        // The local is a MemberDeclarationSyntax and not a TypeDeclarationSyntax, because an enum declaration is a
        // BaseTypeDeclarationSyntax and a delegate declaration is neither. InjectedMember.Syntax is already typed that
        // way, so nothing downstream changes.
        var type =
            (this.BuilderData.TypeKind switch
            {
                // A record is emitted as a record declaration whose class or struct keyword names the authoring
                // form. Its positional parameter list is the parameter list of the primary constructor, and every
                // member the compiler synthesizes from it is registered in the code model and emitted by nothing.
#if ROSLYN_5_11_0_OR_GREATER

                // A union declaration is a struct in the code model, so the union flag tells it apart. The emission
                // is compiled into the latest Roslyn variant only, because that is the one that declares the syntax.
                TypeKind.Struct when this.BuilderData.IsUnion =>
                    UnionDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.UnionKeyword ),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        UnionHelper.GetCaseList( introducedType, context ),
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        default,
                        List<MemberDeclarationSyntax>(),
                        default,
                        Token( SyntaxKind.SemicolonToken ) ),
#endif
                TypeKind.Class or TypeKind.Struct when this.BuilderData.IsRecord =>
                    RecordDeclaration(
                        introducedType.TypeKind == TypeKind.Class ? SyntaxKind.RecordDeclaration : SyntaxKind.RecordStructDeclaration,
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RecordKeyword ),
                        introducedType.TypeKind == TypeKind.Class
                            ? default
                            : SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StructKeyword ),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        RecordHelper.GetParameterList( introducedType, context ),
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        Token( SyntaxKind.OpenBraceToken ),
                        List<MemberDeclarationSyntax>(),
                        Token( SyntaxKind.CloseBraceToken ),
                        default ),
                TypeKind.Class =>
                    (MemberDeclarationSyntax) ClassDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),
                TypeKind.Struct =>
                    StructDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),
                TypeKind.Interface =>
                    InterfaceDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        baseList,
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        List<MemberDeclarationSyntax>() ),

                // The members of an enum are part of the declaration, so they are emitted here rather than injected
                // separately. The underlying type takes the place of the base list, which GetEnumBaseList supplies.
                TypeKind.Enum =>
                    EnumDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        Token( SyntaxKind.EnumKeyword ),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        EnumHelper.GetBaseList( introducedType, context ),
                        Token( SyntaxKind.OpenBraceToken ),
                        SeparatedList( EnumHelper.GetMembers( introducedType, context ) ),
                        Token( SyntaxKind.CloseBraceToken ),
                        default ),

                // A delegate declaration is a method signature with the delegate keyword in front of it. The
                // signature comes from the Invoke method, which the advice registers in the code model without
                // emitting it, because this declaration has no member list to put it in.
                TypeKind.Delegate =>
                    DelegateDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( introducedType, context ),
                        introducedType.GetSyntaxModifierList(),
                        Token( SyntaxKind.DelegateKeyword ),
                        DelegateHelper.GetReturnType( introducedType, context )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        context.SyntaxGenerator.ParameterList( DelegateHelper.GetInvokeMethod( introducedType ), context.FinalCompilation ),
                        context.SyntaxGenerator.ConstraintClauses( introducedType ),
                        Token( SyntaxKind.SemicolonToken ) ),
                _ => throw new AssertionFailedException( $"Unsupported type kind '{introducedType.TypeKind}'." )
            }).NormalizeWhitespaceIfNecessary( context.SyntaxGenerationContext );

        switch ( introducedType.ContainingDeclaration?.DeclarationKind )
        {
            case DeclarationKind.NamedType:
            case DeclarationKind.Namespace when introducedType.ContainingDeclaration is INamespace { IsGlobalNamespace: true }:
                return [new InjectedMember( this, type, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];

            case DeclarationKind.Namespace:
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