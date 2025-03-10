// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Pipeline
{
    public sealed class IntroducedSyntaxTree
    {
        public string Name { get; }

        /// <summary>
        /// Gets the source syntax tree or null if the generated syntax tree does not have a source syntax tree.
        /// </summary>
        public SyntaxTree? SourceSyntaxTree { get; }

        public SyntaxTree GeneratedSyntaxTree { get; }

        public IntroducedSyntaxTree( string name, SyntaxTree? sourceSyntaxTree, SyntaxTree generatedSyntaxTree )
        {
            IdentifierHelper.ValidateSyntaxTreeName( name );

            this.Name = name;
            this.SourceSyntaxTree = sourceSyntaxTree;
            this.GeneratedSyntaxTree = generatedSyntaxTree;
        }
    }
}