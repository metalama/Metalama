// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using System.Collections.Concurrent;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends;

/// <summary>
/// Tests of <see cref="LayeredCachingBackendEnhancer"/> that control the clock.
/// </summary>
/// <remarks>
/// The enhancer stamps the items that it writes to the second layer, and it writes a tombstone with a transition
/// period into the first layer when an item is removed. When a value comes back from the second layer while a
/// tombstone is present in the first layer, the enhancer compares the two timestamps. These behaviours read the
/// clock, so they can only be tested when the clock is substitutable.
/// </remarks>
public sealed partial class LayeredCachingBackendEnhancerTests
{
    private const string _timeTestKey = "time-test-key";

    private static readonly DateTimeOffset _timeTestOrigin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    /// <summary>
    /// Creates a layered backend over the given second layer. The tombstone path of the enhancer is the one taken
    /// when the underlying backend is not blocking, so the wrapper reports that it is not.
    /// </summary>
    private static LayeredCachingBackendEnhancer CreateLayeredBackendOver( CachingBackend secondLayer )
    {
        var wrapper = new ConfigurableFeaturesBackend( secondLayer, blocking: false );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );
        layered.Initialize();

        return layered;
    }

    /// <summary>
    /// Verifies that the tombstone written into the first layer when an item is removed stops suppressing the value of
    /// the second layer once the transition period has elapsed.
    /// </summary>
    [Fact]
    public async Task RemovedItemTombstone_ExpiresAfterTransitionPeriod()
    {
        using var fakes = new FakeCachingServices( _timeTestOrigin );
        using var cancellationTokenSource = new CancellationTokenSource();

        var secondLayer = new TypePreservingBackend( serviceProvider: fakes.ServiceProvider );
        using var layered = CreateLayeredBackendOver( secondLayer );

        layered.SetItem( _timeTestKey, new CacheItem( "value" ) );
        var secondLayerItem = secondLayer.GetItem( _timeTestKey );
        Assert.NotNull( secondLayerItem );

        await fakes.AdvanceAsync( TimeSpan.FromSeconds( 5 ), cancellationTokenSource.Token );
        layered.RemoveItem( _timeTestKey );

        // Write the value back into the second layer, as another node of the cluster would. The tombstone is newer,
        // so it suppresses the value.
        secondLayer.SetItem( _timeTestKey, secondLayerItem );
        Assert.Null( layered.GetItem( _timeTestKey ) );

        // The transition period is one minute.
        await fakes.AdvanceAsync( TimeSpan.FromMinutes( 2 ), cancellationTokenSource.Token );

        var retrieved = layered.GetItem( _timeTestKey );
        Assert.NotNull( retrieved );
        Assert.Equal( "value", retrieved.Value );
    }

    /// <summary>
    /// Verifies that a value coming back from the second layer wins over the tombstone of the first layer when the
    /// timestamp of the value is newer than the timestamp of the tombstone.
    /// </summary>
    /// <remarks>
    /// The two layered backends have clocks that are ten minutes apart, which is the clock skew between two nodes of a
    /// cluster. The backend that writes the value has the clock that is ahead, and it writes before the other backend
    /// removes the item, so the outcome differs from the one that the wall clock would produce.
    /// </remarks>
    [Fact]
    public void GetItem_WhenSecondLayerValueIsNewerThanTombstone_ReturnsSecondLayerValue()
    {
        using var writerFakes = new FakeCachingServices( _timeTestOrigin + TimeSpan.FromMinutes( 10 ) );
        using var removerFakes = new FakeCachingServices( _timeTestOrigin );

        // The two nodes reach the same store, but each has its own clock.
        var store = new ConcurrentDictionary<string, CacheItem>();
        var writerSecondLayer = new TypePreservingBackend( serviceProvider: writerFakes.ServiceProvider, store: store );
        var removerSecondLayer = new TypePreservingBackend( serviceProvider: removerFakes.ServiceProvider, store: store );
        using var writer = CreateLayeredBackendOver( writerSecondLayer );
        using var remover = CreateLayeredBackendOver( removerSecondLayer );

        writer.SetItem( _timeTestKey, new CacheItem( "second-layer-value" ) );
        var secondLayerItem = writerSecondLayer.GetItem( _timeTestKey );
        Assert.NotNull( secondLayerItem );

        // Removing writes a tombstone into the first layer of the remover, and removes the item from the second layer.
        remover.RemoveItem( _timeTestKey );

        // Write the value back into the second layer, as another node of the cluster would.
        writerSecondLayer.SetItem( _timeTestKey, secondLayerItem );

        var retrieved = remover.GetItem( _timeTestKey );

        Assert.NotNull( retrieved );
        Assert.Equal( "second-layer-value", retrieved.Value );
    }

    /// <summary>
    /// Verifies that the tombstone of the first layer wins over a value coming back from the second layer when the
    /// timestamp of the value is older than the timestamp of the tombstone.
    /// </summary>
    [Fact]
    public async Task GetItem_WhenTombstoneIsNewerThanSecondLayerValue_ReturnsNull()
    {
        using var fakes = new FakeCachingServices( _timeTestOrigin );
        using var cancellationTokenSource = new CancellationTokenSource();

        var secondLayer = new TypePreservingBackend( serviceProvider: fakes.ServiceProvider );
        using var layered = CreateLayeredBackendOver( secondLayer );

        layered.SetItem( _timeTestKey, new CacheItem( "second-layer-value" ) );
        var secondLayerItem = secondLayer.GetItem( _timeTestKey );
        Assert.NotNull( secondLayerItem );

        // Move the clock forward, then remove the item, so that the tombstone is stamped after the value.
        await fakes.AdvanceAsync( TimeSpan.FromSeconds( 5 ), cancellationTokenSource.Token );
        layered.RemoveItem( _timeTestKey );

        // Write the value back into the second layer, as another node of the cluster would.
        secondLayer.SetItem( _timeTestKey, secondLayerItem );

        Assert.Null( layered.GetItem( _timeTestKey ) );
    }
}
