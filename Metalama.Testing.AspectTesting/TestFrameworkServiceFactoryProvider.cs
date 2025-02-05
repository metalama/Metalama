// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;

namespace Metalama.Testing.AspectTesting;

internal static class TestFrameworkServiceFactoryProvider
{
    public static GlobalServiceProvider GetServiceProvider()
    {
        TestingServices.Initialize();

        var additionalServicesCollection = new AdditionalServiceCollection();
        additionalServicesCollection.AddGlobalService( TestingServices.CompileTimeAssemblyLocatorProvider );

        return ServiceProviderFactory.GetServiceProvider( BackstageServiceFactory.ServiceProvider, additionalServicesCollection )
            .WithService( new TestAssemblyMetadataReader() );
    }
}