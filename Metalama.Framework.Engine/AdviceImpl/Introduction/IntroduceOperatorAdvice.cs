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
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceOperatorAdvice : IntroduceMemberAdvice<IMethod, IMethod, MethodBuilder>
{
    private readonly OperatorKind _operatorKind;
    private readonly IType _leftOperandType;
    private readonly IType? _rightOperandType;
    private readonly IType _resultType;
    private readonly PartiallyBoundTemplateMethod _template;

    public IntroduceOperatorAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        OperatorKind operatorKind,
        IType leftOperandType,
        IType? rightOperandType,
        IType resultType,
        PartiallyBoundTemplateMethod template,
        OverrideStrategy overrideStrategy,
        Action<IMethodBuilder>? buildAction,
        INamedType? explicitlyImplementedInterfaceType )
        : base(
            parameters,
            explicitName: null,
            template.TemplateMember,
            IntroductionScope.Static,
            overrideStrategy,
            buildAction,
            explicitlyImplementedInterfaceType )
    {
        this._operatorKind = operatorKind;
        this._leftOperandType = leftOperandType;
        this._rightOperandType = rightOperandType;
        this._resultType = resultType;
        this._template = template;
    }

    protected override MethodBuilder CreateBuilder()
    {
        var builder = new MethodBuilder(
            this.AspectLayerInstance,
            this.TargetDeclaration,
            this._operatorKind.ToOperatorMethodName(),
            DeclarationKind.Operator,
            this._operatorKind ) { IsStatic = true };

        var runtimeParameters = this.Template.AssertNotNull().TemplateClassMember.RunTimeParameters;

        // Add predefined parameters of correct types.
        var firstParameterName = !runtimeParameters.IsEmpty ? runtimeParameters[0].Name : "a";
        builder.AddParameter( firstParameterName, this._leftOperandType );

        if ( this._rightOperandType != null )
        {
            var secondParameterName = !runtimeParameters.IsEmpty ? runtimeParameters[1].Name : "a";
            builder.AddParameter( secondParameterName, this._rightOperandType );
        }

        builder.ReturnType = this._resultType;

        return builder;
    }

    protected override void InitializeBuilderCore(
        MethodBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var serviceProvider = context.ServiceProvider;
        var runtimeParameters = this.Template.AssertNotNull().TemplateClassMember.RunTimeParameters;

        var templateDeclaration = this.Template.AssertNotNull().GetDeclaration( this.SourceCompilation );

        CopyTemplateAttributes( templateDeclaration.ReturnParameter, builder.ReturnParameter, serviceProvider );

        if ( runtimeParameters.Length == builder.Parameters.Count )
        {
            for ( var i = 0; i < runtimeParameters.Length; i++ )
            {
                var runtimeParameter = runtimeParameters[i];
                var templateParameter = templateDeclaration.Parameters[runtimeParameter.SourceIndex];
                var parameterBuilder = builder.Parameters[i];

                CopyTemplateAttributes( templateParameter, parameterBuilder, serviceProvider );
            }
        }

        // Invalid signatures may have incorrect parameter number, but we validate template later than initializing the builder.
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceOperator;

    protected override IntroductionAdviceResult<IMethod> ImplementCore( MethodBuilder builder, in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingOperator = targetDeclaration.FindClosestVisibleMethod( builder );

        if ( existingOperator == null )
        {
            builder.Freeze();

            var overriddenOperator = new OverrideOperatorTransformation(
                this.AspectLayerInstance,
                builder.ToFullRef(),
                this._template.ForIntroduction( builder ) );

            context.AddTransformation( builder.ToTransformation() );
            context.AddTransformation( overriddenOperator );

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
                                 existingOperator.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingOperator );

                case OverrideStrategy.New:
                    // If the existing declaration is in the current type, fail, otherwise, declare a new method and override.
                    if ( targetDeclaration.Equals( existingOperator.DeclaringType ) )
                    {
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceNewMemberWhenItAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, existingOperator.DeclaringType),
                                this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = true;
                        builder.IsOverride = false;
                        builder.Freeze();

                        var overriddenOperator = new OverrideOperatorTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._template.ForIntroduction( builder ) );

                        context.AddTransformation( overriddenOperator );
                        context.AddTransformation( builder.ToTransformation() );

                        return this.CreateSuccessResult( AdviceOutcome.New, builder );
                    }

                case OverrideStrategy.Override:
                    if ( targetDeclaration.Equals( existingOperator.DeclaringType ) )
                    {
                        var overriddenOperator = new OverrideOperatorTransformation(
                            this.AspectLayerInstance,
                            existingOperator.ToFullRef(),
                            this._template.ForIntroduction( existingOperator ) );

                        context.AddTransformation( overriddenOperator );

                        return this.CreateSuccessResult( AdviceOutcome.Override, existingOperator );
                    }
                    else if ( existingOperator.IsSealed || !existingOperator.IsOverridable() )
                    {
                        return
                            this.CreateFailedResult(
                                AdviceDiagnosticDescriptors.CannotIntroduceOverrideOfSealed.CreateRoslynDiagnostic(
                                    targetDeclaration.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                     existingOperator.DeclaringType),
                                    this ) );
                    }
                    else
                    {
                        builder.IsOverride = true;
                        builder.HasNewKeyword = builder.IsNew = false;
                        builder.OverriddenMethod = existingOperator;
                        builder.Freeze();

                        var overriddenOperator = new OverrideOperatorTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._template.ForIntroduction( builder ) );

                        context.AddTransformation( builder.ToTransformation() );
                        context.AddTransformation( overriddenOperator );

                        return this.CreateSuccessResult( AdviceOutcome.Override, builder );
                    }

                default:
                    throw new AssertionFailedException( $"Invalid value for OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}