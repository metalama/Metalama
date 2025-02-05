// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
using Metalama.Framework.Engine.Utilities;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceMethodAdvice : IntroduceMemberAdvice<IMethod, IMethod, MethodBuilder>
{
    private readonly PartiallyBoundTemplateMethod _template;

    public IntroduceMethodAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        string? explicitName,
        PartiallyBoundTemplateMethod template,
        IntroductionScope scope,
        OverrideStrategy overrideStrategy,
        Action<IMethodBuilder>? buildAction,
        INamedType? explicitlyImplementedInterfaceType,
        IAdviceFactoryImpl adviceFactory )
        : base( parameters, explicitName, template.TemplateMember, scope, overrideStrategy, buildAction, explicitlyImplementedInterfaceType, adviceFactory )
    {
        this._template = template;
    }

    protected override MethodBuilder CreateBuilder() => new( this.AspectLayerInstance, this.TargetDeclaration, this.MemberName );

    protected override void InitializeBuilderCore(
        MethodBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var templateDeclaration = this.Template.AssertNotNull().GetDeclaration( this.SourceCompilation );

        var serviceProvider = context.ServiceProvider;

        builder.IsAsync = templateDeclaration.IsAsync;

        var typeRewriter = TemplateTypeRewriter.Get( this._template );

        // Handle iterator info.
        builder.SetIsIteratorMethod( this.Template.IsIteratorMethod );

        // Handle return type.

        if ( templateDeclaration.ReturnParameter.Type.TypeKind == TypeKind.Dynamic )
        {
            // Templates with dynamic return value result in object return type of the introduced member.
            builder.ReturnParameter.Type = builder.Compilation.Cache.SystemObjectType;
        }
        else
        {
            builder.ReturnParameter.Type = typeRewriter.Visit( templateDeclaration.ReturnParameter.Type );

            if ( templateDeclaration.ReturnParameter.RefKind != RefKind.None )
            {
                throw new InvalidOperationException(
                    MetalamaStringFormatter.Format(
                        $"The '{this.AspectInstance.AspectClass.ShortName}' cannot introduce the method '{builder}' because methods returning 'ref' are not supported." ) );
            }
        }

        CopyTemplateAttributes( templateDeclaration.ReturnParameter, builder.ReturnParameter, serviceProvider );

        var runtimeParameters = this.Template.AssertNotNull().TemplateClassMember.RunTimeParameters;

        foreach ( var runtimeParameter in runtimeParameters )
        {
            var templateParameter = templateDeclaration.Parameters[runtimeParameter.SourceIndex];

            var parameterBuilder = builder.AddParameter(
                templateParameter.Name,
                typeRewriter.Visit( templateParameter.Type ),
                templateParameter.RefKind,
                templateParameter.DefaultValue );

            parameterBuilder.IsParams = templateParameter.IsParams;

            parameterBuilder.IsThis = templateParameter.Attributes.Any(
                templateParameter.Compilation.Cache.GetOrAdd( static c => c.Factory.GetTypeByReflectionName( typeof(ThisAttribute).FullName! ) ) );

            CopyTemplateAttributes( templateParameter, parameterBuilder, serviceProvider );
        }

        var runtimeTypeParameters = this.Template.AssertNotNull().TemplateClassMember.RunTimeTypeParameters;

        foreach ( var runtimeTypeParameter in runtimeTypeParameters )
        {
            var templateTypeParameter = templateDeclaration.TypeParameters[runtimeTypeParameter.SourceIndex];
            var typeParameterBuilder = builder.AddTypeParameter( templateTypeParameter.Name );
            typeParameterBuilder.Variance = templateTypeParameter.Variance;
            typeParameterBuilder.HasDefaultConstructorConstraint = templateTypeParameter.HasDefaultConstructorConstraint;
            typeParameterBuilder.TypeKindConstraint = templateTypeParameter.TypeKindConstraint;
            typeParameterBuilder.AllowsRefStruct = templateTypeParameter.AllowsRefStruct;
            typeParameterBuilder.IsConstraintNullable = templateTypeParameter.IsConstraintNullable;

            foreach ( var templateGenericParameterConstraint in templateTypeParameter.TypeConstraints )
            {
                typeParameterBuilder.AddTypeConstraint( typeRewriter.Visit( templateGenericParameterConstraint ) );
            }

            CopyTemplateAttributes( templateTypeParameter, typeParameterBuilder, serviceProvider );
        }
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceMethod;

    protected override IntroductionAdviceResult<IMethod> ImplementCore( MethodBuilder builder, in AdviceImplementationContext context )
    {
        // Determine whether we need introduction transformation (something may exist in the original code or could have been introduced by previous steps).
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );
        var existingMethod = targetDeclaration.FindClosestVisibleMethod( builder );

        var hasNoBody = this.Template?.TemplateClassMember.TemplateInfo.HasNoBody == true;

        // TODO: Introduce attributes that are added not present on the existing member?
        if ( existingMethod == null )
        {
            // Check that there is no other member named the same, otherwise we cannot add a method.
            var existingOtherMember =
                builder is { Name: "Finalize", Parameters.Count: 0, TypeParameters.Count: 0 }
                    ? targetDeclaration.Finalizer
                    : targetDeclaration.FindClosestUniquelyNamedMember( builder.Name );

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

            context.AddTransformation( builder.ToTransformation() );

            // There is no existing declaration, we will introduce and override the introduced (if it has a body).
            var overriddenMethod = new OverrideMethodTransformation(
                this.AspectLayerInstance,
                builder.ToFullRef(),
                this._template.ForIntroduction( builder ) );

            if ( !hasNoBody )
            {
                context.AddTransformation( overriddenMethod );
            }

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            if ( existingMethod.IsStatic != builder.IsStatic )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentStaticity.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                             existingMethod.DeclaringType),
                            this ) );
            }

            switch ( this.OverrideStrategy )
            {
                case OverrideStrategy.Fail:
                    // Produce fail diagnostic.
                    return
                        this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceMemberAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                 existingMethod.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingMethod );

                case OverrideStrategy.New:
                    // If the existing declaration is in the current type, fail, otherwise, declare a new method and override.
                    if ( targetDeclaration.Equals( existingMethod.DeclaringType ) )
                    {
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceNewMemberWhenItAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, existingMethod.DeclaringType),
                                this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = true;
                        builder.IsOverride = false;
                        builder.OverriddenMethod = existingMethod;

                        builder.Freeze();

                        var overriddenMethod = new OverrideMethodTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._template.AssertNotNull().ForIntroduction( builder ) );

                        context.AddTransformation( builder.ToTransformation() );

                        if ( !hasNoBody )
                        {
                            context.AddTransformation( overriddenMethod );
                        }

                        return this.CreateSuccessResult( AdviceOutcome.New, builder );
                    }

                case OverrideStrategy.Override:
                    Invariant.Assert( !hasNoBody );

                    if ( !builder.ReturnType.IsConvertibleTo( existingMethod.ReturnType, ConversionKind.Reference ) )
                    {
                        return
                            this.CreateFailedResult(
                                AdviceDiagnosticDescriptors.CannotIntroduceDifferentExistingReturnType.CreateRoslynDiagnostic(
                                    targetDeclaration.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                     existingMethod.DeclaringType, existingMethod.ReturnType),
                                    this ) );
                    }
                    else if ( targetDeclaration.Equals( existingMethod.DeclaringType ) )
                    {
                        var overriddenMethod = new OverrideMethodTransformation(
                            this.AspectLayerInstance,
                            existingMethod.ToFullRef(),
                            this._template.AssertNotNull().ForIntroduction( existingMethod ) );

                        context.AddTransformation( overriddenMethod );

                        return this.CreateSuccessResult( AdviceOutcome.Override, existingMethod );
                    }
                    else if ( !existingMethod.IsOverridable() )
                    {
                        return
                            this.CreateFailedResult(
                                AdviceDiagnosticDescriptors.CannotIntroduceOverrideOfSealed.CreateRoslynDiagnostic(
                                    targetDeclaration.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                     existingMethod.DeclaringType),
                                    this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = false;
                        builder.IsOverride = true;
                        builder.OverriddenMethod = existingMethod;

                        builder.Freeze();

                        var overriddenMethod = new OverrideMethodTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._template.AssertNotNull().ForIntroduction( builder ) );

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