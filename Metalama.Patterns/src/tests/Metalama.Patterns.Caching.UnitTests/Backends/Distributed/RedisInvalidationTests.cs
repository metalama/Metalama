// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Redis;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Distributed;

// ReSharper disable once UnusedType.Global
public class RedisInvalidationTests : BaseInvalidationBrokerTests, IAssemblyFixture<RedisAssemblyFixture>
{
    private readonly RedisAssemblyFixture _redisAssemblyFixture;

    public RedisInvalidationTests(
        CachingClassFixture cachingClassFixture,
        RedisAssemblyFixture redisAssemblyFixture,
        ITestOutputHelper testOutputHelper ) : base(
        cachingClassFixture,
        testOutputHelper )
    {
        this._redisAssemblyFixture = redisAssemblyFixture;
    }

    protected override void ConnectToRedisIfRequired()
    {
        RedisFactory.CreateTestInstance( this.ClassFixture, this._redisAssemblyFixture );
    }

    protected override ConcreteCachingBackendBuilder AddInvalidationBroker( MemoryCachingBackendBuilder builder, string prefix )
        => builder.WithRedisSynchronization(
            new RedisCacheSynchronizerConfiguration() { Connection = RedisFactory.CreateConnection( this.ClassFixture ), Prefix = prefix } );
}