// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Metrics
{
    /// <summary>
    /// The default implementation of <see cref="IMetricProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of metric.</typeparam>
    public abstract class MetricProvider<T> : IMetricProvider<T>
        where T : struct, IMetric
    {
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