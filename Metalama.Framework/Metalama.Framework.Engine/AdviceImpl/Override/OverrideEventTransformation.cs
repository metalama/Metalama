// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideEventTransformation : OverrideMemberTransformation
{
    private readonly IFullRef<IEvent> _overriddenDeclaration;

    private BoundTemplateMethod? AddTemplate { get; }

    private BoundTemplateMethod? RemoveTemplate { get; }

    public OverrideEventTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IEvent> overriddenDeclaration,
        BoundTemplateMethod? addTemplate,
        BoundTemplateMethod? removeTemplate )
        : base( aspectLayerInstance, overriddenDeclaration )
    {
        this._overriddenDeclaration = overriddenDeclaration;
        this.AddTemplate = addTemplate;
        this.RemoveTemplate = removeTemplate;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this._overriddenDeclaration;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var overriddenDeclaration = this._overriddenDeclaration.GetTarget( this.InitialCompilation );

        var eventName = context.InjectionNameProvider.GetOverrideName(
            overriddenDeclaration.DeclaringType,
            this.AspectLayerId,
            overriddenDeclaration );

        var templateExpansionError = false;
        BlockSyntax? addAccessorBody = null;

        if ( this.AddTemplate != null )
        {
            templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                context,
                this.AddTemplate,
                overriddenDeclaration.AddMethod,
                overriddenDeclaration,
                out addAccessorBody );
        }
        else
        {
            addAccessorBody = this.CreateIdentityAccessorBody( SyntaxKind.AddAccessorDeclaration, context );
        }

        BlockSyntax? removeAccessorBody = null;

        if ( this.RemoveTemplate != null )
        {
            templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                context,
                this.RemoveTemplate,
                overriddenDeclaration.RemoveMethod,
                overriddenDeclaration,
                out removeAccessorBody );
        }
        else
        {
            removeAccessorBody = this.CreateIdentityAccessorBody( SyntaxKind.RemoveAccessorDeclaration, context );
        }

        if ( templateExpansionError )
        {
            // Template expansion error.
            return [];
        }

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        // TODO: Do not throw exception when template expansion fails.
        var overrides = new[]
        {
            new InjectedMember(
                this,
                EventDeclaration(
                    List<AttributeListSyntax>(),
                    modifiers,
                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.EventKeyword ),
                    context.SyntaxGenerator.EventType( overriddenDeclaration )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    null!,
                    Identifier( eventName ),
                    AccessorList(
                        List(
                        [
                            AccessorDeclaration(
                                SyntaxKind.AddAccessorDeclaration,
                                List<AttributeListSyntax>(),
                                overriddenDeclaration.AddMethod.AssertNotNull().GetSyntaxModifierList(),
                                addAccessorBody.AssertNotNull() ),
                            AccessorDeclaration(
                                SyntaxKind.RemoveAccessorDeclaration,
                                List<AttributeListSyntax>(),
                                overriddenDeclaration.RemoveMethod.AssertNotNull().GetSyntaxModifierList(),
                                removeAccessorBody.AssertNotNull() )
                        ] ) ) ),
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        };

        return overrides;
    }

    private bool TryExpandAccessorTemplate(
        MemberInjectionContext context,
        BoundTemplateMethod accessorTemplate,
        IMethod accessor,
        IEvent overriddenDeclaration,
        [NotNullWhen( true )] out BlockSyntax? body )
    {
        var proceedExpression = new SyntaxUserExpression(
            accessor.MethodKind switch
            {
                MethodKind.EventAdd => this.CreateAddExpression( context ),
                MethodKind.EventRemove => this.CreateRemoveExpression( context ),
                _ => throw new AssertionFailedException( $"Unexpected MethodKind: {accessor.MethodKind}." )
            },
            context.FinalCompilation.Cache.SystemVoidType );

        var metaApi = MetaApi.ForEvent(
            overriddenDeclaration,
            accessor,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                accessorTemplate.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this.AspectInstance,
                context.ServiceProvider,
                MetaApiStaticity.Default ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            accessor,
            accessorTemplate,
            _ => proceedExpression,
            this.AspectLayerId );

        var templateDriver = accessorTemplate.TemplateMember.Driver;

        return templateDriver.TryExpandDeclaration( expansionContext, accessorTemplate.TemplateArguments, out body );
    }

    /// <summary>
    /// Creates a trivial passthrough body for cases where we have template only for one accessor kind.
    /// </summary>
    private BlockSyntax CreateIdentityAccessorBody( SyntaxKind accessorDeclarationKind, MemberInjectionContext context )
    {
        switch ( accessorDeclarationKind )
        {
            case SyntaxKind.AddAccessorDeclaration:
                return context.SyntaxGenerator.FormattedBlock( ExpressionStatement( this.CreateAddExpression( context ) ) );

            case SyntaxKind.RemoveAccessorDeclaration:
                return context.SyntaxGenerator.FormattedBlock( ExpressionStatement( this.CreateRemoveExpression( context ) ) );

            default:
                throw new AssertionFailedException( $"Unexpected syntax kind: {accessorDeclarationKind}." );
        }
    }

    private ExpressionSyntax CreateAddExpression( MemberInjectionContext context )
        => AssignmentExpression(
            SyntaxKind.AddAssignmentExpression,
            this.CreateMemberAccessExpression( AspectReferenceTargetKind.EventAddAccessor, context ),
            IdentifierName( "value" ) );

    private ExpressionSyntax CreateRemoveExpression( MemberInjectionContext context )
        => AssignmentExpression(
            SyntaxKind.SubtractAssignmentExpression,
            this.CreateMemberAccessExpression( AspectReferenceTargetKind.EventRemoveAccessor, context ),
            IdentifierName( "value" ) );
}