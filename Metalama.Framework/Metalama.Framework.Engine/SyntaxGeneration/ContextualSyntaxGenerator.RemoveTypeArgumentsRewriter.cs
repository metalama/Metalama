// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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