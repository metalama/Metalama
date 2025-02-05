// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Formatting
{
    public partial class FormattedCodeWriter
    {
        private sealed class FormattingVisitor : ClassifierBase
        {
            private bool _visitTrivia;

            public FormattingVisitor( ClassifiedTextSpanCollection textSpans ) : base( textSpans ) { }

            protected override void VisitCore( SyntaxNode? node )
            {
                if ( node == null )
                {
                    // Coverage: ignore.
                    return;
                }

                if ( !this._visitTrivia && !node.ContainsAnnotations )
                {
                    // Neither this node or any child node has any annotation.
                    return;
                }

                var setVisitTrivia = node.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind );

                // Give preference to source code.
                // TODO: Ideally, both annotations should not be present as it indicated inefficiency.
                if ( node.HasAnnotation( FormattingAnnotations.SourceCodeAnnotation ) )
                {
                    this.ClassifiedTextSpans.Add( node.Span, TextSpanClassification.SourceCode );
                }
                else if ( node.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ) )
                {
                    this.ClassifiedTextSpans.Add( node.Span, TextSpanClassification.GeneratedCode );

                    var annotation = node.GetAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ).FirstOrDefault();

                    if ( annotation != null && annotation.Data != null )
                    {
                        this.ClassifiedTextSpans.SetTag( node.Span, GeneratingAspectTagName, annotation.Data );
                    }
                }

                if ( setVisitTrivia )
                {
                    var previousVisitTrivia = this._visitTrivia;
                    this._visitTrivia = true;

                    try
                    {
                        base.VisitCore( node );
                    }
                    finally
                    {
                        this._visitTrivia = previousVisitTrivia;
                    }
                }
                else
                {
                    base.VisitCore( node );
                }

                foreach ( var diagnosticAnnotation in node.GetAnnotations( _diagnosticAnnotationName ) )
                {
                    var span = node.Span;

                    if ( node is MethodDeclarationSyntax method )
                    {
                        span = method.Identifier.Span;
                    }

                    this.ClassifiedTextSpans.SetTag( span, DiagnosticTagName, diagnosticAnnotation.Data! );
                }
            }

            public override void VisitToken( SyntaxToken token )
            {
                if ( this._visitTrivia )
                {
                    base.VisitToken( token );
                }
            }
        }
    }
}