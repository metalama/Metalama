// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

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
            private readonly SyntaxTree _syntaxTree;
            private readonly SemanticModelProvider _semanticModelProvider;
            private SemanticModel? _semanticModel;
            private List<SyntaxTrivia>? _removedTrivia;

            public RemoveInvalidUsingRewriter( Compilation compileTimeCompilation, SyntaxTree syntaxTree )
            {
                this._syntaxTree = syntaxTree;
                this._semanticModelProvider = compileTimeCompilation.GetSemanticModelProvider();
            }

            public override SyntaxNode? VisitUsingDirective( UsingDirectiveSyntax node )
            {
                this._semanticModel ??= this._semanticModelProvider.GetSemanticModel( this._syntaxTree );
                var symbolInfo = this._semanticModel.GetSymbolInfo( node.GetNamespaceOrType() );

                if ( symbolInfo.Symbol == null )
                {
                    this._removedTrivia ??= new List<SyntaxTrivia>();
                    this._removedTrivia.AddRange( node.GetLeadingTrivia() );
                    this._removedTrivia.AddRange( node.GetTrailingTrivia() );

                    return null;
                }

                return node;
            }

            protected override SyntaxNode? VisitCore( SyntaxNode? node )
            {
                if ( node == null )
                {
                    return null;
                }

                var transformedNode = base.VisitCore( node );

                if ( transformedNode == null )
                {
                    return null;
                }
                
                if ( this._removedTrivia is { Count: > 0 } )
                {
                    transformedNode = transformedNode.WithLeadingTrivia( transformedNode.GetLeadingTrivia().InsertRange( 0, this._removedTrivia ) );
                    this._removedTrivia.Clear();
                }

                return transformedNode;
            }
        }
    }
}