// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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