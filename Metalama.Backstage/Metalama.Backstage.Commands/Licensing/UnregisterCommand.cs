// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;

namespace Metalama.Backstage.Commands.Licensing;

internal class UnregisterCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        if ( !context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>().TryRemoveCurrentLicense( out var licenseString ) )
        {
            throw new CommandException( "A license is not registered." );
        }

        context.Console.WriteSuccess( $"The license key '{licenseString}' has been unregistered." );
    }
}