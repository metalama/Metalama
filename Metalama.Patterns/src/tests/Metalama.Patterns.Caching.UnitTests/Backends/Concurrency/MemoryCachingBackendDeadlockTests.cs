// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Tests.Implementation;
using System.Collections.Immutable;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that concurrent operations of <see cref="MemoryCachingBackend"/> that use the lock of a key and the lock of a
/// dependency set complete instead of deadlocking.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MemoryCachingBackend"/> uses two kinds of locks. Every operation that changes the item of a key, or the
/// registrations of that key in the dependency index, holds the lock of that key. The lock of a dependency set is held
/// only while one set is changed or copied. An operation that holds the lock of a key acquires the lock of a dependency
/// set when it registers or unregisters that key, so the lock of a dependency set must be the last lock that a thread
/// acquires: no thread may acquire the lock of a key, or the lock of another dependency set, while it holds the lock of
/// a dependency set. These tests guard that invariant.
/// </para>
/// <para>
/// Before the fix of metalama/Metalama#2066, <see cref="MemoryCachingBackend.InvalidateDependencyImpl"/> held the lock
/// of a dependency set while it removed the items of that set. It therefore acquired the lock of an item while it held
/// the lock of a set, and it deadlocked with a thread that held the lock of that item and requested the lock of that
/// set.
/// </para>
/// <para>
/// Each test pauses one thread while it holds the lock of a key, and pauses another thread while it holds the lock of a
/// dependency set, at the synchronization point <c>MemoryCachingBackend.InvalidateDependencyImpl:DependencyLocked</c>.
/// The next step of the first thread requests the lock of that dependency set, and the next step of the second thread
/// requests the lock of that key. The test then releases both threads and asserts that both complete, that the final
/// state is consistent, and, where the outcome is determined by the schedule, which events the backend raises. The
/// timeout of the completion only detects a deadlock. It does not order the threads.
/// </para>
/// <para>
/// A thread is paused while it holds the lock of a key either at the synchronization point
/// <c>MemoryCachingBackend.RemoveItemImpl:ItemLocked</c> or with a gate of an <see cref="InterceptingMemoryCache"/> on
/// its read of the item under that lock. Each gate is restricted to one named thread, and each synchronization point is
/// armed when only the intended thread can reach it next. The test waits until each thread has reached its pause
/// point, so that a change of the order of the calls of the backend is reported as a failed schedule precondition and
/// not as a deadlock.
/// </para>
/// </remarks>
public sealed partial class MemoryCachingBackendDeadlockTests
{
    /// <summary>
    /// The name of the thread that removes an item.
    /// </summary>
    private const string _removerThreadName = "Remover";

    /// <summary>
    /// The name of the thread that invalidates a dependency.
    /// </summary>
    private const string _invalidatorThreadName = "Invalidator";

    /// <summary>
    /// The name of the thread that replaces the value of an item.
    /// </summary>
    private const string _setterThreadName = "Setter";

    /// <summary>
    /// The name of the first of two threads that invalidate a dependency.
    /// </summary>
    private const string _firstInvalidatorThreadName = "FirstInvalidator";

    /// <summary>
    /// The name of the second of two threads that invalidate a dependency.
    /// </summary>
    private const string _secondInvalidatorThreadName = "SecondInvalidator";

    /// <summary>
    /// The name of the synchronization point that an invalidation reaches while it holds the lock of the dependency set
    /// that it copies.
    /// </summary>
    private const string _dependencyLockedSyncPoint = "MemoryCachingBackend.InvalidateDependencyImpl:DependencyLocked";

    /// <summary>
    /// The name of the synchronization point that a removal, or the removal of an item by an invalidation, reaches after
    /// it has acquired the lock of the key and before it reads the item.
    /// </summary>
    private const string _itemLockedSyncPoint = "MemoryCachingBackend.RemoveItemImpl:ItemLocked";

    /// <summary>
    /// The description of the place of <see cref="_dependencyLockedSyncPoint"/>, used in the failure messages.
    /// </summary>
    private const string _dependencyLockedDescription = "the synchronization point inside the lock of the dependency set";

    /// <summary>
    /// The description of the place of <see cref="_itemLockedSyncPoint"/>, used in the failure messages.
    /// </summary>
    private const string _itemLockedDescription = "the synchronization point that follows the acquisition of the lock of the key";

