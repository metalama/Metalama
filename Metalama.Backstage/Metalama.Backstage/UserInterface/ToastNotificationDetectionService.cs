// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;

namespace Metalama.Backstage.UserInterface;

internal class ToastNotificationDetectionService : IToastNotificationDetectionService
{
    private readonly IToastNotificationService _toastNotificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IIdeExtensionStatusService? _ideExtensionStatusService;
    private readonly ILicenseRegistrationService? _licenseRegistrationService;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly object _initializationSync = new();
    private readonly ILogger _logger;

    private DateTime _lastTimeDetectionStarted;

    public ToastNotificationDetectionService( IServiceProvider serviceProvider )
    {
        this._licenseRegistrationService = serviceProvider.GetBackstageService<ILicenseRegistrationService>();
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._ideExtensionStatusService = serviceProvider.GetBackstageService<IIdeExtensionStatusService>();
        this._toastNotificationService = serviceProvider.GetRequiredBackstageService<IToastNotificationService>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
    }

    private string FormatExpiration( DateTime expiration )
    {
        var daysToExpiration = (int) Math.Floor( (expiration - this._dateTimeProvider.UtcNow).TotalDays );

        return daysToExpiration switch
        {
            < 0 => "has expired",
            0 => "expires today",
            1 => "expires tomorrow",
            _ => $"expires in {daysToExpiration} days"
        };
    }

    private void ValidateRegisteredLicense( LicenseProperties? license, ref bool notificationReported )
    {
        // We set notificationReported to true even if the notification is not reported because of snoozing
        // because the reason of this flag is to avoid displaying VsxNotInstalled.

        switch ( license )
        {
            case null:
                this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.RequiresLicense ) );

                notificationReported = true;

                break;

            case { ValidTo: not null }
                when license.ValidTo.Value.Subtract( LicensingConstants.LicenseExpirationWarningPeriod ) < this._dateTimeProvider.UtcNow:
                {
                    if ( license.LicenseType == LicenseType.Evaluation )
                    {
                        this._toastNotificationService.Show(
                            new ToastNotification(
                                ToastNotificationKinds.TrialExpiring,
                                $"Your Metalama trial {this.FormatExpiration( license.ValidTo.Value )}",
                                "Switch to Metalama [Free] or register a license key to avoid loosing functionality." ) );
                    }
                    else
                    {
                        this._toastNotificationService.Show(
                            new ToastNotification(
                                ToastNotificationKinds.LicenseExpiring,
                                $"Your Metalama license {this.FormatExpiration( license.ValidTo.Value )}",
                                "Register a new license license key to avoid loosing functionality." ) );
                    }

                    notificationReported = true;

                    break;
                }

            case { SubscriptionEndDate: not null }
                when license.LicenseType != LicenseType.Evaluation // We only show license expiration warnings for evaluation licenses.
                     && license.SubscriptionEndDate.Value.Subtract( LicensingConstants.SubscriptionExpirationWarningPeriod ) < this._dateTimeProvider.UtcNow:
                this._toastNotificationService.Show(
                    new ToastNotification(
                        ToastNotificationKinds.SubscriptionExpiring,
                        $"Your Metalama subscription {this.FormatExpiration( license.SubscriptionEndDate.Value )}",
                        "Renew your subscription and register a new license key to continue benefiting from updates." ) );

                notificationReported = true;

                break;
        }
    }

    private void DetectImpl( ToastNotificationDetectionOptions options )
    {
        // Avoid too frequent detections for performance reasons. The threshold (here 15 seconds) should be lower
        // than the lowest auto-snooze period of a toast notification.
        lock ( this._initializationSync )
        {
            if ( this._lastTimeDetectionStarted > this._dateTimeProvider.UtcNow.Subtract( TimeSpan.FromSeconds( 15 ) ) )
            {
                this._logger.Trace?.Log( "Skipping detection because it has been performed recently." );

                return;
            }

            this._lastTimeDetectionStarted = this._dateTimeProvider.UtcNow;
        }

        var notificationReported = false;

        if ( !this._userDeviceDetectionService.IsInteractiveDevice )
        {
            this._logger.Trace?.Log( "Skipping detection because the session is not interactive." );

            return;
        }

        this._logger.Trace?.Log( "Detecting relevant toast notifications." );

        if ( this._licenseRegistrationService != null && !options.HasValidLicense )
        {
            this.ValidateRegisteredLicense( this._licenseRegistrationService.RegisteredLicense, ref notificationReported );
        }

        // TODO: Show a toast notification suggesting to subscribe the newsletter. (34701)

        if ( !notificationReported && this._ideExtensionStatusService?.ShouldRecommendToInstallVisualStudioExtension == true )
        {
            this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.VsxNotInstalled ) );
        }
    }

    public void Detect( ToastNotificationDetectionOptions? options )
        => this._backgroundTasksService.Enqueue( () => this.DetectImpl( options ?? new ToastNotificationDetectionOptions() ) );
}