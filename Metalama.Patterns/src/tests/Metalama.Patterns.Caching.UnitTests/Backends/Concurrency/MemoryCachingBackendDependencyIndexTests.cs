// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Tests.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that the dependency index of <see cref="MemoryCachingBackend"/> stays consistent with the cached items when
/// operations run concurrently, and when the post-eviction callback of an item runs late.
/// </summary>
/// <remarks>
/// <para>
/// The backend owns its dependency index. For each dependency, the index holds a dependency set, which contains the keys
/// of the items that declare the dependency. After every operation of a test has completed, the test checks the following
/// property: <see cref="CachingBackend.InvalidateDependency"/> removes every cached item that declares the dependency, and
/// <see cref="CachingBackend.ContainsDependency"/> returns <see langword="true"/> exactly when a cached item declares the
/// dependency.
/// </para>
/// <para>
/// Every operation on a key holds the lock of that key, so two operations on the same key never overlap. The concurrent
/// tests therefore race operations on different keys that share a dependency. They pause a thread between its read of a
/// dependency set and its lock of that set, at the synchronization points
/// <c>MemoryCachingBackend.AddDependency:DependencySetRead</c> and <c>MemoryCachingBackend.RemoveDependency:DependencySetRead</c>,
/// or while it holds the lock of a key, with a gate of an <see cref="InterceptingMemoryCache"/>. They run the competing
/// operation to completion on another thread, release the paused thread, and check the property. When a change of the
/// product changes the place of a pause point, the test fails on an assertion whose message starts with
/// "Schedule precondition failed", and not on the assertion that checks the property.
/// </para>
/// <para>
/// A synchronization point is armed only when the intended thread is the only thread that can reach it next. The
/// timeouts only detect a failure, such as a deadlock. They never order the threads.
/// </para>
/// </remarks>
public sealed class MemoryCachingBackendDependencyIndexTests
{
    /// <summary>
    /// The name of the synchronization point that <c>AddDependency</c> reaches after it has read a dependency set and
    /// before it locks that set.
    /// </summary>
    private const string _addDependencySetReadSyncPoint = "MemoryCachingBackend.AddDependency:DependencySetRead";

    /// <summary>
    /// The name of the synchronization point that <c>RemoveDependency</c> reaches after it has read a dependency set and
    /// before it locks that set.
    /// </summary>
    private const string _removeDependencySetReadSyncPoint = "MemoryCachingBackend.RemoveDependency:DependencySetRead";

