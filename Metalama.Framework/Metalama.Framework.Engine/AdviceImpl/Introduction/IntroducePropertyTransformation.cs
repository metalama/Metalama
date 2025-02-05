// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
             && finalProperty is { IsAutoPropertyOrField: true, DeclaringType.TypeKind: TypeKind.Struct or TypeKind.RecordStruct }
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
                    ? ExplicitInterfaceSpecifier( (NameSyntax) syntaxGenerator.TypeSyntax( finalProperty.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                    : null,
                finalProperty.GetCleanName(),
                GenerateAccessorList(),
                null,
                initializerExpression != null
                    ? EqualsValueClause( initializerExpression )
                    : null,
                initializerExpression != null
                    ? Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.ElasticEndOfLineTriviaList )
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
                    return AccessorList( List( [GenerateGetAccessor(), GenerateSetAccessor()] ) );

                // Init only fields.
                case (true, Writeability.InitOnly, { IsImplicitlyDeclared: true }, { IsImplicitlyDeclared: true }):
                    return AccessorList( List( [GenerateGetAccessor(), GenerateSetAccessor()] ) );

                // Properties with only get accessor.
                case (false, _, not null, null):
                // Read only fields or get-only auto properties.
                case (true, Writeability.ConstructorOnly, not null, { IsImplicitlyDeclared: true }):
                    return AccessorList( List( [GenerateGetAccessor()] ) );

                // Properties with only set accessor.
                case (_, _, null, not null):
                    return AccessorList( List( [GenerateSetAccessor()] ) );

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
                                        : finalProperty.Type.ToNullable() ),
                                Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) ),
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

            return
                AccessorDeclaration(
                    this.BuilderData.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    AdviceSyntaxGenerator.GetAttributeLists( finalProperty.SetMethod, context ),
                    TokenList( tokens ),
                    this.BuilderData.HasInitOnlySetter
                        ? Token( TriviaList(), SyntaxKind.InitKeyword, TriviaList( ElasticSpace ) )
                        : Token( TriviaList(), SyntaxKind.SetKeyword, TriviaList( ElasticSpace ) ),
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
}