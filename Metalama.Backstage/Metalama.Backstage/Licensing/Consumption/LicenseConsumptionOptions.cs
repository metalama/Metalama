// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using System;

namespace Metalama.Backstage.Licensing.Consumption;

public sealed record LicenseConsumptionOptions
{
    public string? ProjectLicenseKey { get; init; }

    public TimeSpan? SubscriptionGracePeriod { get; init; }

    public LicenseSourceKind IgnoredLicenseSources { get; init; } = LicenseSourceKind.None;

    internal bool IgnoreSubscriptionPeriod { get; private init; }

    public bool RequireActiveOrGraceSubscription { get; init; }

    public static LicenseConsumptionOptions Default { get; } = new();

    // While registering license keys, we ignore the subscription period, i.e. we allow to register the license key for any build
    // of the application. This allows to use a recent build of the licensing registration service to register a license key that will
    // be used by a more recent consumer.
    internal static LicenseConsumptionOptions ForRegistration { get; } = new() { IgnoreSubscriptionPeriod = true };
}