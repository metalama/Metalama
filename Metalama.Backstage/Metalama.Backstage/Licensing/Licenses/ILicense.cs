// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Registration;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// A license providing licensed features.
    /// </summary>
    public interface ILicense
    {
        /// <summary>
        /// Tries to retrieves or deserialize and validate license data relevant to license consumption.
        /// The data is either deserialized (e.g. from a license key) or retrieved from a license provider (e.g. license server.)
        /// </summary>
        /// <param name="licenseConsumptionData">The license data relevant to license consumption.</param>
        /// <param name="errorMessage">Description of a failure when the return value is <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the object represents or retrieves a consistent and valid license.
        /// </returns>
        bool TryGetLicenseConsumptionData(
            [MaybeNullWhen( false )] out LicenseConsumptionData licenseConsumptionData,
            [MaybeNullWhen( true )] out string errorMessage );

        /// <summary>
        /// Tries to deserialize data relevant to license registration.
        /// </summary>
        /// <param name="licenseProperties">The license data relevant to license registration.</param>
        /// <param name="errorMessage">Description of a failure when the return value is <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the object represents a consistent license.
        /// </returns>
        bool TryGetProperties(
            [MaybeNullWhen( false )] out LicenseProperties licenseProperties,
            [MaybeNullWhen( true )] out string errorMessage );
    }
}