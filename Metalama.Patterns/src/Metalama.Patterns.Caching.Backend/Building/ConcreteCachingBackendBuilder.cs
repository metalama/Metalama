// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// A base class for a <see cref="CachingBackendBuilder"/> able to create an instance of the <see cref="CachingBackend"/> class.
/// </summary>
/// <seealso cref="CachingBackendBuilder"/>
/// <seealso cref="MemoryCachingBackendBuilder"/>
/// <seealso cref="OutOfProcessCachingBackendBuilder"/>
public abstract class ConcreteCachingBackendBuilder : CachingBackendBuilder
{
    /// <summary>
    /// Creates the <see cref="CachingBackend"/>.
    /// </summary>
    /// <param name="args">Arguments containing context for backend creation.</param>
    /// <returns>The created <see cref="CachingBackend"/>.</returns>
    public abstract CachingBackend CreateBackend( CreateBackendArgs args );

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcreteCachingBackendBuilder"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    protected ConcreteCachingBackendBuilder( IServiceProvider? serviceProvider ) : base( serviceProvider ) { }
}