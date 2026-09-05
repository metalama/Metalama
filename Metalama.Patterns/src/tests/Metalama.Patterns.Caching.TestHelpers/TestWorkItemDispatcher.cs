// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.TestHelpers;

/// <summary>
/// An implementation of <see cref="IWorkItemDispatcher"/> that runs the work items on the thread pool and
/// lets a test wait for the completion of the work items that are pending.
/// </summary>
/// <remarks>
/// <para>
/// The work items still run on real threads. The caching code blocks in several places, so a single pump thread
/// executing work queued to itself would deadlock. This class therefore offers a completion point, not an ordering
/// guarantee.
/// </para>
/// <para>
/// A work item that queues another work item before it returns is covered: the count of pending work items does not
/// reach zero between the two, so <see cref="WhenPendingWorkItemsCompletedAsync"/> observes the whole chain.
/// </para>
/// </remarks>
public sealed class TestWorkItemDispatcher : IWorkItemDispatcher
{
    private readonly object _sync = new();

    private int _pendingWorkItemCount;
    private TaskCompletionSource<bool>? _idleTaskSource;

    /// <summary>
    /// Gets the number of work items that have been queued and have not completed yet.
    /// </summary>
    public int PendingWorkItemCount
    {
        get
        {
            lock ( this._sync )
            {
                return this._pendingWorkItemCount;
            }
        }
    }

    /// <inheritdoc />
    public void Dispatch( WaitCallback workItem, object? state, bool flowExecutionContext = true )
    {
        lock ( this._sync )
        {
            this._pendingWorkItemCount++;
        }

        var workItemState = new WorkItemState( workItem, state );

        if ( flowExecutionContext )
        {
            ThreadPool.QueueUserWorkItem( this.Execute, workItemState );
        }
        else
        {
            ThreadPool.UnsafeQueueUserWorkItem( this.Execute, workItemState );
        }
    }

    private void Execute( object? state )
    {
        var workItemState = (WorkItemState) state!;

        try
        {
            workItemState.WorkItem( workItemState.State );
        }
        finally
        {
            this.OnWorkItemCompleted();
        }
    }

    private void OnWorkItemCompleted()
    {
        TaskCompletionSource<bool>? idleTaskSource;

        lock ( this._sync )
        {
            this._pendingWorkItemCount--;

            if ( this._pendingWorkItemCount > 0 )
            {
                return;
            }

            idleTaskSource = this._idleTaskSource;
            this._idleTaskSource = null;
        }

        idleTaskSource?.TrySetResult( true );
    }

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when no work item is pending.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    public Task WhenPendingWorkItemsCompletedAsync( CancellationToken cancellationToken = default )
    {
        TaskCompletionSource<bool> idleTaskSource;

        lock ( this._sync )
        {
            if ( this._pendingWorkItemCount == 0 )
            {
                return Task.CompletedTask;
            }

            idleTaskSource = this._idleTaskSource ??= new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        }

        if ( !cancellationToken.CanBeCanceled )
        {
            return idleTaskSource.Task;
        }

        return WaitAsync( idleTaskSource.Task, cancellationToken );
    }

    /// <summary>
    /// Awaits the completion of the pending work items, or the cancellation of <paramref name="cancellationToken"/>,
    /// whichever comes first.
    /// </summary>
    /// <remarks>
    /// The cancellation completes a task of its own instead of the idle task. The idle task is shared by every
    /// caller that waits for the same period of activity, and it is reused until the count of pending work items
    /// reaches zero, so cancelling it would cancel the wait of the other callers as well.
    /// </remarks>
    private static async Task WaitAsync( Task idleTask, CancellationToken cancellationToken )
    {
        var cancellationTaskSource = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using ( cancellationToken.Register( () => cancellationTaskSource.TrySetCanceled( cancellationToken ) ) )
        {
            var completedTask = await Task.WhenAny( idleTask, cancellationTaskSource.Task );

            await completedTask;
        }
    }

    /// <summary>
    /// Blocks the calling thread until no work item is pending.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    public void WaitForPendingWorkItems( CancellationToken cancellationToken = default )
        => this.WhenPendingWorkItemsCompletedAsync( cancellationToken ).GetAwaiter().GetResult();

    /// <summary>
    /// The work item and the object passed to it, kept together so that a single delegate can be queued.
    /// </summary>
    private sealed class WorkItemState
    {
        public WorkItemState( WaitCallback workItem, object? state )
        {
            this.WorkItem = workItem;
            this.State = state;
        }

        public WaitCallback WorkItem { get; }

        public object? State { get; }
    }
}
