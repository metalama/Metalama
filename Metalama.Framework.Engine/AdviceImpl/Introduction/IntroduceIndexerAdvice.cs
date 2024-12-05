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
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceIndexerAdvice : IntroduceMemberAdvice<IIndexer, IIndexer, IndexerBuilder>
{
    private readonly IReadOnlyList<(IType Type, string Name)> _indices;
    private readonly PartiallyBoundTemplateMethod? _getTemplate;
    private readonly PartiallyBoundTemplateMethod? _setTemplate;

    public IntroduceIndexerAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        IReadOnlyList<(IType Type, string Name)> indices,
        PartiallyBoundTemplateMethod? getTemplate,
        PartiallyBoundTemplateMethod? setTemplate,
        IntroductionScope scope,
        OverrideStrategy overrideStrategy,
        Action<IIndexerBuilder>? buildAction,
        INamedType? explicitlyImplementedInterfaceType )
        : base( parameters, "this[]", template: null, scope, overrideStrategy, buildAction, explicitlyImplementedInterfaceType )
    {
        this._indices = indices;
        this._getTemplate = getTemplate;
        this._setTemplate = setTemplate;
    }

    protected override IndexerBuilder CreateBuilder()
    {
        var hasGet = this._getTemplate != null;
        var hasSet = this._setTemplate != null;

        var builder = new IndexerBuilder( this.AspectLayerInstance, this.TargetDeclaration, hasGet, hasSet );

        foreach ( var pair in this._indices )
        {
            builder.AddParameter( pair.Name, pair.Type );
        }

        return builder;
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceIndexer;

    protected override void InitializeBuilderCore(
        IndexerBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context )
    {
        base.InitializeBuilderCore( builder, templateAttributeProperties, in context );

        var setTemplateDeclaration = this._setTemplate?.TemplateMember.GetDeclaration( this.SourceCompilation );
        var getTemplateDeclaration = this._getTemplate?.TemplateMember.GetDeclaration( this.SourceCompilation );

        var serviceProvider = context.ServiceProvider;

        if ( this._getTemplate != null )
        {
            Invariant.Assert( getTemplateDeclaration != null );

            var typeRewriter = TemplateTypeRewriter.Get( this._getTemplate );

            builder.Type = typeRewriter.Visit( getTemplateDeclaration.ReturnType );
        }
        else if ( this._setTemplate != null )
        {
            Invariant.Assert( setTemplateDeclaration != null );

            var lastRuntimeParameter = this._setTemplate.TemplateMember.TemplateClassMember.RunTimeParameters.LastOrDefault();

            var typeRewriter = TemplateTypeRewriter.Get( this._setTemplate );

            if ( lastRuntimeParameter != null )
            {
                // There may be an invalid template without runtime parameters.

                builder.Type = typeRewriter.Visit( setTemplateDeclaration.Parameters[lastRuntimeParameter.SourceIndex].Type );
            }
        }

        builder.Accessibility =
            this._getTemplate != null
                ? this._getTemplate.TemplateMember.Accessibility
                : this._setTemplate.AssertNotNull().TemplateMember.Accessibility;

        // Extern template denotes an abstract member of an interface.
        builder.IsAbstract = 
            this.TargetDeclaration.TypeKind is TypeKind.Interface 
            && (this._getTemplate != null
                ? getTemplateDeclaration.AssertNotNull().IsExtern
                : setTemplateDeclaration.AssertNotNull().IsExtern);

        if ( this._getTemplate != null )
        {
            Invariant.Assert( getTemplateDeclaration != null );

            CopyTemplateAttributes( getTemplateDeclaration, builder.GetMethod.AssertNotNull(), serviceProvider );

            CopyTemplateAttributes(
                getTemplateDeclaration.ReturnParameter,
                builder.GetMethod!.ReturnParameter,
                serviceProvider );
        }

        if ( this._setTemplate != null )
        {
            Invariant.Assert( setTemplateDeclaration != null );

            CopyTemplateAttributes( setTemplateDeclaration, builder.SetMethod!, serviceProvider );

            var lastRuntimeParameter = this._setTemplate.TemplateMember.TemplateClassMember.RunTimeParameters.LastOrDefault();

            if ( lastRuntimeParameter != null )
            {
                // There may be an invalid template without runtime parameters.

                CopyTemplateAttributes(
                    setTemplateDeclaration.Parameters[lastRuntimeParameter.SourceIndex],
                    builder.SetMethod.AssertNotNull().Parameters.Last(),
                    serviceProvider );
            }

            CopyTemplateAttributes(
                setTemplateDeclaration.ReturnParameter,
                builder.SetMethod.AssertNotNull().ReturnParameter,
                serviceProvider );
        }

        var (accessorTemplateForAttributeCopy, accessorTemplateDeclarationForAttributeCopy, skipLastParameter) =
            this._getTemplate == null
                ? (this._setTemplate!.TemplateMember, setTemplateDeclaration.AssertNotNull(), true)
                : (this._getTemplate.TemplateMember, getTemplateDeclaration.AssertNotNull(), false);

        var runtimeParameters = accessorTemplateForAttributeCopy.TemplateClassMember.RunTimeParameters;

        for ( var i = 0; i < runtimeParameters.Length - (skipLastParameter ? 1 : 0); i++ )
        {
            var runtimeParameter = runtimeParameters[i];
            var templateParameter = accessorTemplateDeclarationForAttributeCopy.Parameters[runtimeParameter.SourceIndex];
            var parameterBuilder = builder.Parameters[i];

            CopyTemplateAttributes( templateParameter, parameterBuilder, serviceProvider );
        }

        // TODO: For get accessor template, we are ignoring accessibility of set accessor template because it can be easily incompatible.
    }

    protected override void ValidateBuilder( IndexerBuilder builder, IDiagnosticAdder diagnosticAdder )
    {
        base.ValidateBuilder( builder, diagnosticAdder );

        var targetDeclaration = this.TargetDeclaration;

        if ( builder.Parameters.Count <= 0 )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceIndexerWithoutParameters.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }

        if ( builder.IsStatic )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceStaticIndexer.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }
    }

    protected override IntroductionAdviceResult<IIndexer> ImplementCore( IndexerBuilder builder, in AdviceImplementationContext context )
    {
        // Determine whether we need introduction transformation (something may exist in the original code or could have been introduced by previous steps).
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingDeclaration = targetDeclaration.FindClosestVisibleIndexer( builder );

        // TODO: Introduce attributes that are added not present on the existing member?
        if ( existingDeclaration == null )
        {
            // There is no existing declaration.

            builder.Freeze();

            context.AddTransformation( builder.CreateTransformation() );

            if ( (this._getTemplate ?? this._setTemplate)?.TemplateMember.TemplateClassMember.TemplateInfo.HasNoBody != true )
            {
                // Introduce and override using the template.
                var overrideIndexerTransformation = new OverrideIndexerTransformation(
                    this.AspectLayerInstance,
                    builder.ToFullRef(),
                    this._getTemplate?.ForIntroduction( builder.GetMethod ),
                    this._setTemplate?.ForIntroduction( builder.SetMethod ) );

                context.AddTransformation( overrideIndexerTransformation );
            }

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            if ( existingDeclaration is not { } existingIndexer )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceWithDifferentKind.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration, existingDeclaration.DeclarationKind),
                            this ) );
            }

            if ( !builder.Type.Equals( existingIndexer.Type ) )
            {
                return
                    this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceDifferentExistingReturnType.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                             existingIndexer.DeclaringType, existingIndexer.Type),
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
                                 existingIndexer.DeclaringType),
                                this ) );

                case OverrideStrategy.Ignore:
                    // Do nothing.
                    return this.CreateIgnoredResult( existingIndexer );

                case OverrideStrategy.New:
                    // If the existing declaration is in the current type, fail, otherwise, declare a new method and override.
                    if ( targetDeclaration.Equals( existingIndexer.DeclaringType ) )
                    {
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.CannotIntroduceNewMemberWhenItAlreadyExists.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, builder, existingIndexer.DeclaringType),
                                this ) );
                    }
                    else
                    {
                        builder.HasNewKeyword = builder.IsNew = true;
                        builder.OverriddenIndexer = existingIndexer;
                        builder.Freeze();

                        context.AddTransformation( builder.CreateTransformation() );

                        if ( (this._getTemplate ?? this._setTemplate)?.TemplateMember.TemplateClassMember.TemplateInfo.HasNoBody != true )
                        {
                            var overrideIndexerTransformation = new OverrideIndexerTransformation(
                                this.AspectLayerInstance,
                                builder.ToFullRef(),
                                this._getTemplate?.ForIntroduction( builder.GetMethod ),
                                this._setTemplate?.ForIntroduction( builder.SetMethod ) );

                            context.AddTransformation( overrideIndexerTransformation );
                        }

                        return this.CreateSuccessResult( AdviceOutcome.New, builder );
                    }

                case OverrideStrategy.Override:
                    if ( targetDeclaration.Equals( existingIndexer.DeclaringType ) )
                    {
                        var overrideIndexerTransformation = new OverrideIndexerTransformation(
                            this.AspectLayerInstance,
                            existingIndexer.ToFullRef(),
                            this._getTemplate?.ForIntroduction( existingIndexer.GetMethod ),
                            this._setTemplate?.ForIntroduction( existingIndexer.SetMethod ) );

                        context.AddTransformation( overrideIndexerTransformation );

                        return this.CreateSuccessResult( AdviceOutcome.Override, existingIndexer );
                    }
                    else if ( existingIndexer.IsSealed || !existingIndexer.IsOverridable() )
                    {
                        return
                            this.CreateFailedResult(
                                AdviceDiagnosticDescriptors.CannotIntroduceOverrideOfSealed.CreateRoslynDiagnostic(
                                    targetDeclaration.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration,
                                     existingIndexer.DeclaringType),
                                    this ) );
                    }
                    else
                    {
                        builder.IsOverride = true;
                        builder.HasNewKeyword = builder.IsNew = false;
                        builder.OverriddenIndexer = existingIndexer;
                        builder.Freeze();

                        var overriddenIndexer = new OverrideIndexerTransformation(
                            this.AspectLayerInstance,
                            builder.ToFullRef(),
                            this._getTemplate?.ForIntroduction( builder.GetMethod ),
                            this._setTemplate?.ForIntroduction( builder.SetMethod ) );

                        context.AddTransformation( builder.CreateTransformation() );
                        context.AddTransformation( overriddenIndexer );

                        return this.CreateSuccessResult( AdviceOutcome.Override, builder );
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}