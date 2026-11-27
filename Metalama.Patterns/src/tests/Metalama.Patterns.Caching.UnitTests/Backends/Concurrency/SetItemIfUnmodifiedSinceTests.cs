// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the <c>ifUnmodifiedSince</c> precondition of <see cref="CachingBackend.SetItem"/> with <see cref="MemoryCachingBackend"/> and with
/// <see cref="LayeredCachingBackendEnhancer"/>.
/// </summary>
/// <remarks>
/// The clock of the services does not advance unless a test advances it, so that an invalidation that runs after the
/// computation start time has been read is timed at the same instant as the start, and counts as an invalidation during
/// the computation.
/// </remarks>
public sealed class SetItemIfUnmodifiedSinceTests
{
    private const string _key = "item";
    private const string _dependency = "dependency";
    private const string _otherDependency = "other-dependency";
    private const string _innerKey = "inner";

    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    [Fact]
    public void MemoryBackend_SupportsFeature()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        Assert.True( backend.SupportedFeatures.SetItemIfUnmodifiedSince );
    }

    [Fact]
    public void DependencyInvalidatedDuringComputation_ItemIsNotStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();
        backend.InvalidateDependency( _dependency );

        Assert.False( backend.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.False( backend.ContainsItem( _key ), "The item was stored although its dependency was invalidated during its computation." );
    }

    [Fact]
    public async Task DependencyInvalidatedDuringComputation_ItemIsNotStored_Async()
    {
        using var fakes = new FakeCachingServices( _origin );
        await using var backend = CreateMemoryBackend( fakes );

        var start = await backend.Clock.GetCurrentTimeAsync( TestContext.Current.CancellationToken );
        await backend.InvalidateDependencyAsync( _dependency, TestContext.Current.CancellationToken );

        Assert.False( await backend.SetItemAsync( _key, new CacheItem( "value", [_dependency] ), start, TestContext.Current.CancellationToken ) );
        Assert.False( await backend.ContainsItemAsync( _key, TestContext.Current.CancellationToken ), "The item was stored although its dependency was invalidated during its computation." );
    }

    [Fact]
    public void KeyRemovedDuringComputation_ItemIsNotStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();
        backend.RemoveItem( _key );

        Assert.False( backend.SetItem( _key, new CacheItem( "value" ), start ) );
        Assert.False( backend.ContainsItem( _key ), "The item was stored although its key was removed during its computation." );
    }

    [Fact]
    public void IntermediateItemInvalidatedDuringComputation_OuterItemIsNotStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        backend.SetItem( _innerKey, new CacheItem( "inner-value", [_dependency] ) );
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );

        // The outer item depends on the key of the inner item only. The invalidation of the dependency removes the inner
        // item, which invalidates its key.
        var start = backend.Clock.GetCurrentTime();
        backend.InvalidateDependency( _dependency );

        Assert.False( backend.SetItem( _key, new CacheItem( "outer-value", [_innerKey] ), start ) );
        Assert.False( backend.ContainsItem( _key ), "The outer item was stored although the inner item was invalidated during its computation." );
    }

    [Fact]
    public void InvalidatedBeforeComputation_ItemIsStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        backend.InvalidateDependency( _dependency );
        backend.RemoveItem( _key );
        fakes.TimeProvider.Advance( TimeSpan.FromMilliseconds( 1 ) );

        var start = backend.Clock.GetCurrentTime();

        Assert.True( backend.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.True( backend.ContainsItem( _key ) );
    }

    [Fact]
    public void UnrelatedDependencyInvalidatedDuringComputation_ItemIsStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();
        backend.InvalidateDependency( _otherDependency );

        Assert.True( backend.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.True( backend.ContainsItem( _key ) );
    }

    [Fact]
    public void ComputationLongerThanTombstoneLifetime_ItemIsStoredWithoutCheck()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes, TimeSpan.FromMinutes( 1 ) );

        var start = backend.Clock.GetCurrentTime();
        backend.InvalidateDependency( _dependency );
        fakes.TimeProvider.Advance( TimeSpan.FromMinutes( 2 ) );

        Assert.True( backend.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.True( backend.ContainsItem( _key ) );
    }

    [Fact]
    public void RejectedItem_DoesNotRemoveNewerItem()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();
        backend.InvalidateDependency( _dependency );
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );

        // A computation that started after the invalidation stores its value.
        Assert.True( backend.SetItem( _key, new CacheItem( "newer-value", [_dependency] ), backend.Clock.GetCurrentTime() ) );

        // The computation that started before the invalidation is rejected and does not remove the newer value.
        Assert.False( backend.SetItem( _key, new CacheItem( "older-value", [_dependency] ), start ) );
        Assert.Equal( "newer-value", backend.GetItem( _key )?.Value );
    }

    [Fact]
    public void NewerValueStoredDuringComputation_OlderValueIsNotStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var olderStart = backend.Clock.GetCurrentTime();
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );
        var newerStart = backend.Clock.GetCurrentTime();

        // A computation that started later, for example a refresh after a change of the data, stores its value first.
        Assert.True( backend.SetItem( _key, new CacheItem( "newer-value" ), newerStart ) );

        Assert.False( backend.SetItem( _key, new CacheItem( "older-value" ), olderStart ) );
        Assert.Equal( "newer-value", backend.GetItem( _key )?.Value );
    }

    [Fact]
    public void OlderValueStored_NewerValueIsStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        Assert.True( backend.SetItem( _key, new CacheItem( "older-value" ), backend.Clock.GetCurrentTime() ) );
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );

        Assert.True( backend.SetItem( _key, new CacheItem( "newer-value" ), backend.Clock.GetCurrentTime() ) );
        Assert.Equal( "newer-value", backend.GetItem( _key )?.Value );
    }

    [Fact]
    public void ValueStoredWithSameStart_IsReplaced()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();

        Assert.True( backend.SetItem( _key, new CacheItem( "first-value" ), start ) );
        Assert.True( backend.SetItem( _key, new CacheItem( "second-value" ), start ) );
        Assert.Equal( "second-value", backend.GetItem( _key )?.Value );
    }

    [Fact]
    public void ValueStoredUnconditionally_IsReplacedByConditionalStore()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        var start = backend.Clock.GetCurrentTime();
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );

        // An unconditional store has no computation start time, so it does not supersede a conditional one.
        Assert.True( backend.SetItem( _key, new CacheItem( "unconditional-value" ) ) );
        Assert.True( backend.SetItem( _key, new CacheItem( "conditional-value" ), start ) );
        Assert.Equal( "conditional-value", backend.GetItem( _key )?.Value );
    }

    [Fact]
    public void UnconditionalStore_ReturnsTrue()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        backend.InvalidateDependency( _dependency );

        Assert.True( backend.SetItem( _key, new CacheItem( "value", [_dependency] ) ) );
        Assert.True( backend.ContainsItem( _key ) );
    }

    [Fact]
    public void Layered_NewerValueStoredDuringComputation_OlderValueIsStoredInNoLayer()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var layered = CreateLayeredBackend( fakes, out var remote );

        var olderStart = layered.Clock.GetCurrentTime();
        fakes.TimeProvider.Advance( TimeSpan.FromSeconds( 1 ) );

        Assert.True( layered.SetItem( _key, new CacheItem( "newer-value" ), layered.Clock.GetCurrentTime() ) );

        Assert.False( layered.SetItem( _key, new CacheItem( "older-value" ), olderStart ) );
        Assert.Equal( "newer-value", remote.GetItem( _key )?.Value );
        Assert.Equal( "newer-value", layered.LocalCache.GetItem( _key )?.Value );
    }

    [Fact]
    public void Layered_UsesClockOfRemoteBackend()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var layered = CreateLayeredBackend( fakes, out var remote );

        Assert.Same( remote.Clock, layered.Clock );
        Assert.True( layered.SupportedFeatures.SetItemIfUnmodifiedSince );
    }

    [Fact]
    public void Layered_DependencyInvalidatedInRemoteDuringComputation_ItemIsStoredInNoLayer()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var layered = CreateLayeredBackend( fakes, out var remote );

        var start = layered.Clock.GetCurrentTime();

        // Another node invalidates the dependency in the remote backend.
        remote.InvalidateDependency( _dependency );

        Assert.False( layered.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.False( remote.ContainsItem( _key ), "The remote backend stored the item." );
        Assert.False( layered.LocalCache.ContainsItem( _key ), "The local cache kept the item that the remote backend rejected." );
    }

    [Fact]
    public async Task Layered_DependencyInvalidatedInRemoteDuringComputation_ItemIsStoredInNoLayer_Async()
    {
        using var fakes = new FakeCachingServices( _origin );
        await using var layered = CreateLayeredBackend( fakes, out var remote );

        var start = await layered.Clock.GetCurrentTimeAsync( TestContext.Current.CancellationToken );

        await remote.InvalidateDependencyAsync( _dependency, TestContext.Current.CancellationToken );

        Assert.False( await layered.SetItemAsync( _key, new CacheItem( "value", [_dependency] ), start, TestContext.Current.CancellationToken ) );
        Assert.False( remote.ContainsItem( _key ), "The remote backend stored the item." );
        Assert.False( layered.LocalCache.ContainsItem( _key ), "The local cache kept the item that the remote backend rejected." );
    }

    [Fact]
    public void Layered_NotInvalidated_ItemIsStoredInBothLayers()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var layered = CreateLayeredBackend( fakes, out var remote );

        var start = layered.Clock.GetCurrentTime();

        Assert.True( layered.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
        Assert.True( remote.ContainsItem( _key ) );
        Assert.True( layered.LocalCache.ContainsItem( _key ) );
    }

    [Fact]
    public void NonBlockingEnhancer_DoesNotSupportFeature()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var memory = CreateMemoryBackend( fakes );
        using var nonBlocking = new NonBlockingCachingBackendEnhancer( memory );

        Assert.False( nonBlocking.SupportedFeatures.SetItemIfUnmodifiedSince );
    }

    [Fact]
    public void Invalidations_RecordKeys_AndRecordsArePurgedAfterLifetime()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes, TimeSpan.FromMinutes( 1 ) );

        backend.SetItem( _innerKey, new CacheItem( "inner-value", [_dependency] ) );

        // The invalidation records the dependency and the key of the inner item, which it removes. The removal records the
        // removed key.
        backend.InvalidateDependency( _dependency );
        backend.RemoveItem( _key );

        Assert.Equal( 3, backend.InvalidationTombstoneCount );

        // The next invalidation after the lifetime purges the expired records and records its own key.
        fakes.TimeProvider.Advance( TimeSpan.FromMinutes( 2 ) );
        backend.InvalidateDependency( _otherDependency );

        Assert.Equal( 1, backend.InvalidationTombstoneCount );
    }

    [Fact]
    public void StoresWithoutInvalidations_RecordNothing()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = CreateMemoryBackend( fakes );

        for ( var i = 0; i < 100; i++ )
        {
            Assert.True( backend.SetItem( _key + i, new CacheItem( "value", [_dependency] ), backend.Clock.GetCurrentTime() ) );
        }

        Assert.Equal( 0, backend.InvalidationTombstoneCount );
    }

    [Fact]
    public void ClockFromTimeProvider_ReturnsTimeAndTolerance()
    {
        using var fakes = new FakeCachingServices( _origin );
        var clock = CachingBackendClock.FromTimeProvider( fakes.TimeProvider, TimeSpan.FromSeconds( 2 ) );

        Assert.Equal( _origin, clock.GetCurrentTime() );
        Assert.Equal( TimeSpan.FromSeconds( 2 ), clock.Tolerance );
        Assert.Throws<ArgumentOutOfRangeException>( () => CachingBackendClock.FromTimeProvider( fakes.TimeProvider, TimeSpan.FromSeconds( -1 ) ) );
    }

    [Fact]
    public void ClockTolerance_InvalidationJustBeforeStart_ItemIsNotStored()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var backend = new ToleranceMemoryBackend( fakes, TimeSpan.FromSeconds( 1 ) );
        backend.Initialize();

        // The invalidation is timed half a second before the start, which is within the tolerance of the clock.
        backend.InvalidateDependency( _dependency );
        fakes.TimeProvider.Advance( TimeSpan.FromMilliseconds( 500 ) );
        var start = backend.Clock.GetCurrentTime();

        Assert.False( backend.SetItem( _key, new CacheItem( "value", [_dependency] ), start ) );
    }

    private static MemoryCachingBackend CreateMemoryBackend( FakeCachingServices fakes, TimeSpan? tombstoneLifetime = null )
    {
        var configuration = new MemoryCachingBackendConfiguration { DebugName = "test" };

        if ( tombstoneLifetime != null )
        {
            configuration = configuration with { InvalidationTombstoneLifetime = tombstoneLifetime.Value };
        }

        var backend = new MemoryCachingBackend( null, configuration, fakes.ServiceProvider );
        backend.Initialize();

        return backend;
    }

    private static LayeredCachingBackendEnhancer CreateLayeredBackend( FakeCachingServices fakes, out MemoryCachingBackend remote )
    {
        remote = new MemoryCachingBackend( null, new MemoryCachingBackendConfiguration { DebugName = "remote" }, fakes.ServiceProvider );
        var local = new MemoryCachingBackend( null, new MemoryCachingBackendConfiguration { DebugName = "local" }, fakes.ServiceProvider );
        var layered = new LayeredCachingBackendEnhancer( remote, local, null );
        layered.Initialize();

        return layered;
    }

    /// <summary>
    /// A <see cref="MemoryCachingBackend"/> whose clock has a tolerance.
    /// </summary>
    private sealed class ToleranceMemoryBackend : MemoryCachingBackend
    {
        private readonly TimeSpan _tolerance;

        public ToleranceMemoryBackend( FakeCachingServices fakes, TimeSpan tolerance ) : base(
            null,
            new MemoryCachingBackendConfiguration { DebugName = "tolerance" },
            fakes.ServiceProvider )
        {
            this._tolerance = tolerance;
        }

        protected override CachingBackendClock CreateClock() => CachingBackendClock.FromTimeProvider( this.TimeProvider, this._tolerance );
    }
}
