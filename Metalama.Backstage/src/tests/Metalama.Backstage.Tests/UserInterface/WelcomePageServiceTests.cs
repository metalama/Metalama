// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Welcome;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

// The command-line compiler scenario: OpenWelcomePage is enabled, so the welcome page opens the first time telemetry
// is activated (not at process init, and never on an opted-out machine). See #1701.
public sealed class WelcomePageServiceTests : TestsBase
{
    public WelcomePageServiceTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } )
    {
        this.InitializationOptions = this.InitializationOptions with { OpenWelcomePage = true };
    }

    protected override void ConfigureServices( ServiceProviderBuilder services ) => services.AddTelemetryServices();

    // Instantiating the service is what subscribes it to the telemetry-activated event.
    private void RegisterWelcomePageService() => this.ServiceProvider.GetRequiredBackstageService<WelcomePageService>();

    [Fact]
    public async Task WelcomePageOpenedWhenTelemetryActivates()
    {
        this.RegisterWelcomePageService();

        this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().EnsureActivated();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Single( this.UserInterface.ExternalWebPagesOpened );
        Assert.StartsWith( new WebLinks().Welcome, this.UserInterface.ExternalWebPagesOpened.Single().Url, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WelcomePageNotOpenedWhenTelemetryNeverActivates()
    {
        this.RegisterWelcomePageService();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Empty( this.UserInterface.ExternalWebPagesOpened );
    }

    [Fact]
    public async Task WelcomePageOpenedOnlyOnce()
    {
        this.RegisterWelcomePageService();
        var telemetryConfigurationService = this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        telemetryConfigurationService.EnsureActivated();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();
        this.UserInterface.ExternalWebPagesOpened.Clear();

        // A second activation call does not re-raise the event (telemetry is already activated), and the
        // WelcomePageDisplayed guard would prevent re-opening anyway.
        telemetryConfigurationService.EnsureActivated();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Empty( this.UserInterface.ExternalWebPagesOpened );
    }
}
