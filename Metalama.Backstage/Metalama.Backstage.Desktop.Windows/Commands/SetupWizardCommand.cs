// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class SetupWizardCommand : BaseAsyncCommand<BaseSettings>
{
    public const string Name = "setup";

    protected override async Task<int> ExecuteAsync( ExtendedCommandContext context, BaseSettings settings )
    {
        // Start the web server.
        var serviceProvider = App.GetBackstageServices( settings );
        var userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();

        await userInterfaceService.OpenConfigurationWebPageAsync( "Setup" );

        return 0;
    }
}