    /// <summary>
    /// The maximum time to wait for a pause point, for a worker thread or for a post-eviction callback. It only detects a
    /// failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output to which the tests write diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendDependencyIndexTests"/> class.
    /// </summary>
    /// <param name="output">The output to which the tests write diagnostic lines.</param>
    public MemoryCachingBackendDependencyIndexTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Checks that two first registrations of different keys on a dependency that has no dependency set both remain
    /// registered when the second registration completes while the first one is between its read of the new set and its
    /// lock of that set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a creation of a missing dependency set that is not atomic, where each writer registers its
    /// key in a set of its own and the set stored last replaces the other one. The key of the other writer is then
    /// registered only in a set that no lookup reaches.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The first writer runs <c>SetItem</c> for the first key. It creates the dependency set, and the
    /// synchronization point <c>AddDependency:DependencySetRead</c> pauses it before it locks the set. It holds the lock
    /// of the first key and no lock of a dependency set.</description></item>
    /// <item><description>The second writer runs <c>SetItem</c> for the second key to completion. It does not need the
    /// lock of the first key. It registers its key in the dependency set and stores its item.</description></item>
    /// <item><description>The test releases the first writer, which registers its key and stores its
    /// item.</description></item>
    /// </list>
    /// <para>
    /// Both keys must then be registered in the dependency set that the index holds, so the invalidation of the dependency
    /// removes both items.
    /// </para>
    /// </remarks>
    [Fact]
    public void SetItem_ConcurrentFirstRegistrationsOfNewDependency_RegistersBothKeys()
    {
        const string firstWriterName = "FirstWriter";
        const string secondWriterName = "SecondWriter";
        const string firstKey = "first-item";
        const string secondKey = "second-item";
        const string firstValue = "first-value";
        const string secondValue = "second-value";
        const string dependency = "dependency";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        // No other thread runs, so only the first writer can reach the synchronization point.
        var firstWriterSyncPoint = synchronization.Arm( _addDependencySetReadSyncPoint );

        var firstWriter = ConcurrencyTestWorker.Start(
            firstWriterName,
            () => backend.SetItem( firstKey, new CacheItem( firstValue, [dependency] ) ) );

        ConcurrencyTestWorker? secondWriter = null;

        try
        {
            Assert.True(
                firstWriterSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the first writer did not read the dependency set before it registered its key." );

            secondWriter = ConcurrencyTestWorker.Start(
                secondWriterName,
                () => backend.SetItem( secondKey, new CacheItem( secondValue, [dependency] ) ) );

            Assert.True(
                secondWriter.Join( _timeout ),
                "Schedule precondition failed: the second writer did not complete while the first writer was paused before it locked the dependency set." );
        }
        finally
        {
            firstWriterSyncPoint.Release();
            JoinWorkers( firstWriter, secondWriter );
        }

        AssertCompletedWithoutException( firstWriter );
        AssertCompletedWithoutException( secondWriter );
        AssertStoredValue( backend, firstKey, firstValue );
        AssertStoredValue( backend, secondKey, secondValue );

        this.AssertDependencyIndexIsConsistent( backend, dependency, firstKey, secondKey );
    }

    /// <summary>
    /// Checks that a first registration of a key on a dependency lands in the live dependency set when, between its read
    /// of the set and its lock of that set, a concurrent refresh of another key empties the set, which removes it from the
    /// index, and then creates a new set for the same dependency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a registration that adds its key to a dependency set that has already been removed from
    /// the index. The key is then registered only in a set that no lookup reaches, and the invalidation of the dependency
    /// does not remove its item.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the refreshed key with the dependency, so the dependency set holds only this
    /// key.</description></item>
    /// <item><description>The new dependent runs <c>SetItem</c> for a new key with the same dependency. The
    /// synchronization point <c>AddDependency:DependencySetRead</c> pauses it after it has read the dependency set and
    /// before it locks the set. It holds the lock of the new key and no lock of a dependency set.</description></item>
    /// <item><description>The emptier runs <c>SetItem</c> for the refreshed key with another dependency, to completion.
    /// This removes the refreshed key from the dependency set, which becomes empty and is removed from the index. The test
    /// checks that the dependency is no longer registered.</description></item>
    /// <item><description>The refresher runs <c>SetItem</c> for the refreshed key with the dependency again, to completion.
    /// This creates a new dependency set that holds the refreshed key.</description></item>
    /// <item><description>The test releases the new dependent, which registers its key and stores its
    /// item.</description></item>
    /// </list>
    /// <para>
    /// Both keys must then be registered in the dependency set that the index holds, so the invalidation of the dependency
    /// removes both items.
    /// </para>
    /// </remarks>
    [Fact]
    public void SetItem_RefreshConcurrentWithFirstRegistrationOfAnotherKey_RegistersBothKeys()
    {
        const string newDependentName = "NewDependent";
        const string emptierName = "Emptier";
        const string refresherName = "Refresher";
        const string refreshedKey = "refreshed-item";
        const string newKey = "new-item";
        const string refreshedValue = "refreshed-value";
        const string newValue = "new-value";
        const string dependency = "dependency";
        const string otherDependency = "other-dependency";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        backend.SetItem( refreshedKey, new CacheItem( "original-value", [dependency] ) );

        // No other thread runs, so only the new dependent can reach the synchronization point.
        var newDependentSyncPoint = synchronization.Arm( _addDependencySetReadSyncPoint );

        var newDependent = ConcurrencyTestWorker.Start(
            newDependentName,
            () => backend.SetItem( newKey, new CacheItem( newValue, [dependency] ) ) );

        ConcurrencyTestWorker? emptier = null;
        ConcurrencyTestWorker? refresher = null;

        try
        {
            Assert.True(
                newDependentSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the new dependent did not read the dependency set before it registered its key." );

            emptier = ConcurrencyTestWorker.Start(
                emptierName,
                () => backend.SetItem( refreshedKey, new CacheItem( "intermediate-value", [otherDependency] ) ) );

            Assert.True(
                emptier.Join( _timeout ),
                "Schedule precondition failed: the emptier did not complete while the new dependent was paused before it locked the dependency set." );

            Assert.False(
                backend.ContainsDependency( dependency ),
                "Schedule precondition failed: the emptier did not empty the dependency set that the new dependent read." );

            refresher = ConcurrencyTestWorker.Start(
                refresherName,
                () => backend.SetItem( refreshedKey, new CacheItem( refreshedValue, [dependency] ) ) );

            Assert.True(
                refresher.Join( _timeout ),
                "Schedule precondition failed: the refresher did not complete while the new dependent was paused before it locked the dependency set." );
        }
        finally
        {
            newDependentSyncPoint.Release();
            JoinWorkers( newDependent, emptier, refresher );
        }

        AssertCompletedWithoutException( newDependent );
        AssertCompletedWithoutException( emptier );
        AssertCompletedWithoutException( refresher );
        AssertStoredValue( backend, refreshedKey, refreshedValue );
        AssertStoredValue( backend, newKey, newValue );

        this.AssertDependencyIndexIsConsistent( backend, dependency, refreshedKey, newKey );
    }

    /// <summary>
    /// Checks that the late cleanup of the dependencies of a stale previous value does not remove a newer dependency set
    /// that holds registrations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a cleanup that removes the dependency set of a dependency by its key, whatever set the index
    /// holds under that key at that time. When the set that the cleanup has read is emptied, removed and replaced by a
    /// newer set, such a cleanup removes the newer set, and the items registered in it can no longer be invalidated.
    /// </para>
    /// <para>
    /// The stale cleanup is the post-eviction callback of the previous value of the rewritten key. The callback is
    /// deferred, so it runs after the invalidation of the dependency has already removed the registration of the evicted
    /// value, and after the rewriter has stored a newer value that does not declare the dependency. The callback then
    /// unregisters the rewritten key from the dependency, although the key is no longer registered in the live set.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the original value of the rewritten key with the dependency, and evicts it with
    /// <see cref="InterceptingMemoryCache.Compact"/>. The post-eviction callback is captured and does not run, so the
    /// registration of the evicted value stays in the dependency set.</description></item>
    /// <item><description>The test invalidates the dependency, which removes the registration of the evicted value and
    /// removes the empty dependency set from the index.</description></item>
    /// <item><description>The test runs <c>SetItem</c> for the rewritten key with another dependency, then stores the last
    /// item with the dependency, which creates a new dependency set that holds only the last key.</description></item>
    /// <item><description>The stale cleaner runs the captured callback. The callback holds the lock of the rewritten key,
    /// reads the dependency set that holds the last key, and the synchronization point
    /// <c>RemoveDependency:DependencySetRead</c> pauses it before it locks the set.</description></item>
    /// <item><description>The remover runs <c>RemoveItem</c> for the last key to completion, which empties the dependency
    /// set and removes it from the index. The test checks that the dependency is no longer registered. The newer writer
    /// then runs <c>SetItem</c> for the newer key with the dependency to completion, which creates a newer dependency set
    /// that holds the newer key. Neither operation needs the lock of the rewritten key.</description></item>
    /// <item><description>The test releases the stale cleaner, which completes.</description></item>
    /// </list>
    /// <para>
    /// The newer key must then still be registered, so <see cref="CachingBackend.ContainsDependency"/> returns
    /// <see langword="true"/> and the invalidation of the dependency removes the newer item. The rewritten key must stay
    /// registered under the other dependency.
    /// </para>
    /// </remarks>
    [Fact]
    public void SetItem_CleaningStalePreviousValue_DoesNotRemoveNewerDependencySet()
    {
        const string staleCleanerName = "StaleCleaner";
        const string removerName = "Remover";
        const string newerWriterName = "NewerWriter";
        const string rewrittenKey = "rewritten-item";
        const string lastKey = "last-item";
        const string newerKey = "newer-item";
        const string rewrittenValue = "rewritten-value";
        const string newerValue = "newer-value";
        const string dependency = "dependency";
        const string otherDependency = "other-dependency";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( rewrittenKey ) );
        backend.SetItem( rewrittenKey, new CacheItem( "original-value", [dependency] ) );
        cache.Compact( 1 );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Schedule precondition failed: the eviction of the original value did not invoke its post-eviction callback." );

        Assert.True( backend.GetItem( rewrittenKey ) == null, "Schedule precondition failed: the compaction did not evict the original value." );

        Assert.True(
            backend.ContainsDependency( dependency ),
            "Schedule precondition failed: the eviction did not leave the registration of the evicted value until its post-eviction callback runs." );

        backend.InvalidateDependency( dependency );

        Assert.False(
            backend.ContainsDependency( dependency ),
            "Schedule precondition failed: the invalidation did not remove the registration of the evicted value." );

        backend.SetItem( rewrittenKey, new CacheItem( rewrittenValue, [otherDependency] ) );
        backend.SetItem( lastKey, new CacheItem( "last-value", [dependency] ) );

        // No other thread runs, and no post-eviction callback is pending apart from the captured one, so only the stale
        // cleaner can reach the synchronization point.
        var staleCleanerSyncPoint = synchronization.Arm( _removeDependencySetReadSyncPoint );
        var callbackCount = 0;

        var staleCleaner = ConcurrencyTestWorker.Start( staleCleanerName, () => callbackCount = cache.RunCapturedCallbacks() );
        ConcurrencyTestWorker? remover = null;
        ConcurrencyTestWorker? newerWriter = null;

        try
        {
            Assert.True(
                staleCleanerSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the post-eviction callback of the original value did not read the dependency set to unregister the rewritten key." );

            remover = ConcurrencyTestWorker.Start( removerName, () => backend.RemoveItem( lastKey ) );

            Assert.True(
                remover.Join( _timeout ),
                "Schedule precondition failed: RemoveItem did not complete while the stale cleaner was paused before it locked the dependency set." );

            Assert.False(
                backend.ContainsDependency( dependency ),
                "Schedule precondition failed: the removal of the last item did not empty the dependency set that the stale cleaner read." );

            newerWriter = ConcurrencyTestWorker.Start(
                newerWriterName,
                () => backend.SetItem( newerKey, new CacheItem( newerValue, [dependency] ) ) );

            Assert.True(
                newerWriter.Join( _timeout ),
                "Schedule precondition failed: SetItem did not complete while the stale cleaner was paused before it locked the dependency set." );
        }
        finally
        {
            staleCleanerSyncPoint.Release();
            JoinWorkers( staleCleaner, remover, newerWriter );
        }

        AssertCompletedWithoutException( staleCleaner );
        AssertCompletedWithoutException( remover );
        AssertCompletedWithoutException( newerWriter );
        Assert.Equal( 1, callbackCount );
        AssertStoredValue( backend, rewrittenKey, rewrittenValue );
        AssertStoredValue( backend, newerKey, newerValue );

        this.AssertDependencyIndexIsConsistent( backend, dependency, newerKey );

        // The value of the rewritten key does not declare the invalidated dependency, so the invalidation keeps it.
        AssertStoredValue( backend, rewrittenKey, rewrittenValue );
        this.AssertDependencyIndexIsConsistent( backend, otherDependency, rewrittenKey );
    }

    /// <summary>
    /// Checks that an item that <c>SetItem</c> stores concurrently with <see cref="CachingBackend.Clear"/> is either
    /// removed together with its registrations, or stays cached and registered in the dependency index, when
    /// <c>Clear</c> starts after the registration of the dependencies and before the item is stored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a <c>Clear</c> that drops the dependency index while a concurrent <c>SetItem</c> has
    /// registered its dependencies and has not stored its item yet. The item is then cached without a registration, and
    /// the invalidation of its dependency does not remove it.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The writer runs <c>SetItem</c> with a dependency. It registers its key in the dependency set, and
    /// a gate pauses it in <see cref="IMemoryCache.CreateEntry"/> for its item. It holds the lock of the
    /// key.</description></item>
    /// <item><description>The clearer starts <c>Clear</c>. The key of the writer is among the keys that <c>Clear</c>
    /// processes, so <c>Clear</c> needs the lock of that key and cannot complete before the writer
    /// does.</description></item>
    /// <item><description>The test releases the gate. The writer stores its item and releases the lock of the key. Both
    /// operations then complete.</description></item>
    /// </list>
    /// <para>
    /// The test does not depend on the order in which the two operations acquire the lock of the key. It checks the
    /// consistency of the dependency index for the final state.
    /// </para>
    /// </remarks>
    [Fact]
    public void SetItem_ConcurrentWithClearBeforeStore_RemainsInvalidatable()
    {
        const string writerName = "Writer";
        const string clearerName = "Clearer";
        const string key = "item";
        const string dependency = "dependency";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        var storeGate = cache.Arm(
            InterceptedOperation.CreateEntry,
            InterceptingMemoryCache.ItemKey( key ),
            condition: InterceptingMemoryCache.OnThread( writerName ) );

        var writer = ConcurrencyTestWorker.Start( writerName, () => backend.SetItem( key, new CacheItem( "value", [dependency] ) ) );
        ConcurrencyTestWorker? clearer = null;

        try
        {
            Assert.True(
                storeGate.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the writer did not create the cache entry of its item." );

            Assert.True(
                backend.ContainsDependency( dependency ),
                "Schedule precondition failed: the writer did not register its dependency before it stored its item." );

            clearer = ConcurrencyTestWorker.Start( clearerName, () => backend.Clear() );
        }
        finally
        {
            storeGate.Release();
            JoinWorkers( writer, clearer );
        }

        AssertCompletedWithoutException( writer );
        AssertCompletedWithoutException( clearer );

        this._output.WriteLine( $"The item is cached after both operations: {backend.GetItem( key ) != null}." );
        this.AssertDependencyIndexIsConsistent( backend, dependency, key );
    }

    /// <summary>
    /// Checks that a <c>SetItem</c> that has read a dependency set before a concurrent <see cref="CachingBackend.Clear"/>
    /// does not restore, in the dependency index, the registration of an item that <c>Clear</c> removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against a registration that stores again, or adds its key to, a dependency set that <c>Clear</c>
    /// has already emptied and removed. The key of the cleared item then stays registered, or the key of the new item is
    /// registered in a set that no lookup reaches.
    /// </para>
    /// <para>The schedule is the following.</para>
    /// <list type="number">
    /// <item><description>The test stores the cleared item with the dependency, so the dependency set holds its
    /// key.</description></item>
    /// <item><description>The writer runs <c>SetItem</c> for another key with the same dependency. The synchronization
    /// point <c>AddDependency:DependencySetRead</c> pauses it after it has read the dependency set and before it locks the
    /// set. It holds the lock of its key, which <c>Clear</c> does not process, because the writer has not stored its item
    /// yet.</description></item>
    /// <item><description>The clearer runs <c>Clear</c> to completion. The cleared item is removed, and the dependency set
    /// becomes empty and is removed from the index. The test checks that the dependency is no longer
    /// registered.</description></item>
    /// <item><description>The test releases the writer, which registers its key and stores its item.</description></item>
    /// <item><description>The test removes the item of the writer with <see cref="CachingBackend.RemoveItem"/>. No cached
    /// item then declares the dependency.</description></item>
    /// </list>
    /// <para>
    /// The test removes the item with <see cref="CachingBackend.RemoveItem"/> and not with
    /// <see cref="CachingBackend.InvalidateDependency"/>, so that the result does not depend on how the invalidation
    /// handles the keys of absent items. After the removal, <see cref="CachingBackend.ContainsDependency"/> must return
    /// <see langword="false"/>.
    /// </para>
    /// </remarks>
    [Fact]
    public void SetItem_ConcurrentWithClearAfterDependencySetRead_KeepsNoRegistrationOfClearedItem()
    {
        const string writerName = "Writer";
        const string clearerName = "Clearer";
        const string clearedKey = "cleared-item";
        const string key = "item";
        const string dependency = "dependency";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        backend.SetItem( clearedKey, new CacheItem( "cleared-value", [dependency] ) );

        // No other thread runs, so only the writer can reach the synchronization point.
        var writerSyncPoint = synchronization.Arm( _addDependencySetReadSyncPoint );

        var writer = ConcurrencyTestWorker.Start( writerName, () => backend.SetItem( key, new CacheItem( "value", [dependency] ) ) );
        ConcurrencyTestWorker? clearer = null;

        try
        {
            Assert.True(
                writerSyncPoint.WaitUntilReached( _timeout ),
                "Schedule precondition failed: the writer did not read the dependency set before it registered its key." );

            clearer = ConcurrencyTestWorker.Start( clearerName, () => backend.Clear() );

            Assert.True(
                clearer.Join( _timeout ),
                "Schedule precondition failed: Clear did not complete while the writer was paused before it locked the dependency set." );

            Assert.False(
                backend.ContainsDependency( dependency ),
                "Schedule precondition failed: Clear did not empty the dependency set that the writer read." );
        }
        finally
        {
            writerSyncPoint.Release();
            JoinWorkers( writer, clearer );
        }

        AssertCompletedWithoutException( writer );
        AssertCompletedWithoutException( clearer );

        Assert.True( backend.GetItem( clearedKey ) == null, "Clear did not remove the item that was stored before it." );

        var itemIsCached = backend.GetItem( key ) != null;
        var containsDependencyWithItem = backend.ContainsDependency( dependency );

        Assert.True(
            containsDependencyWithItem == itemIsCached,
            $"ContainsDependency(\"{dependency}\") returned {containsDependencyWithItem}, but the item that declares the dependency is cached: {itemIsCached}." );

        backend.RemoveItem( key );

        Assert.True( backend.GetItem( key ) == null, "RemoveItem did not remove the item of the writer." );

        Assert.False(
            backend.ContainsDependency( dependency ),
            $"ContainsDependency(\"{dependency}\") returned true after RemoveItem removed the last cached item that declares the dependency. "
            + "The dependency index keeps a registration that no cached item declares, such as the key of the item that Clear removed." );
    }

    /// <summary>
    /// Checks that <see cref="CachingBackend.InvalidateDependency"/> removes a registration whose item is not cached, and
    /// that the post-eviction callback of that item, when it runs afterwards, leaves the dependency index consistent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test guards against an invalidation that removes a registration only through the removal of a cached item.
    /// When the item is absent, such an invalidation keeps the registration, and nothing removes it later.
    /// </para>
    /// <para>
    /// The test creates the registration of an absent item without concurrency. It stores the item and evicts it with
    /// <see cref="InterceptingMemoryCache.Compact"/>, and the <see cref="InterceptingMemoryCache"/> captures the
    /// post-eviction callback instead of running it. Until the callback runs, the key of the evicted item stays
    /// registered in the dependency set.
    /// </para>
    /// <para>
    /// After the invalidation, <see cref="CachingBackend.ContainsDependency"/> must return <see langword="false"/>. The
    /// test then runs the captured callback, which must not register the key again, and checks that a new value of the
    /// same key is registered and can be invalidated.
    /// </para>
    /// </remarks>
    [Fact]
    public void InvalidateDependency_WithRegistrationOfAbsentItem_RemovesTheRegistration()
    {
        const string key = "item";
        const string dependency = "dependency";
        const string newValue = "new-value";

        using var synchronization = new TestSynchronizationProvider();
        using var cache = new InterceptingMemoryCache();
        using var backend = CreateBackend( cache, synchronization );

        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( key ) );
        backend.SetItem( key, new CacheItem( "value", [dependency] ) );
        cache.Compact( 1 );

        Assert.True(
            cache.WaitUntilCallbacksCaptured( 1, _timeout ),
            "Precondition failed: the eviction of the item did not invoke its post-eviction callback." );

        Assert.True( backend.GetItem( key ) == null, "Precondition failed: the compaction did not evict the item." );

        Assert.True(
            backend.ContainsDependency( dependency ),
            "Precondition failed: the eviction did not leave the registration of the evicted item until its post-eviction callback runs, so the test no longer creates the registration of an absent item." );

        backend.InvalidateDependency( dependency );

        Assert.False(
            backend.ContainsDependency( dependency ),
            $"ContainsDependency(\"{dependency}\") returned true after InvalidateDependency(\"{dependency}\"), although no cached item declares the dependency. "
            + "The invalidation kept the registration of the absent item." );

        Assert.Equal( 1, cache.RunCapturedCallbacks() );

        Assert.False(
            backend.ContainsDependency( dependency ),
            $"ContainsDependency(\"{dependency}\") returned true after the post-eviction callback of the evicted item ran, although no cached item declares the dependency." );

        backend.SetItem( key, new CacheItem( newValue, [dependency] ) );
        AssertStoredValue( backend, key, newValue );

        this.AssertDependencyIndexIsConsistent( backend, dependency, key );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> that stores its entries in a given memory cache and
    /// blocks at the synchronization points armed on a given provider.
    /// </summary>
    /// <remarks>
    /// The backend does not own <paramref name="cache"/>, because the cache is passed with
    /// <see cref="MemoryCachingBackendBuilder.WithMemoryCache"/>. The test therefore disposes the cache after the backend.
    /// </remarks>
    /// <param name="cache">The memory cache of the backend.</param>
    /// <param name="synchronization">The synchronization provider, which the backend resolves as its service provider.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache cache, TestSynchronizationProvider synchronization )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( cache ),
            synchronization );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Waits until each started worker has completed, so that no worker still runs inside the backend when the test
    /// disposes the backend.
    /// </summary>
    /// <remarks>
    /// The timeout only detects a deadlock. The test reports it afterwards through
    /// <see cref="AssertCompletedWithoutException"/>.
    /// </remarks>
    /// <param name="workers">The workers, or <see langword="null"/> for a worker that was not started.</param>
    private static void JoinWorkers( params ConcurrencyTestWorker?[] workers )
    {
        foreach ( var worker in workers )
        {
            worker?.Join( _timeout );
        }
    }

    /// <summary>
    /// Asserts that a worker was started, has completed, and threw no exception.
    /// </summary>
    /// <param name="worker">The worker.</param>
    private static void AssertCompletedWithoutException( ConcurrencyTestWorker? worker )
    {
        Assert.NotNull( worker );
        Assert.True( worker.Join( TimeSpan.Zero ), $"The worker thread {worker.Name} did not complete. The operations may be deadlocked." );
        Assert.True( worker.Exception == null, $"The worker thread {worker.Name} threw an exception: {worker.Exception}" );
    }

    /// <summary>
    /// Asserts that the backend returns a given value for a key.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="key">The key of the item.</param>
    /// <param name="expectedValue">The value that the last <c>SetItem</c> of the key stored.</param>
    private static void AssertStoredValue( CachingBackend backend, string key, string expectedValue )
    {
        var value = backend.GetItem( key )?.Value;

        Assert.True(
            Equals( value, expectedValue ),
            $"The backend does not return the value that SetItem stored for the key '{key}'. Expected: {expectedValue}. Actual: {value ?? "<absent>"}." );
    }

    /// <summary>
    /// Invalidates a dependency and asserts that the dependency index of the backend was consistent for this dependency
    /// before and after the invalidation.
    /// </summary>
    /// <remarks>
    /// The method checks that <see cref="CachingBackend.InvalidateDependency"/> removes every cached item that declares the
    /// dependency, that <see cref="CachingBackend.ContainsDependency"/> returned <see langword="true"/> before the
    /// invalidation exactly when a cached item declared the dependency, and that the dependency is no longer registered
    /// after the invalidation.
    /// </remarks>
    /// <param name="backend">The backend.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependentKeys">The keys whose last stored value declares the dependency.</param>
    private void AssertDependencyIndexIsConsistent( CachingBackend backend, string dependency, params string[] dependentKeys )
    {
        var cachedDependents = dependentKeys.Where( key => backend.GetItem( key ) != null ).ToList();
        var containsDependencyBefore = backend.ContainsDependency( dependency );

        backend.InvalidateDependency( dependency );

        var survivors = dependentKeys.Where( key => backend.GetItem( key ) != null ).ToList();
        var containsDependencyAfter = backend.ContainsDependency( dependency );

        this._output.WriteLine(
            $"Before InvalidateDependency(\"{dependency}\"): cached dependents = [{string.Join( ", ", cachedDependents )}], "
            + $"ContainsDependency = {containsDependencyBefore}. After: surviving dependents = [{string.Join( ", ", survivors )}], "
            + $"ContainsDependency = {containsDependencyAfter}." );

        Assert.True(
            survivors.Count == 0,
            $"InvalidateDependency(\"{dependency}\") did not remove the following items, although they declare this dependency: {string.Join( ", ", survivors )}." );

        var anyDependentWasCached = cachedDependents.Count > 0;

        Assert.True(
            containsDependencyBefore == anyDependentWasCached,
            $"ContainsDependency(\"{dependency}\") returned {containsDependencyBefore} before the invalidation, although the number of cached items that declared this dependency was {cachedDependents.Count}." );

        Assert.False(
            containsDependencyAfter,
            $"ContainsDependency(\"{dependency}\") returned true after the invalidation, although no cached item declares this dependency any more." );
    }
}
