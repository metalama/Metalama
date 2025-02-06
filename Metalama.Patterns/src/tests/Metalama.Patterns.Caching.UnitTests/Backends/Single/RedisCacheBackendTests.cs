// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Backends.Redis;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Backends.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

public class RedisCacheBackendTests : BaseCacheBackendTests, IAssemblyFixture<RedisAssemblyFixture>
{
    private readonly RedisAssemblyFixture _redisAssemblyFixture;
    private readonly RedisBackendObserver _observer = new();

    public RedisCacheBackendTests(
        CachingClassFixture cachingClassFixture,
#pragma warning disable xUnit1041 // False warning.
        RedisAssemblyFixture redisAssemblyFixture,
#pragma warning restore xUnit1041
        ITestOutputHelper testOutputHelper ) : base(
        cachingClassFixture,
        testOutputHelper )
    {
        this._redisAssemblyFixture = redisAssemblyFixture;
        this.ClassFixture.Endpoint = redisAssemblyFixture.TestInstance.Endpoint;
    }

    protected override void AddServices( ServiceCollection serviceCollection )
    {
        base.AddServices( serviceCollection );
        serviceCollection.AddSingleton<IRedisBackendObserver>( this._observer );
    }

    protected override void Cleanup()
    {
        base.Cleanup();
        
        Assert.NotEqual( 0, this._observer.CreatedNotificationThreads );
        AssertEx.Equal( 0, this._observer.ActiveNotificationThreads, "RedisNotificationQueue.NotificationProcessingThreads" );
    }

    protected virtual bool GarbageCollectorEnabled => false;

    protected override CheckAfterDisposeCachingBackend CreateBackend() => Task.Run( () => this.CreateBackendAsync( null ) ).GetAwaiter().GetResult();

    protected override async Task<CheckAfterDisposeCachingBackend> CreateBackendAsync() => await this.CreateBackendAsync( null );

    protected async Task<CheckAfterDisposeCachingBackend> CreateBackendAsync( string? keyPrefix )
        => await RedisFactory.CreateBackendAsync(
            this.ClassFixture,
            this._redisAssemblyFixture,
            this.ServiceProvider,
            keyPrefix,
            supportsDependencies: true,
            collector: this.GarbageCollectorEnabled );

    protected static string GeneratePrefix()
    {
        var keyPrefix = Guid.NewGuid().ToString();

        return keyPrefix;
    }

    [Fact]
    public void TestDisposeRedisBeforeCaching()
    {
        ServiceCollection serviceCollection = [];
        var connection = RedisFactory.CreateConnection( this.ClassFixture );
        serviceCollection.AddSingleton( connection );
        this.AddServices( serviceCollection );

        var backend = CachingBackend.Create( b => b.Redis(), serviceCollection.BuildServiceProvider() );
        backend.Initialize();
        connection.Dispose();
        backend.Dispose();
    }

    [Fact]
    public async Task TestDisposeRedisBeforeCachingAsync()
    {
        ServiceCollection serviceCollection = [];
        var connection = RedisFactory.CreateConnection( this.ClassFixture );
        serviceCollection.AddSingleton( connection );
        this.AddServices( serviceCollection );

        var backend = CachingBackend.Create( b => b.Redis(), serviceCollection.BuildServiceProvider() );
        await backend.InitializeAsync();
        await connection.DisposeAsync();
        await backend.DisposeAsync();
    }

