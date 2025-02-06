// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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