    /// <summary>
    /// The maximum time to wait for a thread to reach a pause point, for the threads to complete, or for the events to
    /// be delivered. It only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines when a thread does not complete.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendDeadlockTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendDeadlockTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Runs <see cref="CachingBackend.RemoveItem"/> of an item and <see cref="CachingBackend.InvalidateDependency"/> of
    /// its dependency concurrently. Both operations must complete, the item and its dependency set must be removed, and
    /// the events must report exactly one removal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The removal thread is paused after it has acquired the lock of the key and before it reads the item. Its next
    /// step removes the item and requests the lock of the dependency set to unregister the key. The invalidation thread
    /// is then paused while it holds the lock of the dependency set. Its next step requests the lock of the key to remove
    /// the item. The test guards against an invalidation that requests the lock of the key while it still holds the
    /// lock of the set, which deadlocks with the removal in both release orders.
    /// </para>
    /// <para>
    /// The removal holds the lock of the key before the invalidation reads the item, so the removal always removes the
    /// item and the invalidation finds no item. The backend therefore raises one <see cref="CachingBackend.ItemRemoved"/>
    /// event with the reason <see cref="CacheItemRemovedReason.Removed"/>, no event with the reason
    /// <see cref="CacheItemRemovedReason.Invalidated"/>, and one <see cref="CachingBackend.DependencyInvalidated"/> event
    /// for the dependency.
    /// </para>
    /// </remarks>
    /// <param name="releaseInvalidationFirst">
    /// <see langword="true"/> to release the invalidation thread before the removal thread, <see langword="false"/> to
    /// release the removal thread first.
    /// </param>
    [Theory]
    [InlineData( false )]
    [InlineData( true )]
    public async Task RemoveItem_ConcurrentWithInvalidateDependency_DoesNotDeadlock( bool releaseInvalidationFirst )
    {
        const string key = "key";
        const string dependency = "dependency";

        using var backendUnderTest = new BackendUnderTest();
        var backend = backendUnderTest.Backend;

        backend.SetItem( key, new CacheItem( "value", ImmutableArray.Create( dependency ) ) );

        // No other thread runs, so only the removal thread can reach the synchronization point.
        var removalSyncPoint = backendUnderTest.Synchronization.Arm( _itemLockedSyncPoint );
        TestSynchronizationProvider.SyncPoint? invalidationSyncPoint = null;

        try
        {
            var remover = StartUntilReached(
                _removerThreadName,
                () => backend.RemoveItem( key ),
                removalSyncPoint.WaitUntilReached,
                _itemLockedDescription );

            // The removal thread is paused and never copies a dependency set, so only the invalidation thread can reach
            // the synchronization point.
            invalidationSyncPoint = backendUnderTest.Synchronization.Arm( _dependencyLockedSyncPoint );

            var invalidator = StartUntilReached(
                _invalidatorThreadName,
                () => backend.InvalidateDependency( dependency ),
                invalidationSyncPoint.WaitUntilReached,
                _dependencyLockedDescription );

            if ( releaseInvalidationFirst )
            {
                invalidationSyncPoint.Release();
                removalSyncPoint.Release();
            }
            else
            {
                removalSyncPoint.Release();
                invalidationSyncPoint.Release();
            }

            this.AssertCompleted( backendUnderTest.Cache, remover, invalidator );
        }
        finally
        {
            removalSyncPoint.Release();
            invalidationSyncPoint?.Release();
        }

        Assert.True( backend.GetItem( key ) is null, "The item is still in the cache after its removal." );
        Assert.False( backend.ContainsDependency( dependency ), "The dependency is still registered although no item depends on it." );

        Assert.True( await backendUnderTest.WhenEventsDeliveredAsync(), "The events were not delivered within the timeout." );

        Assert.Equal(
            new[] { BackendUnderTest.FormatItemRemovedEvent( key, CacheItemRemovedReason.Removed ) },
            backendUnderTest.GetItemRemovedEvents() );

        Assert.Equal( new[] { dependency }, backendUnderTest.GetDependencyInvalidatedEvents() );
    }

