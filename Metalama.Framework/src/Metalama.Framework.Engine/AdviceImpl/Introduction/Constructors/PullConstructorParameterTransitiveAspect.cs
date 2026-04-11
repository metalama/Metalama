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
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class PullConstructorParameterTransitiveAspect : IAspect<INamedType>
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IRef<IParameter> _parameter;
    private readonly int _order;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;

    public PullConstructorParameterTransitiveAspect(
        IPullStrategy? pullStrategy,
        IRef<IParameter> parameter,
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

        foreach ( var instance in allInstances )
        {
            var parameter = instance._parameter.GetTarget( internalBuilder.AdviceFactory.MutableCompilation );
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