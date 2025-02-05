// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Metrics
{
    /// <summary>
    /// A weakly-typed base interface for <see cref="IMetric{T}"/>. Never implement directly. Always implement <see cref="IMetric{T}"/>.
    /// </summary>
    [CompileTime]
    public interface IMetric;

    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Base interface for all metrics. This interface exists only for strong typing. It does not have any members.
    /// </summary>
    /// <remarks>
    /// When you implement you own metric, you also need to implement the <see cref="IMetricProvider{T}"/> interface,
    /// and you need to add the service to the service provider (TODO).
    /// </remarks>
    /// <typeparam name="T">The type of objects for which the metric applies.</typeparam>
    public interface IMetric<in T> : IMetric
        where T : IMeasurable;
}