// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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