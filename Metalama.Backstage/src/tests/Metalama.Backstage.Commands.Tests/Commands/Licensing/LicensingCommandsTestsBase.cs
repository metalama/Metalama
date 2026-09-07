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
        /// The minimal version of Metalama that <see cref="CreateLicenseKeyRequiringLaterVersion"/> requires. It is
        /// greater than the version of the test application, so the test application does not support a group of
        /// that version.
        /// </summary>
        protected static Version LaterVersion { get; } = new( 2027, 0 );

        /// <summary>
        /// Creates a license key whose minimal version of Metalama is greater than the version of the test
        /// application. Registering it therefore stores it in a group that the test application does not support.
        /// </summary>
        /// <returns>The license key.</returns>
        /// <remarks>
        /// The license key is signed by the Elliptic Curve DSA authority of #1864, and its minimal version is
        /// detected from the identifier of the signature key.
        /// </remarks>
        protected static string CreateLicenseKeyRequiringLaterVersion()
        {
            var builder = new LicenseKeyDataBuilder
            {
                LicenseId = 801,
                Product = LicenseProduct.MetalamaProfessional,
                LicenseType = LicenseType.Business,
                Generation = LicenseGeneration.Current,
                SubscriptionEndDate = LicenseKeyProvider.DefaultSubscriptionExpirationDate
            };

            return builder.SignAndSerialize( LicenseKeyProvider.ECDsaAuthority );
        }
    }
}