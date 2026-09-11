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
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceNamedTypeTransformation : IntroduceDeclarationTransformation<NamedTypeBuilderData>
{
    public IntroduceNamedTypeTransformation( AspectLayerInstance aspectLayerInstance, NamedTypeBuilderData introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration ) { }

    public override TransformationObservability Observability => TransformationObservability.Always;

    /// <summary>
    /// Builds the base list of an enum, which is its underlying integral type. The list is omitted when that type is
    /// <c>int</c>, which the language implies.
    /// </summary>
    private static BaseListSyntax? GetEnumBaseList( INamedType introducedType, MemberInjectionContext context )
    {
        var underlyingType = introducedType.UnderlyingType;

        return underlyingType.SpecialType == SpecialType.Int32
            ? null
            : BaseList( SingletonSeparatedList<BaseTypeSyntax>( SimpleBaseType( context.SyntaxGenerator.TypeSyntax( underlyingType ) ) ) );
    }

    /// <summary>
    /// Builds the members of an enum, in the order in which the aspect added them, each with the value it was given
    /// or none when the language assigns it.
    /// </summary>
    private static IEnumerable<EnumMemberDeclarationSyntax> GetEnumMembers( INamedType introducedType, MemberInjectionContext context )
    {
        // The order is taken from the facet and not from INamedType.Fields, because the order of that collection
        // depends on which fields a previous consumer resolved by name, while the members of an enum are emitted in
        // the order in which the aspect added them.
        foreach ( var field in introducedType.Facets.Enum.AssertNotNull().Members )
        {
            var value = field.ConstantValue;

            var equalsValue =
                value is { IsInitialized: true, Value: not null }
                    ? EqualsValueClause( context.SyntaxGenerator.TypedConstant( value.Value ) )
                    : null;

            yield return
                EnumMemberDeclaration(
                    AdviceSyntaxGenerator.GetAttributeLists( field, context ),
                    default,
                    SyntaxFactoryEx.SafeIdentifier( field.Name ),
                    equalsValue );
        }
    }

    /// <summary>
    /// Gets the <c>Invoke</c> method of an introduced delegate, which carries its signature.
    /// </summary>
    private static IMethod GetInvokeMethod( INamedType introducedType ) => introducedType.Facets.Delegate.AssertNotNull().InvokeMethod;

    /// <summary>
    /// Builds the return type of a delegate declaration, including the <c>ref</c> and <c>ref readonly</c> modifiers
    /// of a return by reference, which the declaration writes before the return type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ContextualSyntaxGenerator.ReturnType</c> is not used, because it emits the type alone. Metalama does not
    /// introduce a method that returns by reference, which is why the setter of
    /// <c>ParameterBuilder.RefKind</c> refuses a return parameter for every kind but a delegate, and a delegate is
    /// therefore the one declaration that needs the modifier emitted here.
    /// </para>
    /// </remarks>
    private static TypeSyntax GetDelegateReturnType( INamedType introducedType, MemberInjectionContext context )
    {
        var returnParameter = GetInvokeMethod( introducedType ).ReturnParameter;
        var returnType = context.SyntaxGenerator.TypeSyntax( returnParameter.Type );

        return returnParameter.RefKind switch
        {
            RefKind.None => returnType,
            RefKind.Ref => RefType( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ), default, returnType ),
            RefKind.RefReadOnly => RefType(
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ),
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReadOnlyKeyword ),
                returnType ),
            _ => throw new AssertionFailedException(
                $"Unsupported reference kind '{returnParameter.RefKind}' on the return value of the delegate '{introducedType}'." )
        };
    }

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
                        GetEnumBaseList( introducedType, context ),
                        Token( SyntaxKind.OpenBraceToken ),
                        SeparatedList( GetEnumMembers( introducedType, context ) ),
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
                        GetDelegateReturnType( introducedType, context )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                        SyntaxFactoryEx.SafeIdentifier( introducedType.Name ),
                        typeArgs,
                        context.SyntaxGenerator.ParameterList( GetInvokeMethod( introducedType ), context.FinalCompilation ),
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