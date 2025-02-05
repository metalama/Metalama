// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        // 253 is reserved as unknown optional field for testing purposes
        MinPostSharpVersion = 254,
        End = 255
    }
}