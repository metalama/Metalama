// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Internal interface for testable caching components, providing access to background task state.
/// </summary>
internal interface ITestableCachingComponent : IDisposable
{
    /// <summary>
    /// Returns a task that completes when all background tasks have completed.
    /// </summary>
    Task WhenBackgroundTasksCompleted( CancellationToken cancellationToken );

    /// <summary>
    /// Event raised when a background task is enqueued.
    /// </summary>
    event Action? BackgroundTaskEnqueued;

    /// <summary>
    /// Disposes the component asynchronously.
    /// </summary>
    Task DisposeAsync( CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets the number of background task exceptions that occurred.
    /// </summary>
    int BackgroundTaskExceptions { get; }
}