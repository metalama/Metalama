// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class IntroduceConstructorParameterTransitiveAspect : IAspect<INamedType>, ITransitiveAspect
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IReadOnlyList<IRef<IParameter>> _parameters;
    private readonly int _order;

    public IntroduceConstructorParameterTransitiveAspect(
        IPullStrategy? pullStrategy,
        IReadOnlyList<IRef<IParameter>> parameters,
        int order )
    {
        this._pullStrategy = pullStrategy;
        this._parameters = parameters;
        this._order = order;
    }

    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var allInstances = builder.AspectInstance.SecondaryInstances.Select( x => (IntroduceConstructorParameterTransitiveAspect) x.Aspect )
            .Concat( this )
            .OrderBy( a => a._order );

        var internalBuilder = (AspectBuilder<INamedType>) builder;

        foreach ( var instance in allInstances )
        {
            foreach ( var parameterRef in instance._parameters )
            {
                var parameter = parameterRef.GetTarget( internalBuilder.AdviceFactory.MutableCompilation );
                internalBuilder.AdviceFactory.PullParameter( parameter, this._pullStrategy );
            }
        }
    }

    public PipelineContributorKind Kind => PipelineContributorKind.TransitiveAspect;
}