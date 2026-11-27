// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class PullConstructorParameterTransitiveAspect : IAspect<INamedType>
{
    private readonly IPullStrategy? _pullStrategy;

    /// <summary>
    /// The parameter of the base constructor whose value the derived constructor is made to pull.
    /// </summary>
    /// <remarks>
    /// This reference is durable, like <see cref="TransitiveAspectInstance.TargetDeclaration"/>, so that a transitive
    /// aspect instance stored in the design-time pipeline results does not retain the version of the project it was
    /// produced in. See issue #1797.
    /// </remarks>
    private readonly IDurableRef<IParameter> _parameter;

    private readonly int _order;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;

    public PullConstructorParameterTransitiveAspect(
        IPullStrategy? pullStrategy,
        IDurableRef<IParameter> parameter,
        int order,
        IConstructorOverloadingStrategy? overloadingStrategy )
    {
        this._pullStrategy = pullStrategy;
        this._parameter = parameter;
        this._order = order;
        this._overloadingStrategy = overloadingStrategy;
    }

    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var allInstances = builder.AspectInstance.SecondaryInstances.Select( x => (PullConstructorParameterTransitiveAspect) x.Aspect )
            .Concat( this )
            .OrderBy( a => a._order );

        var internalBuilder = (AspectBuilder<INamedType>) builder;

        var isDesignTime = MetalamaExecutionContext.Current.ExecutionScenario.IsDesignTime;

        foreach ( var instance in allInstances )
        {
            IParameter? parameter;

            if ( isDesignTime )
            {
                // At design time, the consuming project sees the referenced project through its source code and the code
                // that the design-time pipeline generated for it. When the base type is not partial, the design-time
                // pipeline cannot add the constructor overload that has the parameter, so the parameter is not visible
                // and there is nothing to pull. See issue #2425.
                parameter = instance._parameter.GetTargetOrNull( internalBuilder.AdviceFactory.MutableCompilation );

                if ( parameter == null )
                {
                    continue;
                }
            }
            else
            {
                parameter = instance._parameter.GetTarget( internalBuilder.AdviceFactory.MutableCompilation );
            }

            internalBuilder.AdviceFactory.PullParameter( parameter, instance._pullStrategy, instance._overloadingStrategy );
        }
    }

    public static IBoundAspectClass CreateAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation )
        => new SystemAspectClass(
            serviceProvider,
            compilation,
            $"<{nameof(PullConstructorParameterTransitiveAspect)}>",
            typeof(PullConstructorParameterTransitiveAspect) );
}