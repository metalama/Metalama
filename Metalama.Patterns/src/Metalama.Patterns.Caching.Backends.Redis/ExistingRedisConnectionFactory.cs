// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal sealed class ExistingRedisConnectionFactory( IConnectionMultiplexer connection ) : IRedisConnectionFactory
{
    public IConnectionMultiplexer GetConnection( IServiceProvider? serviceProvider ) => connection;

    public Task<IConnectionMultiplexer> GetConnectionAsync( IServiceProvider? serviceProvider, bool logRedisConnection, CancellationToken cancellationToken )
        => Task.FromResult( connection );
}