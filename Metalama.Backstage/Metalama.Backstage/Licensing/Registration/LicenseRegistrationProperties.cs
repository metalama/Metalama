// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    public record LicenseRegistrationProperties(
        string LicenseString,
        string UniqueId,
        bool IsSelfCreated,
        int? LicenseId,
        string? Licensee,
        string Description,
        LicenseProduct Product,
        LicenseType LicenseType,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        bool? Perpetual,
        DateTime? SubscriptionEndDate,
        bool Auditable,
        bool LicenseServerEligible,
        Version MinPostSharpVersion,
        LicenseGeneration Generation ) { }
}