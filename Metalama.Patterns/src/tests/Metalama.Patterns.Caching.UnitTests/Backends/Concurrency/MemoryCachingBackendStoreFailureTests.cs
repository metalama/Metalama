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
/// Tests the state that <see cref="CachingBackend.SetItem"/> leaves in <see cref="MemoryCachingBackend"/> when the
/// <see cref="IMemoryCache"/> does not store the new value of a key that already has a value.
/// </summary>
/// <remarks>
/// <para>
/// A size-limited <see cref="MemoryCache"/> rejects an entry whose size would make the total size exceed
/// <see cref="MemoryCacheOptions.SizeLimit"/>. It then also removes the previous entry of the key. The previous value
/// is therefore removed from the cache, and the backend must handle this removal as it handles an eviction.
/// </para>
/// <para>
/// An <see cref="IMemoryCache"/> can also throw an exception when the backend stores the entry. The previous entry then
/// stays in the cache, and the backend must keep handling it as its current entry.
/// </para>
/// <para>
/// The memory cache of every test is an <see cref="InterceptingMemoryCache"/> that defers the post-eviction callbacks,
/// which a <see cref="MemoryCache"/> otherwise runs on the thread pool. The test runs them at a chosen step.
/// </para>
/// </remarks>
public sealed class MemoryCachingBackendStoreFailureTests
{
    /// <summary>
    /// The key of the item whose second value is not stored.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The key of an item that depends on <see cref="_key"/>.
    /// </summary>
    private const string _outerKey = "outer";

    /// <summary>
    /// The dependency of the first value of the item.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The dependency of the second value of the item.
    /// </summary>
    private const string _newDependency = "new-dependency";

    /// <summary>
    /// The first value of the item, which the cache stores.
    /// </summary>
    private const string _firstValue = "first-value";

    /// <summary>
    /// The second value of the item, to which the size calculator of the backend gives a size above the size limit of
    /// the cache.
    /// </summary>
    private const string _largeValue = "large-value";

    /// <summary>
    /// The size limit of the cache of the capacity tests.
    /// </summary>
    private const long _sizeLimit = 10;

    /// <summary>
    /// The message of the exception that the cache throws in the tests of a failing cache.
    /// </summary>
    private const string _simulatedFailureMessage = "Simulated failure of the memory cache.";

    /// <summary>
    /// The maximum time to wait for the post-eviction callbacks or the pending work items. It only detects a failure of
    /// the test.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendStoreFailureTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendStoreFailureTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Verifies that one <see cref="CachingBackend.ItemRemoved"/> event is raised when a size-limited
    /// <see cref="MemoryCache"/> rejects the new value of a key and removes the previous value.
    /// </summary>
    [Fact]
    public async Task SetItem_RejectedForCapacityOverPreviousValue_RaisesItemRemoved()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();
        using var cache = CreateSizeLimitedCache();
        using var backend = CreateBackend( cache, fakes );

        var removedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedEvents.Enqueue( args );

        await this.StoreRejectedValueOverPreviousValueAsync( backend, cache, fakes, cancellation.Token );

        var eventsForKey = removedEvents.Where( e => e.Key == _key ).ToList();
        this._output.WriteLine( $"ItemRemoved events for the item: [{DescribeEvents( eventsForKey )}]." );

        Assert.True(
            eventsForKey.Count == 1,
            $"Expected one ItemRemoved event for the previous value, which the cache removed, but the events were [{DescribeEvents( eventsForKey )}]." );

