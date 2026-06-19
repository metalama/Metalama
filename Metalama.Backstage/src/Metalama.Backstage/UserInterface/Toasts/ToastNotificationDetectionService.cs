// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Welcome;
using System;

namespace Metalama.Backstage.UserInterface.Toasts;

internal sealed class ToastNotificationDetectionService : IToastNotificationDetectionService
{
    private readonly IToastNotificationService _toastNotificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IIdeExtensionStatusService? _ideExtensionStatusService;
    private readonly ILicenseRegistrationService? _licenseRegistrationService;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly WebLinks _webLinks;
    private readonly IConfigurationManager _configurationManager;
    private readonly ITelemetryConfigurationService? _telemetryConfigurationService;
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
        this._webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ToastNotificationDetectionService) );

        // The first-run telemetry notice is shown only once telemetry is actually activated, so re-run detection when
        // that happens (bypassing the throttle, since the process-init detection likely ran moments earlier). OnActivated
        // (rather than a plain event) ensures we still react if telemetry was already activated by this process before
        // this service was constructed. The telemetry configuration service is absent when support services are not
        // registered. See #1701.
        this._telemetryConfigurationService = serviceProvider.GetBackstageService<ITelemetryConfigurationService>();
        this._telemetryConfigurationService?.OnActivated( this.OnTelemetryActivated );
    }

    private void OnTelemetryActivated() => this._backgroundTasksService.Enqueue( () => this.DetectImpl( bypassThrottle: true ) );

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
                                $"Your Metalama trial {this.FormatExpiration( license.ValidTo.Value )}",
                                "Register a new license key to avoid losing functionality." ) );
                    }
                    else
                    {
                        this._toastNotificationService.Show(
                            new ToastNotification(
                                ToastNotificationKinds.LicenseExpiring,
                                $"Your Metalama license {this.FormatExpiration( license.ValidTo.Value )}",
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
                            $"Your Metalama subscription {this.FormatExpiration( license.SubscriptionEndDate.Value )}",
                            "Renew your subscription and register a new license key to continue benefiting from updates." ) );

                    notificationReported = true;
                }

                break;
        }
    }

    private void DetectImpl( bool bypassThrottle )
    {
        // Avoid too frequent detections for performance reasons. The threshold (here 15 seconds) should be lower
        // than the lowest auto-snooze period of a toast notification. Detection triggered by telemetry activation
        // bypasses the throttle so the first-run telemetry notice is not suppressed by the process-init detection.
        lock ( this._initializationSync )
        {
            if ( !bypassThrottle && this._lastTimeDetectionStarted > this._dateTimeProvider.UtcNow.Subtract( TimeSpan.FromSeconds( 15 ) ) )
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

        // Show the first-run telemetry notice exactly once, the first time telemetry is actually activated — so a
        // machine that never activates telemetry (e.g. one that only builds opted-out repositories) never sees it.
        // It is tracked by its own WelcomeConfiguration.TelemetryNoticeDisplayed flag (independent of WelcomePageDisplayed,
        // which also serves as an installation tracker), so the toast and the legacy welcome web page are independent.
        // See #1701.
        if ( this._telemetryConfigurationService?.IsActivated == true
             && this._configurationManager.UpdateIf<WelcomeConfiguration>(
                 c => !c.TelemetryNoticeDisplayed,
                 c => c with { TelemetryNoticeDisplayed = true } ) )
        {
            this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.TelemetryNotice ) );
        }

        // Validate registered licenses, but do not complain about the lack of licenses.
        if ( this._licenseRegistrationService != null )
        {
            foreach ( var license in this._licenseRegistrationService.RegisteredLicenses )
            {
                this.ValidateRegisteredLicense( license, ref notificationReported );
            }
        }

        // Suggest to install Visual Studio Tools for Metalama.
        if ( !notificationReported && this._ideExtensionStatusService?.ShouldRecommendToInstallVisualStudioExtension == true )
        {
            this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.VsxNotInstalled, Uri: this._webLinks.InstallVsx ) );
        }
    }

    public void Detect() => this._backgroundTasksService.Enqueue( () => this.DetectImpl( bypassThrottle: false ) );
}