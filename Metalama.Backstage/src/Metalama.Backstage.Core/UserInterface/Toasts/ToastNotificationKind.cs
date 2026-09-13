// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.UserInterface.Toasts;

[PublicAPI]
public sealed record ToastNotificationKind( string Name )
{
    /// <summary>
    /// Gets the period when the notification should not be displayed after it has been displayed.
    /// </summary>
    internal TimeSpan AutoSnoozePeriod { get; init; } = TimeSpan.FromHours( 1 );

    public TimeSpan ManualSnoozePeriod { get; init; } = TimeSpan.FromDays( 1 );

    /// <summary>
    /// Gets a value indicating whether this is a low-priority notification that should not be displayed shortly after
    /// another notification was displayed (see <c>ToastNotificationStatusService</c>). Blocking or urgent notifications
    /// (e.g. a required license, which fails the build, or an exception) are not throttled. The default is <c>false</c>.
    /// </summary>
    internal bool IsThrottled { get; init; }

    /// <summary>
    /// Gets a value indicating whether this kind of notification can be muted, i.e. disabled for good. The default is
    /// <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Muting is permanent and cannot be undone from the product, so it is unacceptable for a notification that is the
    /// only way to take an action, such as approving an exception report. Such a kind sets this to <c>false</c>: the
    /// notification offers no Mute button, <see cref="IToastNotificationStatusService.Mute"/> is a no-op, and a mute
    /// stored by an earlier version is ignored, so users who muted before the button was removed start seeing the
    /// notification again. See #1751.
    /// </remarks>
    internal bool CanBeMuted { get; init; } = true;
}