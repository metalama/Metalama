// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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