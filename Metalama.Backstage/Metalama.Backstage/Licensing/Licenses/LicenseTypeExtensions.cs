// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing.Licenses
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
#pragma warning disable 618
            switch ( licenseType )
            {
                case LicenseType.Essentials:
                    return "Essentials License";

                case LicenseType.Enterprise:
                case LicenseType.PerUser:
                    return "Per-Developer Subscription";

                case LicenseType.Site:
                    return "Site License";

                case LicenseType.Global:
                    return "Global License";

                case LicenseType.Evaluation:
                    return "Evaluation License";

                case LicenseType.Academic:
                    return "Academic License";

                case LicenseType.OpenSourceRedistribution:
                    return "Open-Source Redistribution License";

                case LicenseType.CommercialRedistribution:
                    return "Commercial Redistribution License";

                case LicenseType.PerUsage:
                    return "Per-Usage Subscription";

#pragma warning disable 618
                case LicenseType.Anonymous:
                    return "Anonymous License";
#pragma warning restore 618

                case LicenseType.Personal:
                    return "Personal License";

                default:

                    // We don't want to display the license type for other licenses, because there may be
                    // a mismatch between what we sell (i.e. what is represented in the CRM and in the license certificate)
                    // and what is serialized into the license key.
                    return "License";
            }
        }
    }
}