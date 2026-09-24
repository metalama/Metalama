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
/// <see cref="InterceptingMemoryCache.RunCapturedCallbacks"/>.
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
public sealed class MemoryCachingBackendEvictionTests
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
    /// The maximum time to wait for a callback to be captured, or for a whole test. It only detects a failure, such as a
    /// deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

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
    /// Creates and initializes a memory backend that stores its entries in the given cache and that dispatches its events
    /// through the work-item dispatcher of <paramref name="fakes"/>.
    /// </summary>
    /// <param name="cache">The cache in which the backend stores its entries. The backend does not own it.</param>
    /// <param name="fakes">The services from which the backend resolves its work-item dispatcher.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache cache, FakeCachingServices fakes )
    {
        var configuration = new MemoryCachingBackendConfiguration { DebugName = "test" };
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

    /// <summary>
    /// The observations of <see cref="RunLateEvictionCallbackScheduleAsync"/> that the tests assert.
    /// </summary>
    private sealed class LateEvictionCallbackOutcome
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LateEvictionCallbackOutcome"/> class.
        /// </summary>
        /// <param name="dependencyBeforeCallback">The value of <see cref="DependencyBeforeCallback"/>.</param>
        /// <param name="eventsAfterSecondSet">The value of <see cref="EventsAfterSecondSet"/>.</param>
        /// <param name="itemAfterCallback">The value of <see cref="ItemAfterCallback"/>.</param>
        /// <param name="dependencyAfterCallback">The value of <see cref="DependencyAfterCallback"/>.</param>
        /// <param name="itemAfterInvalidation">The value of <see cref="ItemAfterInvalidation"/>.</param>
        public LateEvictionCallbackOutcome(
            bool dependencyBeforeCallback,
            IReadOnlyList<CacheItemRemovedEventArgs> eventsAfterSecondSet,
            CacheItem? itemAfterCallback,
            bool dependencyAfterCallback,
            CacheItem? itemAfterInvalidation )
        {
            this.DependencyBeforeCallback = dependencyBeforeCallback;
            this.EventsAfterSecondSet = eventsAfterSecondSet;
            this.ItemAfterCallback = itemAfterCallback;
            this.DependencyAfterCallback = dependencyAfterCallback;
            this.ItemAfterInvalidation = itemAfterInvalidation;
        }

        /// <summary>
        /// Gets a value indicating whether the dependency set existed after the second value was stored and before the
        /// late callback ran.
        /// </summary>
        public bool DependencyBeforeCallback { get; }

        /// <summary>
        /// Gets the <see cref="CachingBackend.ItemRemoved"/> events that were raised after the second value was stored and
        /// before the dependency was invalidated.
        /// </summary>
        public IReadOnlyList<CacheItemRemovedEventArgs> EventsAfterSecondSet { get; }

        /// <summary>
        /// Gets the item that <see cref="CachingBackend.GetItem"/> returned after the late callback ran.
        /// </summary>
        public CacheItem? ItemAfterCallback { get; }

        /// <summary>
        /// Gets a value indicating whether the dependency set existed after the late callback ran.
        /// </summary>
        public bool DependencyAfterCallback { get; }

        /// <summary>
        /// Gets the item that <see cref="CachingBackend.GetItem"/> returned after the dependency was invalidated.
        /// </summary>
        public CacheItem? ItemAfterInvalidation { get; }
    }
}
