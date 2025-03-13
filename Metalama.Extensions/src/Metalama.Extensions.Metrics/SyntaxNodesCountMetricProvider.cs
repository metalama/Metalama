// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Metrics;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A prototype implementation of <see cref="StatementsCount"/>.
    /// </summary>
    [MetalamaPlugIn]
    public partial class SyntaxNodesCountMetricProvider : SyntaxMetricProvider<SyntaxNodesCount>
    {
        public SyntaxNodesCountMetricProvider() : base( new Visitor() ) { }

        protected override void Aggregate( ref SyntaxNodesCount aggregate, in SyntaxNodesCount newValue ) => aggregate.Add( newValue );
    }
}