        Assert.Equal( CacheItemRemovedReason.Evicted, eventsForKey[0].RemovedReason );
    }

    /// <summary>
    /// Verifies that an item that depends on a key stays reachable from the dependency of the previous value of that key,
    /// when a size-limited <see cref="MemoryCache"/> rejects the new value of the key and removes the previous value.
    /// </summary>
    [Fact]
    public async Task SetItem_RejectedForCapacityOverPreviousValue_KeepsDependentItemReachable()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();
        using var cache = CreateSizeLimitedCache();
        using var backend = CreateBackend( cache, fakes );

        await this.StoreRejectedValueOverPreviousValueAsync( backend, cache, fakes, cancellation.Token );

        Assert.True( backend.ContainsItem( _outerKey ), "Precondition: the outer item is not in the cache before the invalidation." );

        backend.InvalidateDependency( _dependency );

        Assert.False(
            backend.ContainsItem( _outerKey ),
            "The invalidation of the dependency did not remove the outer item, which depends on the dependency through the key whose value the cache removed." );

        Assert.False( backend.ContainsDependency( _dependency ), "A key remained registered under the dependency after the invalidation." );
        Assert.False( backend.ContainsDependency( _key ), "The outer item remained registered under the key after the invalidation." );
    }

    /// <summary>
    /// Verifies that <see cref="CachingBackend.SetItem"/> registers no dependency of the new value, and keeps the previous
    /// value and its dependency, when the <see cref="IMemoryCache"/> throws an exception.
    /// </summary>
    [Fact]
    public void SetItem_WhenMemoryCacheThrows_KeepsPreviousValueAndRegistrations()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, fakes );

        var exception = StoreValueWithFailingCache( backend, cache );

        Assert.True(
            exception is InvalidOperationException { Message: _simulatedFailureMessage },
            $"Precondition: SetItem did not throw the simulated exception. It threw: {exception}" );

        Assert.Equal( _firstValue, backend.GetItem( _key )?.Value );
        Assert.True( backend.ContainsDependency( _dependency ), "The previous value is no longer registered under its dependency." );

        Assert.False(
            backend.ContainsDependency( _newDependency ),
            "The key is registered under the dependency of the new value, although the cache did not store that value." );
    }

    /// <summary>
    /// Verifies that the backend still handles the eviction of the previous value after the
    /// <see cref="IMemoryCache"/> has thrown an exception in <see cref="CachingBackend.SetItem"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="CachingBackend.SetItem"/> marks the previous entry as handled before it stores the new entry, so that
    /// the post-eviction callback of a replaced entry does nothing. When the cache throws, the previous entry stays in the
    /// cache, so the backend must handle its later eviction: it must unregister the key from the dependency and raise
    /// <see cref="CachingBackend.ItemRemoved"/>.
    /// </remarks>
    [Fact]
    public async Task SetItem_WhenMemoryCacheThrows_StillHandlesEvictionOfPreviousValue()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var fakes = new FakeCachingServices();
        using var cache = new InterceptingMemoryCache();
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.AnyItemKey() );

        using var backend = CreateBackend( cache, fakes );

        var removedEvents = new ConcurrentQueue<CacheItemRemovedEventArgs>();
        backend.ItemRemoved += ( _, args ) => removedEvents.Enqueue( args );

        var exception = StoreValueWithFailingCache( backend, cache );

        Assert.True(
            exception is InvalidOperationException { Message: _simulatedFailureMessage },
            $"Precondition: SetItem did not throw the simulated exception. It threw: {exception}" );

        // Evict the previous value, which is the only entry of the cache.
        cache.Compact( 1 );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Precondition: the compaction did not invoke the post-eviction callback of the previous value." );

        cache.RunCapturedCallbacks();
        await fakes.WhenPendingWorkItemsCompletedAsync( cancellation.Token );

        var eventsForKey = removedEvents.Where( e => e.Key == _key ).ToList();
        this._output.WriteLine( $"ItemRemoved events for the item: [{DescribeEvents( eventsForKey )}]." );

        Assert.False( backend.ContainsItem( _key ), "Precondition: the compaction did not evict the previous value." );

        Assert.True(
            eventsForKey.Count == 1,
            $"Expected one ItemRemoved event for the evicted previous value, but the events were [{DescribeEvents( eventsForKey )}]." );

        Assert.False(
            backend.ContainsDependency( _dependency ),
            "The key remained registered under the dependency of the previous value after that value had been evicted." );
    }

    /// <summary>
    /// Stores a first value that depends on <see cref="_dependency"/>, then calls <see cref="CachingBackend.SetItem"/>
    /// with a second value that depends on <see cref="_newDependency"/> while the cache throws an exception when the
    /// entry is stored.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="cache">The cache of the backend.</param>
    /// <returns>The exception that the second call threw, or <see langword="null"/>.</returns>
    private static Exception? StoreValueWithFailingCache( CachingBackend backend, InterceptingMemoryCache cache )
    {
        backend.SetItem( _key, new CacheItem( _firstValue, [_dependency] ) );

        cache.ArmFault(
            InterceptedOperation.CommitBefore,
            InterceptingMemoryCache.ItemKey( _key ),
            () => new InvalidOperationException( _simulatedFailureMessage ) );

        return Record.Exception( () => backend.SetItem( _key, new CacheItem( "second-value", [_newDependency] ) ) );
    }

    /// <summary>
    /// Stores a first value that depends on <see cref="_dependency"/> and an outer item that depends on the key, then
    /// calls <see cref="CachingBackend.SetItem"/> with a value that the cache rejects, runs the post-eviction callbacks,
    /// and waits for the events.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="cache">The cache of the backend, which has a size limit and defers the post-eviction callbacks.</param>
    /// <param name="fakes">The services whose work-item dispatcher delivers the events of the backend.</param>
    /// <param name="cancellationToken">A token that cancels the wait for the events.</param>
    private async Task StoreRejectedValueOverPreviousValueAsync(
        CachingBackend backend,
        InterceptingMemoryCache cache,
        FakeCachingServices fakes,
        CancellationToken cancellationToken )
    {
        backend.SetItem( _key, new CacheItem( _firstValue, [_dependency] ) );
        backend.SetItem( _outerKey, new CacheItem( "outer-value", [_key] ) );

        Assert.True(
            backend.ContainsItem( _key ) && backend.ContainsItem( _outerKey ),
            "Precondition: the cache did not store the first value and the outer item, whose sizes are below the size limit." );

        backend.SetItem( _key, new CacheItem( _largeValue, [_dependency] ) );

        Assert.False(
            backend.ContainsItem( _key ),
            "Precondition: the cache kept a value under the key, although it must reject the new value and remove the previous value." );

        // The cache invokes the callback of the rejected entry and the callback of the previous entry.
        Assert.True(
            cache.WaitUntilCallbacksCaptured( 2, _timeout ),
            "Precondition: the cache did not invoke the post-eviction callbacks of the rejected entry and of the previous entry." );

        this._output.WriteLine(
            "Captured callbacks: " + string.Join( "; ", cache.PendingCapturedCallbacks.Select( c => $"{c.Key} ({c.Reason})" ) ) );

        cache.RunCapturedCallbacks();
        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationToken );
    }

    /// <summary>
    /// Creates a size-limited <see cref="MemoryCache"/> behind an <see cref="InterceptingMemoryCache"/> that defers the
    /// post-eviction callbacks of every item.
    /// </summary>
    /// <returns>The cache.</returns>
    private static InterceptingMemoryCache CreateSizeLimitedCache()
    {
        var cache = new InterceptingMemoryCache( new MemoryCache( new MemoryCacheOptions { SizeLimit = _sizeLimit } ) );
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.AnyItemKey() );

        return cache;
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> on a given <see cref="IMemoryCache"/>. The backend
    /// gives the size 1 to every value except <see cref="_largeValue"/>, whose size exceeds <see cref="_sizeLimit"/>.
    /// </summary>
    /// <param name="memoryCache">The memory cache of the backend. The backend does not own it.</param>
    /// <param name="fakes">The services of the backend.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache memoryCache, FakeCachingServices fakes )
    {
        var configuration = new MemoryCachingBackendConfiguration
        {
            DebugName = "test", SizeCalculator = value => Equals( value, _largeValue ) ? _sizeLimit + 1 : 1
        };

        var backend = CachingBackend.Create( b => b.Memory( configuration ).WithMemoryCache( memoryCache ), fakes.ServiceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Formats <see cref="CachingBackend.ItemRemoved"/> events for a diagnostic message.
    /// </summary>
    /// <param name="events">The arguments of the events.</param>
    /// <returns>The formatted events.</returns>
    private static string DescribeEvents( IEnumerable<CacheItemRemovedEventArgs> events )
        => string.Join( "; ", events.Select( e => $"ItemRemoved({e.Key}, {e.RemovedReason})" ) );
}
