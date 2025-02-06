// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal sealed class ServiceProviderRedisConnectionFactory : IRedisConnectionFactory
{
    public static ServiceProviderRedisConnectionFactory Instance { get; } = new();

    private ServiceProviderRedisConnectionFactory() { }

    public IConnectionMultiplexer GetConnection( IServiceProvider? serviceProvider )
    {
        if ( serviceProvider == null )
        {
            throw new InvalidOperationException( "An IServiceProvider is required." );
        }

        return serviceProvider.GetRequiredService<IConnectionMultiplexer>();
    }

    public Task<IConnectionMultiplexer> GetConnectionAsync( IServiceProvider? serviceProvider, bool logRedisConnection, CancellationToken cancellationToken )
        => Task.FromResult( this.GetConnection( serviceProvider ) );
}