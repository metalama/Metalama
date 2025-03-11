// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// An implementation of <see cref="HighLevelPipelineStage"/> that does not do anything but append results.
    /// </summary>
    internal sealed class NullPipelineStage : HighLevelPipelineStage
    {
        public NullPipelineStage( IReadOnlyList<OrderedAspectLayer> aspectLayers ) :
            base( aspectLayers ) { }

        protected override Task<AspectPipelineResult> GetStageResultAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepsResult,
            TestableCancellationToken cancellationToken )
            => Task.FromResult(
                new AspectPipelineResult(
                    input.LastCompilation,
                    input.Project,
                    input.AspectLayers,
                    input.FirstCompilationModel.AssertNotNull(),
                    pipelineStepsResult.LastCompilation,
                    input.Configuration,
                    input.Diagnostics.Concat( pipelineStepsResult.Diagnostics ),
                    input.ContributorSources with { Contributors = input.ContributorSources.Contributors.Add( pipelineStepsResult.OverflowAspectSource ) },
                    pipelineStepsResult.InheritableAspectInstances.Concat( pipelineStepsResult.InheritableAspectInstances ).ToImmutableArray(),
                    additionalSyntaxTrees: input.AdditionalSyntaxTrees,
                    aspectInstanceResults: input.AspectInstanceResults.AddRange( pipelineStepsResult.AspectInstanceResults ) ) );
    }
}