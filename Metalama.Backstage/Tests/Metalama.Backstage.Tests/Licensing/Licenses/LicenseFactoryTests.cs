// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Licenses
{
    public sealed class LicenseFactoryTests : LicensingTestsBase
    {
        public LicenseFactoryTests( ITestOutputHelper logger )
            : base( logger ) { }

        private LicenseFactory _licenseFactory = null!;

        private LicenseFactory LicenseFactory
        {
            get
            {
                this.EnsureServicesInitialized();

                return this._licenseFactory;
            }
        }

        protected override void OnAfterServicesCreated( Services services )
        {
            base.OnAfterServicesCreated( services );

            this._licenseFactory = new LicenseFactory( services.ServiceProvider );
            this.UserDeviceDetection.IsInteractiveDevice = true;
        }

        [Fact]
        public void NullLicenseStringFails()
        {
            Assert.False( this.LicenseFactory.TryCreate( "", out _, out _ ) );
        }

        [Fact]
        public void EmptyLicenseStringFails()
        {
            Assert.False( this.LicenseFactory.TryCreate( string.Empty, out _, out _ ) );
        }

        [Fact]
        public void WhitespaceLicenseStringFails()
        {
            Assert.False( this.LicenseFactory.TryCreate( " ", out _, out _ ) );
        }

        [Fact]
        public void InvalidLicenseStringCreatesInvalidLicense()
        {
            const string invalidLicenseString = "SomeInvalidLicenseString";
            Assert.True( this.LicenseFactory.TryCreate( invalidLicenseString, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );
            Assert.False( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out _, out _ ) );
        }

        [Fact]
        public void RevokedLicenseStringCreatesInvalidLicense()
        {
            // ReSharper disable StringLiteralTypo
            const string revokedLicenseString =
                "1-ZEQQQQQQZTQEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEJZWKEM8SCXJK6KJLFD92CAJFQKCGC67A9NVYA2JGNEHLB8QQG4JAF94J58KUJQZW8ZQQDTFJJPA";

            // ReSharper restore StringLiteralTypo

            Assert.True( this.LicenseFactory.TryCreate( revokedLicenseString, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );
            Assert.False( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out _, out _ ) );
        }

        [Fact]
        public void ValidLicenseKeyCreatesValidLicense()
        {
            Assert.True( this.LicenseFactory.TryCreate( LicenseKeyProvider.PostSharpUltimate, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );
            Assert.True( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var licenseData, out errorMessage ) );
            Assert.NotNull( licenseData );
            Assert.Null( errorMessage );
        }

        [Fact]
        public void UrlCreatesLicenseLease()
        {
            // LicenseConsumptionOptions.Default

            // Assert.True( this._licenseFactory.TryCreate( "http://hello.world", out var license ) );
            // Assert.True( license is LicenseLease );

            Assert.False( this.LicenseFactory.TryCreate( "http://hello.world", out _, out _ ) );
        }

        [Fact]
        public void LicenseKeyWithInvalidSignatureFails()
        {
            var licenseKey = new LicenseKeyDataBuilder
            {
                Product = LicenseProduct.MetalamaProfessional, Signature = new byte[16], SignatureKeyId = this.LicensingAuthority.KeyIds.Single()
            }.SerializeToLicenseString();

            Assert.True( this.LicenseFactory.TryCreate( licenseKey, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );

            Assert.False( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var data, out errorMessage ) );
            Assert.Null( data );
            Assert.NotEmpty( errorMessage );
        }
    }
}