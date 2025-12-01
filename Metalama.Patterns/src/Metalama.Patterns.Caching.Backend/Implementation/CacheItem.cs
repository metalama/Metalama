// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Serializers;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Represents an item being added to the cache, including the cached value, its dependencies, and configuration.
/// </summary>
/// <seealso cref="ICacheItemConfiguration"/>
/// <seealso cref="Backends.CachingBackend"/>
[PublicAPI]
public record CacheItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheItem"/> class.
    /// </summary>
    /// <param name="value">The value to cache.</param>
    /// <param name="dependencies">Optional dependencies for cache invalidation.</param>
    /// <param name="configuration">Optional configuration for this cache item.</param>
    public CacheItem(
        object? value,
        ImmutableArray<string> dependencies = default,
        ICacheItemConfiguration? configuration = null )
    {
        this.Value = value;
        this.Dependencies = dependencies;
        this.Configuration = configuration;
    }

    private protected CacheItem() { }

    internal CacheItem( BinaryReader reader, ImmutableArray<string> dependencies, ICachingSerializer serializer )
    {
        this.Value = serializer.Deserialize( reader );
        this.Dependencies = dependencies;
    }

    /// <summary>
    /// Determines whether the current <see cref="CacheItem"/> is structurally equal to another <see cref="CacheItem"/>.
    /// </summary>
    /// <param name="other">A <see cref="CacheItem"/>.</param>
    /// <returns><c>true</c> both items are equal, otherwise <c>false</c>.</returns>
    public virtual bool Equals( CacheItem? other )
    {
        if ( ReferenceEquals( null, other ) )
        {
            return false;
        }

        if ( ReferenceEquals( this, other ) )
        {
            return true;
        }

        if ( !Equals( this.Value, other.Value ) )
        {
            return false;
        }

        if ( this.Dependencies.IsDefaultOrEmpty )
        {
            if ( !other.Dependencies.IsDefaultOrEmpty )
            {
                return false;
            }
        }
        else
        {
            if ( other.Dependencies.IsDefaultOrEmpty )
            {
                return false;
            }

            if ( other.Dependencies.Length != this.Dependencies.Length )
            {
                return false;
            }

            for ( var i = 0; i < this.Dependencies.Length; i++ )
            {
                if ( !string.Equals( this.Dependencies[i], other.Dependencies[i], StringComparison.Ordinal ) )
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 47;

            hashCode = (hashCode * 53) ^ (this.Value == null ? 0 : this.Value.GetHashCode());

            if ( !this.Dependencies.IsDefaultOrEmpty )
            {
                foreach ( var dependency in this.Dependencies )
                {
                    hashCode = (hashCode * 53) ^ StringComparer.Ordinal.GetHashCode( dependency );
                }
            }

            return hashCode;
        }
    }

    /// <summary>
    /// Gets or initializes the cached value.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets or initializes the dependencies associated with this cache item.
    /// </summary>
    public ImmutableArray<string> Dependencies { get; init; }

    /// <summary>
    /// Gets or initializes the configuration for this cache item.
    /// </summary>
    public ICacheItemConfiguration? Configuration { get; init; }

    internal virtual void Serialize( BinaryWriter writer, ICachingSerializer serializer )
    {
        serializer.Serialize( this.Value, writer );
    }
}