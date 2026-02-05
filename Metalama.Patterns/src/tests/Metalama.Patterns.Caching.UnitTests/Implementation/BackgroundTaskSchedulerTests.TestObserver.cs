// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Implementation;

public sealed partial class BackgroundTaskSchedulerTests
{
    /// <summary>
    /// A test observer to track task lifecycle events.
    /// </summary>
    private sealed class TestObserver : IBackgroundTaskSchedulerObserver
    {
        public ConcurrentBag<int> EnqueuedIds { get; } = new();

        public ConcurrentBag<int> CompletedIds { get; } = new();

        private int _nextId;

        public int OnTaskEnqueued()
        {
            var id = Interlocked.Increment( ref this._nextId );
            this.EnqueuedIds.Add( id );

            return id;
        }

        public void OnTaskCompleted( int observedTaskId )
        {
            this.CompletedIds.Add( observedTaskId );
        }
    }
}