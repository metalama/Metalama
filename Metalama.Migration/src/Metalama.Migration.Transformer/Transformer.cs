// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Migration.Transformer
{
    [Transformer]
    public class Transformer : ISourceTransformer
    {
        public void Execute( TransformerContext context )
        {
            HashSet<ISymbol> processedPartialTypes = new( SymbolEqualityComparer.Default );

            foreach ( var syntaxTree in context.Compilation.SyntaxTrees )
            {
                var semanticModel = context.Compilation.GetSemanticModel( syntaxTree );

                var rewriter = new Rewriter( semanticModel, processedPartialTypes );
                var newSyntaxRoot = rewriter.Visit( syntaxTree.GetRoot() );
                var newSyntaxTree = syntaxTree.WithRootAndOptions( newSyntaxRoot, syntaxTree.Options );
                context.ReplaceSyntaxTree( syntaxTree, newSyntaxTree );
            }
        }
    }
}