// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating.Mapping
{
    internal sealed partial class TextMapFile
    {
        private sealed class Visitor : SafeSyntaxWalker
        {
            private readonly ILocationAnnotationMap _annotationMap;

            public Visitor( ILocationAnnotationMap annotationMap )
            {
                this._annotationMap = annotationMap;
            }

            public string? SourcePath { get; private set; }

            public SkipListDictionary<int, TextPointMapping> TextPointMappings { get; } = new();

            private enum PointKind
            {
                Start,
                End
            }

            private static TextPoint GetTextPoint( Location location, PointKind kind )
                => kind switch
                {
                    PointKind.Start => new TextPoint( location.SourceSpan.Start, location.GetLineSpan().StartLinePosition ),
                    PointKind.End => new TextPoint( location.SourceSpan.End, location.GetLineSpan().EndLinePosition ),
                    _ => throw new AssertionFailedException( $"Unexpected PointKind: {kind}." )
                };

            private static TextPointMapping GetTextPointMapping( Location source, Location target, PointKind kind )
                => new( GetTextPoint( source, kind ), GetTextPoint( target, kind ) );

            private void ProcessNodeOrToken( SyntaxNodeOrToken node )
            {
                var targetLocation = node.GetLocation();

                if ( targetLocation == null )
                {
                    return;
                }

                var sourceLocation = this._annotationMap.GetLocation( node );

                if ( sourceLocation != null )
                {
                    if ( !string.IsNullOrEmpty( sourceLocation.SourceTree?.FilePath ) )
                    {
                        this.SourcePath ??= sourceLocation.SourceTree!.FilePath;

                        if ( !this.TextPointMappings.ContainsKey( targetLocation.SourceSpan.Start ) )
                        {
                            _ = this.TextPointMappings.Add(
                                targetLocation.SourceSpan.Start,
                                GetTextPointMapping( sourceLocation, targetLocation, PointKind.Start ) );
                        }

                        if ( !this.TextPointMappings.ContainsKey( targetLocation.SourceSpan.End ) )
                        {
                            _ = this.TextPointMappings.Add(
                                targetLocation.SourceSpan.End,
                                GetTextPointMapping( sourceLocation, targetLocation, PointKind.End ) );
                        }
                    }
                }
            }

            protected override void VisitCore( SyntaxNode? node )
            {
                this.ProcessNodeOrToken( node );
                base.VisitCore( node );
            }

            public override void VisitToken( SyntaxToken token )
            {
                this.ProcessNodeOrToken( token );
                base.VisitToken( token );
            }
        }
    }
}