// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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