// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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