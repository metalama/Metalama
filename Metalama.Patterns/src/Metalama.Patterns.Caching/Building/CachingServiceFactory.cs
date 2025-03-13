// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// Extension methods to <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class CachingServiceFactory
{
    [Obsolete( "Renamed to AddMetalamaCaching." )]
    public static IServiceCollection AddCaching(
        this IServiceCollection serviceCollection,
        Action<ICachingServiceBuilder>? build = null )
        => AddMetalamaCaching( serviceCollection, build );

    /// <summary>
    /// Adds Metalama's <see cref="ICachingService"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="build">An optional delegate allowing to configure the <see cref="ICachingService"/>.</param>
    /// <returns>The same <paramref name="serviceCollection"/>.</returns>
    public static IServiceCollection AddMetalamaCaching(
        this IServiceCollection serviceCollection,
        Action<ICachingServiceBuilder>? build = null )
    {
        serviceCollection.AddFlashtrace( false );

        serviceCollection.Add(
            ServiceDescriptor.Singleton<ICachingService, CachingService>( serviceProvider => CachingService.Create( build, serviceProvider ) ) );

        return serviceCollection;
    }
}