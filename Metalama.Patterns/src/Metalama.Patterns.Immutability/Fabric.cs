// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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