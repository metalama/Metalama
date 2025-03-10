// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Metalama.Testing.AspectTesting
{
    internal abstract partial class BaseTestRunner
    {
        private sealed class ValidateAttributesVisitor : SafeSyntaxWalker
        {
            private readonly SemanticModelProvider _semanticModelProvider;

            public ValidateAttributesVisitor( Compilation compilation )
            {
                this._semanticModelProvider = compilation.GetSemanticModelProvider();
            }

            public override void VisitAttribute( AttributeSyntax node )
            {
                var semanticModel = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree );
                var symbol = semanticModel.GetSymbolInfo( node.Name );

                if ( symbol.Symbol == null )
                {
                    Assert.True(
                        false,
                        $"The custom attribute '{node}' cannot be resolved. Check that you are importing the correct namespaces and assemblies and that the custom attribute has correct accessibility level (e.g. internal vs. public)." );
                }
            }
        }
    }
}