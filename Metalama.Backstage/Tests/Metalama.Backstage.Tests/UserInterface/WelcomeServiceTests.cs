// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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