// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Backends.Redis;

/// <summary>
/// A <see cref="CachingBackendBuilder"/> that synchronizes the underlying in-memory cache thanks to Redis Pub/Sub.
/// </summary>
[PublicAPI]
public sealed class RedisCacheSynchronizerBuilder : ConcreteCachingBackendBuilder
{
    private readonly MemoryCachingBackendBuilder _underlying;
    private readonly RedisCacheSynchronizerConfiguration _configuration;

    internal RedisCacheSynchronizerBuilder(
        MemoryCachingBackendBuilder underlying,
        RedisCacheSynchronizerConfiguration? configuration,
        IServiceProvider? serviceProvider ) : base( serviceProvider )
    {
        this._underlying = underlying;
        this._configuration = configuration ?? new RedisCacheSynchronizerConfiguration();
    }

    public override CachingBackend CreateBackend( CreateBackendArgs args )
    {
        var underlying = this._underlying.CreateBackend( args );

        return new RedisCacheSynchronizer( underlying, this._configuration );
    }
}