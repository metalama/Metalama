// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Immutable;
using System.IO.Hashing;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// The transitive aspect manifest of a project in its serialized form, together with an
/// <see cref="T:System.IO.Hashing.XxHash64"/> of its bytes.
/// </summary>
/// <remarks>
/// <para>
/// The hash identifies the manifest by content rather than by the identity of the pipeline result that produced it.
/// That distinction is what makes it useful: a producing project yields a new result on every run, so anything
/// keyed on result identity treats the manifest as changed after every edit, whereas most edits do not touch the
/// exported surface at all and leave these bytes identical.
/// </para>
/// <para>
/// Two instances are equal when their hashes are equal. This treats a 64-bit hash as an identity rather than as a
/// mere filter, which is a deliberate and bounded risk: the manifests live only for the duration of a design-time
/// session and only a few hundred are in play at once, which puts the probability of a collision around 10^-14.
/// The bytes are therefore never compared, and equality costs one comparison of a <see cref="long"/>.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class SerializedTransitiveAspectManifest : IEquatable<SerializedTransitiveAspectManifest>
{
    private long? _hash;

    /// <summary>
    /// Gets the serialized manifest.
    /// </summary>
    public ImmutableArray<byte> Bytes { get; }

    private SerializedTransitiveAspectManifest( ImmutableArray<byte> bytes, long? hash )
    {
        Invariant.AssertNot( bytes.IsDefault );
        
        this.Bytes = bytes;
        this._hash = hash;
    }
    
    /// <summary>
    /// Gets an <see cref="T:System.IO.Hashing.XxHash64"/> of <see cref="Bytes"/>, computed on first access unless
    /// the producer supplied it.
    /// </summary>
    public long Hash => this._hash ??= this.ComputeHash();

    private long ComputeHash()
    {
        var hash = new XxHash64();
        hash.Append( this.Bytes );

        return (long) hash.GetCurrentHashAsUInt64();
    }

    /// <summary>
    /// Creates a <see cref="SerializedTransitiveAspectManifest"/> from its bytes, hashing them on first access.
    /// </summary>
    /// <remarks>
    /// Wrapping is all this does: a project with nothing to inherit is represented by a <c>null</c> manifest, never
    /// by one holding no bytes, so callers decide absence before they get here.
    /// </remarks>
    public static SerializedTransitiveAspectManifest Create( ImmutableArray<byte> bytes )
        => new( bytes, null );

    /// <summary>
    /// Creates a <see cref="SerializedTransitiveAspectManifest"/> from its bytes and a hash already computed over
    /// exactly those bytes, skipping the hashing.
    /// </summary>
    /// <remarks>
    /// For a manifest received from a project built against a different version of Metalama, whose producer hashed
    /// the bytes it sent and reports the result through <c>ITransitiveCompilationResult2.ManifestHash</c>. The hash
    /// is taken on trust: it is treated as an identity here (see the remarks on this type), so passing a hash of
    /// anything other than <paramref name="bytes"/> would make two different manifests compare equal.
    /// </remarks>
    public static SerializedTransitiveAspectManifest Create( ImmutableArray<byte> bytes, long? hash ) => new( bytes, hash );

    /// <summary>
    /// Determines whether two manifests have the same content, by comparing their hashes alone. See the remarks on
    /// <see cref="SerializedTransitiveAspectManifest"/> for why a hash equality is taken as an identity here.
    /// </summary>
    public bool Equals( SerializedTransitiveAspectManifest? other ) => other != null && this.Hash == other.Hash;

    public override bool Equals( object? obj ) => obj is SerializedTransitiveAspectManifest other && this.Equals( other );

    public override int GetHashCode() => this.Hash.GetHashCode();

    // ReSharper disable once ArrangeRedundantParentheses
    public static bool operator ==( SerializedTransitiveAspectManifest? left, SerializedTransitiveAspectManifest? right )
        => left?.Equals( right ) ?? (right is null);

    public static bool operator !=( SerializedTransitiveAspectManifest? left, SerializedTransitiveAspectManifest? right )
        => !(left == right);
}
