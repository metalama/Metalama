// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Building;

#pragma warning disable CA1001

/// <summary>
/// A <see cref="CachingBackendBuilder"/> that adds an in-memory L1 cache in front a another, typically out-of-process,
/// cache.
/// </summary>
[PublicAPI]
public sealed class LayeredCachingBackendBuilder : ConcreteCachingBackendBuilder
{
    private readonly ConcreteCachingBackendBuilder _underlying;
    private readonly LayeredCachingBackendConfiguration? _configuration;

    private IMemoryCache? _memoryCache;

    internal LayeredCachingBackendBuilder(
        ConcreteCachingBackendBuilder underlying,
        IServiceProvider? serviceProvider,
        LayeredCachingBackendConfiguration? configuration ) : base( serviceProvider )
    {
        this._underlying = underlying;
        this._configuration = configuration;
    }

    public LayeredCachingBackendBuilder WithMemoryCache( IMemoryCache memoryCache )
    {
        this._memoryCache = memoryCache;

        return this;
    }

    public LayeredCachingBackendBuilder WithMemoryCacheOptions( MemoryCacheOptions memoryCacheOptions )
    {
        this._memoryCache = new MemoryCache( memoryCacheOptions );

        return this;
    }

    public override CachingBackend CreateBackend( CreateBackendArgs args )
    {
        // ReSharper disable once WithExpressionModifiesAllMembers
        var underlying = this._underlying.CreateBackend( args with { Layer = args.Layer + 1 } );

        // We don't add a non-blocking modifier because it may reorder reads and writes if we don't order wait for the
        // completion of background tasks before executing reads. However, because we have a single queue, this may make
        // performance even worse than with blocking operations.

        var memoryCache = new MemoryCachingBackend( this._memoryCache, this._configuration?.L1Configuration, this.ServiceProvider );

        return new LayeredCachingBackendEnhancer( underlying, memoryCache, this._configuration );
    }
}