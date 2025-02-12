// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Builders;
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

internal sealed class IntroduceEventTransformation : IntroduceMemberTransformation<EventBuilder>
{
    public IntroduceEventTransformation( Advice advice, EventBuilder introducedDeclaration ) : base( advice, introducedDeclaration ) { }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;
        var eventBuilder = this.IntroducedDeclaration;

        _ = eventBuilder.GetInitializerExpressionOrMethod(
            this.ParentAdvice,
            context,
            eventBuilder.Type,
            eventBuilder.InitializerExpression,
            eventBuilder.InitializerTemplate,
            eventBuilder.InitializerTags,
            out var initializerExpression,
            out var initializerMethod );

        Invariant.Assert( !(!eventBuilder.IsEventField && initializerExpression != null) );

        // TODO: This should be handled by the linker.
        // If we are introducing a field into a struct in C# 10, it must have an explicit default value.
        if ( initializerExpression == null
             && eventBuilder is { IsEventField: true, DeclaringType.TypeKind: TypeKind.Struct or TypeKind.RecordStruct }
             && context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            initializerExpression = SyntaxFactoryEx.Default;
        }

        // TODO: If the user adds (different) attributes to event field's accessors, we cannot use event fields.

        MemberDeclarationSyntax @event =
            eventBuilder is { IsEventField: true, ExplicitInterfaceImplementations.Count: 0 }
                ? EventFieldDeclaration(
                    eventBuilder.GetAttributeLists( context ).AddRange( GetAdditionalAttributeListsForEventField() ),
                    eventBuilder.GetSyntaxModifierList(),
                    Token( TriviaList(), SyntaxKind.EventKeyword, TriviaList( ElasticSpace ) ),
                    VariableDeclaration(
                        syntaxGenerator.TypeSyntax( eventBuilder.Type )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                        SeparatedList(
                            new[]
                            {
                                VariableDeclarator(
                                    Identifier( TriviaList(), eventBuilder.Name, TriviaList( ElasticSpace ) ),
                                    null,
                                    initializerExpression != null
                                        ? EqualsValueClause( initializerExpression )
                                        : null ) // TODO: Initializer.
                            } ) ),
                    Token( SyntaxKind.SemicolonToken ) )
                : EventDeclaration(
                    eventBuilder.GetAttributeLists( context ),
                    eventBuilder.GetSyntaxModifierList(),
                    Token( TriviaList(), SyntaxKind.EventKeyword, TriviaList( ElasticSpace ) ),
                    syntaxGenerator.TypeSyntax( eventBuilder.Type )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    eventBuilder.ExplicitInterfaceImplementations.Count > 0
                        ? ExplicitInterfaceSpecifier(
                                (NameSyntax) syntaxGenerator.TypeSyntax( eventBuilder.ExplicitInterfaceImplementations.Single().DeclaringType ) )
                            .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options )
                        : null,
                    Identifier( this.IntroducedDeclaration.GetCleanName() ),
                    GenerateAccessorList(),
                    default );

        if ( eventBuilder is { IsEventField: true, ExplicitInterfaceImplementations.Count: > 0 } )
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
            return new[]
            {
                new InjectedMember( this, @event, this.ParentAdvice.AspectLayerId, InjectedMemberSemantic.Introduction, eventBuilder ),
                new InjectedMember(
                    this,
                    initializerMethod,
                    this.ParentAdvice.AspectLayerId,
                    InjectedMemberSemantic.InitializerMethod,
                    eventBuilder )
            };
        }
        else
        {
            return new[] { new InjectedMember( this, @event, this.ParentAdvice.AspectLayerId, InjectedMemberSemantic.Introduction, eventBuilder ) };
        }

        AccessorListSyntax GenerateAccessorList()
        {
            switch (Adder: eventBuilder.AddMethod, Remover: eventBuilder.RemoveMethod)
            {
                case (not null, not null):
                    return AccessorList(
                        List(
                            new[]
                            {
                                GenerateAccessor( eventBuilder.AddMethod, SyntaxKind.AddAccessorDeclaration ),
                                GenerateAccessor( eventBuilder.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration )
                            } ) );

                case (not null, null):
                    return AccessorList( List( new[] { GenerateAccessor( eventBuilder.AddMethod, SyntaxKind.AddAccessorDeclaration ) } ) );

                case (null, not null):
                    return AccessorList( List( new[] { GenerateAccessor( eventBuilder.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration ) } ) );

                default:
                    throw new AssertionFailedException( "Both accessors are null." );
            }
        }

        AccessorDeclarationSyntax GenerateAccessor( IMethod accessor, SyntaxKind accessorDeclarationKind )
        {
            var attributes = eventBuilder.GetAttributeLists( context, accessor );

            var block =
                accessor switch
                {
                    // Special case - explicit interface implementation event field with initialized.
                    // Hide initializer expression into the single statement of the add.
                    { MethodKind: MethodKind.EventAdd } when eventBuilder is { IsEventField: true, ExplicitInterfaceImplementations.Count: > 0 }
                                                             && initializerExpression != null
                        => context.SyntaxGenerator.FormattedBlock(
                            ExpressionStatement(
                                context.AspectReferenceSyntaxProvider.GetEventFieldInitializerExpression(
                                    syntaxGenerator.TypeSyntax( eventBuilder.Type ),
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

            foreach ( var attribute in eventBuilder.FieldAttributes )
            {
                attributes.Add(
                    AttributeList(
                        AttributeTargetSpecifier( Token( SyntaxKind.FieldKeyword ) ),
                        SingletonSeparatedList( context.SyntaxGenerator.Attribute( attribute ) ) ) );
            }

            // Here we take attributes only for add method because we presume it's the same.

            foreach ( var attribute in eventBuilder.AddMethod.Attributes )
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