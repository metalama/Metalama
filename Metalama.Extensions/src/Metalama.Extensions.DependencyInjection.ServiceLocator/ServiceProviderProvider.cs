// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator
{
    /// <summary>
    /// Exposes the global service provider.
    /// </summary>
    public static class ServiceProviderProvider
    {
        /// <summary>
        /// Gets or sets a delegate that provides a <see cref="IServiceProvider"/>. This delegate is called from types that consume dependencies.
        /// The default implementation is to return a <see cref="IServiceProvider"/> that contains no service.
        /// </summary>
        public static Func<IServiceProvider> ServiceProvider { get; set; } = () => EmptyServiceProvider.Instance;
    }
}