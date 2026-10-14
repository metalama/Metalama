// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the clearing, the disposal and the exception safety of <see cref="MemoryCachingBackend"/>.
/// </summary>
/// <remarks>
/// <para>
/// Several instances of <see cref="MemoryCachingBackend"/> can share one <see cref="IMemoryCache"/>, for example the
/// single instance that <c>AddMemoryCache</c> registers in the service container of an application. The remarks of
/// <see cref="MemoryCachingBackend"/> state that such instances keep separate items and separate dependencies. The tests
/// of <c>Clear</c> and <c>Dispose</c> check that an operation on one instance keeps the entries of the other users of the
/// memory cache, and keeps the memory cache usable. Two further tests check what an operation or a post-eviction callback
/// observes when it runs during or after the disposal of the backend.
/// </para>
/// <para>
/// The tests of <c>SetItem</c> configure a size calculator or a serializer whose code the test supplies. The backend calls
/// this code while it stores an item. The tests check that a failure of this code keeps the dependency index consistent
/// with the values in the cache, and that this code can call the backend without causing a deadlock.
/// </para>
/// <para>
/// Each test asserts the correct behavior, so a test fails while the backend behaves incorrectly. Before the decisive
/// assertion, a concurrent test asserts that each thread reached the intended point, so that a change of the order in
/// which the backend calls the memory cache is reported as such.
/// </para>
/// </remarks>
public sealed partial class MemoryCachingBackendLifecycleTests
{
    /// <summary>
    /// The key of the item that most tests store.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The key of a second item.
    /// </summary>
    private const string _otherKey = "other-key";

    /// <summary>
    /// The dependency of the item that most tests store.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// The dependency of the value that a failed replacement must keep.
    /// </summary>
    private const string _previousDependency = "previous-dependency";

    /// <summary>
    /// The dependency of the value that a failed store or replacement does not store.
    /// </summary>
    private const string _newDependency = "new-dependency";

    /// <summary>
    /// The key of the entry that the application stores directly in the memory cache.
    /// </summary>
    private const string _applicationKey = "application-key";

    /// <summary>
    /// The value of the entry that the application stores directly in the memory cache.
    /// </summary>
    private const string _applicationValue = "application-value";

    /// <summary>
    /// Selects a size calculator as the code that the test supplies to the backend.
    /// </summary>
    private const string _sizeCalculatorComponent = "SizeCalculator";

    /// <summary>
    /// Selects a serializer as the code that the test supplies to the backend.
    /// </summary>
    private const string _serializerComponent = "Serializer";

    /// <summary>
    /// The value for which the code that the test supplies throws an exception.
    /// </summary>
    private const string _failingValue = "failing-value";

    /// <summary>
    /// The message of the exception that the code supplied by the test throws.
    /// </summary>
    private const string _simulatedFailureMessage = "The code supplied by the test failed as instructed.";

    /// <summary>
    /// The key that the first writer stores in the reentrancy test.
    /// </summary>
    private const string _firstKey = "first-key";

    /// <summary>
    /// The key that the second writer stores in the reentrancy test.
    /// </summary>
    private const string _secondKey = "second-key";

    /// <summary>
    /// The value that the first writer stores in the reentrancy test. The code supplied by the test recognizes it.
    /// </summary>
    private const string _firstMarker = "first-marker";

    /// <summary>
    /// The value that the second writer stores in the reentrancy test. The code supplied by the test recognizes it.
    /// </summary>
    private const string _secondMarker = "second-marker";

    /// <summary>
    /// The name of the thread that removes the item while the backend is disposed.
    /// </summary>
    private const string _removerThreadName = "RemoveItem";

    /// <summary>
    /// The name of the thread that disposes the backend.
    /// </summary>
    private const string _disposerThreadName = "Dispose";

    /// <summary>
    /// The name of the thread of the first writer in the reentrancy test.
    /// </summary>
    private const string _firstWriterThreadName = "FirstWriter";

