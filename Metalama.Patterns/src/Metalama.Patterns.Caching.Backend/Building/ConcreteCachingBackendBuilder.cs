// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// A base class for a <see cref="CachingBackendBuilder"/> able to create an instance of the <see cref="CachingBackend"/> class.
/// </summary>
public abstract class ConcreteCachingBackendBuilder : CachingBackendBuilder
{
    /// <summary>
    /// Creates the <see cref="CachingBackend"/>.
    /// </summary>
    public abstract CachingBackend CreateBackend( CreateBackendArgs args );

    protected ConcreteCachingBackendBuilder( IServiceProvider? serviceProvider ) : base( serviceProvider ) { }
}