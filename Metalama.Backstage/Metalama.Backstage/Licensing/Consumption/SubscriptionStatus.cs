// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing.Consumption;

public enum SubscriptionStatus
{
    /// <summary>
    /// The license has no subscription field or the subscription status is not applicable
    /// because the license key was generated before an active subscription was a requirement for any component.
    /// </summary>
    None,

    /// <summary>
    /// The license is active.
    /// </summary>
    Active,

    /// <summary>
    /// The license is past its end date, but within <see cref="LicenseConsumptionOptions.SubscriptionGracePeriod"/>.
    /// </summary>
    Grace,

    /// <summary>
    /// The license is past its end date and the optional <see cref="LicenseConsumptionOptions.SubscriptionGracePeriod"/>.
    /// </summary>
    Expired
}