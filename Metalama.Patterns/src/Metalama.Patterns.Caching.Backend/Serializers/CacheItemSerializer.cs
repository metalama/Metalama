// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// Serializes and deserializes <see cref="CacheItem"/> instances for storage in distributed caching backends.
/// </summary>
/// <remarks>
/// <para>This class wraps an <see cref="ICachingSerializer"/> and adds a marker byte to distinguish between
/// different <see cref="CacheItem"/> types (standard vs. materialized) during deserialization.</para>
/// </remarks>
/// <seealso cref="ICachingSerializer"/>
/// <seealso cref="CacheItem"/>
public sealed class CacheItemSerializer
{
    private const byte _defaultCacheItemMarker = 0;
    private const byte _materializedCacheItemMarker = 1;
    private readonly ICachingSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheItemSerializer"/> class.
    /// </summary>
    /// <param name="serializer">The underlying serializer used for the cached value.</param>
    public CacheItemSerializer( ICachingSerializer serializer )
    {
        this._serializer = serializer;
    }

    /// <summary>
    /// Serializes a <see cref="CacheItem"/> to a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="cacheItem">The cache item to serialize.</param>
    /// <param name="writer">The writer to serialize to.</param>
    public void Serialize( CacheItem cacheItem, BinaryWriter writer )
    {
        var marker = cacheItem switch
        {
            MaterializedCacheItem => _materializedCacheItemMarker,
            _ => _defaultCacheItemMarker
        };

        writer.Write( marker );

        cacheItem.Serialize( writer, this._serializer );
    }

    /// <summary>
    /// Deserializes a <see cref="CacheItem"/> from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to deserialize from.</param>
    /// <param name="dependencies">The dependencies associated with the cache item.</param>
    /// <returns>The deserialized <see cref="CacheItem"/>.</returns>
    /// <exception cref="InvalidCacheItemException">Thrown when the cache item marker is not recognized.</exception>
    public CacheItem Deserialize( BinaryReader reader, ImmutableArray<string> dependencies )
    {
        var marker = reader.ReadByte();

        return marker switch
        {
            _defaultCacheItemMarker => new CacheItem( reader, dependencies, this._serializer ),
            _materializedCacheItemMarker => new MaterializedCacheItem( reader, dependencies, this._serializer ),
            _ => throw new InvalidCacheItemException()
        };
    }
}