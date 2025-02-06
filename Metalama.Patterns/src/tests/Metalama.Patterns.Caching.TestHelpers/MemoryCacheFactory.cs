// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Serializers;
using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.TestHelpers;

public static class MemoryCacheFactory
{
    private static MemoryCache CreateCache() => new( new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMilliseconds( 10 ) } );

    public static CachingBackend CreateBackend( IServiceProvider? serviceProvider, string debugName = "test", bool withSerializer = false )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration() { DebugName = debugName, Serializer = withSerializer ? new JsonCachingSerializer() : null } )
                .WithMemoryCache( CreateCache() ),
            serviceProvider );

        return backend;
    }
}