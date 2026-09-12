// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Welcome;
using System;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// Subscribes the user interface to the events of the lower packages: it shows a notification when an exception
/// report is captured, when a license requirement is not satisfied, and when telemetry is activated, and it opens the
/// welcome page in the latter case. It is the reason why the telemetry and licensing services do not reference the
/// user interface.
/// </summary>
/// <remarks>
/// The subscriber is created eagerly by the services initializer, because a service that nobody resolves is never
/// created and would therefore never subscribe.
/// </remarks>
internal sealed class UserInterfaceEventSubscriber : IBackstageService, IDisposable
{
    private readonly IToastNotificationService _toastNotificationService;
    private readonly WelcomePageService? _welcomePageService;
    private readonly ILicenseProductCatalog? _catalog;
    private readonly IDisposable[] _subscriptions;

    public UserInterfaceEventSubscriber( IServiceProvider serviceProvider )
    {
        this._toastNotificationService = serviceProvider.GetRequiredBackstageService<IToastNotificationService>();
        this._welcomePageService = serviceProvider.GetBackstageService<WelcomePageService>();
        this._catalog = serviceProvider.GetBackstageService<ILicenseProductCatalog>();

        var dispatcher = serviceProvider.GetRequiredBackstageService<IEventDispatcher>();

        this._subscriptions =
        [
            dispatcher.Subscribe<ExceptionReportCaptured>( this.OnExceptionReportCaptured ),
            dispatcher.Subscribe<TelemetryActivated>( this.OnTelemetryActivated ),
            dispatcher.Subscribe<LicenseRequirementNotSatisfied>( this.OnLicenseRequirementNotSatisfied )
        ];
    }

    private void OnExceptionReportCaptured( ExceptionReportCaptured @event )
    {
        var category = @event.Scenario == TelemetryScenario.Performance ? "performance problem" : "exception";

        // For a review-first report the call to action is to review and send; for an auto-sent report the notification
        // is purely informational (clicking it still opens the page, which shows what was reported).
        var callToAction = @event.AutoSent ? "Click to review what was reported." : "Click to review and report it.";

        // The Uri carries only the bare report file name, not a page path, so it stays specifically an exception-report
        // reference rather than an arbitrary URL. It is token-safe (no spaces), so the desktop notifier can pass it as
        // a single command argument; the desktop command builds the review-page path itself. The category is stored
        // inside the report, so it is not passed here. See #1674.
        this._toastNotificationService.Show(
            new ToastNotification(
                ToastNotificationKinds.ExceptionReport,
                Text: $"The process {@event.ApplicationName} encountered an unexpected {category}. {callToAction}",
                Uri: @event.ReportFileName ) );
    }

    private void OnTelemetryActivated( TelemetryActivated @event )
    {
        this._toastNotificationService.Show( new ToastNotification( ToastNotificationKinds.TelemetryNotice ) );
        this._welcomePageService?.OpenWelcomePageOnce();
    }

    private void OnLicenseRequirementNotSatisfied( LicenseRequirementNotSatisfied @event )
    {
        this._toastNotificationService.Show(
            new ToastNotification(
                ToastNotificationKinds.RequiresLicense,
                this._catalog?.PremiumEditionDisplayName ?? "Premium edition",
                @event.Message + "Open to start a trial or register a license key." ) );
    }

    public void Dispose()
    {
        foreach ( var subscription in this._subscriptions )
        {
            subscription.Dispose();
        }
    }
}
