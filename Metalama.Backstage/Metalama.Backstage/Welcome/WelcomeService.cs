// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System;

namespace Metalama.Backstage.Welcome;

public sealed class WelcomeService : IBackstageService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly WebLinks _webLinks;

    internal WelcomeService( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
    }

    // Used by the VSX.
    [PublicAPI]
    public bool WelcomePageDisplayed
    {
        get => this._configurationManager.Get<WelcomeConfiguration>().WelcomePageDisplayed;
        set => this._configurationManager.Update<WelcomeConfiguration>( c => c with { WelcomePageDisplayed = value } );
    }

    public string? GetWelcomePageUrlAndRemember()
    {
        if ( this._configurationManager.UpdateIf<WelcomeConfiguration>( c => !c.WelcomePageDisplayed, c => c with { WelcomePageDisplayed = true } ) )
        {
            return this._webLinks.AfterSetup;
        }
        else
        {
            return null;
        }
    }
}