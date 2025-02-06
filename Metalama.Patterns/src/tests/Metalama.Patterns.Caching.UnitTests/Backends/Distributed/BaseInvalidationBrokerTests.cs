// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Distributed;

public abstract class BaseInvalidationBrokerTests : BaseDistributedCacheTests
{
    protected BaseInvalidationBrokerTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
        cachingClassFixture,
        testOutputHelper ) { }

    protected abstract ConcreteCachingBackendBuilder AddInvalidationBroker( MemoryCachingBackendBuilder builder, string prefix );

    protected override async Task<CachingBackend[]> CreateBackendsAsync()
    {
        var testId = Guid.NewGuid().ToString();

        async Task<CachingBackend> CreateCacheInvalidator()
        {
            var backend = CachingBackend.Create(
                b => this.AddInvalidationBroker( b.Memory(), testId ),
                this.ServiceProvider );

            await backend.InitializeAsync();

            return backend;
        }

        return [await CreateCacheInvalidator(), await CreateCacheInvalidator(), await CreateCacheInvalidator()];
    }

    protected override CachingBackend[] CreateBackends()
    {
        return this.CreateBackendsAsync().Result;
    }
}