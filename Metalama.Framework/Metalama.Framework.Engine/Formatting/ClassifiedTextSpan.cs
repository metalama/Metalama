// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> and its <see cref="TextSpanClassification"/>.
    /// </summary>
    public readonly struct ClassifiedTextSpan
    {
        /// <summary>
        /// Gets the <see cref="TextSpan"/>.
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        /// Gets the classification of <see cref="Span"/>.
        /// </summary>
        public TextSpanClassification Classification { get; }

        [UsedImplicitly]
        public ImmutableDictionary<string, string> Tags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassifiedTextSpan"/> struct.
        /// </summary>
        internal ClassifiedTextSpan( TextSpan span, TextSpanClassification classification, ImmutableDictionary<string, string>? tags )
        {
            this.Span = span;
            this.Classification = classification;
            this.Tags = tags ?? ImmutableDictionary<string, string>.Empty;
        }

        public override string ToString()
        {
            var s = this.Span.ToString().ReplaceOrdinal( "2147483647" /* int.Max */, "inf" ) + "=>" + this.Classification;

            if ( this.Tags.Any() )
            {
                s += " " + string.Join( ", ", this.Tags.SelectAsReadOnlyCollection( tag => $"{tag.Key}={tag.Value}" ) );
            }

            return s;
        }
    }
}