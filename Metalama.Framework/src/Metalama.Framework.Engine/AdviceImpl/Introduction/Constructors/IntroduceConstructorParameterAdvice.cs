// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class IntroduceConstructorParameterAdvice : Advice<IntroductionAdviceResult<IParameter>>
{
    private readonly string _parameterName;
    private readonly IType _parameterType;
    private readonly Action<ParameterBuilder>? _buildAction;
    private readonly IPullStrategy? _pullStrategy;
    private readonly TypedConstant _defaultValue;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;

    public IntroduceConstructorParameterAdvice(
        in AdviceConstructorParameters<IConstructor> parameters,
        string parameterName,
        IType parameterType,
        Action<ParameterBuilder>? buildAction,
        IPullStrategy? pullStrategy,
        TypedConstant defaultValue,
        IConstructorOverloadingStrategy? overloadingStrategy = null )
        : base( parameters )
    {
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._buildAction = buildAction;
        this._pullStrategy = pullStrategy;
        this._defaultValue = defaultValue;
        this._overloadingStrategy = overloadingStrategy;
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceParameter;

    protected override IntroductionAdviceResult<IParameter> Implement( AdviceImplementationContext context )
    {
        var compilation = context.MutableCompilation;

        var constructor = (IConstructor) this.TargetDeclaration.ForCompilation( compilation );
        var initializedConstructor = constructor;

        var existingParameter = constructor.Parameters.FirstOrDefault( p => p.Name == this._parameterName );

        if ( existingParameter != null )
        {
            if ( existingParameter.Origin.Kind == DeclarationOriginKind.Source )
            {
                // Source-defined parameter with the same name. We cannot modify or replace it.
                // Check if there's an aspect-introduced parameter with a compatible type that we
                // should reuse or replace instead (e.g. from a base-class pull with a less-specific type).
                var introducedCompatible = constructor.Parameters.FirstOrDefault(
                    p => p.Origin is IAspectDeclarationOrigin ao
                         && ao.AspectInstance != this.AspectInstance
                         && this._parameterType.IsConvertibleTo( p.Type ) );

                if ( introducedCompatible != null )
                {
                    if ( !introducedCompatible.Type.IsConvertibleTo( this._parameterType ) )
                    {
                        // Strictly more specific → replace.
                        return this.ReplaceExistingParameter( context, constructor, introducedCompatible );
                    }
                    else
                    {
                        // Compatible (identical or mutually convertible) → reuse.
                        return new IntroductionAdviceResult<IParameter>(
                            AdviceKind.IntroduceParameter,
                            AdviceOutcome.Default,
                            this.AdviceFactory,
                            introducedCompatible.ToRef() );
                    }
                }

                // No compatible introduced parameter found — introduce a new one with a deduplicated name.
                return this.IntroduceNewParameter( context, constructor );
            }

            // When the existing parameter was introduced by a DIFFERENT aspect instance and the new type
            // is compatible, we can reuse or replace it. This occurs when an inheritable aspect's pull
            // strategy propagated a parameter from a base class, and now the derived class's own aspect
            // encounters it.
            // When the same aspect instance introduces the same name twice, it's always a duplicate error.
            var isFromDifferentAspect = existingParameter.Origin is IAspectDeclarationOrigin aspectOrigin
                                        && aspectOrigin.AspectInstance != this.AspectInstance;

            if ( isFromDifferentAspect
                 && this._parameterType.IsConvertibleTo( existingParameter.Type ) )
            {
                if ( !existingParameter.Type.IsConvertibleTo( this._parameterType ) )
                {
                    // The new type is strictly more specific (e.g. ILogger<Derived> → ILogger<Base> via covariance).
                    // Replace the existing parameter's type.
                    return this.ReplaceExistingParameter( context, constructor, existingParameter );
                }
                else
                {
                    // The existing parameter has a compatible type (identical or mutually convertible).
                    // No replacement needed — just return it.
                    return new IntroductionAdviceResult<IParameter>(
                        AdviceKind.IntroduceParameter,
                        AdviceOutcome.Default,
                        this.AdviceFactory,
                        existingParameter.ToRef() );
                }
            }

            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotIntroduceParameterAlreadyExists.CreateRoslynDiagnostic(
                    constructor.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, this._parameterName, constructor, existingParameter.Name),
                    this ) );
        }

        // Introducing parameters into static constructors is not allowed.
        if ( constructor.IsStatic )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotIntroduceParameterIntoStaticConstructor.CreateRoslynDiagnostic(
                    constructor.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, constructor),
                    this ) );
        }

        return this.IntroduceNewParameter( context, constructor );
    }

    /// <summary>
    /// Replaces the type of an existing introduced parameter with <see cref="_parameterType"/> and
    /// re-runs the pull strategy so that further-derived constructors see the updated type.
    /// </summary>
    private IntroductionAdviceResult<IParameter> ReplaceExistingParameter(
        AdviceImplementationContext context,
        IConstructor constructor,
        IParameter existingParameter )
    {
        var replaceParameterBuilder = PullConstructorParameterAdviceImpl.CreateReplacementParameter(
            context,
            this.AspectLayerInstance,
            constructor,
            existingParameter,
            this._parameterType,
            this._defaultValue.IsInitialized ? this._defaultValue : null,
            this._buildAction );

        // Re-run the pull strategy so that derived constructors that already received
        // the old less-specific type get updated to the new more-specific type.
        var effectivePullStrategy = this._pullStrategy
                                    ?? (this._defaultValue.IsInitialized
                                        ? null
                                        : new IntroduceParameterPullStrategy( null, null, null ));

        var forwardingHelper = new ForwardingConstructorHelper(
            context,
            this.AspectLayerInstance,
            this._overloadingStrategy,
            this._pullStrategy,
            this );

        var impl = new PullConstructorParameterAdviceImpl(
            context,
            effectivePullStrategy,
            this.AspectLayerInstance,
            false,
            forwardingHelper );

        impl.PullConstructorParameterRecursive( replaceParameterBuilder );

        return new IntroductionAdviceResult<IParameter>(
            AdviceKind.IntroduceParameter,
            AdviceOutcome.Default,
            this.AdviceFactory,
            replaceParameterBuilder.BuilderData.ToRef() );
    }

    /// <summary>
    /// Introduces a brand-new parameter (the normal path when no existing same-name parameter exists).
    /// </summary>
    private IntroductionAdviceResult<IParameter> IntroduceNewParameter(
        AdviceImplementationContext context,
        IConstructor constructor )
    {
        var initializedConstructor = constructor;

        // If we have an implicit constructor, make it explicit.
        if ( constructor.IsImplicitInstanceConstructor() )
        {
            var constructorBuilder = new ConstructorBuilder( this.AspectLayerInstance, constructor );

            constructorBuilder.Freeze();

            initializedConstructor = constructorBuilder;
            context.AddTransformation( constructorBuilder.CreateTransformation() );
        }

        // Create the parameter, deduplicating the name if it collides with a source-defined parameter.
        // An uninitialized TypedConstant (i.e. default(TypedConstant)) means "required parameter, no C# default value".
        var parameterName = PullConstructorParameterAdviceImpl.GetUniqueParameterName( initializedConstructor, this._parameterName );

        var parameterBuilder = new ParameterBuilder(
            initializedConstructor,
            initializedConstructor.Parameters.Count,
            parameterName,
            this._parameterType,
            RefKind.None,
            this.AspectLayerInstance ) { DefaultValue = this._defaultValue.IsInitialized ? this._defaultValue : null };

        var parameter = parameterBuilder;

        this._buildAction?.Invoke( parameterBuilder );

        if ( constructor.CanBeChainedFromOutsideAssembly() )
        {
            parameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
        }

        parameterBuilder.Freeze();
        var parameterBuilderData = parameterBuilder.BuilderData;

        // The MaterializeOnRecord flag is a property of the parameter introduction itself, not of any individual
        // pull call. For the top-level target we read it from the user's pull strategy (if it is the standard
        // IntroduceParameterPullStrategy); custom IPullStrategy implementations do not carry this concept, so
        // non-materialized (false) is used as the default on a record primary.
        var materializeOnRecord = (this._pullStrategy as IntroduceParameterPullStrategy)?.MaterializeOnRecord ?? false;

        context.AddTransformation( new IntroduceParameterTransformation( this.AspectLayerInstance, parameterBuilderData, materializeOnRecord ) );

        // Determine the effective pull strategy.
        // - If the user supplied one, use it.
        // - Otherwise, if the parameter is required (no C# default value), synthesize a default strategy
        //   that propagates the parameter through chained constructors. Required parameters need a value
        //   at every chain-call site, so the "do nothing" fallback does not work.
        // - Otherwise (optional parameter, no strategy), keep the legacy null-strategy behavior.
        var effectivePullStrategy = this._pullStrategy
                                    ?? (this._defaultValue.IsInitialized
                                        ? null
                                        : new IntroduceParameterPullStrategy( null, null, null ));

        // Build the forwarding-constructor helper (no-op if the overloading strategy is null).
        var forwardingHelper = new ForwardingConstructorHelper(
            context,
            this.AspectLayerInstance,
            this._overloadingStrategy,
            this._pullStrategy,
            this );

        // Generate a forwarding constructor for the target if the overloading strategy asks for one.
        forwardingHelper.ApplyForwarderIfNeeded( initializedConstructor, parameter );

        // Pull from constructors that call the current constructor, and recursively.
        var impl = new PullConstructorParameterAdviceImpl(
            context,
            effectivePullStrategy,
            this.AspectLayerInstance,
            false,
            forwardingHelper );

        impl.PullConstructorParameterRecursive( parameter );

        // Note: when both a :this-chained caller and its target receive aspect-introduced parameters
        // with the same name (in either advice order), the recursive pull above emits an initializer
        // argument carrying a ForwardParameterName hint. The hint is resolved at LinkerInjectionStep
        // Sort() time once every aspect-introduced parameter is visible on the target ctor, so no
        // post-pull override is needed here. OnConstructedEpilogueEmitter still uses isOverride: true
        // legitimately — that is a cross-aspect rewrite of the pulled 'context' argument.

        return new IntroductionAdviceResult<IParameter>(
            AdviceKind.IntroduceParameter,
            AdviceOutcome.Default,
            this.AdviceFactory,
            parameterBuilderData.ToRef() );
    }

    protected override IntroductionAdviceResult<IParameter> CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceKind.IntroduceParameter, AdviceOutcome.Error, this.AdviceFactory, reportedDiagnostics: diagnostics );
}