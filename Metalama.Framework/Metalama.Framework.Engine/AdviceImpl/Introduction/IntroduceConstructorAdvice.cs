// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceConstructorAdvice : IntroduceMemberAdvice<IMethod, IConstructor, ConstructorBuilder>
{
    private readonly PartiallyBoundTemplateMethod _template;

    public IntroduceConstructorAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        PartiallyBoundTemplateMethod template,
        OverrideStrategy overrideStrategy,
        Action<IConstructorBuilder>? buildAction,
        IAdviceFactoryImpl adviceFactory )
        : base(
            parameters,
            null,
            template.TemplateMember,
            IntroductionScope.Instance,
            overrideStrategy,
            buildAction,
            explicitlyImplementedInterfaceType: null,
            adviceFactory )
    {
        this._template = template;
    }

    protected override ConstructorBuilder CreateBuilder()
    {
        return new ConstructorBuilder( this.AspectLayerInstance, this.TargetDeclaration );
    }

    protected override void InitializeBuilderCore(
        ConstructorBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var templateDeclaration = this.Template.AssertNotNull().GetDeclaration( this.SourceCompilation );

        var typeRewriter = TemplateTypeRewriter.Get( this._template );

        var runtimeParameters = this.Template.AssertNotNull().TemplateClassMember.RunTimeParameters;

        foreach ( var runtimeParameter in runtimeParameters )
        {
            var templateParameter = templateDeclaration.Parameters[runtimeParameter.SourceIndex];

            var parameterBuilder = builder.AddParameter(
                templateParameter.Name,
                typeRewriter.Visit( templateParameter.Type ),
                templateParameter.RefKind,
                templateParameter.DefaultValue );

            CopyTemplateAttributes( templateParameter, parameterBuilder, context.ServiceProvider );
        }
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceConstructor;

    protected override IntroductionAdviceResult<IConstructor> ImplementCore( ConstructorBuilder builder, in AdviceImplementationContext context )
    {
        // Determine whether we need introduction transformation (something may exist in the original code or could have been introduced by previous steps).
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingImplicitConstructor =
            builder.IsStatic
                ? targetDeclaration.StaticConstructor?.IsImplicitlyDeclared == true
                    ? targetDeclaration.StaticConstructor
                    : null
                : targetDeclaration.Constructors.FirstOrDefault( c => c.IsImplicitInstanceConstructor() );

        var existingConstructor =
            builder.IsStatic
                ? targetDeclaration.StaticConstructor
                : targetDeclaration.Constructors.OfExactSignature( builder );

        // TODO: Introduce attributes that are added not present on the existing member?
        if ( existingConstructor == null || existingImplicitConstructor != null )
        {
            if ( existingImplicitConstructor != null && builder.Parameters.Count == 0 )
            {
                // Redirect if the builder has no parameters and the existing constructor is implicit.
                builder.ReplacedImplicitConstructor = existingImplicitConstructor;
            }

            builder.Freeze();

            // There is no existing declaration, we will introduce and override the introduced.

            var overriddenConstructor = new OverrideConstructorTransformation(
                this.AspectLayerInstance,
                builder.ToFullRef(),
                this._template.ForIntroduction( builder ) );

            context.AddTransformation( builder.CreateTransformation() );
            context.AddTransformation( overriddenConstructor );

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
                                 existingConstructor.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingConstructor );

                case OverrideStrategy.Override:
                    var overriddenMethod = new OverrideConstructorTransformation(
                        this.AspectLayerInstance,
                        existingConstructor.ToFullRef(),
                        this._template.ForIntroduction( existingConstructor ) );

                    context.AddTransformation( overriddenMethod );

                    return this.CreateSuccessResult( AdviceOutcome.Override, existingConstructor );

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}