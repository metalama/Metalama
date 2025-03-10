// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption.Sources;
using System;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public sealed record LicenseConsumptionOptions
{
    public string? ProjectLicenseKey { get; init; }
    
    public string? ProjectName { get; init; }

    public TimeSpan? SubscriptionGracePeriod { get; init; }
    
    public LicenseSourceKind IgnoredLicenseSources { get; init; } = LicenseSourceKind.None;

    /// <summary>
    /// Gets a value indicating whether to ignore the condition that requires the build date to be within the subscription period.
    /// This property is enabled when license keys are registered, so the build date of the registration tool does not interfere
    /// (only the build date of the consuming tool matters).
    /// </summary>
    internal bool IgnoreSubscriptionPeriod { get; private init; }

    /// <summary>
    /// Gets a value indicating whether obsolete license types and products should be accepted.
    /// This property is enabled when license keys are registered, because the registration tool may be of a
    /// more recent version than the consuming tool.
    /// </summary>
    internal bool AcceptsObsoleteLicenses { get; private init; }

    public static LicenseConsumptionOptions Default { get; } = new();

    // While registering license keys, we ignore the subscription period, i.e. we allow to register the license key for any build
    // of the application. This allows to use a recent build of the licensing registration service to register a license key that will
    // be used by a more recent consumer.
    internal static LicenseConsumptionOptions ForRegistration { get; } = new() { IgnoreSubscriptionPeriod = true, AcceptsObsoleteLicenses = true };
}