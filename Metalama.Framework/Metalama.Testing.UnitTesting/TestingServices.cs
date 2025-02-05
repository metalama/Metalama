// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Diagnostics;

namespace Metalama.Testing.UnitTesting;

internal static class TestingServices
{
    public static CompileTimeAssemblyLocatorProvider CompileTimeAssemblyLocatorProvider { get; }

    static TestingServices()
    {
        BackstageServiceFactoryInitializer.Initialize(
            new BackstageInitializationOptions( new TestApiApplicationInfo() ) { AddSupportServices = true, AddLicensing = true, AddDumperService = true } );

        CompileTimeAssemblyLocatorProvider =
            new CompileTimeAssemblyLocatorProvider( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ITempFileManager>() );
    }

    public static void Initialize() { }
}