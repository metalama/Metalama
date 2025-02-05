// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.UserInterface;

public record ToastNotificationKind( string Name )
{
    /// <summary>
    /// Gets the period when the notification should not be displayed after it has been displayed.
    /// </summary>
    internal TimeSpan AutoSnoozePeriod { get; init; } = TimeSpan.FromHours( 1 );

    public TimeSpan ManualSnoozePeriod { get; init; } = TimeSpan.FromDays( 1 );
}