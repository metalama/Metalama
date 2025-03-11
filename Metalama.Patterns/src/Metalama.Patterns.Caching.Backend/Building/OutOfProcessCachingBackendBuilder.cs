// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// A base class for <see cref="CachingBackendBuilder"/> representing an out-of-process cache. These back-ends can be further
/// enhanced with an in-memory L1 layer through the  <see cref="CachingBackendFactory.WithL1(Metalama.Patterns.Caching.Building.OutOfProcessCachingBackendBuilder,Metalama.Patterns.Caching.Backends.LayeredCachingBackendConfiguration?)"/>
/// methods. 
/// </summary>
public abstract class OutOfProcessCachingBackendBuilder : ConcreteCachingBackendBuilder
{
    protected OutOfProcessCachingBackendBuilder( IServiceProvider? serviceProvider ) : base( serviceProvider ) { }
}