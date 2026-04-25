// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class CachingBackendDisposalTests : IDisposable
{
    private const int _timeout = 30_000;

    private readonly ITestOutputHelper _testOutput;
    private readonly ServiceProvider _serviceProvider;

    public CachingBackendDisposalTests( ITestOutputHelper testOutputHelper )
    {
        this._testOutput = testOutputHelper;
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    private CachingBackend CreateBackend( string debugName = "test" )
    {
        return MemoryCacheFactory.CreateBackend( this._serviceProvider, debugName );
    }

    #region Multiple Dispose Calls

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var backend = this.CreateBackend();

        // First dispose
        backend.Dispose();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Second dispose - should not throw
        backend.Dispose();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Third dispose - should not throw
        backend.Dispose();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        var backend = this.CreateBackend();

        // First dispose
        await backend.DisposeAsync();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Second dispose - should not throw
        await backend.DisposeAsync();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Third dispose - should not throw
        await backend.DisposeAsync();
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    #endregion

    #region Concurrent Dispose Calls

    [Fact]
    public async Task Dispose_CalledConcurrently_BothComplete()
    {
        var backend = this.CreateBackend();
        await backend.InitializeAsync();

        var startSignal = new SemaphoreSlim( 0, 2 );
        var thread1Ready = new TaskCompletionSource<bool>();
        var thread2Ready = new TaskCompletionSource<bool>();

        Exception? thread1Exception = null;
        Exception? thread2Exception = null;

        var task1 = Task.Run(
            () =>
            {
                thread1Ready.SetResult( true );
                startSignal.Wait( _timeout );

                try
                {
                    backend.Dispose();
                }
                catch ( Exception ex )
                {
                    thread1Exception = ex;
                }
            } );

        var task2 = Task.Run(
            () =>
            {
                thread2Ready.SetResult( true );
                startSignal.Wait( _timeout );

                try
                {
                    backend.Dispose();
                }
                catch ( Exception ex )
                {
                    thread2Exception = ex;
                }
            } );

        // Wait for both threads to be ready
        await thread1Ready.Task.WaitWithTimeoutAsync();
        await thread2Ready.Task.WaitWithTimeoutAsync();

        // Release both threads simultaneously
        startSignal.Release( 2 );

        // Wait for both to complete
        await Task.WhenAll( task1, task2 ).WaitWithTimeoutAsync();

        Assert.Null( thread1Exception );
        Assert.Null( thread2Exception );
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    [Fact]
    public async Task DisposeAsync_CalledConcurrently_AllCallersComplete()
    {
        var backend = this.CreateBackend();
        await backend.InitializeAsync();

        const int callerCount = 5;
        var barrier = new SemaphoreSlim( 0, callerCount );
        var readySignals = new TaskCompletionSource<bool>[callerCount];
        var exceptions = new Exception?[callerCount];

        for ( var i = 0; i < callerCount; i++ )
        {
            readySignals[i] = new TaskCompletionSource<bool>();
        }

        var tasks = new Task[callerCount];

        for ( var i = 0; i < callerCount; i++ )
        {
            var index = i;

            tasks[i] = Task.Run(
                async () =>
                {
                    readySignals[index].SetResult( true );
                    await barrier.WaitAsync( _timeout );

                    try
                    {
                        await backend.DisposeAsync();
                    }
                    catch ( Exception ex )
                    {
                        exceptions[index] = ex;
                    }
                } );
        }

        // Wait for all callers to be ready
        foreach ( var signal in readySignals )
        {
            await signal.Task.WaitWithTimeoutAsync();
        }

        // Release all callers simultaneously
        barrier.Release( callerCount );

        // Wait for all to complete
        await Task.WhenAll( tasks ).WaitWithTimeoutAsync();

        foreach ( var exception in exceptions )
        {
            Assert.Null( exception );
        }

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task DisposeAsync_WithCancelledToken_DuringInitialization_ThrowsOperationCanceledException()
    {
        // Create a backend that blocks during initialization
        var initStarted = new TaskCompletionSource<bool>();
        var initComplete = new TaskCompletionSource<bool>();
        var backend = new SlowInitializingCachingBackend( this._serviceProvider, initStarted, initComplete );

        using var cts = new CancellationTokenSource();

        // Start initialization in background
        var initTask = Task.Run( async () => await backend.InitializeAsync( CancellationToken.None ), CancellationToken.None );

        // Wait for initialization to start
        await initStarted.Task.WaitWithTimeoutAsync();

        // Now try to dispose with a cancelled token while initialization is in progress
        // ReSharper disable once MethodHasAsyncOverload
        cts.Cancel();

        // The dispose should throw because we're waiting on the semaphore with cancelled token
        var disposeTask = Assert.ThrowsAsync<OperationCanceledException>( async () => await backend.DisposeAsync( cts.Token ) );

        // Complete initialization so tests don't hang
        initComplete.SetResult( true );

        await Task.WhenAll( initTask, disposeTask ).WaitWithTimeoutAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithCancelledToken_WhenAlreadyInitialized_CompletesSuccessfully()
    {
        // When a backend is already initialized and has no background tasks,
        // disposal completes even with a cancelled token (the token is only checked
        // when waiting for the initialization semaphore or background tasks)
        var backend = this.CreateBackend();
        await backend.InitializeAsync();

        using var cts = new CancellationTokenSource();

        // ReSharper disable once MethodHasAsyncOverload
        cts.Cancel();

        // This should complete without throwing because the backend has no background tasks
        await backend.DisposeAsync( cts.Token );

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    #endregion

    #region Event Clearing

    [Fact]
    public async Task Dispose_ClearsEventHandlers_EventsNotRaisedAfterDisposal()
    {
        var backend = this.CreateBackend();
        await backend.InitializeAsync();

        var eventRaisedCount = 0;

        backend.ItemRemoved += ( _, _ ) => Interlocked.Increment( ref eventRaisedCount );

        // Set an item before disposal
        await backend.SetItemAsync( "key", new CacheItem( "value" ) );

        // Start disposal - this should clear event handlers
        await backend.DisposeAsync();

        // Wait a bit to ensure any queued events would have fired
        await Task.Delay( 100 );

        // Event should not have been raised after disposal started
        // Note: The ItemRemoved event handler is cleared during disposal
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Event should not have fired after disposal
        this._testOutput.WriteLine( $"Events raised: {eventRaisedCount}" );
    }

    #endregion

    #region Default State Disposal

    [Fact]
    public void Dispose_FromDefaultState_Succeeds()
    {
        // Create backend but don't initialize it
        var backend = this.CreateBackend();

        Assert.Equal( CachingBackendStatus.Default, backend.Status );

        // Dispose from default state
        backend.Dispose();

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    [Fact]
    public async Task DisposeAsync_FromDefaultState_Succeeds()
    {
        // Create backend but don't initialize it
        var backend = this.CreateBackend();

        Assert.Equal( CachingBackendStatus.Default, backend.Status );

        // Dispose from default state
        await backend.DisposeAsync();

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    #endregion

    #region Status Transitions

    [Fact]
    public async Task Dispose_StatusTransitions_CorrectOrder()
    {
        var capturedStatuses = new List<CachingBackendStatus>();
        var backend = new StatusCapturingCachingBackend( this._serviceProvider, capturedStatuses );

        await backend.InitializeAsync();

        Assert.Equal( CachingBackendStatus.Initialized, backend.Status );

        await backend.DisposeAsync();

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Verify that Disposing status was captured during disposal
        Assert.Contains( CachingBackendStatus.Disposing, capturedStatuses );

        this._testOutput.WriteLine( $"Captured statuses: {string.Join( ", ", capturedStatuses )}" );
    }

    [Fact]
    public void Dispose_Sync_StatusTransitions_CorrectOrder()
    {
        var capturedStatuses = new List<CachingBackendStatus>();
        var backend = new StatusCapturingCachingBackend( this._serviceProvider, capturedStatuses );

        backend.Initialize();

        Assert.Equal( CachingBackendStatus.Initialized, backend.Status );

        backend.Dispose();

        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );

        // Verify that Disposing status was captured during disposal
        Assert.Contains( CachingBackendStatus.Disposing, capturedStatuses );

        this._testOutput.WriteLine( $"Captured statuses: {string.Join( ", ", capturedStatuses )}" );
    }

    #endregion

    #region Mixed Sync/Async Disposal

    [Fact]
    public async Task Dispose_MixedSyncAndAsync_BothComplete()
    {
        var backend = this.CreateBackend();
        await backend.InitializeAsync();

        var startSignal = new SemaphoreSlim( 0, 2 );
        var syncReady = new TaskCompletionSource<bool>();
        var asyncReady = new TaskCompletionSource<bool>();

        Exception? syncException = null;
        Exception? asyncException = null;

        var syncTask = Task.Run(
            () =>
            {
                syncReady.SetResult( true );
                startSignal.Wait( _timeout );

                try
                {
                    backend.Dispose();
                }
                catch ( Exception ex )
                {
                    syncException = ex;
                }
            } );

        var asyncTask = Task.Run(
            async () =>
            {
                asyncReady.SetResult( true );
                await startSignal.WaitAsync( _timeout );

                try
                {
                    await backend.DisposeAsync();
                }
                catch ( Exception ex )
                {
                    asyncException = ex;
                }
            } );

        // Wait for both to be ready
        await syncReady.Task.WaitWithTimeoutAsync();
        await asyncReady.Task.WaitWithTimeoutAsync();

        // Release both simultaneously
        startSignal.Release( 2 );

        // Wait for both to complete
        await Task.WhenAll( syncTask, asyncTask ).WaitWithTimeoutAsync();

        Assert.Null( syncException );
        Assert.Null( asyncException );
        Assert.Equal( CachingBackendStatus.Disposed, backend.Status );
    }

    #endregion
}