    /// <summary>
    /// Runs <see cref="CachingBackend.SetItem"/>, which replaces a value that has a dependency, and
    /// <see cref="CachingBackend.InvalidateDependency"/> of that dependency concurrently. Both operations must complete
    /// and leave the dependency index consistent with the stored item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The setter thread is paused after it has acquired the lock of the key and before it reads the previous value.
    /// Its next steps register the key under the new dependency and unregister it from the dependencies that the new
    /// value no longer declares, so it requests the lock of the dependency set of the previous value in both cases. The
    /// invalidation thread is then paused while it holds the lock of that dependency set. Its next step requests the
    /// lock of the key to remove the item. The invalidation thread is released first, then the setter thread. The test
    /// guards against an invalidation that requests the lock of the key while it still holds the lock of the set, which
    /// deadlocks with the setter.
    /// </para>
    /// <para>
    /// The setter holds the lock of the key before the invalidation reads the item, so the invalidation always reads
    /// the new value. It removes the new value exactly when that value declares the invalidated dependency. Otherwise,
    /// the new value stays in the cache, and the invalidation of its own dependency must remove it, which shows that the
    /// dependency index still registers it.
    /// </para>
    /// </remarks>
    /// <param name="newValueKeepsDependency">
    /// <see langword="true"/> when the new value depends on the dependency of the previous value,
    /// <see langword="false"/> when it depends on another dependency only.
    /// </param>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void SetItem_ReplacingItemConcurrentWithInvalidateDependency_DoesNotDeadlock( bool newValueKeepsDependency )
    {
        const string key = "key";
        const string dependency = "dependency";
        var newDependency = newValueKeepsDependency ? dependency : "other-dependency";

        using var backendUnderTest = new BackendUnderTest();
        var backend = backendUnderTest.Backend;
        var cache = backendUnderTest.Cache;

        backend.SetItem( key, new CacheItem( "previous", ImmutableArray.Create( dependency ) ) );

        // The setter reads the previous value while it holds the lock of the key.
        var setterGate = cache.Arm(
            InterceptedOperation.TryGetValueBefore,
            InterceptingMemoryCache.ItemKey( key ),
            condition: InterceptingMemoryCache.OnThread( _setterThreadName ) );

        TestSynchronizationProvider.SyncPoint? invalidationSyncPoint = null;

        try
        {
            var setter = StartUntilReached(
                _setterThreadName,
                () => backend.SetItem( key, new CacheItem( "new", ImmutableArray.Create( newDependency ) ) ),
                setterGate.WaitUntilReached,
                "the read of the previous value under the lock of the key" );

            // The setter thread is paused and never copies a dependency set, so only the invalidation thread can reach
            // the synchronization point.
            invalidationSyncPoint = backendUnderTest.Synchronization.Arm( _dependencyLockedSyncPoint );

            var invalidator = StartUntilReached(
                _invalidatorThreadName,
                () => backend.InvalidateDependency( dependency ),
                invalidationSyncPoint.WaitUntilReached,
                _dependencyLockedDescription );

            invalidationSyncPoint.Release();
            setterGate.Release();

            this.AssertCompleted( cache, setter, invalidator );
        }
        finally
        {
            setterGate.Release();
            invalidationSyncPoint?.Release();
        }

        if ( newValueKeepsDependency )
        {
            Assert.True( backend.GetItem( key ) is null, "The new value declares the invalidated dependency, but the invalidation did not remove it." );
            Assert.False( backend.ContainsDependency( dependency ), "The dependency is still registered although no item depends on it." );
        }
        else
        {
            Assert.True(
                backend.GetItem( key ) is { Value: "new" },
                "The new value does not declare the invalidated dependency, but it is not in the cache after the invalidation." );

            Assert.False(
                backend.ContainsDependency( dependency ),
                "The dependency of the previous value is still registered although the new value does not declare it." );

            Assert.True( backend.ContainsDependency( newDependency ), "The dependency of the new value is not registered." );

            backend.InvalidateDependency( newDependency );

            Assert.True(
                backend.GetItem( key ) is null,
                "The invalidation of the dependency of the new value does not remove it, so the dependency index no longer registers the item." );

            Assert.False( backend.ContainsDependency( newDependency ), "The dependency is still registered although no item depends on it." );
        }
    }

