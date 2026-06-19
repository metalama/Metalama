// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;
using System;

namespace Metalama.Backstage.Welcome;

/// <summary>
/// Opens the welcome web page the first time telemetry is activated, when the host has requested it through
/// <see cref="BackstageInitializationOptions.OpenWelcomePage"/>. The command-line compiler requests it; Visual Studio
/// does not, because the extension shows its own welcome page. See #1701.
/// </summary>
[PublicAPI( "Used by VSX and Metalama.Framework." )]
public sealed class WelcomePageService : IBackstageService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly WebLinks _webLinks;
    private readonly IUserInterfaceService _userInterfaceService;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly bool _openWelcomePage;

    internal WelcomePageService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
        this._userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._openWelcomePage = serviceProvider.GetRequiredBackstageService<BackstageInitializationOptionsProvider>().Options.OpenWelcomePage;

        // The welcome page is opened the first time telemetry is activated, so that an opted-out or dormant machine
        // never opens it. The telemetry configuration service is absent when support services are not registered, in
        // which case there is nothing to open the page for.
        // OnActivated (rather than a plain event) so we still react if telemetry was already activated by this process
        // before this service was constructed. See #1701.
        serviceProvider.GetBackstageService<ITelemetryConfigurationService>()?.OnActivated( this.OnTelemetryActivated );
    }

    [PublicAPI( "Used by VSX." )]
    public bool WelcomePageDisplayed
    {
        get => this._configurationManager.Get<WelcomeConfiguration>().WelcomePageDisplayed;
        set => this._configurationManager.Update<WelcomeConfiguration>( c => c with { WelcomePageDisplayed = value } );
    }

    private void OnTelemetryActivated()
    {
        if ( this._openWelcomePage )
        {
            this.OpenWelcomePageIfRequired();
        }
    }

    private void OpenWelcomePageIfRequired()
        => this._backgroundTasksService.Enqueue(
            () =>
            {
                if ( this._configurationManager.UpdateIf<WelcomeConfiguration>(
                        c => !c.WelcomePageDisplayed,
                        c => c with { WelcomePageDisplayed = true } ) )
                {
                    this._userInterfaceService.OpenExternalWebPage( this._webLinks.Welcome, BrowserMode.Default );
                }
            } );
}
