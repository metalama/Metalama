// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests when <see cref="MemoryCachingBackend"/> releases the registrations that it keeps for a removed item.
/// </summary>
/// <remarks>
/// <para>
/// When an item is removed while other items depend on its key, the backend keeps the key registered under the
/// dependencies of the removed item, so that an invalidation of one of these dependencies still reaches the dependent
/// items. The backend must release these registrations when no item that is still in the cache depends on the key,
/// directly or through the keys of other removed items. Otherwise, the index grows without bound.
/// </para>
/// <para>
/// The tests observe the index through <see cref="CachingBackend.ContainsDependency"/>, which returns
/// <see langword="true"/> while at least one key is registered under a dependency.
/// </para>
/// </remarks>
public sealed class MemoryCachingBackendRetentionTests
{
    /// <summary>
    /// The key of the first item of a dependency cycle.
    /// </summary>
    private const string _firstKey = "first";

    /// <summary>
    /// The key of the second item of a dependency cycle.
    /// </summary>
    private const string _secondKey = "second";

    /// <summary>
    /// The key of an item that depends on an item of the cycle and is not part of the cycle.
    /// </summary>
    private const string _dependentKey = "dependent";

    /// <summary>
    /// A dependency that is the key of no item.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The key of the intermediate item of a chain, which depends on <see cref="_dependency"/>.
    /// </summary>
    private const string _innerKey = "inner";

    /// <summary>
    /// The key of the outer item of a chain, which depends on the key of the intermediate item.
    /// </summary>
    private const string _outerKey = "outer";

    /// <summary>
    /// The maximum time to wait for the pending work items. It only detects a failure of the test.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The instant at which the clock of the expiration test starts.
    /// </summary>
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    /// <summary>
    /// Verifies that the registrations of two items that depend on each other are released when both items have been
    /// removed.
    /// </summary>
    /// <remarks>
    /// The removal of the first item keeps its key registered, because the second item depends on it. The removal of the
    /// second item must not keep its key registered only because the key of the first item, whose item is no longer in
    /// the cache, is registered under it.
    /// </remarks>
    [Fact]
    public void RemoveItem_OfBothItemsOfDependencyCycle_ReleasesRegistrations()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _firstKey, new CacheItem( "first-value", [_secondKey] ) );
        backend.SetItem( _secondKey, new CacheItem( "second-value", [_firstKey] ) );

        backend.RemoveItem( _firstKey );

        Assert.True(
            backend.ContainsDependency( _secondKey ),
            "Precondition: the key of the first item is not registered under the key of the second item, although the second item is still in the cache and depends on the first item." );

        backend.RemoveItem( _secondKey );

        AssertNoRegistration( backend, _firstKey, _secondKey );
    }

    /// <summary>
    /// Verifies that the registration of an item that depends on its own key is released when the item has been removed.
    /// </summary>
    [Fact]
    public void RemoveItem_OfItemThatDependsOnItsOwnKey_ReleasesRegistration()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _firstKey, new CacheItem( "first-value", [_firstKey] ) );
        backend.RemoveItem( _firstKey );

        AssertNoRegistration( backend, _firstKey );
    }

    /// <summary>
    /// Verifies that the registrations of two removed items that depend on each other are kept while an item that is
    /// still in the cache depends on one of them, and are released when that item is removed.
    /// </summary>
    /// <remarks>
    /// The removal of the dependent item removes its key from a backward dependency set that does not become empty,
    /// because the set still lists the key of a removed item of the cycle. The backend must nevertheless release the
    /// registrations of the cycle.
    /// </remarks>
    [Fact]
    public void RemoveItem_OfLastDependentOfRemovedDependencyCycle_ReleasesRegistrations()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _firstKey, new CacheItem( "first-value", [_secondKey] ) );
        backend.SetItem( _secondKey, new CacheItem( "second-value", [_firstKey] ) );
        backend.SetItem( _dependentKey, new CacheItem( "dependent-value", [_firstKey] ) );

        backend.RemoveItem( _firstKey );
        backend.RemoveItem( _secondKey );

        Assert.True(
            backend.ContainsDependency( _firstKey ) && backend.ContainsDependency( _secondKey ),
            "The registrations of the removed items were released, although an item that is still in the cache depends on the first item, which depends on the second item." );

        backend.RemoveItem( _dependentKey );

        AssertNoRegistration( backend, _firstKey, _secondKey );
    }

    /// <summary>
    /// Verifies that an invalidation of a dependency of a removed item of a dependency cycle still removes an item that
    /// depends on the cycle. This test is a control for
    /// <see cref="RemoveItem_OfLastDependentOfRemovedDependencyCycle_ReleasesRegistrations"/>.
    /// </summary>
    [Fact]
    public void InvalidateDependency_OfDependencyOfRemovedDependencyCycle_RemovesDependentItem()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _firstKey, new CacheItem( "first-value", [_secondKey, _dependency] ) );
        backend.SetItem( _secondKey, new CacheItem( "second-value", [_firstKey] ) );
        backend.SetItem( _dependentKey, new CacheItem( "dependent-value", [_secondKey] ) );

        backend.RemoveItem( _firstKey );
        backend.RemoveItem( _secondKey );

        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _dependentKey ),
            "The invalidation did not remove the dependent item, which depends on the invalidated dependency through the keys of the two removed items." );

        AssertNoRegistration( backend, _firstKey, _secondKey, _dependency );
    }

    /// <summary>
    /// Verifies that an invalidation of a dependency removes an outer item that was stored after the intermediate item,
    /// through whose key the outer item depends on the dependency, had been evicted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a cached method gets a cache hit on another cached method, the caching frontend adds only the key of the
    /// called method to the dependencies of the calling method. When the item of the called method is evicted before the
    /// item of the calling method is stored, no item depends on the evicted item at the time of the eviction. The backend
    /// therefore keeps no registration for the evicted item, and the outer item is not reachable from the dependency.
    /// </para>
    /// <para>
    /// The backend cannot keep the dependencies of every evicted item, because the index would then grow with the number
    /// of evicted items. A fix needs a design decision, such as a frontend that also adds the dependencies of the called
    /// method to the calling method when it gets a cache hit.
    /// </para>
    /// </remarks>
    [Fact(
        Skip =
            "Not fixed: the backend keeps the dependencies of a removed item only when another item depends on its key at the time of the removal. A fix needs a design decision, such as a frontend that propagates the dependencies of a called method on a cache hit." )]
    public async Task InvalidateDependency_OuterItemStoredAfterEvictionOfIntermediateItem_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellation = new CancellationTokenSource( _timeout );
        using var backend = CreateBackend( fakes.MemoryCache, fakes );

        backend.SetItem(
            _innerKey,
            new CacheItem( "inner-value", [_dependency], new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 1 ) } ) );

        await fakes.AdvanceAsync( TimeSpan.FromMinutes( 2 ), cancellation.Token );

        Assert.False( backend.ContainsItem( _innerKey ), "Precondition: the intermediate item did not expire." );

        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_innerKey] ) );
        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item, which depends on the dependency through the key of the evicted intermediate item." );
    }

    /// <summary>
    /// Asserts that no key is registered under any of the given dependencies.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="dependencies">The dependencies.</param>
    private static void AssertNoRegistration( CachingBackend backend, params string[] dependencies )
    {
        var registeredDependencies = dependencies.Where( backend.ContainsDependency ).ToList();

        Assert.True(
            registeredDependencies.Count == 0,
            $"The backend still holds registrations under [{string.Join( ", ", registeredDependencies )}], although no item that depends on them is in the cache." );
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