    /// <summary>
    /// Runs two <see cref="CachingBackend.InvalidateDependency"/> calls concurrently on two dependencies that two items
    /// share. Both operations must complete, the items must be removed, the dependencies must be unregistered, and each
    /// item must be reported once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both items depend on the first and on the second dependency. The first thread invalidates the first dependency.
    /// It copies the first dependency set and is paused after it has acquired the lock of the first item that it
    /// processes, before it reads that item. Its next step removes the item and requests the locks of both dependency
    /// sets to unregister the key. The second thread invalidates the second dependency and is paused while it holds the
    /// lock of the second dependency set. Its next steps request the lock of each key of that set, including the key
    /// that the first thread holds. The first thread is released first, then the second thread. The test guards against
    /// an invalidation that requests the lock of a key while it still holds the lock of a set, which deadlocks with the
    /// other invalidation.
    /// </para>
    /// <para>
    /// The enumeration order of a dependency set is not specified, so the gate of the first thread matches the read of
    /// any item, and the test determines from the gate which item the first thread holds. The other item is removed by
    /// whichever thread reaches it first, so the test asserts the set of events and not the thread that raises each of
    /// them.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task InvalidateDependency_ConcurrentOnSharedDependents_DoesNotDeadlock()
    {
        const string firstItem = "item1";
        const string secondItem = "item2";
        const string firstDependency = "dependency1";
        const string secondDependency = "dependency2";

        using var backendUnderTest = new BackendUnderTest();
        var backend = backendUnderTest.Backend;
        var cache = backendUnderTest.Cache;

        backend.SetItem( firstItem, new CacheItem( "value1", ImmutableArray.Create( firstDependency, secondDependency ) ) );
        backend.SetItem( secondItem, new CacheItem( "value2", ImmutableArray.Create( firstDependency, secondDependency ) ) );

        // The first read of an item by the first thread is the read under the lock of the first key that it processes.
        var firstGate = cache.Arm(
            InterceptedOperation.TryGetValueBefore,
            InterceptingMemoryCache.AnyItemKey(),
            condition: InterceptingMemoryCache.OnThread( _firstInvalidatorThreadName ) );

        TestSynchronizationProvider.SyncPoint? secondSyncPoint = null;

        try
        {
            var firstInvalidator = StartUntilReached(
                _firstInvalidatorThreadName,
                () => backend.InvalidateDependency( firstDependency ),
                firstGate.WaitUntilReached,
                "the read of the first item that it processes, under the lock of the key" );

            var heldItem = firstGate.TrippedKey!.EndsWith( ":item:" + firstItem, StringComparison.Ordinal ) ? firstItem : secondItem;
            this._output.WriteLine( $"The first invalidation thread holds the lock of the item '{heldItem}'." );

            // The first thread has already copied its dependency set and is paused, so only the second thread can reach
            // the synchronization point.
            secondSyncPoint = backendUnderTest.Synchronization.Arm( _dependencyLockedSyncPoint );

            var secondInvalidator = StartUntilReached(
                _secondInvalidatorThreadName,
                () => backend.InvalidateDependency( secondDependency ),
                secondSyncPoint.WaitUntilReached,
                _dependencyLockedDescription );

            firstGate.Release();
            secondSyncPoint.Release();

            this.AssertCompleted( cache, firstInvalidator, secondInvalidator );
        }
        finally
        {
            firstGate.Release();
            secondSyncPoint?.Release();
        }

        Assert.True( backend.GetItem( firstItem ) is null, "The first item is still in the cache after the invalidation." );
        Assert.True( backend.GetItem( secondItem ) is null, "The second item is still in the cache after the invalidation." );
        Assert.False( backend.ContainsDependency( firstDependency ), "The first dependency is still registered although no item depends on it." );
        Assert.False( backend.ContainsDependency( secondDependency ), "The second dependency is still registered although no item depends on it." );

        Assert.True( await backendUnderTest.WhenEventsDeliveredAsync(), "The events were not delivered within the timeout." );

        Assert.Equal(
            new[]
            {
                BackendUnderTest.FormatItemRemovedEvent( firstItem, CacheItemRemovedReason.Invalidated ),
                BackendUnderTest.FormatItemRemovedEvent( secondItem, CacheItemRemovedReason.Invalidated )
            },
            backendUnderTest.GetItemRemovedEvents() );

        // The invalidation of an item also invalidates the dependency named by the key of the item.
        Assert.Equal(
            new[] { firstDependency, secondDependency, firstItem, secondItem },
            backendUnderTest.GetDependencyInvalidatedEvents() );
    }

