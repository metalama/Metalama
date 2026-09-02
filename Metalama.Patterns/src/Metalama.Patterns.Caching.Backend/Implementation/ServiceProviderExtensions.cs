// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.DependencyInjection;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Resolves the dependencies that every caching component shares: the clock and the work-item dispatcher.
/// </summary>
/// <remarks>
/// Both dependencies have a production implementation that is used when the service provider supplies none, so
/// neither is ever absent.
/// </remarks>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the <see cref="TimeProvider"/> of the given service provider, or <see cref="TimeProvider.System"/> when
    /// the service provider supplies none.
    /// </summary>
    public static TimeProvider GetTimeProvider( this IServiceProvider? serviceProvider )
        => serviceProvider?.GetService<TimeProvider>() ?? TimeProvider.System;

    /// <summary>
    /// Gets the <see cref="ICachingWorkItemDispatcher"/> of the given service provider, or
    /// <see cref="ThreadPoolWorkItemDispatcher.Instance"/> when the service provider supplies none.
    /// </summary>
    public static ICachingWorkItemDispatcher GetWorkItemDispatcher( this IServiceProvider? serviceProvider )
        => serviceProvider?.GetService<ICachingWorkItemDispatcher>() ?? ThreadPoolWorkItemDispatcher.Instance;

    /// <summary>
    /// Queues an asynchronous work item and returns a <see cref="Task"/> that completes when the work item completes.
    /// </summary>
    /// <remarks>
    /// This is the equivalent of <see cref="Task.Run(Func{Task},CancellationToken)"/> for a caller that dispatches
    /// through an <see cref="ICachingWorkItemDispatcher"/>.
    /// </remarks>
    /// <param name="dispatcher">The dispatcher.</param>
    /// <param name="function">A function that starts the asynchronous work item.</param>
    /// <param name="cancellationToken">A token that cancels the work item before it starts.</param>
    public static Task RunAsync( this ICachingWorkItemDispatcher dispatcher, Func<Task> function, CancellationToken cancellationToken )
    {
        var taskCompletionSource = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        if ( cancellationToken.IsCancellationRequested )
        {
            taskCompletionSource.TrySetCanceled( cancellationToken );

            return taskCompletionSource.Task;
        }

        dispatcher.Dispatch(
            _ =>
            {
                try
                {
                    function()
                        .ContinueWith(
                            ( task, state ) => Complete( task, (TaskCompletionSource<bool>) state! ),
                            taskCompletionSource,
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default );
                }
                catch ( Exception exception )
                {
                    taskCompletionSource.TrySetException( exception );
                }
            },
            null );

        return taskCompletionSource.Task;
    }

    private static void Complete( Task task, TaskCompletionSource<bool> taskCompletionSource )
    {
        if ( task.IsCanceled )
        {
            taskCompletionSource.TrySetCanceled();
        }
        else if ( task.Exception != null )
        {
            taskCompletionSource.TrySetException( task.Exception.InnerExceptions );
        }
        else
        {
            taskCompletionSource.TrySetResult( true );
        }
    }
}
