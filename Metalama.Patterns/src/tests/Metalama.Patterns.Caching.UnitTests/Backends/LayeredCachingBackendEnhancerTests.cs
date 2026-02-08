// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable UseAwaitUsing
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace Metalama.Patterns.Caching.Tests.Backends;

/// <summary>
/// Tests for <see cref="LayeredCachingBackendEnhancer"/>.
/// </summary>
public sealed partial class LayeredCachingBackendEnhancerTests : IDisposable
{
    private const int _timeout = 30_000;

    private readonly ServiceProvider _serviceProvider;

    public LayeredCachingBackendEnhancerTests( ITestOutputHelper testOutputHelper )
    {
        _ = testOutputHelper; // Available for debugging if needed
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    private LayeredCachingBackendEnhancer CreateLayeredBackend(
        bool blocking = true,
        CachingBackend? customL2 = null,
        MemoryCachingBackend? customL1 = null )
    {
        var l2 = customL2 ?? MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );
        var wrapper = new ConfigurableFeaturesBackend( l2, blocking: blocking );
        var layered = new LayeredCachingBackendEnhancer( wrapper, customL1, null );
        layered.Initialize();

        return layered;
    }

    /// <summary>
    /// Creates a layered backend using <see cref="TypePreservingBackend"/> as L2,
    /// which preserves the <see cref="MaterializedCacheItem"/> type for L2 fallback testing.
    /// </summary>
    private static LayeredCachingBackendEnhancer CreateLayeredBackendWithTypePreservingL2( bool blocking = true )
    {
        var l2 = new TypePreservingBackend( blocking );
        var wrapper = new ConfigurableFeaturesBackend( l2, blocking: blocking );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );
        layered.Initialize();

