// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Commands.Misc;

public class VersionCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var applicationInfo = context.ServiceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        context.Console.WriteMessage( "Application: " + applicationInfo.Name );
        context.Console.WriteMessage( "PackageVersion: " + applicationInfo.PackageVersion );
        context.Console.WriteMessage( "AssemblyVersion: " + applicationInfo.AssemblyVersion );
        context.Console.WriteMessage( "BuildDate: " + applicationInfo.BuildDate );
        context.Console.WriteMessage( "RuntimeInformation.FrameworkDescription: " + RuntimeInformation.FrameworkDescription );
        context.Console.WriteMessage( "RuntimeInformation.ProcessArchitecture: " + RuntimeInformation.ProcessArchitecture );
        context.Console.WriteMessage( "RuntimeInformation.OSDescription: " + RuntimeInformation.OSDescription );

        var standardDirectories = context.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        context.Console.WriteMessage( $"IStandardDirectories.ApplicationDataDirectory: {standardDirectories.ApplicationDataDirectory}" );
        context.Console.WriteMessage( $"IStandardDirectories.TempDirectory: {standardDirectories.TempDirectory}" );
    }
}