// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;

namespace Metalama.Backstage.Commands;

public class DocsCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var links = context.ServiceProvider.GetRequiredBackstageService<WebLinks>();
        context.ServiceProvider.GetRequiredBackstageService<IUserInterfaceService>().OpenExternalWebPage( links.Documentation, BrowserMode.Default );
    }

    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options ) => options with { AddUserInterface = true };
}