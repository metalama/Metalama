// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the post-eviction callback of <see cref="MemoryCachingBackend"/>. The callback cleans the dependencies of an
/// evicted item and raises <see cref="CachingBackend.ItemRemoved"/>. It holds the lock of the key of the item while it
/// cleans the dependencies.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MemoryCache"/> removes an entry from its store before it invokes the post-eviction callbacks of the entry,
/// and it invokes them on the thread pool. The callback of the backend can therefore run after another operation on the
/// same key has completed. The tests control this timing with
/// <see cref="InterceptingMemoryCache.DeferEvictionCallbacks"/>, which captures a callback and runs it when the test calls
/// <see cref="InterceptingMemoryCache.RunCapturedCallbacks"/>. Without this deferral, <see cref="FakeMemoryCache"/> runs a
/// callback on the thread that causes the eviction, before the evicting call returns. Because the callback acquires the
/// lock of the key, the tests never let a callback run on the test thread while a worker is paused with that lock.
/// </para>
/// <para>
/// A test simulates an eviction for capacity by calling <see cref="InterceptingMemoryCache.Compact"/> directly. The
/// backend does not own a cache that the test passes to it, so <see cref="CachingBackend.Clear"/> removes its keys one
/// at a time instead of compacting the cache.
/// </para>
/// <para>
/// Each test asserts the correct behavior, so it fails while the defect that it describes is present. Before its decisive
/// assertion, each test verifies that the intended schedule has occurred. A message that starts with
/// "Schedule precondition failed" reports that the backend now calls the cache in a different order. Such a message does
/// not report the defect.
/// </para>
/// </remarks>
public sealed partial class MemoryCachingBackendEvictionTests
{
    /// <summary>
    /// The key of the item that the tests store.
    /// </summary>
    private const string _key = "k";

    /// <summary>
    /// The dependency of the item that the tests store.
    /// </summary>
    private const string _dependency = "d";

    /// <summary>
    /// The name of the thread that removes the item while its entry is evicted.
    /// </summary>
    private const string _removerThreadName = "Remover";

    /// <summary>
    /// The maximum number of times that <see cref="CapacityRejection_WithAutoReload_DoesNotReloadRepeatedly"/> runs the
    /// captured post-eviction callbacks.
    /// </summary>
    private const int _maximumReloadRounds = 3;

    /// <summary>
    /// The maximum time to wait for a thread to reach a gate, for a thread to complete, for a callback to be captured, or
    /// for a whole test. It only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The instant at which the fake clock starts in the tests that advance it.
    /// </summary>
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    /// <summary>
    /// The output of the current test, which receives the observations that help to diagnose a failure.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendEvictionTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendEvictionTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Verifies that the post-eviction callback of an old value does not remove the registration of a new value that was
    /// stored under the same key, with the same dependency, before the callback ran. The invalidation of the dependency
    /// must then remove the new value.
    /// </summary>
    /// <remarks>
    /// The schedule is described in <see cref="RunLateEvictionCallbackScheduleAsync"/>. The test guards against a late
    /// callback that removes the key from the dependency set without checking whether a newer value of the key declares
    /// the same dependency. The invalidation of the dependency then finds no registration, and the new value stays in the
    /// cache.
    /// </remarks>
    [Fact]
    public async Task EvictionCallback_RunningAfterSetItemOfSameKey_KeepsRegistrationOfNewValue()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        var outcome = await this.RunLateEvictionCallbackScheduleAsync( cancellation.Token );

        var newValueRemoved = outcome.ItemAfterInvalidation == null;

