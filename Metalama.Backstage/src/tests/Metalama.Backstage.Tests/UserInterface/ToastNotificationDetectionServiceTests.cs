// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Tests.Licensing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

public sealed class ToastNotificationDetectionServiceTests : LicensingTestsBase
{
    private readonly IToastNotificationDetectionService _toastNotificationDetectionService;

    public ToastNotificationDetectionServiceTests( ITestOutputHelper logger ) : base( logger, isTelemetryEnabled: true )
    {
        this._toastNotificationDetectionService = this.ServiceProvider.GetRequiredBackstageService<IToastNotificationDetectionService>();
    }

    protected override void OnAfterServicesCreated( Services services )
    {
        base.OnAfterServicesCreated( services );
        this.UserDeviceDetection.IsInteractiveDevice = true;
    }

    private async Task DetectToastNotificationsAsync( bool openTelemetrySession = true, bool requireLicense = true )
    {
        // In practice, licensing is enforced from an MSBuild task prior to the Metalama compiler process.
        if ( requireLicense )
        {
            var licensing = this.ServiceProvider.GetRequiredBackstageService<ILicenseConsumptionService>();
            var consumer = licensing.CreateConsumer();
            consumer.TryConsume( new AnyLicenseRequirement() );
        }

        // The telemetry notification is linked to the first activation of telemetry, from
        // default to enabled status for the 'usage' scenario.
        if ( openTelemetrySession )
        {
            this.FileSystem.CreateDirectory( "c:\\src" );
            var telemetryService = this.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();
            var telemetryPolicy = telemetryService.GetPolicy( "c:\\src" );
            var telemetryContext = telemetryService.OpenContext( telemetryPolicy );
            telemetryContext.StartUsageSession( "Test" );
        }

        await this._toastNotificationDetectionService.DetectAsync();
    }

