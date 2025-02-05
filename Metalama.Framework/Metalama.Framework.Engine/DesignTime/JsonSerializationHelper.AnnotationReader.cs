// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.DesignTime;

public static partial class JsonSerializationHelper
{
    private sealed class AnnotationReader : SafeSyntaxWalker
    {
        private readonly ImmutableArray<SerializableAnnotation>.Builder _annotations = ImmutableArray.CreateBuilder<SerializableAnnotation>();

        public ImmutableArray<SerializableAnnotation> GetAnnotations() => this._annotations.ToImmutable();

        private void AddAnnotation( SyntaxNodeOrToken node, SerializableAnnotationKind kind, string? data = null )
        {
            this._annotations.Add(
                new SerializableAnnotation(
                    node.IsNode ? SerializableAnnotationTargetKind.Node : SerializableAnnotationTargetKind.Token,
                    node.Span.Start,
                    node.Span.Length,
                    kind,
                    data ) );
        }

        public override void DefaultVisit( SyntaxNode node )
        {
            this.AddAnnotation( node );

            base.DefaultVisit( node );
        }

        private void AddAnnotation( SyntaxNodeOrToken node )
        {
            if ( node.ContainsAnnotations )
            {
                if ( node.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ) )
                {
                    // Weirdly there can be many annotations of the same kind on the node, but we don't want to fail for this reason here.
                    var annotation = node.GetAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ).First();
                    this.AddAnnotation( node, SerializableAnnotationKind.GeneratedCode, annotation.Kind );
                }
                else if ( node.HasAnnotation( FormattingAnnotations.SourceCodeAnnotation ) )
                {
                    this.AddAnnotation( node, SerializableAnnotationKind.SourceCode );
                }
                else if ( node.HasAnnotation( Simplifier.Annotation ) )
                {
                    this.AddAnnotation( node, SerializableAnnotationKind.Simplifier );
                }
                else if ( node.HasAnnotation( Formatter.Annotation ) )
                {
                    this.AddAnnotation( node, SerializableAnnotationKind.Formatter );
                }
            }
        }
    }
}