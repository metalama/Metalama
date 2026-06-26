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
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    internal WelcomePageService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
        this._userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        if ( !serviceProvider.GetRequiredBackstageService<BackstageInitializationOptionsProvider>().Options.OpenWelcomePage )
        {
            throw new InvalidOperationException(
                $"{nameof(BackstageInitializationOptions)}.{nameof(BackstageInitializationOptions.OpenWelcomePage)} is false." );
        }
    }

    [PublicAPI( "Used by VSX." )]
    public bool WelcomePageDisplayed
    {
        get => this._configurationManager.Get<WelcomeConfiguration>().WelcomePageDisplayed;
        set => this._configurationManager.Update<WelcomeConfiguration>( c => c with { WelcomePageDisplayed = value } );
    }

    public void OpenWelcomePageOnce()
    {
        if ( this._telemetryConfigurationService.GetEffectiveConsent( TelemetryScenario.Usage ) != TelemetryConsent.Yes )
        {
            throw new InvalidOperationException( "Cannot open the welcome page when telemetry is disabled." );
        }

        this._backgroundTasksService.Enqueue(
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
}