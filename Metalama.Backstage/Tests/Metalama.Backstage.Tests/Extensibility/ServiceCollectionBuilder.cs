// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Metalama.Backstage.Tests.Extensibility;

internal sealed class ServiceCollectionBuilder : ServiceProviderBuilder
{
    public IServiceCollection ServiceCollection { get; }

    public ServiceCollectionBuilder() : this( new ServiceCollection() ) { }

    public ServiceCollectionBuilder( IServiceCollection serviceCollection ) : base(
        ( type, func ) => serviceCollection.Add( new ServiceDescriptor( type, func, ServiceLifetime.Singleton ) ) )
    {
        this.ServiceCollection = serviceCollection;
    }
}