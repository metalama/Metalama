// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Distributed;

// ReSharper disable once UnusedType.Global
public class RedisDistributedCachingBackendTests(
    CachingClassFixture cachingClassFixture,
    RedisAssemblyFixture redisAssemblyFixture,
    ITestOutputHelper testOutputHelper )
    : BaseDistributedCacheTests( cachingClassFixture, testOutputHelper ), IAssemblyFixture<RedisAssemblyFixture>
{
    protected override async Task<CachingBackend[]> CreateBackendsAsync()
    {
        var prefix = Guid.NewGuid().ToString();

        return
        [
            await RedisFactory.CreateBackendAsync( this.ClassFixture, redisAssemblyFixture, this.ServiceProvider, prefix, supportsDependencies: true ),
            await RedisFactory.CreateBackendAsync( this.ClassFixture, redisAssemblyFixture, this.ServiceProvider, prefix, supportsDependencies: true )
        ];
    }

    protected override CachingBackend[] CreateBackends() => Task.Run( this.CreateBackendsAsync ).Result;

    protected override void ConnectToRedisIfRequired()
    {
        var redisTestInstance = redisAssemblyFixture.TestInstance;
        this.ClassFixture.Endpoint = redisTestInstance.Endpoint;
    }
}