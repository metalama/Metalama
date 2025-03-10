// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

internal sealed class OverrideIndexerTransformation : OverrideIndexerBaseTransformation
{
    private BoundTemplateMethod? GetTemplate { get; }

    private BoundTemplateMethod? SetTemplate { get; }

    public OverrideIndexerTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IIndexer> overriddenDeclaration,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate )
        : base( aspectLayerInstance, overriddenDeclaration )
    {
        this.GetTemplate = getTemplate;
        this.SetTemplate = setTemplate;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var templateExpansionError = false;
        BlockSyntax? getAccessorBody = null;

        var overriddenDeclaration = this.OverriddenIndexer.GetTarget( this.InitialCompilation );

        if ( overriddenDeclaration.GetMethod != null )
        {
            if ( this.GetTemplate != null )
            {
                templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                    context,
                    this.GetTemplate,
                    overriddenDeclaration.GetMethod,
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
            if ( this.SetTemplate != null )
            {
                templateExpansionError = templateExpansionError || !this.TryExpandAccessorTemplate(
                    context,
                    this.SetTemplate,
                    overriddenDeclaration.SetMethod,
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
        [NotNullWhen( true )] out BlockSyntax? body )
    {
        var overriddenDeclaration = (IIndexer) this.OverriddenDeclaration.GetTarget( context.FinalCompilation );

        SyntaxUserExpression ProceedExpressionProvider( TemplateKind kind )
        {
            return this.CreateProceedDynamicExpression( context, accessor, kind, overriddenDeclaration );
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