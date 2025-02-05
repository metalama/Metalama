// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.VisualStudio.Threading;
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

    internal void Run( Func<Task> asyncAction, CancellationToken cancellationToken = default )
    {
        var taskId = Interlocked.Increment( ref this._nextId );
        var taskCompleted = false;
        var sync = new object();

        var task = Task.Run(
            async () =>
            {
                try
                {
                    await asyncAction();
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
            },
            cancellationToken );

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