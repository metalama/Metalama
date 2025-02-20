using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;
using System;

namespace Metalama.Backstage.Commands.Licensing;

[Obsolete]
internal class RegisterLegacyFreeCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var service = context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>();

        if ( !service.TryRegisterLegacyFreeEdition( out var errorMessage ) )
        {
            throw new CommandException( errorMessage );
        }
        
        // TODO: We should ask for the reason (eligibility).

        context.Console.WriteSuccess( "You are now using Metalama Community." );
    }
}