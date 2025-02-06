// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Single
{
    [UsedImplicitly]
    public sealed class MemoryCachingBackendTests : BaseCacheBackendTests
    {
        public MemoryCachingBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override CheckAfterDisposeCachingBackend CreateBackend()
        {
            return new CheckAfterDisposeCachingBackend( MemoryCacheFactory.CreateBackend( this.ServiceProvider ) );
        }
    }

    [UsedImplicitly]
    public sealed class SerializingMemoryCachingBackendTests : BaseCacheBackendTests
    {
        public SerializingMemoryCachingBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override CheckAfterDisposeCachingBackend CreateBackend()
        {
            return new CheckAfterDisposeCachingBackend( MemoryCacheFactory.CreateBackend( this.ServiceProvider, withSerializer: true ) );
        }
    }
}