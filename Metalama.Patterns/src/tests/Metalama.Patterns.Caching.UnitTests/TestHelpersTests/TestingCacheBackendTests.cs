// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.TestHelpersTests
{
    // TODO: as we have prepared the generalization of testing of the CacheBackend ancestors, we should remove the respective test from this class.
    // (See MemoryCacheBackendTests.)

    public sealed class TestingCacheBackendTests : BaseCachingTests
    {
        private const string _namePrefix = "Caching.Tests.Backends.TestingCacheBackendTests_";

        public TestingCacheBackendTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

        [Fact]
        public void TestContainsKey()
        {
            using ( var cache = new TestingCacheBackend( _namePrefix + "TestContainsKey", this.ServiceProvider ) )
            {
                const string key = "0";

                cache.ExpectedContainsKeyCount = 1;
                var keyContained = cache.ContainsItem( key );
                cache.AssertAndReset( "When checkin existence of a non-existing key." );

                Assert.False( keyContained, "The cache does not return false on miss." );

                var storedValue0 = new CachedValueClass( 0 );
                var cacheItem0 = new CacheItem( storedValue0 );

                cache.ExpectedSetCount = 1;
                cache.SetItem( key, cacheItem0 );
                cache.AssertAndReset( "When setting the cache item" );

                cache.ExpectedContainsKeyCount = 1;
                keyContained = cache.ContainsItem( key );
                cache.AssertAndReset( "When checkin existence of an existing key." );

                Assert.True( keyContained, "The cache does not return true on hit." );
            }
        }

        [Fact]
        public void TestGet()
        {
            using ( var cache = new TestingCacheBackend( _namePrefix + "TestGet", this.ServiceProvider ) )
            {
                const string key = "0";

                cache.ExpectedGetCount = 1;
                var retrievedValue = cache.GetItem( key );
                cache.AssertAndReset( "When getting the cached value" );

                AssertEx.Null( retrievedValue, "The cache does not return null on miss." );
            }
        }

        [Fact]
        public void TestSet()
        {
            using ( var cache = new TestingCacheBackend( _namePrefix + "TestSet", this.ServiceProvider ) )
            {
                var storedValue0 = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem0 = new CacheItem( storedValue0 );

                cache.ExpectedSetCount = 1;
                cache.SetItem( key, cacheItem0 );
                cache.AssertAndReset( "When setting the cache item" );

                cache.ExpectedGetCount = 1;
                var retrievedItem = cache.GetItem( key );
                cache.AssertAndReset( "When getting the cache item" );
                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );
                AssertEx.Equal( cacheItem0.Value, retrievedItem.Value, "The retrieved item is not equal to the stored item." );

                var storedValue1 = new CachedValueClass( 1 );
                var cacheItem1 = new CacheItem( storedValue1 );

                cache.ExpectedSetCount = 1;
                cache.SetItem( key, cacheItem1 );
                cache.AssertAndReset( "When setting the second item" );

                retrievedItem = cache.GetItem( key );
                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );
                AssertEx.NotEqual( cacheItem0, retrievedItem, "The item has not been changed." );
            }
        }

        [Fact]
        public void TestInvalidateObjectDependency()
        {
            using ( var cache = new TestingCacheBackend( _namePrefix + "TestInvalidateObjectDependency", this.ServiceProvider ) )
            {
                const string dependencyKey = "0";

                cache.ExpectedInvalidateCount = 1;
                cache.InvalidateDependency( dependencyKey );
                cache.AssertAndReset( "When invalidating dependency" );
            }
        }
    }
}