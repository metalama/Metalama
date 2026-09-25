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
/// items an invalidation removes when it runs concurrently with <see cref="CachingBackend.SetItem"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MemoryCachingBackend"/> keeps, for each dependency, a set of the keys of the items that depend on it. An
/// invalidation copies this set while it holds the lock of the set, and releases that lock. It then processes each copied
/// key under the lock of the key: it removes the item when its current value still declares the invalidated dependency.
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
/// <see cref="FakeCachingServices"/> supplies the work-item dispatcher through which the backend raises its events,
/// and the synchronization provider of the backend.
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
    /// Returns the word that describes whether an entry is in the cache.
    /// </summary>
    /// <param name="isPresent">A value indicating whether the entry is in the cache.</param>
    /// <returns><c>present</c> or <c>absent</c>.</returns>
    private static string DescribePresence( bool isPresent ) => isPresent ? "present" : "absent";
}
