// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Utilities;

/// <summary>
/// Allows to run tasks in the background and await until all tasks have completed.
/// </summary>
public sealed class TaskBag
{
    private readonly ConcurrentDictionary<int, (Task Task, Func<Task> Func)> _pendingTasks = new();
    private readonly ILogger _logger;
    private readonly DesignTimeExceptionHandler _exceptionHandler;
    private int _nextId;

    public TaskBag( ILogger logger, ServiceProvider<IGlobalService> exceptionHandler )
    {
        this._logger = logger;
        this._exceptionHandler = exceptionHandler.GetRequiredService<DesignTimeExceptionHandler>();
    }

    /// <summary>
    /// Runs an asynchronous action in the background and keeps track of it until it has completed.
    /// </summary>
    /// <remarks>
    /// The cancellation token is not passed to <see cref="Task.Run(Func{Task})"/> and is observed from inside the
    /// delegate instead. When a token is given to <see cref="Task.Run(Func{Task},CancellationToken)"/> and is
    /// signalled before the thread pool invokes the delegate, the task goes directly to the canceled state and the
    /// delegate never runs at all. The <c>finally</c> block below, which is what removes the entry from
    /// <see cref="_pendingTasks"/>, is part of that delegate, so the entry would stay in the dictionary forever
    /// together with the closure that produced it, and therefore with everything that closure captured. The callers
    /// of this class enqueue one delegate per version of a compilation and cancel the previous one, so such an entry
    /// retains a whole Roslyn compilation. See issue #1793.
    /// </remarks>
    internal void Enqueue( Func<Task> asyncAction, CancellationToken cancellationToken = default )
    {
        var taskId = Interlocked.Increment( ref this._nextId );
        var taskCompleted = false;
        var sync = new object();

        var task = Task.Run(
            async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await asyncAction();
                }
                catch ( OperationCanceledException )
                {
                    // Cancellation is the normal way for the caller to abandon work that a newer request has
                    // superseded, therefore it is not reported as an exception.
                }
                catch ( Exception e )
                {
                    this._exceptionHandler.ReportException( e, this._logger );
                }
                finally
                {
                    lock ( sync )
                    {
                        taskCompleted = true;
                        this._pendingTasks.TryRemove( taskId, out _ );
                    }
                }
            } );

        lock ( sync )
        {
            if ( !taskCompleted )
            {
                this._pendingTasks.TryAdd( taskId, (task, asyncAction) );
            }
            else
            {
                // If we add the task, it will never be removed.
            }
        }
    }

    [PublicAPI]
    public async Task WaitAllAsync( CancellationToken cancellationToken = default )
    {
#pragma warning disable VSTHRD003

        var shortDelay = TimeSpan.FromSeconds( 5 );
        var shortDelayTask = Task.Delay( 5_000, cancellationToken );

        if ( await Task.WhenAny( shortDelayTask, Task.WhenAll( this._pendingTasks.Values.Select( x => x.Task ) ) ) == shortDelayTask )
        {
            this._logger.Warning?.Log(
                $"The following tasks take more than {shortDelay} to complete: " + string.Join(
                    ", ",
                    this._pendingTasks.SelectAsReadOnlyCollection( x => x.Value.Func.ToString() ) ) );
        }

        if ( cancellationToken.CanBeCanceled )
        {
            await Task.WhenAll( this._pendingTasks.Values.Select( x => x.Task ) ).WithCancellation( cancellationToken );
        }
        else
        {
            // Avoid blocking forever in case of bug.

            using var timeout = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );
            await Task.WhenAll( this._pendingTasks.Values.Select( x => x.Task ) ).WithCancellation( timeout.Token );
        }
    }

    internal bool IsEmpty => this._pendingTasks.IsEmpty;
}