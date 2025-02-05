// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Advising;

internal sealed class AdviceRequireAspectSource : IAspectSource
{
    private readonly IAspectClass _aspectClass;
    private readonly AspectRequirement _requirement;

    public AdviceRequireAspectSource( AspectRequirement requirement, IAspectClass aspectClass )
    {
        this._requirement = requirement;
        this._aspectClass = aspectClass;
    }

    public ImmutableArray<IAspectClass> AspectClasses => ImmutableArray.Create( this._aspectClass );

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        collector.AddAspectRequirement( this._requirement );

        return Task.CompletedTask;
    }
}