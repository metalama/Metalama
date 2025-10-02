// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeCompilationBuilder
{
    public static Compilation RemoveDuplicateEmbeddedAttributes( Compilation compilation )
    {
        List<SyntaxNode> declarations = new();

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            new EmbeddedAttributeDetector( declarations.Add ).Visit( syntaxTree.GetRoot() );
        }

        if ( declarations.Count <= 1 )
        {
            return compilation;
        }

        var nodesToRemove = declarations
            .OrderBy( d => d.SyntaxTree.FilePath )
            .ThenBy( d => d.GetLocation().SourceSpan.Start )
            .Skip( 1 )
            .ToHashSet();

        var syntaxTreesToRewrite = nodesToRemove.SelectAsArray( n => n.SyntaxTree ).Distinct();

        foreach ( var syntaxTree in syntaxTreesToRewrite )
        {
            var newSyntaxRoot = new EmbeddedAttributeRemover( nodesToRemove ).Visit( syntaxTree.GetRoot() );
            var newSyntaxTree = syntaxTree.WithRootAndOptions( newSyntaxRoot, syntaxTree.Options );
            compilation = compilation.ReplaceSyntaxTree( syntaxTree, newSyntaxTree );
        }

        return compilation;
    }

    private sealed class EmbeddedAttributeRemover : SafeSyntaxRewriter
    {
        private readonly HashSet<SyntaxNode> _nodesToRemove;
        private SyntaxTriviaList _removedTrivia;

        public EmbeddedAttributeRemover( HashSet<SyntaxNode> nodesToRemove )
        {
            this._nodesToRemove = nodesToRemove;
        }

        public override SyntaxToken VisitToken( SyntaxToken token )
        {
            if ( this._removedTrivia.Count != 0 )
            {
                token = token.WithLeadingTrivia( this._removedTrivia.AddRange( token.LeadingTrivia ) );
                this._removedTrivia = default;
            }

            return token;
        }

        public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node )
        {
            if ( !this._nodesToRemove.Contains( node ) )
            {
                return node;
            }

            this._removedTrivia = this._removedTrivia.AddRange( node.GetLeadingTrivia() ).AddRange( node.GetTrailingTrivia() );

            return null;
        }
    }
}