// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Single
{
    public sealed class NullCacheBackendTests
    {
        [Fact]
        public void TestMiss()
        {
            using ( var cache = CachingBackend.Create( b => b.Null() ) )
            {
                const string key = "0";

                var retrievedItem = cache.GetItem( key );

                AssertEx.Null( retrievedItem, "The cache does not return null on miss." );
            }
        }

        [Fact]
        public void TestSet()
        {
            using ( var cache = CachingBackend.Create( b => b.Null() ) )
            {
                var storedValue0 = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem0 = new CacheItem( storedValue0 );

                cache.SetItem( key, cacheItem0 );

                var retrievedItem = cache.GetItem( key );

                AssertEx.Null( retrievedItem, "The item has been stored in the cache." );
            }
        }
    }
}