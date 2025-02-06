// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal interface IRedisConnectionFactory
{
    IConnectionMultiplexer GetConnection( IServiceProvider? serviceProvider );

    Task<IConnectionMultiplexer> GetConnectionAsync(
        IServiceProvider? serviceProvider,
        bool logRedisConnection,
        CancellationToken cancellationToken );
}