    [Fact( Timeout = Timeout )]
    public async Task TestCleanUp()
    {
        var keyPrefix = GeneratePrefix();

        // [Porting] Not fixing, can't be certain of original intent (twice).
        // ReSharper disable UseAwaitUsing
        // ReSharper disable once MethodHasAsyncOverload
        await using ( var redisCachingBackend = await this.CreateBackendAsync( keyPrefix ) )
        {
            var cache = (DependenciesRedisCachingBackend) redisCachingBackend.UnderlyingBackend;

            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable MethodHasAsyncOverload
            cache.SetItem( "i1", new CacheItem( "value", ImmutableArray.Create( "d1", "d2", "d3" ) ) );
            cache.SetItem( "i2", new CacheItem( "value", ImmutableArray.Create( "d1", "d2", "d3" ) ) );
            cache.SetItem( "i3", new CacheItem( "value" ) );

            // ReSharper restore MethodHasAsyncOverload

            cache.Database.ListRightPush( GetValueKey( cache, "lonely-value-key" ), [Guid.NewGuid().ToString(), "value"] );
            cache.Database.StringSet( GetDependenciesKey( cache, "lonely-dependencies-key1" ), "non-existing-value-key1" );
            cache.Database.StringSet( GetDependenciesKey( cache, "lonely-dependencies-key2" ), "" );
            cache.Database.SetAdd( GetDependencyKey( cache, "lonely-dependency-key" ), "non-existing-value-key2" );

            cache.Database.ListRightPush(
                GetValueKey( cache, "non-corresponding-version-key" ),
                ["non-corresponding-version1", "value"] );

            cache.Database.StringSet(
                GetDependenciesKey( cache, "non-corresponding-version-key" ),
                "non-corresponding-version2\nnon-corresponding-version-dependency-key" );

            cache.Database.SetAdd( GetDependencyKey( cache, "non-corresponding-version-dependency-key" ), "non-corresponding-version-key" );

            Assert.True( ValueSmallKeyExists( cache, "i1" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "i1" ) );
            Assert.True( ValueSmallKeyExists( cache, "i2" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "i2" ) );
            Assert.True( ValueSmallKeyExists( cache, "i3" ) );
            Assert.False( DependenciesSmallKeyExists( cache, "i3" ) );

            Assert.True( ValueSmallKeyExists( cache, "lonely-value-key" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "lonely-dependencies-key1" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "lonely-dependencies-key2" ) );
            Assert.False( ValueSmallKeyExists( cache, "non-existing-value-key1" ) );
            Assert.False( ValueSmallKeyExists( cache, "non-existing-value-key2" ) );

            Assert.True( ValueSmallKeyExists( cache, "non-corresponding-version-key" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "non-corresponding-version-key" ) );

            Assert.True( DependencySmallKeyExists( cache, "d1" ) );
            Assert.True( DependencySmallKeyExists( cache, "d2" ) );
            Assert.True( DependencySmallKeyExists( cache, "d3" ) );

            Assert.True( DependencySmallKeyExists( cache, "lonely-dependency-key" ) );

            Assert.True( DependencySmallKeyExists( cache, "non-corresponding-version-dependency-key" ) );

            await cache.CleanUpAsync();

            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable MethodHasAsyncOverload
            Assert.NotNull( cache.GetItem( "i1" ) );
            Assert.NotNull( cache.GetItem( "i2" ) );
            Assert.NotNull( cache.GetItem( "i3" ) );

            // ReSharper restore MethodHasAsyncOverload

            Assert.True( ValueSmallKeyExists( cache, "i1" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "i1" ) );
            Assert.True( ValueSmallKeyExists( cache, "i2" ) );
            Assert.True( DependenciesSmallKeyExists( cache, "i2" ) );
            Assert.True( ValueSmallKeyExists( cache, "i3" ) );
            Assert.False( DependenciesSmallKeyExists( cache, "i3" ) );

            Assert.False( ValueSmallKeyExists( cache, "lonely-value-key" ) );
            Assert.False( DependenciesSmallKeyExists( cache, "lonely-dependencies-key1" ) );
            Assert.False( DependenciesSmallKeyExists( cache, "lonely-dependencies-key2" ) );
            Assert.False( ValueSmallKeyExists( cache, "non-existing-value-key1" ) );
            Assert.False( ValueSmallKeyExists( cache, "non-existing-value-key2" ) );

            Assert.False( ValueSmallKeyExists( cache, "non-corresponding-version-key" ) );
            Assert.False( DependenciesSmallKeyExists( cache, "non-corresponding-version-key" ) );

            Assert.True( DependencySmallKeyExists( cache, "d1" ) );
            Assert.True( DependencySmallKeyExists( cache, "d2" ) );
            Assert.True( DependencySmallKeyExists( cache, "d3" ) );

            Assert.False( DependencySmallKeyExists( cache, "lonely-dependency-key" ) );

            Assert.False( DependencySmallKeyExists( cache, "non-corresponding-version-dependency-key" ) );

            await cache.DisposeAsync();

            Assert.Equal( 0, cache.BackgroundTaskExceptions );
        }

        // ReSharper restore UseAwaitUsing
    }

    private static string GetValueKey( DependenciesRedisCachingBackend cache, string smallKey )
    {
        return cache.Configuration.KeyPrefix + ":value:" + smallKey;
    }

    private static string GetDependenciesKey( DependenciesRedisCachingBackend cache, string smallKey )
    {
        return cache.Configuration.KeyPrefix + ":dependencies:" + smallKey;
    }

    private static string GetDependencyKey( DependenciesRedisCachingBackend cache, string smallKey )
    {
        return cache.Configuration.KeyPrefix + ":dependency:" + smallKey;
    }

    private static bool ValueSmallKeyExists( DependenciesRedisCachingBackend cache, string valueSmallKey )
    {
        return cache.Database.KeyExists( GetValueKey( cache, valueSmallKey ) );
    }

    private static bool DependenciesSmallKeyExists( DependenciesRedisCachingBackend cache, string valueSmallKey )
    {
        return cache.Database.KeyExists( GetDependenciesKey( cache, valueSmallKey ) );
    }

    private static bool DependencySmallKeyExists( DependenciesRedisCachingBackend cache, string valueSmallKey )
    {
        return cache.Database.KeyExists( GetDependencyKey( cache, valueSmallKey ) );
    }

    protected IList<string> GetAllKeys( string prefix )
    {
        using var connection = RedisFactory.CreateConnection( this.ClassFixture );

        var servers = connection.GetEndPoints().Select( endpoint => connection.GetServer( endpoint ) ).ToList();
        var keys = servers.SelectMany( server => server.Keys( pattern: prefix + "*" ) ).ToList();

        var filteredKeys = keys.Select( k => k.ToString() ).Where( k => k.IndexOf( ":gc:", StringComparison.Ordinal ) == -1 ).ToList();

        return filteredKeys;
    }
}