// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities
{
    /// <summary>
    /// A <see cref="SourceText"/> whose line index does not agree with its content, so that converting a position
    /// after the first line into a <see cref="LinePosition"/> throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class reproduces a defect of Roslyn. When Roslyn represents an incremental edit as a composition of
    /// segments of the previous text, the line collection of that composition enumerates the correct line spans, but
    /// it resolves a position to a line that starts after that position. <see cref="TextLineCollection.GetLinePosition"/>
    /// then builds a <see cref="LinePosition"/> with a negative character offset and throws. The defect was fixed in
    /// Roslyn 5.10 by pull request 83000 of the <c>dotnet/roslyn</c> repository, and it is still present in Roslyn
    /// 5.0, which is the lowest version that the supported platform baseline requires. The state is therefore
    /// constructed here instead of being obtained from an edit, so that the tests cover it on every Roslyn version.
    /// </para>
    /// <para>
    /// This is the state a document is in at design time when the user types such an edit. Every operation that maps
    /// a span of that document to a line then throws, including the binding of the caller-information arguments of an
    /// attribute, which is what issue #1858 reports.
    /// </para>
    /// </remarks>
    internal sealed class InconsistentLineIndexSourceText : SourceText
    {
        private readonly SourceText _text;

        private InconsistentLineIndexSourceText( SourceText text )
        {
            this._text = text;
        }

        /// <summary>
        /// Returns the given code as a <see cref="SourceText"/> whose line index is inconsistent. No position of the
        /// first line is affected, and no position after the first line can be converted into a
        /// <see cref="LinePosition"/>. The code must contain at least two lines and must end with a line break.
        /// </summary>
        public static SourceText Create( string code )
        {
            var text = new InconsistentLineIndexSourceText( From( code ) );

            Assert.True( text.FirstPositionWithoutLineMapping < text.Length, "The code must contain at least two lines." );

            // A test that relies on this state must fail rather than pass without covering anything, so the state is
            // verified here instead of being assumed.
            for ( var position = text.FirstPositionWithoutLineMapping; position < text.Length; position++ )
            {
                var positionToTest = position;

                Assert.Throws<ArgumentOutOfRangeException>( () => text.Lines.GetLinePosition( positionToTest ) );
            }

            return text;
        }

        /// <summary>
        /// Gets the first position that cannot be converted into a <see cref="LinePosition"/>, which is the first
        /// position after the line break of the first line.
        /// </summary>
        private int FirstPositionWithoutLineMapping => this._text.Lines[0].EndIncludingLineBreak;

        /// <inheritdoc />
        public override char this[ int position ] => this._text[position];

        /// <inheritdoc />
        public override Encoding? Encoding => this._text.Encoding;

        /// <inheritdoc />
        public override int Length => this._text.Length;

        /// <inheritdoc />
        public override void CopyTo( int sourceIndex, char[] destination, int destinationIndex, int count )
            => this._text.CopyTo( sourceIndex, destination, destinationIndex, count );

        /// <inheritdoc />
        public override string ToString( TextSpan span ) => this._text.ToString( span );

        /// <inheritdoc />
        protected override TextLineCollection GetLinesCore() => new LineCollection( this._text.Lines, this.FirstPositionWithoutLineMapping );

        /// <summary>
        /// A <see cref="TextLineCollection"/> that enumerates the correct lines but resolves every position after the
        /// first line to the last line of the text.
        /// </summary>
        /// <remarks>
        /// When the text ends with a line break, the last line is empty and starts at the end of the text, so the
        /// start of the line that this collection returns is greater than the position that was resolved, and
        /// <see cref="TextLineCollection.GetLinePosition"/> throws. <see cref="Create"/> verifies that the mapping
        /// does fail for every position after the first line.
        /// </remarks>
        private sealed class LineCollection : TextLineCollection
        {
            private readonly TextLineCollection _lines;
            private readonly int _firstPositionWithoutLineMapping;

            public LineCollection( TextLineCollection lines, int firstPositionWithoutLineMapping )
            {
                this._lines = lines;
                this._firstPositionWithoutLineMapping = firstPositionWithoutLineMapping;
            }

            /// <inheritdoc />
            public override int Count => this._lines.Count;

            /// <inheritdoc />
            public override TextLine this[ int index ] => this._lines[index];

            /// <inheritdoc />
            public override int IndexOf( int position )
                => position >= this._firstPositionWithoutLineMapping ? this.Count - 1 : this._lines.IndexOf( position );
        }
    }
}
