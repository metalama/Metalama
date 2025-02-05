// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Extensibility;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Fabrics
{
    internal sealed record StaticFabricResult( ImmutableArray<IPipelineContributor> Contributors )
    {
        public static StaticFabricResult Empty { get; } = new( ImmutableArray<IPipelineContributor>.Empty );
    }
}