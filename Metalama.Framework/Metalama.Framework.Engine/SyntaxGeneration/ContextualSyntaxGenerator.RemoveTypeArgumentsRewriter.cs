// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.SyntaxGeneration
{
    public partial class ContextualSyntaxGenerator
    {
        private sealed class RemoveTypeArgumentsRewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode VisitGenericName( GenericNameSyntax node )
            {
                // We intentionally don't visit type arguments, because we don't want remove the nested type arguments.

                // Replace type arguments with OmittedTypeArgument.
                return SyntaxFactory.GenericName( node.Identifier )
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                node.TypeArgumentList.Arguments.SelectAsImmutableArray( _ => SyntaxFactory.OmittedTypeArgument() ) ) ) );
            }
        }
    }
}