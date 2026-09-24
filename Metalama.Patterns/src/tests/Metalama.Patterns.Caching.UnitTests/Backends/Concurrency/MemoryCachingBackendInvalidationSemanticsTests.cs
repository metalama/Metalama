// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;
using ITestSynchronizationProvider = Metalama.Testing.Hooks.ITestSynchronizationProvider;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the semantics of <see cref="CachingBackend.InvalidateDependency"/> in <see cref="MemoryCachingBackend"/>: which
/// items an invalidation removes when it runs concurrently with <see cref="CachingBackend.SetItem"/> or with another
/// invalidation, and whether an invalidation reaches the items that depend on the dependency through the key of an
/// intermediate item.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MemoryCachingBackend"/> keeps, for each dependency, a set of the keys of the items that depend on it. An
/// invalidation copies this set while it holds the lock of the set, and releases that lock. It then processes each copied
/// key under the lock of the key: it removes the item when its current value still declares the invalidated dependency.
/// It then invalidates the key itself, which processes the items that depend on that key, and only after that removes
/// the key from the dependency set.
/// </para>
/// <para>
/// The concurrent tests run the operations on named <see cref="ConcurrencyTestWorker"/> threads. They pause a thread at
/// the synchronization point <c>MemoryCachingBackend.InvalidateDependencyImpl:DependentsCopied</c>, which an invalidation
/// reaches after it has copied a dependency set, while it holds no lock, or while it holds the lock of a key, with a gate
/// of an <see cref="InterceptingMemoryCache"/>. A synchronization point is armed only when the intended thread is the only
/// thread that can reach it next. Each test asserts that every pause was reached before it asserts the behaviour, so that
/// a change of the order of the operations of the backend is reported as a failed precondition.
/// </para>
/// <para>
/// <see cref="FakeCachingServices"/> supplies the work-item dispatcher through which the backend raises its events, the
/// synchronization provider of the backend, and, in the expiration tests, the clock and the <see cref="FakeMemoryCache"/>.
/// </para>
/// </remarks>
public sealed class MemoryCachingBackendInvalidationSemanticsTests
{
    /// <summary>
    /// The name of the thread that calls <see cref="CachingBackend.SetItem"/>.
    /// </summary>
    private const string _setterThreadName = "Setter";

    /// <summary>
    /// The name of the thread that calls <see cref="CachingBackend.InvalidateDependency"/> when a test has a single
    /// invalidation.
    /// </summary>
    private const string _invalidatorThreadName = "Invalidator";

    /// <summary>
    /// The name of the thread that starts the first of two concurrent invalidations.
    /// </summary>
    private const string _firstInvalidatorThreadName = "FirstInvalidator";

    /// <summary>
    /// The name of the thread that starts the second of two concurrent invalidations.
    /// </summary>
    private const string _secondInvalidatorThreadName = "SecondInvalidator";

    /// <summary>
    /// The dependency that the tests invalidate.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The key of the item that a concurrent <see cref="CachingBackend.SetItem"/> call replaces.
    /// </summary>
    private const string _replacedKey = "replaced";

    /// <summary>
    /// The value of the replaced item before the concurrent <see cref="CachingBackend.SetItem"/> call.
    /// </summary>
    private const string _oldValue = "old-value";

    /// <summary>
    /// The value that the concurrent <see cref="CachingBackend.SetItem"/> call stores.
    /// </summary>
    private const string _newValue = "new-value";

    /// <summary>
    /// The only dependency of the value that the concurrent <see cref="CachingBackend.SetItem"/> call stores.
    /// </summary>
    private const string _newDependency = "new-dependency";

    /// <summary>
    /// The key of the item that the dependency set lists when the invalidation copies the set.
    /// </summary>
    private const string _listedKey = "listed";

    /// <summary>
    /// The value of the item stored under <see cref="_listedKey"/>.
    /// </summary>
    private const string _listedValue = "listed-value";

    /// <summary>
    /// The key of the item that is stored after the invalidation has copied the dependency set.
    /// </summary>
    private const string _addedKey = "added";

    /// <summary>
    /// The value of the item stored under <see cref="_addedKey"/>.
    /// </summary>
    private const string _addedValue = "added-value";

    /// <summary>
    /// The key of the intermediate item, which depends on <see cref="_dependency"/>.
    /// </summary>
    private const string _innerKey = "inner";

    /// <summary>
    /// The value of the intermediate item.
    /// </summary>
    private const string _innerValue = "inner-value";

    /// <summary>
    /// The key of the outer item, which depends on the key of the intermediate item.
    /// </summary>
    private const string _outerKey = "outer";

    /// <summary>
    /// The value of the outer item.
    /// </summary>
    private const string _outerValue = "outer-value";

