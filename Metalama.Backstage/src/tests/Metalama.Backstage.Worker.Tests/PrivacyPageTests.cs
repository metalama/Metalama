// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Pages;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Worker.Tests;

public sealed class PrivacyPageTests : TestsBase
{
    public PrivacyPageTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnGetReflectsCurrentTelemetryStatus( bool enabled )
    {
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Usage, enabled );
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Exception, enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService );
        model.OnGet();

        Assert.Equal( enabled, model.IsUsageReportingEnabled );
        Assert.Equal( enabled, model.IsExceptionReportingEnabled );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesUsageReportingIndependently( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Usage, !enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService ) { IsUsageReportingEnabled = enabled, IsExceptionReportingEnabled = false };
        model.OnPost();

        Assert.Equal( enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesExceptionReportingIndependently( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Exception, !enabled );
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Performance, !enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService ) { IsUsageReportingEnabled = true, IsExceptionReportingEnabled = enabled };
        model.OnPost();

        // The exception checkbox drives both exception and performance reporting.
        Assert.Equal( enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Exception ) );
        Assert.Equal( enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Performance ) );

        // Usage reporting must be unaffected.
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }
}
