// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Implementation;

public interface IBackgroundTaskSchedulerObserver
{
    int OnTaskEnqueued();

    void OnTaskCompleted( int observedTaskId );
}