    /// <summary>
    /// The name of the synchronization point that an invalidation reaches after it has copied a dependency set, while it
    /// holds no lock.
    /// </summary>
    private const string _dependentsCopiedSyncPoint = "MemoryCachingBackend.InvalidateDependencyImpl:DependentsCopied";

    /// <summary>
    /// The maximum time to wait for a gate, a worker or the pending work items. It only detects a failure, such as a
    /// deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The instant at which the clock of the expiration tests starts.
    /// </summary>
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    /// <summary>
    /// The absolute expiration of the intermediate item in the expiration tests.
    /// </summary>
    private static readonly TimeSpan _innerItemLifetime = TimeSpan.FromMinutes( 1 );

    /// <summary>
    /// The amount by which the expiration tests advance the clock to expire the intermediate item.
    /// </summary>
    private static readonly TimeSpan _advanceBeyondExpiration = TimeSpan.FromMinutes( 2 );

    /// <summary>
    /// The amount by which the control of the expiration tests advances the clock without expiring the intermediate item.
    /// </summary>
    private static readonly TimeSpan _advanceBeforeExpiration = TimeSpan.FromSeconds( 30 );

    /// <summary>
    /// The helper that writes diagnostic lines to the output of the test.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendInvalidationSemanticsTests"/> class.
    /// </summary>
    /// <param name="output">The helper that writes diagnostic lines to the output of the test.</param>
    public MemoryCachingBackendInvalidationSemanticsTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Tests that an invalidation keeps a value that a concurrent <see cref="CachingBackend.SetItem"/> call stores
    /// without the invalidated dependency, although the previous value of the same key declared that dependency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setup: the replaced item holds the old value, which depends on the invalidated dependency. The dependency set of
    /// the invalidated dependency therefore lists the replaced item.
    /// </para>
    /// <para>
    /// Schedule:
    /// </para>
    /// <list type="number">
    /// <item>The invalidator thread invalidates the dependency. It copies the dependency set, which lists the replaced
    /// item, and the synchronization point <c>InvalidateDependencyImpl:DependentsCopied</c> pauses it before it processes
    /// the item. It holds no lock.</item>
    /// <item>The setter thread calls <see cref="CachingBackend.SetItem"/> with the new value, which depends only on
    /// another dependency, and completes. It registers the item under the other dependency, stores the new value, and
    /// unregisters the item from the invalidated dependency.</item>
    /// <item>The test releases the invalidator. It processes the copied key under the lock of the key, and finds the new
    /// value, which does not declare the invalidated dependency.</item>
    /// </list>
    /// <para>
    /// The new value does not declare the invalidated dependency, so the invalidation must not remove it, and the item
    /// must stay registered only under its new dependency. A later invalidation of the new dependency must remove it.
    /// </para>
    /// <para>
    /// The test guards against an invalidation that removes whatever value a copied key holds when it processes the key,
    /// without checking that this value still declares the invalidated dependency.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_ConcurrentSetItemStoresValueWithoutTheDependency_KeepsTheNewValue()
    {
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = CreateFakes( synchronization );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        backend.SetItem( _replacedKey, new CacheItem( _oldValue, [_dependency] ) );

        var removedItems = RecordRemovedItems( backend );

        // No other thread runs, so only the invalidator can reach the synchronization point.
        var invalidatorSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );

        ConcurrencyTestWorker? invalidator = null;
        ConcurrencyTestWorker? setter = null;

        try
        {
            invalidator = ConcurrencyTestWorker.Start( _invalidatorThreadName, () => backend.InvalidateDependency( _dependency ) );

            Assert.True(
                invalidatorSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the invalidation did not copy the dependency set." );

            setter = ConcurrencyTestWorker.Start(
                _setterThreadName,
                () => backend.SetItem( _replacedKey, new CacheItem( _newValue, [_newDependency] ) ) );

            AssertCompleted(
                setter,
                "The SetItem call did not complete while the invalidation was paused without a lock after it copied the dependency set." );

            Assert.Equal( _newValue, backend.GetItem( _replacedKey )?.Value );

            Assert.False(
                backend.ContainsDependency( _dependency ),
                "Schedule precondition failed: the SetItem call did not unregister the item from the dependency of the old value." );

            invalidatorSyncPoint.Release();

            AssertCompleted( invalidator, "The invalidation did not complete after its synchronization point was released." );
        }
        finally
        {
            invalidatorSyncPoint.Release();
            invalidator?.Join( _timeout );
            setter?.Join( _timeout );
        }

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        var replacedItemState = DescribePresence( backend.ContainsItem( _replacedKey ) );
        var invalidatedDependencyState = DescribePresence( backend.ContainsDependency( _dependency ) );
        var newDependencyState = DescribePresence( backend.ContainsDependency( _newDependency ) );

        this._output.WriteLine(
            "Final state: the replaced item is " + replacedItemState + ", the registration under the invalidated dependency is "
            + invalidatedDependencyState + ", and the registration under the new dependency is " + newDependencyState + "." );

        Assert.True(
            backend.ContainsItem( _replacedKey ),
            "The invalidation removed the value that the concurrent SetItem call stored, although that value does not declare the invalidated dependency." );

        Assert.Equal( _newValue, backend.GetItem( _replacedKey )?.Value );
        Assert.Empty( removedItems );

        Assert.False(
            backend.ContainsDependency( _dependency ),
            "The item is still registered under the invalidated dependency, although its value no longer declares that dependency." );

        Assert.True(
            backend.ContainsDependency( _newDependency ),
            "The item is not registered under the new dependency, although the new value is still in the cache and declares that dependency." );

        // The new value must be reachable through its new dependency.
        backend.InvalidateDependency( _newDependency );

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        Assert.False( backend.ContainsItem( _replacedKey ), "The invalidation of the new dependency did not remove the new value." );
        Assert.False( backend.ContainsDependency( _newDependency ), "The new dependency remained registered after its only item had been removed." );
        Assert.Collection( removedItems, args => AssertItemRemoved( args, _replacedKey, CacheItemRemovedReason.Invalidated ) );
    }

