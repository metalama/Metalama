// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

internal sealed class ConcurrentTaskRunner : IConcurrentTaskRunner, IDisposable
{
    private static readonly TaskScheduler _scheduler = TaskSchedulerProvider.TaskScheduler;

    public Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken )
        where T : notnull
    {
        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return Task.CompletedTask;
        }

        var item1 = enumerator.Current;

        if ( !enumerator.MoveNext() )
        {
            action( item1 );

            return Task.CompletedTask;
        }

        var queue = new ConcurrentQueue<T>();
        queue.Enqueue( item1 );
        queue.Enqueue( enumerator.Current );

        while ( enumerator.MoveNext() )
        {
            queue.Enqueue( enumerator.Current );
        }

        // Start tasks to process the queue.
        var taskCount = Math.Min( Environment.ProcessorCount, queue.Count );
        var tasks = new Task[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            tasks[i] = Task.Factory.StartNew( ProcessQueue, cancellationToken, TaskCreationOptions.None, _scheduler );
        }

        // Await all tasks.
        return Task.WhenAll( tasks );

        // Process the queue.
        void ProcessQueue()
        {
            while ( queue.TryDequeue( out var item ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                action( item );
            }
        }
    }

    public Task RunConcurrentlyAsync<TItem, TContext>(
        IEnumerable<TItem> items,
        Action<TItem, TContext> action,
        Func<TContext> createContext,
        CancellationToken cancellationToken )
        where TItem : notnull
        where TContext : IDisposable
    {
        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return Task.CompletedTask;
        }

        var item1 = enumerator.Current;

        if ( !enumerator.MoveNext() )
        {
            using var context = createContext();
            action( item1, context );

            return Task.CompletedTask;
        }

        var queue = new ConcurrentQueue<TItem>();
        queue.Enqueue( item1 );
        queue.Enqueue( enumerator.Current );

        while ( enumerator.MoveNext() )
        {
            queue.Enqueue( enumerator.Current );
        }

        // Start tasks to process the queue.
        var taskCount = Math.Min( Environment.ProcessorCount, queue.Count );
        var tasks = new Task[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            tasks[i] = Task.Factory.StartNew( ProcessQueue, cancellationToken, TaskCreationOptions.None, _scheduler );
        }

        // Await all tasks.
        return Task.WhenAll( tasks );

        // Process the queue.
        void ProcessQueue()
        {
            using var context = createContext();

            while ( queue.TryDequeue( out var item ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                action( item, context );
            }
        }
    }

    public Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Func<T, Task> action, CancellationToken cancellationToken )
        where T : notnull
    {
        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return Task.CompletedTask;
        }

        var item1 = enumerator.Current;

        if ( !enumerator.MoveNext() )
        {
            return action( item1 );
        }

        var queue = new ConcurrentQueue<T>();
        queue.Enqueue( item1 );
        queue.Enqueue( enumerator.Current );

        while ( enumerator.MoveNext() )
        {
            queue.Enqueue( enumerator.Current );
        }

        var taskCount = Math.Min( Environment.ProcessorCount, queue.Count );
        var tasks = new Task[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            tasks[i] = Task.Factory.StartNew( ProcessQueueAsync, cancellationToken, TaskCreationOptions.None, _scheduler ).Unwrap();
        }

        return Task.WhenAll( tasks );

        async Task ProcessQueueAsync()
        {
            while ( queue.TryDequeue( out var item ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action( item );
            }
        }
    }

    public void Dispose() { }
}