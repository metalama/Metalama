// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Builders;
using Metalama.Framework.Engine.CodeModel.References;
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

internal class IntroducePropertyTransformation : IntroduceMemberTransformation<PropertyBuilder>
{
    public IntroducePropertyTransformation( Advice advice, PropertyBuilder introducedDeclaration ) : base( advice, introducedDeclaration ) { }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var propertyBuilder = this.IntroducedDeclaration;
        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

        // TODO: What if non-auto property has the initializer template?

        // If template fails to expand, we will still generate the field, albeit without the initializer.
        _ = propertyBuilder.GetPropertyInitializerExpressionOrMethod( this.ParentAdvice, context, out var initializerExpression, out var initializerMethod );

        // TODO: This should be handled by the linker.
        // If we are introducing a field into a struct in C# 10, it must have an explicit default value.
        if ( initializerExpression == null
             && propertyBuilder is { IsAutoPropertyOrField: true, DeclaringType.TypeKind: TypeKind.Struct or TypeKind.RecordStruct }
             && context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            initializerExpression = SyntaxFactoryEx.Default;
        }

        // TODO: Creating the ref to get attributes is a temporary fix for promoted field until there is a correct injection context that has compilation that includes the builder.
        //       now the reference to promoted field is resolved to the original field, which has incorrect attributes.
        var property =
            PropertyDeclaration(
                propertyBuilder.GetAttributeLists( context, Ref.FromBuilder( this.IntroducedDeclaration ) ).AddRange( GetAdditionalAttributeLists() ),
                propertyBuilder.GetSyntaxModifierList(),
                syntaxGenerator.TypeSyntax( propertyBuilder.Type ).WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                propertyBuilder.ExplicitInterfaceImplementations.Count > 0
                    ? ExplicitInterfaceSpecifier(
                        (NameSyntax) syntaxGenerator.TypeSyntax( propertyBuilder.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                    : null,
                Identifier( propertyBuilder.GetCleanName() ),
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
            this.ParentAdvice.AspectLayerId,
            InjectedMemberSemantic.Introduction,
            propertyBuilder );

        var introducedInitializerMethod =
            initializerMethod != null
                ? new InjectedMember(
                    this,
                    initializerMethod,
                    this.ParentAdvice.AspectLayerId,
                    InjectedMemberSemantic.InitializerMethod,
                    propertyBuilder )
                : null;

        if ( introducedInitializerMethod != null )
        {
            return new[] { introducedProperty, introducedInitializerMethod };
        }
        else
        {
            return new[] { introducedProperty };
        }

        AccessorListSyntax GenerateAccessorList()
        {
            switch (propertyBuilder.IsAutoPropertyOrField, propertyBuilder.Writeability, propertyBuilder.GetMethod, propertyBuilder.SetMethod)
            {
                // Properties with both accessors.
                case (false, _, not null, not null):
                // Writeable fields.
                case (true, Writeability.All, { IsImplicitlyDeclared: true }, { IsImplicitlyDeclared: true }):
                // Auto-properties with both accessors.
                case (true, Writeability.All or Writeability.InitOnly, { IsImplicitlyDeclared: false }, { IsImplicitlyDeclared: _ }):
                    return AccessorList( List( new[] { GenerateGetAccessor(), GenerateSetAccessor() } ) );

                // Init only fields.
                case (true, Writeability.InitOnly, { IsImplicitlyDeclared: true }, { IsImplicitlyDeclared: true }):
                    return AccessorList( List( new[] { GenerateGetAccessor(), GenerateSetAccessor() } ) );

                // Properties with only get accessor.
                case (false, _, not null, null):
                // Read only fields or get-only auto properties.
                case (true, Writeability.ConstructorOnly, not null, { IsImplicitlyDeclared: true }):
                    return AccessorList( List( new[] { GenerateGetAccessor() } ) );

                // Properties with only set accessor.
                case (_, _, null, not null):
                    return AccessorList( List( new[] { GenerateSetAccessor() } ) );

                default:
                    throw new AssertionFailedException( "Both the getter and the setter are undefined." );
            }
        }

        AccessorDeclarationSyntax GenerateGetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( propertyBuilder.GetMethod!.Accessibility != propertyBuilder.Accessibility )
            {
                propertyBuilder.GetMethod.Accessibility.AddTokens( tokens );
            }

            return
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    propertyBuilder.GetAttributeLists( context, propertyBuilder.GetMethod ),
                    TokenList( tokens ),
                    Token( SyntaxKind.GetKeyword ),
                    propertyBuilder.IsAutoPropertyOrField
                        ? null
                        : syntaxGenerator.FormattedBlock(
                            ReturnStatement(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                syntaxGenerator.SuppressNullableWarningExpression(
                                    syntaxGenerator.DefaultExpression( propertyBuilder.Type ),
                                    propertyBuilder.Type.IsReferenceType == false
                                        ? propertyBuilder.Type
                                        : propertyBuilder.Type.ToNullableType() ),
                                Token( TriviaList(), SyntaxKind.SemicolonToken, context.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) ),
                    null,
                    propertyBuilder.IsAutoPropertyOrField ? Token( SyntaxKind.SemicolonToken ) : default );
        }

        AccessorDeclarationSyntax GenerateSetAccessor()
        {
            var tokens = new List<SyntaxToken>();

            if ( propertyBuilder.SetMethod!.Accessibility != propertyBuilder.Accessibility )
            {
                propertyBuilder.SetMethod.Accessibility.AddTokens( tokens );
            }

            return
                AccessorDeclaration(
                    propertyBuilder.HasInitOnlySetter ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    propertyBuilder.GetAttributeLists( context, propertyBuilder.SetMethod ),
                    TokenList( tokens ),
                    propertyBuilder.HasInitOnlySetter
                        ? Token( TriviaList(), SyntaxKind.InitKeyword, TriviaList( ElasticSpace ) )
                        : Token( TriviaList(), SyntaxKind.SetKeyword, TriviaList( ElasticSpace ) ),
                    propertyBuilder.IsAutoPropertyOrField
                        ? null
                        : syntaxGenerator.FormattedBlock(),
                    null,
                    propertyBuilder.IsAutoPropertyOrField ? Token( SyntaxKind.SemicolonToken ) : default );
        }

        IEnumerable<AttributeListSyntax> GetAdditionalAttributeLists()
        {
            var attributes = new List<AttributeListSyntax>();

            foreach ( var attribute in propertyBuilder.FieldAttributes )
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