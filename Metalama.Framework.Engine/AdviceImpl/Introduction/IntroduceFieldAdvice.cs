// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Diagnostics;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceFieldAdvice : IntroduceMemberAdvice<IField, IField, FieldBuilder>
{
    public IntroduceFieldAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        string? explicitName,
        TemplateMember<IField>? fieldTemplate,
        IntroductionScope scope,
        OverrideStrategy overrideStrategy,
        Action<IFieldBuilder>? buildAction )
        : base( parameters, explicitName, fieldTemplate, scope, overrideStrategy, buildAction, explicitlyImplementedInterfaceType: null ) { }

    protected override FieldBuilder CreateBuilder() => new( this.AspectLayerInstance, this.TargetDeclaration, this.MemberName );

    protected override void InitializeBuilderCore(
        FieldBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var templateDeclaration = this.Template?.GetDeclaration( this.SourceCompilation );
        builder.IsRequired = templateAttributeProperties?.IsRequired ?? templateDeclaration?.IsRequired ?? false;

        if ( this.Template != null )
        {
            builder.Type = templateDeclaration.AssertNotNull().Type;
            builder.Writeability = templateDeclaration.Writeability;

            // The InitializerExpression of template cannot be copied because it's an initializer template.
            // However, we need to store the template because we will need it when the field is overwritten.
            builder.InitializerTemplate = this.Template.GetInitializerTemplate();
        }
        else
        {
            builder.Type = this.SourceCompilation.Cache.SystemObjectType;
            builder.Writeability = Writeability.All;
        }

        var targetType = this.TargetDeclaration;

        if ( targetType.TypeKind is TypeKind.Struct or TypeKind.RecordStruct && targetType.IsReadOnly )
        {
            builder.Writeability = Writeability.ConstructorOnly;
        }
    }

    protected override void ValidateBuilder( FieldBuilder builder, IDiagnosticAdder diagnosticAdder )
    {
        base.ValidateBuilder( builder, diagnosticAdder );

        if ( this.TargetDeclaration is { TypeKind: TypeKind.Interface } )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceFieldIntoInterface.CreateRoslynDiagnostic(
                    this.TargetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, this.TargetDeclaration),
                    this ) );
        }
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceField;

    protected override IntroductionAdviceResult<IField> ImplementCore( FieldBuilder builder, in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );
        var existingDeclaration = targetDeclaration.FindClosestUniquelyNamedMember( builder.Name );

        if ( existingDeclaration != null )
        {
            if ( existingDeclaration is not IField )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentKind.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration, existingDeclaration.DeclarationKind),
                            this ) );
            }

            if ( existingDeclaration.IsStatic != builder.IsStatic )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentStaticity.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                             existingDeclaration.DeclaringType),
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
                                 existingDeclaration.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingDeclaration );

                case OverrideStrategy.New:
                    // If the existing declaration is in the current type, fail, otherwise, declare a new method and override.
                    if ( targetDeclaration.Equals( existingDeclaration.DeclaringType ) )
                    {
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceNewMemberWhenItAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, existingDeclaration.DeclaringType),
                                this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = true;
                        builder.Freeze();
                        context.AddTransformation( builder.CreateTransformation( this.Template ) );

                        return this.CreateSuccessResult( AdviceOutcome.New, builder );
                    }

                case OverrideStrategy.Override:
                    throw new NotSupportedException( "Override is not a supported OverrideStrategy for fields." );

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
        else
        {
            builder.Freeze();
            context.AddTransformation( builder.CreateTransformation( this.Template ) );

            OverrideHelper.AddTransformationsForStructField(
                targetDeclaration,
                this.AspectLayerInstance,
                context.AddTransformation /* We add an initializer if it does not have one */ );

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
    }
}