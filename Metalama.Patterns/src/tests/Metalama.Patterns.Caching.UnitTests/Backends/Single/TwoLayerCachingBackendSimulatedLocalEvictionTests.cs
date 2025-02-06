// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single
{
    // ReSharper disable once UnusedType.Global
    public sealed class TwoLayerCachingBackendSimulatedLocalEvictionTests : TwoLayerCachingBackendTests
    {
        public TwoLayerCachingBackendSimulatedLocalEvictionTests(
            RedisAssemblyFixture redisAssemblyFixture,
            CachingClassFixture cachingClassFixture,
            ITestOutputHelper testOutputHelper ) : base(
            redisAssemblyFixture,
            cachingClassFixture,
            testOutputHelper ) { }

        protected override void GiveChanceToResetLocalCache( CachingBackend backend )
        {
            backend.Clear( ClearCacheOptions.Local );
        }
    }
}