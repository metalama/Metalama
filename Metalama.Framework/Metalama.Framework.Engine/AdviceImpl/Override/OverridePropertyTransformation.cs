// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverridePropertyTransformation : OverridePropertyBaseTransformation
{
    private BoundTemplateMethod? GetTemplate { get; }

    private BoundTemplateMethod? SetTemplate { get; }

    public OverridePropertyTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IProperty> overriddenProperty,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate )
        : base( aspectLayerInstance, overriddenProperty )
    {
        // We need the getTemplate and setTemplate to be set by the caller even if propertyTemplate is set.
        // The caller is responsible for verifying the compatibility of the template with the target.

        this.GetTemplate = getTemplate;
        this.SetTemplate = setTemplate;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var getTemplate = this.GetTemplate;
        var setTemplate = this.SetTemplate;

        var templateExpansionError = false;
        BlockSyntax? getAccessorBody = null;

        var overriddenDeclaration = this.OverriddenProperty.As<IProperty>().GetTarget( this.InitialCompilation );

        if ( overriddenDeclaration.GetMethod != null )
        {
            if ( getTemplate != null )
            {
                templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                    context,
                    getTemplate,
                    overriddenDeclaration.GetMethod,
                    overriddenDeclaration,
                    out getAccessorBody );
            }
            else
            {
                getAccessorBody = this.CreateIdentityAccessorBody( context, SyntaxKind.GetAccessorDeclaration );
            }
        }
        else
        {
            getAccessorBody = null;
        }

        BlockSyntax? setAccessorBody = null;

        if ( overriddenDeclaration.SetMethod != null )
        {
            if ( setTemplate != null )
            {
                templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                    context,
                    setTemplate,
                    overriddenDeclaration.SetMethod,
                    overriddenDeclaration,
                    out setAccessorBody );
            }
            else
            {
                setAccessorBody = this.CreateIdentityAccessorBody( context, SyntaxKind.SetAccessorDeclaration );
            }
        }
        else
        {
            setAccessorBody = null;
        }

        if ( templateExpansionError )
        {
            // Template expansion error.
            return [];
        }

        return this.GetInjectedMembersImpl( context, getAccessorBody, setAccessorBody );
    }

    private bool TryExpandAccessorTemplate(
        MemberInjectionContext context,
        BoundTemplateMethod accessorTemplate,
        IMethod accessor,
        IProperty overriddenDeclaration,
        [NotNullWhen( true )] out BlockSyntax? body )
    {
        SyntaxUserExpression ProceedExpressionProvider( TemplateKind kind )
        {
            return this.CreateProceedDynamicExpression( context, accessor, kind );
        }

        var metaApi = MetaApi.ForFieldOrPropertyOrIndexer(
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
            ProceedExpressionProvider,
            this.AspectLayerId );

        var templateDriver = accessorTemplate.TemplateMember.Driver;

        return templateDriver.TryExpandDeclaration( expansionContext, accessorTemplate.TemplateArguments, out body );
    }
}