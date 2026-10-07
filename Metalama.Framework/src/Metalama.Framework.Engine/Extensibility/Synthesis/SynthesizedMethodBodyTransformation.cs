// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Generates the body of a method that a pipeline extension declares, by expanding a template.
/// </summary>
/// <remarks>
/// <para>
/// The declared method is introduced by an <c>IntroduceMethodTransformation</c> with a default body, and this transformation overrides it, in the same
/// way as a method that an aspect introduces from a template. The linker then inlines the override into the introduced method. The difference with
/// an aspect is the expression of <c>meta.Proceed()</c>, which invokes the method given by a <see cref="ProceedBinding"/> instead of the previous
/// implementation of the method.
/// </para>
/// <para>
/// When the expansion fails, the template has reported a diagnostic, and <see cref="HasFailed"/> becomes <c>true</c>, so the linker does not
/// redirect the call sites to the method.
/// </para>
/// </remarks>
internal sealed class SynthesizedMethodBodyTransformation : OverrideMethodBaseTransformation
{
    private readonly BoundTemplateMethod _boundTemplate;
    private readonly ProceedBinding _proceedBinding;
    private readonly Location? _diagnosticLocation;
    private readonly ImmutableArray<IMetaExtension> _metaExtensions;
    private readonly IAspectInstanceInternal? _metaAspectInstance;
    private volatile bool _hasFailed;

    public SynthesizedMethodBodyTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMethod> method,
        BoundTemplateMethod boundTemplate,
        ProceedBinding proceedBinding,
        Location? diagnosticLocation,
        ImmutableArray<IMetaExtension> metaExtensions,
        IAspectInstanceInternal? metaAspectInstance )
        : base( aspectLayerInstance, method )
    {
        this._boundTemplate = boundTemplate;
        this._proceedBinding = proceedBinding;
        this._diagnosticLocation = diagnosticLocation;
        this._metaExtensions = metaExtensions;
        this._metaAspectInstance = metaAspectInstance;
    }

    /// <summary>
    /// Gets a value indicating whether the expansion of the template failed.
    /// </summary>
    public bool HasFailed => this._hasFailed;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var declaredMethod = this.OverriddenMethod.GetTarget( this.InitialCompilation );
        var finalMethod = this.OverriddenMethod.GetTarget( context.FinalCompilation );

        var metaApi = MetaApi.ForMethod(
            declaredMethod,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                this._boundTemplate.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this._metaAspectInstance,
                context.ServiceProvider,
                AdviceKind.OverrideMethod ) { DiagnosticLocationOverride = this._diagnosticLocation, Extensions = this._metaExtensions } );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            declaredMethod,
            this._boundTemplate,
            kind => ExtensionProceedExpressionFactory.Create( this._proceedBinding, finalMethod, kind, context.SyntaxGenerationContext ),
            this.AspectLayerId );

        if ( !this._boundTemplate.TemplateMember.Driver.TryExpandDeclaration( expansionContext, this._boundTemplate.TemplateArguments, out var body ) )
        {
            this._hasFailed = true;

            return [];
        }

        return this.GetInjectedMembersImpl( context, body, this._boundTemplate.TemplateMember.MustInterpretAsAsyncTemplate() );
    }
}
