// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Metrics;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A prototype implementation of <see cref="StatementsCount"/>.
    /// </summary>
    [MetalamaPlugIn]
    public partial class StatementsCountMetricProvider : SyntaxMetricProvider<StatementsCount>
    {
        public StatementsCountMetricProvider() : base( new Visitor() ) { }

        protected override void Aggregate( ref StatementsCount aggregate, in StatementsCount newValue ) => aggregate.Add( newValue );
    }
}