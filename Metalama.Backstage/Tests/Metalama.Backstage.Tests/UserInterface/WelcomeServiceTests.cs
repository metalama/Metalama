// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Welcome;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

public sealed class WelcomeServiceTests : TestsBase
{
    public WelcomeServiceTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Fact]
    public void WelcomePageOpenedOnFirstTime()
    {
        var webLinks = new WebLinks();
        var welcomeService = new WelcomeService( this.ServiceProvider );
        welcomeService.OnBackstageInitialized();

        Assert.Single( this.UserInterface.ExternalWebPagesOpened );
        Assert.StartsWith( webLinks.Welcome, this.UserInterface.ExternalWebPagesOpened.Single().Url, StringComparison.Ordinal );
        Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( ToastNotificationKinds.Welcome, this.UserInterface.Notifications.Single().Kind );
    }

    [Fact]
    public void WelcomePageNotOpenedOnSecondTime()
    {
        var welcomeService = new WelcomeService( this.ServiceProvider );
        welcomeService.OnBackstageInitialized();
        this.UserInterface.ExternalWebPagesOpened.Clear();
        welcomeService.OnBackstageInitialized();
        Assert.Empty( this.UserInterface.ExternalWebPagesOpened );
    }
}