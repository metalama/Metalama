// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.AspectWeavers;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// The static configuration of a <see cref="Metalama.Framework.Engine.Pipeline.PipelineStage"/>.
    /// </summary>
    internal sealed record PipelineStageConfiguration( PipelineStageKind Kind, ImmutableArray<OrderedAspectLayer> AspectLayers, IAspectWeaver? Weaver );
}