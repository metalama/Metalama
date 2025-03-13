// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// The initial object of the <see cref="CachingBackend"/> factory fluent API.
/// </summary>
[PublicAPI]
public class CachingBackendBuilder
{
    public IServiceProvider? ServiceProvider { get; }

    public CachingBackendBuilder( IServiceProvider? serviceProvider )
    {
        this.ServiceProvider = serviceProvider;
    }
}