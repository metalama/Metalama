// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator;

/// <summary>
/// An implementation of a dependency injection framework adapter that pulls dependencies from a global <see cref="IServiceProvider"/>
/// exposed on the <see cref="ServiceProviderProvider"/> class.
/// </summary>
/// <remarks>
/// <para>
/// This framework adapter implements a service locator pattern, where dependencies are resolved from a global service provider
/// rather than being injected through constructors. This approach can be useful for legacy code or scenarios where constructor
/// injection is not practical, though constructor injection is generally preferred when possible.
/// </para>
/// <para>
/// This framework is automatically registered when you add a reference to the <c>Metalama.Extensions.DependencyInjection.ServiceLocator</c> package.
/// To use it, ensure the <see cref="ServiceProviderProvider"/> is configured with your application's <see cref="IServiceProvider"/>.
/// </para>
/// </remarks>
/// <seealso cref="ServiceProviderProvider"/>
/// <seealso cref="DependencyInjectionExtensions"/>
/// <seealso href="@dependency-injection"/>
[PublicAPI]
[CompileTime]
public class ServiceLocatorDependencyInjectionFramework : DefaultDependencyInjectionFramework
{
    protected override DefaultDependencyInjectionStrategy GetStrategy( DependencyProperties properties )
        => properties.IsLazy
            ? new LazyServiceLocatorDependencyInjectionStrategy( properties )
            : new EarlyServiceLocatorDependencyInjectionStrategy( properties );
}