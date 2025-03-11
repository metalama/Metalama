// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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