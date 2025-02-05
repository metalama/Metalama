// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// An implementation of <see cref="IAspectSource"/> that adds an <see cref="IAspect{T}"/>.
/// </summary>
internal sealed class AdviceAddAspectSource : IAspectSource
{
    private readonly AspectInstance _aspect;

    public AdviceAddAspectSource( AspectInstance aspect )
    {
        this._aspect = aspect;
    }

    public ImmutableArray<IAspectClass> AspectClasses => ImmutableArray.Create<IAspectClass>( this._aspect.AspectClass );

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        collector.AddAspectInstance( this._aspect );

        return Task.CompletedTask;
    }
}