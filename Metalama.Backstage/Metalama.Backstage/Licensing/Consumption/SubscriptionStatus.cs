// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing.Consumption;

public enum SubscriptionStatus
{
    /// <summary>
    /// The license has no subscription field.
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