// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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