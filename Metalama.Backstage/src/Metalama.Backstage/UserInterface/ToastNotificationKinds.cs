// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    public static ToastNotificationKind Welcome { get; } = new( nameof(Welcome) );

    // Must be last.
    public static ImmutableDictionary<string, ToastNotificationKind> All { get; } =
        new[] { RequiresLicense, VsxNotInstalled, SubscriptionExpiring, TrialExpiring, LicenseExpiring, Exception, Welcome }.ToImmutableDictionary(
            i => i.Name,
            i => i );
}