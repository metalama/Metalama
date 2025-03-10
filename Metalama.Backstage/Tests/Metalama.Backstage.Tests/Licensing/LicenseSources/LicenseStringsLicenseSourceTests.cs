// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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
    }
}