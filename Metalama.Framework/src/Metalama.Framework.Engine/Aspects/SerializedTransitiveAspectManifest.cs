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
public readonly struct SerializedTransitiveAspectManifest : IEquatable<SerializedTransitiveAspectManifest>
{
    /// <summary>
    /// Gets the serialized manifest. Default or empty when the project exports nothing to inherit.
    /// </summary>
    public ImmutableArray<byte> Bytes { get; }

    /// <summary>
    /// Gets an <see cref="T:System.IO.Hashing.XxHash64"/> of <see cref="Bytes"/>, or zero when there are no bytes.
    /// </summary>
    public long Hash { get; }

    private SerializedTransitiveAspectManifest( ImmutableArray<byte> bytes, long hash )
    {
        this.Bytes = bytes;
        this.Hash = hash;
    }

    /// <summary>
    /// Creates a <see cref="SerializedTransitiveAspectManifest"/> from its bytes, computing the hash. Returns the
    /// default value when there are no bytes, so that "nothing to inherit" has a single representation.
    /// </summary>
    public static SerializedTransitiveAspectManifest Create( ImmutableArray<byte> bytes )
    {
        if ( bytes.IsDefaultOrEmpty )
        {
            return default;
        }

        var hash = new XxHash64();
        hash.Append( bytes );

        return new SerializedTransitiveAspectManifest( bytes, (long) hash.GetCurrentHashAsUInt64() );
    }

    public bool IsDefaultOrEmpty => this.Bytes.IsDefaultOrEmpty;

    /// <summary>
    /// Determines whether two manifests have the same content, by comparing their hashes alone. See the remarks on
    /// <see cref="SerializedTransitiveAspectManifest"/> for why a hash equality is taken as an identity here.
    /// </summary>
    public bool Equals( SerializedTransitiveAspectManifest other ) => this.Hash == other.Hash;

    public override bool Equals( object? obj ) => obj is SerializedTransitiveAspectManifest other && this.Equals( other );

    public override int GetHashCode() => this.Hash.GetHashCode();

    public static bool operator ==( SerializedTransitiveAspectManifest left, SerializedTransitiveAspectManifest right )
        => left.Equals( right );

    public static bool operator !=( SerializedTransitiveAspectManifest left, SerializedTransitiveAspectManifest right )
        => !left.Equals( right );
}
