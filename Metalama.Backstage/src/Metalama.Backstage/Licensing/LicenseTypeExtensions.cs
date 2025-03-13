// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing
{
    /// <summary>
    /// Provides extensions to <see cref="LicenseType" />.
    /// </summary>
    public static class LicenseTypeExtensions
    {
        /// <summary>
        /// Gets the name of the <paramref name="licenseType"/>.
        /// </summary>
        /// <param name="licenseType">The license type.</param>
        /// <returns>The name of the <paramref name="licenseType"/>.</returns>
        public static string GetLicenseTypeName( this LicenseType licenseType )
        {
            switch ( licenseType )
            {
                case LicenseType.Community:
                    return "Community License";

#pragma warning disable 618
                case LicenseType.Enterprise:
#pragma warning restore 618
                case LicenseType.Business:
                    return "Business License";

                case LicenseType.Site:
                    return "Site License";

                case LicenseType.Global:
                    return "Global License";

                case LicenseType.Evaluation:
                    return "Evaluation License";

                case LicenseType.Academic:
                    return "Academic License";

#pragma warning disable 618
                case LicenseType.OpenSourceRedistribution:
                    return "Open-Source Redistribution License";

                case LicenseType.CommercialRedistribution:
                    return "Commercial Redistribution License";

                case LicenseType.PerUsage:
                    return "Per-Usage Subscription";

                case LicenseType.Anonymous:
                    return "Anonymous License";
#pragma warning restore 618

                case LicenseType.Personal:
                    return "Personal License";
                
                case LicenseType.Test:
                    return "Test License";

                default:

                    // We don't want to display the license type for other licenses, because there may be
                    // a mismatch between what we sell (i.e. what is represented in the CRM and in the license certificate)
                    // and what is serialized into the license key.
                    return $"{licenseType} License";
            }
        }
    }
}