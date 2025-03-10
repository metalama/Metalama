// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Commands;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Tool;

internal class InfoCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        context.Console.WriteSuccess( $"Metalama Tool v{EngineAssemblyMetadataReader.Instance.PackageVersion ?? "<unknown>"}" );

        var standardDirectories = context.ServiceProvider.GetBackstageService<IStandardDirectories>();

        if ( standardDirectories != null )
        {
            context.Console.WriteImportantMessage( $"Application data directory: {standardDirectories.ApplicationDataDirectory}" );
            context.Console.WriteImportantMessage( $"Temp directory: {standardDirectories.TempDirectory}" );
        }
    }

    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options ) => options with { AddUserInterface = true };
}