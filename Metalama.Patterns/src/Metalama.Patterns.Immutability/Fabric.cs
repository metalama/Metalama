// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Immutability.Configuration;
using System.Collections.Immutable;

namespace Metalama.Patterns.Immutability;

[UsedImplicitly]
internal class Fabric : TransitiveProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        var classifier = new ImmutableCollectionClassifier();

        amender.SelectReflectionTypes(
                typeof(IImmutableDictionary<,>),
                typeof(IImmutableList<>),
                typeof(IImmutableQueue<>),
                typeof(IImmutableSet<>),
                typeof(IImmutableStack<>),
                typeof(ImmutableArray<>),
                typeof(ImmutableDictionary<,>),
                typeof(ImmutableHashSet<>),
                typeof(ImmutableList<>),
                typeof(ImmutableSortedSet<>),
                typeof(ImmutableStack<>) )
            .ConfigureImmutability( classifier );
    }
}