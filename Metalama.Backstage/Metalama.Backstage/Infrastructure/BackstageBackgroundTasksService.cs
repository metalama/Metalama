// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Backstage.Infrastructure;

// BackstageBackgroundTasksService is intentionally not disposable, relying instead on GC, because Metalama's
// service provider disposal implementation would dispose all backstage services for all tests, and a few
// tests would cause issues.
#pragma warning disable CA1001

public class BackstageBackgroundTasksService : IBackstageService
{
    private readonly object _lock = new();
    
    private readonly TaskCompletionSource<bool> _completedTaskSource = new();
    private readonly List<TaskCompletionSource<bool>> _onQueueEmptyWaiters = new();

    private int _pendingTasks;
    private bool _canEnqueue = true;

    /// <summary>
    /// Gets the default instance, which is intentionally shared in the process.
    /// It means that <see cref="CompleteAsync"/> can be only called once in the process.
    /// </summary>
    public static BackstageBackgroundTasksService Default { get; } = new();

    internal void Enqueue( Func<Task> func )
    {
        this.OnTaskStarting();
        Task.Run( func ).ContinueWith( this.OnTaskCompleted );
    }

    internal void Enqueue( Action action )
    {
        this.OnTaskStarting();
        Task.Run( action ).ContinueWith( this.OnTaskCompleted );
    }

    /// <summary>
    /// Prevents new tasks to be enqueued and awaits for the completion of previously enqueued tasks. 
    /// </summary>
    internal Task CompleteAsync()
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
            if ( !this._canEnqueue )
            {
                throw new InvalidOperationException();
            }

            this._pendingTasks++;
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void OnTaskCompleted( Task t )
    {
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