    /// <summary>
    /// The name of the thread of the second writer in the reentrancy test.
    /// </summary>
    private const string _secondWriterThreadName = "SecondWriter";

    /// <summary>
    /// The maximum time to wait for a thread or an event. The timeout only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendLifecycleTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendLifecycleTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Clearing one backend must not remove the items and the dependencies of another backend that shares the same
    /// memory cache.
    /// </summary>
    /// <remarks>
    /// Both backends resolve the single memory cache of the service provider, and both store an item under the same key
    /// with the same dependency. The test fails when <c>Clear</c> removes every entry of the shared memory cache instead
    /// of the entries of the backend on which it is called.
    /// </remarks>
    [Fact]
    public void Clear_OnOneBackend_KeepsItemsOfOtherBackendSharingTheStore()
    {
        using var fakes = new FakeCachingServices();
        using var first = CreateBackend( fakes.ServiceProvider, "first" );
        using var second = CreateBackend( fakes.ServiceProvider, "second" );

        first.SetItem( _key, new CacheItem( "first-value", [_dependency] ) );
        second.SetItem( _key, new CacheItem( "second-value", [_dependency] ) );

        first.Clear();

        Assert.Null( first.GetItem( _key ) );
        Assert.False( first.ContainsDependency( _dependency ), "Clear did not remove the dependency of the backend on which it was called." );

        Assert.Equal( "second-value", second.GetItem( _key )?.Value );
        Assert.True( second.ContainsDependency( _dependency ), "Clear on the first backend removed the dependency of the second backend." );
    }

    /// <summary>
    /// Clearing a backend must not remove the entries that the application stores directly in the memory cache that the
    /// backend uses.
    /// </summary>
    /// <remarks>
    /// When the application registers a memory cache in its service container, the backend resolves that instance, and
    /// the application and other libraries store their own entries in it. The test fails when <c>Clear</c> removes every
    /// entry of the memory cache instead of the entries of the backend.
    /// </remarks>
    [Fact]
    public void Clear_OnBackendSharingTheStore_KeepsEntriesOfTheApplication()
    {
        using var fakes = new FakeCachingServices();
        using var backend = CreateBackend( fakes.ServiceProvider, "backend" );

        fakes.MemoryCache.Set( _applicationKey, _applicationValue );
        backend.SetItem( _key, new CacheItem( "value", [_dependency] ) );

        backend.Clear();

        Assert.Null( backend.GetItem( _key ) );
        Assert.Equal( _applicationValue, fakes.MemoryCache.Get( _applicationKey ) );
    }

    /// <summary>
    /// Clearing one backend with <see cref="ClearCacheOptions.Compact"/> must not evict the items of another backend that
    /// shares the same memory cache, and must not raise <see cref="CachingBackend.ItemRemoved"/> in that backend.
    /// </summary>
    /// <remarks>
    /// <see cref="FakeMemoryCache"/> runs the post-eviction callbacks on the thread that evicts the entries, and the backend
    /// dispatches its events through the <see cref="TestWorkItemDispatcher"/> of the services, so the test waits for the
    /// dispatched events before it asserts. The test fails when the compaction applies to every entry of the shared memory
    /// cache instead of the entries of the backend on which it is called.
    /// </remarks>
    [Fact]
    public async Task Clear_WithCompactOptionOnOneBackend_KeepsItemsOfOtherBackendSharingTheStore()
    {
        using var fakes = new FakeCachingServices();
        using var first = CreateBackend( fakes.ServiceProvider, "first" );
        using var second = CreateBackend( fakes.ServiceProvider, "second" );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );

        var itemsRemovedFromSecond = new List<(string Key, CacheItemRemovedReason Reason)>();

        second.ItemRemoved += ( _, args ) =>
        {
            lock ( itemsRemovedFromSecond )
            {
                itemsRemovedFromSecond.Add( (args.Key, args.RemovedReason) );
            }
        };

        first.SetItem( _key, new CacheItem( "first-value" ) );
        second.SetItem( _key, new CacheItem( "second-value", [_dependency] ) );

