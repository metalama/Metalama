// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using System.Collections;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Exposes the profiles registered in the <see cref="CachingService"/>.
/// </summary>
[PublicAPI]
public sealed class CachingProfileRegistry : IReadOnlyCollection<CachingProfile>
{
    private readonly ImmutableDictionary<string, CachingProfile> _profiles;

    internal CachingProfileRegistry( ImmutableDictionary<string, CachingProfile> profiles )
    {
        this._profiles = profiles;
        this.AllBackends = profiles.Select( x => x.Value.Backend ).ToImmutableHashSet();
    }

    internal ImmutableHashSet<CachingBackend> AllBackends { get; }

    /// <summary>
    /// Gets the default <see cref="CachingProfile"/>.
    /// </summary>
    public CachingProfile Default => this[CachingProfile.DefaultName];

    /// <summary>
    /// Gets a <see cref="CachingProfile"/> of a given name. If no profile exists, a <see cref="KeyNotFoundException"/> is thrown.
    /// </summary>
    /// <param name="profileName">The profile name (a case-insensitive string).</param>
    /// <returns>A <see cref="CachingProfile"/> object with name <paramref name="profileName"/>.</returns>
    public CachingProfile this[ string? profileName ]
    {
        get
        {
            profileName ??= CachingProfile.DefaultName;

            if ( !this._profiles.TryGetValue( profileName, out var profile ) )
            {
                throw new KeyNotFoundException( $"The caching profile '{profileName}' has not been defined." );
            }

            return profile;
        }
    }

    // ReSharper disable once NotDisposedResourceIsReturned
    public IEnumerator<CachingProfile> GetEnumerator() => this._profiles.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this._profiles.Count;
}