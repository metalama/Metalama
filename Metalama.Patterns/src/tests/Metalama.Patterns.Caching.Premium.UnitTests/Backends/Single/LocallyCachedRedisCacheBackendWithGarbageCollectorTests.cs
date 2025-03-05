// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single;

[UsedImplicitly]
public class LocallyCachedRedisCacheBackendWithGarbageCollectorTests : LocallyCachedRedisCacheBackendTests
{
    public LocallyCachedRedisCacheBackendWithGarbageCollectorTests(
        CachingClassFixture cachingClassFixture,
        RedisAssemblyFixture redisAssemblyFixture,
        ITestOutputHelper testOutputHelper ) : base( cachingClassFixture, redisAssemblyFixture, testOutputHelper ) { }

    protected override bool EnableGarbageCollector => true;
}