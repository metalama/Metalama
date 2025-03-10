// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Tests.Licensing;
using Metalama.Backstage.UserInterface;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

public sealed class ToastNotificationDetectionServiceTests : LicensingTestsBase
{
    private readonly IToastNotificationDetectionService _toastNotificationDetectionService;
    private readonly BackstageServicesInitializer _backstageServicesInitializer;

    public ToastNotificationDetectionServiceTests( ITestOutputHelper logger ) : base( logger )
    {
        this._toastNotificationDetectionService = this.ServiceProvider.GetRequiredBackstageService<IToastNotificationDetectionService>();
        this._backstageServicesInitializer = this.ServiceProvider.GetRequiredBackstageService<BackstageServicesInitializer>();
    }

    private async Task DetectToastNotificationsAsync()
    {
        this._toastNotificationDetectionService.Detect();
        await this.BackgroundTasks.WhenNoPendingTaskAsync();
    }

    [Theory]
    [InlineData( true, false )]
    [InlineData( false, false )]
    public async Task IsActivationSuggestedOnFirstRunAsync( bool isUserInteractive, bool shouldBeOpened )
    {
        this.UserDeviceDetection.IsInteractiveDevice = isUserInteractive;

        this._backstageServicesInitializer.Initialize();
        await this.DetectToastNotificationsAsync();

        if ( !shouldBeOpened )
        {
            Assert.Empty( this.UserInterface.Notifications );
        }
        else
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.RequiresLicense );
        }

        // Initializing a second time should not show a notification because of snoozing.
        this.UserInterface.Notifications.Clear();

        await this.DetectToastNotificationsAsync();
        Assert.Empty( this.UserInterface.Notifications );

        // After the snooze period, we should see a notification.
        this.Time.AddTime( ToastNotificationKinds.RequiresLicense.AutoSnoozePeriod.Add( TimeSpan.FromSeconds( 1 ) ) );

        await this.DetectToastNotificationsAsync();

        if ( !shouldBeOpened )
        {
            Assert.Empty( this.UserInterface.Notifications );
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
        this._backstageServicesInitializer.Initialize();
        await this.DetectToastNotificationsAsync();

        if ( expectedTitleSubstring == null )
        {
            Assert.Empty( this.UserInterface.Notifications );
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

        // Initialize
        this._backstageServicesInitializer.Initialize();
        await this.DetectToastNotificationsAsync();

        if ( expectedTitleSubstring == null )
        {
            Assert.Empty( this.UserInterface.Notifications );
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

        // Initialize
        this._backstageServicesInitializer.Initialize();
        await this.DetectToastNotificationsAsync();

        if ( extensionInstalled )
        {
            Assert.Empty( this.UserInterface.Notifications );
        }
        else
        {
            Assert.Single( this.UserInterface.Notifications, n => n.Kind == ToastNotificationKinds.VsxNotInstalled );
        }
    }
}