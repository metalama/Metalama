// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;

namespace Metalama.Backstage.Application
{
    /// <summary>
    /// Extension methods of <see cref="IApplicationInfo"/> that licensing uses to identify the components that the
    /// vendor of the product built.
    /// </summary>
    internal static class ApplicationInfoLicenseExtensions
    {
        /// <summary>
        /// Gets the version of the application that decides which license keys apply to it.
        /// </summary>
        public static Version GetLicensingVersion( this IApplicationInfo application ) => application.AssemblyVersion ?? new Version( 0, 0 );

        private static bool IsPreviewLicenseEligible( this IComponentInfo component, string company )
            => (component.IsPrerelease ?? false) && component.BuildDate != null && component.Company == company;

        /// <summary>
        /// Determines whether the application, or one of its components built by the vendor, is a prerelease build
        /// that is eligible for a preview license.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="company">The name of the vendor, as given by <see cref="ProductProfile.Company"/>.</param>
        public static bool IsPreviewLicenseEligible( this IApplicationInfo application, string company )
            => ((IComponentInfo) application).IsPreviewLicenseEligible( company ) || application.Components.Any( c => c.IsPreviewLicenseEligible( company ) );

        /// <summary>
        /// Gets the component of the application that the vendor built most recently. It is the application itself
        /// when none of the components has a later build date. Licensing compares the build date of this component
        /// with the subscription end date of a license.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="company">The name of the vendor, as given by <see cref="ProductProfile.Company"/>.</param>
        /// <exception cref="InvalidOperationException">A component built by the vendor has no build date.</exception>
        public static IComponentInfo GetLatestVendorComponent( this IApplicationInfo application, string company )
        {
            IComponentInfo latestComponentLicensedByBuildDate = application;

            foreach ( var component in application.Components )
            {
                if ( component.Company != company )
                {
                    continue;
                }

                if ( !component.BuildDate.HasValue )
                {
                    throw new InvalidOperationException( $"Application component '{component.Name}' is missing build date information." );
                }

                if ( latestComponentLicensedByBuildDate.BuildDate <= component.BuildDate )
                {
                    latestComponentLicensedByBuildDate = component;
                }
            }

            return latestComponentLicensedByBuildDate;
        }
    }
}
