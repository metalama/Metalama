// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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