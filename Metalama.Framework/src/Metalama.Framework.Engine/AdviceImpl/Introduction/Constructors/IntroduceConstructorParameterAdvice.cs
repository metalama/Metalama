// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
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

    public IntroduceConstructorParameterAdvice(
        in AdviceConstructorParameters<IConstructor> parameters,
        string parameterName,
        IType parameterType,
        Action<ParameterBuilder>? buildAction,
        IPullStrategy? pullStrategy,
        TypedConstant defaultValue )
        : base( parameters )
    {
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._buildAction = buildAction;
        this._pullStrategy = pullStrategy;
        this._defaultValue = defaultValue;
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
        var parameterBuilder = new ParameterBuilder(
            initializedConstructor,
            initializedConstructor.Parameters.Count,
            this._parameterName,
            this._parameterType,
            RefKind.None,
            this.AspectLayerInstance ) { DefaultValue = this._defaultValue };

        var parameter = parameterBuilder;

        this._buildAction?.Invoke( parameterBuilder );

        if ( constructor.CanBeChainedFromOutsideAssembly() )
        {
            parameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
        }

        parameterBuilder.Freeze();
        var parameterBuilderData = parameterBuilder.BuilderData;

        context.AddTransformation( new IntroduceParameterTransformation( this.AspectLayerInstance, parameterBuilderData ) );

        // Pull from constructors that call the current constructor, and recursively.
        var impl = new PullConstructorParameterAdviceImpl( context, this._pullStrategy, this.AspectLayerInstance, false );
        impl.PullConstructorParameterRecursive( parameter );

        return new IntroductionAdviceResult<IParameter>(
            AdviceKind.IntroduceParameter,
            AdviceOutcome.Default,
            this.AdviceFactory,
            parameterBuilderData.ToRef(),
            null );
    }

    protected override IntroductionAdviceResult<IParameter> CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new IntroductionAdviceResult<IParameter>( AdviceKind.IntroduceParameter, AdviceOutcome.Error, this.AdviceFactory, reportedDiagnostics: diagnostics );
}