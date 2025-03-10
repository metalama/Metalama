// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// An <see cref="IAspectSource"/> that stores aspect sources that are not a part of the current <see cref="PipelineStage"/>.
/// </summary>
internal sealed class OverflowAspectSource : IAspectSource
{
    private readonly ConcurrentLinkedList<(IAspectSource Source, IAspectClass AspectClass)> _aspectSources = new();

    public ImmutableArray<IAspectClass> AspectClasses => this._aspectSources.SelectAsReadOnlyCollection( a => a.AspectClass ).Distinct().ToImmutableArray();

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        var tasks =
            this._aspectSources
                .Where( s => s.AspectClass.FullName == collector.AspectClass.FullName )
                .Select( a => a.Source )
                .Distinct()
                .Select( a => a.CollectAspectInstancesAsync( collector ) );

        return Task.WhenAll( tasks );
    }

    public void Add( IAspectSource aspectSource, IAspectClass aspectClass ) => this._aspectSources.Add( (aspectSource, aspectClass) );
}