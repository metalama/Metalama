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

        // If we have an implicit constructor, make it explicit.
        if ( constructor.IsImplicitInstanceConstructor() )
        {
            var constructorBuilder = new ConstructorBuilder( this.AspectLayerInstance, constructor );

            constructorBuilder.Freeze();

            initializedConstructor = constructorBuilder;
            context.AddTransformation( constructorBuilder.CreateTransformation() );
        }

        // Create the parameter.
        // An uninitialized TypedConstant (i.e. default(TypedConstant)) means "required parameter, no C# default value".
        var parameterBuilder = new ParameterBuilder(
            initializedConstructor,
            initializedConstructor.Parameters.Count,
            this._parameterName,
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

        context.AddTransformation( new IntroduceParameterTransformation( this.AspectLayerInstance, parameterBuilderData ) );

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

        return new IntroductionAdviceResult<IParameter>(
            AdviceKind.IntroduceParameter,
            AdviceOutcome.Default,
            this.AdviceFactory,
            parameterBuilderData.ToRef() );
    }

    protected override IntroductionAdviceResult<IParameter> CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceKind.IntroduceParameter, AdviceOutcome.Error, this.AdviceFactory, reportedDiagnostics: diagnostics );
}