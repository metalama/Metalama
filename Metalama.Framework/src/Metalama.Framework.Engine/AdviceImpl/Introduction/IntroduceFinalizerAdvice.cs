// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceFinalizerAdvice : IntroduceMemberAdvice<IMethod, IMethod, MethodBuilder>
{
    private readonly PartiallyBoundTemplateMethod _template;

    public IntroduceFinalizerAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        PartiallyBoundTemplateMethod template,
        OverrideStrategy overrideStrategy,
        IAdviceFactoryImpl adviceFactory )
        : base(
            parameters,
            null,
            template.TemplateMember,
            IntroductionScope.Instance,
            overrideStrategy,
            _ => { },
            explicitlyImplementedInterfaceType: null,
            adviceFactory )
    {
        this._template = template;
    }

    protected override MethodBuilder CreateBuilder()
    {
        return new MethodBuilder( this.AspectLayerInstance, this.TargetDeclaration, "Finalize", DeclarationKind.Finalizer );
    }

    protected override void InitializeBuilderCore(
        MethodBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        switch ( this.OverrideStrategy )
        {
            case OverrideStrategy.New:
                context.Diagnostics.Report(
                    AdviceDiagnosticDescriptors.CannotUseNewOverrideStrategyWithFinalizers.CreateRoslynDiagnostic(
                        this.TargetDeclaration.GetDiagnosticLocation(),
                        (this.AspectInstance.AspectClass.ShortName, this.TargetDeclaration, this.OverrideStrategy),
                        this ) );

                break;
        }

        // TODO: The base implementation may take more than needed from the template. Most would be ignored by the transformation, but
        //       the user may see it in the code model.
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceFinalizer;

    protected override IntroductionAdviceResult<IMethod> ImplementCore( MethodBuilder builder, in AdviceImplementationContext context )
    {
        // Determine whether we need introduction transformation (something may exist in the original code or could have been introduced by previous steps).
        var targetDeclaration = this.TargetDeclaration;

        var existingFinalizer = targetDeclaration.Finalizer;

        // TODO: Introduce attributes that are added not present on the existing member?
        if ( existingFinalizer == null )
        {
            // Check that there is no other member named the same, which is possible, but very unlikely.
            var existingOtherMember = targetDeclaration.FindClosestUniquelyNamedMember( builder.Name );

            if ( existingOtherMember != null )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentKind.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration, existingOtherMember.DeclarationKind),
                            this ) );
            }

            builder.IsOverride = false;
            builder.HasNewKeyword = builder.IsNew = false;
            builder.Freeze();

            // There is no existing declaration, we will introduce and override the introduced.
            var overriddenMethod = new OverrideFinalizerTransformation(
                this.AspectLayerInstance,
                builder.ToFullRef(),
                this._template.ForIntroduction( builder ) );

            context.AddTransformation( builder.ToTransformation() );
            context.AddTransformation( overriddenMethod );

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            switch ( this.OverrideStrategy )
            {
                case OverrideStrategy.Fail:
                    // Produce fail diagnostic.
                    return
                        this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceMemberAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                 existingFinalizer.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingFinalizer );

                case OverrideStrategy.Override:
                    if ( targetDeclaration.Equals( existingFinalizer.DeclaringType ) )
                    {
                        var overriddenMethod = new OverrideFinalizerTransformation(
                            this.AspectLayerInstance,
                            existingFinalizer.ToFullRef(),
                            this._template.ForIntroduction( existingFinalizer ) );

                        context.AddTransformation( overriddenMethod );

                        return this.CreateSuccessResult( AdviceOutcome.Override, existingFinalizer );
                    }
                    else
                    {
                        builder.IsOverride = true;
                        builder.HasNewKeyword = builder.IsNew = false;
                        builder.OverriddenMethod = existingFinalizer;

                        builder.Freeze();

                        var overriddenMethod = new OverrideFinalizerTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._template.ForIntroduction( builder ) );

                        context.AddTransformation( builder.ToTransformation() );
                        context.AddTransformation( overriddenMethod );

                        return this.CreateSuccessResult( AdviceOutcome.Override, builder );
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}