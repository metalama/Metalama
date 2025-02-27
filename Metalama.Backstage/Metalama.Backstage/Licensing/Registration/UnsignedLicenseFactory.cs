// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsignedLicenseFactory"/> class.
        /// </summary>
        /// <param name="services">Services.</param>
        public UnsignedLicenseFactory( IServiceProvider services )
        {
            this._time = services.GetRequiredBackstageService<IDateTimeProvider>();
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
                OriginVersion = LicenseKeyDataBuilder.CurrentVersion,
                Generation = LicenseGeneration.Current,
                LicenseGuid = Guid.NewGuid(),
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
                OriginVersion = LicenseKeyDataBuilder.CurrentVersion,
                Generation = LicenseGeneration.Current,
                LicenseGuid = Guid.NewGuid(),
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
                OriginVersion = LicenseKeyDataBuilder.CurrentVersion,
                Generation = LicenseGeneration.Current,
                LicenseGuid = Guid.NewGuid(),
                Product = LicenseProduct.MetalamaFree,
                LicenseType = LicenseType.Community,
                ValidFrom = start
            };

            var licenseRegistrationData = licenseKeyData.Build().ToLicenseRegistrationProperties( licenseKeyData.Serialize() );

            return licenseRegistrationData;
        }
    }
}