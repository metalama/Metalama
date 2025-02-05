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
    internal class UnsignedLicenseFactory
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
        public (string LicenseKey, LicenseProperties Data) CreateEvaluationLicense()
        {
            var start = this._time.UtcNow.Date;
            var end = start + LicensingConstants.EvaluationPeriod;

            var licenseKeyData = new LicenseKeyDataBuilder()
            {
                LicenseGuid = Guid.NewGuid(),
                Product = LicensedProduct.MetalamaUltimate,
                LicenseType = LicenseType.Evaluation,
                ValidFrom = start,
                ValidTo = end,
                SubscriptionEndDate = end
            };

            var licenseKey = licenseKeyData.Serialize();
            var licenseRegistrationData = licenseKeyData.Build().ToLicenseProperties();

            return (licenseKey, licenseRegistrationData);
        }

        /// <summary>
        /// Creates an unsigned Metalama Free license.
        /// </summary>
        /// <returns>The unsigned Metalama Free license.</returns>
        public (string LicenseKey, LicenseProperties Data) CreateFreeLicense()
        {
            var start = this._time.UtcNow;

            var licenseKeyData = new LicenseKeyDataBuilder
            {
                LicenseGuid = Guid.NewGuid(), Product = LicensedProduct.MetalamaFree, LicenseType = LicenseType.Personal, ValidFrom = start
            };

            var licenseKey = licenseKeyData.Serialize();
            var licenseRegistrationData = licenseKeyData.Build().ToLicenseProperties();

            return (licenseKey, licenseRegistrationData);
        }
    }
}