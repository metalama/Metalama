// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal struct RedisNotification
{
    public RedisChannel Channel;
    public RedisValue Value;
}