// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Implementation;

public sealed partial class BackgroundTaskSchedulerTests : IDisposable
{
    private readonly ITestOutputHelper _testOutput;
    private readonly ServiceProvider _serviceProvider;

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 30 );

    public BackgroundTaskSchedulerTests( ITestOutputHelper testOutputHelper )
    {
        this._testOutput = testOutputHelper;
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    #region Test Helpers

    /// <summary>
    /// Waits for a task with a timeout, throwing if the timeout is exceeded.
    /// Compatible with .NET Framework 4.7.2.
    /// </summary>
    private static async Task WaitWithTimeoutAsync( Task task, string message = "Timeout exceeded" )
    {
        if ( !await task.WithTimeout( _timeout ) )
        {
            throw new TimeoutException( message );
        }
    }

    /// <summary>
    /// Waits for a task with a timeout and returns the result.
    /// Compatible with .NET Framework 4.7.2.
    /// </summary>
    private static async Task<T> WaitWithTimeoutAsync<T>( Task<T> task, string message = "Timeout exceeded" )
    {
        if ( await Task.WhenAny( task, Task.Delay( _timeout ) ) != task )
        {
            throw new TimeoutException( message );
        }

        return await task;
    }

    #endregion

    #region Basic Enqueueing Tests

    [Fact]
    public async Task EnqueueTask_Executes()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var taskExecuted = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                taskExecuted.SetResult( true );

                return Task.CompletedTask;
            } );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.True( taskExecuted.Task.IsCompleted );
        Assert.True( await taskExecuted.Task );
    }

    [Fact]
    public async Task EnqueueValueTask_Executes()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var taskExecuted = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                taskExecuted.SetResult( true );

                return new ValueTask();
            } );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.True( taskExecuted.Task.IsCompleted );
        Assert.True( await taskExecuted.Task );
    }

    [Fact]
    public void EnqueueAfterDispose_Throws()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        scheduler.Dispose();

        Assert.Throws<ObjectDisposedException>( () => scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask ) );
    }

    [Fact]
    public void EnqueueAfterStopAccepting_Throws()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        scheduler.StopAcceptingBackgroundTasks();

        Assert.Throws<ObjectDisposedException>( () => scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask ) );
    }

    #endregion

    #region Sequential Mode Tests

    [Fact]
    public async Task Sequential_TasksRunInOrder()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, sequential: true );
        var executionOrder = new ConcurrentQueue<int>();
        var task1Started = new TaskCompletionSource<bool>();
        var task1Complete = new TaskCompletionSource<bool>();
        var task2Started = new TaskCompletionSource<bool>();

        // Task 1: signals when it starts, waits for release, records order
        scheduler.EnqueueBackgroundTask(
            async _ =>
            {
                task1Started.SetResult( true );
                await task1Complete.Task;
                executionOrder.Enqueue( 1 );
            } );

        // Task 2: signals when it starts, records order
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                task2Started.SetResult( true );
                executionOrder.Enqueue( 2 );

                return Task.CompletedTask;
            } );

        // Wait for task 1 to start
        await WaitWithTimeoutAsync( task1Started.Task );

        // Task 2 should NOT have started yet (sequential mode)
        Assert.False( task2Started.Task.IsCompleted );

        // Release task 1
        task1Complete.SetResult( true );

        // Wait for all tasks to complete
        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        // Verify task 2 started after task 1 completed
        Assert.True( task2Started.Task.IsCompleted );

        // Verify execution order
        Assert.Equal( 2, executionOrder.Count );
        Assert.True( executionOrder.TryDequeue( out var first ) );
        Assert.True( executionOrder.TryDequeue( out var second ) );
        Assert.Equal( 1, first );
        Assert.Equal( 2, second );
    }

    [Fact]
    public async Task Sequential_PreviousTaskException_NextStillRuns()
    {
        var exceptionObserver = new TestExceptionObserver();
        var services = new ServiceCollection();
        services.AddSingleton<ICachingExceptionObserver>( exceptionObserver );
        using var sp = services.BuildServiceProvider();

        var scheduler = new BackgroundTaskScheduler( sp, sequential: true );
        var task2Executed = new TaskCompletionSource<bool>();

        // Task 1: throws exception
        scheduler.EnqueueBackgroundTask( _ => throw new InvalidOperationException( "Task 1 failed" ) );

        // Task 2: should still run
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                task2Executed.SetResult( true );

                return Task.CompletedTask;
            } );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.True( await task2Executed.Task );
        Assert.True( scheduler.BackgroundTaskExceptions > 0 );
    }

    #endregion

    #region Parallel Mode Tests

    [Fact]
    public async Task Parallel_MultipleTasksRunConcurrently()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        const int taskCount = 5;
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var allTasksReady = new TaskCompletionSource<bool>();
        var tasksReady = new int[taskCount];
        var syncLock = new object();

        for ( var i = 0; i < taskCount; i++ )
        {
            var taskIndex = i;

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

                    tasksReady[taskIndex] = 1;

                    // Check if all tasks are ready
                    if ( tasksReady.All( r => r == 1 ) )
                    {
                        allTasksReady.TrySetResult( true );
                    }

                    // Wait until all tasks are ready
                    await allTasksReady.Task;

                    Interlocked.Decrement( ref concurrentCount );
                } );
        }

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        this._testOutput.WriteLine( $"Max concurrent: {maxConcurrent}" );
        Assert.True( maxConcurrent > 1, "Tasks should run concurrently in parallel mode" );
    }

    [Fact]
    public async Task Parallel_RespectsMaxConcurrency()
    {
        const int maxConcurrency = 2;
        const int taskCount = 5;
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, null, sequential: false, maxConcurrency: maxConcurrency );

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var syncLock = new object();
        var holdTasks = new TaskCompletionSource<bool>();

        for ( var i = 0; i < taskCount; i++ )
        {
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

                    await holdTasks.Task;
                    Interlocked.Decrement( ref concurrentCount );
                } );
        }

        // Give some time for tasks to start
        await Task.Delay( 100 );

        // Release all tasks
        holdTasks.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        this._testOutput.WriteLine( $"Max concurrent: {maxConcurrent}" );
        Assert.True( maxConcurrent <= maxConcurrency, $"Max concurrent {maxConcurrent} should not exceed {maxConcurrency}" );
    }

    #endregion

    #region Overload Detection Tests

    [Fact]
    public async Task Overload_TriggersWhenThresholdExceeded()
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
        var isOverloadedDuringTest = false;
        var overloadedChanged = false;

        scheduler.IsOverloadedChanged += () => overloadedChanged = true;

        // Enqueue enough tasks to trigger overload
        // Overload triggers when: taskCount - maxConcurrency > overloadThreshold
        // So we need: taskCount > maxConcurrency + overloadThreshold = 5
        const int taskCount = 7;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        // Check if overloaded
        isOverloadedDuringTest = scheduler.IsOverloaded;

        // Release all tasks
        holdTasks.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.True( isOverloadedDuringTest, "Scheduler should be overloaded" );
        Assert.True( overloadedChanged, "IsOverloadedChanged should have fired" );
    }

    [Fact]
    public async Task Overload_ClearsWhenTasksComplete()
    {
        const int maxConcurrency = 2;
        const int overloadThreshold = 2;

        var scheduler = new BackgroundTaskScheduler(
            this._serviceProvider,
            null,
            sequential: false,
            maxConcurrency: maxConcurrency,
            overloadThreshold: overloadThreshold );

        var holdTasks = new TaskCompletionSource<bool>();

        // Enqueue enough tasks to trigger overload
        const int taskCount = 6;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        var wasOverloaded = scheduler.IsOverloaded;

        // Release all tasks
        holdTasks.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        var isOverloadedAfter = scheduler.IsOverloaded;

        Assert.True( wasOverloaded, "Should have been overloaded during task execution" );
        Assert.False( isOverloadedAfter, "Should not be overloaded after all tasks complete" );
    }

    [Fact]
    public async Task OverloadedChanged_FiresOnTransition()
    {
        const int maxConcurrency = 2;
        const int overloadThreshold = 2;

        var scheduler = new BackgroundTaskScheduler(
            this._serviceProvider,
            null,
            sequential: false,
            maxConcurrency: maxConcurrency,
            overloadThreshold: overloadThreshold );

        var eventCount = 0;
        scheduler.IsOverloadedChanged += () => Interlocked.Increment( ref eventCount );

        var holdTasks = new TaskCompletionSource<bool>();

        // Enqueue enough tasks to trigger overload
        const int taskCount = 6;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => holdTasks.Task );
        }

        // Release all tasks
        holdTasks.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        this._testOutput.WriteLine( $"Event count: {eventCount}" );
        Assert.True( eventCount > 0, "IsOverloadedChanged should have fired at least once" );
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Cancel_PendingTasksRespectCancellation()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        var taskStarted = new TaskCompletionSource<bool>();
        var holdTask = new TaskCompletionSource<bool>();
        var taskWasCancelled = false;

        // Task: should detect cancellation and exit early
        scheduler.EnqueueBackgroundTask(
            async ct =>
            {
                taskStarted.SetResult( true );

                try
                {
                    // Wait for cancellation or release
                    while ( !ct.IsCancellationRequested )
                    {
                        if ( holdTask.Task.IsCompleted )
                        {
                            break;
                        }

                        await Task.Delay( 10 );
                    }

                    if ( ct.IsCancellationRequested )
                    {
                        taskWasCancelled = true;
                    }
                }
                catch ( OperationCanceledException )
                {
                    taskWasCancelled = true;
                }
            } );

        // Wait for task to start
        await WaitWithTimeoutAsync( taskStarted.Task );

        // Cancel all tasks
        scheduler.Cancel();

        // Wait for completion
        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        // Task should have detected cancellation
        Assert.True( taskWasCancelled, "Task should have detected cancellation" );
    }

    [Fact]
    public async Task PerTaskCancellation_TaskCancelledDuringExecution()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        var taskStarted = new TaskCompletionSource<bool>();
        var taskCancelled = new TaskCompletionSource<bool>();
        var holdTask = new TaskCompletionSource<bool>();

        using var taskCts = new CancellationTokenSource();

        // Task: starts, then waits, then checks cancellation
        scheduler.EnqueueBackgroundTask(
            async ct =>
            {
                taskStarted.SetResult( true );
                await holdTask.Task;

                if ( ct.IsCancellationRequested )
                {
                    taskCancelled.SetResult( true );
                }
            },
            taskCts.Token );

        // Wait for task to start
        await WaitWithTimeoutAsync( taskStarted.Task );

        // Cancel the task
        taskCts.Cancel();

        // Let task complete
        holdTask.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        // Task should have detected cancellation
        Assert.True( await taskCancelled.Task );
    }

    [Fact]
    public async Task CombinedCancellation_EitherCancels()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        var taskExecuted = false;
        var holdTask = new TaskCompletionSource<bool>();
        var taskStarted = new TaskCompletionSource<bool>();

        using var taskCts = new CancellationTokenSource();

        // Task: waits until cancelled
        scheduler.EnqueueBackgroundTask(
            async ct =>
            {
                taskStarted.SetResult( true );

                try
                {
                    await holdTask.Task;

                    if ( ct.IsCancellationRequested )
                    {
                        return;
                    }

                    taskExecuted = true;
                }
                catch ( OperationCanceledException )
                {
                    // Expected
                }
            },
            taskCts.Token );

        // Wait for task to start
        await WaitWithTimeoutAsync( taskStarted.Task );

        // Cancel using the per-task token
        taskCts.Cancel();

        // Let task complete
        holdTask.TrySetCanceled();

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.False( taskExecuted );
    }

    #endregion

    #region Retry Policy Tests

    [Fact]
    public async Task Retry_SucceedsAfterFailure()
    {
        var mockPolicy = new MockRetryPolicy { MaxAttempts = 3 };
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, mockPolicy, sequential: false );

        var attemptCount = 0;
        var succeeded = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                var attempt = Interlocked.Increment( ref attemptCount );

                if ( attempt < 2 )
                {
                    throw new InvalidOperationException( $"Attempt {attempt} failed" );
                }

                succeeded.SetResult( true );

                return Task.CompletedTask;
            } );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.True( await succeeded.Task );
        Assert.Equal( 2, attemptCount );
    }

    [Fact]
    public async Task Retry_ReleaseSemaphoreDuringDelay()
    {
        var delayTcs = new TaskCompletionSource<bool>();
        var mockPolicy = new MockRetryPolicy { MaxAttempts = 3, DelayTcs = delayTcs };

        const int maxConcurrency = 1;
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, mockPolicy, sequential: false, maxConcurrency: maxConcurrency );

        var task1AttemptCount = 0;
        var task1Failed = new TaskCompletionSource<bool>();
        var task2Executed = new TaskCompletionSource<bool>();

        // Task 1: fails on first attempt, waits for delay
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                var attempt = Interlocked.Increment( ref task1AttemptCount );

                if ( attempt == 1 )
                {
                    task1Failed.SetResult( true );

                    throw new InvalidOperationException( "First attempt failed" );
                }

                return Task.CompletedTask;
            } );

        // Wait for task 1 to fail
        await WaitWithTimeoutAsync( task1Failed.Task );

        // Task 2 should be able to run while task 1 is waiting for retry
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                task2Executed.SetResult( true );

                return Task.CompletedTask;
            } );

        // Wait for task 2 to execute (this proves semaphore was released)
        var task2ExecutedResult = await WaitWithTimeoutAsync( task2Executed.Task );
        Assert.True( task2ExecutedResult, "Task 2 should execute while Task 1 waits for retry" );

        // Release the delay
        delayTcs.SetResult( true );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );
    }

    [Fact]
    public async Task Retry_FailuresIncrementExceptionCount()
    {
        // Use a retry policy that allows 3 attempts (0, 1, 2) before giving up
        var mockPolicy = new MockRetryPolicy { MaxAttempts = 3 };
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider, mockPolicy, sequential: false );

        var attemptCount = 0;

        // Task fails twice, then succeeds on third attempt
        scheduler.EnqueueBackgroundTask(
            _ =>
            {
                var attempt = Interlocked.Increment( ref attemptCount );

                if ( attempt < 3 )
                {
                    throw new InvalidOperationException( $"Attempt {attempt} failed" );
                }

                return Task.CompletedTask;
            } );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        // Two failures should increment exception count twice
        Assert.Equal( 2, scheduler.BackgroundTaskExceptions );
        Assert.Equal( 3, attemptCount );
    }

    #endregion

    #region Completion Tracking Tests

    [Fact]
    public async Task WhenCompleted_CompletesWhenEmpty()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        // Should complete immediately when no tasks
        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        // If we get here without timeout, the test passes
        Assert.True( true );
    }

    [Fact]
    public async Task WhenCompleted_WaitsForAllTasks()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        const int taskCount = 5;
        var completedTasks = 0;
        var releaseTasks = new TaskCompletionSource<bool>[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            releaseTasks[i] = new TaskCompletionSource<bool>();
            var tcs = releaseTasks[i];

            scheduler.EnqueueBackgroundTask(
                async _ =>
                {
                    await tcs.Task;
                    Interlocked.Increment( ref completedTasks );
                } );
        }

        // Release all tasks
        foreach ( var tcs in releaseTasks )
        {
            tcs.SetResult( true );
        }

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Equal( taskCount, completedTasks );
    }

    [Fact]
    public async Task WhenCompleted_Cancellation()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var holdTask = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask( _ => holdTask.Task );

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>( async () => await scheduler.WhenBackgroundTasksCompleted( cts.Token ) );

        // Cleanup
        holdTask.SetResult( true );
        await scheduler.DisposeAsync();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Exception_IncrementsCounter()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );

        scheduler.EnqueueBackgroundTask( _ => throw new InvalidOperationException( "Test exception" ) );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Equal( 1, scheduler.BackgroundTaskExceptions );
    }

    [Fact]
    public async Task Exception_ObserverCalled()
    {
        var exceptionObserver = new TestExceptionObserver();
        var services = new ServiceCollection();
        services.AddSingleton<ICachingExceptionObserver>( exceptionObserver );
        using var sp = services.BuildServiceProvider();

        var scheduler = new BackgroundTaskScheduler( sp );

        scheduler.EnqueueBackgroundTask( _ => throw new InvalidOperationException( "Test exception" ) );

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Single( exceptionObserver.Exceptions );
        Assert.IsType<InvalidOperationException>( exceptionObserver.Exceptions.First().Exception );
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task Dispose_WaitsForTaskCompletion()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var taskCompleted = false;
        var holdTask = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            async _ =>
            {
                await holdTask.Task;
                taskCompleted = true;
            } );

        // Start dispose in background
        var disposeTask = Task.Run(
            () =>
            {
                // Release the task after a short delay
                holdTask.SetResult( true );
                scheduler.Dispose();
            } );

        await WaitWithTimeoutAsync( disposeTask );

        Assert.True( taskCompleted );
    }

    [Fact]
    public async Task DisposeAsync_WaitsForTaskCompletion()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var taskCompleted = false;
        var holdTask = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            async _ =>
            {
                await holdTask.Task;
                taskCompleted = true;
            } );

        // Release the task
        holdTask.SetResult( true );

        await scheduler.DisposeAsync();

        Assert.True( taskCompleted );
    }

    [Fact]
    public async Task Dispose_WithCancellation_Cancels()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var taskStarted = new TaskCompletionSource<bool>();
        var holdTask = new TaskCompletionSource<bool>();

        scheduler.EnqueueBackgroundTask(
            async _ =>
            {
                taskStarted.SetResult( true );
                await holdTask.Task;
            } );

        await WaitWithTimeoutAsync( taskStarted.Task );

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Dispose with cancelled token should throw
        Assert.Throws<OperationCanceledException>( () => scheduler.Dispose( cts.Token ) );

        // Cleanup
        holdTask.SetResult( true );
        scheduler.Cancel();
        await scheduler.DisposeAsync();
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task BackgroundTaskEnqueued_Fires()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        var eventCount = 0;

        scheduler.BackgroundTaskEnqueued += () => Interlocked.Increment( ref eventCount );

        const int taskCount = 3;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask );
        }

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Equal( taskCount, eventCount );
    }

    [Fact]
    public async Task Observer_TracksTaskLifecycle()
    {
        var observer = new TestObserver();
        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundTaskSchedulerObserver>( observer );
        using var sp = services.BuildServiceProvider();

        var scheduler = new BackgroundTaskScheduler( sp );
        const int taskCount = 3;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask );
        }

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Equal( taskCount, observer.EnqueuedIds.Count );
        Assert.Equal( taskCount, observer.CompletedIds.Count );

        // All enqueued IDs should be completed
        foreach ( var id in observer.EnqueuedIds )
        {
            Assert.Contains( id, observer.CompletedIds );
        }
    }

    #endregion

    #region Multiple Tasks Completion Order Tests

    [Fact]
    public async Task MultipleTasks_AllComplete()
    {
        var scheduler = new BackgroundTaskScheduler( this._serviceProvider );
        const int taskCount = 10;
        var completedCount = 0;

        for ( var i = 0; i < taskCount; i++ )
        {
            scheduler.EnqueueBackgroundTask(
                _ =>
                {
                    Interlocked.Increment( ref completedCount );

                    return Task.CompletedTask;
                } );
        }

        await WaitWithTimeoutAsync( scheduler.WhenBackgroundTasksCompleted( CancellationToken.None ) );

        Assert.Equal( taskCount, completedCount );
    }

    #endregion
}