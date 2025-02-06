// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Backends.Redis;

internal interface IRedisBackendObserver
{
    void OnNotificationThreadStarted();

    void OnNotificationThreadCompleted();
}