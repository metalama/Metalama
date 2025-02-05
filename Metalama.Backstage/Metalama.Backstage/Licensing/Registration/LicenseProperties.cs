// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Registration
{
    /// <summary>
    /// Information about a license relevant to license registration.
    /// </summary>
    /// <remarks>
    /// Properties returning a null value are not intended to be presented to a user.
    /// </remarks>
    [PublicAPI]
    public record LicenseProperties(
        string? LicenseString,
        string UniqueId,
        bool IsSelfCreated,
        int? LicenseId,
        string? Licensee,
        string Description,
        LicensedProduct Product,
        LicenseType LicenseType,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        bool? Perpetual,
        DateTime? SubscriptionEndDate,
        bool Auditable,
        bool LicenseServerEligible,
        Version MinPostSharpVersion );
}