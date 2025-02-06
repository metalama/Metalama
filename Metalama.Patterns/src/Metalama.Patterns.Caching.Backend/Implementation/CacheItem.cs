// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Serializers;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Represents an item being added to the cache.
/// </summary>
[PublicAPI]
public record CacheItem
{
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

    public object? Value { get; init; }

    public ImmutableArray<string> Dependencies { get; init; }

    public ICacheItemConfiguration? Configuration { get; init; }

    internal virtual void Serialize( BinaryWriter writer, ICachingSerializer serializer )
    {
        serializer.Serialize( this.Value, writer );
    }
}