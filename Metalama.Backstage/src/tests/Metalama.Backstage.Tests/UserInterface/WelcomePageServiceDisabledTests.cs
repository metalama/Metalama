// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

// The Visual Studio scenario: OpenWelcomePage is left at its default (false) because the extension shows its own
// welcome page, so the welcome web page must not be opened even when telemetry activates. See #1701.
public sealed class WelcomePageServiceDisabledTests : TestsBase
{
    public WelcomePageServiceDisabledTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) => services.AddTelemetryServices();

    [Fact]
    public async Task WelcomePageNotOpenedWhenOptionDisabled()
    {
        this.ServiceProvider.GetRequiredBackstageService<Welcome.WelcomePageService>();

        this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().EnsureActivated();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Empty( this.UserInterface.ExternalWebPagesOpened );
    }
}
