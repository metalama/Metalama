// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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