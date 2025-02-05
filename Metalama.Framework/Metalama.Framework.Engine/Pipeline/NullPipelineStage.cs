// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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