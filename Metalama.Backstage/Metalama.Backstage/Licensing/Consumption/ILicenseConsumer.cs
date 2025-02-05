// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public interface ILicenseConsumer
{
    /// <summary>
    /// Provides information about availability of <paramref name="requirement"/>.
    /// </summary>
    /// <param name="requirement">The required license requirement.</param>
    /// <param name="consumerNamespace">The consuming namespace, or <c>null</c> if this is a global feature.</param>
    /// <returns>A value indicating if the <paramref name="requirement"/> is available.</returns>
    bool CanConsume( LicenseRequirement requirement, string? consumerNamespace = null );

    bool IsTrialLicense { get; }

    bool IsRedistributionLicense { get; }

    string? LicenseString { get; }
}