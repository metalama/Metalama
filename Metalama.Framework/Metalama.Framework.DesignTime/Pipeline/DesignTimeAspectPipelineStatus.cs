// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline
{
    /// <summary>
    /// Statuses of a <see cref="DesignTimeAspectPipeline"/> instance.
    /// </summary>
    internal enum DesignTimeAspectPipelineStatus
    {
        /// <summary>
        /// The pipeline has never been successfully initialized.
        /// </summary>
        Default,

        /// <summary>
        /// The pipeline has a working configuration.
        /// </summary>
        Ready,

        /// <summary>
        /// The compile-time code is being edited, so the pipeline is paused and serving cached results only.
        /// </summary>
        Paused
    }
}