    /// <summary>
    /// Runs <see cref="CachingBackend.RemoveItem"/> of an item whose dependency is the key of another item, concurrently
    /// with an invalidation that reaches the item through the recursive invalidation of that other item. Both operations
    /// must complete, remove both items and unregister both dependencies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The intermediate item depends on the root dependency, and the dependent item depends on the key of the
    /// intermediate item. The removal thread is paused after it has acquired the lock of the dependent key and before it
    /// reads the dependent item. Its next step removes the dependent item and requests the lock of the dependency set of
    /// the intermediate key to unregister the dependent key. The invalidation thread invalidates the root dependency,
    /// removes the intermediate item, and is paused in the recursive invalidation of the intermediate key while it holds
    /// the lock of the dependency set of that key. Its next step requests the lock of the dependent key. The removal
    /// thread is released first, then the invalidation thread. The test guards against a recursive invalidation that
    /// requests the lock of a key while it still holds the lock of a set, which deadlocks with the removal.
    /// </para>
    /// <para>
    /// The invalidation thread is paused in two steps. A gate pauses it on its read of the intermediate item, after it
    /// has copied the root dependency set. The synchronization point is armed at that moment, so that the next copy of a
    /// dependency set by that thread, which is the copy of the set of the intermediate key, is the one that pauses it.
    /// </para>
    /// <para>
    /// The removal holds the lock of the dependent key before the invalidation reads the dependent item, so the removal
    /// always removes it and the invalidation finds no dependent item.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task RemoveItem_ConcurrentWithRecursiveInvalidation_DoesNotDeadlock()
    {
        const string rootDependency = "root";
        const string intermediateItem = "intermediate";
        const string dependentItem = "dependent";

        using var backendUnderTest = new BackendUnderTest();
        var backend = backendUnderTest.Backend;
        var cache = backendUnderTest.Cache;

        backend.SetItem( intermediateItem, new CacheItem( "intermediate-value", ImmutableArray.Create( rootDependency ) ) );
        backend.SetItem( dependentItem, new CacheItem( "dependent-value", ImmutableArray.Create( intermediateItem ) ) );

        // No other thread runs, so only the removal thread can reach the synchronization point.
        var removalSyncPoint = backendUnderTest.Synchronization.Arm( _itemLockedSyncPoint );

        // The invalidation thread reads the intermediate item under the lock of its key, after it has copied the root
        // dependency set.
        var intermediateGate = cache.Arm(
            InterceptedOperation.TryGetValueBefore,
            InterceptingMemoryCache.ItemKey( intermediateItem ),
            condition: InterceptingMemoryCache.OnThread( _invalidatorThreadName ) );

        TestSynchronizationProvider.SyncPoint? invalidationSyncPoint = null;

        try
        {
            var remover = StartUntilReached(
                _removerThreadName,
                () => backend.RemoveItem( dependentItem ),
                removalSyncPoint.WaitUntilReached,
                _itemLockedDescription );

            var invalidator = StartUntilReached(
                _invalidatorThreadName,
                () => backend.InvalidateDependency( rootDependency ),
                intermediateGate.WaitUntilReached,
                "the read of the intermediate item under the lock of its key" );

            // The removal thread is paused and never copies a dependency set, and the invalidation thread has already
            // copied the root dependency set, so the next copy by the invalidation thread is the recursive one.
            invalidationSyncPoint = backendUnderTest.Synchronization.Arm( _dependencyLockedSyncPoint );
            intermediateGate.Release();

            Assert.True(
                invalidationSyncPoint.WaitUntilReached( _timeout ),
                $"Schedule precondition failed: the thread {_invalidatorThreadName} did not reach {_dependencyLockedDescription} in the recursive invalidation. Exception of the thread: {invalidator.Exception?.ToString() ?? "none"}." );

            Assert.False(
                backend.ContainsItem( intermediateItem ),
                "Schedule precondition failed: the invalidation thread should have removed the intermediate item before it copied the dependency set of its key." );

            Assert.True(
                backend.ContainsItem( dependentItem ),
                "Schedule precondition failed: the dependent item should still be in the cache while the removal thread is paused." );

            removalSyncPoint.Release();
            invalidationSyncPoint.Release();

            this.AssertCompleted( cache, remover, invalidator );
        }
        finally
        {
            removalSyncPoint.Release();
            intermediateGate.Release();
            invalidationSyncPoint?.Release();
        }

        Assert.True( backend.GetItem( intermediateItem ) is null, "The intermediate item is still in the cache after the invalidation." );
        Assert.True( backend.GetItem( dependentItem ) is null, "The dependent item is still in the cache after its removal." );
        Assert.False( backend.ContainsDependency( rootDependency ), "The root dependency is still registered although no item depends on it." );

        Assert.False(
            backend.ContainsDependency( intermediateItem ),
            "The key of the intermediate item is still registered as a dependency although no item depends on it." );

        Assert.True( await backendUnderTest.WhenEventsDeliveredAsync(), "The events were not delivered within the timeout." );

        Assert.Equal(
            new[]
            {
                BackendUnderTest.FormatItemRemovedEvent( dependentItem, CacheItemRemovedReason.Removed ),
                BackendUnderTest.FormatItemRemovedEvent( intermediateItem, CacheItemRemovedReason.Invalidated )
            },
            backendUnderTest.GetItemRemovedEvents() );

        // The invalidation of the intermediate item also invalidates the dependency named by its key.
        Assert.Equal( new[] { intermediateItem, rootDependency }, backendUnderTest.GetDependencyInvalidatedEvents() );
    }