        return layered;
    }

    #region L1/L2 Basic Operations

    [Fact]
    public void SetItem_StoresInL1AndL2()
    {
        using var layered = this.CreateLayeredBackend();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );

        // L1 should have the item
        var l1Item = layered.LocalCache.GetItem( key );
        Assert.NotNull( l1Item );
        Assert.Equal( "test-value", l1Item.Value );

        // L2 should also have an item
        var l2Item = layered.UnderlyingBackend.GetItem( key );
        Assert.NotNull( l2Item );
    }

    [Fact( Timeout = _timeout )]
    public async Task SetItem_Async_StoresInBothLayers()
    {
        using var layered = this.CreateLayeredBackend();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        await layered.SetItemAsync( key, item );

        // L1 should have the item
        var l1Item = layered.LocalCache.GetItem( key );
        Assert.NotNull( l1Item );

        // L2 should have the item
        var l2Item = layered.UnderlyingBackend.GetItem( key );
        Assert.NotNull( l2Item );
    }

    [Fact]
    public void GetItem_WhenInL1_ReturnsL1Item()
    {
        using var layered = this.CreateLayeredBackend();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );

        // Get should return the item from L1
        var retrieved = layered.GetItem( key );
        Assert.NotNull( retrieved );
        Assert.Equal( "test-value", retrieved.Value );
    }

    [Fact]
    public void GetItem_WhenNotInEitherCache_ReturnsNull()
    {
        using var layered = this.CreateLayeredBackend();

        var retrieved = layered.GetItem( "non-existent-key" );
        Assert.Null( retrieved );
    }

    [Fact]
    public void ContainsItem_BlockingBackend_ChecksBothLayers()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        // Set item through layered backend
        var item = new CacheItem( "test-value" );
        layered.SetItem( "test-key", item );

        // Clear L1 to force check to hit L2
        layered.LocalCache.Clear();

        // Should find item even though it's only in L2
        Assert.True( layered.ContainsItem( "test-key" ) );

        // Non-existent key should return false
        Assert.False( layered.ContainsItem( "non-existent" ) );
    }

    #endregion

    #region RemovedValue Markers - Non-Blocking Backend

    [Fact]
    public void RemoveItem_NonBlocking_SetsRemovedValueMarker()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );
        layered.RemoveItem( key );

        // L1 should have a RemovedValue marker (which GetItem returns as null)
        var retrieved = layered.GetItem( key );
        Assert.Null( retrieved );
    }

    [Fact]
    public void InvalidateDependency_NonBlocking_SetsRemovedValueMarker()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        const string key = "test-key";
        const string dependency = "dep1";
        var item = new CacheItem( "test-value", [dependency] );

        layered.SetItem( key, item );
        layered.InvalidateDependency( dependency );

        // The item should be removed (marker set in L1)
        var retrieved = layered.GetItem( key );
        Assert.Null( retrieved );
    }

    [Fact]
    public void GetItem_WithMarker_AndL2HasOlderItem_ReturnsNull()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        var item = new CacheItem( "value" );
        layered.SetItem( "test-key", item );
        layered.RemoveItem( "test-key" ); // Creates RemovedValue marker with current timestamp

        // The L2 item has an older timestamp than the marker, so should return null
        var retrieved = layered.GetItem( "test-key" );
        Assert.Null( retrieved );
    }

    [Fact]
    public void GetItem_WithMarker_AndL2HasNoItem_ReturnsNull()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );
        layered.RemoveItem( key );

        // Both L1 (marker) and L2 (item removed) - should return null
        var retrieved = layered.GetItem( key );
        Assert.Null( retrieved );
    }

    [Fact]
    public void RemoveItem_BlockingBackend_DirectlyRemoves()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );
        layered.RemoveItem( key );

        // With blocking backend, item is directly removed from L1
        var retrieved = layered.GetItem( key );
        Assert.Null( retrieved );
    }

    [Fact]
    public void InvalidateDependency_BlockingBackend_DirectlyInvalidates()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        const string key = "test-key";
        const string dependency = "dep1";
        var item = new CacheItem( "test-value", [dependency] );

        layered.SetItem( key, item );
        layered.InvalidateDependency( dependency );

        var retrieved = layered.GetItem( key );
        Assert.Null( retrieved );
    }

    #endregion

    #region Invalidation Propagation

    [Fact( Timeout = _timeout )]
    public async Task OnBackendDependencyInvalidated_FromL2_InvalidatesL1()
    {
        using var l2 = MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );
        l2.Initialize();

        // Create layered backend - this subscribes to L2's events via the wrapper
        var wrapper = new ConfigurableFeaturesBackend( l2 );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );
        layered.Initialize();

        try
        {
            const string key = "test-key";
            const string dependency = "dep1";
            var item = new CacheItem( "test-value", [dependency] );

            layered.SetItem( key, item );

            // Set up event tracking
            var eventReceived = new TaskCompletionSource<bool>();

            layered.DependencyInvalidated += ( _, args ) =>
            {
                if ( args.Key == dependency )
                {
                    eventReceived.TrySetResult( true );
                }
            };

            // Invalidate the dependency through the wrapper (which triggers the L2 invalidation)
            wrapper.InvalidateDependency( dependency );

            // Wait for event propagation
            using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
            await eventReceived.Task.WaitWithTimeoutAsync();

            // The item should be removed from L1 as well
            await Task.Delay( 100, cts.Token );
            var retrieved = layered.LocalCache.GetItem( key );
            Assert.Null( retrieved );
        }
        finally
        {
            layered.Dispose();
        }
    }

    [Fact( Timeout = _timeout )]
    public async Task OnBackendItemRemoved_FromL2_RemovesFromL1()
    {
        using var l2 = MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );
        l2.Initialize();

        // Create layered backend
        var wrapper = new ConfigurableFeaturesBackend( l2 );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );
        layered.Initialize();

        try
        {
            const string key = "test-key";
            var item = new CacheItem( "test-value" );

            layered.SetItem( key, item );

            // Set up event tracking
            var eventReceived = new TaskCompletionSource<bool>();

            layered.ItemRemoved += ( _, args ) =>
            {
                if ( args.Key == key )
                {
                    eventReceived.TrySetResult( true );
                }
            };

            // Remove the item through the wrapper (which triggers the L2 removal)
            wrapper.RemoveItem( key );

            // Wait for event propagation
            using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
            await eventReceived.Task.WaitWithTimeoutAsync();

            // The item should be removed from L1 as well
            await Task.Delay( 100, cts.Token );
            var retrieved = layered.LocalCache.GetItem( key );
            Assert.Null( retrieved );
        }
        finally
        {
            layered.Dispose();
        }
    }

    [Fact( Timeout = _timeout )]
    public async Task Events_PropagatedToExternalListeners()
    {
        using var layered = this.CreateLayeredBackend();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        var itemRemovedReceived = new TaskCompletionSource<CacheItemRemovedEventArgs>();

        layered.ItemRemoved += ( _, args ) =>
        {
            if ( args.Key == key )
            {
                itemRemovedReceived.TrySetResult( args );
            }
        };

        layered.SetItem( key, item );
        layered.RemoveItem( key );

        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
        await itemRemovedReceived.Task.WaitWithTimeoutAsync();

        var args = await itemRemovedReceived.Task;
        Assert.Equal( key, args.Key );
        Assert.Equal( CacheItemRemovedReason.Removed, args.RemovedReason );
    }

    #endregion

    #region ContainsDependency

    [Fact]
    public void ContainsDependency_BlockingBackend_ChecksBothLayers()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        const string dependency = "dep1";
        var item = new CacheItem( "test-value", [dependency] );

        // Set item through layered backend
        layered.SetItem( "test-key", item );

        // Should find dependency
        Assert.True( layered.ContainsDependency( dependency ) );
    }

    [Fact]
    public void ContainsDependency_NonBlocking_ThrowsNotSupportedException()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        Assert.Throws<NotSupportedException>( () => layered.ContainsDependency( "dep1" ) );
    }

    [Fact]
    public void Features_ContainsDependency_FalseForNonBlocking()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        Assert.False( layered.SupportedFeatures.ContainsDependency );
    }

    [Fact]
    public void Features_ContainsDependency_TrueForBlocking()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        Assert.True( layered.SupportedFeatures.ContainsDependency );
    }

    #endregion

    #region Clear Operations

    [Fact]
    public void Clear_Default_ClearsBothL1AndL2()
    {
        using var layered = this.CreateLayeredBackend();

        layered.SetItem( "key1", new CacheItem( "value1" ) );
        layered.SetItem( "key2", new CacheItem( "value2" ) );

        layered.Clear();

        Assert.Null( layered.GetItem( "key1" ) );
        Assert.Null( layered.GetItem( "key2" ) );

        // Both L1 and L2 should be cleared
        Assert.Null( layered.LocalCache.GetItem( "key1" ) );
        Assert.Null( layered.UnderlyingBackend.GetItem( "key1" ) );
    }

    [Fact]
    public void Clear_Local_ClearsOnlyL1()
    {
        using var layered = this.CreateLayeredBackend();

        layered.SetItem( "key1", new CacheItem( "value1" ) );

        layered.Clear( ClearCacheOptions.Local );

        // L1 should be cleared
        Assert.Null( layered.LocalCache.GetItem( "key1" ) );

        // L2 should still have the item
        Assert.NotNull( layered.UnderlyingBackend.GetItem( "key1" ) );
    }

    [Fact]
    public void Clear_Compact_ClearsOnlyL1()
    {
        using var layered = this.CreateLayeredBackend();

        layered.SetItem( "key1", new CacheItem( "value1" ) );

        layered.Clear( ClearCacheOptions.Compact );

        // L1 should be cleared (or compacted)
        Assert.Null( layered.LocalCache.GetItem( "key1" ) );

        // L2 should still have the item
        Assert.NotNull( layered.UnderlyingBackend.GetItem( "key1" ) );
    }

    #endregion

    #region Initialization & Disposal

    [Fact]
    public void Initialize_InitializesBothL1AndUnderlying()
    {
        using var l2 = MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );

        // Don't initialize L2 - let the layered backend do it
        Assert.Equal( CachingBackendStatus.Default, l2.Status );

        var wrapper = new ConfigurableFeaturesBackend( l2 );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );

        Assert.Equal( CachingBackendStatus.Default, layered.Status );
        Assert.Equal( CachingBackendStatus.Default, layered.LocalCache.Status );

        layered.Initialize();

        Assert.Equal( CachingBackendStatus.Initialized, layered.Status );
        Assert.Equal( CachingBackendStatus.Initialized, layered.LocalCache.Status );
        Assert.Equal( CachingBackendStatus.Initialized, l2.Status );

        layered.Dispose();
    }

    [Fact]
    public void Dispose_DisposesBothL1AndUnderlying()
    {
        var l2 = MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );
        var wrapper = new ConfigurableFeaturesBackend( l2 );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );

        layered.Initialize();
        layered.Dispose();

        Assert.Equal( CachingBackendStatus.Disposed, layered.Status );
        Assert.Equal( CachingBackendStatus.Disposed, layered.LocalCache.Status );
        Assert.Equal( CachingBackendStatus.Disposed, l2.Status );
    }

    [Fact( Timeout = _timeout )]
    public async Task DisposeAsync_DisposesBothL1AndUnderlying()
    {
        var l2 = MemoryCacheFactory.CreateBackend( this._serviceProvider, "L2" );
        var wrapper = new ConfigurableFeaturesBackend( l2 );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );

        await layered.InitializeAsync();
        await layered.DisposeAsync();

        Assert.Equal( CachingBackendStatus.Disposed, layered.Status );
        Assert.Equal( CachingBackendStatus.Disposed, layered.LocalCache.Status );
        Assert.Equal( CachingBackendStatus.Disposed, l2.Status );
    }

    #endregion

    #region Async Variants

    [Fact( Timeout = _timeout )]
    public async Task ContainsItemAsync_BlockingBackend_ChecksBothLayers()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        // Set item through the layered backend
        var item = new CacheItem( "test-value" );
        layered.SetItem( "test-key", item );

        // Clear L1 to force check to hit L2
        layered.LocalCache.Clear();

        Assert.True( await layered.ContainsItemAsync( "test-key" ) );
        Assert.False( await layered.ContainsItemAsync( "non-existent" ) );
    }

    [Fact( Timeout = _timeout )]
    public async Task RemoveItemAsync_NonBlocking_SetsRemovedValueMarker()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        await layered.SetItemAsync( key, item );
        await layered.RemoveItemAsync( key );

        var retrieved = await layered.GetItemAsync( key );
        Assert.Null( retrieved );
    }

    [Fact( Timeout = _timeout )]
    public async Task InvalidateDependencyAsync_NonBlocking_SetsRemovedValueMarker()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        const string key = "test-key";
        const string dependency = "dep1";
        var item = new CacheItem( "test-value", [dependency] );

        await layered.SetItemAsync( key, item );
        await layered.InvalidateDependencyAsync( dependency );

        var retrieved = await layered.GetItemAsync( key );
        Assert.Null( retrieved );
    }

    [Fact( Timeout = _timeout )]
    public async Task ClearAsync_Default_ClearsBothL1AndL2()
    {
        using var layered = this.CreateLayeredBackend();

        await layered.SetItemAsync( "key1", new CacheItem( "value1" ) );
        await layered.SetItemAsync( "key2", new CacheItem( "value2" ) );

        await layered.ClearAsync();

        Assert.Null( await layered.GetItemAsync( "key1" ) );
        Assert.Null( await layered.GetItemAsync( "key2" ) );
    }

    [Fact( Timeout = _timeout )]
    public async Task ContainsDependencyAsync_BlockingBackend_ChecksBothLayers()
    {
        using var layered = this.CreateLayeredBackend( blocking: true );

        const string dependency = "dep1";
        var item = new CacheItem( "test-value", [dependency] );

        // Set item through layered backend
        layered.SetItem( "test-key", item );

        Assert.True( await layered.ContainsDependencyAsync( dependency ) );
    }

    [Fact( Timeout = _timeout )]
    public async Task ContainsDependencyAsync_NonBlocking_ThrowsNotSupportedException()
    {
        using var layered = this.CreateLayeredBackend( blocking: false );

        await Assert.ThrowsAsync<NotSupportedException>( async () => await layered.ContainsDependencyAsync( "dep1" ) );
    }

    #endregion

    #region L2 Fallback (using TypePreservingBackend)

    [Fact]
    public void GetItem_WhenNotInL1ButInL2_PopulatesL1()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        // Set item through layered backend
        var originalItem = new CacheItem( "test-value" );
        layered.SetItem( "test-key", originalItem );

        // Clear L1 only
        layered.Clear( ClearCacheOptions.Local );

        // L1 should not have the item
        Assert.Null( layered.LocalCache.GetItem( "test-key" ) );

        // L2 should still have the item (TypePreservingBackend preserves MaterializedCacheItem)
        var l2Item = layered.UnderlyingBackend.GetItem( "test-key" );
        Assert.NotNull( l2Item );
        Assert.IsType<MaterializedCacheItem>( l2Item );

        // Get through layered - should fetch from L2 and populate L1
        var retrieved = layered.GetItem( "test-key" );
        Assert.NotNull( retrieved );
        Assert.Equal( "test-value", retrieved.Value );

        // Now L1 should have the item
        var l1ItemAfter = layered.LocalCache.GetItem( "test-key" );
        Assert.NotNull( l1ItemAfter );
    }

    [Fact( Timeout = _timeout )]
    public async Task GetItemAsync_WhenNotInL1ButInL2_PopulatesL1()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        // Set item through layered backend
        var originalItem = new CacheItem( "test-value" );
        await layered.SetItemAsync( "test-key", originalItem );

        // Clear L1 only
        await layered.ClearAsync( ClearCacheOptions.Local );

        Assert.Null( layered.LocalCache.GetItem( "test-key" ) );

        var retrieved = await layered.GetItemAsync( "test-key" );
        Assert.NotNull( retrieved );
        Assert.Equal( "test-value", retrieved.Value );

        var l1Item = layered.LocalCache.GetItem( "test-key" );
        Assert.NotNull( l1Item );
    }

    [Fact]
    public void GetItem_WithMarker_AndL2HasNewerItem_ReturnsL2Item()
    {
        using var l2 = new TypePreservingBackend();
        var wrapper = new ConfigurableFeaturesBackend( l2, blocking: false );
        var layered = new LayeredCachingBackendEnhancer( wrapper, null, null );
        layered.Initialize();

        try
        {
            // Set item through layered backend, then remove it (creating marker)
            var item = new CacheItem( "initial-value" );
            layered.SetItem( "test-key", item );
            layered.RemoveItem( "test-key" ); // Creates marker with current timestamp

            // Ensure the clock has advanced so the newer item gets a strictly greater timestamp
            // than the RemovedValue marker. Without this, both timestamps can be identical
            // (same tick), causing the test to be flaky.
            var timestampAfterRemove = LayeredCachingBackendEnhancer.GetTimestamp();

            SpinWait.SpinUntil( () => LayeredCachingBackendEnhancer.GetTimestamp() > timestampAfterRemove );

            // Now set a newer item directly in L2 (simulating another node updating cache)
            var newerItem = new MaterializedCacheItem( new CacheItem( "newer-value" ) );
            l2.SetItem( "test-key", newerItem );

            // The L2 item has a newer timestamp than the marker, so should return the L2 item
            var retrieved = layered.GetItem( "test-key" );
            Assert.NotNull( retrieved );
            Assert.Equal( "newer-value", retrieved.Value );
        }
        finally
        {
            layered.Dispose();
        }
    }

    [Fact]
    public void GetItem_AfterL1ClearLocal_RetrievesFromL2AndPopulatesL1()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        layered.SetItem( key, item );

        // Clear L1 only using Local option
        layered.Clear( ClearCacheOptions.Local );

        // L1 should be empty
        Assert.Null( layered.LocalCache.GetItem( key ) );

        // Get through layered - should fetch from L2
        var retrieved = layered.GetItem( key );
        Assert.NotNull( retrieved );
        Assert.Equal( "test-value", retrieved.Value );

        // L1 should now be populated
        var l1Item = layered.LocalCache.GetItem( key );
        Assert.NotNull( l1Item );
    }

    [Fact( Timeout = _timeout )]
    public async Task GetItemAsync_AfterL1ClearLocal_RetrievesFromL2AndPopulatesL1()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        await layered.SetItemAsync( key, item );

        // Clear L1 only using Local option
        await layered.ClearAsync( ClearCacheOptions.Local );

        // L1 should be empty
        Assert.Null( layered.LocalCache.GetItem( key ) );

        // Get through layered - should fetch from L2
        var retrieved = await layered.GetItemAsync( key );
        Assert.NotNull( retrieved );
        Assert.Equal( "test-value", retrieved.Value );

        // L1 should now be populated
        var l1Item = layered.LocalCache.GetItem( key );
        Assert.NotNull( l1Item );
    }

    [Fact]
    public void MaterializedCacheItem_PreservesTimestamp()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        const string key = "test-key";
        var item = new CacheItem( "test-value" );

        var beforeSet = DateTimeOffset.UtcNow.UtcTicks;
        layered.SetItem( key, item );
        var afterSet = DateTimeOffset.UtcNow.UtcTicks;

        // Get the MaterializedCacheItem from L2
        var l2Item = layered.UnderlyingBackend.GetItem( key );
        Assert.NotNull( l2Item );
        Assert.IsType<MaterializedCacheItem>( l2Item );

        var materializedItem = (MaterializedCacheItem) l2Item;

        // Timestamp should be within the expected range
        Assert.True( materializedItem.Timestamp >= beforeSet );
        Assert.True( materializedItem.Timestamp <= afterSet );
    }

    [Fact]
    public void MaterializedCacheItem_PreservesConfiguration()
    {
        using var layered = CreateLayeredBackendWithTypePreservingL2();

        const string key = "test-key";
        var config = new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 5 ) };
        var item = new CacheItem( "test-value", configuration: config );

        layered.SetItem( key, item );

        // Get the MaterializedCacheItem from L2
        var l2Item = layered.UnderlyingBackend.GetItem( key );
        Assert.NotNull( l2Item );
        Assert.IsType<MaterializedCacheItem>( l2Item );

        var materializedItem = (MaterializedCacheItem) l2Item;

        // The configuration should be materialized
        Assert.NotNull( materializedItem.AbsoluteExpiration );
    }

    #endregion

    #region Features

    [Fact]
    public void Features_BlockingIsInherited()
    {
        using var blockingLayered = this.CreateLayeredBackend( blocking: true );
        using var nonBlockingLayered = this.CreateLayeredBackend( blocking: false );

        Assert.True( blockingLayered.SupportedFeatures.Blocking );
        Assert.False( nonBlockingLayered.SupportedFeatures.Blocking );
    }

    [Fact]
    public void Features_DependenciesIsInherited()
    {
        using var withDeps = this.CreateLayeredBackend( blocking: true );
        Assert.True( withDeps.SupportedFeatures.Dependencies );
    }

    #endregion
}