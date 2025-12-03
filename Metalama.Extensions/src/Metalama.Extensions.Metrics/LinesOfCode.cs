// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using System.Globalization;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// A metric that counts lines of code in a declaration using multiple counting methods.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This metric provides three line count measurements:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="Logical"/>: Lines containing code tokens, excluding braces, comments, and blank lines.</item>
    /// <item><see cref="NonBlank"/>: Lines containing any tokens (including braces), excluding blank lines and comments.</item>
    /// <item><see cref="Total"/>: Total line span from start to end of the declaration.</item>
    /// </list>
    /// <para>
    /// Use this metric with <see cref="IDeclaration"/>.<see cref="MetricsExtensions.Metrics{TExtensible}(TExtensible)"/>.<see cref="Metrics{T}.Get{TExtension}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="SyntaxNodesCount"/>
    /// <seealso cref="StatementsCount"/>
    /// <seealso cref="LinesOfCodeMetricProvider"/>
    /// <seealso href="@metrics"/>
    public struct LinesOfCode : IMetric<IMemberOrNamedType>, IMetric<INamespace>, IMetric<ICompilation>
    {
        /// <summary>
        /// Gets the number of logical lines of code, excluding braces, comments, and blank lines.
        /// </summary>
        public int Logical { get; internal set; }

        /// <summary>
        /// Gets the number of non-blank lines (lines containing any tokens, including braces).
        /// </summary>
        public int NonBlank { get; internal set; }

        /// <summary>
        /// Gets the total line span of the declaration (end line - start line + 1).
        /// </summary>
        public int Total { get; internal set; }

        internal void Add( in LinesOfCode other )
        {
            this.Logical += other.Logical;
            this.NonBlank += other.NonBlank;
            this.Total += other.Total;
        }

        public override readonly string ToString() => this.Logical.ToString( CultureInfo.InvariantCulture );
    }
}
