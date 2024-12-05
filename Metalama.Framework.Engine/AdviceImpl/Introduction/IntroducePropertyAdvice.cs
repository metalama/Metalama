// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroducePropertyAdvice : IntroduceMemberAdvice<IProperty, IProperty, PropertyBuilder>
{
    private readonly IType? _explicitType;
    private readonly PartiallyBoundTemplateMethod? _getTemplate;
    private readonly PartiallyBoundTemplateMethod? _setTemplate;
    private readonly bool _isProgrammaticAutoProperty;

    public IntroducePropertyAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        string? explicitName,
        IType? explicitType,
        TemplateMember<IProperty>? propertyTemplate,
        PartiallyBoundTemplateMethod? getTemplate,
        PartiallyBoundTemplateMethod? setTemplate,
        IntroductionScope scope,
        OverrideStrategy overrideStrategy,
        Action<IPropertyBuilder>? buildAction,
        INamedType? explicitlyImplementedInterfaceType )
        : base( parameters, explicitName, propertyTemplate, scope, overrideStrategy, buildAction, explicitlyImplementedInterfaceType )
    {
        this._explicitType = explicitType;
        this._getTemplate = getTemplate;
        this._setTemplate = setTemplate;
        this._isProgrammaticAutoProperty = propertyTemplate == null && getTemplate == null && setTemplate == null;
    }

    protected override PropertyBuilder CreateBuilder()
    {
        var templatePropertyDeclaration = this.Template?.GetDeclaration( this.SourceCompilation );
        var name = this.MemberName;

        var hasGet = this._isProgrammaticAutoProperty
                     || (templatePropertyDeclaration != null ? templatePropertyDeclaration.GetMethod != null : this._getTemplate != null);

        var hasSet = this._isProgrammaticAutoProperty
                     || (templatePropertyDeclaration != null ? templatePropertyDeclaration.SetMethod != null : this._setTemplate != null);

        var builder = new PropertyBuilder(
            this.AspectLayerInstance,
            this.TargetDeclaration,
            name,
            hasGet,
            hasSet,
            this._isProgrammaticAutoProperty || templatePropertyDeclaration is { IsAutoPropertyOrField: true },
            templatePropertyDeclaration is { Writeability: Writeability.InitOnly },
            false,
            templatePropertyDeclaration is { Writeability: Writeability.ConstructorOnly, IsAutoPropertyOrField: true } );

        if ( this._explicitType != null )
        {
            builder.Type = this._explicitType;
        }

        return builder;
    }

    protected override void InitializeBuilderCore(
        PropertyBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var templateDeclaration = this.Template?.GetDeclaration( this.SourceCompilation );
        var getTemplateDeclaration = this._getTemplate?.GetDeclaration( this.SourceCompilation );
        var setTemplateDeclaration = this._setTemplate?.GetDeclaration( this.SourceCompilation );

        var serviceProvider = context.ServiceProvider;

        builder.IsRequired = templateAttributeProperties?.IsRequired ?? templateDeclaration?.IsRequired ?? false;

        if ( !this._isProgrammaticAutoProperty )
        {
            if ( this._getTemplate != null )
            {
                var typeRewriter = TemplateTypeRewriter.Get( this._getTemplate );

                builder.Type = typeRewriter.Visit( getTemplateDeclaration.AssertNotNull().ReturnType );
            }
            else if ( this._setTemplate != null )
            {
                var runtimeParameters = this._setTemplate.TemplateMember.TemplateClassMember.RunTimeParameters;

                var typeRewriter = TemplateTypeRewriter.Get( this._setTemplate );

                if ( runtimeParameters.Length > 0 )
                {
                    // There may be an invalid template without runtime parameters, in which case type cannot be determined.

                    builder.Type = typeRewriter.Visit( setTemplateDeclaration.AssertNotNull().Parameters[runtimeParameters[0].SourceIndex].Type );
                }
            }
            else if ( this.Template != null )
            {
                // Case for event fields.
                builder.Type = TemplateTypeRewriter.Unbound.Visit( templateDeclaration.AssertNotNull().Type );
            }

            builder.Accessibility =
                this.Template?.Accessibility ?? (this._getTemplate != null
                    ? this._getTemplate.TemplateMember.Accessibility
                    : this._setTemplate?.TemplateMember.Accessibility).AssertNotNull();

            builder.GetMethod?.SetIsIteratorMethod( this.Template?.IsIteratorMethod ?? this._getTemplate?.TemplateMember.IsIteratorMethod ?? false );

            if ( this.Template != null )
            {
                if ( templateDeclaration.AssertNotNull().GetMethod != null )
                {
                    builder.GetMethod.AssertNotNull().Accessibility = this.Template.GetAccessorAccessibility;
                }

                if ( templateDeclaration.AssertNotNull().SetMethod != null )
                {
                    builder.SetMethod.AssertNotNull().Accessibility = this.Template.SetAccessorAccessibility;
                }

                if ( templateDeclaration.GetSymbol().AssertSymbolNotNull().GetBackingField() is { } backingField )
                {
                    var classificationService = serviceProvider.Global.GetRequiredService<AttributeClassificationService>();

                    foreach ( var attribute in backingField.GetAttributes() )
                    {
                        if ( classificationService.MustCopyTemplateAttribute( attribute ) )
                        {
                            builder.AddFieldAttribute( new SourceAttribute( attribute, this.SourceCompilation, builder ) );
                        }
                    }
                }
            }
        }

        if ( builder.GetMethod != null )
        {
            if ( this._getTemplate != null )
            {
                AddAttributeForAccessorTemplate(
                    this._getTemplate.TemplateMember.TemplateClassMember,
                    getTemplateDeclaration.AssertNotNull(),
                    builder.GetMethod );
            }
            else if ( templateDeclaration?.GetMethod != null )
            {
                AddAttributeForAccessorTemplate( this.Template.AssertNotNull().TemplateClassMember, templateDeclaration.GetMethod, builder.GetMethod );
            }
        }

        if ( builder.SetMethod != null )
        {
            if ( this._setTemplate != null )
            {
                AddAttributeForAccessorTemplate(
                    this._setTemplate.TemplateMember.TemplateClassMember,
                    setTemplateDeclaration.AssertNotNull(),
                    builder.SetMethod );
            }
            else if ( templateDeclaration?.SetMethod != null )
            {
                AddAttributeForAccessorTemplate( this.Template.AssertNotNull().TemplateClassMember, templateDeclaration.SetMethod, builder.SetMethod );
            }
        }

        void AddAttributeForAccessorTemplate( TemplateClassMember templateClassMember, IMethod accessorTemplate, IMethodBuilder accessorBuilder )
        {
            CopyTemplateAttributes( accessorTemplate, accessorBuilder, serviceProvider );

            if ( accessorBuilder.Parameters.Count > 0 && templateClassMember.RunTimeParameters.Length > 0 )
            {
                // There may be an invalid template without runtime parameters, in which case attributes cannot be copied.
                CopyTemplateAttributes(
                    accessorTemplate.Parameters[templateClassMember.RunTimeParameters[0].SourceIndex],
                    accessorBuilder.Parameters[0],
                    serviceProvider );
            }

            CopyTemplateAttributes( accessorTemplate.ReturnParameter, accessorBuilder.ReturnParameter, serviceProvider );
        }

        // TODO: For get accessor template, we are ignoring accessibility of set accessor template because it can be easily incompatible.

        builder.InitializerTemplate = this.Template?.GetInitializerTemplate()?.As<IFieldOrProperty>();
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceProperty;

    protected override IntroductionAdviceResult<IProperty> ImplementCore( PropertyBuilder builder, in AdviceImplementationContext context )
    {
        var serviceProvider = context.ServiceProvider;

        // Determine whether we need introduction transformation (something may exist in the original code or could have been introduced by previous steps).
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingDeclaration = targetDeclaration.FindClosestUniquelyNamedMember( builder.Name );

        var templateDeclaration = this.Template?.GetDeclaration( this.SourceCompilation );
        var isAutoProperty = templateDeclaration is { IsAutoPropertyOrField: true };

        // TODO: Introduce attributes that are added not present on the existing member?
        if ( existingDeclaration == null )
        {
            builder.Freeze();

            // There is no existing declaration.
            if ( isAutoProperty )
            {
                // Introduced auto property.
                context.AddTransformation( builder.CreateTransformation( this.Template ) );

                OverrideHelper.AddTransformationsForStructField( targetDeclaration, this.AspectLayerInstance, context.AddTransformation );
            }
            else
            {
                // Introduce and override using the template.
                var overriddenProperty = new OverridePropertyTransformation(
                    this.AspectLayerInstance,
                    builder.ToRef(),
                    this._getTemplate?.ForIntroduction( builder.GetMethod ),
                    this._setTemplate?.ForIntroduction( builder.SetMethod ) );

                context.AddTransformation( builder.CreateTransformation( this.Template ) );

                if ( this.Template?.TemplateClassMember.TemplateInfo.HasNoBody != true )
                {
                    context.AddTransformation( overriddenProperty );
                }
            }

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            if ( existingDeclaration is not IProperty existingProperty )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentKind.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration, existingDeclaration.DeclarationKind),
                            this ) );
            }

            if ( existingProperty.IsStatic != builder.IsStatic )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentStaticity.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                             existingProperty.DeclaringType),
                            this ) );
            }
            else if ( !builder.Type.Equals( existingProperty.Type ) )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceDifferentExistingReturnType.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                             existingProperty.DeclaringType, existingProperty.Type),
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
                                 existingProperty.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingProperty );

                case OverrideStrategy.New:
                    // If the existing declaration is in the current type, fail, otherwise, declare a new method and override.
                    if ( targetDeclaration.Equals( existingProperty.DeclaringType ) )
                    {
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceNewMemberWhenItAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, existingProperty.DeclaringType),
                                this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = true;
                        builder.OverriddenProperty = existingProperty;
                        builder.Freeze();

                        var overriddenProperty = new OverridePropertyTransformation(
                            this.AspectLayerInstance,
                            builder.ToRef(),
                            this._getTemplate?.ForIntroduction( builder.GetMethod ),
                            this._setTemplate?.ForIntroduction( builder.SetMethod ) );

                        context.AddTransformation( builder.CreateTransformation( this.Template ) );

                        if ( this.Template?.TemplateClassMember.TemplateInfo.HasNoBody != true )
                        {
                            context.AddTransformation( overriddenProperty );
                        }

                        return this.CreateSuccessResult( AdviceOutcome.New, builder );
                    }

                case OverrideStrategy.Override:
                    if ( targetDeclaration.Equals( existingProperty.DeclaringType ) )
                    {
                        var overriddenMethod = new OverridePropertyTransformation(
                            this.AspectLayerInstance,
                            existingProperty.ToFullRef(),
                            this._getTemplate?.ForIntroduction( existingProperty.GetMethod ),
                            this._setTemplate?.ForIntroduction( existingProperty.SetMethod ) );

                        context.AddTransformation( overriddenMethod );

                        return this.CreateSuccessResult( AdviceOutcome.Override, existingProperty );
                    }
                    else if ( existingProperty.IsSealed || !existingProperty.IsOverridable() )
                    {
                        return
                            this.CreateFailedResult(
                                AdviceDiagnosticDescriptors.CannotIntroduceOverrideOfSealed.CreateRoslynDiagnostic(
                                    targetDeclaration.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                     existingProperty.DeclaringType),
                                    this ) );
                    }
                    else
                    {
                        builder.IsOverride = true;
                        builder.OverriddenProperty = existingProperty;
                        builder.Freeze();

                        context.AddTransformation( builder.CreateTransformation( this.Template ) );

                        OverrideHelper.OverrideProperty(
                            serviceProvider,
                            this.AspectLayerInstance,
                            builder,
                            this._getTemplate?.ForIntroduction( builder.GetMethod ),
                            this._setTemplate?.ForIntroduction( builder.SetMethod ),
                            context.AddTransformation );

                        return this.CreateSuccessResult( AdviceOutcome.Override, builder );
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}