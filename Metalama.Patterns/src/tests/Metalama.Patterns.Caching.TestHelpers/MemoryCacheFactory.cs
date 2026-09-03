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
    private static MemoryCache CreateCache() => new( new MemoryCacheOptions() );

    /// <summary>
    /// Creates a backend that stores its items in memory.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the backend.</param>
    /// <param name="debugName">The debug name of the backend.</param>
    /// <param name="withSerializer">A value indicating whether the backend serializes its values.</param>
    /// <param name="memoryCache">
    /// The store of the backend, or <see langword="null"/> to give the backend a new <see cref="MemoryCache"/> of its own.
    /// A test that substitutes the clock passes the <see cref="FakeMemoryCache"/> of its <see cref="FakeCachingServices"/>,
    /// because a <see cref="MemoryCache"/> reads the wall clock and would not expire its items when the fake clock advances.
    /// </param>
    public static CachingBackend CreateBackend(
        IServiceProvider? serviceProvider,
        string debugName = "test",
        bool withSerializer = false,
        IMemoryCache? memoryCache = null )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration() { DebugName = debugName, Serializer = withSerializer ? new JsonCachingSerializer() : null } )
                .WithMemoryCache( memoryCache ?? CreateCache() ),
            serviceProvider );

        return backend;
    }
}