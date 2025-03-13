// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;

namespace Metalama.Backstage.Commands.Licensing;

internal class UnregisterCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>().RemoveLicenses();

        context.Console.WriteSuccess( $"All license keys have been unregistered." );
    }
}