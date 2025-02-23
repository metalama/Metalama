// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
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
                AddLicensing = true,
                AddDumperService = true,
                
                // Provide a test license for all tests. This is not useful for the open-soure tests,
                // but it's useful for premium components.
                LicensingOptions = LicensingInitializationOptions.ForTest(
                    license =>
                    {
                        license.Product = LicensedProduct.MetalamaProfessional;
                        license.LicenseType = LicenseType.Test;
                        license.SubscriptionEndDate = DateTime.MaxValue;
                    } )
            } );

        CompileTimeAssemblyLocatorProvider =
            new CompileTimeAssemblyLocatorProvider( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ITempFileManager>() );
    }

    public static void Initialize() { }
}