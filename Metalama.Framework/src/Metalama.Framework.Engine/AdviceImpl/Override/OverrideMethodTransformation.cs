// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// Method override, which expands a template.
/// </summary>
internal sealed class OverrideMethodTransformation : OverrideMethodBaseTransformation
{
    private BoundTemplateMethod BoundTemplate { get; }

    public OverrideMethodTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMethod> targetMethod,
        BoundTemplateMethod boundTemplate )
        : base( aspectLayerInstance, targetMethod )
    {
        this.BoundTemplate = boundTemplate;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        SyntaxUserExpression ProceedExpressionProvider( TemplateKind kind )
        {
            return this.CreateProceedExpression( context, kind );
        }

        var overriddenDeclaration = this.OverriddenMethod.GetTarget( this.InitialCompilation );

        var metaApi = MetaApi.ForMethod(
            overriddenDeclaration,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                this.BoundTemplate.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this.AspectInstance,
                context.ServiceProvider,
                MetaApiStaticity.Default ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            overriddenDeclaration,
            this.BoundTemplate,
            ProceedExpressionProvider,
            this.AspectLayerId );

        var templateDriver = this.BoundTemplate.TemplateMember.Driver;

        if ( !templateDriver.TryExpandDeclaration( expansionContext, this.BoundTemplate.TemplateArguments, out var newMethodBody ) )
        {
            // Template expansion error.
            return [];
        }

        return this.GetInjectedMembersImpl( context, newMethodBody, this.BoundTemplate.TemplateMember.MustInterpretAsAsyncTemplate() );
    }
}