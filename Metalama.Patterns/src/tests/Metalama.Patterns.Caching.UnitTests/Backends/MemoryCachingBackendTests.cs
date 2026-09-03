// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends
{
    /// <summary>
    /// Substitutes the clock, the work-item dispatcher and the memory cache of the backend under test, for the test
    /// classes whose backend expires its items in this process.
    /// </summary>
    /// <remarks>
    /// The class registers the three services of a <see cref="FakeCachingServices"/> in the service provider of the
    /// test. The expiration tests of <see cref="BaseCacheBackendTests"/> then advance the clock and wait for the work
    /// items that the advance queues, instead of sleeping.
    /// </remarks>
    public abstract class BaseFakeTimeCacheBackendTests : BaseCacheBackendTests
    {
        private readonly FakeCachingServices _fakeServices = new();

        protected BaseFakeTimeCacheBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override FakeCachingServices FakeServices => this._fakeServices;

        protected override void AddServices( ServiceCollection serviceCollection )
        {
            base.AddServices( serviceCollection );
            this._fakeServices.AddServices( serviceCollection );
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            this._fakeServices.Dispose();
        }

        /// <summary>
        /// Runs <see cref="BaseCacheBackendTests.TestSlidingExpiration"/>, which the base class skips.
        /// </summary>
        /// <remarks>
        /// The test is skipped in the base class because it was flaky on the real clock: it had to store an unrelated
        /// item in a loop to force the collection of the expired one, and it compared two readings of the wall clock.
        /// Neither is done on the substituted clock, so the test is deterministic here.
        /// </remarks>
        [Fact( Timeout = Timeout )]
        public override Task TestSlidingExpiration() => base.TestSlidingExpiration();

        /// <summary>
        /// Runs <see cref="BaseCacheBackendTests.TestSlidingExpirationAsync"/>, which the base class skips, for the
        /// reason given on <see cref="TestSlidingExpiration"/>.
        /// </summary>
        [Fact( Timeout = Timeout )]
        public override Task TestSlidingExpirationAsync() => base.TestSlidingExpirationAsync();
    }

    [UsedImplicitly]
    public sealed class MemoryCachingBackendTests : BaseFakeTimeCacheBackendTests
    {
        public MemoryCachingBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override CheckAfterDisposeCachingBackend CreateBackend()
        {
            return new CheckAfterDisposeCachingBackend(
                MemoryCacheFactory.CreateBackend( this.ServiceProvider, memoryCache: this.FakeServices.MemoryCache ) );
        }
    }

    [UsedImplicitly]
    public sealed class SerializingMemoryCachingBackendTests : BaseFakeTimeCacheBackendTests
    {
        public SerializingMemoryCachingBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override CheckAfterDisposeCachingBackend CreateBackend()
        {
            return new CheckAfterDisposeCachingBackend(
                MemoryCacheFactory.CreateBackend( this.ServiceProvider, withSerializer: true, memoryCache: this.FakeServices.MemoryCache ) );
        }
    }
}
