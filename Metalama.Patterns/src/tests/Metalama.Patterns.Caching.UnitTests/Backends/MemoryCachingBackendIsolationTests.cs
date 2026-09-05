// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends;

/// <summary>
/// Tests that two instances of <see cref="MemoryCachingBackend"/> that share one
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> keep separate items and separate dependencies.
/// </summary>
/// <remarks>
/// An application that calls <c>AddMemoryCache</c> registers a single memory cache in its service container. Both
/// layers of a layered backend over an in-memory second layer then resolve that same instance, so the keys of the
/// backend must carry a discriminator for the backend instance.
/// </remarks>
public sealed class MemoryCachingBackendIsolationTests
{
    private static CachingBackend CreateBackend( IServiceProvider serviceProvider, string debugName )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = debugName } ),
            serviceProvider );

        backend.Initialize();

        return backend;
    }

    [Fact]
    public void TwoBackends_SharingOneMemoryCache_KeepSeparateItems()
    {
        using var fakes = new FakeCachingServices();
        using var first = CreateBackend( fakes.ServiceProvider, "first" );
        using var second = CreateBackend( fakes.ServiceProvider, "second" );

        const string key = "shared-key";

        second.SetItem( key, new CacheItem( "second-value" ) );

        Assert.Null( first.GetItem( key ) );

        first.SetItem( key, new CacheItem( "first-value" ) );

        Assert.Equal( "first-value", first.GetItem( key )?.Value );
        Assert.Equal( "second-value", second.GetItem( key )?.Value );
    }

    [Fact]
    public void TwoBackends_SharingOneMemoryCache_KeepSeparateDependencies()
    {
        using var fakes = new FakeCachingServices();
        using var first = CreateBackend( fakes.ServiceProvider, "first" );
        using var second = CreateBackend( fakes.ServiceProvider, "second" );

        const string key = "shared-key";
        const string dependency = "shared-dependency";

        first.SetItem( key, new CacheItem( "first-value", [dependency] ) );
        second.SetItem( key, new CacheItem( "second-value", [dependency] ) );

        first.InvalidateDependency( dependency );

        Assert.Null( first.GetItem( key ) );
        Assert.Equal( "second-value", second.GetItem( key )?.Value );
    }

    [Fact]
    public void LayeredBackend_OverInMemorySecondLayer_KeepsTheLayersSeparate()
    {
        using var fakes = new FakeCachingServices();

        // The second layer resolves the single memory cache of the service provider, and the enhancer builds its
        // first layer with the service provider of the second layer, so both layers share one store.
        using var secondLayer = CreateBackend( fakes.ServiceProvider, "L2" );
        using var layered = new LayeredCachingBackendEnhancer( secondLayer, null, null );
        layered.Initialize();

        const string key = "second-layer-only-key";

        secondLayer.SetItem( key, new CacheItem( "second-layer-value" ) );

        // The first layer must not observe what was written to the second layer alone.
        Assert.Null( layered.LocalCache.GetItem( key ) );

        // Writing through the layered backend populates the first layer, and the first layer keeps its own value.
        layered.SetItem( key, new CacheItem( "layered-value" ) );

        Assert.Equal( "layered-value", layered.LocalCache.GetItem( key )?.Value );
    }
}
