// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Serializers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Serializers;

public sealed class CacheItemSerializerTest
{
    private static CacheItem RoundLoop( CacheItem item )
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );
        var memoryStream = new MemoryStream();
        var writer = new BinaryWriter( memoryStream );
        serializer.Serialize( item, writer );
        memoryStream.Seek( 0, SeekOrigin.Begin );
        var reader = new BinaryReader( memoryStream );

        return serializer.Deserialize( reader, default );
    }

    [Fact]
    public void TestDefaultCacheItem()
    {
        var initialObject = new MyObject();
        var initialItem = new CacheItem( initialObject );
        var roundloopItem = RoundLoop( initialItem );

        Assert.Equal( initialObject.Value, ((MyObject) roundloopItem.Value!).Value );
    }

    [Fact]
    public void TestMaterializedCacheItem()
    {
        var initialObject = new MyObject();

        var initialConfiguration = new CacheItemConfiguration
        {
            SlidingExpiration = TimeSpan.FromMinutes( 5 ), AbsoluteExpiration = TimeSpan.FromHours( 10 ), Priority = CacheItemPriority.Low
        };

        var initialItem = new MaterializedCacheItem( new CacheItem( initialObject, default, initialConfiguration ) );
        var roundloopItem = RoundLoop( initialItem );

        Assert.Equal( initialObject.Value, ((MyObject) roundloopItem.Value!).Value );
        Assert.Equal( initialConfiguration.SlidingExpiration, roundloopItem.Configuration!.SlidingExpiration );
        Assert.Equal( initialConfiguration.Priority, roundloopItem.Configuration!.Priority );

        // The AbsoluteExpiration is computed from the system clock so we need to have some tolerance in the test.
        Assert.True( (initialConfiguration.AbsoluteExpiration.Value - roundloopItem.Configuration!.AbsoluteExpiration!.Value).TotalMinutes < 10 );
    }
}