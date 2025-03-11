// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Metrics
{
    /// <summary>
    /// Exposes a <see cref="Get{TExtension}"/> method, which computes and returns a metric for an eligible object.
    /// </summary>
    /// <typeparam name="TMeasurable">The type extended with metrics.</typeparam>
    [CompileTime]
    public readonly struct Metrics<TMeasurable>
        where TMeasurable : IMeasurable
    {
        private readonly TMeasurable _extensible;

        internal Metrics( TMeasurable extensible )
        {
            this._extensible = extensible;
        }

        /// <summary>
        /// Gets a given metric for the current object.
        /// </summary>
        /// <typeparam name="TExtension">The type of metric to be returned.</typeparam>
        /// <returns></returns>
        public TExtension Get<TExtension>()
            where TExtension : IMetric<TMeasurable>
            => ((IMeasurableInternal) this._extensible).GetMetric<TExtension>();
    }
}