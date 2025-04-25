// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    /// <summary>
    /// Identifies a field in a license key. It is given as the first byte of a field binary data.
    /// </summary>
    internal enum LicenseFieldIndex : byte
    {
        [Obsolete( "This field is no longer used. It has been replaced by former LicensedProducts enum, which has been renamed to LicensedFeatures." )]
        Features = 1,
        ValidFrom = 2,
        ValidTo = 3,
        Licensee = 4,
        Namespace = 5,
        PublicKeyToken = 8,
        UserNumber = 9,
        Signature = 10,
        SignatureKeyId = 11,
        LicenseeHash = 12,
        GraceDays = 15,
        GracePercent = 16,
        DevicesPerUser = 17,
        SubscriptionEndDate = 18,
        Auditable = 19,
        AllowInheritance = 20,
        LicenseServerEligible = 21,

        // 128 is reserved as unknown must-understand field for testing purposes
        Generation = 129,
        // 130 was used for "origin version" concept, but was removed before released (there may be test keys using this field).
        ServicingPhase = 131,

        // 253 is reserved as unknown optional field for testing purposes
        MinPostSharpVersion = 254,
        End = 255
    }
}