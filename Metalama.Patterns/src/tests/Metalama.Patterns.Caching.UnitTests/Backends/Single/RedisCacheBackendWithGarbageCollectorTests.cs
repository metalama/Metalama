// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Redis;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

public sealed class RedisCacheBackendWithGarbageCollectorTests : RedisCacheBackendTests
{
    public RedisCacheBackendWithGarbageCollectorTests(
        CachingClassFixture cachingClassFixture,
#pragma warning disable xUnit1041 // False warning.
        RedisAssemblyFixture redisAssemblyFixture,
#pragma warning restore xUnit1041
        ITestOutputHelper testOutputHelper ) : base( cachingClassFixture, redisAssemblyFixture, testOutputHelper ) { }

    protected override bool GarbageCollectorEnabled => true;

    [Fact( Timeout = Timeout )]
    public async Task TestGarbageCollectionAsync()
    {
        var prefix = GeneratePrefix();

        Assert.Empty( this.GetAllKeys( prefix ) );

        await using ( var cache = await this.CreateBackendAsync( prefix ) )
        {
            await cache.SetItemAsync( "i1", new CacheItem( "value", ImmutableArray.Create( "d1", "d2", "d3" ) ) );
            await cache.SetItemAsync( "i2", new CacheItem( "value", ImmutableArray.Create( "d1", "d2", "d3" ) ) );
            await cache.SetItemAsync( "i3", new CacheItem( "value", ImmutableArray.Create( "d1", "d2", "d3" ) ) );

            // ReSharper restore MethodHasAsyncOverload

            Assert.NotEmpty( this.GetAllKeys( prefix ) );

            await cache.InvalidateDependencyAsync( "d1" );
        }

        // ReSharper restore UseAwaitUsing

        // Make sure we dispose the back-end so that the GC key gets removed too.

        var keys = this.GetAllKeys( prefix );

        this.TestOutputHelper.WriteLine( "Remaining keys:" + string.Join( ", ", keys ) );

        Assert.Empty( keys );
    }

    [Fact( Timeout = Timeout )]
    public async Task TestGarbageCollectionByExpiration()
    {
        var keyPrefix = GeneratePrefix();

        // [Porting] Not fixing, can't be certain of original intent.
        // ReSharper disable UseAwaitUsing
        await using ( var cache = await this.CreateBackendAsync( keyPrefix ) )
        {
            var redisBackend = (DependenciesRedisCachingBackend) cache.UnderlyingBackend;
            var collector = redisBackend.Collector!;

            const string valueSmallKey = "i";
            const string dependencySmallKey = "d";
            var offset = this.GetExpirationQuantum();

            var keyBuilder = new RedisKeyBuilder( redisBackend.Database, redisBackend.Configuration );

            var valueKey = keyBuilder.GetValueKey( valueSmallKey );
            var dependenciesKey = keyBuilder.GetDependenciesKey( valueSmallKey );
            var dependencyKey = keyBuilder.GetDependencyKey( dependencySmallKey );

            collector.NotificationQueueProcessor.SuspendProcessing();

            var itemExpiredEvent = new TaskCompletionSource<bool>();

            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            cache.ItemRemoved += ( _, args ) =>
            {
                Assert.Equal( valueSmallKey, args.Key );
                Assert.Equal( CacheItemRemovedReason.Expired, args.RemovedReason );
                Assert.False( itemExpiredEvent.Task.IsCompleted );
                itemExpiredEvent.SetResult( true );
            };

            var cacheItem = new CacheItem(
                "v",
                ImmutableArray.Create( dependencySmallKey ),
                new CacheItemConfiguration { AbsoluteExpiration = offset } );

            await cache.SetItemAsync( valueSmallKey, cacheItem );

            Assert.True( await redisBackend.Database.KeyExistsAsync( valueKey ) );
            Assert.True( await redisBackend.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.True( await redisBackend.Database.KeyExistsAsync( dependencyKey ) );

            collector.NotificationQueueProcessor.ResumeProcessing();

            Assert.True( await itemExpiredEvent.Task.WithTimeout( TimeoutTimeSpan ) );

            await Task.Delay( this.GetExpirationQuantum( 2 ) );

            Assert.False( await redisBackend.Database.KeyExistsAsync( valueKey ) );
            Assert.False( await redisBackend.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.False( await redisBackend.Database.KeyExistsAsync( dependencyKey ) );
        }

        // ReSharper restore UseAwaitUsing
    }

    [Fact( Timeout = Timeout )]
    public async Task TestSetBeforeGarbageCollectionByExpiration()
    {
        var keyPrefix = GeneratePrefix();

        await using ( var checkAfterDisposeCachingBackend = await this.CreateBackendAsync( keyPrefix ) )
        {
            var cache = (DependenciesRedisCachingBackend) checkAfterDisposeCachingBackend.UnderlyingBackend;

            const string valueSmallKey = "i";
            const string dependencySmallKey = "d";
            var offset = this.GetExpirationQuantum();

            var keyBuilder = new RedisKeyBuilder( cache.Database, cache.Configuration );

            string? valueKey = keyBuilder.GetValueKey( valueSmallKey );
            string? dependenciesKey = keyBuilder.GetDependenciesKey( valueSmallKey );
            string? dependencyKey = keyBuilder.GetDependencyKey( dependencySmallKey );

            var collector = cache.Collector!;
            collector.NotificationQueueProcessor.SuspendProcessing();

            var expiringCacheItem = new CacheItem(
                "v",
                ImmutableArray.Create( dependencySmallKey ),
                new CacheItemConfiguration { AbsoluteExpiration = offset } );

            var nonExpiringCacheItem = new CacheItem(
                "v",
                ImmutableArray.Create( dependencySmallKey ) );

            await cache.SetItemAsync( valueSmallKey, expiringCacheItem );

            Assert.True( await cache.Database.KeyExistsAsync( valueKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependencyKey ) );

            await Task.Delay( this.GetExpirationQuantum( 2 ) );

            Assert.False( await cache.Database.KeyExistsAsync( valueKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependencyKey ) );

            await cache.SetItemAsync( valueSmallKey, nonExpiringCacheItem );

            Assert.True( await cache.Database.KeyExistsAsync( valueKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependencyKey ) );

            collector.NotificationQueueProcessor.ResumeProcessing();

            Assert.True( await cache.Database.KeyExistsAsync( valueKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependenciesKey ) );
            Assert.True( await cache.Database.KeyExistsAsync( dependencyKey ) );
        }
    }
}