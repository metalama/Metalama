// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;

namespace Metalama.Framework.Metrics
{
    /// <summary>
    /// Implements the computation or reading of a metric.
    /// </summary>
    /// <typeparam name="T">Type of the metric handled by the current provider.</typeparam>
    public interface IMetricProvider<out T> : IProjectService
        where T : IMetric
    {
        /// <summary>
        /// Computes and returns the metric for a given object.
        /// </summary>
        /// <param name="measurable">An object on which the metric is defined.</param>
        /// <returns>The metric value for <paramref name="measurable"/>.</returns>
        T GetMetric( IMeasurable measurable );
    }
}