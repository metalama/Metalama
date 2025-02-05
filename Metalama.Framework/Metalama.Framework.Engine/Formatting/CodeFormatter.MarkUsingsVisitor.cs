// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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