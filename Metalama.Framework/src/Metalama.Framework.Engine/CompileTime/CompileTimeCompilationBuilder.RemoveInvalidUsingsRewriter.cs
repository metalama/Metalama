// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed partial class CompileTimeCompilationBuilder
    {
        /// <summary>
        /// Removes invalid <c>using</c> statements from a compile-time syntax tree. Such using statements
        /// are typically run-time-only.
        /// </summary>
        internal sealed class RemoveInvalidUsingRewriter : SafeSyntaxRewriter
        {
            private readonly SemanticModelProvider _semanticModelProvider;

            public RemoveInvalidUsingRewriter( Compilation compileTimeCompilation )
            {
                this._semanticModelProvider = compileTimeCompilation.GetSemanticModelProvider();
            }

            public override SyntaxNode? VisitUsingDirective( UsingDirectiveSyntax node )
            {
                var symbolInfo = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetSymbolInfo( node.GetNamespaceOrType() );

                if ( symbolInfo.Symbol == null )
                {
                    return null;
                }

                return node;
            }
        }
    }
}