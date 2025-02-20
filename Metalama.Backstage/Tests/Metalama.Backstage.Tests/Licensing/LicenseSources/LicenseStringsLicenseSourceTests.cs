// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
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
            ExplicitLicenseSource source = new( LicenseKeyProvider.MetalamaProfessionalBusiness, this.ServiceProvider );

            var license = source.GetLicense( _ => { } );
            Assert.NotNull( license );

            var dataParsed = license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var data, out var errorMessage );
            Assert.True( dataParsed );
            Assert.Null( errorMessage );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, data!.LicenseString );
        }
    }
}