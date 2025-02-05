// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public partial class ContextualSyntaxGenerator
{
    private sealed class SubstitutionRewriter : SafeSyntaxRewriter
    {
        private readonly IReadOnlyDictionary<string, TypeSyntax> _substitutions;

        public SubstitutionRewriter( IReadOnlyDictionary<string, TypeSyntax> substitutions )
        {
            this._substitutions = substitutions;
        }

        public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
        {
            if ( this._substitutions.TryGetValue( node.Identifier.Text, out var substitution ) )
            {
                return substitution;
            }
            else
            {
                return base.VisitIdentifierName( node );
            }
        }
    }
}