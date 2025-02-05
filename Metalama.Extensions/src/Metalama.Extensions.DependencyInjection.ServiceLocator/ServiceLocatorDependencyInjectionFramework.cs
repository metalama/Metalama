// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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