// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Building;

[PublicAPI]
public static class CachingBackendFactory
{
    /// <summary>
    /// Creates a <see cref="CachingBackend"/> that throws an exception whenever it is used.
    /// </summary>
    public static ConcreteCachingBackendBuilder Uninitialized( this CachingBackendBuilder builder )
        => new UninitializedCachingBackendBuilder( builder.ServiceProvider );

    /// <summary>
    /// Creates an in-memory caching backend based on a <see cref="MemoryCache"/>.
    /// </summary>
    public static MemoryCachingBackendBuilder Memory( this CachingBackendBuilder builder, MemoryCachingBackendConfiguration? configuration = null )
        => new( configuration, builder.ServiceProvider );

    /// <summary>
    /// Adds an in-memory, in-process L1 cache in front of an out-of-process cache.
    /// </summary>
    public static LayeredCachingBackendBuilder WithL1(
        this OutOfProcessCachingBackendBuilder builder,
        LayeredCachingBackendConfiguration? configuration = null )
        => new( builder, builder.ServiceProvider, configuration );

    /// <summary>
    /// Adds an in-memory, in-process L1 cache in front of another in-memory back-end. This method is used for tests.
    /// </summary>
    [Obsolete( "Adding a memory cache on the top of another memory cache should only be used in tests." )]
    public static LayeredCachingBackendBuilder WithL1( this ConcreteCachingBackendBuilder builder, LayeredCachingBackendConfiguration? configuration = null )
        => new( builder, builder.ServiceProvider, configuration );

    /// <summary>
    /// Creates a <see cref="CachingBackendBuilder"/> that returns a specific instance of the <see cref="CachingBackend"/> class.
    /// This method is used in tests.
    /// </summary>
    public static ConcreteCachingBackendBuilder Specific( this CachingBackendBuilder builder, CachingBackend backend )
        => new SpecificCachingBackendBuilder( _ => backend, builder.ServiceProvider );

    /// <summary>
    /// Creates a <see cref="CachingBackendBuilder"/> that returns an instance of the <see cref="CachingBackend"/> class that does not do anything.
    /// </summary>
    public static ConcreteCachingBackendBuilder Null( this CachingBackendBuilder builder ) => builder.Specific( new NullCachingBackend() );
}