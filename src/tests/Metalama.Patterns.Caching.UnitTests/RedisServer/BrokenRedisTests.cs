// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Backends.Redis;
using Metalama.Patterns.Caching.TestHelpers;
using StackExchange.Redis;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.RedisServer;

public sealed class BrokenRedisTests
{
    [Fact( Timeout = 20000 )]
    public async Task TestWeAbortConnection()
    {
        try
        {
            var connection = CreateConnection( false );

            var configuration =
                new RedisCachingBackendConfiguration
                {
                    Connection = connection,
                    KeyPrefix = Guid.NewGuid().ToString(),
                    OwnsConnection = true,
                    SupportsDependencies = false,
                    ConnectionTimeout = TimeSpan.FromMilliseconds( 1000 )
                };

            await using var backend = CachingBackend.Create( b => b.Redis( configuration ) );

            using var cancellation = new CancellationTokenSource( 20 );

            await backend.InitializeAsync( cancellation.Token );

            Assert.Fail( "An OperationCanceledException or RedisConnectionException was expected but we got no exception." );
        }
        catch ( OperationCanceledException ) { }
        catch ( RedisConnectionException ) { }
        catch ( Exception e )
        {
            Assert.Fail( $"An OperationCanceledException or RedisConnectionException was expected but we got {e.GetType()}: {e.Message}" );
        }

        // Make sure there are no deadlocks in finalizers.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void TestRedisAbortsConnection()
    {
        AssertEx.Throws<RedisConnectionException>( () => _ = CreateConnection( true ) );
    }

    private static IConnectionMultiplexer CreateConnection( bool redisAborts )
    {
        var socketManager = new SocketManager( "BrokenTest" );

        var redisConfigurationOptions = new ConfigurationOptions();
        redisConfigurationOptions.EndPoints.Add( "192.168.45.127:12345" );
        redisConfigurationOptions.AbortOnConnectFail = redisAborts;
        redisConfigurationOptions.ConnectTimeout = 1000;
        redisConfigurationOptions.SocketManager = socketManager;

        var connection = ConnectionMultiplexer.Connect( redisConfigurationOptions, Console.Out );

        return connection;
    }
}