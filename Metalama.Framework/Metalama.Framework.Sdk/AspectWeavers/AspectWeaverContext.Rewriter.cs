// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AspectWeavers
{
    public sealed partial class AspectWeaverContext
    {
        private sealed class Rewriter : SafeSyntaxRewriter
        {
            private readonly ImmutableHashSet<SyntaxNode> _targets;
            private readonly CSharpSyntaxRewriter _userRewriter;

            public Rewriter( ImmutableHashSet<SyntaxNode> targets, CSharpSyntaxRewriter userRewriter )
            {
                this._userRewriter = userRewriter;
                this._targets = targets;
            }

            protected override SyntaxNode? VisitCore( SyntaxNode? node )
            {
                switch ( node )
                {
                    case CompilationUnitSyntax:
                        return base.VisitCore( node );

                    case MemberDeclarationSyntax or AccessorDeclarationSyntax:
                        var rewrittenNode = node;

                        if ( node is BaseTypeDeclarationSyntax or BaseNamespaceDeclarationSyntax )
                        {
                            // Visit types and namespaces.

                            rewrittenNode = base.VisitCore( rewrittenNode );
                        }

                        if ( this._targets.Contains( node ) )
                        {
                            rewrittenNode = this._userRewriter.Visit( rewrittenNode );
                        }

                        return rewrittenNode;
                }

                // Don't visit other members.
                return node;
            }
        }
    }
}