        Assert.True(
            newValueRemoved,
            "InvalidateDependency did not remove the value that was stored after the eviction of the previous value, although "
            + "that value depends on the invalidated dependency. "
            + $"GetItem after the invalidation: {DescribeItem( outcome.ItemAfterInvalidation )}. "
            + $"ContainsDependency before the late callback: {outcome.DependencyBeforeCallback}. "
            + $"ContainsDependency after the late callback: {outcome.DependencyAfterCallback}." );
    }

    /// <summary>
    /// Verifies that the post-eviction callback of an old value does not raise <see cref="CachingBackend.ItemRemoved"/>
    /// for the key after a new value has been stored under that key.
    /// </summary>
    /// <remarks>
    /// The schedule is described in <see cref="RunLateEvictionCallbackScheduleAsync"/>. The test counts the events that
    /// are raised after <see cref="CachingBackend.SetItem"/> has stored the new value and before the invalidation of the
    /// dependency, so that a correct invalidation of the new value is not counted. A listener that receives the event,
    /// such as the automatic reload of the caching service, executes the cached method again although the new value is in
    /// the cache.
    /// </remarks>
    [Fact]
    public async Task EvictionCallback_RunningAfterSetItemOfSameKey_RaisesNoItemRemoved()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        var outcome = await this.RunLateEvictionCallbackScheduleAsync( cancellation.Token );

        var eventsForKey = outcome.EventsAfterSecondSet.Where( e => e.Key == _key ).ToList();
        var noEventRaised = eventsForKey.Count == 0;

        Assert.True(
            noEventRaised,
            "The post-eviction callback of the previous value raised ItemRemoved after the new value had been stored: "
            + $"[{DescribeEvents( eventsForKey )}]. GetItem after the callback: {DescribeItem( outcome.ItemAfterCallback )}." );
    }

    /// <summary>
    /// Verifies that one <see cref="CachingBackend.ItemRemoved"/> event is raised when the entry of an item expires while
    /// <see cref="CachingBackend.RemoveItem"/> is removing the item.
    /// </summary>
    /// <remarks>
    /// The schedule is described in <see cref="RunRemovalConcurrentWithEvictionAsync"/>. The test thread advances the fake
    /// clock past the absolute expiration of the item, so the timer of <see cref="FakeMemoryCache"/> evicts the entry with
    /// <see cref="EvictionReason.Expired"/>.
    /// </remarks>
    [Fact]
    public async Task RemoveItem_ConcurrentWithExpiration_RaisesOneItemRemoved()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        await this.RunRemovalConcurrentWithEvictionAsync(
            ( fakes, _ ) => fakes.TimeProvider.Advance( TimeSpan.FromMinutes( 6 ) ),
            "expiration",
            EvictionReason.Expired,
            cancellation.Token );
    }

    /// <summary>
    /// Verifies that one <see cref="CachingBackend.ItemRemoved"/> event is raised when the entry of an item is compacted
    /// while <see cref="CachingBackend.RemoveItem"/> is removing the item.
    /// </summary>
    /// <remarks>
    /// The schedule is described in <see cref="RunRemovalConcurrentWithEvictionAsync"/>. The test thread calls
    /// <see cref="InterceptingMemoryCache.Compact"/> with the percentage 1, so <see cref="FakeMemoryCache.Compact"/>
    /// evicts the entry, which is the only entry of the cache, with <see cref="EvictionReason.Capacity"/>.
    /// </remarks>
    [Fact]
    public async Task RemoveItem_ConcurrentWithCompaction_RaisesOneItemRemoved()
    {
        using var cancellation = new CancellationTokenSource( _timeout );

        await this.RunRemovalConcurrentWithEvictionAsync(
            ( _, cache ) => cache.Compact( 1 ),
            "compaction",
            EvictionReason.Capacity,
            cancellation.Token );
    }

    /// <summary>
    /// Verifies that <see cref="CachingBackend.SetItem"/> causes no <see cref="CachingBackend.ItemRemoved"/> event when a
    /// size-limited <see cref="MemoryCache"/> rejects the new entry, because the value was never stored.
    /// </summary>
    /// <remarks>
    /// <see cref="MemoryCache"/> rejects an entry when its size would make the total size exceed
    /// <see cref="MemoryCacheOptions.SizeLimit"/>. It marks the entry with <see cref="EvictionReason.Capacity"/> and
    /// invokes the post-eviction callbacks of the entry. While the defect is present, the callback of the backend maps this
    /// reason to <see cref="CacheItemRemovedReason.Evicted"/> and raises <see cref="CachingBackend.ItemRemoved"/> for a
    /// value that no reader could observe. The automatic reload of the caching service reacts to this event, as
    /// <see cref="CapacityRejection_WithAutoReload_DoesNotReloadRepeatedly"/> shows.
    /// </remarks>
    [Fact]
    public async Task SetItem_EntryRejectedForCapacity_RaisesNoItemRemoved()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();

        // The size limit of the cache is 1, and the backend gives every item the size 2, so the cache rejects every item.
        // The backend does not own the cache. The cache is declared before the backend, so it is disposed after it.
        using var cache = new InterceptingMemoryCache( new MemoryCache( new MemoryCacheOptions { SizeLimit = 1 } ) );
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( _key ) );

        using var backend = CreateBackend( cache, fakes, itemSize: 2 );

        var removedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedEvents.Enqueue( args );

        backend.SetItem( _key, new CacheItem( "V1" ) );

        Assert.False(
            backend.ContainsItem( _key ),
            "Schedule precondition failed: the cache stored an item whose size exceeds the size limit of the cache." );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Schedule precondition failed: the rejection of the entry did not invoke its post-eviction callback." );

        var pendingCallbacks = cache.PendingCapturedCallbacks;
        var capacityCallbackCaptured = pendingCallbacks.Count == 1 && pendingCallbacks[0].Reason == EvictionReason.Capacity;

        Assert.True(
            capacityCallbackCaptured,
            "Schedule precondition failed: expected one captured callback with the reason Capacity, but the captured callbacks "
            + $"were [{DescribeCallbacks( pendingCallbacks )}]." );

        cache.RunCapturedCallbacks();
        await WaitForPendingWorkItemsAsync( fakes, "after the post-eviction callback of the rejected entry", cancellation.Token );

        var eventsForKey = removedEvents.Where( e => e.Key == _key ).ToList();
        this._output.WriteLine( $"ItemRemoved events for the item: [{DescribeEvents( eventsForKey )}]." );

        var noEventRaised = eventsForKey.Count == 0;

        Assert.True(
            noEventRaised,
            $"ItemRemoved was raised for a value that the cache rejected and never stored: [{DescribeEvents( eventsForKey )}]." );
    }

    /// <summary>
    /// Verifies that the caching service executes a cached method only once when the size-limited
    /// <see cref="MemoryCache"/> of its backend rejects the value of the method, although the automatic reload is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The profile of the caching service enables the automatic reload, which executes the cached method again when the
    /// backend raises <see cref="CachingBackend.ItemRemoved"/> for its key. The cache rejects every value of the method.
    /// While the backend raises this event for a rejected entry, each reload stores a value that the cache rejects again,
    /// so the method is executed again after each rejection, with no limit.
    /// </para>
    /// <para>
    /// The post-eviction callbacks of the rejected entries are captured, so each step of the reload waits until the test
    /// runs the captured callbacks. The test runs them at most <see cref="_maximumReloadRounds"/> times, and it waits for
    /// the work items of the backend and of the caching service after each round. The work items include the delivery of
    /// the events and the reloads. The number of commits of the item gives the number of callbacks to wait for, because
    /// the cache rejects every commit and every rejection invokes one callback.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task CapacityRejection_WithAutoReload_DoesNotReloadRepeatedly()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();

        // The size limit of the cache is 1, and the backend gives every item the size 2, so the cache rejects every item.
        // The backend does not own the cache. The cache is declared before the backend, so it is disposed after it.
        using var cache = new InterceptingMemoryCache( new MemoryCache( new MemoryCacheOptions { SizeLimit = 1 } ) );
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.AnyItemKey() );

        using var backend = CreateBackend( cache, fakes, itemSize: 2 );

        // The caching service does not own the backend. It is declared after the backend, so it is disposed first.
        using var cachingService = CachingService.Create(
            b =>
            {
                b.WithBackend( backend );
                b.AddProfile( new CachingProfile { AutoReload = true } );
            },
            fakes.ServiceProvider );

        var method =
            typeof(MemoryCachingBackendEvictionTests).GetMethod( nameof(ComputeReloadedValue), BindingFlags.NonPublic | BindingFlags.Static )
            ?? throw new MissingMethodException( nameof(MemoryCachingBackendEvictionTests), nameof(ComputeReloadedValue) );

        var metadata = CachedMethodMetadata.Register( method, throwIfAlreadyRegistered: false );
        var invocationCount = 0;

        cachingService.GetFromCacheOrExecute<int>(
            metadata,
            null,
            Array.Empty<object?>(),
            ( _, _ ) =>
            {
                Interlocked.Increment( ref invocationCount );

                return ComputeReloadedValue();
            } );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Schedule precondition failed: the rejection of the value of the cached method did not invoke its post-eviction callback." );

        var pendingCallbacks = cache.PendingCapturedCallbacks;
        var capacityCallbackCaptured = pendingCallbacks.Count == 1 && pendingCallbacks[0].Reason == EvictionReason.Capacity;

        Assert.True(
            capacityCallbackCaptured,
            "Schedule precondition failed: expected one captured callback with the reason Capacity, but the captured callbacks "
            + $"were [{DescribeCallbacks( pendingCallbacks )}]." );

        var rounds = 0;

        while ( rounds < _maximumReloadRounds )
        {
            // The cache rejects every commit of the item, and every rejection invokes one post-eviction callback.
            var commitCount = cache.GetCallCount( InterceptedOperation.CommitAfter, InterceptingMemoryCache.AnyItemKey() );

            Assert.True(
                cache.WaitUntilCallbacksCaptured( commitCount, _timeout ),
                $"Schedule precondition failed: the item was committed {commitCount} times, but fewer post-eviction callbacks were captured." );

            if ( cache.RunCapturedCallbacks() == 0 )
            {
                // No rejected entry is pending, so no further reload can start.
                break;
            }

            rounds++;

            await WaitForPendingWorkItemsAsync( fakes, $"after round {rounds} of post-eviction callbacks", cancellation.Token );
        }

        var finalInvocationCount = Volatile.Read( ref invocationCount );
        this._output.WriteLine( $"The cached method ran {finalInvocationCount} times, and {rounds} rounds of post-eviction callbacks ran." );

        var executedOnce = finalInvocationCount == 1;

        Assert.True(
            executedOnce,
            $"The cached method ran {finalInvocationCount} times instead of once. After each rejection, the backend raised ItemRemoved, "
            + "and the automatic reload executed the method again and stored a value that the cache rejected again." );
    }

    /// <summary>
    /// Stores a value, evicts it while its post-eviction callback is captured, stores a new value under the same key with
    /// the same dependency, runs the captured callback, and invalidates the dependency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The backend runs over an <see cref="InterceptingMemoryCache"/> that wraps a <see cref="MemoryCache"/>. Every step
    /// runs on the test thread, in the following order.
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>SetItem(k, V1)</c> with the dependency <c>d</c>. The dependency set of <c>d</c>, which the backend owns,
    /// contains <c>k</c>.
    /// </description></item>
    /// <item><description>
    /// <see cref="InterceptingMemoryCache.Compact"/> with the percentage 1. <see cref="MemoryCache.Compact"/> removes
    /// <c>V1</c> with <see cref="EvictionReason.Capacity"/>. <see cref="MemoryCache"/> queues the post-eviction callback of
    /// <c>V1</c> to the thread pool, where the wrapper captures it. Until the callback runs, <c>k</c> stays registered in
    /// the dependency set of <c>d</c>.
    /// </description></item>
    /// <item><description>
    /// <c>SetItem(k, V2)</c> with the dependency <c>d</c>. The backend reads no previous value. The key is already in the
    /// dependency set when the backend registers it.
    /// </description></item>
    /// <item><description>
    /// The test runs the captured callback of <c>V1</c>, and it waits for the events that the callback raises.
    /// </description></item>
    /// <item><description>
    /// <c>InvalidateDependency(d)</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">A token that is signaled when the test times out.</param>
    /// <returns>The observations of the schedule that the tests assert.</returns>
    private async Task<LateEvictionCallbackOutcome> RunLateEvictionCallbackScheduleAsync( CancellationToken cancellationToken )
    {
        using var fakes = new FakeCachingServices();
        // The backend does not own the cache. The cache is declared before the backend, so it is disposed after it.
        using var cache = new InterceptingMemoryCache();

        using var backend = CreateBackend( cache, fakes );

        var removedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedEvents.Enqueue( args );

        // The post-eviction callbacks of the entries of the item that are stored from now on are captured.
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( _key ) );

        backend.SetItem( _key, new CacheItem( "V1", ImmutableArray.Create( _dependency ) ) );

        var firstValueStored = backend.GetItem( _key ) is { Value: "V1" };

        Assert.True( firstValueStored, "Schedule precondition failed: SetItem did not store the first value." );
        Assert.True( backend.ContainsDependency( _dependency ), "Schedule precondition failed: SetItem did not create the dependency set." );

        // The backend does not own the cache, so Clear would remove the key itself instead of compacting the cache. The
        // test compacts the cache directly to simulate an eviction for capacity.
        cache.Compact( 1 );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Schedule precondition failed: the post-eviction callback of the first value was not captured after the compaction." );

        var pendingCallbacks = cache.PendingCapturedCallbacks;

        var capacityCallbackCaptured = pendingCallbacks.Count == 1
                                       && InterceptingMemoryCache.ItemKey( _key )( pendingCallbacks[0].Key )
                                       && pendingCallbacks[0].Reason == EvictionReason.Capacity;

        Assert.True(
            capacityCallbackCaptured,
            "Schedule precondition failed: expected one captured callback for the item, with the reason Capacity, but the captured "
            + $"callbacks were [{DescribeCallbacks( pendingCallbacks )}]." );

        Assert.False( backend.ContainsItem( _key ), "Schedule precondition failed: the compaction did not remove the first value." );

        Assert.True(
            backend.ContainsDependency( _dependency ),
            "Schedule precondition failed: the registration of the first value was removed before its post-eviction callback ran." );

        // No work item is pending after this wait. The events recorded after the count below are therefore raised by the
        // operations that follow.
        await WaitForPendingWorkItemsAsync( fakes, "before the second SetItem", cancellationToken );
        var eventCountBeforeSecondSet = removedEvents.Count;

        backend.SetItem( _key, new CacheItem( "V2", ImmutableArray.Create( _dependency ) ) );

        var secondValueStored = backend.GetItem( _key ) is { Value: "V2" };
        Assert.True( secondValueStored, "Schedule precondition failed: SetItem did not store the second value." );

        var dependencyBeforeCallback = backend.ContainsDependency( _dependency );
        Assert.True( dependencyBeforeCallback, "Schedule precondition failed: the dependency set was absent after the second SetItem." );

        var callbacksRun = cache.RunCapturedCallbacks();
        await WaitForPendingWorkItemsAsync( fakes, "after the late post-eviction callback", cancellationToken );

        var oneCallbackRun = callbacksRun == 1;
        Assert.True( oneCallbackRun, $"Schedule precondition failed: expected to run one captured callback, but ran {callbacksRun}." );

        var eventsAfterSecondSet = removedEvents.Skip( eventCountBeforeSecondSet ).ToList();
        var itemAfterCallback = backend.GetItem( _key );
        var dependencyAfterCallback = backend.ContainsDependency( _dependency );

        backend.InvalidateDependency( _dependency );
        await WaitForPendingWorkItemsAsync( fakes, "after InvalidateDependency", cancellationToken );

        var itemAfterInvalidation = backend.GetItem( _key );

        this._output.WriteLine(
            $"After the late callback: GetItem = {DescribeItem( itemAfterCallback )}, ContainsDependency = {dependencyAfterCallback}, "
            + $"ItemRemoved events = [{DescribeEvents( eventsAfterSecondSet )}]." );

        this._output.WriteLine( $"After InvalidateDependency: GetItem = {DescribeItem( itemAfterInvalidation )}." );

        return new LateEvictionCallbackOutcome(
            dependencyBeforeCallback,
            eventsAfterSecondSet,
            itemAfterCallback,
            dependencyAfterCallback,
            itemAfterInvalidation );
    }

    /// <summary>
    /// Removes an item on a worker thread, evicts the entry of the item while the worker holds the lock of the key between
    /// its read of the item and its call to <see cref="IMemoryCache.Remove"/>, and asserts that one
    /// <see cref="CachingBackend.ItemRemoved"/> event is raised for the item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The backend runs over an <see cref="InterceptingMemoryCache"/> that wraps the <see cref="FakeMemoryCache"/> of a
    /// <see cref="FakeCachingServices"/>. The post-eviction callbacks of the item are deferred, because the callback
    /// acquires the lock of the key, which the paused worker holds. The test waits for the events through the work-item
    /// dispatcher of the fakes.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the item with a dependency and an absolute expiration.</description></item>
    /// <item><description>The worker calls <see cref="CachingBackend.RemoveItem"/>. It acquires the lock of the key and
    /// reads the item once. A gate pauses it after this read has returned the stored value.</description></item>
    /// <item><description>The test thread evicts the entry. The cache removes the entry from its store and invokes the
    /// post-eviction callback, which the wrapper captures.</description></item>
    /// <item><description>The test releases the worker and waits until it completes. The worker finds no entry to remove
    /// in the cache, but it has read a value, so it detaches that value, unregisters its dependency and raises
    /// <see cref="CachingBackend.ItemRemoved"/> with the reason <see cref="CacheItemRemovedReason.Removed"/>.</description></item>
    /// <item><description>The test runs the captured callback. It finds the evicted value detached, so it must do
    /// nothing.</description></item>
    /// </list>
    /// <para>
    /// The test guards against a removal and a post-eviction callback that both report the removal of the same value,
    /// which raises two events for one removal.
    /// </para>
    /// </remarks>
    /// <param name="evict">An action that evicts the entry of the item on the test thread.</param>
    /// <param name="eviction">The name of the eviction, which the messages include.</param>
    /// <param name="expectedReason">The reason with which the cache is expected to evict the entry.</param>
    /// <param name="cancellationToken">A token that is signaled when the test times out.</param>
    private async Task RunRemovalConcurrentWithEvictionAsync(
        Action<FakeCachingServices, InterceptingMemoryCache> evict,
        string eviction,
        EvictionReason expectedReason,
        CancellationToken cancellationToken )
    {
        using var fakes = new FakeCachingServices( _origin );

        // The fakes dispose the inner cache. The backend is declared after the fakes, so it is disposed before them.
        var cache = new InterceptingMemoryCache( fakes.MemoryCache );
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( _key ) );

        using var backend = CreateBackend( cache, fakes );

        var removedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedEvents.Enqueue( args );

        var configuration = new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 5 ) };
        backend.SetItem( _key, new CacheItem( "V0", ImmutableArray.Create( _dependency ), configuration ) );

        // The removal reads the item once, while it holds the lock of the key, before it calls Remove.
        var itemRead = cache.Arm(
            InterceptedOperation.TryGetValueAfter,
            InterceptingMemoryCache.ItemKey( _key ),
            condition: InterceptingMemoryCache.OnThread( _removerThreadName ) );

        var remover = ConcurrencyTestWorker.Start( _removerThreadName, () => backend.RemoveItem( _key ) );

        try
        {
            Assert.True(
                itemRead.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the removal thread did not read the item." );

            var readReturnedStoredValue = itemRead.ObservedValue is CacheItem { Value: "V0" };

            Assert.True(
                readReturnedStoredValue,
                "Schedule precondition failed: the read of the removal thread did not return the stored value, so the removal "
                + "does not take the path that removes a value." );

            // The post-eviction callback is deferred, so the eviction does not wait for the lock of the key, which the
            // removal thread holds.
            evict( fakes, cache );

            Assert.True(
                cache.WaitUntilCallbacksCaptured( 1, _timeout ),
                $"Schedule precondition failed: the {eviction} did not invoke the post-eviction callback of the item." );

            var pendingCallbacks = cache.PendingCapturedCallbacks;
            var expectedCallbackCaptured = pendingCallbacks.Count == 1 && pendingCallbacks[0].Reason == expectedReason;

            Assert.True(
                expectedCallbackCaptured,
                $"Schedule precondition failed: expected one captured callback with the reason {expectedReason}, but the captured "
                + $"callbacks were [{DescribeCallbacks( pendingCallbacks )}]." );

            Assert.False(
                backend.ContainsItem( _key ),
                $"Schedule precondition failed: the {eviction} did not remove the entry while the removal thread was paused." );

            itemRead.Release();

            Assert.True( remover.Join( _timeout ), "The removal thread did not complete." );
            Assert.Null( remover.Exception );
        }
        finally
        {
            // No worker may remain inside the backend when the backend and the cache are disposed.
            itemRead.Release();
            remover.Join( _timeout );
        }

        // The removal thread has completed and holds no lock, so the callback can run on the test thread.
        var callbacksRun = cache.RunCapturedCallbacks();

        Assert.True(
            callbacksRun == 1,
            $"Schedule precondition failed: expected to run one captured callback, but ran {callbacksRun}." );

        await WaitForPendingWorkItemsAsync( fakes, "after the removal and the post-eviction callback", cancellationToken );

        var eventsForKey = removedEvents.Where( e => e.Key == _key ).ToList();
        this._output.WriteLine( $"ItemRemoved events for the item: [{DescribeEvents( eventsForKey )}]." );

        var oneEventRaised = eventsForKey.Count == 1;

        Assert.True(
            oneEventRaised,
            $"The item was removed once, but {eventsForKey.Count} ItemRemoved events were raised for it: [{DescribeEvents( eventsForKey )}]." );

        Assert.Equal( CacheItemRemovedReason.Removed, eventsForKey[0].RemovedReason );
        Assert.Null( backend.GetItem( _key ) );
        Assert.False( backend.ContainsDependency( _dependency ), "The dependency remained registered after the removal of its only item." );
    }

    /// <summary>
    /// Creates and initializes a memory backend that stores its entries in the given cache and that dispatches its events
    /// through the work-item dispatcher of <paramref name="fakes"/>.
    /// </summary>
    /// <param name="cache">The cache in which the backend stores its entries. The backend does not own it.</param>
    /// <param name="fakes">The services from which the backend resolves its work-item dispatcher.</param>
    /// <param name="itemSize">The size that the backend gives to every item.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache cache, FakeCachingServices fakes, long itemSize = 1 )
    {
        var configuration = new MemoryCachingBackendConfiguration { DebugName = "test", SizeCalculator = _ => itemSize };
        var backend = CachingBackend.Create( b => b.Memory( configuration ).WithMemoryCache( cache ), fakes.ServiceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Waits until every work item that has been dispatched through the work-item dispatcher of
    /// <paramref name="fakes"/> has completed. The work items include the delivery of the events of the backend.
    /// </summary>
    /// <param name="fakes">The services whose work-item dispatcher the backend uses.</param>
    /// <param name="step">The description of the step, which the timeout message includes.</param>
    /// <param name="cancellationToken">A token that is signaled when the test times out.</param>
    private static async Task WaitForPendingWorkItemsAsync( FakeCachingServices fakes, string step, CancellationToken cancellationToken )
    {
        try
        {
            await fakes.WhenPendingWorkItemsCompletedAsync( cancellationToken );
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
        {
            throw new TimeoutException( $"The dispatched work items did not complete before the test timed out ({step})." );
        }
    }

    /// <summary>
    /// Computes the value of the cached method of <see cref="CapacityRejection_WithAutoReload_DoesNotReloadRepeatedly"/>.
    /// The <see cref="MethodInfo"/> of this method identifies the cached method in the cache key.
    /// </summary>
    /// <returns>A constant value.</returns>
    private static int ComputeReloadedValue() => 42;

    /// <summary>
    /// Formats <see cref="CachingBackend.ItemRemoved"/> events for a diagnostic message.
    /// </summary>
    /// <param name="events">The arguments of the events.</param>
    /// <returns>The formatted events.</returns>
    private static string DescribeEvents( IEnumerable<CacheItemRemovedEventArgs> events )
        => string.Join( "; ", events.Select( e => $"ItemRemoved({e.Key}, {e.RemovedReason})" ) );

    /// <summary>
    /// Formats the post-eviction callbacks captured by an <see cref="InterceptingMemoryCache"/> for a diagnostic message.
    /// </summary>
    /// <param name="callbacks">The cache key and the eviction reason of each callback.</param>
    /// <returns>The formatted callbacks.</returns>
    private static string DescribeCallbacks( IEnumerable<(string Key, EvictionReason Reason)> callbacks )
        => string.Join( "; ", callbacks.Select( c => $"{c.Key} ({c.Reason})" ) );

    /// <summary>
    /// Formats a cache item for a diagnostic message.
    /// </summary>
    /// <param name="item">The cache item, or <see langword="null"/>.</param>
    /// <returns>The formatted item.</returns>
    private static string DescribeItem( CacheItem? item ) => item == null ? "null" : $"Value={item.Value ?? "null"}";
}
