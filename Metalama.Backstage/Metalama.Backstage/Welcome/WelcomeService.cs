// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;
using System;

namespace Metalama.Backstage.Welcome;

[PublicAPI( "Used by VSX." )]
public sealed class WelcomeService : IBackstageService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly WebLinks _webLinks;
    private readonly IUserInterfaceService _userInterfaceService;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    internal WelcomeService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
        this._userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
    }

    [PublicAPI( "Used by VSX." )]
    public bool WelcomePageDisplayed
    {
        get => this._configurationManager.Get<WelcomeConfiguration>().WelcomePageDisplayed;
        set => this._configurationManager.Update<WelcomeConfiguration>( c => c with { WelcomePageDisplayed = value } );
    }

    public void OnBackstageInitialized()
    {
        if ( this._telemetryConfigurationService.IsEnabled )
        {
            if ( this._configurationManager.UpdateIf<WelcomeConfiguration>( c => !c.WelcomePageDisplayed, c => c with { WelcomePageDisplayed = true } ) )
            {
                this._userInterfaceService.OpenExternalWebPage( this._webLinks.Welcome, BrowserMode.Default );
                this._userInterfaceService.ShowToastNotification( new ToastNotification( ToastNotificationKinds.Welcome ) );
            }
        }
    }
}