    /// <summary>
    /// Tests that an item stored after an invalidation has copied the dependency set survives the invalidation, and that
    /// the dependency set still lists the item, so that a later invalidation removes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test characterizes the snapshot semantics of the invalidation. The invalidation removes the items that the
    /// dependency set lists when the invalidation copies it. A key that is registered after the copy is not removed, and
    /// the dependency set stays in the index because it is not empty. This outcome is equivalent to the serialization in
    /// which the <see cref="CachingBackend.SetItem"/> call runs after the invalidation.
    /// </para>
    /// <para>
    /// Schedule:
    /// </para>
    /// <list type="number">
    /// <item>The invalidator thread invalidates the dependency. It copies the dependency set, which lists only the listed
    /// item, and pauses after it has read the listed item.</item>
    /// <item>The setter thread stores the added item, which depends on the same dependency, and completes.</item>
    /// <item>The test releases the invalidator, which removes the listed item and completes.</item>
    /// </list>
    /// <para>
    /// Before metalama/Metalama#2066 was fixed, the invalidation held the monitor of the dependency set while it
    /// removed the items, so the <see cref="CachingBackend.SetItem"/> call waited for the whole invalidation, and this
    /// test failed when the SetItem call did not complete within the timeout.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task SetItem_AfterInvalidationSnapshot_KeepsItemAndRegistration()
    {
        using var fakes = new FakeCachingServices();
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, fakes );
        var removedItems = RecordRemovedItems( backend );

        backend.SetItem( _listedKey, new CacheItem( _listedValue, [_dependency] ) );

        var invalidatorGate = cache.Arm(
            InterceptedOperation.TryGetValueAfter,
            InterceptingMemoryCache.ItemKey( _listedKey ),
            condition: InterceptingMemoryCache.OnThread( _invalidatorThreadName ) );

        ConcurrencyTestWorker? invalidator = null;
        ConcurrencyTestWorker? setter = null;

        try
        {
            invalidator = ConcurrencyTestWorker.Start( _invalidatorThreadName, () => backend.InvalidateDependency( _dependency ) );

            Assert.True(
                invalidatorGate.WaitUntilReached( _timeout ),
                "The invalidation did not read the listed item after it copied the dependency set." );

            Assert.True(
                IsCacheItemWithValue( invalidatorGate.ObservedValue, _listedValue ),
                "The invalidation did not read the value of the listed item." );

            setter = ConcurrencyTestWorker.Start(
                _setterThreadName,
                () => backend.SetItem( _addedKey, new CacheItem( _addedValue, [_dependency] ) ) );

            AssertCompleted(
                setter,
                "The SetItem call did not complete while the invalidation was paused after it copied the dependency set. The SetItem call must not wait for the invalidation." );

            invalidatorGate.Release();

            AssertCompleted( invalidator, "The invalidation did not complete after its gate was released." );
        }
        finally
        {
            invalidatorGate.Release();
            invalidator?.Join( _timeout );
            setter?.Join( _timeout );
        }

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        Assert.False( backend.ContainsItem( _listedKey ), "The invalidation did not remove the item that the copied dependency set listed." );

        Assert.True( backend.ContainsItem( _addedKey ), "The invalidation removed the item that was stored after the dependency set had been copied." );
        Assert.Equal( _addedValue, backend.GetItem( _addedKey )?.Value );

        Assert.True(
            backend.ContainsDependency( _dependency ),
            "The dependency set was removed, although the item that was stored after the copy still depends on it." );

        Assert.Collection( removedItems, args => AssertItemRemoved( args, _listedKey, CacheItemRemovedReason.Invalidated ) );

