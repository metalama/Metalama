// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal class IntroducePropertyTransformation : IntroduceMemberTransformation<PropertyBuilderData>
{
    private readonly TemplateMember<IProperty>? _template;

    public IntroducePropertyTransformation(
        AspectLayerInstance aspectLayerInstance,
        PropertyBuilderData introducedDeclaration,
        TemplateMember<IProperty>? template ) : base(
        aspectLayerInstance,
        introducedDeclaration )
    {
        this._template = template;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var finalProperty = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );
        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

        // TODO: What if non-auto property has the initializer template?

        // If template fails to expand, we will still generate the field, albeit without the initializer.

        _ = AdviceSyntaxGenerator.GetInitializerExpressionOrMethod(
            finalProperty,
            this.AspectLayerInstance,
            context,
            finalProperty.Type,
            this.BuilderData.InitializerExpression,
            this._template?.GetInitializerTemplate()?.As<IFieldOrProperty>() ?? this.BuilderData.InitializerTemplate,
            out var initializerExpression,
            out var initializerMethod );

        // TODO: This should be handled by the linker.
        // If we are introducing a field into a struct in C# 10, it must have an explicit default value.
        if ( initializerExpression == null
             && finalProperty is { IsAutoPropertyOrField: true, DeclaringType.TypeKind: TypeKind.Struct }
             && context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            initializerExpression = SyntaxFactoryEx.Default;
        }

        var hasNoBody = finalProperty.IsAutoPropertyOrField is true || finalProperty.IsAbstract || finalProperty.IsPartial || finalProperty.IsExtern;

        // TODO: Creating the ref to get attributes is a temporary fix for promoted field until there is a correct injection context that has compilation that includes the builder.
        //       now the reference to promoted field is resolved to the original field, which has incorrect attributes.
        var property =
            PropertyDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( finalProperty, context )
                    .AddRange( GetAdditionalAttributeLists() ),
                finalProperty.GetSyntaxModifierList(),
                syntaxGenerator.TypeSyntax( finalProperty.Type ).WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                finalProperty.ExplicitInterfaceImplementations.Count > 0
                    ? ExplicitInterfaceSpecifier(
                        (NameSyntax) syntaxGenerator.TypeSyntax( finalProperty.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                    : null,
                SyntaxFactoryEx.SafeIdentifier( finalProperty.GetCleanName() ),
                GenerateAccessorList(),
                null,
                initializerExpression != null
                    ? EqualsValueClause( initializerExpression )
                    : null,
                initializerExpression != null
                    ? Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList )
                    : default );

        var introducedProperty = new InjectedMember(
            this,
            property,
            this.AspectLayerId,
            InjectedMemberSemantic.Introduction,
            this.BuilderData.ToRef() );

        var introducedInitializerMethod =
            initializerMethod != null
                ? new InjectedMember(
                    this,
                    initializerMethod,
                    this.AspectLayerId,
                    InjectedMemberSemantic.InitializerMethod,
                    this.BuilderData.ToRef() )
                : null;

        if ( introducedInitializerMethod != null )
        {
            return [introducedProperty, introducedInitializerMethod];
        }
        else
        {
            return [introducedProperty];
        }

        AccessorListSyntax GenerateAccessorList()
        {
            switch (finalProperty.IsAutoPropertyOrField, finalProperty.Writeability, finalProperty.GetMethod, finalProperty.SetMethod)
            {
                // Properties with both accessors.
                case (false, _, not null, not null):
                // Writeable fields.
                case (true, Writeability.All, { IsImplicitlyDeclared: true }, { IsImplicitlyDeclared: true }):
                // Auto-properties with both accessors.
                case (true, Writeability.All or Writeability.InitOnly, { IsImplicitlyDeclared: false }, { IsImplicitlyDeclared: _ }):
                    return SyntaxFactoryEx.FormattedAccessorList(
                        [GenerateGetAccessor(), GenerateSetAccessor()],
                        context.SyntaxGenerationContext );

                // Init only fields.
                case (true, Writeability.InitOnly, { IsImplicitlyDeclared: true }, { IsImplicitlyDeclared: true }):
                    return SyntaxFactoryEx.FormattedAccessorList(
                        [GenerateGetAccessor(), GenerateSetAccessor()],
                        context.SyntaxGenerationContext );

                // Properties with only get accessor.
                case (false, _, not null, null):
                // Read only fields or get-only auto properties.
                case (true, Writeability.ConstructorOnly, not null, { IsImplicitlyDeclared: true }):
                    return SyntaxFactoryEx.FormattedAccessorList( [GenerateGetAccessor()], context.SyntaxGenerationContext );

                // Properties with only set accessor.
                case (_, _, null, not null):
                    return SyntaxFactoryEx.FormattedAccessorList( [GenerateSetAccessor()], context.SyntaxGenerationContext );

                default:
                    throw new AssertionFailedException( "Both the getter and the setter are undefined." );
            }
        }

        AccessorDeclarationSyntax GenerateGetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( finalProperty.GetMethod!.Accessibility != finalProperty.Accessibility )
            {
                finalProperty.GetMethod.Accessibility.AddTokens( tokens );
            }

            return
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalProperty.GetMethod, context ),
                    TokenList( tokens ),
                    Token( SyntaxKind.GetKeyword ),
                    hasNoBody
                        ? null
                        : syntaxGenerator.FormattedBlock(
                            ReturnStatement(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                syntaxGenerator.SuppressNullableWarningExpression(
                                    syntaxGenerator.DefaultExpression( finalProperty.Type ),
                                    finalProperty.Type.IsReferenceType == false
                                        ? finalProperty.Type
                                        : finalProperty.Type.ToNullable(),
                                    finalProperty.Type ),
                                Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) ) ),
                    null,
                    hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );
        }

        AccessorDeclarationSyntax GenerateSetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( finalProperty.SetMethod!.Accessibility != finalProperty.Accessibility )
            {
                finalProperty.SetMethod.Accessibility.AddTokens( tokens );
            }

            // The keyword needs a trailing space only when followed by a body block ("set { ... }").
            // For bodyless accessors ("set;"), no trailing trivia is emitted, so we don't get "set ;".
            var keywordKind = this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword;
            var keyword = hasNoBody
                ? Token( keywordKind )
                : Token( default, keywordKind, TriviaList( ElasticSpace ) );

            return
                AccessorDeclaration(
                    this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalProperty.SetMethod, context ),
                    TokenList( tokens ),
                    keyword,
                    hasNoBody ? null : syntaxGenerator.FormattedBlock(),
                    null,
                    hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );
        }

        IEnumerable<AttributeListSyntax> GetAdditionalAttributeLists()
        {
            var attributes = new List<AttributeListSyntax>();

            foreach ( var attribute in this.BuilderData.FieldAttributes )
            {
                attributes.Add(
                    AttributeList(
                        AttributeTargetSpecifier( Token( SyntaxKind.FieldKeyword ) ),
                        SingletonSeparatedList( context.SyntaxGenerator.Attribute( attribute ) ) ) );
            }

            return List( attributes );
        }
    }

