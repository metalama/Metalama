// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Metrics
{
    /// <summary>
    /// Exposes metrics to eligible objects.
    /// </summary>
    [CompileTime]
    public static class MetricsExtensions
    {
        /// <summary>
        /// Gets an object that allows to get metrics.
        /// </summary>
        /// <param name="extensible">The object which metrics are requested.</param>
        /// <typeparam name="TExtensible">The type of object for which objects are requested.</typeparam>
        /// <returns></returns>
        public static Metrics<TExtensible> Metrics<TExtensible>( this TExtensible extensible )
            where TExtensible : IMeasurable
            => new( extensible );
    }
}