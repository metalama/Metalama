// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// Groups a set of transformations that, within the group, do not require the Roslyn compilation
    /// to be updated.
    /// </summary>
    internal abstract class PipelineStage
    {
        /// <summary>
        /// Executes the pipeline, i.e. transforms inputs into outputs.
        /// </summary>
        public abstract Task<FallibleResult<AspectPipelineResult>> ExecuteAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            IDiagnosticAdder diagnostics,
            TestableCancellationToken cancellationToken );
    }
}