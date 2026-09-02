// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.DependencyInjection;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Resolves the dependencies that every caching component shares: the clock and the work-item dispatcher.
/// </summary>
/// <remarks>
/// Both dependencies have a production implementation that is used when the service provider supplies none, so
/// neither is ever absent.
/// </remarks>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the <see cref="TimeProvider"/> of the given service provider, or <see cref="TimeProvider.System"/> when
    /// the service provider supplies none.
    /// </summary>
    public static TimeProvider GetTimeProvider( this IServiceProvider? serviceProvider )
        => serviceProvider?.GetService<TimeProvider>() ?? TimeProvider.System;

    /// <summary>
    /// Gets the <see cref="IWorkItemDispatcher"/> of the given service provider, or
    /// <see cref="ThreadPoolWorkItemDispatcher.Instance"/> when the service provider supplies none.
    /// </summary>
    public static IWorkItemDispatcher GetWorkItemDispatcher( this IServiceProvider? serviceProvider )
        => serviceProvider?.GetService<IWorkItemDispatcher>() ?? ThreadPoolWorkItemDispatcher.Instance;
}
