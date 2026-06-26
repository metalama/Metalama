// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface.Rss;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface.Toasts;

internal sealed class ToastNotificationDetectionService : IToastNotificationDetectionService, IDisposable
{
    private readonly IToastNotificationService _toastNotificationService;
    private readonly IToastNotificationStatusService _toastNotificationStatusService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IIdeExtensionStatusService? _ideExtensionStatusService;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly WebLinks _webLinks;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new( 1 );
    private readonly IRssClient? _rssClient;
    private readonly ILicenseRegistrationService? _licenseRegistrationService;

    private DateTime _lastTimeDetectionEnqueued;

    public ToastNotificationDetectionService( IServiceProvider serviceProvider )
    {
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._ideExtensionStatusService = serviceProvider.GetBackstageService<IIdeExtensionStatusService>();
        this._toastNotificationService = serviceProvider.GetRequiredBackstageService<IToastNotificationService>();
        this._toastNotificationStatusService = serviceProvider.GetRequiredBackstageService<IToastNotificationStatusService>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ToastNotificationDetectionService) );
        this._rssClient = serviceProvider.GetBackstageService<IRssClient>();
        this._licenseRegistrationService = serviceProvider.GetBackstageService<ILicenseRegistrationService>();
    }

    private async Task DetectImplAsync( ITelemetryContext? telemetryContext )
    {
        await this._semaphoreSlim.WaitAsync();

        try
        {
            if ( !this._userDeviceDetectionService.IsInteractiveDevice )
            {
                this._logger.Trace?.Log( "Skipping detection because the session is not interactive." );

                return;
            }

            this._logger.Trace?.Log( "Detecting relevant toast notifications." );

            var notificationReported = false;

            this.DetectHighPriorityNotifications( ref notificationReported );

            if ( !this._toastNotificationStatusService.CanDisplayLowPriorityNotifications )
            {
                this._logger.Trace?.Log( $"Skipping detection because it's not a good time to display low-priority notifications." );

                return;
            }

            await this.DetectLowPriorityNotificationsAsync( notificationReported, telemetryContext );
        }
        finally
        {
            this._semaphoreSlim.Release();
        }
    }

    private async Task DetectLowPriorityNotificationsAsync( bool notificationReported, ITelemetryContext? telemetryContext )
    {
        // Suggest to install Visual Studio Tools for Metalama.
        if ( !notificationReported && this._ideExtensionStatusService?.ShouldRecommendToInstallVisualStudioExtension == true )
        {
            this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.VsxNotInstalled, Uri: this._webLinks.InstallVsx ) );
            notificationReported = true;
        }

        // Display news.
        if ( !notificationReported && this._rssClient != null && telemetryContext != null )
        {
            await this._rssClient.DisplayUnreadLatestNewsAsync( telemetryContext );
        }
    }

    private void DetectHighPriorityNotifications( ref bool notificationReported )
    {
        // Validate registered licenses, but do not complain about the lack of licenses.

        if ( this._licenseRegistrationService != null )
        {
            foreach ( var license in this._licenseRegistrationService.RegisteredLicenses )
            {
                this.ValidateRegisteredLicense( license, ref notificationReported );
            }
        }
    }

    private void ValidateRegisteredLicense( LicenseRegistrationProperties? license, ref bool notificationReported )
    {
        // We set notificationReported to true even if the notification is not reported because of snoozing
        // because the reason of this flag is to avoid displaying VsxNotInstalled.

        switch ( license )
        {
            case { ValidTo: not null }
                when license.ValidTo.Value - LicensingConstants.LicenseExpirationWarningPeriod < this._dateTimeProvider.UtcNow:
                {
                    if ( license.LicenseType == LicenseType.Evaluation )
                    {
                        this._toastNotificationService.Show(
                            new ToastNotification(
                                ToastNotificationKinds.TrialExpiring,
                                $"Your Metalama trial {FormatExpiration( license.ValidTo.Value )}",
                                "Register a new license key to avoid losing functionality." ) );
                    }
                    else
                    {
                        this._toastNotificationService.Show(
                            new ToastNotification(
                                ToastNotificationKinds.LicenseExpiring,
                                $"Your Metalama license {FormatExpiration( license.ValidTo.Value )}",
                                "Register a new license key to avoid losing functionality." ) );
                    }

                    notificationReported = true;

                    break;
                }

            case { SubscriptionEndDate: not null }
                when license.SubscriptionEndDate.Value - LicensingConstants.SubscriptionExpirationWarningPeriod < this._dateTimeProvider.UtcNow:

                if ( license.Product == LicenseProduct.MetalamaCommunity )
                {
                    // TODO.
                }
                else if ( license.LicenseType == LicenseType.Evaluation )
                {
                    // Nothing to do.
                }
                else
                {
                    this._toastNotificationService.Show(
                        new ToastNotification(
                            ToastNotificationKinds.SubscriptionExpiring,
                            $"Your Metalama subscription {FormatExpiration( license.SubscriptionEndDate.Value )}",
                            "Renew your subscription and register a new license key to continue benefiting from updates." ) );

                    notificationReported = true;
                }

                break;
        }

        string FormatExpiration( DateTime expiration )
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
    }

    public Task DetectAsync( ITelemetryContext? telemetryContext )
    {
        // Avoid too frequent detections for performance reasons. We take the throttle period quite arbitrarily
        // because we don't have a case that requires more frequent detection.
        // This throttling deliberately ignores the telemetry context.
        if ( this._lastTimeDetectionEnqueued > this._dateTimeProvider.UtcNow.Subtract( ToastNotificationStatusService.LowPriorityThrottlePeriod ) )
        {
            this._logger.Trace?.Log( "Skipping detection because it has been performed recently." );

            return Task.CompletedTask;
        }
        
        this._lastTimeDetectionEnqueued = this._dateTimeProvider.UtcNow;
        
        return this._backgroundTasksService.Enqueue( () => this.DetectImplAsync( telemetryContext ) );
    }

    public void Dispose() => this._semaphoreSlim.Dispose();
}