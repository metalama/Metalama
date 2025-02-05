// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.DesignTime;

public static partial class JsonSerializationHelper
{
    private sealed class AnnotationWriter : SafeSyntaxRewriter
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ImmutableDictionaryOfArray<int, SerializableAnnotation> _annotations;

        public AnnotationWriter( SerializableSyntaxTree parent, CancellationToken cancellationToken )
        {
            this._cancellationToken = cancellationToken;
            this._annotations = parent.Annotations.ToMultiValueDictionary( a => a.SpanStart );
        }

        public override SyntaxToken VisitToken( SyntaxToken token )
        {
            var annotations = this._annotations[token.SpanStart];

            if ( annotations.IsDefaultOrEmpty )
            {
                return token;
            }
            else
            {
                return token.WithAdditionalAnnotations(
                    annotations.Where( a => a.TargetKind == SerializableAnnotationTargetKind.Token && a.SpanLength == token.Span.Length )
                        .Select( a => a.ToSyntaxAnnotation() ) );
            }
        }

        protected override SyntaxNode? VisitCore( SyntaxNode? node )
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            if ( node == null )
            {
                return null;
            }

            var rewrittenNode = base.VisitCore( node )!;

            var annotations = this._annotations[node.SpanStart];

            if ( annotations.IsDefaultOrEmpty )
            {
                return rewrittenNode;
            }
            else
            {
                return rewrittenNode.WithAdditionalAnnotations(
                    annotations.Where( a => a.TargetKind == SerializableAnnotationTargetKind.Node && a.SpanLength == node.Span.Length )
                        .Select( a => a.ToSyntaxAnnotation() ) );
            }
        }
    }
}