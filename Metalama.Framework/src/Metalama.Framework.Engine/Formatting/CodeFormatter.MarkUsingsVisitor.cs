// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Metalama.Framework.Engine.Formatting;

public sealed partial class CodeFormatter
{
    private sealed class MarkTextSpansVisitor : SafeSyntaxWalker
    {
        private readonly ClassifiedTextSpanCollection _collection;

        public MarkTextSpansVisitor( ClassifiedTextSpanCollection collection )
        {
            this._collection = collection;
        }

        public override void DefaultVisit( SyntaxNode node )
        {
            if ( node.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ) ||
                 node.HasAnnotation( Formatter.Annotation ) )
            {
                this._collection.Add( node.FullSpan, TextSpanClassification.GeneratedCode );
            }
            else if ( node.HasAnnotation( FormattingAnnotations.SourceCodeAnnotation ) )
            {
                this._collection.Add( node.FullSpan, TextSpanClassification.SourceCode );
            }

            base.DefaultVisit( node );
        }
    }
}