// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Metrics;
using Metalama.Framework.Metrics;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// Provides the implementation for the <see cref="StatementsCount"/> metric.
    /// </summary>
    /// <remarks>
    /// This provider is automatically registered and used when you call <see cref="Metrics{T}.Get{TExtension}"/> on the
    /// <see cref="StatementsCount"/> metric.
    /// </remarks>
    /// <seealso cref="StatementsCount"/>
    /// <seealso href="@custom-metrics"/>
    [MetalamaPlugIn]
    public sealed partial class StatementsCountMetricProvider : SyntaxMetricProvider<StatementsCount>
    {
        public StatementsCountMetricProvider() : base( new Visitor() ) { }

        protected override void Aggregate( ref StatementsCount aggregate, in StatementsCount newValue ) => aggregate.Add( newValue );
    }
}