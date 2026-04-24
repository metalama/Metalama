// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator
{
    /// <summary>
    /// Exposes the global service provider used by the service locator dependency injection framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a global access point for dependency resolution when using the
    /// <see cref="ServiceLocatorDependencyInjectionFramework"/>. Configure the <see cref="ServiceProvider"/>
    /// property in your application startup code to provide the <see cref="IServiceProvider"/> instance.
    /// </para>
    /// </remarks>
    /// <seealso cref="ServiceLocatorDependencyInjectionFramework"/>
    /// <seealso href="@dependency-injection"/>
    [PublicAPI]
    public static class ServiceProviderProvider
    {
        /// <summary>
        /// Gets or sets a delegate that provides a <see cref="IServiceProvider"/>. This delegate is called from types that consume dependencies.
        /// The default implementation is to return a <see cref="IServiceProvider"/> that contains no service.
        /// </summary>

        public static Func<IServiceProvider> ServiceProvider { get; set; } = () => EmptyServiceProvider.Instance;
    }
}