// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    }
}