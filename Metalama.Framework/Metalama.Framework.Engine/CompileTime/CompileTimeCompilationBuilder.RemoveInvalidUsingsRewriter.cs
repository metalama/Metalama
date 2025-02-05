// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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