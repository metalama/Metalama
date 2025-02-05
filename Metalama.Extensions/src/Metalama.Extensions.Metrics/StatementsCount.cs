// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System.Globalization;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A metric that counts the number of statements in a declaration.
    /// </summary>
    /// <remarks>
    /// Counting statements is more relevant than counting lines of code. However, modern C# is more expression-oriented than
    /// earlier versions of the language. Counting expression nodes has become a more relevant metric.
    /// </remarks>
    public struct StatementsCount : IMetric<IMethodBase>, IMetric<INamedType>, IMetric<INamespace>, IMetric<ICompilation>
    {
        /// <summary>
        /// Gets the total number of statements.
        /// </summary>
        public int Value { get; internal set; }

        internal void Add( in StatementsCount other )
        {
            this.Value += other.Value;
        }

        public override string ToString() => this.Value.ToString( CultureInfo.InvariantCulture );
    }
}