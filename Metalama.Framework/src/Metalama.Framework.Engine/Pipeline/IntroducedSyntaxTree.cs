// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Pipeline
{
    [Durable]
    public sealed class IntroducedSyntaxTree
    {
        public string Name { get; }

        /// <summary>
        /// Gets the source syntax tree or null if the generated syntax tree does not have a source syntax tree.
        /// </summary>
        /// <remarks>
        /// Reports LAMA0870, deliberately left unsuppressed as a problem to be solved.
        /// <c>DesignTimeAspectPipelineResult.SplitResultsByTree</c> already converts this tree to a
        /// <c>DocumentKey</c> in order to file the introduction, and every consumer outside the pipeline reads only
        /// its <see cref="SyntaxTree.FilePath"/>, so replacing the member with a key looks local.
        /// </remarks>
        public SyntaxTree? SourceSyntaxTree { get; }

        /// <summary>
        /// Gets the syntax tree that the pipeline introduced.
        /// </summary>
        /// <remarks>
        /// Reports LAMA0870, deliberately left unsuppressed as a problem to be solved. This is the introduced code
        /// itself, and a tree that Metalama produced rather than one belonging to the source compilation, so it
        /// cannot simply be dropped. Whether it reaches a source compilation at all has not been measured. See
        /// "The per-file result holds three Roslyn objects" in <c>design-time-memory.md</c>.
        /// </remarks>
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