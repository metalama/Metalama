// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Backends.Azure;

/// <summary>
/// A <see cref="CachingBackendBuilder"/> that synchronizes the underlying in-memory cache thanks to Azure Service Bus.
/// </summary>
public sealed class AzureCacheSynchronizerBuilder : ConcreteCachingBackendBuilder
{
    private readonly MemoryCachingBackendBuilder _underlying;
    private readonly AzureCacheSynchronizerConfiguration _configuration;

    internal AzureCacheSynchronizerBuilder(
        MemoryCachingBackendBuilder underlying,
        AzureCacheSynchronizerConfiguration configuration,
        IServiceProvider? serviceProvider ) : base( serviceProvider )
    {
        this._underlying = underlying;
        this._configuration = configuration;
    }

    public override CachingBackend CreateBackend( CreateBackendArgs args )
    {
        var underlying = this._underlying.CreateBackend( args );

        return new AzureCacheSynchronizer( underlying, this._configuration );
    }
}