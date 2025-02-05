// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption
{
    public class RedistributionLicenseKeyValidationTests : LicenseConsumptionManagerTestsBase
    {
        public RedistributionLicenseKeyValidationTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Theory]
        [InlineData(
            nameof(LicenseKeyProvider.PostSharpUltimateOpenSourceRedistribution),
            TestLicenseKeyProvider.PostSharpUltimateOpenSourceRedistributionNamespace )]
        [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateOpenSourceRedistribution), TestLicenseKeyProvider.MetalamaUltimateRedistributionNamespace )]
        [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateCommercialRedistribution), TestLicenseKeyProvider.MetalamaUltimateRedistributionNamespace )]
        public void RedistributionLicenseAllowsLicensedNamespace( string licenseKeyName, string requiredNamespace )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
            var manager = this.CreateConsumptionManager();

            var actualIsValid = manager.TryValidateRedistributionLicenseKey( licenseKey, requiredNamespace, out _ );

            Assert.True( actualIsValid );
        }

        [Theory]
        [InlineData( nameof(LicenseKeyProvider.PostSharpUltimateOpenSourceRedistribution) )]
        [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateOpenSourceRedistribution) )]
        [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateCommercialRedistribution) )]
        public void RedistributionLicenseForbidsArbitraryNamespace( string licenseKeyName )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
            var manager = this.CreateConsumptionManager();

            var actualIsValid = manager.TryValidateRedistributionLicenseKey( licenseKey, "Foo", out _ );

            Assert.False( actualIsValid );
        }

        [Fact]
        public void NonRedistributableLicenseKeyFailsRedistributionLicenseKeyValidation()
        {
            var manager = this.CreateConsumptionManager();

            var actualIsValid = manager.TryValidateRedistributionLicenseKey(
                LicenseKeyProvider.MetalamaUltimatePersonalProjectBound,
                TestLicenseKeyProvider.MetalamaUltimateProjectBoundProjectName,
                out _ );

            Assert.False( actualIsValid );
        }
    }
}