// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Redis;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

internal sealed class RedisBackendObserver : IRedisBackendObserver
{
    private int _activeNotificationThreads;
    private int _createdNotificationThreads;

    public int ActiveNotificationThreads => this._activeNotificationThreads;

    public int CreatedNotificationThreads => this._createdNotificationThreads;

    public void OnNotificationThreadStarted()
    {
        Interlocked.Increment( ref this._activeNotificationThreads );
        Interlocked.Increment( ref this._createdNotificationThreads );
    }

    public void OnNotificationThreadCompleted() => Interlocked.Decrement( ref this._activeNotificationThreads );
}