        // A later invalidation must reach the added item through the dependency set.
        backend.InvalidateDependency( _dependency );

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        Assert.False(
            backend.ContainsItem( _addedKey ),
            "A later invalidation of the dependency did not remove the item that was stored after the first invalidation had copied the dependency set." );

        Assert.False( backend.ContainsDependency( _dependency ), "The dependency set was not removed after its last item had been removed." );

        Assert.Collection(
            removedItems,
            args => AssertItemRemoved( args, _listedKey, CacheItemRemovedReason.Invalidated ),
            args => AssertItemRemoved( args, _addedKey, CacheItemRemovedReason.Invalidated ) );
    }

    /// <summary>
    /// Tests that an invalidation does not return before the items that depend on the invalidated dependency through the
    /// key of an intermediate item are removed, when a concurrent invalidation of the same dependency removes the
    /// intermediate item first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setup: the intermediate item depends on the dependency, and the outer item depends on the key of the intermediate
    /// item.
    /// </para>
    /// <para>
    /// Schedule, in which every pause is at the synchronization point <c>InvalidateDependencyImpl:DependentsCopied</c>,
    /// where an invalidation holds no lock:
    /// </para>
    /// <list type="number">
    /// <item>The first invalidation copies the dependency set, which lists the intermediate item, and pauses.</item>
    /// <item>The second invalidation copies the same dependency set, which still lists the intermediate item, and
    /// pauses.</item>
    /// <item>The test releases the first invalidation. It removes the intermediate item, copies the dependency set of the
    /// key of the intermediate item, which lists the outer item, and pauses before it processes the outer item.</item>
    /// <item>The test releases the second invalidation, and waits until it returns. It finds that the intermediate item
    /// has been removed. It must still process the items that depend on the key of the intermediate item, which are still
    /// registered because the first invalidation has not processed them yet.</item>
    /// <item>The test releases the first invalidation, and waits until it returns.</item>
    /// </list>
    /// <para>
    /// When an invalidation returns, the outer item must be removed. A caller that reads the outer item right after its
    /// invalidation has returned must not receive a value that depends on the invalidated dependency. Each worker records
    /// whether the outer item is still in the cache right after its invalidation has returned.
    /// </para>
    /// <para>
    /// The test guards against an invalidation that skips the items that depend on the key of an item that a concurrent
    /// invalidation has already removed, and therefore returns before these items are removed.
    /// </para>
    /// </remarks>
    [Fact]
    public void InvalidateDependency_BothInvalidationsCopiedTheDependencySet_RemovesTransitiveDependentsBeforeReturning()
    {
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = CreateFakes( synchronization );
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        SetUpChain( backend, null );

        var firstInvalidation = new InvalidationObservation();
        var secondInvalidation = new InvalidationObservation();

        // No other thread runs, so only the first invalidation can reach the synchronization point.
        var firstSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );
        TestSynchronizationProvider.SyncPoint? secondSyncPoint = null;
        TestSynchronizationProvider.SyncPoint? cascadeSyncPoint = null;

        ConcurrencyTestWorker? first = null;
        ConcurrencyTestWorker? second = null;

        try
        {
            first = ConcurrencyTestWorker.Start( _firstInvalidatorThreadName, () => InvalidateAndObserve( backend, firstInvalidation ) );

            Assert.True(
                firstSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first invalidation did not copy the dependency set." );

            // The first invalidation is paused, so only the second invalidation can reach the synchronization point.
            secondSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );

            second = ConcurrencyTestWorker.Start( _secondInvalidatorThreadName, () => InvalidateAndObserve( backend, secondInvalidation ) );

            Assert.True(
                secondSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the second invalidation did not copy the dependency set while the first invalidation was paused." );

            // The second invalidation is paused, so only the first invalidation can reach the synchronization point when it
            // copies the dependency set of the key of the intermediate item.
            cascadeSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );
            firstSyncPoint.Release();

            Assert.True(
                cascadeSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first invalidation did not copy the dependency set of the key of the intermediate item." );

            Assert.False(
                backend.ContainsItem( _innerKey ),
                "Schedule precondition failed: the first invalidation did not remove the intermediate item before it reached the dependency set of its key." );

            Assert.True(
                backend.ContainsItem( _outerKey ),
                "Schedule precondition failed: the outer item was removed before either invalidation processed it." );

            secondSyncPoint.Release();

            AssertCompleted(
                second,
                "The second invalidation did not return while the first invalidation was paused without a lock before it processed the outer item." );

            cascadeSyncPoint.Release();

            AssertCompleted( first, "The first invalidation did not complete after its synchronization points were released." );
        }
        finally
        {
            firstSyncPoint.Release();
            secondSyncPoint?.Release();
            cascadeSyncPoint?.Release();
            first?.Join( _timeout );
            second?.Join( _timeout );
        }

        this._output.WriteLine(
            "Right after its invalidation returned, the outer item was " + DescribePresence( secondInvalidation.OuterItemPresentAfterReturn )
            + " for the second invalidation and " + DescribePresence( firstInvalidation.OuterItemPresentAfterReturn ) + " for the first invalidation." );

        Assert.False(
            secondInvalidation.OuterItemPresentAfterReturn,
            "The second invalidation returned before the outer item was removed, although the outer item depends on the invalidated dependency through the key of the intermediate item." );

        Assert.False(
            firstInvalidation.OuterItemPresentAfterReturn,
            "The first invalidation returned before the outer item was removed, although the outer item depends on the invalidated dependency through the key of the intermediate item." );

        Assert.False( backend.ContainsItem( _outerKey ), "The invalidations did not remove the outer item." );
        Assert.False( backend.ContainsDependency( _dependency ), "The dependency remained registered after the invalidations." );
        Assert.False( backend.ContainsDependency( _innerKey ), "The key of the intermediate item remained registered as a dependency after the invalidations." );
    }

    /// <summary>
    /// Tests that an invalidation does not return before the items that depend on the invalidated dependency through the
    /// key of an intermediate item are removed, when a concurrent invalidation of the same dependency has already removed
    /// the intermediate item and is still removing those items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setup: the intermediate item depends on the dependency, and the outer item depends on the key of the intermediate
    /// item.
    /// </para>
    /// <para>
    /// Schedule, in which every pause is at the synchronization point <c>InvalidateDependencyImpl:DependentsCopied</c>,
    /// where an invalidation holds no lock:
    /// </para>
    /// <list type="number">
    /// <item>The first invalidation copies the dependency set and pauses. The test then lets it continue. It removes the
    /// intermediate item, copies the dependency set of the key of the intermediate item, which lists the outer item, and
    /// pauses before it processes the outer item.</item>
    /// <item>The second invalidation of the same dependency runs and returns while the first invalidation is paused.</item>
    /// <item>The test releases the first invalidation, and waits until it returns.</item>
    /// </list>
    /// <para>
    /// The backend keeps a key in the dependency set of an invalidated dependency until the items that depend on that key
    /// have been processed. While the first invalidation is paused, the intermediate item is therefore removed, but its key
    /// is still registered under the dependency, so the second invalidation still finds it and processes the outer item.
    /// The test asserts this invariant as a precondition.
    /// </para>
    /// <para>
    /// When the second invalidation returns, the outer item must be removed. A caller that reads the outer item right
    /// after its invalidation has returned must not receive a value that depends on the invalidated dependency. The worker
    /// of the second invalidation records whether the outer item is still in the cache right after the call has returned.
    /// </para>
    /// <para>
    /// The test guards against an invalidation that unregisters a removed item from the invalidated dependency before it
    /// has processed the items that depend on the key of that item. A concurrent invalidation of the same dependency then
    /// finds no registered item, and returns while the outer item is still in the cache.
    /// </para>
    /// </remarks>
    [Fact]
    public void InvalidateDependency_OtherInvalidationPausedInCascade_RemovesTransitiveDependentsBeforeReturning()
    {
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = CreateFakes( synchronization );
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        SetUpChain( backend, null );

        var firstInvalidation = new InvalidationObservation();
        var secondInvalidation = new InvalidationObservation();

        // No other thread runs, so only the first invalidation can reach the synchronization point.
        var firstSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );
        TestSynchronizationProvider.SyncPoint? cascadeSyncPoint = null;

        ConcurrencyTestWorker? first = null;
        ConcurrencyTestWorker? second = null;

        try
        {
            first = ConcurrencyTestWorker.Start( _firstInvalidatorThreadName, () => InvalidateAndObserve( backend, firstInvalidation ) );

            Assert.True(
                firstSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first invalidation did not copy the dependency set." );

            // The first invalidation is the only thread in the backend, so only it can reach the synchronization point
            // when it copies the dependency set of the key of the intermediate item.
            cascadeSyncPoint = synchronization.Arm( _dependentsCopiedSyncPoint );
            firstSyncPoint.Release();

            Assert.True(
                cascadeSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first invalidation did not copy the dependency set of the key of the intermediate item." );

            Assert.False(
                backend.ContainsItem( _innerKey ),
                "Schedule precondition failed: the first invalidation did not remove the intermediate item before it reached the dependency set of its key." );

            Assert.True(
                backend.ContainsItem( _outerKey ),
                "Schedule precondition failed: the outer item was removed before the first invalidation processed it." );

            // The key of the intermediate item stays registered under the dependency until the first invalidation has
            // processed the items that depend on that key.
            Assert.True(
                backend.ContainsDependency( _dependency ),
                "Schedule precondition failed: the key of the removed intermediate item was unregistered from the dependency before the items that depend on that key were processed." );

            second = ConcurrencyTestWorker.Start( _secondInvalidatorThreadName, () => InvalidateAndObserve( backend, secondInvalidation ) );

            AssertCompleted(
                second,
                "The second invalidation did not return while the first invalidation was paused without a lock before it processed the outer item." );

            cascadeSyncPoint.Release();

            AssertCompleted( first, "The first invalidation did not complete after its synchronization points were released." );
        }
        finally
        {
            firstSyncPoint.Release();
            cascadeSyncPoint?.Release();
            first?.Join( _timeout );
            second?.Join( _timeout );
        }

        this._output.WriteLine(
            "Right after its invalidation returned, the outer item was " + DescribePresence( secondInvalidation.OuterItemPresentAfterReturn )
            + " for the second invalidation and " + DescribePresence( firstInvalidation.OuterItemPresentAfterReturn ) + " for the first invalidation." );

        Assert.False(
            secondInvalidation.OuterItemPresentAfterReturn,
            "The second invalidation returned before the outer item was removed, although the outer item depends on the invalidated dependency through the key of the intermediate item." );

        Assert.False(
            firstInvalidation.OuterItemPresentAfterReturn,
            "The first invalidation returned before the outer item was removed." );

        Assert.False( backend.ContainsItem( _outerKey ), "The invalidations did not remove the outer item." );
        Assert.False( backend.ContainsDependency( _dependency ), "The dependency remained registered after the invalidations." );
        Assert.False( backend.ContainsDependency( _innerKey ), "The key of the intermediate item remained registered as a dependency after the invalidations." );
    }

    /// <summary>
    /// Tests that an invalidation of a dependency removes an outer item that depends on the dependency through the key
    /// of an intermediate item, while the intermediate item is in the cache.
    /// </summary>
    /// <remarks>
    /// This test is the control of
    /// <see cref="InvalidateDependency_IntermediateItemRemoved_RemovesOuterItem"/>. The invalidation removes the
    /// intermediate item, then invalidates the key of the intermediate item, which removes the outer item.
    /// </remarks>
    [Fact]
    public void InvalidateDependency_IntermediateItemPresent_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices();
        using var backend = CreateBackend( new MemoryCache( new MemoryCacheOptions() ), fakes );

        SetUpChain( backend, null );

        Assert.True( backend.ContainsItem( _innerKey ), "The intermediate item is not in the cache after the setup." );
        Assert.True( backend.ContainsItem( _outerKey ), "The outer item is not in the cache after the setup." );

        backend.InvalidateDependency( _dependency );

        Assert.False( backend.ContainsItem( _innerKey ), "The invalidation of the dependency did not remove the intermediate item." );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item, which depends on the dependency through the key of the intermediate item." );

        Assert.False( backend.ContainsDependency( _dependency ), "The dependency set of the dependency was not removed." );
        Assert.False( backend.ContainsDependency( _innerKey ), "The dependency set of the key of the intermediate item was not removed." );
    }

    /// <summary>
    /// Tests that an invalidation of a dependency removes an outer item that depends on the dependency through the key
    /// of an intermediate item, after the intermediate item has been removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a cached method gets a cache hit on another cached method, the caching frontend adds only the key of the
    /// called method to the dependencies of the calling method. The outer item therefore reaches the dependency only
    /// through the dependency set of the dependency, which lists the intermediate item, and through the dependency set of
    /// the key of the intermediate item, which lists the outer item. The documentation states that the dependencies of a
    /// called cached method become dependencies of the calling method, including when the called method was already
    /// cached.
    /// </para>
    /// <para>
    /// The removal of the intermediate item removes its value, but the outer item still depends on its key. The dependency
    /// set of the key of the intermediate item, which lists the outer item, stays in the index. The later invalidation of
    /// the dependency must still remove the outer item, so the key of the intermediate item must stay reachable from the
    /// dependency.
    /// </para>
    /// </remarks>
    [Fact]
    public void InvalidateDependency_IntermediateItemRemoved_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices();
        using var backend = CreateBackend( new MemoryCache( new MemoryCacheOptions() ), fakes );

        SetUpChain( backend, null );

        Assert.True( backend.ContainsDependency( _innerKey ), "The dependency set of the key of the intermediate item is not in the cache after the setup." );

        backend.RemoveItem( _innerKey );

        this._output.WriteLine( "After the removal of the intermediate item: " + DescribeChain( backend ) );

        Assert.False( backend.ContainsItem( _innerKey ), "The removal of the intermediate item did not remove it." );
        Assert.True( backend.ContainsItem( _outerKey ), "The removal of the intermediate item also removed the outer item." );

        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item after the intermediate item had been removed, although the outer item depends on the dependency through the key of the intermediate item." );
    }

    /// <summary>
    /// Tests that an invalidation of a dependency removes an outer item that depends on the dependency through the key
    /// of an intermediate item, after the intermediate item has expired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The intermediate item expires one minute after it is stored, and the outer item does not expire. The test
    /// advances the clock by two minutes. The timer of the <see cref="FakeMemoryCache"/> evicts the intermediate item,
    /// and the post-eviction callback of the backend handles the eviction. The dependency set of the key of the
    /// intermediate item, which lists the outer item, stays in the index.
    /// </para>
    /// <para>
    /// The later invalidation of the dependency must still remove the outer item. See
    /// <see cref="InvalidateDependency_IntermediateItemRemoved_RemovesOuterItem"/> for the reason why the outer item
    /// depends on the dependency only through the key of the intermediate item.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_IntermediateItemExpired_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        using var backend = CreateBackend( fakes.MemoryCache, fakes );

        SetUpChain( backend, new CacheItemConfiguration { AbsoluteExpiration = _innerItemLifetime } );

        Assert.True( backend.ContainsDependency( _innerKey ), "The dependency set of the key of the intermediate item is not in the cache after the setup." );

        await fakes.AdvanceAsync( _advanceBeyondExpiration, cancellationTokenSource.Token );

        this._output.WriteLine( "After the expiration of the intermediate item: " + DescribeChain( backend ) );

        Assert.False( backend.ContainsItem( _innerKey ), "The intermediate item did not expire when the clock passed its expiration instant." );
        Assert.True( backend.ContainsItem( _outerKey ), "The outer item, which does not expire, is not in the cache." );

        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item after the intermediate item had expired, although the outer item depends on the dependency through the key of the intermediate item." );
    }

    /// <summary>
    /// Tests that an invalidation of a dependency removes an outer item that depends on the dependency through the key
    /// of an intermediate item, when the clock has advanced but the intermediate item has not expired yet.
    /// </summary>
    /// <remarks>
    /// This test is the control of <see cref="InvalidateDependency_IntermediateItemExpired_RemovesOuterItem"/>. It uses
    /// the same setup and the same clock, and shows that the outer item is removed as long as the intermediate item is in
    /// the cache.
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_IntermediateItemNotExpiredYet_RemovesOuterItem()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        using var backend = CreateBackend( fakes.MemoryCache, fakes );

        SetUpChain( backend, new CacheItemConfiguration { AbsoluteExpiration = _innerItemLifetime } );

        await fakes.AdvanceAsync( _advanceBeforeExpiration, cancellationTokenSource.Token );

        Assert.True( backend.ContainsItem( _innerKey ), "The intermediate item expired before the clock reached its expiration instant." );
        Assert.True( backend.ContainsItem( _outerKey ), "The outer item, which does not expire, is not in the cache." );

        backend.InvalidateDependency( _dependency );

        Assert.False( backend.ContainsItem( _innerKey ), "The invalidation of the dependency did not remove the intermediate item." );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item, which depends on the dependency through the key of the intermediate item." );

        Assert.False( backend.ContainsDependency( _dependency ), "The dependency set of the dependency was not removed." );
        Assert.False( backend.ContainsDependency( _innerKey ), "The dependency set of the key of the intermediate item was not removed." );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> on a given <see cref="IMemoryCache"/>, with the
    /// services of <paramref name="fakes"/>.
    /// </summary>
    /// <param name="memoryCache">The memory cache of the backend. The backend does not own it.</param>
    /// <param name="fakes">The services that supply the clock and the work-item dispatcher of the backend.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache memoryCache, FakeCachingServices fakes )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( memoryCache ),
            fakes.ServiceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Creates the services of a test whose backend resolves a given synchronization provider.
    /// </summary>
    /// <param name="synchronization">The synchronization provider that the backend resolves.</param>
    /// <returns>The services.</returns>
    private static FakeCachingServices CreateFakes( TestSynchronizationProvider synchronization )
        => new( configureServices: s => s.AddSingleton<ITestSynchronizationProvider>( synchronization ) );

    /// <summary>
    /// Invalidates <see cref="_dependency"/>, and records whether the outer item is still in the cache right after the
    /// invalidation has returned.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="observation">The object that receives the observation.</param>
    private static void InvalidateAndObserve( CachingBackend backend, InvalidationObservation observation )
    {
        backend.InvalidateDependency( _dependency );
        observation.OuterItemPresentAfterReturn = backend.ContainsItem( _outerKey );
    }

    /// <summary>
    /// Stores the intermediate item, which depends on <see cref="_dependency"/>, and the outer item, which depends on the
    /// key of the intermediate item.
    /// </summary>
    /// <remarks>
    /// The caching frontend creates this state when the method of the outer item gets a cache hit on the method of the
    /// intermediate item.
    /// </remarks>
    /// <param name="backend">The backend.</param>
    /// <param name="innerConfiguration">The configuration of the intermediate item, or <see langword="null"/>.</param>
    private static void SetUpChain( CachingBackend backend, CacheItemConfiguration? innerConfiguration )
    {
        backend.SetItem( _innerKey, new CacheItem( _innerValue, [_dependency], innerConfiguration ) );
        backend.SetItem( _outerKey, new CacheItem( _outerValue, [_innerKey] ) );
    }

    /// <summary>
    /// Subscribes to the <see cref="CachingBackend.ItemRemoved"/> event of a backend and records the arguments of each
    /// event.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <returns>The queue that receives the arguments of each event, in the order in which the handlers run.</returns>
    private static ConcurrentQueue<CacheItemRemovedEventArgs> RecordRemovedItems( CachingBackend backend )
    {
        var removedItems = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedItems.Enqueue( args );

        return removedItems;
    }

    /// <summary>
    /// Asserts that the arguments of an <see cref="CachingBackend.ItemRemoved"/> event have a given key and a given
    /// reason.
    /// </summary>
    /// <param name="args">The arguments of the event.</param>
    /// <param name="key">The expected key.</param>
    /// <param name="reason">The expected reason.</param>
    private static void AssertItemRemoved( CacheItemRemovedEventArgs args, string key, CacheItemRemovedReason reason )
    {
        Assert.Equal( key, args.Key );
        Assert.Equal( reason, args.RemovedReason );
    }

    /// <summary>
    /// Waits until a worker completes, and asserts that its action did not throw an exception.
    /// </summary>
    /// <param name="worker">The worker.</param>
    /// <param name="timeoutMessage">The message of the assertion that fails when the worker does not complete within the timeout.</param>
    private static void AssertCompleted( ConcurrencyTestWorker worker, string timeoutMessage )
    {
        Assert.True( worker.Join( _timeout ), timeoutMessage );

        if ( worker.Exception != null )
        {
            Assert.Fail( "The thread '" + worker.Name + "' threw an exception: " + worker.Exception );
        }
    }

    /// <summary>
    /// Determines whether an object is a <see cref="CacheItem"/> whose value equals a given value.
    /// </summary>
    /// <param name="observedValue">The object, typically the value that an <see cref="InterceptionGate"/> observed.</param>
    /// <param name="expectedValue">The expected value of the cache item.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="observedValue"/> is a <see cref="CacheItem"/> whose value equals
    /// <paramref name="expectedValue"/>, otherwise <see langword="false"/>.
    /// </returns>
    private static bool IsCacheItemWithValue( object? observedValue, string expectedValue )
        => observedValue is CacheItem item && Equals( item.Value, expectedValue );

    /// <summary>
    /// Describes which of the dependency sets and items of the chain set up by <see cref="SetUpChain"/> are in the
    /// cache.
    /// </summary>
    /// <remarks>
    /// The dependency sets are read before the items, because reading an expired item evicts it.
    /// </remarks>
    /// <param name="backend">The backend.</param>
    /// <returns>A sentence that describes the state of the chain.</returns>
    private static string DescribeChain( CachingBackend backend )
    {
        var dependencySetState = DescribePresence( backend.ContainsDependency( _dependency ) );
        var innerDependencySetState = DescribePresence( backend.ContainsDependency( _innerKey ) );
        var innerItemState = DescribePresence( backend.ContainsItem( _innerKey ) );
        var outerItemState = DescribePresence( backend.ContainsItem( _outerKey ) );

        return "the dependency set of the dependency is " + dependencySetState + ", the dependency set of the key of the intermediate item is "
               + innerDependencySetState + ", the intermediate item is " + innerItemState + ", and the outer item is " + outerItemState + ".";
    }

    /// <summary>
    /// Returns the word that describes whether an entry is in the cache.
    /// </summary>
    /// <param name="isPresent">A value indicating whether the entry is in the cache.</param>
    /// <returns><c>present</c> or <c>absent</c>.</returns>
    private static string DescribePresence( bool isPresent ) => isPresent ? "present" : "absent";

    /// <summary>
    /// The observation that a worker of an invalidation records right after the invalidation has returned.
    /// </summary>
    private sealed class InvalidationObservation
    {
        /// <summary>
        /// Gets or sets a value indicating whether the outer item was in the cache right after the invalidation returned.
        /// </summary>
        /// <remarks>
        /// The initial value is <see langword="true"/>, so that a worker that does not record the observation fails the
        /// assertion. The worker writes the value before it completes, and the test reads it after it has joined the
        /// worker, so no further synchronization is required.
        /// </remarks>
        public bool OuterItemPresentAfterReturn { get; set; } = true;
    }
}
