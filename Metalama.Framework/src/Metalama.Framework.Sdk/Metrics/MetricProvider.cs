// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Metrics
{
    /// <summary>
    /// Base class for implementing custom metric providers. Derive from this class to create metrics that
    /// can measure code characteristics like complexity, coupling, or custom patterns.
    /// </summary>
    /// <typeparam name="T">The metric type, which must be a struct implementing <see cref="IMetric"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// To create a custom metric:
    /// </para>
    /// <list type="number">
    /// <item><description>Create a metric struct implementing <see cref="IMetric{TTarget}"/> for each declaration type it supports.</description></item>
    /// <item><description>Create a metric provider class deriving from <see cref="MetricProvider{T}"/> (or <see cref="SyntaxMetricProvider{T}"/> for syntax-based metrics).</description></item>
    /// <item><description>Annotate the provider class with <see cref="Metalama.Framework.Engine.MetalamaPlugInAttribute"/>.</description></item>
    /// </list>
    /// <para>
    /// Override the following methods:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ComputeMetricForType"/>: Compute the metric for a type.</description></item>
    /// <item><description><see cref="ComputeMetricForMember"/>: Compute the metric for a member.</description></item>
    /// <item><description><see cref="Aggregate"/>: Combine metric values when aggregating across members or types.</description></item>
    /// </list>
    /// <para>
    /// For syntax-based metrics that analyze syntax trees, use <see cref="SyntaxMetricProvider{T}"/> instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="SyntaxMetricProvider{T}"/>
    /// <seealso cref="IMetric"/>
    /// <seealso href="@custom-metrics"/>
    public abstract class MetricProvider<T> : IMetricProvider<T>
        where T : struct, IMetric
    {
        /// <summary>
        /// Gets the metric for a measurable code element.
        /// </summary>
        /// <param name="measurable">The code element to measure (type, member, namespace, or compilation).</param>
        /// <returns>The computed metric value.</returns>
        /// <exception cref="InvalidOperationException">The metric cannot be computed for the given target type.</exception>
        /// <remarks>
        /// This method dispatches to the appropriate compute method based on the target type:
        /// <see cref="ComputeMetricForType"/> for types, <see cref="ComputeMetricForMember"/> for members,
        /// and aggregates results for namespaces and compilations.
        /// </remarks>
        public T GetMetric( IMeasurable measurable )
        {
            switch ( measurable )
            {
                case INamedType namedType:
                    return this.ComputeMetricForType( namedType );

                case IMember member:
                    return this.ComputeMetricForMember( member );

                case INamespace ns:
                    return this.ComputeMetricForTypes( ns.Types );

                case ICompilation compilation:
                    return this.ComputeMetricForTypes( compilation.Types );

                default:
                    throw new InvalidOperationException( $"The metric cannot be computed on a target of type '{measurable.GetType().Name}'." );
            }
        }

        // TODO: implement caching. Currently the lifetime of IMetricProvider and MetricManager is not well defined.
        // When used from workspaces, caching should persist across queries, but should be cleared when Reload is called.

        // TODO: not sure how to handle nested types. They can be included or excluded. Currently, they are included.

        /// <summary>
        /// Aggregates a new value into an aggregate value.
        /// </summary>
        /// <param name="aggregate">The aggregate value.</param>
        /// <param name="newValue">The new value.</param>
        protected abstract void Aggregate( ref T aggregate, in T newValue );

        /// <summary>
        /// Computes the metric for a whole type.
        /// </summary>
        protected abstract T ComputeMetricForType( INamedType namedType );

        /// <summary>
        /// Computes the metric for a member.
        /// </summary>
        protected abstract T ComputeMetricForMember( IMember member );

        private T ComputeMetricForTypes( IEnumerable<INamedType> types )
        {
            var aggregate = default(T);

            foreach ( var t in types )
            {
                this.Aggregate( ref aggregate, this.ComputeMetricForType( t ) );
            }

            return aggregate;
        }
    }
}