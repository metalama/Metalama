// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator;

/// <summary>
/// An implementation a dependency injection framework adapter that pulls dependency from a global <see cref="IServiceProvider"/>
/// exposed on the <see cref="ServiceProviderProvider"/> class.
/// </summary>
[PublicAPI]
[CompileTime]
public class ServiceLocatorDependencyInjectionFramework : DefaultDependencyInjectionFramework
{
    protected override DefaultDependencyInjectionStrategy GetStrategy( DependencyProperties properties )
        => properties.IsLazy
            ? new LazyServiceLocatorDependencyInjectionStrategy( properties )
            : new EarlyServiceLocatorDependencyInjectionStrategy( properties );
}