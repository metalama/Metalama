// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Generic;
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

    public IEnumerable<IAspectClass> AspectClasses => ImmutableArray.Create<IAspectClass>( this._aspect.AspectClass );

    public bool ContainsAspectClass( IAspectClass aspectClass ) => this._aspect.AspectClass == aspectClass;

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        collector.AddAspectInstance( this._aspect );

        return Task.CompletedTask;
    }

    public ContributorKind ContributorKind => ContributorKind.AspectSource;
}