// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

        var result = service.RegisterLegacyFreeEdition();

        if ( !result.IsSuccess )
        {
            throw new CommandException( result.ErrorMessage );
        }

        context.Console.WriteSuccess( "You are now using Metalama Free for Metalama 2025.0 and earlier." );
    }
}