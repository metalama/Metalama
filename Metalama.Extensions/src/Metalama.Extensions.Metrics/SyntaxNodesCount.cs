// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System.Globalization;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A metric that counts the number of syntax nodes in a declaration.
    /// </summary>
    public struct SyntaxNodesCount : IMetric<IMethodBase>, IMetric<INamedType>, IMetric<INamespace>, IMetric<ICompilation>
    {
        /// <summary>
        /// Gets the total number of statements.
        /// </summary>
        public int Value { get; internal set; }

        internal void Add( in SyntaxNodesCount other )
        {
            this.Value += other.Value;
        }

        public override string ToString() => this.Value.ToString( CultureInfo.InvariantCulture );
    }
}