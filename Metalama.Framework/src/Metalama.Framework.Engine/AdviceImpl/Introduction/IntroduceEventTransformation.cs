// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceEventTransformation : IntroduceMemberTransformation<EventBuilderData>
{
    private readonly TemplateMember<IEvent>? _template;

    public IntroduceEventTransformation(
        AspectLayerInstance aspectLayerInstance,
        EventBuilderData introducedDeclaration,
        TemplateMember<IEvent>? template ) : base(
        aspectLayerInstance,
        introducedDeclaration )
    {
        this._template = template;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;
        var finalEvent = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        _ = AdviceSyntaxGenerator.GetInitializerExpressionOrMethod(
            finalEvent,
            this.AspectLayerInstance,
            context,
            finalEvent.Type,
            this.BuilderData.InitializerExpression,
            this._template?.GetInitializerTemplate(),
            out var initializerExpression,
            out var initializerMethod );

        var isEventField = this.BuilderData.IsEventField;
        Invariant.Assert( !(isEventField == false && initializerExpression != null) );

        // TODO: This should be handled by the linker.
        // If we are introducing a field into a struct in C# 10, it must have an explicit default value.
        if ( initializerExpression == null
             && isEventField
             && finalEvent is { DeclaringType.TypeKind: TypeKind.Struct or TypeKind.RecordStruct }
             && context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            initializerExpression = SyntaxFactoryEx.Default;
        }

        var hasNoBody = isEventField || finalEvent.IsAbstract || this._template?.TemplateClassMember.TemplateInfo.HasNoBody == true;

        // TODO: If the user adds (different) attributes to event field's accessors, we cannot use event fields.

        MemberDeclarationSyntax @event =
            hasNoBody && finalEvent is { ExplicitInterfaceImplementations.Count: 0 }
                ? EventFieldDeclaration(
                    AdviceSyntaxGenerator.GetAttributeLists( finalEvent, context ).AddRange( GetAdditionalAttributeListsForEventField() ),
                    finalEvent.GetSyntaxModifierList(),
                    Token( TriviaList(), SyntaxKind.EventKeyword, TriviaList( ElasticSpace ) ),
                    VariableDeclaration(
                        syntaxGenerator.TypeSyntax( finalEvent.Type )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                        SeparatedList(
                        [
                            VariableDeclarator(
                                Identifier( TriviaList(), finalEvent.Name, TriviaList( ElasticSpace ) ),
                                null,
                                initializerExpression != null
                                    ? EqualsValueClause( initializerExpression )
                                    : null ) // TODO: Initializer.
                        ] ) ),
                    Token( SyntaxKind.SemicolonToken ) )
                : EventDeclaration(
                    AdviceSyntaxGenerator.GetAttributeLists( finalEvent, context ),
                    finalEvent.GetSyntaxModifierList(),
                    Token( TriviaList(), SyntaxKind.EventKeyword, TriviaList( ElasticSpace ) ),
                    syntaxGenerator.TypeSyntax( finalEvent.Type )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    finalEvent.ExplicitInterfaceImplementations.Count > 0
                        ? ExplicitInterfaceSpecifier(
                                (NameSyntax) syntaxGenerator.TypeSyntax( finalEvent.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options )
                        : null,
                    Identifier(finalEvent.GetCleanName() ),
                    GenerateAccessorList(),
                    default );

        if ( isEventField && finalEvent is { ExplicitInterfaceImplementations.Count: > 0 } )
        {
            // Add annotation to the explicit annotation that the linker should treat this an event field.
            if ( initializerExpression != null )
            {
                @event = @event.WithLinkerDeclarationFlags(
                    AspectLinkerDeclarationFlags.EventField | AspectLinkerDeclarationFlags.HasHiddenInitializerExpression );
            }
            else
            {
                @event = @event.WithLinkerDeclarationFlags( AspectLinkerDeclarationFlags.EventField );
            }
        }

        if ( initializerMethod != null )
        {
            return
            [
                new InjectedMember( this, @event, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() ),
                new InjectedMember(
                    this,
                    initializerMethod,
                    this.AspectLayerId,
                    InjectedMemberSemantic.InitializerMethod,
                    this.BuilderData.ToRef() )
            ];
        }
        else
        {
            return [new InjectedMember( this, @event, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
        }

        AccessorListSyntax GenerateAccessorList()
        {
            switch (Adder: finalEvent.AddMethod, Remover: finalEvent.RemoveMethod)
            {
                case (not null, not null):
                    return AccessorList(
                        List(
                        [
                            GenerateAccessor( finalEvent.AddMethod, SyntaxKind.AddAccessorDeclaration ),
                            GenerateAccessor( finalEvent.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration )
                        ] ) );

                case (not null, null):
                    return AccessorList( List( [GenerateAccessor( finalEvent.AddMethod, SyntaxKind.AddAccessorDeclaration )] ) );

                case (null, not null):
                    return AccessorList( List( [GenerateAccessor( finalEvent.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration )] ) );

                default:
                    throw new AssertionFailedException( "Both accessors are null." );
            }
        }

        AccessorDeclarationSyntax GenerateAccessor( IMethod accessor, SyntaxKind accessorDeclarationKind )
        {
            var attributes = AdviceSyntaxGenerator.GetAttributeLists( accessor, context );

            var block =
                accessor switch
                {
                    // Special case - explicit interface implementation event field with initialized.
                    // Hide initializer expression into the single statement of the add.
                    { MethodKind: MethodKind.EventAdd } when isEventField && finalEvent is { ExplicitInterfaceImplementations.Count: > 0 }
                                                                          && initializerExpression != null
                        => context.SyntaxGenerator.FormattedBlock(
                            ExpressionStatement(
                                context.AspectReferenceSyntaxProvider.GetEventFieldInitializerExpression(
                                    syntaxGenerator.TypeSyntax( finalEvent.Type ),
                                    initializerExpression ) ) ),
                    _ => context.SyntaxGenerator.FormattedBlock()
                };

            return
                AccessorDeclaration(
                    accessorDeclarationKind,
                    attributes,
                    TokenList(),
                    block,
                    null );
        }

        IEnumerable<AttributeListSyntax> GetAdditionalAttributeListsForEventField()
        {
            var attributes = new List<AttributeListSyntax>();

            foreach ( var attribute in this.BuilderData.FieldAttributes )
            {
                attributes.Add(
                    AttributeList(
                        AttributeTargetSpecifier( Token( SyntaxKind.FieldKeyword ) ),
                        SingletonSeparatedList( context.SyntaxGenerator.Attribute( attribute ) ) ) );
            }

            // Here we take attributes only for add method because we presume it's the same.

            foreach ( var attribute in finalEvent.AddMethod.Attributes )
            {
                attributes.Add(
                    AttributeList(
                        AttributeTargetSpecifier( Token( SyntaxKind.MethodKeyword ) ),
                        SingletonSeparatedList( context.SyntaxGenerator.Attribute( attribute ) ) ) );
            }

            return List( attributes );
        }
    }
}