// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Backends.Redis;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.LoadTests.Tests;

internal sealed class RedisLoadTest : BaseTestClass<RedisLoadTestConfiguration>
{
    private readonly string _keyPrefix = Guid.NewGuid().ToString();

    public override async Task TestAsync( RedisLoadTestConfiguration configuration, TimeSpan duration )
    {
        Console.WriteLine( "collector init" );

        var collectors = new IHostedService?[configuration.CollectorsCount];

        try
        {
            for ( var i = 0; i < collectors.Length; i++ )
            {
                var collectorConnection = CreateConnection();

                var collectorConfiguration = new RedisCachingBackendConfiguration() { Connection = collectorConnection, KeyPrefix = this._keyPrefix };

                var garbageCollector = RedisCachingFactory.CreateRedisCacheDependencyGarbageCollector( collectorConfiguration );

                await garbageCollector.StartAsync( default );
                collectors[i] = garbageCollector;
            }

            await base.TestAsync( configuration, duration );
        }
        finally
        {
            foreach ( var collector in collectors )
            {
                if ( collector != null )
                {
                    await collector.StopAsync( default );
                }
            }
        }
    }

    private static IConnectionMultiplexer CreateConnection()
    {
        const string redisConnectionStringEnvironmentVariableName = "REDIS_CONNECTION_STRING";
        var redisConnectionString = Environment.GetEnvironmentVariable( redisConnectionStringEnvironmentVariableName );

        if ( string.IsNullOrEmpty( redisConnectionString ) )
        {
            throw new InvalidOperationException(
                $"We use Azure Cache for Redis for this test. We don't keep the service active. To perform the test, create a new Azure Cache for Redis and assign the connection string to '{redisConnectionStringEnvironmentVariableName}' environment variable. This can be done in the debugging profile, if needed." );
        }

        var connection = ConnectionMultiplexer.Connect( redisConnectionString );

        return connection;
    }

    protected override CachingBackend CreateCachingBackend()
    {
        var connection = CreateConnection();

        var configuration =
            new RedisCachingBackendConfiguration { Connection = connection, KeyPrefix = this._keyPrefix, SupportsDependencies = true };

        return CachingBackend.Create( b => b.Redis( configuration ) );
    }
}