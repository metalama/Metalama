// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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