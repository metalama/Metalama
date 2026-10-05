// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        /// <summary>
        /// Gets or sets a value indicating whether this stage is the first high-level stage of the current pipeline execution, which starts from the
        /// source compilation.
        /// </summary>
        /// <remarks>
        /// A stage object is created for each pipeline execution, so the property can be set by the pipeline before the stage executes. The hooks
        /// of the pipeline extensions run only in this stage. The system layers precede the user layers, so no low-level aspect weaver executes
        /// before it.
        /// </remarks>
        internal bool IsFirstHighLevelStage { get; set; }

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