// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;

namespace Metalama.Framework.Engine.Formatting
{
    internal abstract class ClassifierBase : SafeSyntaxWalker
    {
        public ClassifiedTextSpanCollection ClassifiedTextSpans { get; }

#if !DEBUG
#pragma warning disable IDE0052 // Remove unread private members

        // ReSharper disable once NotAccessedField.Local
#endif
        private readonly SourceText? _sourceText;

        protected ClassifierBase( ClassifiedTextSpanCollection classifiedTextSpans, SourceText? sourceText = null )
            : base( SyntaxWalkerDepth.StructuredTrivia )
        {
            this.ClassifiedTextSpans = classifiedTextSpans;
            this._sourceText = sourceText;
        }

        private bool _isAfterEndOfLine;

        public override void VisitTrivia( SyntaxTrivia trivia )
        {
            switch ( trivia.Kind() )
            {
                case SyntaxKind.EndOfLineTrivia:
                    this._isAfterEndOfLine = true;

                    break;

                case SyntaxKind.WhitespaceTrivia when this._isAfterEndOfLine:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                    this.Mark( trivia.Span, TextSpanClassification.NeutralTrivia );

                    break;

                case SyntaxKind.DocumentationCommentExteriorTrivia when this._isAfterEndOfLine:
                    var triviaText = trivia.ToString();
                    var nonSpaceIndex = triviaText.Length - triviaText.AsSpan().TrimStart().Length;
                    this.Mark( new TextSpan( trivia.SpanStart, nonSpaceIndex ), TextSpanClassification.NeutralTrivia );

                    break;
            }

            base.VisitTrivia( trivia );
        }

        public override void VisitTrailingTrivia( SyntaxToken token )
        {
            if ( !token.FullSpan.IsEmpty )
            {
                this._isAfterEndOfLine = token.IsKind( SyntaxKind.XmlTextLiteralNewLineToken );
            }

            base.VisitTrailingTrivia( token );
        }

        protected void Mark( TextSpan span, TextSpanClassification classification )
        {
            if ( span.IsEmpty )
            {
                return;
            }

#if DEBUG

            // ReSharper disable once UnusedVariable
            var text = this._sourceText?.GetSubText( span ).ToString();
#endif

            this.ClassifiedTextSpans.Add( span, classification );
        }
    }
}