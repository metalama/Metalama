// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses;
using Xunit;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Security.Cryptography;
#endif

namespace Metalama.Backstage.Tests.Licensing.Licenses
{
    public class LicenseFactoryTests : LicensingTestsBase
    {
        public LicenseFactoryTests( ITestOutputHelper logger )
            : base( logger ) { }

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
            Assert.False( license.TryGetLicenseConsumptionData( out _, out _ ) );
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
            Assert.False( license.TryGetLicenseConsumptionData( out _, out _ ) );
        }

        [Fact]
        public void ValidLicenseKeyCreatesValidLicense()
        {
            Assert.True( this.LicenseFactory.TryCreate( LicenseKeyProvider.PostSharpUltimate, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );
            Assert.True( license.TryGetLicenseConsumptionData( out var licenseData, out errorMessage ) );
            Assert.NotNull( licenseData );
            Assert.Null( errorMessage );
        }

        [Fact]
        public void UrlCreatesLicenseLease()
        {
            // TODO

            // Assert.True( this._licenseFactory.TryCreate( "http://hello.world", out var license ) );
            // Assert.True( license is LicenseLease );

            Assert.False( this.LicenseFactory.TryCreate( "http://hello.world", out _, out _ ) );
        }

        [Fact]
        public void LicenseKeyWithInvalidSignatureFails()
        {
            const string licenseKeyWithInvalidSignature =
                "38-ZTDQQQQQZTQEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAQBFWVXMN427EKZ65PRVX5REXJGX4JXFNVJQZFKKUA6RYS6CY5897CWN85QQVBSREX3U5Z8WTX8KNK8XDRLB29PB2J2K5C98UYNAWU5YJ4QQWANS3P3";

            Assert.True( this.LicenseFactory.TryCreate( licenseKeyWithInvalidSignature, out var license, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( license is License );

#if NETFRAMEWORK
            Assert.Throws<CryptographicException>( () => license.TryGetLicenseConsumptionData( out _, out _ ) );
#else
            Assert.False( license.TryGetLicenseConsumptionData( out var data, out errorMessage ) );
            Assert.Null( data );
            Assert.NotEmpty( errorMessage );
#endif
        }
    }
}