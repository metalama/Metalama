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
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Performance, enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService, this.ConfigurationManager! );
        model.OnGet();

        Assert.Equal( enabled, model.IsUsageReportingEnabled );
        Assert.Equal( enabled, model.IsExceptionReportingEnabled );
        Assert.Equal( enabled, model.IsPerformanceReportingEnabled );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesUsageReportingIndependently( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Usage, !enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService, this.ConfigurationManager! ) { IsUsageReportingEnabled = enabled, IsExceptionReportingEnabled = false };
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

        var model = new PrivacyPageModel( this.TelemetryConfigurationService, this.ConfigurationManager! )
        {
            IsUsageReportingEnabled = true, IsExceptionReportingEnabled = enabled, IsPerformanceReportingEnabled = !enabled
        };

        model.OnPost();

        // The exception checkbox drives only exception reporting.
        Assert.Equal( enabled, this.TelemetryConfigurationService.GetEffectiveReportingAction( TelemetryScenario.Exception ) != ReportingAction.No );

        // Performance reporting follows its own checkbox and must be unaffected by the exception one.
        Assert.Equal( !enabled, this.TelemetryConfigurationService.GetEffectiveReportingAction( TelemetryScenario.Performance ) != ReportingAction.No );

        // Usage reporting must be unaffected.
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesPerformanceReportingIndependently( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Exception, !enabled );
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Performance, !enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService, this.ConfigurationManager! )
        {
            IsUsageReportingEnabled = true, IsExceptionReportingEnabled = !enabled, IsPerformanceReportingEnabled = enabled
        };

        model.OnPost();

        // The performance checkbox drives only performance reporting.
        Assert.Equal( enabled, this.TelemetryConfigurationService.GetEffectiveReportingAction( TelemetryScenario.Performance ) != ReportingAction.No );

        // Exception reporting follows its own checkbox and must be unaffected by the performance one.
        Assert.Equal( !enabled, this.TelemetryConfigurationService.GetEffectiveReportingAction( TelemetryScenario.Exception ) != ReportingAction.No );

        // Usage reporting must be unaffected.
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }
}
