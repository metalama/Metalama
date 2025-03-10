// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Registration;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// A license providing licensed features.
    /// </summary>
    internal interface ILicense
    {
        /// <summary>
        /// Gets a value indicating whether the license can be registered. If not, returns the reason.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        bool CanBeRegistered( [MaybeNullWhen( true )] out string errorMessage );

        /// <summary>
        /// Tries to retrieves or deserialize and validate license data relevant to license consumption.
        /// The data is either deserialized (e.g. from a license key) or retrieved from a license provider (e.g. license server.)
        /// </summary>
        /// <param name="options"></param>
        /// <param name="licenseConsumptionProperties">The license data relevant to license consumption.</param>
        /// <param name="errorMessage">Description of a failure when the return value is <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the object represents or retrieves a consistent and valid license.
        /// </returns>
        bool TryGetConsumptionProperties(
            LicenseConsumptionOptions options,
            [MaybeNullWhen( false )] out LicenseConsumptionProperties licenseConsumptionProperties,
            [MaybeNullWhen( true )] out string errorMessage );

        /// <summary>
        /// Tries to deserialize data relevant to license registration but does not attempt to validate all properties.
        /// </summary>
        /// <param name="licenseProperties">The license data relevant to license registration.</param>
        /// <param name="errorMessage">Description of a failure when the return value is <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the object represents a consistent license.
        /// </returns>
        bool TryGetRegistrationProperties(
            [MaybeNullWhen( false )] out LicenseRegistrationProperties licenseProperties,
            [MaybeNullWhen( true )] out string errorMessage );

        /// <summary>
        /// This method must be called once per day when the license is consumed.
        /// </summary>
        void OnConsumed();
    }
}