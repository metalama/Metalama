// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Serializers;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// A <see cref="CacheItem"/> where the configuration is materialized and must be serialized. Used by <see cref="LayeredCachingBackendEnhancer"/>.
/// </summary>
internal sealed record MaterializedCacheItem : CacheItem, ICacheItemConfiguration
{
    public MaterializedCacheItem( BinaryReader reader, ImmutableArray<string> dependencies, ICachingSerializer serializer )
    {
        var absoluteExpiration = reader.ReadInt64();

        if ( absoluteExpiration != 0 )
        {
            this.AbsoluteExpiration = DateTime.FromFileTimeUtc( absoluteExpiration );
        }

        var slidingExpiration = reader.ReadInt64();

        if ( slidingExpiration != 0 )
        {
            this.SlidingExpiration = TimeSpan.FromMilliseconds( slidingExpiration );
        }

        var priority = reader.ReadInt32();

        if ( priority != -1 )
        {
            this.Priority = (CacheItemPriority) priority;
        }

        this.Timestamp = reader.ReadInt64();
        this.Value = serializer.Deserialize( reader );
        this.Dependencies = dependencies;
        this.Configuration = this;
    }

    public MaterializedCacheItem( CacheItem cacheItem ) : base( cacheItem.Value, cacheItem.Dependencies, cacheItem.Configuration )
    {
        var absoluteExpiration = cacheItem.Configuration?.AbsoluteExpiration;

        if ( absoluteExpiration != null )
        {
            this.AbsoluteExpiration = DateTime.UtcNow + absoluteExpiration;
        }

        this.SlidingExpiration = cacheItem.Configuration?.SlidingExpiration;
        this.Priority = cacheItem.Configuration?.Priority;
        this.Timestamp = LayeredCachingBackendEnhancer.GetTimestamp();
    }

    /// <summary>
    /// Gets the timestamp of the cache item.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Gets the absolute expiration of the cache item.
    /// </summary>
    [PublicAPI]
    public DateTime? AbsoluteExpiration { get; }

    bool? ICacheItemConfiguration.AutoReload => false;

    TimeSpan? ICacheItemConfiguration.AbsoluteExpiration => this.AbsoluteExpiration?.Subtract( DateTime.UtcNow );

    string? ICacheItemConfiguration.ProfileName => null;

    /// <summary>
    /// Gets the sliding expiration of the cache item.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; }

    bool? ICacheItemConfiguration.IsEnabled => true;

    /// <summary>
    /// Gets the cache item priority.
    /// </summary>
    public CacheItemPriority? Priority { get; }

    internal override void Serialize( BinaryWriter writer, ICachingSerializer serializer )
    {
        writer.Write( this.AbsoluteExpiration?.ToFileTimeUtc() ?? 0 );           // Int64
        writer.Write( (long?) this.SlidingExpiration?.TotalMilliseconds ?? 0L ); // Int64
        writer.Write( (int?) this.Priority ?? -1 );                              // Int32
        writer.Write( this.Timestamp );                                          // Int64
        serializer.Serialize( this.Value, writer );
    }
}