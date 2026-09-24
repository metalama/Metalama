// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that concurrent operations of <see cref="MemoryCachingBackend"/> on one key leave the dependency index consistent
/// with the cached value of that key.
/// </summary>
/// <remarks>
/// <para>
/// These tests guard against the serialization of the operations on a key by an object that belongs to the stored value.
/// Such an object does not exist while the key is absent, and a new one is created each time a value is stored under an
/// absent key. Two operations on the same key can then run at the same time, and the dependency cleanup of one value
/// removes the registration of another value of the same key, because a dependency set records the key and not the
/// value.
/// </para>
/// <para>
/// Every operation on a key now holds the lock of that key, which does not depend on the stored value. Each test pauses
/// one operation while it holds the lock of the key, with a gate of an <see cref="InterceptingMemoryCache"/> on a call
/// that the operation makes under that lock. The test then starts the competing operations on worker threads, releases
/// the gate, and waits for every worker. The competing operations cannot change the key while the paused operation
/// holds its lock, so the final state is the result of a serial order of the operations.
/// </para>
/// <para>
/// After every operation has completed, each test checks the dependency index: a cached value that declares a dependency
/// must be removed by a later invalidation of that dependency, and a dependency that no cached value declares must not
/// be registered. The check is valid for every serial order of the operations.
/// </para>
/// <para>
/// A gate encodes the order of the calls that the backend makes to the cache. When a change of the product changes this
/// order, the test fails on an assertion whose message starts with "Schedule precondition failed", and not on the
/// assertion that checks the dependency index. The timeouts only detect a failure, such as a deadlock. They never order
/// the threads.
/// </para>
/// </remarks>
public sealed class MemoryCachingBackendItemMonitorTests
{
    /// <summary>
    /// The key of the item that the tests write and remove.
    /// </summary>
    private const string _key = "k";

    /// <summary>
    /// The dependency that the values of the item declare.
    /// </summary>
    private const string _dependency = "d";

    /// <summary>
    /// A second dependency, which only some values of the item declare.
    /// </summary>
    private const string _otherDependency = "e";

    /// <summary>
    /// The name of the thread that removes the item.
    /// </summary>
    private const string _removerThreadName = "Remover";

    /// <summary>
    /// The name of the thread that stores a value when a test has a single writer thread.
    /// </summary>
    private const string _writerThreadName = "Writer";

    /// <summary>
    /// The name of the thread that stores a value first, when a test has two writer threads.
    /// </summary>
    private const string _firstWriterThreadName = "FirstWriter";

    /// <summary>
    /// The name of the thread that stores a value while the first writer is paused.
    /// </summary>
    private const string _secondWriterThreadName = "SecondWriter";

    /// <summary>
    /// The name of the thread that clears the cache and stores the item again.
    /// </summary>
    private const string _reinserterThreadName = "Reinserter";

    /// <summary>
    /// The maximum time to wait for a gate or for a worker thread. It only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// Checks that a value that <see cref="CachingBackend.SetItem"/> stores concurrently with a removal of the same key is
    /// removed by a later invalidation of its dependency.
    /// </summary>
    /// <param name="remover">
    /// The name of the operation that removes the item: <see cref="CachingBackend.InvalidateDependency"/> or
    /// <see cref="CachingBackend.RemoveItem"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// The test guards against a removal whose dependency cleanup, which follows the removal of the entry, also removes the
    /// registration of a value that a concurrent <c>SetItem</c> has stored under the same key in the meantime.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores <c>V1</c> with the dependency <c>d</c> under the key <c>k</c>.</description></item>
    /// <item><description>The remover runs the operation that <paramref name="remover"/> names. A gate pauses it after it
    /// has removed the entry of <c>k</c> and before it removes <c>k</c> from the dependency set of <c>d</c>. It holds the
    /// lock of <c>k</c>.</description></item>
    /// <item><description>The writer starts <c>SetItem</c> for <c>k</c> with the value <c>V2</c> and the dependency
    /// <c>d</c>. It needs the lock of <c>k</c>, so it cannot store its value before the remover releases that
    /// lock.</description></item>
    /// <item><description>The test releases the gate and waits for both threads.</description></item>
    /// <item><description>The test checks that <c>k</c> holds <c>V2</c>, invalidates <c>d</c>, and checks that <c>V2</c>
    /// has been removed.</description></item>
    /// </list>
    /// <para>
    /// When the remover is <see cref="CachingBackend.InvalidateDependency"/>, the invalidation releases the lock of
    /// <c>k</c> after the removal of the item, and acquires it again to remove <c>k</c> from the dependency set of the
    /// invalidated dependency. The writer can store <c>V2</c> between these two steps. The second step must then keep the
    /// registration, because <c>V2</c> declares <c>d</c>.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( nameof(CachingBackend.InvalidateDependency) )]
    [InlineData( nameof(CachingBackend.RemoveItem) )]
    public void SetItem_BetweenRemovalAndDependencyCleanup_IsRemovedByLaterInvalidation( string remover )
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        backend.SetItem( _key, new CacheItem( "V1", [_dependency] ) );

