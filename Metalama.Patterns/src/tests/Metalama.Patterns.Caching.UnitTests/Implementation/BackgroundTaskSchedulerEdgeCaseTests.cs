// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Additional edge case tests for <see cref="BackgroundTaskScheduler"/>.
/// </summary>
public sealed class BackgroundTaskSchedulerEdgeCaseTests : IDisposable
{
    private readonly ITestOutputHelper _testOutput;
    private readonly ServiceProvider _serviceProvider;

    public BackgroundTaskSchedulerEdgeCaseTests( ITestOutputHelper testOutputHelper )
    {
        this._testOutput = testOutputHelper;
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    #region Overload Threshold Boundary Tests

    [Fact]
    public async Task Overload_ExactlyAtThreshold_NotOverloaded()
    {
        const int maxConcurrency = 2;
        const int overloadThreshold = 3;

        var scheduler = new BackgroundTaskScheduler(
            this._serviceProvider,
            null,
            sequential: false,
            maxConcurrency: maxConcurrency,
            overloadThreshold: overloadThreshold );

        var holdTasks = new TaskCompletionSource<bool>();

        // Enqueue exactly maxConcurrency + overloadThreshold = 5 tasks
        // At this exact count, should NOT be overloaded (overload is > threshold, not >=)
        const int taskCount = 5;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        // At exactly the threshold, should not be overloaded
        Assert.False( scheduler.IsOverloaded );

        // Release all tasks
        holdTasks.SetResult( true );

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();
    }

    [Fact]
    public async Task Overload_OneAboveThreshold_Overloaded()
    {
        const int maxConcurrency = 2;
        const int overloadThreshold = 3;

        var scheduler = new BackgroundTaskScheduler(
            this._serviceProvider,
            null,
            sequential: false,
            maxConcurrency: maxConcurrency,
            overloadThreshold: overloadThreshold );

        var holdTasks = new TaskCompletionSource<bool>();

        // Enqueue maxConcurrency + overloadThreshold + 1 = 6 tasks
        // This should trigger overload
        const int taskCount = 6;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        Assert.True( scheduler.IsOverloaded );

        // Release all tasks
        holdTasks.SetResult( true );

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();
    }

    #endregion

    #region Retry Behavior Edge Cases

    [Fact]
    public async Task Retry_MultipleFailures_TracksAllExceptions()
    {
        var mockPolicy = new CountingRetryPolicy { MaxAttempts = 4 };
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, mockPolicy, sequential: false );

        var attemptCount = 0;

        // Task fails 3 times, then succeeds
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                var attempt = Interlocked.Increment( ref attemptCount );

                if ( attempt < 4 )
                {
                    throw new InvalidOperationException( $"Attempt {attempt} failed" );
                }

                return Task.CompletedTask;
            } );

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // 3 failures should increment exception count
        Assert.Equal( 3, scheduler.BackgroundTaskExceptions );
        Assert.Equal( 4, attemptCount );
    }

    #endregion

    #region Sequential Mode Edge Cases

    [Fact]
    public async Task Sequential_TasksWithExceptions_DoNotBlockSubsequentTasks()
    {
        var exceptionObserver = new TestExceptionObserver();
        var services = new ServiceCollection();
        services.AddSingleton<ICachingExceptionObserver>( exceptionObserver );
        await using var sp = services.BuildServiceProvider();

        var scheduler = new BackgroundTaskScheduler( sp, sequential: true );
        const int taskCount = 5;
        var completedTasks = new bool[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            var index = i;

            if ( index % 2 == 0 )
            {
                // Even tasks throw
                scheduler.EnqueueBackgroundTask(
                    _ =>
                    {
                        completedTasks[index] = true;

                        throw new InvalidOperationException( $"Task {index} failed" );
                    } );
            }
            else
            {
                // Odd tasks succeed
                scheduler.EnqueueBackgroundTask(
                    _ =>
                    {
                        completedTasks[index] = true;

                        return Task.CompletedTask;
                    } );
            }
        }

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // All tasks should have executed
        for ( var i = 0; i < taskCount; i++ )
        {
            Assert.True( completedTasks[i], $"Task {i} should have executed" );
        }

        // 3 exceptions (tasks 0, 2, 4)
        Assert.Equal( 3, scheduler.BackgroundTaskExceptions );
    }

    #endregion

    #region Concurrent Enqueue During Dispose

    [Fact]
    public async Task EnqueueDuringDispose_GracefulHandling()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        var tasksEnqueued = 0;
        var tasksCompleted = 0;
        var minimumTasksEnqueued = new TaskCompletionSource<bool>();
        const int minimumTaskCount = 10;

        // Start a background thread that enqueues tasks
        var enqueueTask = Task.Run(
            async () =>
            {
                for ( var i = 0; i < 100; i++ )
                {
                    try
                    {
                        scheduler.EnqueueBackgroundTask(
                            _ =>
                            {
                                Interlocked.Increment( ref tasksCompleted );

                                return Task.CompletedTask;
                            } );

                        var count = Interlocked.Increment( ref tasksEnqueued );

                        if ( count == minimumTaskCount )
                        {
                            minimumTasksEnqueued.TrySetResult( true );
                        }
                    }
                    catch ( ObjectDisposedException )
                    {
                        // Expected after dispose starts
                        break;
                    }

                    await Task.Yield();
                }
            } );

        // Wait until a minimum number of tasks have been enqueued
        await minimumTasksEnqueued.Task.WaitWithTimeoutAsync();

        // Start disposal
        var disposeTask = scheduler.DisposeAsync();

        await Task.WhenAll( enqueueTask, disposeTask ).WaitWithTimeoutAsync();

        this._testOutput.WriteLine( $"Tasks enqueued: {tasksEnqueued}, Tasks completed: {tasksCompleted}" );

        // All enqueued tasks should have completed
        Assert.Equal( tasksEnqueued, tasksCompleted );
    }

    #endregion

    #region WhenBackgroundTasksCompleted Edge Cases

    [Fact]
    public async Task WhenBackgroundTasksCompleted_CalledMultipleTimes_AllComplete()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var holdTasks = new TaskCompletionSource<bool>();

        // Enqueue some tasks
        for ( var i = 0; i < 3; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        // Start multiple waits
        var wait1 = scheduler.WhenBackgroundTasksCompleted( CancellationToken.None );
        var wait2 = scheduler.WhenBackgroundTasksCompleted( CancellationToken.None );
        var wait3 = scheduler.WhenBackgroundTasksCompleted( CancellationToken.None );

        // Release tasks
        holdTasks.SetResult( true );

        // All waits should complete
        await Task.WhenAll( wait1, wait2, wait3 ).WaitWithTimeoutAsync();
    }

    [Fact]
    public async Task WhenBackgroundTasksCompleted_NewTasksAfterCompletion_WaitsForNew()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        // First batch
        scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask );
        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Second batch
        var secondBatchCompleted = false;

        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                secondBatchCompleted = true;

                return Task.CompletedTask;
            } );

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( secondBatchCompleted );
    }

    #endregion

    #region Max Concurrency Edge Cases

    [Fact]
    public async Task MaxConcurrency_One_RunsSequentially()
    {
        const int maxConcurrency = 1;
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, null, sequential: false, maxConcurrency: maxConcurrency );

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var syncLock = new object();
        const int taskCount = 10;
        var tasks = new List<TaskCompletionSource<bool>>();

        for ( var i = 0; i < taskCount; i++ )
        {
            var tcs = new TaskCompletionSource<bool>();
            tasks.Add( tcs );
            var localTcs = tcs;

            scheduler.EnqueueBackgroundTask(
                async _ =>
                {
                    var current = Interlocked.Increment( ref concurrentCount );

                    lock ( syncLock )
                    {
                        if ( current > maxConcurrent )
                        {
                            maxConcurrent = current;
                        }
                    }

                    await localTcs.Task;
                    Interlocked.Decrement( ref concurrentCount );
                } );
        }

        // Release all tasks in sequence
        foreach ( var tcs in tasks )
        {
            await Task.Delay( 10 );
            tcs.SetResult( true );
        }

        await scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        this._testOutput.WriteLine( $"Max concurrent with maxConcurrency=1: {maxConcurrent}" );
        Assert.Equal( 1, maxConcurrent );
    }

    #endregion
}