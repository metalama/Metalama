// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.UserInterface;

public static class ToastNotificationKinds
{
    public static ToastNotificationKind RequiresLicense { get; } = new( nameof(RequiresLicense) ) { AutoSnoozePeriod = TimeSpan.FromMinutes( 1 ) };

    public static ToastNotificationKind VsxNotInstalled { get; } = new( nameof(VsxNotInstalled) ) { AutoSnoozePeriod = TimeSpan.FromHours( 1 ) };

    public static ToastNotificationKind SubscriptionExpiring { get; } =
        new( nameof(SubscriptionExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 7 ) };

    public static ToastNotificationKind TrialExpiring { get; } =
        new( nameof(TrialExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 3 ) };

    public static ToastNotificationKind LicenseExpiring { get; } =
        new( nameof(LicenseExpiring) ) { AutoSnoozePeriod = TimeSpan.FromDays( 1 ), ManualSnoozePeriod = TimeSpan.FromDays( 3 ) };

    public static ToastNotificationKind Exception { get; } =
        new( nameof(Exception) ) { AutoSnoozePeriod = TimeSpan.FromSeconds( 5 ), ManualSnoozePeriod = TimeSpan.FromHours( 1 ) };

    public static ImmutableDictionary<string, ToastNotificationKind> All { get; } =
        new[] { RequiresLicense, VsxNotInstalled, SubscriptionExpiring, TrialExpiring, LicenseExpiring, Exception }.ToImmutableDictionary( i => i.Name, i => i );
}