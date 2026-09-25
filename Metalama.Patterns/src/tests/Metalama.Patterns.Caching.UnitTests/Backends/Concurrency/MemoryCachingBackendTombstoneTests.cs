// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;
using ITestSynchronizationProvider = Metalama.Testing.Hooks.ITestSynchronizationProvider;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the path of <see cref="MemoryCachingBackend.RemoveItemImpl"/> and
/// <see cref="MemoryCachingBackend.InvalidateDependencyImpl"/> that replaces a removed item with a replacement value
/// instead of removing its entry.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="LayeredCachingBackendEnhancer"/> uses this path for its local cache when the remote backend is not
/// blocking. The replacement value marks the removal until the remote backend has processed it. These tests call it a
/// tombstone.
/// </para>
/// <para>
/// The tests call the internal methods directly with a <see cref="Tombstone"/>, which has the same shape as the
/// replacement value of the enhancer. They force interleavings with the synchronization points of the backend and with
/// the gates of <see cref="InterceptingMemoryCache"/>, without a hardcoded delay. Every timeout only detects a failure.
/// </para>
/// </remarks>
public sealed partial class MemoryCachingBackendTombstoneTests
{
    /// <summary>
    /// The key of the item that every test removes or invalidates.
    /// </summary>
    private const string _itemKey = "item";

    /// <summary>
    /// The dependency of the item in the test of two concurrent invalidations.
    /// </summary>
    private const string _dependencyKey = "dependency";

    /// <summary>
    /// The dependency of the value that the item has before a concurrent <see cref="CachingBackend.SetItem"/> call.
    /// </summary>
    private const string _previousDependencyKey = "previousDependency";

    /// <summary>
    /// The dependency of the value that a concurrent <see cref="CachingBackend.SetItem"/> call stores.
    /// </summary>
    private const string _newDependencyKey = "newDependency";

    /// <summary>
    /// The value that each test stores for the item before the operation under test.
    /// </summary>
    private const string _storedValue = "stored";

    /// <summary>
    /// The value that a concurrent <see cref="CachingBackend.SetItem"/> call stores.
    /// </summary>
    private const string _newValue = "new";

    /// <summary>
    /// The message of the exception that bounds the recursion of an invalidation.
    /// </summary>
    private const string _recursionBoundMessage = "The invalidation exceeded the recursion depth allowed by the test.";

    /// <summary>
    /// The number of reads of the item that the test allows during one invalidation. The read that follows them throws
    /// an exception.
    /// </summary>
    /// <remarks>
    /// The invalidation reads the item a bounded number of times per level of recursion. In the test, an invalidation
    /// that terminates reads it only a few times, so the bound leaves a wide margin. The bound also keeps the stack far
    /// from an overflow.
    /// </remarks>
    private const int _maximumItemReads = 50;

    /// <summary>
    /// The name of the synchronization point that an invalidation reaches after it has copied a dependency set, while it
    /// holds no lock.
    /// </summary>
    private const string _dependentsCopiedSyncPoint = "MemoryCachingBackend.InvalidateDependencyImpl:DependentsCopied";

    /// <summary>
    /// The name of the thread of the first invalidation.
    /// </summary>
    private const string _firstInvalidationThreadName = "FirstInvalidation";

    /// <summary>
    /// The name of the thread of the second invalidation.
    /// </summary>
    private const string _secondInvalidationThreadName = "SecondInvalidation";

    /// <summary>
    /// The name of the thread of the removal.
    /// </summary>
    private const string _removalThreadName = "Removal";

    /// <summary>
    /// The name of the thread of the concurrent <see cref="CachingBackend.SetItem"/> call.
    /// </summary>
    private const string _setItemThreadName = "SetItem";

    /// <summary>
    /// The maximum time to wait for a pause point or a worker. It only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The time during which a tombstone stays in the cache. It is much longer than a test, so no tombstone expires while
    /// a test runs.
    /// </summary>
    private static readonly TimeSpan _tombstoneLifetime = TimeSpan.FromHours( 1 );

