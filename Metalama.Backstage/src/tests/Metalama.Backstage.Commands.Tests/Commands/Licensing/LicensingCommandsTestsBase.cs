// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Testing;
using System;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public abstract class LicensingCommandsTestsBase : CommandsTestsBase
    {
        protected LicensingCommandsTestsBase( ITestOutputHelper logger )
            : base( logger )
        {
            this.UserDeviceDetection.IsInteractiveDevice = true;
        }

        protected static TestLicenseKeyProvider LicenseKeyProvider { get; } = new();

        /// <summary>
        /// The minimal version of Metalama that <see cref="CreateLicenseKeyRequiringFutureVersion"/> requires. No
        /// version of Metalama supports it, so a license key of that group is never consumed.
        /// </summary>
        protected static Version FutureVersion { get; } = new( 9999, 0 );

        /// <summary>
        /// Creates a license key that carries a minimal version of Metalama greater than the version of the test
        /// application. Registering it therefore stores it in a group that the test application does not support.
        /// </summary>
        /// <returns>The license key.</returns>
        protected static string CreateLicenseKeyRequiringFutureVersion()
        {
            var builder = new LicenseKeyDataBuilder
            {
                LicenseId = 801,
                Product = LicenseProduct.MetalamaProfessional,
                LicenseType = LicenseType.Business,
                Generation = LicenseGeneration.Current,
                SubscriptionEndDate = LicenseKeyProvider.DefaultSubscriptionExpirationDate,
                MinMetalamaVersion = FutureVersion
            };

            return builder.SignAndSerialize( LicenseKeyProvider.Authority );
        }
    }
}