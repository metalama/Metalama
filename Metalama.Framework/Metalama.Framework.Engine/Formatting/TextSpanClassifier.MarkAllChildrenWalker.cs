// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Formatting
{
    internal sealed partial class TextSpanClassifier
    {
        private sealed class MarkAllChildrenWalker : SafeSyntaxWalker
        {
            private readonly TextSpanClassifier _parent;
            private TextSpanClassification _classification;

            public MarkAllChildrenWalker( TextSpanClassifier parent ) : base( SyntaxWalkerDepth.StructuredTrivia )
            {
                this._parent = parent;
            }

            public void MarkAll( SyntaxNode node, TextSpanClassification classification )
            {
                this._classification = classification;
                this.Visit( node );
            }

            public override void DefaultVisit( SyntaxNode node )
            {
                foreach ( var child in node.ChildNodesAndTokens() )
                {
                    if ( child.IsNode )
                    {
                        this.Visit( child.AsNode() );
                    }
                    else
                    {
                        this._parent.Mark( child.AsToken(), this._classification );
                    }
                }

                if ( ShouldMarkTrivia( this._classification ) )
                {
                    this._parent.Mark( node.GetLeadingTrivia(), this._classification );
                    this._parent.Mark( node.GetTrailingTrivia(), this._classification );
                }
            }
        }
    }
}