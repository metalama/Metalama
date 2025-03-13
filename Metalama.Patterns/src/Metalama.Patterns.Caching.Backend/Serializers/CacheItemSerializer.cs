// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Serializers;

public sealed class CacheItemSerializer
{
    private const byte _defaultCacheItemMarker = 0;
    private const byte _materializedCacheItemMarker = 1;
    private readonly ICachingSerializer _serializer;

    public CacheItemSerializer( ICachingSerializer serializer )
    {
        this._serializer = serializer;
    }

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