// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
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

    /// <summary>
    /// Gets the name of the backing field introduced for a template that uses the C# 14 <c>field</c> keyword.
    /// This is <c>null</c> if the template does not use the <c>field</c> keyword.
    /// </summary>
    private string? BackingFieldName { get; }

    public OverridePropertyTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IProperty> overriddenProperty,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate,
        string? backingFieldName = null )
        : base( aspectLayerInstance, overriddenProperty )
    {
        // We need the getTemplate and setTemplate to be set by the caller even if propertyTemplate is set.
        // The caller is responsible for verifying the compatibility of the template with the target.

        this.GetTemplate = getTemplate;
        this.SetTemplate = setTemplate;
        this.BackingFieldName = backingFieldName;
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
                AdviceKind.OverrideFieldOrPropertyOrIndexer ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            accessor,
            accessorTemplate,
            ProceedExpressionProvider,
            this.AspectLayerId ) { BackingFieldName = this.BackingFieldName };

        var templateDriver = accessorTemplate.TemplateMember.Driver;

        return templateDriver.TryExpandDeclaration( expansionContext, accessorTemplate.TemplateArguments, out body );
    }
}