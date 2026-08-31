// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
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
        /// Gets the key of the source document, or <see cref="DocumentKey.IsDefault"/> when the generated syntax tree
        /// has no source document.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A key rather than the <see cref="SyntaxTree"/> itself, because an introduction is cached and read again in
        /// later compilations, which is the case <see cref="DocumentKey"/> exists for. Holding the tree held its root
        /// and its source text for as long as the design-time result lived, which prevented Roslyn from evicting the
        /// parsed tree of a closed document.
        /// </para>
        /// <para>
        /// It is also the safer of the two. The tree carried here came from the compilation that produced the
        /// introduction, and <c>SplitResultsByTree</c> handed it to a builder that takes a semantic model on it from
        /// the compilation of a later run. That worked only because Roslyn reuses a <see cref="SyntaxTree"/> instance
        /// for a document that was not edited, and the branch is reached exactly when the source document is not
        /// dirty. Resolving a key against the compilation in hand cannot go stale in that way.
        /// </para>
        /// </remarks>
        public DocumentKey SourceDocumentKey { get; }

        /// <summary>
        /// Gets the syntax tree that the pipeline introduced.
        /// </summary>
        /// <remarks>
        /// Reports LAMA0870, deliberately left unsuppressed as a problem to be solved. This is the introduced code
        /// itself, and a tree that Metalama produced rather than one belonging to the source compilation, so it
        /// cannot be replaced by a key the way the source document can: there is no compilation to resolve it
        /// against. Whether it reaches a source compilation at all has not been measured. See "The per-file result
        /// holds three Roslyn objects" in <c>design-time-memory.md</c>.
        /// </remarks>
#pragma warning disable LAMA0870
        public SyntaxTree GeneratedSyntaxTree { get; }
#pragma warning restore LAMA0870

        public IntroducedSyntaxTree( string name, SyntaxTree? sourceSyntaxTree, SyntaxTree generatedSyntaxTree )
            : this( name, sourceSyntaxTree?.GetDocumentKey() ?? default, generatedSyntaxTree ) { }

        public IntroducedSyntaxTree( string name, DocumentKey sourceDocumentKey, SyntaxTree generatedSyntaxTree )
        {
            IdentifierHelper.ValidateSyntaxTreeName( name );

            this.Name = name;
            this.SourceDocumentKey = sourceDocumentKey;
            this.GeneratedSyntaxTree = generatedSyntaxTree;
        }
    }
}
