// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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