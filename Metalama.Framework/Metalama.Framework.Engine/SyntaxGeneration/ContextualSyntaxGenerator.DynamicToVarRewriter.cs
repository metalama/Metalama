// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxGeneration
{
    public partial class ContextualSyntaxGenerator
    {
        private sealed class DynamicToVarRewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
            {
                if ( node.Identifier.Text == "dynamic" )
                {
                    return SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.ObjectKeyword ) );
                }
                else
                {
                    return node;
                }
            }
        }
    }
}