        var gates = new List<InterceptionGate>();
        var workers = new List<ConcurrencyTestWorker>();

        try
        {
            // The gate pauses the remover after the removal of the entry and before the dependency cleanup, while it holds
            // the lock of the key.
            var removalGate = cache.Arm(
                InterceptedOperation.RemoveAfter,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _removerThreadName ) );

            gates.Add( removalGate );

            workers.Add( ConcurrencyTestWorker.Start( _removerThreadName, () => RunRemover( backend, remover ) ) );

            AssertGateReached( removalGate, _removerThreadName, "the point that follows the removal of the entry of the item" );

            workers.Add( ConcurrencyTestWorker.Start( _writerThreadName, () => backend.SetItem( _key, new CacheItem( "V2", [_dependency] ) ) ) );
        }
        finally
        {
            ReleaseAndJoin( gates, workers );
        }

        AssertCompleted( workers );

        var itemAfterOperations = backend.GetItem( _key, includeDependencies: true );

        Assert.True(
            HasValue( itemAfterOperations, "V2" ),
            $"Schedule precondition failed: after both operations, the item '{_key}' holds {Describe( itemAfterOperations )} "
            + "instead of the value 'V2' of the writer." );

        var isDependencyRegistered = backend.ContainsDependency( _dependency );

        backend.InvalidateDependency( _dependency );

        var itemAfterInvalidation = backend.GetItem( _key, includeDependencies: true );

        Assert.True(
            itemAfterInvalidation is null,
            $"After the invalidation of '{_dependency}', the item '{_key}' still holds {Describe( itemAfterInvalidation )}. "
            + $"Before the invalidation, ContainsDependency('{_dependency}') returned {isDependencyRegistered}. "
            + "The dependency cleanup of the removal has removed the registration of the value that the writer stored." );
    }

    /// <summary>
    /// Checks that a value that a first-time <see cref="CachingBackend.SetItem"/> stores, concurrently with the storage and
    /// the invalidation of an intermediate value of the same key, leaves the dependency index consistent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a first-time <c>SetItem</c> that is not serialized with the other operations on the key.
    /// Such a <c>SetItem</c> registers the key in the dependency sets before it stores its value, and the cleanup of an
    /// intermediate value of the key removes this registration, whichever value made it.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The first writer runs <c>SetItem</c> for the absent key <c>k</c> with the value <c>A</c> and the
    /// dependency <c>d</c>. It registers <c>k</c> in the dependency set of <c>d</c>. A gate pauses it when it creates the
    /// cache entry of the item, before its value is stored. It holds the lock of <c>k</c>.</description></item>
    /// <item><description>The second writer starts. It stores <c>B</c> with the dependency <c>d</c> under <c>k</c>, then
    /// invalidates <c>d</c>. It needs the lock of <c>k</c> for both steps.</description></item>
    /// <item><description>The test releases the gate and waits for both writers.</description></item>
    /// <item><description>The test invalidates <c>d</c>, and checks that <c>k</c> holds no value.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public void SetItem_OfAbsentKeyAfterIntermediateInvalidation_IsRemovedByLaterInvalidation()
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        var gates = new List<InterceptionGate>();
        var workers = new List<ConcurrencyTestWorker>();

        try
        {
            // The gate pauses the first writer when it creates the entry of the item, after it has registered the key and
            // while it holds the lock of the key.
            var storeGate = cache.Arm(
                InterceptedOperation.CreateEntry,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _firstWriterThreadName ) );

            gates.Add( storeGate );

            workers.Add(
                ConcurrencyTestWorker.Start( _firstWriterThreadName, () => backend.SetItem( _key, new CacheItem( "A", [_dependency] ) ) ) );

            AssertGateReached( storeGate, _firstWriterThreadName, "the creation of the cache entry of the item" );

            workers.Add(
                ConcurrencyTestWorker.Start(
                    _secondWriterThreadName,
                    () =>
                    {
                        backend.SetItem( _key, new CacheItem( "B", [_dependency] ) );
                        backend.InvalidateDependency( _dependency );
                    } ) );
        }
        finally
        {
            ReleaseAndJoin( gates, workers );
        }

        AssertCompleted( workers );

        var isDependencyRegistered = backend.ContainsDependency( _dependency );

        backend.InvalidateDependency( _dependency );

        var itemAfterInvalidation = backend.GetItem( _key, includeDependencies: true );

        Assert.True(
            itemAfterInvalidation is null,
            $"After the invalidation of '{_dependency}', the item '{_key}' still holds {Describe( itemAfterInvalidation )}. "
            + $"Before the invalidation, ContainsDependency('{_dependency}') returned {isDependencyRegistered}. "
            + "The dependency cleanup of the intermediate value has removed the registration of the value that the first writer stored." );

        Assert.False(
            backend.ContainsDependency( _dependency ),
            $"ContainsDependency('{_dependency}') returned true after the invalidation, although no cached value declares the dependency." );
    }

    /// <summary>
    /// Checks that a <see cref="CachingBackend.RemoveItem"/> that runs concurrently with a <see cref="CachingBackend.Clear"/>
    /// and with two <see cref="CachingBackend.SetItem"/> calls on the same key leaves the value that remains cached
    /// invalidatable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a removal that is serialized by an object of the value that it has read. When that value is
    /// removed by <c>Clear</c> and the key is written again, the new value has another object, so the removal and a
    /// writer run at the same time. The removal then removes the dependency set that the writer has created for its
    /// value.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores <c>V1</c> with the dependency <c>d</c> under the key <c>k</c>.</description></item>
    /// <item><description>The remover runs <c>RemoveItem</c> for <c>k</c>. A gate pauses it after its read of the item,
    /// which returns <c>V1</c>. It holds the lock of <c>k</c>.</description></item>
    /// <item><description>The reinserter starts. It clears the cache, then stores <c>V3</c> with the dependency <c>d</c>
    /// under <c>k</c>.</description></item>
    /// <item><description>The writer starts <c>SetItem</c> for <c>k</c> with the value <c>V4</c> and the dependency
    /// <c>d</c>.</description></item>
    /// <item><description>The test releases the gate and waits for the three threads.</description></item>
    /// <item><description>The test checks that <c>d</c> is registered exactly when the cached value declares it, then
    /// invalidates <c>d</c> and checks that <c>k</c> holds no value.</description></item>
    /// </list>
    /// <para>
    /// The reinserter and the writer need the lock of <c>k</c>, so neither changes <c>k</c> before the removal has
    /// completed. Their own order is not specified. In every order, the cached value declares <c>d</c>, and the
    /// invalidation of <c>d</c> removes it.
    /// </para>
    /// </remarks>
    [Fact]
    public void RemoveItem_HoldingMonitorOfRemovedValue_LeavesConcurrentValueInvalidatable()
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        backend.SetItem( _key, new CacheItem( "V1", [_dependency] ) );

        var gates = new List<InterceptionGate>();
        var workers = new List<ConcurrencyTestWorker>();

        try
        {
            // The gate pauses the remover after its read of the item, while it holds the lock of the key.
            var readGate = cache.Arm(
                InterceptedOperation.TryGetValueAfter,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _removerThreadName ) );

            gates.Add( readGate );

            workers.Add( ConcurrencyTestWorker.Start( _removerThreadName, () => backend.RemoveItem( _key ) ) );

            AssertGateReached( readGate, _removerThreadName, "the completion of the read of the item" );

            var observedItem = readGate.ObservedValue as CacheItem;

            Assert.True(
                HasValue( observedItem, "V1" ),
                $"Schedule precondition failed: the read of the remover returned {Describe( observedItem )} instead of the value 'V1'." );

            workers.Add(
                ConcurrencyTestWorker.Start(
                    _reinserterThreadName,
                    () =>
                    {
                        backend.Clear();
                        backend.SetItem( _key, new CacheItem( "V3", [_dependency] ) );
                    } ) );

            workers.Add( ConcurrencyTestWorker.Start( _writerThreadName, () => backend.SetItem( _key, new CacheItem( "V4", [_dependency] ) ) ) );
        }
        finally
        {
            ReleaseAndJoin( gates, workers );
        }

        AssertCompleted( workers );

        var cachedItem = backend.GetItem( _key, includeDependencies: true );

        Assert.True(
            cachedItem is not null,
            $"Schedule precondition failed: the item '{_key}' holds no value after the reinserter and the writer completed." );

        AssertDependencyIndexMatches( backend, cachedItem, [_dependency] );
    }

    /// <summary>
    /// Checks that two concurrent <see cref="CachingBackend.SetItem"/> calls on one key, with different dependencies,
    /// leave registered exactly the dependencies of the value that remains cached.
    /// </summary>
    /// <param name="hasPreviousValue">
    /// <see langword="true"/> when the key holds a value before the two writers start, <see langword="false"/> when the key
    /// is absent.
    /// </param>
    /// <remarks>
    /// <para>
    /// The test guards against a <c>SetItem</c> that cleans the dependencies of the previous value that it has read, and
    /// not the dependencies of the value that it actually replaces. When another writer stores a value between the read
    /// and the replacement, the replaced value keeps its registrations.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>When <paramref name="hasPreviousValue"/> is <see langword="true"/>, the test stores <c>V0</c>
    /// with the dependency <c>d0</c> under the key <c>k</c>.</description></item>
    /// <item><description>The first writer runs <c>SetItem</c> for <c>k</c> with the value <c>V1</c> and the dependency
    /// <c>d1</c>. A gate pauses it after it has read the previous value. It holds the lock of <c>k</c>.</description></item>
    /// <item><description>The second writer starts <c>SetItem</c> for <c>k</c> with the value <c>V2</c> and the dependency
    /// <c>d2</c>.</description></item>
    /// <item><description>The test releases the gate and waits for both writers.</description></item>
    /// <item><description>The test checks that each of <c>d0</c>, <c>d1</c> and <c>d2</c> is registered exactly when the
    /// cached value declares it, then invalidates a dependency of the cached value and checks that the value and all its
    /// registrations have been removed.</description></item>
    /// </list>
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void ConcurrentReplacements_WithDifferentDependencies_LeaveNoStaleRegistration( bool hasPreviousValue )
    {
        const string initialDependency = "d0";
        const string firstWriterDependency = "d1";
        const string secondWriterDependency = "d2";

        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        if ( hasPreviousValue )
        {
            backend.SetItem( _key, new CacheItem( "V0", [initialDependency] ) );
        }

        var gates = new List<InterceptionGate>();
        var workers = new List<ConcurrencyTestWorker>();

        try
        {
            // The gate pauses the first writer after its read of the previous value, while it holds the lock of the key.
            var readGate = cache.Arm(
                InterceptedOperation.TryGetValueAfter,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _firstWriterThreadName ) );

            gates.Add( readGate );

            workers.Add(
                ConcurrencyTestWorker.Start(
                    _firstWriterThreadName,
                    () => backend.SetItem( _key, new CacheItem( "V1", [firstWriterDependency] ) ) ) );

            AssertGateReached( readGate, _firstWriterThreadName, "the read of the previous value" );

            var observedItem = readGate.ObservedValue as CacheItem;

            if ( hasPreviousValue )
            {
                Assert.True(
                    HasValue( observedItem, "V0" ),
                    $"Schedule precondition failed: the first writer read {Describe( observedItem )} instead of the value 'V0'." );
            }
            else
            {
                Assert.True(
                    observedItem is null,
                    $"Schedule precondition failed: the first writer read {Describe( observedItem )} instead of no value." );
            }

            workers.Add(
                ConcurrencyTestWorker.Start(
                    _secondWriterThreadName,
                    () => backend.SetItem( _key, new CacheItem( "V2", [secondWriterDependency] ) ) ) );
        }
        finally
        {
            ReleaseAndJoin( gates, workers );
        }

        AssertCompleted( workers );

        var cachedItem = backend.GetItem( _key, includeDependencies: true );

        Assert.True( cachedItem is not null, $"Schedule precondition failed: the item '{_key}' holds no value after both writers completed." );

        AssertDependencyIndexMatches( backend, cachedItem, [initialDependency, firstWriterDependency, secondWriterDependency] );
    }

    /// <summary>
    /// Checks that a <see cref="CachingBackend.RemoveItem"/> that runs concurrently with a <see cref="CachingBackend.Clear"/>
    /// and a <see cref="CachingBackend.SetItem"/> of the same key leaves registered exactly the dependencies of the value
    /// that remains cached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a removal that removes the entry of the key by key, after it has read the item, and then
    /// cleans the dependencies of the value that it has read. When a writer stores another value between the read and the
    /// removal, the removal removes that value but leaves its registrations.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores <c>V0</c> with the dependency <c>d</c> under the key <c>k</c>.</description></item>
    /// <item><description>The remover runs <c>RemoveItem</c> for <c>k</c>. A gate pauses it after its read of the item,
    /// which returns <c>V0</c>, and before it removes the entry of <c>k</c>. It holds the lock of <c>k</c>.</description></item>
    /// <item><description>The writer starts. It clears the cache, then stores <c>V1</c> with the dependencies <c>d</c> and
    /// <c>e</c> under <c>k</c>. It needs the lock of <c>k</c> for both steps.</description></item>
    /// <item><description>The test releases the gate and waits for both threads.</description></item>
    /// <item><description>The test checks that each of <c>d</c> and <c>e</c> is registered exactly when the cached value
    /// declares it, then invalidates a dependency of the cached value and checks that the value and all its registrations
    /// have been removed.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public void RemoveItem_ValueStoredBetweenReReadAndRemoval_LeavesNoStaleRegistration()
    {
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache );

        backend.SetItem( _key, new CacheItem( "V0", [_dependency] ) );

        var gates = new List<InterceptionGate>();
        var workers = new List<ConcurrencyTestWorker>();

        try
        {
            // The gate pauses the remover after its read of the item and before it removes the entry, while it holds the
            // lock of the key.
            var readGate = cache.Arm(
                InterceptedOperation.TryGetValueAfter,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _removerThreadName ) );

            gates.Add( readGate );

            workers.Add( ConcurrencyTestWorker.Start( _removerThreadName, () => backend.RemoveItem( _key ) ) );

            AssertGateReached( readGate, _removerThreadName, "the completion of the read of the item" );

            var observedItem = readGate.ObservedValue as CacheItem;

            Assert.True(
                HasValue( observedItem, "V0" ),
                $"Schedule precondition failed: the read of the remover returned {Describe( observedItem )} instead of the value 'V0'." );

            workers.Add(
                ConcurrencyTestWorker.Start(
                    _writerThreadName,
                    () =>
                    {
                        backend.Clear();
                        backend.SetItem( _key, new CacheItem( "V1", [_dependency, _otherDependency] ) );
                    } ) );
        }
        finally
        {
            ReleaseAndJoin( gates, workers );
        }

        AssertCompleted( workers );

        var cachedItem = backend.GetItem( _key, includeDependencies: true );

        AssertDependencyIndexMatches( backend, cachedItem, [_dependency, _otherDependency] );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> that stores its entries in <paramref name="cache"/>.
    /// </summary>
    /// <remarks>
    /// The backend does not own <paramref name="cache"/>, because the cache is passed with
    /// <see cref="MemoryCachingBackendBuilder.WithMemoryCache"/>. The test therefore disposes the cache after the backend,
    /// and the disposal of the cache releases every gate that is still armed.
    /// </remarks>
    /// <param name="cache">The cache in which the backend stores its entries.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( InterceptingMemoryCache cache )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( cache ) );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Removes the item <c>k</c> with the operation named by <paramref name="remover"/>.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="remover">
    /// <c>InvalidateDependency</c> to invalidate the dependency <c>d</c> of the item, or <c>RemoveItem</c> to remove the
    /// item by key.
    /// </param>
    private static void RunRemover( CachingBackend backend, string remover )
    {
        switch ( remover )
        {
            case nameof(CachingBackend.InvalidateDependency):
                backend.InvalidateDependency( _dependency );

                break;

            case nameof(CachingBackend.RemoveItem):
                backend.RemoveItem( _key );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(remover), remover, "The name of the remover is not supported." );
        }
    }

    /// <summary>
    /// Waits until a gate is reached, and asserts that the intended thread reached it.
    /// </summary>
    /// <param name="gate">The gate.</param>
    /// <param name="threadName">The name of the thread that must reach the gate.</param>
    /// <param name="description">A description of the call at which the gate blocks, used in the assertion messages.</param>
    private static void AssertGateReached( InterceptionGate gate, string threadName, string description )
    {
        Assert.True(
            gate.WaitUntilReached( _timeout ),
            $"Schedule precondition failed: the thread '{threadName}' did not reach {description}." );

        Assert.True(
            string.Equals( gate.TrippedThreadName, threadName, StringComparison.Ordinal ),
            $"Schedule precondition failed: {description} was reached by the thread '{gate.TrippedThreadName}' instead of the thread '{threadName}'." );
    }

    /// <summary>
    /// Releases every gate, then waits for every worker, so that no worker runs inside the backend when the test disposes
    /// the backend.
    /// </summary>
    /// <param name="gates">The gates that the test has armed.</param>
    /// <param name="workers">The workers that the test has started.</param>
    private static void ReleaseAndJoin( List<InterceptionGate> gates, List<ConcurrencyTestWorker> workers )
    {
        foreach ( var gate in gates )
        {
            gate.Release();
        }

        foreach ( var worker in workers )
        {
            _ = worker.Join( _timeout );
        }
    }

    /// <summary>
    /// Asserts that every worker has completed without an exception.
    /// </summary>
    /// <param name="workers">The workers that the test has started.</param>
    private static void AssertCompleted( List<ConcurrencyTestWorker> workers )
    {
        foreach ( var worker in workers )
        {
            Assert.True(
                worker.Join( TimeSpan.Zero ),
                $"The thread '{worker.Name}' did not complete after every gate was released. The operations may be deadlocked." );

            Assert.True( worker.Exception is null, $"The thread '{worker.Name}' threw an exception: {worker.Exception}" );
        }
    }

    /// <summary>
    /// Asserts that each candidate dependency is registered exactly when the cached item declares it. When the cached item
    /// declares a candidate dependency, the method then invalidates that dependency and asserts that the item and all its
    /// registrations have been removed.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="cachedItem">The item that the key <c>k</c> holds, or <see langword="null"/> when it holds no value.</param>
    /// <param name="candidateDependencies">The dependencies that any value of the test has declared.</param>
    private static void AssertDependencyIndexMatches( CachingBackend backend, CacheItem? cachedItem, string[] candidateDependencies )
    {
        foreach ( var dependency in candidateDependencies )
        {
            var isRegistered = backend.ContainsDependency( dependency );

            if ( Declares( cachedItem, dependency ) )
            {
                Assert.True(
                    isRegistered,
                    $"The dependency '{dependency}' is not registered, although the item '{_key}' holds {Describe( cachedItem )}." );
            }
            else
            {
                Assert.False(
                    isRegistered,
                    $"The dependency '{dependency}' is still registered, although no cached value declares it. "
                    + $"The item '{_key}' holds {Describe( cachedItem )}. The registration belongs to a value that was replaced or removed, "
                    + $"so a later invalidation of '{dependency}' removes the value that the item holds at that time, whatever its dependencies." );
            }
        }

        var declaredDependency = candidateDependencies.FirstOrDefault( d => Declares( cachedItem, d ) );

        if ( declaredDependency == null )
        {
            return;
        }

        backend.InvalidateDependency( declaredDependency );

        var itemAfterInvalidation = backend.GetItem( _key, includeDependencies: true );

        Assert.True(
            itemAfterInvalidation is null,
            $"After the invalidation of '{declaredDependency}', the item '{_key}' still holds {Describe( itemAfterInvalidation )}." );

        foreach ( var dependency in candidateDependencies )
        {
            Assert.False(
                backend.ContainsDependency( dependency ),
                $"The dependency '{dependency}' is still registered after the invalidation of '{declaredDependency}' removed the only cached value." );
        }
    }

    /// <summary>
    /// Determines whether an item holds a given string value.
    /// </summary>
    /// <param name="item">The item, or <see langword="null"/>.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <returns><see langword="true"/> when <paramref name="item"/> is not <see langword="null"/> and holds <paramref name="expectedValue"/>.</returns>
    private static bool HasValue( CacheItem? item, string expectedValue )
        => item is not null && string.Equals( item.Value as string, expectedValue, StringComparison.Ordinal );

    /// <summary>
    /// Determines whether an item declares a given dependency.
    /// </summary>
    /// <param name="item">The item, or <see langword="null"/>.</param>
    /// <param name="dependency">The dependency.</param>
    /// <returns><see langword="true"/> when <paramref name="item"/> is not <see langword="null"/> and declares <paramref name="dependency"/>.</returns>
    private static bool Declares( CacheItem? item, string dependency )
        => item is not null && !item.Dependencies.IsDefaultOrEmpty && item.Dependencies.Contains( dependency );

    /// <summary>
    /// Describes an item for an assertion message.
    /// </summary>
    /// <param name="item">The item, or <see langword="null"/>.</param>
    /// <returns>A description that can follow the verb "holds".</returns>
    private static string Describe( CacheItem? item )
    {
        if ( item is null )
        {
            return "no value";
        }

        var dependencies = item.Dependencies.IsDefaultOrEmpty ? string.Empty : string.Join( ", ", item.Dependencies );

        return $"the value '{item.Value}' with the dependencies [{dependencies}]";
    }
}
