// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Formatting
{
    internal readonly struct MarkedTextSpan
    {
        internal TextSpan Span { get; }

        public TextSpanClassification Classification { get; }

        public ImmutableDictionary<string, string> Tags { get; }

        internal MarkedTextSpan( in TextSpan span, TextSpanClassification classification, ImmutableDictionary<string, string>? tags )
        {
            this.Span = span;
            this.Classification = classification;
            this.Tags = tags ?? ImmutableDictionary<string, string>.Empty;
        }

        public MarkedTextSpan Update( TextSpanClassification? classification, string? tagName, string? tagValue )
        {
            if ( tagName != null && tagValue == null )
            {
                // Null tag values are not supported.
                throw new ArgumentNullException( nameof(tagValue) );
            }

            var newClassification = classification > this.Classification ? classification.Value : this.Classification;
            ImmutableDictionary<string, string> newTags;

            if ( tagName == null || (this.Tags.TryGetValue( tagName, out var currentTagValue ) && currentTagValue == tagValue) )
            {
                newTags = this.Tags;
            }
            else
            {
                newTags = this.Tags.SetItem( tagName, tagValue! );
            }

            return new MarkedTextSpan( this.Span, newClassification, newTags );
        }

        public bool Satisfies( TextSpanClassification? classification, string? tagName, string? tagValue )
        {
            if ( tagName != null && tagValue == null )
            {
                // Null tag values are not supported.
                throw new ArgumentNullException( nameof(tagValue) );
            }

            if ( classification > this.Classification )
            {
                return false;
            }

            if ( tagName == null )
            {
                return true;
            }
            else
            {
                return this.Tags.TryGetValue( tagName, out var currentValue ) && tagValue == currentValue;
            }
        }

        public bool HasEqualTags( ImmutableDictionary<string, string>? other )
        {
            if ( other == null )
            {
                return this.Tags.IsEmpty;
            }

            if ( this.Tags.Count != other.Count )
            {
                return false;
            }

            foreach ( var t in this.Tags )
            {
                if ( !other.TryGetValue( t.Key, out var otherValue ) || t.Value != otherValue )
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            var s = this.Span.ToString().ReplaceOrdinal( "2147483647" /* int.Max */, "inf" ) + "=>" + this.Classification;

            foreach ( var tag in this.Tags )
            {
                s += $"{{{tag.Key}={tag.Value}}}";
            }

            return s;
        }
    }
}