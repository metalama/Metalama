// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// A <see cref="PipelineStage"/> that groups all aspects written with the high-level API instead of
    /// the <see cref="IAspectWeaver"/>.
    /// </summary>
    internal abstract class HighLevelPipelineStage : PipelineStage
    {
        private readonly IReadOnlyList<OrderedAspectLayer> _aspectLayers;

        protected HighLevelPipelineStage( IReadOnlyList<OrderedAspectLayer> aspectLayers )
        {
            this._aspectLayers = aspectLayers;
        }

        /// <inheritdoc/>
        public override async Task<FallibleResult<AspectPipelineResult>> ExecuteAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            IDiagnosticAdder diagnostics,
            TestableCancellationToken cancellationToken )
        {
            var compilation = input.LastCompilationModel;

            pipelineConfiguration.ServiceProvider.GetService<ICompilationModelObserver>()?.OnInitialCompilationModelCreated( compilation );

            var pipelineStepsState = new PipelineStepsState(
                this._aspectLayers,
                compilation,
                input.ContributorSources,
                pipelineConfiguration,
                cancellationToken );

            var pipelineStepsResult = await pipelineStepsState.ExecuteAsync( cancellationToken );

            return await this.GetStageResultAsync( pipelineConfiguration, input, pipelineStepsResult, cancellationToken );
        }

        /// <summary>
        /// Generates the code required by the aspects whose execution resulted in a given <see cref="PipelineStepsResult"/>, and combine it with an input
        /// <see cref="AspectPipelineResult"/> to produce an output <see cref="AspectPipelineResult"/>.
        /// </summary>
        /// <param name="pipelineConfiguration"></param>
        /// <param name="input"></param>
        /// <param name="pipelineStepsResult"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<AspectPipelineResult> GetStageResultAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepsResult,
            TestableCancellationToken cancellationToken );
    }
}