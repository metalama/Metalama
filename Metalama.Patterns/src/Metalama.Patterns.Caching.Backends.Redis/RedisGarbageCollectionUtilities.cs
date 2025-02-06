// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

/// <summary>
/// Exposes methods that perform garbage collection in a Redis server used as a cache where dependencies are enabled.
/// </summary>
[PublicAPI]
public static class RedisGarbageCollectionUtilities
{
    /// <summary>
    /// Performs a full garbage collection on all Redis servers of a <see cref="IConnectionMultiplexer"/>.
    /// This operation enumerates and validates all keys in the database, and can possibly last several minutes and affect performance in production.
    /// </summary>
    public static Task PerformFullCollectionAsync(
        IConnectionMultiplexer connection,
        RedisCachingBackendConfiguration? configuration = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default )
    {
        configuration ??= new RedisCachingBackendConfiguration
        {
            ConnectionFactory = new ExistingRedisConnectionFactory( connection ), SupportsDependencies = true, OwnsConnection = false
        };

        var backend = new DependenciesRedisCachingBackend( configuration, serviceProvider );

        return backend.CleanUpAsync( cancellationToken );
    }

    /// <summary>
    /// Performs a full garbage collection on a given Redis server. This operation enumerates and validates all keys in the database, and can possibly last several
    /// minutes and affect performance in production.
    /// </summary>
    public static Task PerformFullCollectionAsync(
        IServer server,
        RedisCachingBackendConfiguration? configuration = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default )
    {
        configuration ??= new RedisCachingBackendConfiguration
        {
            ConnectionFactory = new ExistingRedisConnectionFactory( server.Multiplexer ), SupportsDependencies = true, OwnsConnection = false
        };

        var backend = new DependenciesRedisCachingBackend( configuration, serviceProvider );

        return backend.CleanUpAsync( server, cancellationToken );
    }
}