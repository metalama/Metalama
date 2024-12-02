// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating;

internal sealed partial class TemplateSyntaxFactoryImpl
{
    private sealed class SerializedTypeOfRewriter : SafeSyntaxRewriter
    {
        private readonly Dictionary<string, TypeSyntax> _substitutions;

        public SerializedTypeOfRewriter( Dictionary<string, TypeSyntax> substitutions )
        {
            this._substitutions = substitutions;
        }

        public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
        {
            if ( node.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax )
            {
                // We have a type name. Don't substitute.
                return node;
            }

            if ( this._substitutions.TryGetValue( node.Identifier.Text, out var substitution ) )
            {
                return substitution;
            }
            else
            {
                return node;
            }
        }
    }
}