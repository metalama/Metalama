// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests of <see cref="LayeredCachingBackendEnhancer"/> whose first layer is a <see cref="MemoryCachingBackend"/>. The
/// tests check that the first layer does not serve a value that has been removed or invalidated, that a tombstone does
/// not hide a newer value of the second layer, and that the removal paths do not throw.
/// </summary>
/// <remarks>
/// <para>
/// The second layer of every test is a <see cref="RemoteBackendDouble"/> that is passed directly to the enhancer. The
/// double lets a test defer a removal as a backend that is not blocking does, and choose the source identifier of the
/// events.
/// </para>
/// <para>
/// When the second layer is not blocking, a removal replaces the item of the first layer with a tombstone that expires
/// after a transition period of one minute. While the tombstone is present, the enhancer ignores a value of the second
/// layer that is not newer than the removal, so the readers of the same node do not receive a value whose removal is
/// still pending in the second layer. No backend that ships with Metalama reports that it is not blocking, so the tests
/// of the tombstone describe the behavior with a custom second layer.
/// </para>
/// <para>
/// Every test asserts the correct behavior, so a test that fails reports a defect. The assertions that precede the
/// decisive assertion check the preconditions of the scenario, so that a change of the order in which the product calls
/// the layers is reported as a failed precondition and not as the defect.
/// </para>
/// </remarks>
public sealed partial class LayeredCachingBackendEnhancerConcurrencyTests
{
    /// <summary>
    /// The key of the item of every test.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The dependency of the item, in the tests that invalidate a dependency.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The maximum time to wait for an asynchronous operation to complete. It only detects a failure of the test.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The start time of the fake clock of the tests that substitute the clock. It is behind the system clock by several
    /// years.
    /// </summary>
    private static readonly DateTimeOffset _pastClockOrigin = new( 2020, 1, 1, 0, 0, 0, TimeSpan.Zero );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayeredCachingBackendEnhancerConcurrencyTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public LayeredCachingBackendEnhancerConcurrencyTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Verifies that the invalidation of a dependency removes, from the first layer, an item that a synchronous read has
    /// copied from the second layer.
    /// </summary>
    /// <remarks>
    /// The synchronous read requests the item from the second layer without its dependencies, and it stores the item in
    /// the first layer without registering the dependency there. The local invalidation therefore does not find the item
    /// in the first layer. The second layer removes the item and raises its events with its own identifier. The enhancer
    /// ignores these events, because it treats them as the events of an operation of its own node.
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_AfterSynchronousReadThrough_RemovesItemFromL1()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        var finalItem = await this.RunReadThroughThenInvalidateDependencyAsync(
            readAsynchronously: false,
            useForeignEventSource: false,
            cancellation.Token );

