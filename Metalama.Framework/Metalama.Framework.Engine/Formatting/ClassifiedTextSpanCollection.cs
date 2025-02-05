// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Collections;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// A set of <see cref="TextSpan"/>.
    /// </summary>
    public sealed class ClassifiedTextSpanCollection : IReadOnlyCollection<ClassifiedTextSpan>
    {
        private readonly SourceText? _sourceText;
        private readonly SkipListDictionary<int, MarkedTextSpan> _spans = new();
        private readonly int _length;

        // For test only.
        internal ClassifiedTextSpanCollection() : this( int.MaxValue ) { }

        [PublicAPI( "Used from Try" )]
        public ClassifiedTextSpanCollection( SourceText sourceText ) : this( sourceText.Length )
        {
            this._sourceText = sourceText;
        }

        private ClassifiedTextSpanCollection( int length )
        {
            this._length = length;

            // Start with a single default span. This avoid gaps in the partition later.
            this._spans.Add( 0, new MarkedTextSpan( new TextSpan( 0, length ), TextSpanClassification.Default, null ) );
        }

        /// <summary>
        /// Adds a marked <see cref="TextSpan"/>.
        /// </summary>
        internal void Add( in TextSpan span, TextSpanClassification classification )
        {
            this.SetSpanImpl( span, classification, null, null );
        }

        internal void Add( in TextSpan span, TextSpanClassification classification, string? tagName, string? tagValue )
        {
            this.SetSpanImpl( span, classification, tagName, tagValue );
        }

        internal void SetTag( in TextSpan span, string tagName, string tagValue )
        {
            this.SetSpanImpl( span, null, tagName, tagValue );
        }

        private void SetSpanImpl( TextSpan span, TextSpanClassification? classification, string? tagName, string? tagValue )
        {
            if ( span.Start >= this._length || span.End < 0 )
            {
                // This span is totally out of our range so the call can be ignored.
                return;
            }

            if ( span.Start < 0 || span.End > this._length )
            {
                span = TextSpan.FromBounds( Math.Max( 0, span.Start ), Math.Min( this._length, span.End ) );
            }

            for ( var i = 0; /* nothing */; i++ )
            {
                if ( i > 4 )
                {
                    throw new AssertionFailedException( "The recursion level is too high." );
                }

                if ( this._spans.TryGetGreatestSmallerOrEqualValue( span.Start, out var previousStartSpan ) && previousStartSpan.Span.IntersectsWith( span ) )
                {
                    // Check if we have an exact span. If not, we will partition.
                    if ( previousStartSpan.Span.Equals( span ) )
                    {
                        // We have an exact span, so no need to partition.

                        if ( !previousStartSpan.Satisfies( classification, tagName, tagValue ) )
                        {
                            this._spans[span.Start] = previousStartSpan.Update( classification, tagName, tagValue );
                        }

                        return;
                    }

                    // If the new span is contained in a span of weaker category and has the requested tag, there is nothing to do.
                    if ( previousStartSpan.Span.Contains( span ) && previousStartSpan.Satisfies( classification, tagName, tagValue ) )
                    {
                        return;
                    }

                    // We need to partition the existing spans to the new span boundaries.
                    if ( span.Start == previousStartSpan.Span.Start )
                    {
                        // We have hit an existing span start. Check if we need to partition the end.
                        if ( this._spans.TryGetGreatestSmallerOrEqualValue( span.End, out var previousEndSpan ) && previousEndSpan.Span.End != span.End
                            && previousEndSpan.Span.Start != span.End )
                        {
                            // We have to split the existing span that conflicts with the end of the new span.
                            var (a, b) = Split( previousEndSpan, span.End );
                            this._spans[a.Span.Start] = a;
                            this._spans[b.Span.Start] = b;
                        }
                        else
                        {
                            // We have a partition of existing spans than matches the boundaries of the new span.
                            // Check all

                            foreach ( var pair in this._spans.GetItemsGreaterOrEqualThan( span.Start ) )
                            {
                                if ( pair.Value.Span.Start >= span.End )
                                {
                                    break;
                                }

                                if ( !pair.Value.Satisfies( classification, tagName, tagValue ) )
                                {
                                    // Replace the category of this node.
                                    this._spans[pair.Key] = pair.Value.Update( classification, tagName, tagValue );
                                }
                            }

                            return;
                        }
                    }
                    else
                    {
                        // The situation is more complex. We will split the old span in a partition so
                        // that we are back to a simple situation.

                        var (a, b) = Split( previousStartSpan, span.Start );
                        this._spans[a.Span.Start] = a;

                        if ( b.Span.Contains( span.End ) )
                        {
                            var (c, d) = Split( b, span.End );
                            this._spans.Add( c.Span.Start, c );
                            this._spans.Add( d.Span.Start, d );
                        }
                        else
                        {
                            this._spans.Add( b.Span.Start, b );
                        }
                    }
                }
                else
                {
                    // We cannot get here because we start with a whole partition.
                    throw new AssertionFailedException( "The state of the collection is invalid." );
                }
            }
        }

        private static (MarkedTextSpan Span1, MarkedTextSpan Span2) Split( in MarkedTextSpan textSpan, int splitPosition )
        {
            if ( !textSpan.Span.Contains( splitPosition ) || splitPosition == textSpan.Span.Start )
            {
                throw new ArgumentException( nameof(splitPosition) );
            }

            return (new MarkedTextSpan( TextSpan.FromBounds( textSpan.Span.Start, splitPosition ), textSpan.Classification, textSpan.Tags ),
                    new MarkedTextSpan(
                        TextSpan.FromBounds( splitPosition, textSpan.Span.End ),
                        textSpan.Classification,
                        textSpan.Tags ));
        }

        public IEnumerable<ClassifiedTextSpan> GetClassifiedSpans( TextSpan textSpan )
        {
            var previousSpan = default(ClassifiedTextSpan);

            foreach ( var pair in this._spans.GetItemsGreaterOrEqualThan( textSpan.Start, true ) )
            {
                var classifiedSpan = pair.Value;

                if ( classifiedSpan.Classification == previousSpan.Classification &&
                     classifiedSpan.HasEqualTags( previousSpan.Tags ) &&
                     classifiedSpan.Span.Start == previousSpan.Span.End )
                {
                    // Merge the previous span and the current one.
                    previousSpan = new ClassifiedTextSpan(
                        TextSpan.FromBounds( previousSpan.Span.Start, classifiedSpan.Span.End ),
                        classifiedSpan.Classification,
                        classifiedSpan.Tags );
                }
                else
                {
                    // Emit the previous span if any.
                    if ( previousSpan.Span.Length > 0 )
                    {
                        yield return previousSpan;
                    }

                    previousSpan = new ClassifiedTextSpan( pair.Value.Span, pair.Value.Classification, pair.Value.Tags );
                }

                // Break if we are getting further than the last span.
                if ( classifiedSpan.Span.End >= textSpan.End )
                {
                    break;
                }
            }

            // Emit the last span if any.
            if ( previousSpan.Span.Length > 0 )
            {
                yield return previousSpan;
            }
        }

        public IEnumerator<ClassifiedTextSpan> GetEnumerator() => this.GetClassifiedSpans( new TextSpan( 0, int.MaxValue ) ).GetEnumerator();

        public override string ToString()
        {
            // Used for unit testing. Don't change the rendering logic.
            var stringBuilder = new StringBuilder();
            stringBuilder.Append( "{ " );

            foreach ( var pair in this._spans )
            {
                if ( stringBuilder.Length > 2 )
                {
                    stringBuilder.Append( ", " );
                }

                stringBuilder.Append( pair.Value.ToString() );
            }

            stringBuilder.Append( " } " );

            return stringBuilder.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._spans.Count;

        private static bool IsColored( TextSpanClassification classification )
            => classification is not (
                TextSpanClassification.Default or
                TextSpanClassification.RunTime or
                TextSpanClassification.Excluded or
                TextSpanClassification.SourceCode or
                TextSpanClassification.NeutralTrivia);

        /// <summary>
        /// Post-processes the spans and fixes the classification of trailing trivia in spans that are immediately before non-colored spans.
        /// </summary>
        [PublicAPI( "Used from Try" )]
        public void Polish()
        {
            if ( this._sourceText == null )
            {
                throw new InvalidOperationException();
            }

            using var enumerator = this._spans.GetEnumerator();
            using var nextEnumerator = this._spans.GetEnumerator();

            // `nextEnumerator` must always be one node further than `enumerator`.
            if ( !nextEnumerator.MoveNext() )
            {
                return;
            }

            while ( enumerator.MoveNext() )
            {
                if ( !nextEnumerator.MoveNext() )
                {
                    return;
                }

                if ( IsColored( enumerator.Current.Value.Classification )
                     && !IsColored( nextEnumerator.Current.Value.Classification ) )
                {
                    var span = enumerator.Current.Value.Span;
                    var rawSpanLength = span.Length;

                    while ( rawSpanLength > 0 && this._sourceText[span.Start + rawSpanLength - 1] == ' ' )
                    {
                        rawSpanLength--;
                    }

                    if ( rawSpanLength < span.Length )
                    {
                        this.Add( TextSpan.FromBounds( span.Start + rawSpanLength, span.Start + span.Length ), TextSpanClassification.NeutralTrivia );
                    }
                }
            }
        }
    }
}