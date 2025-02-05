// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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