        first.Clear( ClearCacheOptions.Compact );

        await fakes.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

        Assert.Equal( "second-value", second.GetItem( _key )?.Value );
        Assert.True( second.ContainsDependency( _dependency ), "The compaction of the first backend removed the dependency of the second backend." );

        (string Key, CacheItemRemovedReason Reason)[] removedItems;

        lock ( itemsRemovedFromSecond )
        {
            removedItems = itemsRemovedFromSecond.ToArray();
        }

        Assert.Empty( removedItems );
    }

    /// <summary>
    /// Clearing the first layer of a layered backend with <see cref="ClearCacheOptions.Local"/> must not remove the items
    /// of an in-memory second layer that shares the same memory cache.
    /// </summary>
    /// <remarks>
    /// The enhancer builds its first layer with the service provider of the second layer, so both layers resolve the single
    /// memory cache of the service provider, as in
    /// <see cref="MemoryCachingBackendIsolationTests.LayeredBackend_OverInMemorySecondLayer_KeepsTheLayersSeparate"/>. The
    /// option <see cref="ClearCacheOptions.Local"/> clears the first layer only. The test fails when clearing the first
    /// layer removes every entry of the shared memory cache.
    /// </remarks>
    [Fact]
    public void Clear_WithLocalOptionOnLayeredBackend_KeepsItemsOfSecondLayerSharingTheStore()
    {
        using var fakes = new FakeCachingServices();
        using var secondLayer = CreateBackend( fakes.ServiceProvider, "L2" );
        using var layered = new LayeredCachingBackendEnhancer( secondLayer, null, null );
        layered.Initialize();

        layered.SetItem( _key, new CacheItem( "layered-value" ) );
        secondLayer.SetItem( _otherKey, new CacheItem( "second-layer-value" ) );

        layered.Clear( ClearCacheOptions.Local );

        Assert.Null( layered.LocalCache.GetItem( _key ) );
        Assert.Equal( "second-layer-value", secondLayer.GetItem( _otherKey )?.Value );
    }

    /// <summary>
    /// Disposing one backend must keep the memory cache that it shares with another backend usable, together with the
    /// items and the dependencies of the other backend.
    /// </summary>
    /// <remarks>
    /// Both backends resolve the single memory cache of the service provider. Neither backend created that memory cache.
    /// The test fails when the disposal of a backend disposes a memory cache that the backend did not create.
    /// </remarks>
    [Fact]
    public void Dispose_OfOneBackend_KeepsSharedStoreUsable()
    {
        using var fakes = new FakeCachingServices();
        using var first = CreateBackend( fakes.ServiceProvider, "first" );
        using var second = CreateBackend( fakes.ServiceProvider, "second" );

        second.SetItem( _key, new CacheItem( "second-value", [_dependency] ) );

        first.Dispose();

        var exception = Record.Exception( () => second.SetItem( _otherKey, new CacheItem( "other-value" ) ) );

        Assert.True( exception is null, $"The second backend could not store an item after the disposal of the first backend: {exception}" );
        Assert.Equal( "second-value", second.GetItem( _key )?.Value );
        Assert.True( second.ContainsDependency( _dependency ), "The disposal of the first backend removed the dependency of the second backend." );
    }

    /// <summary>
    /// Disposing a backend must keep usable the memory cache that the application passed to
    /// <see cref="MemoryCachingBackendBuilder.WithMemoryCache"/>.
    /// </summary>
    /// <remarks>
    /// The application creates the memory cache, stores an entry of its own in it, and owns it. The test fails when the
    /// disposal of the backend disposes this memory cache.
    /// </remarks>
    [Fact]
    public void Dispose_OfBackendOverApplicationCache_KeepsApplicationCacheUsable()
    {
        using var applicationCache = new MemoryCache( new MemoryCacheOptions() );
        applicationCache.Set( _applicationKey, _applicationValue );

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "backend" } ).WithMemoryCache( applicationCache ) );

        backend.Initialize();
        backend.SetItem( _key, new CacheItem( "value" ) );

        backend.Dispose();

        var exception = Record.Exception( () => applicationCache.TryGetValue( _applicationKey, out _ ) );

        Assert.True( exception is null, $"The memory cache of the application could not be read after the disposal of the backend: {exception}" );
        Assert.Equal( _applicationValue, applicationCache.Get( _applicationKey ) );
    }

    /// <summary>
    /// A removal that is in progress while the backend is disposed must either complete, or fail with the
    /// <see cref="ObjectDisposedException"/> that <see cref="CachingBackend"/> throws for an operation on a disposed
    /// backend.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CachingBackend"/> reports an operation on a disposed backend with an <see cref="ObjectDisposedException"/>
    /// whose object name is the string representation of the backend. The disposal does not wait for the operations in
    /// progress. An operation that has passed the status check when the disposal runs must therefore either complete, or
    /// fail with the same exception as an operation that starts after the disposal. It must not fail with the exception
    /// of the memory cache, whose object name is the memory cache: that exception exposes an implementation detail, and it
    /// does not identify the object that the caller used.
    /// </para>
    /// <para>
    /// The removal blocks at its first read of the item key, which follows the status check. A second thread then
    /// disposes the backend. The backend disposes the memory cache through
    /// <see cref="DisposalRedirectingMemoryCache"/>, which keeps the gate armed. The test then releases the removal. The
    /// removal also completes when the disposal of the backend keeps the memory cache usable, which this test accepts.
    /// </para>
    /// </remarks>
    [Fact]
    public void RemoveItem_ConcurrentWithDispose_CompletesOrThrowsBackendObjectDisposedException()
    {
        using var memoryCache = new MemoryCache( new MemoryCacheOptions() );
        using var interceptingCache = new InterceptingMemoryCache( memoryCache );
        var backendCache = new DisposalRedirectingMemoryCache( interceptingCache, memoryCache );

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "dispose-race" } ).WithMemoryCache( backendCache ) );

        backend.Initialize();
        backend.SetItem( _key, new CacheItem( "value" ) );

        InterceptionGate? gate = null;
        ConcurrencyTestWorker? remover = null;
        ConcurrencyTestWorker? disposer = null;

        try
        {
            // The gate blocks the first read of the item key by the removal. The removal has then passed the status check.
            gate = interceptingCache.Arm(
                InterceptedOperation.TryGetValueBefore,
                InterceptingMemoryCache.ItemKey( _key ),
                condition: InterceptingMemoryCache.OnThread( _removerThreadName ) );

            remover = ConcurrencyTestWorker.Start( _removerThreadName, () => backend.RemoveItem( _key ) );

            Assert.True( gate.WaitUntilReached( _timeout ), "The removal did not reach its first read of the item key." );

            // The disposal runs on a worker, so that a disposal that waits for the operations in progress is reported
            // instead of blocking the test.
            disposer = ConcurrencyTestWorker.Start( _disposerThreadName, backend.Dispose );

            Assert.True(
                disposer.Join( _timeout ),
                "Dispose did not complete while the removal was blocked inside the backend. The test requires that Dispose does not wait for the operations in progress." );

            Assert.True( disposer.Exception is null, $"Dispose failed: {disposer.Exception}" );

            gate.Release();

            Assert.True( remover.Join( _timeout ), "The removal did not complete after the release of its gate." );

            var exception = remover.Exception;

            this._output.WriteLine( $"The removal completed with the following exception: {exception?.ToString() ?? "none"}" );

            Assert.True(
                exception is null || IsObjectDisposedExceptionOf( exception, backend ),
                $"The removal that was in progress during the disposal failed with an exception that does not identify the backend: {exception}" );
        }
        finally
        {
            gate?.Release();
            remover?.Join( _timeout );
            disposer?.Join( _timeout );
        }
    }

    /// <summary>
    /// A post-eviction callback that runs after the disposal of the backend must complete without an exception.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MemoryCache"/> runs the post-eviction callbacks on the thread pool, so a callback that was queued before
    /// the disposal of the backend can run after it. <see cref="MemoryCache"/> catches an exception that a callback throws
    /// and writes it to its log at the error level. The test captures the callback of an evicted item that has a
    /// dependency with <see cref="InterceptingMemoryCache.DeferEvictionCallbacks"/>, disposes the backend, and then runs
    /// the callback.
    /// </para>
    /// <para>
    /// The test evicts the item with <see cref="InterceptingMemoryCache.Compact"/>, which evicts it for capacity. It does
    /// not call <see cref="CachingBackend.Clear"/>, because the backend does not own a cache that the test passes to it,
    /// and <see cref="CachingBackend.Clear"/> then removes the keys of the backend one at a time instead of compacting the
    /// cache.
    /// </para>
    /// <para>
    /// The test guards against a callback that, after the disposal, still accesses the memory cache or the released state
    /// of the backend to clean the dependencies of the evicted item, and throws.
    /// </para>
    /// </remarks>
    [Fact]
    public void EvictionCallback_RunningAfterDispose_CompletesWithoutException()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new InterceptingMemoryCache( fakes.MemoryCache );
        cache.DeferEvictionCallbacks( InterceptingMemoryCache.ItemKey( _key ) );

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "late-callback" } ).WithMemoryCache( cache ),
            fakes.ServiceProvider );

        backend.Initialize();
        backend.SetItem( _key, new CacheItem( "value", [_dependency] ) );

        // The compaction evicts the item, which is the only entry of the cache.
        cache.Compact( 1 );

        Assert.True( cache.WaitUntilCallbacksCaptured( 1, _timeout ), "The compaction did not evict the item, so no post-eviction callback was captured." );

        var capturedCallback = Assert.Single( cache.PendingCapturedCallbacks );

        Assert.True(
            capturedCallback.Reason is EvictionReason.Capacity,
            $"The item was evicted for the reason {capturedCallback.Reason} instead of the reason Capacity." );

        backend.Dispose();

        var exception = Record.Exception( () => cache.RunCapturedCallbacks() );

        Assert.True( exception is null, $"The post-eviction callback that ran after the disposal of the backend threw an exception: {exception}" );
    }

    /// <summary>
    /// A <c>SetItem</c> call that replaces a value and fails in the size calculator or in the serializer must keep the
    /// dependency index consistent with the value that remains in the cache.
    /// </summary>
    /// <remarks>
    /// The key first holds a value that depends on one dependency. The replacement depends on another dependency, and the
    /// code supplied by the test throws for it. After the failure, the key must not be registered for the dependency of
    /// the value that was never stored, and the invalidation of the dependency of the value in the cache must remove that
    /// value. The test fails when <c>SetItem</c> removes the registrations of the previous value and adds the
    /// registrations of the new value before it calls the code that throws.
    /// </remarks>
    /// <param name="failingComponent">The component whose code throws: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_ThrowingAfterDependencyUpdate_KeepsIndexConsistent( string failingComponent )
    {
        using var backend = CreateBackendWithForeignCode( "exception-safety", failingComponent, ThrowForFailingValue );

        backend.SetItem( _key, new CacheItem( "previous-value", [_previousDependency] ) );

        var exception = Record.Exception( () => backend.SetItem( _key, new CacheItem( _failingValue, [_newDependency] ) ) );

        Assert.True(
            IsSimulatedFailure( exception ),
            $"The replacement did not fail with the simulated failure of the code supplied by the test. Actual exception: {exception?.ToString() ?? "none"}" );

        Assert.False(
            backend.ContainsDependency( _newDependency ),
            "The failed replacement left the key registered for a dependency of the value that was never stored." );

        backend.InvalidateDependency( _previousDependency );

        Assert.True(
            backend.GetItem( _key ) is null,
            "The invalidation of a dependency of the value that remained in the cache did not remove that value." );
    }

    /// <summary>
    /// A first <c>SetItem</c> call for a key that fails in the size calculator or in the serializer must not leave the
    /// key registered for the dependencies of the value that was never stored.
    /// </summary>
    /// <remarks>
    /// A registration without a value is never removed: an invalidation of the dependency finds no value to remove, so
    /// it does not clean the registration. The test fails when <c>SetItem</c> registers the dependencies of the new value
    /// before it calls the code that throws.
    /// </remarks>
    /// <param name="failingComponent">The component whose code throws: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_ThrowingOnFirstStoreOfKey_LeavesNoDependencyRegistration( string failingComponent )
    {
        using var backend = CreateBackendWithForeignCode( "exception-safety", failingComponent, ThrowForFailingValue );

        var exception = Record.Exception( () => backend.SetItem( _key, new CacheItem( _failingValue, [_newDependency] ) ) );

        Assert.True(
            IsSimulatedFailure( exception ),
            $"The store did not fail with the simulated failure of the code supplied by the test. Actual exception: {exception?.ToString() ?? "none"}" );

        Assert.True( backend.GetItem( _key ) is null, "The failed store left a value in the cache." );

        Assert.False(
            backend.ContainsDependency( _newDependency ),
            "The failed store left the key registered for a dependency although no value is stored." );
    }

    /// <summary>
    /// Two <c>SetItem</c> calls whose size calculator or serializer stores a value under the key of the other call must
    /// both complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both keys already hold a value, and each <c>SetItem</c> call acquires the lock of its key. The code supplied by the
    /// test waits on a barrier until both calls run it, and then calls <c>SetItem</c> for the key of the other call. The
    /// test fails with a deadlock when the backend runs this code while it holds the lock of the key: each call then waits
    /// for the lock that the other call holds.
    /// </para>
    /// <para>
    /// When a deadlock is detected, the backend is not disposed, because the two writers remain blocked inside it.
    /// </para>
    /// </remarks>
    /// <param name="reenteringComponent">The component whose code calls the backend: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_WithForeignCodeReenteringTheBackend_DoesNotDeadlock( string reenteringComponent )
    {
        using var barrier = new Barrier( 2 );
        using var writersInForeignCode = new CountdownEvent( 2 );

        // The local function below is passed to the factory of the backend, so the variable must be assigned before the
        // backend exists. The local function runs only after the assignment of the backend.
        CachingBackend backend = null!;

        void ReenterBackend( object? value )
        {
            var nestedKey = value switch
            {
                _firstMarker => _secondKey,
                _secondMarker => _firstKey,
                _ => null
            };

            if ( nestedKey is null )
            {
                return;
            }

            if ( !barrier.SignalAndWait( _timeout ) )
            {
                throw new InvalidOperationException( "The other writer did not reach the code supplied by the test." );
            }

            writersInForeignCode.Signal();

            backend.SetItem( nestedKey, new CacheItem( "nested-value" ) );
        }

        backend = CreateBackendWithForeignCode( "reentrancy", reenteringComponent, ReenterBackend );

        // Each key already holds a value, so that each writer replaces a value, as in the original report of the defect.
        backend.SetItem( _firstKey, new CacheItem( "initial-value" ) );
        backend.SetItem( _secondKey, new CacheItem( "initial-value" ) );

        var bothCompleted = false;

        try
        {
            var firstWriter = ConcurrencyTestWorker.Start( _firstWriterThreadName, () => backend.SetItem( _firstKey, new CacheItem( _firstMarker ) ) );
            var secondWriter = ConcurrencyTestWorker.Start( _secondWriterThreadName, () => backend.SetItem( _secondKey, new CacheItem( _secondMarker ) ) );

            Assert.True( writersInForeignCode.Wait( _timeout ), "The two writers did not both reach the code supplied by the test." );

            var firstCompleted = firstWriter.Join( _timeout );

            // When the first writer has not completed, the failure is already detected, so the state of the second writer
            // is read without waiting.
            var secondCompleted = secondWriter.Join( firstCompleted ? _timeout : TimeSpan.Zero );

            this._output.WriteLine( $"First writer completed: {firstCompleted}. Second writer completed: {secondCompleted}." );

            bothCompleted = firstCompleted && secondCompleted;

            Assert.True(
                bothCompleted,
                "The two writers deadlocked. Each writer waits for the lock of the key that the other writer holds while it runs the code supplied by the test." );

            Assert.True( firstWriter.Exception is null, $"The first writer failed: {firstWriter.Exception}" );
            Assert.True( secondWriter.Exception is null, $"The second writer failed: {secondWriter.Exception}" );
        }
        finally
        {
            // A deadlocked writer remains blocked inside the backend, so the backend is disposed only when both writers
            // have completed.
            if ( bothCompleted )
            {
                backend.Dispose();
            }
        }
    }

    /// <summary>
    /// Creates and initializes a memory caching backend that resolves its memory cache from a service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider that supplies the memory cache.</param>
    /// <param name="debugName">The debug name of the backend.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackend( IServiceProvider serviceProvider, string debugName )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = debugName } ),
            serviceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Creates and initializes a memory caching backend, over a memory cache of its own, whose size calculator or
    /// serializer invokes a callback with the value of each item that the backend stores.
    /// </summary>
    /// <param name="debugName">The debug name of the backend.</param>
    /// <param name="foreignComponent">
    /// <see cref="_sizeCalculatorComponent"/> to invoke the callback from the size calculator, or
    /// <see cref="_serializerComponent"/> to invoke it from the serializer.
    /// </param>
    /// <param name="onForeignCall">The callback, which receives the value of the stored item.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackendWithForeignCode( string debugName, string foreignComponent, Action<object?> onForeignCall )
    {
        var configuration = foreignComponent switch
        {
            _sizeCalculatorComponent => new MemoryCachingBackendConfiguration
            {
                DebugName = debugName,
                SizeCalculator = value =>
                {
                    onForeignCall( value );

                    return 1;
                }
            },
            _serializerComponent => new MemoryCachingBackendConfiguration { DebugName = debugName, Serializer = new CallbackSerializer( onForeignCall ) },
            _ => throw new ArgumentOutOfRangeException( nameof(foreignComponent), foreignComponent, "The component is not supported by the test." )
        };

        var backend = CachingBackend.Create( b => b.Memory( configuration ) );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the value is <see cref="_failingValue"/>.
    /// </summary>
    /// <param name="value">The value of the item that the backend stores.</param>
    private static void ThrowForFailingValue( object? value )
    {
        if ( value is _failingValue )
        {
            throw new InvalidOperationException( _simulatedFailureMessage );
        }
    }

    /// <summary>
    /// Determines whether an exception is the exception thrown by <see cref="ThrowForFailingValue"/>.
    /// </summary>
    /// <param name="exception">The exception, or <see langword="null"/> when no exception was thrown.</param>
    /// <returns><see langword="true"/> when the exception is the simulated failure, otherwise <see langword="false"/>.</returns>
    private static bool IsSimulatedFailure( Exception? exception ) => exception is InvalidOperationException { Message: _simulatedFailureMessage };

    /// <summary>
    /// Determines whether an exception is the <see cref="ObjectDisposedException"/> that <see cref="CachingBackend"/>
    /// throws for an operation on a disposed backend.
    /// </summary>
    /// <remarks>
    /// <see cref="CachingBackend"/> gives this exception the string representation of the backend as object name. The
    /// string representation includes the status of the backend, so this method must be called after the disposal has
    /// completed.
    /// </remarks>
    /// <param name="exception">The exception.</param>
    /// <param name="backend">The disposed backend.</param>
    /// <returns><see langword="true"/> when the exception identifies the disposed backend, otherwise <see langword="false"/>.</returns>
    private static bool IsObjectDisposedExceptionOf( Exception exception, CachingBackend backend )
        => exception is ObjectDisposedException objectDisposedException
           && string.Equals( objectDisposedException.ObjectName, backend.ToString(), StringComparison.Ordinal );
}
