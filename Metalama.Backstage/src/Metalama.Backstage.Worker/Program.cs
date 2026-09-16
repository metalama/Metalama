// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Worker;
using System.Threading.Tasks;

namespace Metalama.Backstage;

/// <summary>
/// The entry point of the worker of Metalama, which binds the worker library to the Metalama product.
/// </summary>
internal static class Program
{
    public static Task<int> Main( string[] args )
        => BackstageWorkerProgram.RunAsync(
            args,
            new BackstageWorkerOptions(
                builder => builder.AddBackstageServices(
                    new BackstageInitializationOptions(
                        new BackstageWorkerApplicationInfo( typeof(Program).Assembly, MetalamaProduct.Profile, "Metalama Backstage Worker" ) )
                    {
                        AddSupportServices = true, AddLicensing = true, AddUserInterface = true
                    } ),
                serviceProvider => serviceProvider.InitializeBackstageServices() ) );
}
