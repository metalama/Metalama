// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.TestHelpers;

/// <summary>
/// Extension methods for <see cref="ITestableCachingComponent"/>.
/// </summary>
internal static class TestableCachingComponentExtensions
{
    /// <summary>
    /// Returns a task that completes when all background tasks have completed and the component has been quiet for 1 second.
    /// </summary>
    public static Task WhenBackgroundTasksCompletedAndQuietAsync(
        this ITestableCachingComponent component,
        CancellationToken cancellationToken = default )
        => component.WhenBackgroundTasksCompletedAndQuietAsync( TimeSpan.FromSeconds( 1 ), cancellationToken );

    /// <summary>
    /// Returns a task that completes when all background tasks have completed and the component has been quiet for a specified period.
    /// </summary>
    public static async Task WhenBackgroundTasksCompletedAndQuietAsync(
        this ITestableCachingComponent component,
        TimeSpan quietPeriod,
        CancellationToken cancellationToken = default )
    {
        var events = 0;

        void Handler() => Interlocked.Increment( ref events );

        component.BackgroundTaskEnqueued += Handler;

        try
        {
            while ( true )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var eventsBefore = events;
                await component.WhenBackgroundTasksCompleted( cancellationToken );
                await Task.Delay( quietPeriod, cancellationToken );

                if ( events == eventsBefore )
                {
                    return;
                }
            }
        }
        finally
        {
            component.BackgroundTaskEnqueued -= Handler;
        }
    }
}