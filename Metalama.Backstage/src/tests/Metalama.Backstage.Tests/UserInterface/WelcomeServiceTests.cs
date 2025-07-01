// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Welcome;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

public sealed class WelcomeServiceTests : TestsBase
{
    public WelcomeServiceTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Fact]
    public async Task WelcomePageOpenedOnFirstTime()
    {
        var webLinks = new WebLinks();
        var welcomeService = new WelcomeService( this.ServiceProvider );
        welcomeService.OpenWelcomePageIfRequired();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Single( this.UserInterface.ExternalWebPagesOpened );
        Assert.StartsWith( webLinks.Welcome, this.UserInterface.ExternalWebPagesOpened.Single().Url, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WelcomePageNotOpenedOnSecondTime()
    {
        var welcomeService = new WelcomeService( this.ServiceProvider );
        welcomeService.OpenWelcomePageIfRequired();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();
        this.UserInterface.ExternalWebPagesOpened.Clear();
        welcomeService.OpenWelcomePageIfRequired();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();
        Assert.Empty( this.UserInterface.ExternalWebPagesOpened );
    }
}