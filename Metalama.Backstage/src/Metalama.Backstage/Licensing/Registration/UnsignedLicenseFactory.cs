// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Registration
{
    /// <summary>
    /// Creates unsigned licenses for self-registration.
    /// </summary>
    internal sealed class UnsignedLicenseFactory
    {
        private readonly IDateTimeProvider _time;
        private readonly RandomNumberGenerator _randomNumberGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsignedLicenseFactory"/> class.
        /// </summary>
        /// <param name="services">Services.</param>
        public UnsignedLicenseFactory( IServiceProvider services )
        {
            this._time = services.GetRequiredBackstageService<IDateTimeProvider>();
            this._randomNumberGenerator = services.GetRequiredBackstageService<RandomNumberGenerator>();
        }

        /// <summary>
        /// Creates an unsigned evaluation license.
        /// </summary>
        /// <returns>The unsigned evaluation license.</returns>
        public LicenseRegistrationProperties CreateEvaluationLicense()
        {
            var start = this._time.UtcNow.Date;
            var end = start + LicensingConstants.EvaluationPeriod;

            var licenseKeyData = new LicenseKeyDataBuilder()
            {
                Generation = LicenseGeneration.Current,
                LicenseGuid = this._randomNumberGenerator.NextGuid(),
                Product = LicenseProduct.MetalamaProfessional,
                LicenseType = LicenseType.Evaluation,
                ValidFrom = start,
                ValidTo = end,
                SubscriptionEndDate = end
            };

            return licenseKeyData.Build().ToLicenseRegistrationProperties( licenseKeyData.Serialize() );
        }

        /// <summary>
        /// Creates an unsigned Metalama Community license.
        /// </summary>
        /// <returns>The unsigned Metalama Community license.</returns>
        public LicenseRegistrationProperties CreateCommunityLicense()
        {
            var start = this._time.UtcNow;

            var licenseKeyData = new LicenseKeyDataBuilder()
            {
                Generation = LicenseGeneration.Current,
                LicenseGuid = this._randomNumberGenerator.NextGuid(),
                Product = LicenseProduct.MetalamaCommunity,
                LicenseType = LicenseType.Community,
                ValidFrom = start,

                // Must be renewed yearly.
                ValidTo = start.AddYears( 1 )
            };

            var licenseRegistrationData = licenseKeyData.Build().ToLicenseRegistrationProperties( licenseKeyData.Serialize() );

            return licenseRegistrationData;
        }

        /// <summary>
        /// Creates an unsigned legacy Metalama Free license.
        /// </summary>
        /// <returns>The unsigned Metalama Community license.</returns>
        [Obsolete]
        public LicenseRegistrationProperties CreateLegacyFreeLicense()
        {
            var start = this._time.UtcNow;

            var licenseKeyData = new LicenseKeyDataBuilder()
            {
                Generation = LicenseGeneration.Current,
                LicenseGuid = this._randomNumberGenerator.NextGuid(),
                Product = LicenseProduct.MetalamaFree,
                LicenseType = LicenseType.Community,
                ValidFrom = start
            };

            var licenseRegistrationData = licenseKeyData.Build().ToLicenseRegistrationProperties( licenseKeyData.Serialize() );

            return licenseRegistrationData;
        }
    }
}