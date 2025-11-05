// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

internal abstract class OpenWorkerWebPageCommand : BaseAsyncCommand<BaseSettings>
{
    private readonly string _page;

    protected OpenWorkerWebPageCommand( string page )
    {
        this._page = page;
    }

    protected override async Task<int> ExecuteAsync( ExtendedCommandContext context, BaseSettings settings )
    {
        // Start the web server.
        var serviceProvider = App.GetBackstageServices( settings );
        var userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();

        await userInterfaceService.OpenConfigurationWebPageAsync( this._page );

        return 0;
    }
}