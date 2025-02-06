// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Azure;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.TestHelpers;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Distributed
{
    // ReSharper disable once UnusedType.Global
    public class AzureServiceBusInvalidationTests : BaseInvalidationBrokerTests
    {
        private readonly string _connectionString = Secrets.Get( "CacheInvalidationTestServiceBusConnectionString" );

        public AzureServiceBusInvalidationTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base(
            cachingClassFixture,
            testOutputHelper ) { }

        protected override ConcreteCachingBackendBuilder AddInvalidationBroker( MemoryCachingBackendBuilder builder, string prefix )
            => builder.WithAzureSynchronization( new AzureCacheSynchronizerConfiguration( this._connectionString ) { Prefix = prefix } );
    }
}