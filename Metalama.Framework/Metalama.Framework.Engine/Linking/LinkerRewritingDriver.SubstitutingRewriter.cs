// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerRewritingDriver
{
    /// <summary>
    /// Transforms an original body using substitutions.
    /// </summary>
    internal sealed class SubstitutingRewriter : SafeSyntaxRewriter
    {
        private readonly SubstitutionContext _substitutionContext;

        public SubstitutingRewriter( SubstitutionContext substitutionContext )
        {
            this._substitutionContext = substitutionContext;
        }

        protected override SyntaxNode? VisitCore( SyntaxNode? node )
        {
            if ( node == null )
            {
                return null;
            }

            var substitutions = this._substitutionContext.GetSubstitutions();

            if ( substitutions != null && substitutions.TryGetValue( node, out var substitution ) )
            {
                var currentNode = base.VisitCore( node ).AssertNotNull();
                var substitutedNode = substitution.Substitute( currentNode, this._substitutionContext );

                return substitutedNode;
            }
            else
            {
                return base.VisitCore( node );
            }
        }
    }
}