    /// <summary>
    /// Runs <see cref="MemoryCachingBackend.RemoveItemImpl"/> of an item and
    /// <see cref="MemoryCachingBackend.InvalidateDependencyImpl"/> of its dependency concurrently, both with a replacement
    /// value. Both operations must complete, the replacement value must be stored, and the dependency must be
    /// unregistered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>LayeredCachingBackendEnhancer</c> calls these two methods with a replacement value when the underlying backend
    /// does not block. The replacement value marks the item as removed in the local cache during a transition period. In
    /// this mode, <see cref="MemoryCachingBackend.RemoveItemImpl"/> stores the replacement value instead of removing
    /// the entry.
    /// </para>
    /// <para>
    /// The removal thread is paused after it has acquired the lock of the key and before it reads the item. Its next
    /// step stores the replacement value and requests the lock of the dependency set to unregister the key. The
    /// invalidation thread is then paused while it holds the lock of the dependency set. Its next step requests the lock
    /// of the key. The removal thread is released first, then the invalidation thread. The test guards against an
    /// invalidation that requests the lock of the key while it still holds the lock of the set, which deadlocks with the
    /// removal.
    /// </para>
    /// <para>
    /// The removal holds the lock of the key before the invalidation reads the item, so the invalidation always finds the
    /// replacement value and removes nothing. A direct call to <see cref="MemoryCachingBackend.RemoveItemImpl"/> raises no
    /// event, so the backend raises no <see cref="CachingBackend.ItemRemoved"/> event and one
    /// <see cref="CachingBackend.DependencyInvalidated"/> event for the dependency.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task RemoveItemWithReplacementValue_ConcurrentWithInvalidationWithReplacementValue_DoesNotDeadlock()
    {
        const string key = "key";
        const string dependency = "dependency";

        using var backendUnderTest = new BackendUnderTest();
        var backend = backendUnderTest.Backend;

        backend.SetItem( key, new CacheItem( "value", ImmutableArray.Create( dependency ) ) );

        // The backend converts the expiration to a duration with its own clock, so the expiration is computed from that
        // clock. It is far enough in the future for the replacement value to stay in the cache during the test.
        var replacementValueExpiration = backendUnderTest.TimeProvider.GetUtcNow().AddHours( 1 );

        // No other thread runs, so only the removal thread can reach the synchronization point.
        var removalSyncPoint = backendUnderTest.Synchronization.Arm( _itemLockedSyncPoint );
        TestSynchronizationProvider.SyncPoint? invalidationSyncPoint = null;

        try
        {
            var remover = StartUntilReached(
                _removerThreadName,
                () => backend.RemoveItemImpl( key, CreateReplacementValue(), replacementValueExpiration ),
                removalSyncPoint.WaitUntilReached,
                _itemLockedDescription );

            // The removal thread is paused and never copies a dependency set, so only the invalidation thread can reach
            // the synchronization point.
            invalidationSyncPoint = backendUnderTest.Synchronization.Arm( _dependencyLockedSyncPoint );

            var invalidator = StartUntilReached(
                _invalidatorThreadName,
                () => backend.InvalidateDependencyImpl( dependency, CreateReplacementValue(), replacementValueExpiration ),
                invalidationSyncPoint.WaitUntilReached,
                _dependencyLockedDescription );

            removalSyncPoint.Release();
            invalidationSyncPoint.Release();

            this.AssertCompleted( backendUnderTest.Cache, remover, invalidator );
        }
        finally
        {
            removalSyncPoint.Release();
            invalidationSyncPoint?.Release();
        }

        Assert.True( backend.GetItem( key ) is { Value: null }, "The replacement value is not stored under the key of the removed item." );
        Assert.False( backend.ContainsDependency( dependency ), "The dependency is still registered although no item depends on it." );

        Assert.True( await backendUnderTest.WhenEventsDeliveredAsync(), "The events were not delivered within the timeout." );

        Assert.Empty( backendUnderTest.GetItemRemovedEvents() );
        Assert.Equal( new[] { dependency }, backendUnderTest.GetDependencyInvalidatedEvents() );
    }

