// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Licensing.Registration;

internal static class LicensingConstants
{
    /// <summary>
    /// Gets the time span of the evaluation license validity.
    /// </summary>
    internal static TimeSpan EvaluationPeriod { get; } = TimeSpan.FromDays( 45 );

    /// <summary>
    /// Gets the time span from the end of an evaluation license validity
    /// in which a new evaluation license cannot be registered.
    /// </summary>
    internal static TimeSpan NoEvaluationPeriod { get; } = TimeSpan.FromDays( 120 );

    public static TimeSpan LicenseExpirationWarningPeriod { get; } = TimeSpan.FromDays( 7 );

    public static TimeSpan SubscriptionExpirationWarningPeriod { get; } = TimeSpan.FromDays( 30 );
}