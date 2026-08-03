// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel;

/// <summary>
/// Identifies a document of a project across the compilations of that project. This is the key of every design-time
/// cache that has to survive an edit.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SyntaxTree"/> is a transient artifact: the IDE produces a new <see cref="Compilation"/> holding new
/// trees on every keystroke, so reference identity distinguishes trees only <em>within</em> one compilation and cannot
/// key a cache that outlives it. The permanent identity of a document is therefore needed, and
/// <see cref="SyntaxTree.FilePath"/> is the only candidate reachable where it is needed: a Roslyn analyzer callback
/// carries a <see cref="SemanticModel"/> and nothing else, the cross-process contract passes a path, and
/// <c>WorkspaceProvider</c> returns no workspace at all in a supported host, so no <c>DocumentId</c> is available. See
/// issue #1742.
/// </para>
/// <para>
/// This type exists because a bare <see cref="string"/> made two mistakes easy. It let the comparer be chosen per
/// dictionary, which is how the design-time indexes came to compare paths ordinally while the command-line compiler
/// deduplicates them with <see cref="StringComparer.OrdinalIgnoreCase"/>. And it let one key space be confused with
/// another, because a document path, an introduced syntax tree name and a source generator hint name are all strings;
/// the duplicate-introduction assertion of <c>DesignTimeAspectPipelineResult</c> was exactly such a confusion. Keeping
/// document paths in a distinct type makes both a compile error.
/// </para>
/// <para>
/// The comparison is ordinal. Case-insensitive comparison would match the command-line compiler on Windows and be
/// wrong on Linux, where two paths differing in case are two files.
/// </para>
/// <para>
/// The hash code is computed once, on construction. These keys are long strings compared and hashed on every lookup of
/// the design-time hot path, so caching the hash is why this type is not slower than the string it replaces.
/// </para>
/// <para>
/// A key is a value, not a reference into a compilation. It does not guarantee that the document exists in any given
/// compilation, and it deliberately does not carry the <see cref="SyntaxTree"/>: a cache entry that held one would
/// root the syntax tree of an earlier compilation.
/// </para>
/// </remarks>
public readonly struct DocumentKey : IEquatable<DocumentKey>, IComparable<DocumentKey>
{
    private readonly string? _path;
    private readonly int _hashCode;

    /// <summary>
    /// Gets the path of the document, which is the <see cref="SyntaxTree.FilePath"/> of every syntax tree that
    /// represents it.
    /// </summary>
    public string Path => this._path ?? "";

    /// <summary>
    /// Gets a value indicating whether this key is the default value, which identifies no document.
    /// </summary>
    /// <remarks>
    /// Distinct from a key over the empty path. <c>SplitResultsByTree</c> files results that belong to the compilation
    /// rather than to a document under the empty path, so the empty path is a legitimate key with a defined meaning
    /// and must not be confused with the absence of a key.
    /// </remarks>
    public bool IsDefault => this._path == null;

    /// <remarks>
    /// Private, so that every key is created through a named factory. Which key space a string belongs to is the
    /// distinction this type exists to make, and a public constructor over <see cref="string"/> would let a name of
    /// another space be turned into a document key by an implicit conversion of intent.
    /// </remarks>
    private DocumentKey( string path )
    {
        this._path = path;
        this._hashCode = StringComparer.Ordinal.GetHashCode( path );
    }

    /// <summary>
    /// Gets the key of the document at a given path.
    /// </summary>
    public static DocumentKey FromPath( string path )
        => new( path ?? throw new ArgumentNullException( nameof(path) ) );

    /// <summary>
    /// Gets the key of the document a <see cref="SyntaxTree"/> belongs to.
    /// </summary>
    public static DocumentKey FromSyntaxTree( SyntaxTree syntaxTree ) => new( syntaxTree.FilePath );

    /// <summary>
    /// Gets the key under which results that belong to the compilation rather than to any document are filed.
    /// </summary>
    public static DocumentKey Compilation { get; } = new( "" );

    public bool Equals( DocumentKey other )
        => this._hashCode == other._hashCode && string.Equals( this._path, other._path, StringComparison.Ordinal );

    public override bool Equals( object? obj ) => obj is DocumentKey other && this.Equals( other );

    /// <summary>
    /// Compares two keys by path, ordinally.
    /// </summary>
    /// <remarks>
    /// Ordering exists so that anything enumerating a key-indexed collection can produce a stable sequence: test
    /// baselines, trace output and the manifest indexes all depend on an order that does not vary between runs.
    /// </remarks>
    public int CompareTo( DocumentKey other ) => string.CompareOrdinal( this._path, other._path );

    public override int GetHashCode() => this._hashCode;

    public static bool operator ==( DocumentKey left, DocumentKey right ) => left.Equals( right );

    public static bool operator !=( DocumentKey left, DocumentKey right ) => !left.Equals( right );

    public override string ToString() => this._path ?? "(none)";
}

/// <summary>
/// Extension methods to obtain a <see cref="DocumentKey"/>.
/// </summary>
public static class DocumentKeyExtensions
{
    /// <summary>
    /// Gets the key of the document a <see cref="SyntaxTree"/> belongs to.
    /// </summary>
    public static DocumentKey GetDocumentKey( this SyntaxTree syntaxTree ) => DocumentKey.FromSyntaxTree( syntaxTree );

    /// <summary>
    /// Gets the key of the document a <see cref="Location"/> points into, or <see langword="false"/> when the location
    /// is not in a document.
    /// </summary>
    public static bool TryGetDocumentKey( this Location location, out DocumentKey documentKey )
    {
        var path = location.GetLineSpan().Path;

        if ( path == null )
        {
            documentKey = default;

            return false;
        }

        documentKey = DocumentKey.FromPath( path );

        return true;
    }
}
