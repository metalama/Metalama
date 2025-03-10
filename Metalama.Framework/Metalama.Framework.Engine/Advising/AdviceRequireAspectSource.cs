// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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