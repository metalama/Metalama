// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

[PublicAPI]
public static class LongTaskHelper
{
    public static Task WarnIfLongAsync( this Task task, ILogger? logger, string taskDescription, CancellationToken cancellationToken )
    {
        if ( task.IsCompleted || task.IsFaulted || task.IsCanceled )
        {
            return task;
        }

        var warnings = logger?.Warning;

        if ( warnings == null )
        {
            return task;
        }

        return WarnIfTaskLongCoreAsync( task, warnings, taskDescription, cancellationToken );
    }

    public static ValueTask WarnIfLongAsync( this ValueTask task, ILogger? logger, string taskDescription, CancellationToken cancellationToken )
    {
        if ( task.IsCompleted || task.IsFaulted || task.IsCanceled )
        {
            return task;
        }

        var warnings = logger?.Warning;

        if ( warnings == null )
        {
            return task;
        }

        return WarnIfValueTaskLongCoreAsync( task, warnings, taskDescription, cancellationToken );
    }

    private static async Task WarnIfTaskLongCoreAsync( Task task, ILogWriter logger, string taskDescription, CancellationToken cancellationToken )
    {
        var delayTask = Task.Delay( 1000, cancellationToken );

        if ( await Task.WhenAny( delayTask, task ) == delayTask )
        {
            logger.Log( $"The task is taking a long time: '{taskDescription}'." );

            await task;
        }
    }

    private static async ValueTask WarnIfValueTaskLongCoreAsync(
        ValueTask task,
        ILogWriter logger,
        string taskDescription,
        CancellationToken cancellationToken )
    {
        var delayTask = Task.Delay( 1000, cancellationToken );

        if ( await Task.WhenAny( delayTask, task.AsTask() ) == delayTask )
        {
            logger.Log( $"The task is taking a long time: '{taskDescription}'." );

            await task;
        }
    }

    public static Task<T> WarnIfLongAsync<T>( this Task<T> task, ILogger? logger, string taskDescription, CancellationToken cancellationToken )
    {
        if ( task.IsCompleted || task.IsFaulted || task.IsCanceled )
        {
            return task;
        }

        var warnings = logger?.Warning;

        if ( warnings == null )
        {
            return task;
        }

        return WarnIfTaskLongCoreAsync( task, warnings, taskDescription, cancellationToken );
    }

    public static ValueTask<T> WarnIfLongAsync<T>( this ValueTask<T> task, ILogger? logger, string taskDescription, CancellationToken cancellationToken )
    {
        if ( task.IsCompleted || task.IsFaulted || task.IsCanceled )
        {
            return task;
        }

        var warnings = logger?.Warning;

        if ( warnings == null )
        {
            return task;
        }

        return WarnIfValueTaskLongCoreAsync( task, warnings, taskDescription, cancellationToken );
    }

    private static async Task<T> WarnIfTaskLongCoreAsync<T>( Task<T> task, ILogWriter logger, string taskDescription, CancellationToken cancellationToken )
    {
        var delayTask = Task.Delay( 1000, cancellationToken );

        if ( await Task.WhenAny( delayTask, task ) == delayTask )
        {
            logger.Log( $"The task is taking a long time: '{taskDescription}'." );
        }

        return await task;
    }

    private static async ValueTask<T> WarnIfValueTaskLongCoreAsync<T>(
        ValueTask<T> task,
        ILogWriter logger,
        string taskDescription,
        CancellationToken cancellationToken )
    {
        var delayTask = Task.Delay( 1000, cancellationToken );

        if ( await Task.WhenAny( delayTask, task.AsTask() ) == delayTask )
        {
            logger.Log( $"The task is taking a long time: '{taskDescription}'." );
        }

        return await task;
    }
}