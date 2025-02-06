// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Backends.Distributed;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

public class TwoLayerCachingBackendTests : BaseCacheBackendTests, IAssemblyFixture<RedisAssemblyFixture>
{
    private readonly RedisAssemblyFixture _redisAssemblyFixture;

    // ReSharper disable once MemberCanBeProtected.Global
    public TwoLayerCachingBackendTests(
        RedisAssemblyFixture redisAssemblyFixture,
        CachingClassFixture cachingClassFixture,
        ITestOutputHelper testOutputHelper ) : base(
        cachingClassFixture,
        testOutputHelper )
    {
        this._redisAssemblyFixture = redisAssemblyFixture;
    }

    protected override CheckAfterDisposeCachingBackend CreateBackend() => Task.Run( this.CreateBackendAsync ).Result;

    protected override async Task<CheckAfterDisposeCachingBackend> CreateBackendAsync()
    {
        var redis = await RedisFactory.CreateBackendAsync(
            this.ClassFixture,
            this._redisAssemblyFixture,
            serviceProvider: this.ServiceProvider,
            supportsDependencies: true );

#pragma warning disable CS0618 // Type or member is obsolete
        var backend = CachingBackend.Create(
            b => b.Specific( redis.UnderlyingBackend )
                .WithL1(
                    new LayeredCachingBackendConfiguration
                    {
                        L1Configuration = new MemoryCachingBackendConfiguration { DebugName = "Local" }, DebugName = "TwoLayer"
                    } ) );
#pragma warning restore CS0618 // Type or member is obsolete

        return new CheckAfterDisposeCachingBackend( backend );
    }
}