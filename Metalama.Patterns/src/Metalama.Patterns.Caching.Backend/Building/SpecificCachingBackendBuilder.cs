// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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