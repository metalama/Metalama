// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Metalama.Backstage.Tests.Extensibility;

internal class ServiceCollectionBuilder : ServiceProviderBuilder
{
    public IServiceCollection ServiceCollection { get; }

    public ServiceCollectionBuilder() : this( new ServiceCollection() ) { }

    public ServiceCollectionBuilder( IServiceCollection serviceCollection ) : base(
        ( type, func ) => serviceCollection.Add( new ServiceDescriptor( type, func, ServiceLifetime.Singleton ) ) )
    {
        this.ServiceCollection = serviceCollection;
    }
}