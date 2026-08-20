// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.LicenseSources
{
    public sealed class LicenseStringsLicenseSourceTests : LicensingTestsBase
    {
        public LicenseStringsLicenseSourceTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public void OneLicenseStringPasses()
        {
            ExplicitLicenseSource source = new( LicenseKeyProvider.MetalamaProfessionalBusiness, LicenseSourceKind.Test, this.ServiceProvider );

            var license = source.GetLicenses( _ => { } ).Single();
            Assert.NotNull( license );

            var dataParsed = license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var data, out var errorMessage );
            Assert.True( dataParsed );
            Assert.Null( errorMessage );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, data!.LicenseString );
        }

        /// <summary>
        /// Verifies that a malformed license string is reported as an invalid license instead of throwing. A
        /// continuous integration build usually reads the license string from a secret, so a mistyped value is a
        /// likely mistake and must be reported as such. See issue #1859.
        /// </summary>
        [Fact]
        public void MalformedLicenseStringIsReportedAsInvalid()
        {
            ExplicitLicenseSource source = new( "NOT-A-REAL-KEY", LicenseSourceKind.Test, this.ServiceProvider );

            var license = source.GetLicenses( _ => { } ).Single();
            Assert.NotNull( license );

            var dataParsed = license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var data, out var errorMessage );
            Assert.False( dataParsed );
            Assert.Null( data );
            Assert.NotNull( errorMessage );
            Assert.NotEmpty( errorMessage );
        }
    }
}
