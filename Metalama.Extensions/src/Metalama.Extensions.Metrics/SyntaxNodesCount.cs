// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System.Globalization;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A metric that counts the number of syntax nodes in a declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This metric provides a more accurate measure of code complexity than line counts, as it counts actual syntax
    /// nodes in the syntax tree. 
    /// </para>
    /// <para>
    /// Use this metric with <see cref="IDeclaration"/>.<see cref="MetricsExtensions.Metrics{TExtensible}(TExtensible)"/>.<see cref="Metrics{T}.Get{TExtension}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="StatementsCount"/>
    /// <seealso cref="SyntaxNodesCountMetricProvider"/>
    /// <seealso href="@metrics"/>
    public struct SyntaxNodesCount : IMetric<IMemberOrNamedType>, IMetric<INamespace>, IMetric<ICompilation>
    {
        /// <summary>
        /// Gets the total number of syntax nodes.
        /// </summary>
        public int Value { get; internal set; }

        internal void Add( in SyntaxNodesCount other )
        {
            this.Value += other.Value;
        }

        public override readonly string ToString() => this.Value.ToString( CultureInfo.InvariantCulture );
    }
}