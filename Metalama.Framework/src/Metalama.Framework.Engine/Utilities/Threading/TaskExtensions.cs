// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

public static class TaskExtensions
{
    public static Task WithCancellation( this Task task, CancellationToken cancellationToken )
    {
        if ( !cancellationToken.CanBeCanceled || task.IsCompleted )
        {
            return task;
        }

        if ( cancellationToken.IsCancellationRequested )
        {
            return Task.FromCanceled( cancellationToken );
        }

        return task.WithCancellationSlow( false, cancellationToken );
    }

    public static Task<T> WithCancellation<T>( this Task<T> task, CancellationToken cancellationToken )
    {
        if ( !cancellationToken.CanBeCanceled || task.IsCompleted )
        {
            return task;
        }

        if ( cancellationToken.IsCancellationRequested )
        {
            return Task.FromCanceled<T>( cancellationToken );
        }

        return task.WithCancellationSlow( false, cancellationToken );
    }

    public static ValueTask WithCancellation( this ValueTask task, CancellationToken cancellationToken )
    {
        if ( !cancellationToken.CanBeCanceled || task.IsCompleted )
        {
            return task;
        }

        if ( cancellationToken.IsCancellationRequested )
        {
#if NET6_0_OR_GREATER
            return ValueTask.FromCanceled( cancellationToken );
#else
            return new ValueTask(Task.FromCanceled(cancellationToken));
#endif
        }

        return new ValueTask( task.AsTask().WithCancellationSlow( false, cancellationToken ) );
    }

    public static ValueTask<T> WithCancellation<T>( this ValueTask<T> task, CancellationToken cancellationToken )
    {
        if ( !cancellationToken.CanBeCanceled || task.IsCompleted )
        {
            return task;
        }

        if ( cancellationToken.IsCancellationRequested )
        {
#if NET6_0_OR_GREATER
            return ValueTask.FromCanceled<T>( cancellationToken );
#else
            return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
#endif
        }

        return new ValueTask<T>( task.AsTask().WithCancellationSlow( false, cancellationToken ) );
    }

    private static async Task WithCancellationSlow( this Task task, bool continueOnCapturedContext, CancellationToken cancellationToken )
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        var registration = cancellationToken.Register(
            s => ((TaskCompletionSource<bool>) s!).TrySetResult( true ),
            taskCompletionSource );

#if NET6_0_OR_GREATER
        await using ( registration )
#else
        using ( registration )
#endif
        {
            if ( task != await Task.WhenAny( task, taskCompletionSource.Task )
                    .ConfigureAwait( continueOnCapturedContext ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        await task.ConfigureAwait( continueOnCapturedContext );
    }

    private static async Task<T> WithCancellationSlow<T>( this Task<T> task, bool continueOnCapturedContext, CancellationToken cancellationToken )
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        var registration = cancellationToken.Register(
            s => ((TaskCompletionSource<bool>) s!).TrySetResult( true ),
            taskCompletionSource );

#if NET6_0_OR_GREATER
        await using ( registration )
#else
        using ( registration )
#endif
        {
            if ( task != await Task.WhenAny( task, taskCompletionSource.Task )
                    .ConfigureAwait( continueOnCapturedContext ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return await task.ConfigureAwait( continueOnCapturedContext );
    }
}