    [Theory]
    [InlineData( true, true )]
    [InlineData( false, false )]
    public async Task IsLicenseActivationSuggestedOnFirstRunAsync( bool isUserInteractive, bool shouldBeOpened )
    {
        this.UserDeviceDetection.IsInteractiveDevice = isUserInteractive;

        await this.DetectToastNotificationsAsync( openTelemetrySession: false );

        // The first-run telemetry notice is displayed independently of the license notification, so we assert on the
        // specific notification kind rather than on an empty collection.
        if ( !shouldBeOpened )
        {
            Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );
        }
        else
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );
        }

        // Initializing a second time should not show a notification because of snoozing.
        this.UserInterface.Notifications.Clear();

        await this.DetectToastNotificationsAsync( openTelemetrySession: false );
        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );

        // After the snooze period, we should see a notification.
        this.Time.AddTime( ToastNotificationKinds.RequiresLicense.AutoSnoozePeriod.Add( TimeSpan.FromSeconds( 1 ) ) );

        await this.DetectToastNotificationsAsync();

        if ( !shouldBeOpened )
        {
            Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );
        }
        else
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );
        }
    }

    [Theory]
    [InlineData( 7, null )] // Before LicensingConstants.LicenseExpirationWarningPeriod
    [InlineData( 6, "6 days" )]
    [InlineData( 2, "2 days" )]
    [InlineData( 1, "tomorrow" )]
    [InlineData( 0, "today" )]
    [InlineData( -1, "expired" )]
    public async Task IsUserNotifiedOfTrialExpirationAsync( int daysBeforeExpiration, string? expectedTitleSubstring )
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;

        // Register a trial version.
        Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

        // Move the clock.
        this.Time.AddTime( LicensingConstants.EvaluationPeriod - TimeSpan.FromDays( daysBeforeExpiration + 1 ) );

        // Initialize

        await this.DetectToastNotificationsAsync( openTelemetrySession: false );

        if ( expectedTitleSubstring == null )
        {
            Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TrialExpiring );
        }
        else
        {
#pragma warning disable CA1307
            Assert.Single(
                this.UserInterface.Notifications,
                n => n.Kind == ToastNotificationKinds.TrialExpiring && n.Title?.Contains( expectedTitleSubstring ) == true );
#pragma warning restore CA1307
        }
    }

    [Theory]
    [InlineData( 30, null )] // Before LicensingConstants.SubscriptionExpirationWarningPeriod
    [InlineData( 29, "29 days" )]
    [InlineData( 2, "2 days" )]
    [InlineData( 1, "tomorrow" )]
    [InlineData( 0, "today" )]
    [InlineData( -1, "expired" )]
    public async Task IsUserNotifiedOfSubscriptionExpirationAsync( int daysBeforeExpiration, string? expectedTitleSubstring )
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;

        // Register a license key.
        Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness ).IsSuccess );

        // Move the clock.
        this.Time.Set( LicenseKeyProvider.DefaultSubscriptionExpirationDate - TimeSpan.FromDays( daysBeforeExpiration ) );

        await this.DetectToastNotificationsAsync( openTelemetrySession: false );

        if ( expectedTitleSubstring == null )
        {
            Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.SubscriptionExpiring );
        }
        else
        {
#pragma warning disable CA1307
            Assert.Single(
                this.UserInterface.Notifications,
                n => n.Kind == ToastNotificationKinds.SubscriptionExpiring && n.Title?.Contains( expectedTitleSubstring ) == true );
#pragma warning restore CA1307
        }
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public async Task IsVsxInstallationSuggestedAsync( bool extensionInstalled )
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;
        this.UserDeviceDetection.IsVisualStudioInstalled = true;
        this.ServiceProvider.GetRequiredBackstageService<IIdeExtensionStatusService>().IsVisualStudioExtensionInstalled = extensionInstalled;

        // Register a trial version.
        Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

        // Detect notifications.
        await this.DetectToastNotificationsAsync();

        // On the first run the VSX prompt is throttled behind the first-run telemetry notice (#1692), so it is not
        // shown yet regardless of whether the extension is installed.
        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );

        // After the throttle period, the VSX prompt is shown unless the extension is already installed.
        this.UserInterface.Notifications.Clear();
        this.Time.AddTime( ToastNotificationStatusService.LowPriorityThrottlePeriod + TimeSpan.FromSeconds( 1 ) );
        await this.DetectToastNotificationsAsync();

        if ( extensionInstalled )
        {
            Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );
        }
        else
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );
        }
    }

    [Fact]
    public async Task FirstRunShowsTelemetryNoticeAndDefersVsxPromptAsync()
    {
        // Regression test for #1692: the VSX-install prompt must not appear together with the first-run telemetry notice.
        this.UserDeviceDetection.IsInteractiveDevice = true;
        this.UserDeviceDetection.IsVisualStudioInstalled = true;
        this.ServiceProvider.GetRequiredBackstageService<IIdeExtensionStatusService>().IsVisualStudioExtensionInstalled = false;

        // Detect notifications.
        await this.DetectToastNotificationsAsync( requireLicense: false );

        // First run: only the telemetry notice; the VSX prompt is deferred.
        Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TelemetryNotice );
        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );

        // After the throttle period, the VSX prompt appears (and the once-only telemetry notice does not repeat).
        this.UserInterface.Notifications.Clear();
        this.Time.AddTime( TimeSpan.FromMinutes( 15 ) + TimeSpan.FromSeconds( 1 ) );
        await this.DetectToastNotificationsAsync( requireLicense: false );

        Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );
        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TelemetryNotice );
    }

    [Theory]
    [InlineData( TelemetryConsent.Default, true )]
    [InlineData( TelemetryConsent.Yes, false )]
    [InlineData( TelemetryConsent.No, false )]
    public async Task TelemetryNoticeShownDependingOnReportingStatus( TelemetryConsent telemetryConsent, bool isNotificationExpected )
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;
        this.TelemetryConfigurationService.SetConsent( telemetryConsent );

        // Detect notifications.
        await this.DetectToastNotificationsAsync( requireLicense: false );

        if ( isNotificationExpected )
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TelemetryNotice );
        }
        else
        {
            Assert.Empty( this.UserInterface.Notifications );
        }

        // The notice must not be shown again on subsequent runs (tracked in WelcomeConfiguration).
        // Move the clock past the detection throttle so that detection actually runs again.
        this.UserInterface.Notifications.Clear();
        this.Time.AddTime( TimeSpan.FromMinutes( 1 ) );
        await this.DetectToastNotificationsAsync();
        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TelemetryNotice );
    }

    [Fact]
    public async Task TelemetryNoticeNotShownOnNonInteractiveDeviceAsync()
    {
        this.UserDeviceDetection.IsInteractiveDevice = false;

        // Detect notifications.
        await this.DetectToastNotificationsAsync();

        Assert.DoesNotContain( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.TelemetryNotice );
    }

    [Fact]
    public async Task VsxNotInstalledNotificationHasUri()
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;
        this.UserDeviceDetection.IsVisualStudioInstalled = true;
        this.ServiceProvider.GetRequiredBackstageService<IIdeExtensionStatusService>().IsVisualStudioExtensionInstalled = false;

        // Register a trial version.
        Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

        await this.DetectToastNotificationsAsync();

        // The VSX prompt is throttled behind the first-run telemetry notice (#1692), so advance past the throttle.
        this.Time.AddTime( TimeSpan.FromMinutes( 15 ) + TimeSpan.FromSeconds( 1 ) );
        await this.DetectToastNotificationsAsync();

        var notification = this.UserInterface.Notifications.Single( n => n.Kind == ToastNotificationKinds.VsxNotInstalled );
        var webLinks = new WebLinks();

        // The notification must have a Uri so the "Install" button can navigate to the VS Marketplace.
        Assert.NotNull( notification.Uri );
        Assert.Equal( webLinks.InstallVsx, notification.Uri );
    }
}