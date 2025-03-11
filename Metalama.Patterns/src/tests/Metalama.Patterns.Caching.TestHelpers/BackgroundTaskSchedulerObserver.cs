// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metalama.Patterns.Caching.TestHelpers;

public sealed class BackgroundTaskSchedulerObserver : IBackgroundTaskSchedulerObserver
{
    private readonly ConcurrentDictionary<int, StackTrace> _pendingBackgroundTasks = new();
    private volatile int _nextTaskId;

    public int OnTaskEnqueued()
    {
        var id = Interlocked.Increment( ref this._nextTaskId );
        this._pendingBackgroundTasks.TryAdd( id, new StackTrace() );

        return id;
    }

    public void OnTaskCompleted( int observedTaskId ) => this._pendingBackgroundTasks.TryRemove( observedTaskId, out _ );

    internal IEnumerable<StackTrace> PendingTasks => this._pendingBackgroundTasks.Values;
}