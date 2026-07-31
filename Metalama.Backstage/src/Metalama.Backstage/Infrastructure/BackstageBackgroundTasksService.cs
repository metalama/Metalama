// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Backstage.Infrastructure;

// BackstageBackgroundTasksService is intentionally not disposable, relying instead on GC, because Metalama's
// service provider disposal implementation would dispose all backstage services for all tests, and a few
// tests would cause issues.
#pragma warning disable CA1001

public sealed class BackstageBackgroundTasksService : IBackstageService
{
    private readonly object _lock = new();

    private readonly TaskCompletionSource<bool> _completedTaskSource = new();
    private readonly List<TaskCompletionSource<bool>> _onQueueEmptyWaiters = new();

    private int _pendingTasks;
    private bool _canEnqueue = true;
    private volatile ILogger? _logger;

    /// <summary>
    /// Sets the logger used to report the faults of background tasks. Called once the services are built, because this
    /// service exists before them, and because <see cref="Default"/> is a process-wide singleton.
    /// </summary>
    /// <remarks>
    /// Faults enqueued before the logger is set are still observed, but cannot be reported anywhere. In practice that
    /// window is the construction of the service provider, during which nothing is enqueued yet.
    /// </remarks>
    internal void SetLogger( ILogger logger ) => this._logger = logger;

    /// <summary>
    /// Gets the default instance, which is intentionally shared in the process.
    /// It means that <see cref="CompleteAsync"/> can be only called once in the process.
    /// </summary>
    [PublicAPI]
    public static BackstageBackgroundTasksService Default { get; } = new();

    internal Task Enqueue( Func<Task> func )
    {
        this.OnTaskStarting();

        return Task.Run( func ).ContinueWith( this.OnTaskCompleted );
    }

    internal Task Enqueue( Action action )
    {
        this.OnTaskStarting();

        return Task.Run( action ).ContinueWith( this.OnTaskCompleted );
    }

    /// <summary>
    /// Prevents new tasks to be enqueued and awaits for the completion of previously enqueued tasks.
    /// </summary>
    /// <remarks>
    /// A short-lived process must await this before it exits, otherwise a task that has been enqueued but has not
    /// started yet is killed. <see cref="ShutdownService"/> does it on <c>ProcessExit</c>, but a process that acts on a
    /// user gesture and then exits immediately should await it explicitly rather than rely on the shutdown handler
    /// having time to run. Calling it more than once is harmless. See #1751.
    /// </remarks>
    [PublicAPI]
    public Task CompleteAsync()
    {
        lock ( this._lock )
        {
            this._canEnqueue = false;

            if ( this._pendingTasks == 0 )
            {
                this._completedTaskSource.TrySetResult( true );
            }
        }

        return this._completedTaskSource.Task;
    }

    /// <summary>
    /// This method can be use in tests to wait for any point when the queue is empty but does
    /// not guarantee that no new task will be enqueued.
    /// </summary>
    internal Task WhenNoPendingTaskAsync()
    {
        lock ( this._lock )
        {
            if ( this._pendingTasks == 0 )
            {
                return Task.CompletedTask;
            }
            else
            {
                var waiter = new TaskCompletionSource<bool>();
                this._onQueueEmptyWaiters.Add( waiter );

                return waiter.Task;
            }
        }
    }

    private void OnTaskStarting()
    {
        lock ( this._lock )
        {
            // A task that is still running may legitimately enqueue another one while completion is under way: the
            // telemetry upload does exactly that, since the enqueued StartUpload itself enqueues the start of the
            // upload process. Refusing it silently lost the nested task, because the exception is thrown inside a task
            // nobody observes, so the upload process was never started and the queue was never uploaded. Completion
            // waits for the nested task as well, since it is counted here before the outer one completes. See #1764.
            if ( !this._canEnqueue && this._pendingTasks == 0 )
            {
                throw new InvalidOperationException(
                    $"Cannot enqueue a background task after {nameof(this.CompleteAsync)} has completed." );
            }

            this._pendingTasks++;
        }
    }

    private void OnTaskCompleted( Task task )
    {
        if ( task.IsFaulted )
        {
            // Nothing ever awaits an enqueued task: every caller discards the returned task, and this continuation is
            // the only thing that observes the antecedent. So this is the one place where the failure of a background
            // task can be reported at all. Without it the exception disappears entirely, which is how the telemetry
            // upload managed to be dead for a year without a single log line. See #1765.
            this._logger.LogException( task.Exception!, "A background task failed" );
        }

        IEnumerable<TaskCompletionSource<bool>> waiters;
        bool canEnqueue;

        lock ( this._lock )
        {
            this._pendingTasks--;

            if ( this._pendingTasks != 0 )
            {
                return;
            }

            // We make a copy of the waiters list to avoid a race condition
            // when the TaskCompletionSource.TrySetResult proceeds
            // to code that adds a new waiter before finishing the iteration
            // over the waiters.
            waiters = this._onQueueEmptyWaiters.ToArray();
            this._onQueueEmptyWaiters.Clear();
            canEnqueue = this._canEnqueue;
        }

        foreach ( var waiter in waiters )
        {
            waiter.TrySetResult( true );
        }

        if ( !canEnqueue )
        {
            this._completedTaskSource.TrySetResult( true );
        }
    }
}