    /// <summary>
    /// The output of the current test, which receives a few diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendTombstoneTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendTombstoneTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Two concurrent invalidations of one dependency with a tombstone must remove the dependent item once, so the backend
    /// must raise a single <see cref="CachingBackend.ItemRemoved"/> event for the item and must not overwrite the first
    /// tombstone with the second one.
    /// </summary>
    /// <param name="releaseFirstInvalidationFirst">
    /// <see langword="true"/> to let the first invalidation process the item before the second one,
    /// <see langword="false"/> to let the second invalidation process it first.
    /// </param>
    /// <remarks>
    /// <para>
    /// The test guards against an invalidation that replaces the item with its tombstone without checking, under the lock
    /// of the item, that the item still holds a value. When another invalidation has already replaced the item, such an
    /// invalidation overwrites the tombstone, raises a second <see cref="CachingBackend.ItemRemoved"/> event, and raises a
    /// second <see cref="CachingBackend.DependencyInvalidated"/> event for the key of the item.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the item with the dependency.</description></item>
    /// <item><description>The first invalidation copies the dependency set, which contains the item. The synchronization
    /// point <c>InvalidateDependencyImpl:DependentsCopied</c> pauses it before it processes the item. It holds no
    /// lock.</description></item>
    /// <item><description>The test arms the synchronization point again, and the second invalidation copies the same set
    /// and is paused at the same point. Both invalidations have now copied a set that contains the item, and neither has
    /// processed it.</description></item>
    /// <item><description>The test releases one invalidation and waits until it completes. It replaces the item with its
    /// tombstone. The test then releases the other invalidation and waits until it completes. That invalidation finds
    /// the tombstone under the lock of the item and must leave it in place.</description></item>
    /// </list>
    /// <para>
    /// The item is then removed once. The backend must raise one <see cref="CachingBackend.ItemRemoved"/> event for the
    /// item, one <see cref="CachingBackend.DependencyInvalidated"/> event for the key of the item, which the invalidation
    /// that removed the item raises, and one <see cref="CachingBackend.DependencyInvalidated"/> event for the dependency
    /// per invalidation. The stored tombstone must be the one of the invalidation that ran first.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public async Task TombstoneInvalidation_ConcurrentWithSameInvalidation_RaisesItemRemovedOnce( bool releaseFirstInvalidationFirst )
    {
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = new FakeCachingServices( configureServices: s => s.AddSingleton<ITestSynchronizationProvider>( synchronization ) );
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, fakes.ServiceProvider );

        var itemRemovedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        var dependencyInvalidatedEvents = new ConcurrentQueue<CacheDependencyInvalidatedEventArgs>();
        backend.ItemRemoved += ( _, args ) => itemRemovedEvents.Enqueue( args );
        backend.DependencyInvalidated += ( _, args ) => dependencyInvalidatedEvents.Enqueue( args );

        backend.SetItem( _itemKey, new CacheItem( _storedValue, [_dependencyKey] ) );

        var expiration = fakes.TimeProvider.GetUtcNow() + _tombstoneLifetime;

