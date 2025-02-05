// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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