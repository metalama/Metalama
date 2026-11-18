// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that <see cref="CachingBackend.SetItem"/> in <see cref="MemoryCachingBackend"/> removes the items that depend
/// on the key when it replaces a value.
/// </summary>
/// <remarks>
/// An item that depends on the key of another item was computed from the value of that item. When that value is
/// replaced, for example because the cached method is refreshed, the dependent item is no longer consistent with it, so
/// the backend removes it, with the items that depend on it. The Redis backend has the same behavior.
/// </remarks>
public sealed class MemoryCachingBackendReplacementTests
{
    /// <summary>
    /// The key of the item whose value is replaced.
    /// </summary>
    private const string _innerKey = "inner";

    /// <summary>
    /// The key of an item that depends on <see cref="_innerKey"/>.
    /// </summary>
    private const string _outerKey = "outer";

    /// <summary>
    /// The key of an item that depends on <see cref="_outerKey"/>.
    /// </summary>
    private const string _outermostKey = "outermost";

    /// <summary>
    /// A dependency of the first value of <see cref="_innerKey"/>, in the tests in which that value has a dependency.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The maximum time to wait for the pending work items. It only detects a failure of the test.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// Verifies that the replacement of a value removes the items that depend on the key, directly and transitively, and
    /// raises the <see cref="CachingBackend.ItemRemoved"/> event for each of them.
    /// </summary>
    [Fact]
    public async Task SetItem_OverExistingValue_RemovesDependentItems()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _innerKey, new CacheItem( "inner-value" ) );
        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_innerKey] ) );
        backend.SetItem( _outermostKey, new CacheItem( "outermost-value", [_outerKey] ) );

        var removedItems = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedItems.Enqueue( args );

        backend.SetItem( _innerKey, new CacheItem( "new-inner-value" ) );

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellation.Token );

        Assert.Equal( "new-inner-value", backend.GetItem( _innerKey )?.Value );
        Assert.False( backend.ContainsItem( _outerKey ), "The item that depends on the key of the replaced item was not removed." );
        Assert.False( backend.ContainsItem( _outermostKey ), "The item that depends on the replaced item through another item was not removed." );
        Assert.False( backend.ContainsDependency( _innerKey ), "An item remained registered under the key of the replaced item." );
        Assert.False( backend.ContainsDependency( _outerKey ), "An item remained registered under the key of a removed item." );

        Assert.Equal(
            new[] { _outerKey, _outermostKey },
            removedItems.Select( e => e.Key ).OrderBy( k => k, StringComparer.Ordinal ).ToArray() );

        Assert.All( removedItems, e => Assert.Equal( CacheItemRemovedReason.Invalidated, e.RemovedReason ) );
    }

    /// <summary>
    /// Verifies that the first value stored for a key keeps the items that depend on the key. This test is a control for
    /// <see cref="SetItem_OverExistingValue_RemovesDependentItems"/>.
    /// </summary>
    /// <remarks>
    /// The dependent items were not computed from a value of the key, because the key had no value, so they stay valid.
    /// </remarks>
    [Fact]
    public void SetItem_FirstValue_KeepsDependentItems()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_innerKey] ) );
        backend.SetItem( _innerKey, new CacheItem( "inner-value" ) );

        Assert.True( backend.ContainsItem( _outerKey ), "The first value stored for a key removed the item that depends on the key." );
    }

    /// <summary>
    /// Verifies that an invalidation of a dependency removes an outer item that depends on the dependency through the
    /// key of an intermediate item, after the intermediate item has been removed and a new value, which does not have
    /// the dependency, has been stored for its key.
    /// </summary>
    /// <remarks>
    /// The new value replaces nothing, so the outer item stays in the cache. It was computed from the removed value,
    /// which depended on the dependency, so it must stay reachable from the dependency. The new value itself does not
    /// depend on the dependency, so the invalidation keeps it. The Redis backend has the same behavior.
    /// </remarks>
    [Fact]
    public void InvalidateDependency_AfterRemovedIntermediateItemIsStoredAgainWithoutDependency_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        StoreChainAndStoreIntermediateItemAgain( backend );

        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item after a new value had been stored for the key of the removed intermediate item." );

        Assert.True(
            backend.GetItem( _innerKey ) is { Value: "new-inner-value" },
            "The invalidation of the dependency removed the new value of the intermediate item, which does not have the dependency." );

        Assert.False( backend.ContainsDependency( _dependency ), "A key remained registered under the invalidated dependency." );
    }

    /// <summary>
    /// Verifies that the registrations kept for the previous value of a key are released when the last item that
    /// depends on the key is removed.
    /// </summary>
    /// <remarks>
    /// The registrations exist only for the items that depend on the key. Without the release, the backward dependency
    /// index would grow with every dependency that a previous value of a key has had.
    /// </remarks>
    [Fact]
    public void SetItem_AfterRemovalOfPreviousValue_ReleasesItsRegistrationsWithTheLastDependentItem()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        StoreChainAndStoreIntermediateItemAgain( backend );

        Assert.True(
            backend.ContainsDependency( _dependency ),
            "Precondition: the key of the intermediate item is no longer registered under the dependency of its previous value." );

        backend.RemoveItem( _outerKey );

        Assert.False(
            backend.ContainsDependency( _dependency ),
            "The key of the intermediate item stayed registered under the dependency of its previous value after the last item that depends on the key had been removed." );

        Assert.True( backend.GetItem( _innerKey ) is { Value: "new-inner-value" }, "The new value of the intermediate item was removed." );
    }

    /// <summary>
    /// Verifies that the replacement of the value of an item that depends on its own key keeps the new value.
    /// </summary>
    [Fact]
    public void SetItem_OverExistingValueOfItemThatDependsOnItsOwnKey_KeepsNewValue()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _innerKey, new CacheItem( "inner-value", [_innerKey] ) );
        backend.SetItem( _innerKey, new CacheItem( "new-inner-value", [_innerKey] ) );

        Assert.Equal( "new-inner-value", backend.GetItem( _innerKey )?.Value );
        Assert.True( backend.ContainsDependency( _innerKey ), "The new value is no longer registered under its own key." );
    }

    /// <summary>
    /// Verifies that the replacement of the value of an item of a dependency cycle keeps the new value and removes the
    /// other item of the cycle.
    /// </summary>
    [Fact]
    public void SetItem_OverExistingValueOfItemOfDependencyCycle_KeepsNewValue()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _innerKey, new CacheItem( "inner-value", [_outerKey] ) );
        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_innerKey] ) );

        backend.SetItem( _innerKey, new CacheItem( "new-inner-value", [_outerKey] ) );

        Assert.Equal( "new-inner-value", backend.GetItem( _innerKey )?.Value );
        Assert.False( backend.ContainsItem( _outerKey ), "The other item of the cycle, which depends on the replaced item, was not removed." );
    }

    /// <summary>
    /// Stores an intermediate item that depends on <see cref="_dependency"/> and an outer item that depends on the key
    /// of the intermediate item, removes the intermediate item, and stores a new value without dependencies for its key.
    /// </summary>
    /// <remarks>
    /// The removal keeps the outer item, as an eviction or an expiration of the intermediate item does. The new value
    /// therefore replaces nothing, and the outer item stays in the cache.
    /// </remarks>
    /// <param name="backend">The backend.</param>
    private static void StoreChainAndStoreIntermediateItemAgain( CachingBackend backend )
    {
        backend.SetItem( _innerKey, new CacheItem( "inner-value", [_dependency] ) );
        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_innerKey] ) );

        backend.RemoveItem( _innerKey );
        backend.SetItem( _innerKey, new CacheItem( "new-inner-value" ) );

        Assert.True(
            backend.ContainsItem( _outerKey ),
            "Precondition: the outer item was removed by the removal of the intermediate item or by the value stored for its key." );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> on a given <see cref="IMemoryCache"/>, with the
    /// services of <paramref name="fakes"/>.
    /// </summary>
    /// <param name="memoryCache">The memory cache of the backend. The backend does not own it.</param>
    /// <param name="fakes">The services of the backend.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache memoryCache, FakeCachingServices fakes )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( memoryCache ),
            fakes.ServiceProvider );

        backend.Initialize();

        return backend;
    }
}
