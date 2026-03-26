// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Flashtrace.Messages;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Resilience;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Schedules and manages background tasks for caching operations. This class provides a robust
/// infrastructure for executing cache-related background work with support for concurrency control,
/// retry policies, and overload detection.
/// </summary>
/// <remarks>
/// <para>
/// <b>Execution Modes:</b>
/// <list type="bullet">
/// <item><b>Parallel mode</b> (default): Tasks run concurrently up to the <c>maxConcurrency</c> limit.
/// Additional tasks queue until a slot becomes available.</item>
/// <item><b>Sequential mode</b> (<c>sequential=true</c>): Tasks execute strictly one after another
/// in enqueue order. Useful for operations that must not overlap.</item>
/// </list>
/// </para>
/// <para>
/// <b>Concurrency Control:</b>
/// The scheduler uses an internal <see cref="SemaphoreSlim"/> to throttle execution. The default
/// <c>maxConcurrency</c> is 50, meaning up to 50 tasks can execute simultaneously in parallel mode.
/// Additional tasks wait in a queue until a semaphore slot becomes available.
/// </para>
/// <para>
/// <b>Overload Detection:</b>
/// When the number of queued tasks exceeds the <c>overloadThreshold</c> (default: 500) above the
/// <c>maxConcurrency</c>, the scheduler enters an overloaded state. The <see cref="IsOverloaded"/>
/// property reflects this state, and the <see cref="IsOverloadedChanged"/> event fires on state transitions.
/// </para>
/// <para>
/// <b>Retry Policy:</b>
/// An optional <see cref="IRetryPolicy"/> can be provided to enable automatic retries for failed tasks.
/// During retry delays, the semaphore is released to allow other tasks to proceed, then re-acquired
/// before the next attempt.
/// </para>
/// <para>
/// <b>Cancellation:</b>
/// The scheduler supports two cancellation mechanisms:
/// <list type="bullet">
/// <item>Global cancellation via <see cref="Cancel"/>: Cancels all pending and running tasks.</item>
/// <item>Per-task cancellation: Each enqueued task can have its own <see cref="CancellationToken"/>
/// that is combined with the global token.</item>
/// </list>
/// </para>
/// <para>
/// <b>Disposal:</b>
/// The <see cref="Dispose()"/> and <see cref="DisposeAsync"/> methods wait for all pending tasks
/// to complete before releasing resources. Use the overload with a <see cref="CancellationToken"/>
/// to abort waiting if needed.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class BackgroundTaskScheduler : IDisposable, IAsyncDisposable, ITestableCachingComponent
{
    private readonly FlashtraceSource _logger;
    private readonly AwaitableEvent _backgroundTasksFinishedEvent = new( EventResetMode.ManualReset, true );
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private readonly bool _sequential;
    private readonly object _sync = new();
    private readonly ICachingExceptionObserver? _exceptionObserver;
    private readonly IBackgroundTaskSchedulerObserver? _backgroundTaskSchedulerObserver;
    private readonly IRetryPolicy? _retryPolicy;
    private readonly int _maxConcurrency;
    private readonly int _overloadThreshold;
    private readonly SemaphoreSlim _semaphore;

    private volatile int _backgroundTaskExceptions;
    private volatile int _backgroundTaskCount;
    private bool _disposed;

    /// <summary>
    /// Gets the number of background task exceptions that occurred.
    /// </summary>
    public int BackgroundTaskExceptions => this._backgroundTaskExceptions;

    private volatile Task _lastTask = Task.CompletedTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundTaskScheduler"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="sequential">If <c>true</c>, tasks are executed sequentially.</param>
    public BackgroundTaskScheduler( IServiceProvider? serviceProvider, bool sequential = false )
        : this( serviceProvider, null, sequential ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundTaskScheduler"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="retryPolicy">Optional retry policy for background tasks. If <c>null</c>, tasks are not retried.</param>
    /// <param name="sequential">If <c>true</c>, tasks are executed sequentially.</param>
    /// <param name="maxConcurrency">The maximum number of concurrent background tasks. Default is 50.</param>
    /// <param name="overloadThreshold">The number of queued tasks above <paramref name="maxConcurrency"/> at which the scheduler is considered overloaded. Default is 500.</param>
    public BackgroundTaskScheduler(
        IServiceProvider? serviceProvider,
        IRetryPolicy? retryPolicy,
        bool sequential,
        int maxConcurrency = 50,
        int overloadThreshold = 500 )
    {
        this._sequential = sequential;
        this._retryPolicy = retryPolicy;
        this._maxConcurrency = maxConcurrency;
        this._overloadThreshold = overloadThreshold;
        this._semaphore = new SemaphoreSlim( maxConcurrency );
        this._exceptionObserver = serviceProvider?.GetService<ICachingExceptionObserver>();
        this._backgroundTaskSchedulerObserver = serviceProvider?.GetService<IBackgroundTaskSchedulerObserver>();
        this._logger = serviceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
    }

    /// <summary>
    /// Gets a value indicating whether the scheduler is overloaded with pending tasks.
    /// </summary>
    public bool IsOverloaded { get; private set; }

    /// <summary>
    /// Event raised when the <see cref="IsOverloaded"/> property changes.
    /// </summary>
    public event Action? IsOverloadedChanged;

    /// <summary>
    /// Event raised when a background task is enqueued.
    /// </summary>
    public event Action? BackgroundTaskEnqueued;

    /// <summary>
    /// Cancels all pending background tasks.
    /// </summary>
    public void Cancel() => this._disposeCancellationTokenSource.Cancel();

    /// <summary>
    /// Forbids the use of the <see cref="EnqueueBackgroundTask(System.Func{System.Threading.CancellationToken,System.Threading.Tasks.ValueTask})"/> method. This method is used for debugging purposes only.
    /// </summary>
    public void StopAcceptingBackgroundTasks() => this._disposed = true;

    /// <summary>
    /// Enqueues a background task.
    /// </summary>
    /// <param name="task">A function creating a <see cref="ValueTask"/>.</param>
    public void EnqueueBackgroundTask( Func<CancellationToken, ValueTask> task ) => this.EnqueueBackgroundTask( ct => task( ct ).AsTask() );

    /// <summary>
    /// Enqueues a background task.
    /// </summary>
    /// <param name="task">A function creating a <see cref="Task"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for this specific task.</param>
    public void EnqueueBackgroundTask( Func<CancellationToken, Task> task, CancellationToken cancellationToken = default )
    {
        if ( this._disposed )
        {
            throw new ObjectDisposedException( this.ToString() );
        }

        this.BackgroundTaskEnqueued?.Invoke();

        this.IncrementTaskCount( 1 );

        var observedTaskId = this._backgroundTaskSchedulerObserver?.OnTaskEnqueued();
        var stopwatch = Stopwatch.StartNew();

        if ( this._sequential )
        {
            lock ( this._sync )
            {
                var previousTask = this._lastTask;

                var createdTask = Task.Run(
                    () => this.RunTask( task, previousTask, stopwatch, cancellationToken, observedTaskId ),
                    cancellationToken );

                this._lastTask = createdTask;
            }
        }
        else
        {
            Task.Run(
                () => this.RunTask( task, null, stopwatch, cancellationToken, observedTaskId ),
                cancellationToken );
        }
    }

    private void IncrementTaskCount( int increment )
    {
        lock ( this._backgroundTasksFinishedEvent )
        {
            // ReSharper disable once NonAtomicCompoundOperator
            this._backgroundTaskCount += increment;

            switch ( this._backgroundTaskCount )
            {
                case 1:
                    this._backgroundTasksFinishedEvent.Reset();

                    break;

                case 0:
                    this._backgroundTasksFinishedEvent.Set();

                    break;
            }
        }

        this.MonitorOverloadedStatus();
    }

    private void MonitorOverloadedStatus()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var taskCount = this._backgroundTaskCount;

        var excessTasks = taskCount - this._maxConcurrency;

        if ( excessTasks > 0 )
        {
            var isOverloaded = excessTasks > this._overloadThreshold;

            if ( isOverloaded )
            {
                if ( !this.IsOverloaded || (taskCount % 100) == 0 )
                {
                    this._logger.Warning.IfEnabled?.Write(
                        FormattedMessageBuilder.Formatted(
                            "The task queue is overloaded. There are now {Count} pending background tasks.",
                            taskCount ) );
                }

                this.IsOverloaded = true;
                this.IsOverloadedChanged?.Invoke();
            }
        }
        else
        {
            if ( this.IsOverloaded )
            {
                this._logger.Warning.IfEnabled?.Write( FormattedMessageBuilder.Formatted( "The task queue is no longer overloaded." ) );

                this.IsOverloaded = false;
                this.IsOverloadedChanged?.Invoke();
            }
        }
    }

    private async Task RunTask(
        Func<CancellationToken, Task> task,
        Task? lastTask,
        Stopwatch stopwatch,
        CancellationToken taskCancellationToken,
        int? observedTaskId )
    {
        if ( this._disposeCancellationTokenSource.IsCancellationRequested || taskCancellationToken.IsCancellationRequested )
        {
            this.NotifyTaskCompletedAndDecrementTaskCount( observedTaskId );

            return;
        }

        if ( lastTask != null )
        {
            try
            {
                await lastTask;
            }
            catch ( Exception e )
            {
                this.OnBackgroundTaskException( e );
            }
        }

        CancellationTokenSource? combinedCancellationTokenSource = null;
        var combinedCancellationToken = this._disposeCancellationTokenSource.Token;

        if ( taskCancellationToken.CanBeCanceled )
        {
            combinedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource( this._disposeCancellationTokenSource.Token, taskCancellationToken );

            combinedCancellationToken = combinedCancellationTokenSource.Token;
        }

        try
        {
            await this._semaphore.WaitAsync( combinedCancellationToken );
        }
        catch ( OperationCanceledException )
        {
            this.NotifyTaskCompletedAndDecrementTaskCount( observedTaskId );

            return;
        }

        try
        {
            if ( stopwatch.ElapsedMilliseconds > 10000 )
            {
                this._logger.Warning.IfEnabled?.Write(
                    FormattedMessageBuilder.Formatted(
                        "We waited {Elapsed} for the background queue. The system might be overloaded.",
                        stopwatch.Elapsed ) );
            }

            stopwatch.Restart();

            if ( this._retryPolicy != null )
            {
                await this.TryAndReleaseSemaphoreAsync( task, combinedCancellationToken, stopwatch, observedTaskId );
            }
            else
            {
                await this.RunTaskWithoutRetryAsync( task, combinedCancellationToken, stopwatch, observedTaskId );
            }
        }
        finally
        {
            combinedCancellationTokenSource?.Dispose();
        }
    }

    private async Task TryAndReleaseSemaphoreAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken,
        Stopwatch stopwatch,
        int? observedTaskId )
    {
        object? retryState = null;
        Exception? lastException = null;
        var attempt = 0;

        while ( true )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var retryTask = this._retryPolicy!.TryAsync( OperationKind.Background, attempt, lastException, ref retryState, cancellationToken );

            if ( !retryTask.IsCompleted )
            {
                // Don't hold the semaphore while waiting for retry delay.
                this._semaphore.Release();
                this.IncrementTaskCount( -1 );

                try
                {
                    await retryTask;
                }
                finally
                {
                    await this._semaphore.WaitAsync( cancellationToken );
                    this.IncrementTaskCount( 1 );
                    stopwatch.Restart();
                }
            }

            try
            {
                await task( cancellationToken );

                if ( stopwatch.ElapsedMilliseconds > 10000 )
                {
                    this._logger.Warning.IfEnabled?.Write(
                        FormattedMessageBuilder.Formatted(
                            "The background task {Task} lasted for {Duration}. The system might be overloaded.",
                            task.Method,
                            stopwatch.Elapsed ) );
                }

                // Task succeeded, release semaphore, notify observer, decrement count and return.
                this._semaphore.Release();
                this.NotifyTaskCompletedAndDecrementTaskCount( observedTaskId );

                return;
            }
            catch ( OperationCanceledException )
            {
                this._semaphore.Release();
                this.NotifyTaskCompletedAndDecrementTaskCount( observedTaskId );

                throw;
            }
            catch ( Exception e )
            {
                lastException = e;
                Interlocked.Increment( ref this._backgroundTaskExceptions );
                attempt++;
            }
        }
    }

    private async Task RunTaskWithoutRetryAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken,
        Stopwatch stopwatch,
        int? observedTaskId )
    {
        try
        {
            await task( cancellationToken );

            if ( stopwatch.ElapsedMilliseconds > 10000 )
            {
                this._logger.Warning.IfEnabled?.Write(
                    FormattedMessageBuilder.Formatted(
                        "The background task {Task} lasted for {Duration}. The system might be overloaded.",
                        task.Method,
                        stopwatch.Elapsed ) );
            }
        }
        catch ( Exception e )
        {
            this.OnBackgroundTaskException( e );
        }
        finally
        {
            this._semaphore.Release();
            this.NotifyTaskCompletedAndDecrementTaskCount( observedTaskId );
        }
    }

    /// <summary>
    /// Notifies the observer that a task has completed and then decrements the task count.
    /// The observer is notified before the count is decremented to ensure that
    /// <see cref="WhenBackgroundTasksCompleted"/> callers observe all completions.
    /// </summary>
    private void NotifyTaskCompletedAndDecrementTaskCount( int? observedTaskId )
    {
        if ( observedTaskId != null )
        {
            this._backgroundTaskSchedulerObserver?.OnTaskCompleted( observedTaskId.Value );
        }

        this.DecrementTaskCount();
    }

    private void DecrementTaskCount()
    {
        lock ( this._backgroundTasksFinishedEvent )
        {
            // ReSharper disable once NonAtomicCompoundOperator
            this._backgroundTaskCount--;

            if ( this._backgroundTaskCount == 0 )
            {
                this._backgroundTasksFinishedEvent.Set();
            }
        }

        this.MonitorOverloadedStatus();
    }

    private void OnBackgroundTaskException( Exception e )
    {
        if ( this._exceptionObserver.OnException( e, true ) )
        {
            return;
        }

        this._logger.Error.Write( FormattedMessageBuilder.Formatted( "{ExceptionType} when executing a background task.", e.GetType().Name ), e );
        Interlocked.Increment( ref this._backgroundTaskExceptions );
    }

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when all enqueued background tasks complete.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when all enqueued background tasks complete.</returns>
    public async Task WhenBackgroundTasksCompleted( CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // AwaitableEvent does not support CancellationToken.
        // ReSharper disable once InconsistentlySynchronizedField
        await this._backgroundTasksFinishedEvent.WaitAsync( CancellationToken.None );
    }

    /// <inheritdoc />
    public void Dispose() => this.Dispose( CancellationToken.None );

    /// <summary>
    /// Disposes the scheduler, waiting for all tasks to complete.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public void Dispose( CancellationToken cancellationToken )
    {
        // ReSharper disable once InconsistentlySynchronizedField
        this._backgroundTasksFinishedEvent.Wait( cancellationToken );
        this._disposed = true;
    }

    /// <summary>
    /// Disposes the scheduler asynchronously, waiting for all tasks to complete.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public async Task DisposeAsync( CancellationToken cancellationToken = default )
    {
        await this.WhenBackgroundTasksCompleted( cancellationToken );
        this._disposed = true;
    }

    /// <inheritdoc />
    ValueTask IAsyncDisposable.DisposeAsync() => new( this.DisposeAsync() );
}