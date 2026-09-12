// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.UserInterface.Toasts;

public static class ToastNotificationKinds
{
    public static ToastNotificationKind RequiresLicense { get; } = new( nameof(RequiresLicense) ) { AutoSnoozePeriod = TimeSpan.FromMinutes( 1 ) };

    // Throttled: the VSX-install prompt is low-priority, so it is deferred when another notification (e.g. the
    // first-run telemetry notice) was shown recently, to avoid showing two toasts at once. See #1692.
    public static ToastNotificationKind VsxNotInstalled { get; } =
        new( nameof(VsxNotInstalled) ) { AutoSnoozePeriod = TimeSpan.FromHours( 1 ), IsThrottled = true };

    public static ToastNotificationKind SubscriptionExpiring { get; } =
        new( nameof(SubscriptionExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 7 ) };

    public static ToastNotificationKind TrialExpiring { get; } =
        new( nameof(TrialExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 3 ) };

    public static ToastNotificationKind LicenseExpiring { get; } =
        new( nameof(LicenseExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 3 ) };

    /// <summary>
    /// The review notification for exception and performance reports.
    /// </summary>
    /// <remarks>
    /// This notification is the only way to approve an error report, so it cannot be muted: muting it silenced error
    /// reporting altogether, permanently and with no way back from the product. The review page offers the per-issue
    /// equivalent ("never report this error") and the privacy page remains the visible, reversible way to turn the whole
    /// channel off.
    /// <para>
    /// The kind was renamed from <c>Exception</c> to <c>ExceptionReport</c> in 2026.1.22. Per-kind state in
    /// <c>toastNotifications.json</c> is keyed by this name, so the rename discards whatever was stored under the old
    /// name: users who muted the notification while earlier versions still offered a Mute button start seeing it again,
    /// instead of staying silenced for good. <see cref="ToastNotificationKind.CanBeMuted"/> then keeps it that way.
    /// See #1751.
    /// </para>
    /// </remarks>
    public static ToastNotificationKind ExceptionReport { get; } =
        new( nameof(ExceptionReport) )
        {
            AutoSnoozePeriod = TimeSpan.FromSeconds( 5 ), ManualSnoozePeriod = TimeSpan.FromHours( 1 ), CanBeMuted = false
        };

    // Auto-snooze for RSS news is redundant because we are checking once per day anyway. Setting this to zero eases testing through the `rss notify` CLI command.
    public static ToastNotificationKind News { get; } = new( nameof(News) ) { AutoSnoozePeriod = TimeSpan.Zero };

    // First-run telemetry notice. Shown only once (tracked in WelcomeConfiguration), so the snooze periods are not relevant.
    public static ToastNotificationKind TelemetryNotice { get; } = new( nameof(TelemetryNotice) ) { AutoSnoozePeriod = TimeSpan.Zero };

    // Must be last.
    public static ImmutableDictionary<string, ToastNotificationKind> All { get; } =
        new[] { RequiresLicense, VsxNotInstalled, SubscriptionExpiring, TrialExpiring, LicenseExpiring, ExceptionReport, News, TelemetryNotice }
            .ToImmutableDictionary(
                i => i.Name,
                i => i );
}