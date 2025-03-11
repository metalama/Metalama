// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Registration;

namespace Metalama.Backstage.Commands.Licensing;

internal class RegisterCommunityCommand : BaseCommand<RegisterCommunityCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, RegisterCommunityCommandSettings settings )
    {
        var service = context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>();

        if ( settings.Reason == CommunityLicenseReason.None )
        {
            throw new CommandException( "You must provide a value for the --reason option." );
        }

        var result = service.RegisterCommunityEdition( settings.Reason );

        if ( !result.IsSuccess )
        {
            throw new CommandException( result.ErrorMessage );
        }

        context.Console.WriteSuccess( "You are now using Metalama Community for Metalama 2025.1 and later." );
    }
}