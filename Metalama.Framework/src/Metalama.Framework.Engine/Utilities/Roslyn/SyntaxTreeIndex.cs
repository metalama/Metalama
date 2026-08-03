// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Indexes the syntax trees of a <see cref="Compilation"/> by <see cref="DocumentKey"/> and records the trees that had
/// to be excluded from the index because an earlier tree already represented their document.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn does not guarantee that a path identifies a syntax tree. The command-line compiler deduplicates source files
/// itself, reporting <c>CS2002</c>, so a compilation built by <c>csc</c> never contains two trees with one path. A
/// compilation built by Roslyn Workspaces can, because the project system creates one document per <c>Compile</c> item
/// and does not deduplicate: overlapping globs, an explicit item that repeats a glob, <c>Link</c> metadata pointing two
/// items at one file, or a shared project all produce the condition, out of a project that is otherwise valid. See
/// issue #1742.
/// </para>
/// <para>
/// Metalama treats such a pair as one document included twice rather than as two documents, which is the conclusion
/// the command-line compiler reaches. The alternative cannot be represented: every design-time consumer addresses a
/// document by its path, from the Roslyn analyzer callback that carries only a <see cref="SemanticModel"/> to the
/// cross-process preview contract, so a second document at the same path would have no way of being asked for.
/// </para>
/// <para>
/// The first tree of a document wins, in the order of <see cref="Compilation.SyntaxTrees"/>, which is deterministic for
/// a given compilation. <see cref="DuplicatePathSyntaxTrees"/> holds the others so that the caller can remove them from
/// the compilation as well: an index that excludes a tree the compilation still contains describes a compilation it
/// does not match, and the pipeline would then leave that tree unrewritten while its declarations remain visible in the
/// code model.
/// </para>
/// </remarks>
internal sealed class SyntaxTreeIndex
{
    private readonly Dictionary<DocumentKey, SyntaxTree> _syntaxTreesByDocumentKey;

    /// <summary>
    /// Gets the syntax trees of the compilation indexed by <see cref="DocumentKey"/>, holding the first tree of each
    /// document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keyed by <see cref="DocumentKey"/> rather than by the path it wraps, so that a lookup made from a key reuses the
    /// hash the key computed once, instead of hashing a long path string again on every call.
    /// </para>
    /// <para>
    /// A <see cref="Dictionary{TKey,TValue}"/> and not an <see cref="ImmutableDictionary{TKey,TValue}"/>. The index is
    /// built once per compilation and never updated, so the structural sharing an immutable dictionary provides buys
    /// nothing, while its cost is paid on every build and every lookup: an ordered tree walk instead of a hash lookup,
    /// and two allocations per entry instead of one array.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<DocumentKey, SyntaxTree> SyntaxTreesByDocumentKey => this._syntaxTreesByDocumentKey;

    /// <summary>
    /// Gets the syntax trees of the compilation, one per document.
    /// </summary>
    public IReadOnlyCollection<SyntaxTree> SyntaxTrees => this._syntaxTreesByDocumentKey.Values;

    /// <summary>
    /// Gets the syntax trees that represent a document an earlier tree of the compilation already represents, and are
    /// therefore absent from <see cref="SyntaxTreesByDocumentKey"/>.
    /// </summary>
    public ImmutableArray<SyntaxTree> DuplicatePathSyntaxTrees { get; }

    public bool HasDuplicatePaths => !this.DuplicatePathSyntaxTrees.IsEmpty;

    private SyntaxTreeIndex( Dictionary<DocumentKey, SyntaxTree> syntaxTreesByDocumentKey, ImmutableArray<SyntaxTree> duplicatePathSyntaxTrees )
    {
        this._syntaxTreesByDocumentKey = syntaxTreesByDocumentKey;
        this.DuplicatePathSyntaxTrees = duplicatePathSyntaxTrees;
    }

    public static SyntaxTreeIndex Create( Compilation compilation )
    {
        var syntaxTreesByDocumentKey = new Dictionary<DocumentKey, SyntaxTree>();
        ImmutableArray<SyntaxTree>.Builder? duplicatesBuilder = null;

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            if ( !syntaxTreesByDocumentKey.TryAdd( syntaxTree.GetDocumentKey(), syntaxTree ) )
            {
                duplicatesBuilder ??= ImmutableArray.CreateBuilder<SyntaxTree>();
                duplicatesBuilder.Add( syntaxTree );
            }
        }

        return new SyntaxTreeIndex( syntaxTreesByDocumentKey, duplicatesBuilder?.ToImmutable() ?? ImmutableArray<SyntaxTree>.Empty );
    }
}
