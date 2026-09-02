// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Dispatches a work item for execution outside of the calling thread.
/// </summary>
/// <remarks>
/// <para>
/// The caching backends dispatch every event and every background operation through this interface. The default
/// implementation is <see cref="ThreadPoolWorkItemDispatcher"/>, which queues the work item to the thread pool.
/// A different implementation is supplied by registering it in the service provider of the backend.
/// </para>
/// <para>
/// The interface only queues. The ability to wait for the completion of the pending work items belongs to the
/// implementation that a test substitutes, not to this interface.
/// </para>
/// </remarks>
public interface ICachingWorkItemDispatcher
{
    /// <summary>
    /// Queues a work item.
    /// </summary>
    /// <param name="workItem">The delegate to execute.</param>
    /// <param name="state">The object passed to <paramref name="workItem"/>.</param>
    /// <param name="flowExecutionContext">
    /// <see langword="true"/> to flow the execution context of the calling thread to the work item,
    /// <see langword="false"/> to execute the work item without it.
    /// </param>
    void Dispatch( WaitCallback workItem, object? state, bool flowExecutionContext = true );
}
