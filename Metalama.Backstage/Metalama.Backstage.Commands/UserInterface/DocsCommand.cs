// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;

namespace Metalama.Backstage.Commands.UserInterface;

public class DocsCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var links = context.ServiceProvider.GetRequiredBackstageService<WebLinks>();
        context.ServiceProvider.GetRequiredBackstageService<IUserInterfaceService>().OpenExternalWebPage( links.Documentation, BrowserMode.Default );
    }

    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options ) => options with { AddUserInterface = true };
}