#if ROSLYN_5_0_0_OR_GREATER
    /// <inheritdoc />
    public override IEnumerable<DeclarationBuilderData> GetImplicitDeclarations()
    {
        // Check if the property is in an extension block.
        var containingDeclaration = this.BuilderData.ContainingDeclaration.GetTarget( this.InitialCompilation );

        if ( containingDeclaration is not IExtensionBlock extensionBlock )
        {
            return [];
        }

        var result = new List<DeclarationBuilderData>( 2 );
        var propertyName = this.BuilderData.Name;
        var propertyType = this.BuilderData.Type.GetTarget( this.InitialCompilation );

        // Create implicit method for getter if it exists.
        if ( this.BuilderData.GetMethod != null )
        {
            var getterData = ExtensionImplementationHelper.CreateImplicitAccessorMethod(
                this.AspectLayerInstance,
                extensionBlock,
                propertyName,
                isSetter: false,
                this.BuilderData.GetMethod.Accessibility,
                this.BuilderData.IsStatic,
                propertyType,
                this.BuilderData.RefKind,
                this.InitialCompilation,
                this.BuilderData.GetMethod.Attributes,
                this.BuilderData.GetMethod.ReturnParameter.Attributes );

            result.Add( getterData );
        }

        // Create implicit method for setter if it exists.
        if ( this.BuilderData.SetMethod != null )
        {
            var setterData = ExtensionImplementationHelper.CreateImplicitAccessorMethod(
                this.AspectLayerInstance,
                extensionBlock,
                propertyName,
                isSetter: true,
                this.BuilderData.SetMethod.Accessibility,
                this.BuilderData.IsStatic,
                propertyType,
                this.BuilderData.RefKind,
                this.InitialCompilation,
                this.BuilderData.SetMethod.Attributes,
                this.BuilderData.SetMethod.ReturnParameter.Attributes );

            result.Add( setterData );
        }

        return result;
    }
#endif
}