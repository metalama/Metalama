// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using System;

namespace Metalama.Testing.UnitTesting;

internal static class TestingServices
{
    public static CompileTimeAssemblyLocatorProvider CompileTimeAssemblyLocatorProvider { get; }

    static TestingServices()
    {
        BackstageServiceFactoryInitializer.Initialize(
            new BackstageInitializationOptions( new TestApiApplicationInfo() )
            {
                AddSupportServices = true,
                AddLicensing = false,
                AddDumperService = true,
                
                // Provide a test license for all tests. This is not useful for the open-soure tests,
                // but it's useful for premium components.
                LicensingOptions = LicensingInitializationOptions.ForTest(
                    license =>
                    {
                        license.Product = LicenseProduct.MetalamaProfessional;
                        license.LicenseType = LicenseType.Test;
                        license.SubscriptionEndDate = DateTime.MaxValue;
                    } )
            } );

        CompileTimeAssemblyLocatorProvider =
            new CompileTimeAssemblyLocatorProvider( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ITempFileManager>() );
    }

    public static void Initialize() { }
}