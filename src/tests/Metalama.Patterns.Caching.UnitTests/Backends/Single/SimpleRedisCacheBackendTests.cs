// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Redis;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Backends.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

// ReSharper disable once UnusedType.Global
public class SimpleRedisCacheBackendTests : BaseCacheBackendTests, IAssemblyFixture<RedisAssemblyFixture>
{
    private readonly RedisAssemblyFixture _redisAssemblyFixture;

    public SimpleRedisCacheBackendTests(
        CachingClassFixture cachingClassFixture,
        RedisAssemblyFixture redisAssemblyFixture,
        ITestOutputHelper testOutputHelper ) : base(
        cachingClassFixture,
        testOutputHelper )
    {
        this._redisAssemblyFixture = redisAssemblyFixture;
    }

    protected override void AddServices( ServiceCollection serviceCollection )
    {
        base.AddServices( serviceCollection );
        serviceCollection.AddSingleton<IRedisBackendObserver>( this._observer );
    }

    private readonly RedisBackendObserver _observer = new();

    protected override void Cleanup()
    {
        base.Cleanup();

        AssertEx.Equal( 0, this._observer.ActiveNotificationThreads, "RedisNotificationQueue.NotificationProcessingThreads" );
    }

    protected override bool TestDependencies => false;

    protected override CheckAfterDisposeCachingBackend CreateBackend() => Task.Run( this.CreateBackendAsync ).Result;

    protected override async Task<CheckAfterDisposeCachingBackend> CreateBackendAsync()
    {
        return await this.CreateBackendAsync( null );
    }

    private async Task<CheckAfterDisposeCachingBackend> CreateBackendAsync( string? keyPrefix )
    {
        return new CheckAfterDisposeCachingBackend(
            await RedisFactory.CreateBackendAsync( this.ClassFixture, this._redisAssemblyFixture, serviceProvider: this.ServiceProvider, prefix: keyPrefix ) );
    }
}