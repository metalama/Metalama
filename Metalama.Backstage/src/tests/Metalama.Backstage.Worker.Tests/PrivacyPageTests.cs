// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Pages;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Worker.Tests;

public sealed class PrivacyPageTests : TestsBase
{
    public PrivacyPageTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    // Reads the stored consent the same way the page does, so the assertions distinguish 'Default' (review-first) from
    // 'No' instead of collapsing them. See #1707.
    private TelemetryConsent GetStoredConsent( TelemetryScenario scenario ) => this.ConfigurationManager!.Get<TelemetryConfiguration>().GetConsent( scenario );

    private PrivacyPageModel CreateModel() => new( this.TelemetryConfigurationService, this.ConfigurationManager! );

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnGetReflectsUsageOptOut( bool enabled )
    {
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Usage, enabled ? TelemetryConsent.Yes : TelemetryConsent.No );

        var model = this.CreateModel();
        model.OnGet();

        Assert.Equal( enabled, model.IsUsageReportingEnabled );
    }

    [Theory]
    [InlineData( TelemetryConsent.No )]
    [InlineData( TelemetryConsent.Default )]
    [InlineData( TelemetryConsent.Yes )]
    public void OnGetReflectsStoredExceptionAndPerformanceConsent( TelemetryConsent consent )
    {
        // The whole point of the three-state control: 'Default' (review-first) must be read back distinctly from 'No'.
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Exception, consent );
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Performance, consent );

        var model = this.CreateModel();
        model.OnGet();

        Assert.Equal( consent, model.ExceptionConsent );
        Assert.Equal( consent, model.PerformanceConsent );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesUsageReportingIndependently( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Usage, enabled ? TelemetryConsent.No : TelemetryConsent.Yes );

        var model = this.CreateModel();
        model.IsUsageReportingEnabled = enabled;
        model.ExceptionConsent = TelemetryConsent.Default;
        model.PerformanceConsent = TelemetryConsent.Default;

        model.OnPost();

        Assert.Equal( enabled ? TelemetryConsent.Yes : TelemetryConsent.No, this.GetStoredConsent( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }

    [Theory]
    [InlineData( TelemetryConsent.No )]
    [InlineData( TelemetryConsent.Default )]
    [InlineData( TelemetryConsent.Yes )]
    public void OnPostPersistsExceptionConsentIncludingReviewFirst( TelemetryConsent consent )
    {
        // Start from a different state so the post actually writes the value.
        this.TelemetryConfigurationService.SetConsent(
            TelemetryScenario.Exception,
            consent == TelemetryConsent.Yes ? TelemetryConsent.No : TelemetryConsent.Yes );

        var model = this.CreateModel();
        model.IsUsageReportingEnabled = true;
        model.ExceptionConsent = consent;
        model.PerformanceConsent = TelemetryConsent.Default;

        model.OnPost();

        Assert.Equal( consent, this.GetStoredConsent( TelemetryScenario.Exception ) );
        Assert.True( model.IsSaved );
    }

    [Theory]
    [InlineData( TelemetryConsent.No )]
    [InlineData( TelemetryConsent.Default )]
    [InlineData( TelemetryConsent.Yes )]
    public void OnPostPersistsPerformanceConsentIncludingReviewFirst( TelemetryConsent consent )
    {
        // Start from a different state so the post actually writes the value.
        this.TelemetryConfigurationService.SetConsent(
            TelemetryScenario.Performance,
            consent == TelemetryConsent.Yes ? TelemetryConsent.No : TelemetryConsent.Yes );

        var model = this.CreateModel();
        model.IsUsageReportingEnabled = true;
        model.ExceptionConsent = TelemetryConsent.Default;
        model.PerformanceConsent = consent;

        model.OnPost();

        Assert.Equal( consent, this.GetStoredConsent( TelemetryScenario.Performance ) );
        Assert.True( model.IsSaved );
    }

    [Fact]
    public void OnPostUpdatesEachCategoryIndependently()
    {
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Usage, TelemetryConsent.No );
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Exception, TelemetryConsent.No );
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Performance, TelemetryConsent.No );

        var model = this.CreateModel();
        model.IsUsageReportingEnabled = true;
        model.ExceptionConsent = TelemetryConsent.Yes;
        model.PerformanceConsent = TelemetryConsent.Default;

        model.OnPost();

        Assert.Equal( TelemetryConsent.Yes, this.GetStoredConsent( TelemetryScenario.Usage ) );
        Assert.Equal( TelemetryConsent.Yes, this.GetStoredConsent( TelemetryScenario.Exception ) );
        Assert.Equal( TelemetryConsent.Default, this.GetStoredConsent( TelemetryScenario.Performance ) );
        Assert.True( model.IsSaved );
    }

    [Fact]
    public void OnPostResetDeviceId_RotatesDeviceId_WhenActivated()
    {
        this.TelemetryConfigurationService.EnsureActivated();
        var original = this.TelemetryConfigurationService.DeviceId;
        Assert.NotEqual( Guid.Empty, original );

        var model = this.CreateModel();
        model.OnPostResetDeviceId();

        Assert.True( model.IsTelemetryActivated );
        Assert.True( model.IsDeviceIdReset );
        Assert.NotEqual( Guid.Empty, this.TelemetryConfigurationService.DeviceId );
        Assert.NotEqual( original, this.TelemetryConfigurationService.DeviceId );
    }

    [Fact]
    public void OnPostResetDeviceId_DoesNothing_WhenNotActivated()
    {
        // Rotating a never-activated configuration would create a device ID and thereby activate telemetry, which must
        // stay lazy. The handler is a no-op in that case. See #1701.
        Assert.False( this.TelemetryConfigurationService.IsActivated );

        var model = this.CreateModel();
        model.OnPostResetDeviceId();

        Assert.False( model.IsTelemetryActivated );
        Assert.False( model.IsDeviceIdReset );
        Assert.False( this.TelemetryConfigurationService.IsActivated );
    }
}