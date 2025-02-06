// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Building;

internal sealed class SpecificCachingBackendBuilder : ConcreteCachingBackendBuilder
{
    private readonly Func<CreateBackendArgs, CachingBackend> _factory;

    public SpecificCachingBackendBuilder( Func<CreateBackendArgs, CachingBackend> factory, IServiceProvider? serviceProvider ) : base( serviceProvider )
    {
        this._factory = factory;
    }

    public override CachingBackend CreateBackend( CreateBackendArgs args ) => this._factory( args );
}