    /// <summary>
    /// Creates a replacement value like the removal marker that <c>LayeredCachingBackendEnhancer</c> stores in its local
    /// cache: a <see cref="MemoryCacheItem"/> without a value and without dependencies.
    /// </summary>
    /// <returns>A new replacement value.</returns>
    private static MemoryCacheItem CreateReplacementValue() => new( null, default, new object() );

    /// <summary>
    /// Starts a worker and waits until it reaches its pause point.
    /// </summary>
    /// <param name="threadName">The name of the thread of the worker. The condition of a gate selects the thread by this name.</param>
    /// <param name="action">The operation that the worker runs.</param>
    /// <param name="waitUntilReached">
    /// A function that waits, for at most the given time, until the worker has reached its pause point, and returns
    /// <see langword="true"/> when it has.
    /// </param>
    /// <param name="pausePointDescription">A description of the pause point, used in the failure message.</param>
    /// <returns>The started worker.</returns>
    private static ConcurrencyTestWorker StartUntilReached(
        string threadName,
        Action action,
        Func<TimeSpan, bool> waitUntilReached,
        string pausePointDescription )
    {
        var worker = ConcurrencyTestWorker.Start( threadName, action );

        Assert.True(
            waitUntilReached( _timeout ),
            $"Schedule precondition failed: the thread {threadName} did not reach {pausePointDescription}. Exception of the thread: {worker.Exception?.ToString() ?? "none"}." );

        return worker;
    }

    /// <summary>
    /// Waits until every worker has completed, within a single timeout for all of them, and asserts that none of them
    /// threw an exception.
    /// </summary>
    /// <param name="cache">The cache, whose record of calls is written to the output for each thread that has not completed.</param>
    /// <param name="workers">The workers.</param>
    private void AssertCompleted( InterceptingMemoryCache cache, params ConcurrencyTestWorker[] workers )
    {
        var stopwatch = Stopwatch.StartNew();
        var incompleteThreadNames = new List<string>();

        foreach ( var worker in workers )
        {
            var remainingTime = _timeout - stopwatch.Elapsed;

            if ( !worker.Join( remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero ) )
            {
                incompleteThreadNames.Add( worker.Name );
            }
        }

        foreach ( var threadName in incompleteThreadNames )
        {
            var lastCall = cache.Calls.LastOrDefault( c => c.ThreadName == threadName );

            this._output.WriteLine(
                lastCall is null
                    ? $"The thread {threadName} did not complete and made no cache call."
                    : $"The thread {threadName} did not complete. Its last cache call was {lastCall.Operation} on the key '{lastCall.Key}'." );
        }

        Assert.True(
            incompleteThreadNames.Count == 0,
            $"DEADLOCK: the following threads did not complete within the timeout after their release: {string.Join( ", ", incompleteThreadNames )}." );

        foreach ( var worker in workers )
        {
            Assert.True( worker.Exception is null, $"The thread {worker.Name} threw an exception: {worker.Exception}" );
        }
    }
}