        Assert.True(
            finalItem == null,
            $"GetItem returned {DescribeItem( finalItem )} after the invalidation of the dependency. The first layer serves the invalidated value." );
    }

    /// <summary>
    /// Verifies that the invalidation of a dependency removes, from the first layer, an item that an asynchronous read
    /// has copied from the second layer. This test is a control for
    /// <see cref="InvalidateDependency_AfterSynchronousReadThrough_RemovesItemFromL1"/>.
    /// </summary>
    /// <remarks>
    /// The asynchronous read always requests the dependencies from the second layer, so the first layer registers the
    /// dependency and the local invalidation removes the item.
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_AfterAsynchronousReadThrough_RemovesItemFromL1()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        var finalItem = await this.RunReadThroughThenInvalidateDependencyAsync(
            readAsynchronously: true,
            useForeignEventSource: false,
            cancellation.Token );

        Assert.True(
            finalItem == null,
            $"GetItem returned {DescribeItem( finalItem )} after the invalidation of the dependency, although the asynchronous read requested the dependencies." );
    }

    /// <summary>
    /// Verifies that the event of the second layer removes, from the first layer, an item that a synchronous read has
    /// copied from the second layer, when the event carries the identifier of another node. This test is a control for
    /// <see cref="InvalidateDependency_AfterSynchronousReadThrough_RemovesItemFromL1"/>.
    /// </summary>
    /// <remarks>
    /// The schedule is the same as in the synchronous test, but the second layer raises its events with a foreign source
    /// identifier. The enhancer then removes the item from the first layer when it receives the item-removed event. This
    /// shows that the synchronous test fails because the item is not registered in the first layer and the event of the
    /// own node is ignored, and not because the events are not delivered.
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_AfterSynchronousReadThroughWithForeignSourceEvents_RemovesItemFromL1()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        var finalItem = await this.RunReadThroughThenInvalidateDependencyAsync(
            readAsynchronously: false,
            useForeignEventSource: true,
            cancellation.Token );

        Assert.True(
            finalItem == null,
            $"GetItem returned {DescribeItem( finalItem )} although the second layer raised the item-removed event with a foreign source identifier." );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking masks the value of the
    /// second layer while the removal is pending, when the key is absent from the first layer.
    /// </summary>
    /// <remarks>
    /// The value is stored in the second layer only, as another node would store it. The removal writes a tombstone only
    /// when the first layer holds the key, so no tombstone is written. The read that follows the removal then copies the
    /// value of the second layer into the first layer, which keeps serving it after the removal has completed.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_OfKeyAbsentFromL1_DoesNotServeRemovedValue()
    {
        // The second layer and the first layer both read the system clock.
        var remote = new RemoteBackendDouble( null, blocking: false, defersRemovals: true );
        using var layered = CreateLayeredBackend( remote, new MemoryCache( new MemoryCacheOptions() ) );

        remote.SetItem( _key, new MaterializedCacheItem( new CacheItem( "remote-value" ), TimeProvider.System ) );

        Assert.False( layered.LocalCache.ContainsItem( _key ), "Precondition: the first layer already holds the key." );

        layered.RemoveItem( _key );

        this.AssertPendingRemovalIsMasked( layered, remote );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking masks the value of the
    /// second layer while the removal is pending, when the memory cache of the first layer is compacted.
    /// </summary>
    /// <remarks>
    /// An application that shares its memory cache with the first layer can compact it, for example under memory
    /// pressure. The tombstone has the normal priority, so the compaction evicts it. The read that follows the compaction
    /// then copies the value of the second layer into the first layer, which keeps serving it after the removal has
    /// completed.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_FollowedByCompactionOfL1_DoesNotServeRemovedValue()
    {
        // The second layer and the first layer both read the system clock.
        var l1Cache = new MemoryCache( new MemoryCacheOptions() );
        var remote = new RemoteBackendDouble( null, blocking: false, defersRemovals: true );
        using var layered = CreateLayeredBackend( remote, l1Cache );

        layered.SetItem( _key, new CacheItem( "value" ) );
        layered.RemoveItem( _key );

        Assert.True( layered.LocalCache.GetItem( _key ) is { Value: null }, "Precondition: the removal did not write a tombstone into the first layer." );

        l1Cache.Compact( 1 );

        this.AssertPendingRemovalIsMasked( layered, remote );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking masks the value of the
    /// second layer while the removal is pending, when the key is present in the first layer. This test is a control for
    /// <see cref="NonBlockingRemoveItem_OfKeyAbsentFromL1_DoesNotServeRemovedValue"/> and
    /// <see cref="NonBlockingRemoveItem_FollowedByCompactionOfL1_DoesNotServeRemovedValue"/>.
    /// </summary>
    /// <remarks>
    /// The removal writes a tombstone into the first layer. The value of the second layer is older than the tombstone, so
    /// the enhancer ignores it.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_OfKeyPresentInL1_DoesNotServeRemovedValue()
    {
        // The second layer and the first layer both read the system clock.
        var remote = new RemoteBackendDouble( null, blocking: false, defersRemovals: true );
        using var layered = CreateLayeredBackend( remote, new MemoryCache( new MemoryCacheOptions() ) );

        layered.SetItem( _key, new CacheItem( "value" ) );
        layered.RemoveItem( _key );

        Assert.True( layered.LocalCache.GetItem( _key ) is { Value: null }, "Precondition: the removal did not write a tombstone into the first layer." );

        this.AssertPendingRemovalIsMasked( layered, remote );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking masks the value of the
    /// second layer while the removal is pending, when the clock of the backend is behind the clock of the memory cache
    /// of the first layer.
    /// </summary>
    /// <remarks>
    /// The enhancer computes the expiration of the tombstone as an instant, with the clock of the backend. The memory
    /// cache evaluates that instant with its own clock, which is the system clock. The instant is already past for the
    /// memory cache, so the memory cache stores no tombstone, and it removes the item that the tombstone replaces. The
    /// read that follows the removal then copies the value of the second layer into the first layer.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_WithBackendClockBehindL1Clock_DoesNotServeRemovedValue()
    {
        using var fakes = new FakeCachingServices( _pastClockOrigin );

        var lag = DateTimeOffset.UtcNow - fakes.TimeProvider.GetUtcNow();

        Assert.True(
            lag > TimeSpan.FromMinutes( 2 ),
            "Precondition: the clock of the backend is not behind the system clock by more than the transition period of the tombstone." );

        // The first layer uses a MemoryCache, which reads the system clock, instead of the FakeMemoryCache of the services.
        var remote = new RemoteBackendDouble( fakes.ServiceProvider, blocking: false, defersRemovals: true );
        using var layered = CreateLayeredBackend( remote, new MemoryCache( new MemoryCacheOptions() ) );

        layered.SetItem( _key, new CacheItem( "value" ) );

        Assert.True(
            layered.LocalCache.GetItem( _key ) is { Value: "value" },
            "Precondition: the first layer does not hold the item before the removal, so the removal would not write a tombstone." );

        layered.RemoveItem( _key );

        this.AssertPendingRemovalIsMasked( layered, remote );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking masks the value of the
    /// second layer while the removal is pending, when the memory cache of the first layer reads the clock of the
    /// backend. This test is a control for <see cref="NonBlockingRemoveItem_WithBackendClockBehindL1Clock_DoesNotServeRemovedValue"/>.
    /// </summary>
    /// <remarks>
    /// The clock of the backend has the same start time as in the tested scenario, but the first layer uses the
    /// <see cref="FakeMemoryCache"/> of the services, which reads the same clock. The memory cache therefore stores the
    /// tombstone.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_WithBackendClockSharedByL1_DoesNotServeRemovedValue()
    {
        using var fakes = new FakeCachingServices( _pastClockOrigin );

        var remote = new RemoteBackendDouble( fakes.ServiceProvider, blocking: false, defersRemovals: true );
        using var layered = CreateLayeredBackend( remote, null );

        layered.SetItem( _key, new CacheItem( "value" ) );
        layered.RemoveItem( _key );

        Assert.True( layered.LocalCache.GetItem( _key ) is { Value: null }, "Precondition: the removal did not write a tombstone into the first layer." );

        this.AssertPendingRemovalIsMasked( layered, remote );
    }

    /// <summary>
    /// Verifies that a removal through a layered backend whose second layer is not blocking does not throw when the
    /// memory cache of the first layer has a size limit.
    /// </summary>
    /// <remarks>
    /// A memory cache with a size limit rejects an entry that has no size. Every item and every dependency set of the
    /// first layer has a size, but the tombstone that replaces the item has none.
    /// </remarks>
    [Fact]
    public void NonBlockingRemoveItem_OnSizeLimitedL1_DoesNotThrow()
    {
        var remote = new RemoteBackendDouble( null, blocking: false );
        using var layered = CreateLayeredBackend( remote, new MemoryCache( new MemoryCacheOptions { SizeLimit = 100 } ) );

        layered.SetItem( _key, new CacheItem( "value" ) );

        Assert.True( layered.LocalCache.ContainsItem( _key ), "Precondition: the first layer did not accept the item." );

        var exception = Record.Exception( () => layered.RemoveItem( _key ) );

        Assert.True( exception == null, $"RemoveItem threw an exception: {exception}" );
        Assert.False( remote.ContainsItem( _key ), "The removal did not reach the second layer." );
        Assert.Null( layered.GetItem( _key ) );
    }

    /// <summary>
    /// Verifies that an invalidation through a layered backend whose second layer is not blocking does not throw when
    /// the memory cache of the first layer has a size limit.
    /// </summary>
    /// <remarks>
    /// The invalidation replaces each dependent item of the first layer with a tombstone, which has no size, so the
    /// memory cache rejects it.
    /// </remarks>
    [Fact]
    public void NonBlockingInvalidateDependency_OnSizeLimitedL1_DoesNotThrow()
    {
        var remote = new RemoteBackendDouble( null, blocking: false );
        using var layered = CreateLayeredBackend( remote, new MemoryCache( new MemoryCacheOptions { SizeLimit = 100 } ) );

        layered.SetItem( _key, new CacheItem( "value", [_dependency] ) );

        Assert.True(
            layered.LocalCache.ContainsItem( _key ) && layered.LocalCache.ContainsDependency( _dependency ),
            "Precondition: the first layer did not accept the item or its dependency." );

        var exception = Record.Exception( () => layered.InvalidateDependency( _dependency ) );

        Assert.True( exception == null, $"InvalidateDependency threw an exception: {exception}" );
        Assert.False( remote.ContainsItem( _key ), "The invalidation did not reach the second layer." );
        Assert.Null( layered.GetItem( _key ) );
    }

    /// <summary>
    /// Verifies that an asynchronous read returns the value of the second layer when the first layer holds a tombstone
    /// and the value of the second layer is newer than the tombstone.
    /// </summary>
    /// <remarks>
    /// This test is the asynchronous counterpart of a test of the synchronous read. The asynchronous read casts the value
    /// of the item of the second layer, instead of the item, to <see cref="MaterializedCacheItem"/>. The cast throws, and
    /// the backend handles the exception as an invalid item: it removes the key from both layers and reports a miss.
    /// </remarks>
    [Fact]
    public async Task GetItemAsync_WithMarker_AndL2HasNewerItem_ReturnsL2Item()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices( _pastClockOrigin );

        // The first layer resolves the FakeMemoryCache of the services, which reads the clock of the backend.
        var remote = new RemoteBackendDouble( fakes.ServiceProvider, blocking: false );
        using var layered = CreateLayeredBackend( remote, null );

        await StoreNewerItemBehindTombstoneAsync( fakes, layered, remote, "newer-value", cancellation.Token );

        var retrieved = await layered.GetItemAsync( _key, cancellationToken: cancellation.Token );

        this._output.WriteLine( "Reads of the second layer: " + string.Join( " | ", remote.Reads ) );

        Assert.True(
            retrieved is { Value: "newer-value" },
            $"GetItemAsync returned {DescribeItem( retrieved )} instead of the newer value of the second layer." );

        Assert.True( remote.ContainsItem( _key ), "The read removed the newer value from the second layer." );
    }

    /// <summary>
    /// Verifies that an asynchronous read returns the item of the second layer when the first layer holds a tombstone
    /// and the item of the second layer is newer than the tombstone and has a null value.
    /// </summary>
    /// <remarks>
    /// A cached method can return <see langword="null"/>, so an item can have a null value. Before the asynchronous read
    /// casts the value of the item of the second layer, it throws an exception when that value is null. The backend does
    /// not handle this exception, so it reaches the caller.
    /// </remarks>
    [Fact]
    public async Task GetItemAsync_WithMarker_AndL2HasNewerNullItem_ReturnsL2Item()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices( _pastClockOrigin );

        // The first layer resolves the FakeMemoryCache of the services, which reads the clock of the backend.
        var remote = new RemoteBackendDouble( fakes.ServiceProvider, blocking: false );
        using var layered = CreateLayeredBackend( remote, null );

        await StoreNewerItemBehindTombstoneAsync( fakes, layered, remote, null, cancellation.Token );

        CacheItem? retrieved = null;
        var exception = await Record.ExceptionAsync( async () => retrieved = await layered.GetItemAsync( _key, cancellationToken: cancellation.Token ) );

        Assert.True( exception == null, $"GetItemAsync threw an exception: {exception}" );

        Assert.True(
            retrieved is { Value: null },
            $"GetItemAsync returned {DescribeItem( retrieved )} instead of the newer item of the second layer, whose value is null." );
    }

    /// <summary>
    /// Verifies that an asynchronous check of the presence of an item returns <see langword="true"/> when the first layer
    /// holds a tombstone and the value of the second layer is newer than the tombstone.
    /// </summary>
    /// <remarks>
    /// When the second layer is not blocking, the check performs the asynchronous read of the enhancer. That read casts
    /// the value of the item of the second layer to <see cref="MaterializedCacheItem"/>, and the check does not handle
    /// the exception that the cast throws.
    /// </remarks>
    [Fact]
    public async Task ContainsItemAsync_WithMarker_AndL2HasNewerItem_ReturnsTrue()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices( _pastClockOrigin );

        // The first layer resolves the FakeMemoryCache of the services, which reads the clock of the backend.
        var remote = new RemoteBackendDouble( fakes.ServiceProvider, blocking: false );
        using var layered = CreateLayeredBackend( remote, null );

        await StoreNewerItemBehindTombstoneAsync( fakes, layered, remote, "newer-value", cancellation.Token );

        var contains = false;
        var exception = await Record.ExceptionAsync( async () => contains = await layered.ContainsItemAsync( _key, cancellation.Token ) );

        Assert.True( exception == null, $"ContainsItemAsync threw an exception: {exception}" );
        Assert.True( contains, "ContainsItemAsync returned false although the second layer holds a value that is newer than the tombstone." );
    }

    /// <summary>
    /// Creates and initializes a layered backend whose second layer is <paramref name="remote"/> and whose first layer is
    /// a <see cref="MemoryCachingBackend"/>.
    /// </summary>
    /// <param name="remote">The second layer.</param>
    /// <param name="l1Cache">
    /// The memory cache of the first layer, or <see langword="null"/> to resolve it from the service provider of
    /// <paramref name="remote"/>. The service provider of a <see cref="FakeCachingServices"/> supplies its
    /// <see cref="FakeMemoryCache"/>. When the service provider supplies none, the first layer creates a
    /// <see cref="MemoryCache"/>.
    /// </param>
    /// <returns>The initialized layered backend, which disposes both layers when it is disposed.</returns>
    private static LayeredCachingBackendEnhancer CreateLayeredBackend( RemoteBackendDouble remote, IMemoryCache? l1Cache )
    {
        var l1 = new MemoryCachingBackend( l1Cache, null, remote.ServiceProvider );
        var layered = new LayeredCachingBackendEnhancer( remote, l1, null );
        layered.Initialize();

        return layered;
    }

    /// <summary>
    /// Stores an item in both layers, removes it so that the first layer holds a tombstone, advances the clock, and
    /// stores a newer item directly in the second layer, as another node would.
    /// </summary>
    /// <param name="fakes">The services of the backends, whose clock is advanced.</param>
    /// <param name="layered">The layered backend.</param>
    /// <param name="remote">The second layer, which reports that it is not blocking and does not defer its removals.</param>
    /// <param name="newerValue">The value of the newer item.</param>
    /// <param name="cancellationToken">A token that cancels the wait for the work items queued by the clock.</param>
    private static async Task StoreNewerItemBehindTombstoneAsync(
        FakeCachingServices fakes,
        LayeredCachingBackendEnhancer layered,
        RemoteBackendDouble remote,
        object? newerValue,
        CancellationToken cancellationToken )
    {
        layered.SetItem( _key, new CacheItem( "initial-value" ) );
        layered.RemoveItem( _key );

        Assert.True( layered.LocalCache.GetItem( _key ) is { Value: null }, "Precondition: the removal did not write a tombstone into the first layer." );
        Assert.False( remote.ContainsItem( _key ), "Precondition: the removal did not remove the item from the second layer." );

        // Advance the clock, so that the timestamp of the newer item is strictly greater than the timestamp of the
        // tombstone. The tombstone expires after one minute, so it is still present.
        await fakes.AdvanceAsync( TimeSpan.FromSeconds( 1 ), cancellationToken );

        remote.SetItem( _key, new MaterializedCacheItem( new CacheItem( newerValue ), fakes.TimeProvider ) );
    }

    /// <summary>
    /// Copies an item from the second layer into the first layer with a synchronous or an asynchronous read, invalidates
    /// the dependency of the item, waits for the events, and returns the result of a final synchronous read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The item and its dependency are first stored in both layers. The first layer is then cleared, so that only the
    /// second layer holds them, as after an eviction from the first layer or after a restart of the node.
    /// </para>
    /// <para>
    /// The first layer uses a <see cref="MemoryCache"/> through an <see cref="InterceptingMemoryCache"/>, which captures
    /// the post-eviction callbacks of the items and never runs them. The callbacks that the clearing queues to the thread
    /// pool therefore cannot modify the dependency index of the first layer after the read. These callbacks report the
    /// removal reason that the backend ignores in any case.
    /// </para>
    /// </remarks>
    /// <param name="readAsynchronously">
    /// <see langword="true"/> to copy the item with <see cref="CachingBackend.GetItemAsync"/>, <see langword="false"/> to
    /// copy it with <see cref="CachingBackend.GetItem"/>.
    /// </param>
    /// <param name="useForeignEventSource">
    /// <see langword="true"/> to make the second layer raise its events with the identifier of another node.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the asynchronous operations.</param>
    /// <returns>The result of the final synchronous read.</returns>
    private async Task<CacheItem?> RunReadThroughThenInvalidateDependencyAsync(
        bool readAsynchronously,
        bool useForeignEventSource,
        CancellationToken cancellationToken )
    {
        using var fakes = new FakeCachingServices();

        var l1Cache = new InterceptingMemoryCache();
        l1Cache.DeferEvictionCallbacks( InterceptingMemoryCache.AnyItemKey() );

        var remote = new RemoteBackendDouble(
            fakes.ServiceProvider,
            raisesEvents: true,
            eventSourceId: useForeignEventSource ? Guid.NewGuid() : null );

        using var layered = CreateLayeredBackend( remote, l1Cache );
        var l1 = layered.LocalCache;

        var remoteEvents = new ConcurrentQueue<string>();

        remote.ItemRemoved += ( _, args ) => remoteEvents.Enqueue(
            $"ItemRemoved( {args.Key}, {args.RemovedReason}, raised with the identifier of the second layer: {args.SourceId == remote.Id} )" );

        remote.DependencyInvalidated += ( _, args ) => remoteEvents.Enqueue(
            $"DependencyInvalidated( {args.Key}, raised with the identifier of the second layer: {args.SourceId == remote.Id} )" );

        layered.SetItem( _key, new CacheItem( "value", [_dependency] ) );
        layered.Clear( ClearCacheOptions.Local );

        Assert.True(
            !l1.ContainsItem( _key ) && !l1.ContainsDependency( _dependency ) && remote.ContainsItem( _key ),
            "Precondition: clearing the local layer did not leave the first layer empty and the second layer populated." );

        CacheItem? readItem;

        if ( readAsynchronously )
        {
            readItem = await layered.GetItemAsync( _key, cancellationToken: cancellationToken );
        }
        else
        {
            readItem = layered.GetItem( _key );
        }

        Assert.True(
            readItem is { Value: "value" } && l1.ContainsItem( _key ),
            "Precondition: the read did not return the value of the second layer, or it did not store the value in the first layer." );

        this._output.WriteLine(
            $"First layer after the read: {DescribeItem( l1.GetItem( _key, includeDependencies: true ) )}, dependency registered: {l1.ContainsDependency( _dependency )}." );

        layered.InvalidateDependency( _dependency );

        Assert.False( remote.ContainsItem( _key ), "Precondition: the invalidation did not remove the item from the second layer." );

        // The events of the second layer and of the enhancer go through the work-item dispatcher of the services.
        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationToken );

        var finalItem = layered.GetItem( _key );

        this._output.WriteLine( "Reads of the second layer: " + string.Join( " | ", remote.Reads ) );
        this._output.WriteLine( "Events of the second layer: " + string.Join( " | ", remoteEvents ) );
        this._output.WriteLine( "Final read: " + DescribeItem( finalItem ) );

        return finalItem;
    }

    /// <summary>
    /// Reads the key through the layered backend while the removal of the second layer is pending, completes that
    /// removal, reads the key again, and asserts that neither read returns a value.
    /// </summary>
    /// <param name="layered">The layered backend, on which the key has just been removed.</param>
    /// <param name="remote">The second layer, which defers its removals.</param>
    private void AssertPendingRemovalIsMasked( LayeredCachingBackendEnhancer layered, RemoteBackendDouble remote )
    {
        this._output.WriteLine( $"First layer after the removal: {DescribeItem( layered.LocalCache.GetItem( _key ) )}." );

        Assert.True(
            remote.PendingRemovalCount == 1 && remote.ContainsItem( _key ),
            "Precondition: the removal of the second layer is not pending, or the second layer no longer holds the item." );

        var readDuringRemoval = layered.GetItem( _key );

        remote.CompletePendingRemovals();

        Assert.False( remote.ContainsItem( _key ), "Precondition: the completed removal did not remove the item from the second layer." );

        var readAfterRemoval = layered.GetItem( _key );

        this._output.WriteLine( $"Read while the removal was pending: {DescribeItem( readDuringRemoval )}." );
        this._output.WriteLine( $"Read after the removal had completed: {DescribeItem( readAfterRemoval )}." );

        Assert.True(
            readDuringRemoval == null,
            $"GetItem returned {DescribeItem( readDuringRemoval )} while the removal of the second layer was pending. The first layer did not mask the value of the second layer." );

        Assert.True(
            readAfterRemoval == null,
            $"GetItem returned {DescribeItem( readAfterRemoval )} after the removal of the second layer had completed. The first layer stored the removed value." );
    }

    /// <summary>
    /// Describes a cache item with its runtime type, its value and its dependencies.
    /// </summary>
    /// <param name="item">The item, or <see langword="null"/>.</param>
    /// <returns>The description.</returns>
    private static string DescribeItem( CacheItem? item )
    {
        if ( item == null )
        {
            return "null";
        }

        var dependencies = item.Dependencies.IsDefault ? "default" : "[" + string.Join( ", ", item.Dependencies ) + "]";

        return $"{item.GetType().Name}( Value = {item.Value ?? "null"}, Dependencies = {dependencies} )";
    }
}
