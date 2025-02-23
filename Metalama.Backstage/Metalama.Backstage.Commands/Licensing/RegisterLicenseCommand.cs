// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;

namespace Metalama.Backstage.Commands.Licensing;

internal class RegisterLicenseCommand : BaseCommand<RegisterLicenseCommandSettings>
{
    // We no longer collect license activations reports.
    // We only collect license usage reports.

    protected override void Execute( ExtendedCommandContext context, RegisterLicenseCommandSettings settings )
    {
        var service = context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>();

        var result = service.RegisterLicense( settings.License );

        if ( !result.IsSuccess )
        {
            throw new CommandException( result.ErrorMessage );
        }

        context.Console.WriteSuccess( $"The license key '{settings.License}' has been registered." );
    }
}