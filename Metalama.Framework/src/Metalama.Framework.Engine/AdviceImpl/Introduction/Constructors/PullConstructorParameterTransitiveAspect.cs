// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Services;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class PullConstructorParameterTransitiveAspect : IAspect<INamedType>
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IRef<IParameter> _parameter;
    private readonly int _order;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;
    private readonly IRef<IConstructor> _declaringConstructor;
    private readonly string _parameterName;
    private readonly IRef<IType> _parameterType;
    private readonly int _parameterIndex;
    private readonly RefKind _parameterRefKind;

    public PullConstructorParameterTransitiveAspect(
        IPullStrategy? pullStrategy,
        IRef<IParameter> parameter,
        int order,
        IConstructorOverloadingStrategy? overloadingStrategy,
        IRef<IConstructor> declaringConstructor,
        string parameterName,
        IRef<IType> parameterType,
        int parameterIndex,
        RefKind parameterRefKind )
    {
        this._pullStrategy = pullStrategy;
        this._parameter = parameter;
        this._order = order;
        this._overloadingStrategy = overloadingStrategy;
        this._declaringConstructor = declaringConstructor;
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._parameterIndex = parameterIndex;
        this._parameterRefKind = parameterRefKind;
    }

    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var allInstances = builder.AspectInstance.SecondaryInstances.Select( x => (PullConstructorParameterTransitiveAspect) x.Aspect )
            .Concat( this )
            .OrderBy( a => a._order );

        var internalBuilder = (AspectBuilder<INamedType>) builder;

        foreach ( var instance in allInstances )
        {
            if ( !instance.TryGetPulledParameter( internalBuilder, out var parameter ) )
            {
                continue;
            }

            internalBuilder.AdviceFactory.PullParameter( parameter, instance._pullStrategy, instance._overloadingStrategy );
        }
    }

    /// <summary>
    /// Gets the parameter that has to be pulled into the constructors of the derived types of the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference to the pulled parameter resolves only where the producing project is visible in its transformed
    /// shape, which is the case when the consumer is compiled against the producer's output assembly. At design time
    /// the consumer sees the producer before its transformation, because
    /// <c>SymbolRef.Strategy.IsValidSymbol</c> hides the members that the Metalama source generator produced, so the
    /// pulled parameter is not part of the consumer's code model and the reference does not resolve. The parameter is
    /// then rebuilt from the description carried alongside the reference. The resulting parameter is not added to the
    /// compilation: it only describes the parameter of the base constructor to which the derived constructors have to
    /// pass a value.
    /// </para>
    /// </remarks>
    private bool TryGetPulledParameter( AspectBuilder<INamedType> builder, [NotNullWhen( true )] out IParameter? parameter )
    {
        var compilation = builder.AdviceFactory.MutableCompilation;

        if ( this._parameter.GetTargetOrNull( compilation ) is { } resolvedParameter )
        {
            parameter = resolvedParameter;

            return true;
        }

        if ( this._declaringConstructor.GetTargetOrNull( compilation ) is not { } declaringConstructor
             || this._parameterType.GetTargetOrNull( compilation ) is not { } parameterType )
        {
            parameter = null;

            return false;
        }

        var parameterBuilder = new ParameterBuilder(
            declaringConstructor,
            this._parameterIndex,
            this._parameterName,
            parameterType,
            this._parameterRefKind,
            builder.AdviceFactory.AspectLayerInstance );

        parameterBuilder.Freeze();

        parameter = parameterBuilder;

        return true;
    }

    public static IBoundAspectClass CreateAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation )
        => new SystemAspectClass(
            serviceProvider,
            compilation,
            $"<{nameof(PullConstructorParameterTransitiveAspect)}>",
            typeof(PullConstructorParameterTransitiveAspect) );
}
