// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// The implementation of <see cref="IWorkItemDispatcher"/> that queues the work item to the thread pool.
/// This is the implementation used when the service provider of the backend supplies no other one.
/// </summary>
public sealed class ThreadPoolWorkItemDispatcher : IWorkItemDispatcher
{
    /// <summary>
    /// Gets the single instance of the <see cref="ThreadPoolWorkItemDispatcher"/> class.
    /// </summary>
    public static ThreadPoolWorkItemDispatcher Instance { get; } = new();

    private ThreadPoolWorkItemDispatcher() { }

    /// <inheritdoc />
    public void Dispatch( WaitCallback workItem, object? state, bool flowExecutionContext = true )
    {
        if ( flowExecutionContext )
        {
            ThreadPool.QueueUserWorkItem( workItem, state );
        }
        else
        {
            ThreadPool.UnsafeQueueUserWorkItem( workItem, state );
        }
    }
}
