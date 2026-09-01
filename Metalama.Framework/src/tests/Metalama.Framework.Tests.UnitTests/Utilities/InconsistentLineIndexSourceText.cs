// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Text;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities
{
    /// <summary>
    /// Builds a <see cref="SourceText"/> whose line index does not agree with its content, so that converting a
    /// <see cref="TextSpan"/> into a <see cref="LinePosition"/> throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Roslyn represents an incremental edit as a <c>ChangedText</c> over a composition of segments of the previous
    /// text. When the edit replaces the carriage return of a CR LF pair, the line index computed over those segments
    /// reports, for a position after the edit, a line that starts after that position. The conversion then builds a
    /// <see cref="LinePosition"/> with a negative character offset and throws.
    /// </para>
    /// <para>
    /// This is the state a document is in at design time when the user types such an edit. Every operation that maps
    /// a span of that document to a line then throws, including the binding of the caller-information arguments of an
    /// attribute, which is what issue #1858 reports.
    /// </para>
    /// </remarks>
    internal static class InconsistentLineIndexSourceText
    {
        /// <summary>
        /// Returns the given code as a <see cref="SourceText"/> whose line index is inconsistent. The code must use
        /// CR LF line endings and must remain valid C# after the first CR LF pair is replaced by two line feeds.
        /// </summary>
        public static SourceText Create( string codeWithCarriageReturns )
        {
            var carriageReturnIndex = codeWithCarriageReturns.IndexOf( '\r' );

            Assert.True( carriageReturnIndex >= 0, "The code must use CR LF line endings." );

            var text = SourceText.From( codeWithCarriageReturns )
                .WithChanges( new TextChange( new TextSpan( carriageReturnIndex, 1 ), "\n" ) );

            Assert.True(
                TryFindPositionWithoutLineMapping( text, out _ ),
                "Roslyn no longer produces an inconsistent line index for this edit. The tests that rely on this "
                + "helper no longer cover issue #1858 and must be revised." );

            return text;
        }

        /// <summary>
        /// Determines whether the given text has a position that cannot be converted to a <see cref="LinePosition"/>,
        /// and returns the first such position.
        /// </summary>
        public static bool TryFindPositionWithoutLineMapping( SourceText text, out int position )
        {
            for ( position = 0; position <= text.Length; position++ )
            {
                try
                {
                    _ = text.Lines.GetLinePosition( position );
                }
                catch ( ArgumentOutOfRangeException )
                {
                    return true;
                }
            }

            position = -1;

            return false;
        }
    }
}
