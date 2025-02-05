// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

internal class SingleThreadedTaskRunner : IConcurrentTaskRunner
{
    public Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken )
        where T : notnull
    {
        var orderedItems = this.GetOrderedItems( items );

        try
        {
            foreach ( var item in orderedItems )
            {
                action( item );
            }

            return Task.CompletedTask;
        }
        catch ( Exception e )
        {
            return Task.FromException( e );
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
        var orderedItems = this.GetOrderedItems( items );

        try
        {
            using var context = createContext();

            foreach ( var item in orderedItems )
            {
                action( item, context );
            }

            return Task.CompletedTask;
        }
        catch ( Exception e )
        {
            return Task.FromException( e );
        }
    }

    public async Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Func<T, Task> action, CancellationToken cancellationToken )
        where T : notnull
    {
        var orderedItems = this.GetOrderedItems( items );

        foreach ( var item in orderedItems )
        {
            await action( item );
        }
    }

    protected virtual IEnumerable<T> GetOrderedItems<T>( IEnumerable<T> items )
        where T : notnull
        => items;
}