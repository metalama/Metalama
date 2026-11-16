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
using ITestSynchronizationProvider = Metalama.Testing.Hooks.ITestSynchronizationProvider;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that the <c>add</c> and <c>remove</c> accessors of the <see cref="CachingBackend.ItemRemoved"/> event are atomic
/// when another thread changes the subscriptions at the same time.
/// </summary>
/// <remarks>
/// <para>
/// An accessor reads the current delegate, combines it with the handler, and replaces the delegate only when the field
/// still holds the delegate that it has read. The tests pause one thread between the read and the replacement, with the
/// synchronization points of <see cref="CachingBackend.EventSyncPoints"/>. A synchronization point blocks only the
/// first thread that reaches it, so the accessors that the test thread calls afterwards are not paused.
/// </para>
/// <para>
/// While the thread is paused, the test thread subscribes another handler and unsubscribes it. The field then holds a
/// new delegate instance whose invocation list equals the invocation list of the delegate that the paused thread has
/// read. The equality operator of a delegate type compares the invocation lists. An accessor that decides with this
/// operator whether its replacement succeeded therefore treats the failed replacement as a success.
/// </para>
/// </remarks>
public sealed class CachingBackendEventSubscriptionTests
{
    /// <summary>
    /// The key of the item whose removal raises the event.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The name of the thread whose accessor call is paused.
    /// </summary>
    private const string _subscriberThreadName = "Subscriber";

    /// <summary>
    /// The maximum time to wait for a synchronization point, a worker or the pending work items. It only detects a
    /// failure of the test.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// Verifies that a handler is subscribed when another handler is subscribed and unsubscribed between the read and the
    /// replacement of the delegate.
    /// </summary>
    [Fact]
    public async Task Subscribe_ConcurrentWithSubscribeAndUnsubscribeOfAnotherHandler_SubscribesHandler()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = CreateFakes( synchronization );
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        var calls = new ConcurrentQueue<string>();

        // Two handlers are subscribed first, so that the delegate of the event has several handlers. The removal of the
        // transient handler then creates a new delegate instance.
        backend.ItemRemoved += FirstHandler;
        backend.ItemRemoved += SecondHandler;

        var delegateRead = synchronization.Arm( CachingBackend.EventSyncPoints.AddHandlerDelegateRead );

        RunWhileAccessorIsPaused(
            delegateRead,
            () => backend.ItemRemoved += TestedHandler,
            () =>
            {
                backend.ItemRemoved += TransientHandler;
                backend.ItemRemoved -= TransientHandler;
            } );

        await RaiseItemRemovedAsync( backend, fakes, cancellation.Token );

        Assert.True(
            calls.Contains( "tested" ),
            $"The handler was not called, so its subscription was lost. Called handlers: [{string.Join( ", ", calls )}]." );

        Assert.Equal( new[] { "first", "second", "tested" }, calls.OrderBy( c => c, StringComparer.Ordinal ).ToArray() );

        return;

        void FirstHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "first" );

        void SecondHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "second" );

        void TestedHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "tested" );

        void TransientHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "transient" );
    }

    /// <summary>
    /// Verifies that a handler is unsubscribed when another handler is subscribed and unsubscribed between the read and
    /// the replacement of the delegate.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_ConcurrentWithSubscribeAndUnsubscribeOfAnotherHandler_UnsubscribesHandler()
    {
        using var cancellation = new CancellationTokenSource( _timeout );
        using var synchronization = new TestSynchronizationProvider();
        using var fakes = CreateFakes( synchronization );
        using var cache = new MemoryCache( new MemoryCacheOptions() );
        using var backend = CreateBackend( cache, fakes );

        var calls = new ConcurrentQueue<string>();

        backend.ItemRemoved += FirstHandler;
        backend.ItemRemoved += SecondHandler;
        backend.ItemRemoved += TestedHandler;

        var delegateRead = synchronization.Arm( CachingBackend.EventSyncPoints.RemoveHandlerDelegateRead );

        RunWhileAccessorIsPaused(
            delegateRead,
            () => backend.ItemRemoved -= TestedHandler,
            () =>
            {
                backend.ItemRemoved += TransientHandler;
                backend.ItemRemoved -= TransientHandler;
            } );

        await RaiseItemRemovedAsync( backend, fakes, cancellation.Token );

        Assert.False(
            calls.Contains( "tested" ),
            $"The handler was called, so it was not unsubscribed. Called handlers: [{string.Join( ", ", calls )}]." );

        Assert.Equal( new[] { "first", "second" }, calls.OrderBy( c => c, StringComparer.Ordinal ).ToArray() );

        return;

        void FirstHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "first" );

        void SecondHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "second" );

        void TestedHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "tested" );

        void TransientHandler( object? sender, CacheItemRemovedEventArgs args ) => calls.Enqueue( "transient" );
    }

    /// <summary>
    /// Runs an accessor call on a worker thread, pauses it at a synchronization point, runs an action on the calling
    /// thread, and releases the worker.
    /// </summary>
    /// <param name="syncPoint">The armed synchronization point that the accessor call reaches.</param>
    /// <param name="pausedAccessorCall">The accessor call that runs on the worker thread.</param>
    /// <param name="whilePaused">The action that runs on the calling thread while the worker is paused.</param>
    private static void RunWhileAccessorIsPaused( TestSynchronizationProvider.SyncPoint syncPoint, Action pausedAccessorCall, Action whilePaused )
    {
        var subscriber = ConcurrencyTestWorker.Start( _subscriberThreadName, pausedAccessorCall );
        bool subscriberCompleted;

        try
        {
            Assert.True( syncPoint.WaitUntilReached( _timeout ), "Schedule precondition failed: the accessor did not read the current delegate." );

            whilePaused();
        }
        finally
        {
            syncPoint.Release();
            subscriberCompleted = subscriber.Join( _timeout );
        }

        Assert.True( subscriberCompleted, "The accessor call did not complete after its synchronization point was released." );
        Assert.True( subscriber.Exception == null, $"The accessor call threw an exception: {subscriber.Exception}" );
    }

    /// <summary>
    /// Stores and removes an item, so that the backend raises the <see cref="CachingBackend.ItemRemoved"/> event once,
    /// and waits until the handlers have run.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="fakes">The services whose work-item dispatcher delivers the events of the backend.</param>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    private static async Task RaiseItemRemovedAsync( CachingBackend backend, FakeCachingServices fakes, CancellationToken cancellationToken )
    {
        backend.SetItem( _key, new CacheItem( "value" ) );
        backend.RemoveItem( _key );

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationToken );
    }

    /// <summary>
    /// Creates the services of a test whose backend resolves a given synchronization provider.
    /// </summary>
    /// <param name="synchronization">The synchronization provider that the backend resolves.</param>
    /// <returns>The services.</returns>
    private static FakeCachingServices CreateFakes( TestSynchronizationProvider synchronization )
        => new( configureServices: s => s.AddSingleton<ITestSynchronizationProvider>( synchronization ) );

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> on a given <see cref="IMemoryCache"/>, with the
    /// services of <paramref name="fakes"/>.
    /// </summary>
    /// <param name="memoryCache">The memory cache of the backend. The backend does not own it.</param>
    /// <param name="fakes">The services of the backend.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IMemoryCache memoryCache, FakeCachingServices fakes )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( memoryCache ),
            fakes.ServiceProvider );

        backend.Initialize();

        return backend;
    }
}
