// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// The initial object of the <see cref="CachingBackend"/> factory fluent API.
/// </summary>
/// <remarks>
/// <para>This class is the entry point for building caching backends. Use extension methods like
/// <see cref="CachingBackendFactory.Memory"/> to select a specific backend type.</para>
/// </remarks>
/// <seealso cref="ConcreteCachingBackendBuilder"/>
/// <seealso cref="MemoryCachingBackendBuilder"/>
[PublicAPI]
public class CachingBackendBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services during backend creation.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBackendBuilder"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies, or <c>null</c>.</param>
    public CachingBackendBuilder( IServiceProvider? serviceProvider )
    {
        this.ServiceProvider = serviceProvider;
    }
}