        // No other thread runs, so only the first invalidation can reach the synchronization point.
        var firstSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );
        TestSynchronizationProvider.SyncPoint? secondSyncPoint = null;

        var firstInvalidation = ConcurrencyTestWorker.Start(
            _firstInvalidationThreadName,
            () => backend.InvalidateDependencyImpl( _dependencyKey, new Tombstone( 1 ), expiration ) );

        ConcurrencyTestWorker? secondInvalidation = null;
        bool workersCompleted;

        try
        {
            Assert.True(
                firstSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first invalidation did not copy the dependency set." );

            // The first invalidation is paused, so only the second invalidation can reach the synchronization point.
            secondSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );

            secondInvalidation = ConcurrencyTestWorker.Start(
                _secondInvalidationThreadName,
                () => backend.InvalidateDependencyImpl( _dependencyKey, new Tombstone( 2 ), expiration ) );

            Assert.True(
                secondSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the second invalidation did not copy the dependency set." );

            Assert.Equal( _storedValue, backend.GetItem( _itemKey )?.Value );

            Assert.True(
                backend.ContainsDependency( _dependencyKey ),
                "Schedule precondition failed: the item is no longer registered under the dependency although neither invalidation has processed it." );

            var (earlierSyncPoint, earlierWorker, laterSyncPoint, laterWorker) = releaseFirstInvalidationFirst
                ? (firstSyncPoint, firstInvalidation, secondSyncPoint, secondInvalidation)
                : (secondSyncPoint, secondInvalidation, firstSyncPoint, firstInvalidation);

            earlierSyncPoint.Release();

            Assert.True(
                earlierWorker.Join( _timeout ),
                $"The invalidation on the thread '{earlierWorker.Name}' did not complete while the other invalidation was paused without a lock." );

            Assert.Null( earlierWorker.Exception );

            var itemAfterEarlierInvalidation = backend.GetItem( _itemKey );

            Assert.True(
                itemAfterEarlierInvalidation is Tombstone,
                $"The invalidation on the thread '{earlierWorker.Name}' did not replace the item with its tombstone. "
                + $"The cache holds {Describe( itemAfterEarlierInvalidation )}." );

            laterSyncPoint.Release();

            Assert.True( laterWorker.Join( _timeout ), $"The invalidation on the thread '{laterWorker.Name}' did not complete." );
        }
        finally
        {
            firstSyncPoint.Release();
            secondSyncPoint?.Release();
            workersCompleted = JoinAll( firstInvalidation, secondInvalidation );
        }

        Assert.True( workersCompleted, "An invalidation did not complete after the synchronization points were released." );
        Assert.Null( firstInvalidation.Exception );
        Assert.Null( secondInvalidation?.Exception );

        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        var itemRemovedCount = itemRemovedEvents.Count( e => e.Key == _itemKey );
        var itemDependencyInvalidatedCount = dependencyInvalidatedEvents.Count( e => e.Key == _itemKey );
        var dependencyInvalidatedCount = dependencyInvalidatedEvents.Count( e => e.Key == _dependencyKey );
        var storedItem = backend.GetItem( _itemKey );

        this._output.WriteLine(
            $"ItemRemoved events for the item: {itemRemovedCount}. DependencyInvalidated events for the item: {itemDependencyInvalidatedCount}. "
            + $"DependencyInvalidated events for the dependency: {dependencyInvalidatedCount}. The cache holds {Describe( storedItem )}." );

        Assert.True(
            itemRemovedCount == 1,
            $"The two invalidations raised {itemRemovedCount} ItemRemoved events for the item. "
            + "Exactly one event is expected, because the item was removed once." );

        Assert.All( itemRemovedEvents, e => Assert.Equal( CacheItemRemovedReason.Invalidated, e.RemovedReason ) );

        Assert.True(
            itemDependencyInvalidatedCount == 1,
            $"The two invalidations raised {itemDependencyInvalidatedCount} DependencyInvalidated events for the key of the item. "
            + "Exactly one event is expected, from the invalidation that removed the item." );

        Assert.True(
            dependencyInvalidatedCount == 2,
            $"The two invalidations raised {dependencyInvalidatedCount} DependencyInvalidated events for the dependency. Exactly two events are expected." );

        var expectedTimestamp = releaseFirstInvalidationFirst ? 1 : 2;
        var laterThreadName = releaseFirstInvalidationFirst ? _secondInvalidationThreadName : _firstInvalidationThreadName;

        Assert.True(
            storedItem is Tombstone tombstone && tombstone.Timestamp == expectedTimestamp,
            $"The cache holds {Describe( storedItem )} instead of the tombstone with the timestamp {expectedTimestamp}, "
            + "which the invalidation that ran first stored." );

        var laterEntryCreations = cache.Calls.Count(
            c => c.Operation == InterceptedOperation.CreateEntry
                 && InterceptingMemoryCache.ItemKey( _itemKey )( c.Key )
                 && string.Equals( c.ThreadName, laterThreadName, StringComparison.Ordinal ) );

        Assert.True(
            laterEntryCreations == 0,
            $"The invalidation on the thread '{laterThreadName}' created {laterEntryCreations} cache entries for the item after it had already been replaced with a tombstone." );

        Assert.False( backend.ContainsDependency( _dependencyKey ), "The invalidated dependency is still registered." );
    }

    /// <summary>
    /// A removal with a tombstone that runs concurrently with a <see cref="CachingBackend.SetItem"/> call of the same item
    /// must leave the item registered exactly under the dependencies of its final value.
    /// </summary>
    /// <param name="pauseRemoval">
    /// <see langword="true"/> to pause the removal while it holds the lock of the item, so that the removal completes
    /// first. <see langword="false"/> to pause the <see cref="CachingBackend.SetItem"/> call, so that it completes first.
    /// </param>
    /// <remarks>
    /// <para>
    /// The test guards against a removal that cleans the dependencies of the value that it has read before it acquires
    /// the lock of the item. When a <see cref="CachingBackend.SetItem"/> call stores a new value in the meantime, such a
    /// removal replaces the new value with its tombstone, but it leaves the item registered under the dependency of the
    /// new value, although a tombstone has no dependencies.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the item with the previous dependency.</description></item>
    /// <item><description>A gate pauses one operation after its read of the item, while it holds the lock of the item:
    /// the removal when <paramref name="pauseRemoval"/> is <see langword="true"/>, otherwise the
    /// <see cref="CachingBackend.SetItem"/> call, which stores a new value with the new dependency.</description></item>
    /// <item><description>The test starts the other operation on a worker thread. It needs the lock of the item, so it
    /// cannot change the item before the paused operation has completed.</description></item>
    /// <item><description>The test releases the gate and waits for both operations.</description></item>
    /// </list>
    /// <para>
    /// When the removal completes first, the item must hold the new value, registered under the new dependency only, and
    /// the invalidation of the new dependency must remove it. When the <see cref="CachingBackend.SetItem"/> call
    /// completes first, the item must hold the tombstone, and it must not be registered under any dependency.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void TombstoneRemoval_ConcurrentWithSetItem_LeavesNoStaleRegistration( bool pauseRemoval )
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        backend.SetItem( _itemKey, new CacheItem( _storedValue, [_previousDependencyKey] ) );

        var pausedThreadName = pauseRemoval ? _removalThreadName : _setItemThreadName;

        // The gate pauses the chosen operation after its first read of the item, while it holds the lock of the item.
        var itemReadGate = cache.Arm(
            InterceptedOperation.TryGetValueAfter,
            InterceptingMemoryCache.ItemKey( _itemKey ),
            condition: InterceptingMemoryCache.OnThread( pausedThreadName ) );

        void Remove() => backend.RemoveItemImpl( _itemKey, new Tombstone( 1 ), DateTimeOffset.UtcNow + _tombstoneLifetime );

        void Set() => backend.SetItem( _itemKey, new CacheItem( _newValue, [_newDependencyKey] ) );

        var pausedWorker = pauseRemoval
            ? ConcurrencyTestWorker.Start( _removalThreadName, Remove )
            : ConcurrencyTestWorker.Start( _setItemThreadName, Set );

        ConcurrencyTestWorker? otherWorker = null;
        bool workersCompleted;

        try
        {
            Assert.True(
                itemReadGate.WaitUntilReached( _timeout ),
                $"Schedule precondition failed: the thread '{pausedThreadName}' did not read the item." );

            var observedItem = Assert.IsType<MemoryCacheItem>( itemReadGate.ObservedValue );
            Assert.Equal( _storedValue, observedItem.Value );

            otherWorker = pauseRemoval
                ? ConcurrencyTestWorker.Start( _setItemThreadName, Set )
                : ConcurrencyTestWorker.Start( _removalThreadName, Remove );
        }
        finally
        {
            itemReadGate.Release();
            workersCompleted = JoinAll( pausedWorker, otherWorker );
        }

        Assert.True( workersCompleted, "A worker did not complete after the gate was released." );
        Assert.Null( pausedWorker.Exception );
        Assert.Null( otherWorker?.Exception );

        var storedItem = backend.GetItem( _itemKey, includeDependencies: true );

        this._output.WriteLine( $"After both operations, the cache holds {Describe( storedItem )}." );

        Assert.False(
            backend.ContainsDependency( _previousDependencyKey ),
            "The item is still registered under the dependency of the value that both operations replaced." );

        if ( pauseRemoval )
        {
            Assert.True(
                storedItem is not Tombstone && Equals( storedItem?.Value, _newValue ),
                $"The SetItem call ran after the removal, but the cache holds {Describe( storedItem )} instead of the value '{_newValue}'." );

            Assert.True(
                backend.ContainsDependency( _newDependencyKey ),
                "The SetItem call ran after the removal, but the item is not registered under the dependency of its value." );

            backend.InvalidateDependency( _newDependencyKey );

            Assert.True(
                backend.GetItem( _itemKey ) is null,
                "The invalidation of the dependency of the value stored by the SetItem call did not remove the item." );

            Assert.False(
                backend.ContainsDependency( _newDependencyKey ),
                "The dependency of the value stored by the SetItem call is still registered after its invalidation." );
        }
        else
        {
            Assert.True(
                storedItem is Tombstone,
                $"The removal ran after the SetItem call, but the cache holds {Describe( storedItem )} instead of the tombstone." );

            Assert.False(
                backend.ContainsDependency( _newDependencyKey ),
                "The removal replaced the value stored by the SetItem call with a tombstone, "
                + "but the item is still registered under the dependency of that value." );
        }
    }

    /// <summary>
    /// An invalidation with a tombstone must terminate when the item depends on its own key, and must leave the tombstone
    /// in the cache and no registration of the item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against an invalidation that recurses without end when the key of a removed item stays registered
    /// in the dependency set of that key and each level of recursion treats the tombstone as a removed item again. The
    /// stack then overflows.
    /// </para>
    /// <para>
    /// The test creates the registration legitimately: <see cref="CachingBackend.SetItem"/> stores the item with a
    /// dependency on its own key, which registers the item in its own dependency set. The invalidation of the key of the
    /// item copies that set, which contains the item, and replaces the item with a tombstone. The item has a dependent
    /// (itself), so its registrations are retained, and the invalidation recurses into the key of the item. The recursion
    /// finds the item in the set again, finds the tombstone, and must stop, because the key has already been visited.
    /// </para>
    /// <para>
    /// A stack overflow terminates the test host, so a regression must not produce one. The test therefore bounds the
    /// recursion with a fault gate, which throws an exception in the read of the item that follows
    /// <see cref="_maximumItemReads"/> reads.
    /// </para>
    /// </remarks>
    [Fact]
    public void TombstoneInvalidation_WithLeakedSelfRegistration_Terminates()
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        backend.SetItem( _itemKey, new CacheItem( _storedValue, [_itemKey] ) );

        Assert.Equal( _storedValue, backend.GetItem( _itemKey )?.Value );

        Assert.True(
            backend.ContainsDependency( _itemKey ),
            "Precondition failed: the SetItem call did not register the item in its own dependency set, "
            + "so the registration that this test requires does not exist." );

        var readsBeforeInvalidation = cache.GetCallCount( InterceptedOperation.TryGetValueBefore, InterceptingMemoryCache.ItemKey( _itemKey ) );

        var recursionBound = cache.ArmFault(
            InterceptedOperation.TryGetValueBefore,
            InterceptingMemoryCache.ItemKey( _itemKey ),
            () => new InvalidOperationException( _recursionBoundMessage ),
            skip: _maximumItemReads );

        var invalidationException = Record.Exception(
            () => backend.InvalidateDependencyImpl( _itemKey, new Tombstone( 1 ), DateTimeOffset.UtcNow + _tombstoneLifetime ) );

        var readsDuringInvalidation =
            cache.GetCallCount( InterceptedOperation.TryGetValueBefore, InterceptingMemoryCache.ItemKey( _itemKey ) ) - readsBeforeInvalidation;

        this._output.WriteLine( $"The invalidation read the item {readsDuringInvalidation} times." );

        Assert.False(
            recursionBound.HasTripped,
            $"The invalidation read the item more than {_maximumItemReads} times, so its recursion does not terminate." );

        Assert.Null( invalidationException );

        var storedItem = backend.GetItem( _itemKey );

        Assert.True( storedItem is Tombstone, $"The cache holds {Describe( storedItem )} instead of the tombstone of the invalidation." );

        Assert.False(
            backend.ContainsDependency( _itemKey ),
            "The item is still registered in its own dependency set after the invalidation replaced it with a tombstone." );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> that stores its entries in <paramref name="cache"/>.
    /// </summary>
    /// <remarks>
    /// The backend does not own <paramref name="cache"/>, because the cache is passed to its constructor. The test
    /// therefore disposes the cache after the backend.
    /// </remarks>
    /// <param name="cache">The cache in which the backend stores its entries.</param>
    /// <param name="serviceProvider">The service provider of the backend, or <see langword="null"/>.</param>
    /// <returns>The initialized backend.</returns>
    private static MemoryCachingBackend CreateBackend( InterceptingMemoryCache cache, IServiceProvider? serviceProvider = null )
    {
        var backend = new MemoryCachingBackend(
            cache,
            new MemoryCachingBackendConfiguration { DebugName = "test" },
            serviceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Waits until each started worker completes. The timeout only detects a failure, such as a deadlock.
    /// </summary>
    /// <remarks>
    /// A test calls this method in a <c>finally</c> block after it has released its pause points, so that no worker still
    /// runs inside the backend when the backend and its cache are disposed.
    /// </remarks>
    /// <param name="workers">The workers. A <see langword="null"/> element represents a worker that was not started.</param>
    /// <returns><see langword="true"/> when every started worker has completed, otherwise <see langword="false"/>.</returns>
    private static bool JoinAll( params ConcurrencyTestWorker?[] workers )
    {
        var allCompleted = true;

        foreach ( var worker in workers )
        {
            if ( worker != null && !worker.Join( _timeout ) )
            {
                allCompleted = false;
            }
        }

        return allCompleted;
    }

    /// <summary>
    /// Describes a cache item for a diagnostic message.
    /// </summary>
    /// <param name="item">The cache item, or <see langword="null"/> when the cache holds no item.</param>
    /// <returns>A description of the item.</returns>
    private static string Describe( CacheItem? item )
        => item switch
        {
            null => "no item",
            Tombstone tombstone => $"the tombstone with the timestamp {tombstone.Timestamp}",
            { } other => $"the value '{other.Value}'"
        };
}