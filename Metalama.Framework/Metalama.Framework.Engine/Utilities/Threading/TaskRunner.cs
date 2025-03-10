// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

#pragma warning disable VSTHRD002

internal sealed class TaskRunner : ITaskRunner
{
    // We used a shared scheduler to limit concurrency in the whole process.
    private static readonly TaskScheduler _scheduler = TaskSchedulerProvider.TaskScheduler;

    private static bool MustRunNewTask()
        => !Thread.CurrentThread.IsBackground || TaskScheduler.Current != TaskScheduler.Default || SynchronizationContext.Current != null;

    public void RunSynchronously( Func<Task> func, CancellationToken cancellationToken = default )
    {
        if ( MustRunNewTask() )
        {
            Task.Factory.StartNew( func, cancellationToken, TaskCreationOptions.None, _scheduler ).Unwrap().Wait( cancellationToken );
        }
        else
        {
            func().Wait( cancellationToken );
        }
    }

    public void RunSynchronously( Func<ValueTask> func, CancellationToken cancellationToken = default )
    {
        if ( MustRunNewTask() )
        {
            Task.Factory.StartNew( () => func().AsTask(), cancellationToken, TaskCreationOptions.None, _scheduler ).Unwrap().Wait( cancellationToken );
        }
        else
        {
            var valueTask = func();

            if ( !valueTask.IsCompleted )
            {
                valueTask.AsTask().Wait( cancellationToken );
            }
        }
    }

    public T RunSynchronously<T>( Func<Task<T>> func, CancellationToken cancellationToken = default )
    {
        if ( MustRunNewTask() )
        {
            var task = Task.Factory.StartNew( func, cancellationToken, TaskCreationOptions.None, _scheduler ).Unwrap();
            task.Wait( cancellationToken );

            return task.Result;
        }
        else
        {
            var task = func();

            if ( !task.IsCompleted )
            {
                task.Wait( cancellationToken );
            }

            return task.Result;
        }
    }

    public T RunSynchronously<T>( Func<ValueTask<T>> func, CancellationToken cancellationToken = default )
    {
        if ( MustRunNewTask() )
        {
            var task = Task.Factory.StartNew( () => func().AsTask(), cancellationToken, TaskCreationOptions.None, _scheduler ).Unwrap();

            task.Wait( cancellationToken );

            return task.Result;
        }
        else
        {
            var valueTask = func();

            if ( valueTask.IsCompleted )
            {
                return valueTask.Result;
            }
            else
            {
                var task = valueTask.AsTask();
                task.Wait( cancellationToken );

                return task.Result;
            }
        }
    }
}