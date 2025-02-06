// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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