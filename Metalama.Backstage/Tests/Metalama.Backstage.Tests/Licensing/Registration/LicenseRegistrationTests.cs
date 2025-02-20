// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Registration
{
    public sealed class LicenseRegistrationTests : LicensingTestsBase
    {
        public LicenseRegistrationTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Theory]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity) )]
#pragma warning disable CS0612
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree) )]
#pragma warning restore CS0612 // Type or member is obsolete
        public void RegisterValidLicense( string licenseKeyName )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
            Assert.True( this.LicenseRegistrationService.TryRegisterLicense( licenseKey, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.Equal( licenseKey, this.LicenseRegistrationService.RegisteredLicense.LicenseString );
        }

        [Theory]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false, false )]
        [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false, false )]
        [InlineData( nameof(TestLicenseKeyProvider.NotYetValid), true, false )]
        [InlineData( nameof(TestLicenseKeyProvider.NoLongerValid), true, false )]
        [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription) )]
#pragma warning disable CS0612
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree) )]
#pragma warning restore CS0612
        public void ParseAndValidateLicense( string licenseKeyName, bool isParsable = true, bool isValid = true )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );

            Assert.Equal( isParsable, this.LicenseRegistrationService.TryParseLicenseKey( licenseKey, out _, out _ ) );
            Assert.Equal( isValid, this.LicenseRegistrationService.TryValidateLicenseKey( licenseKey, out _ ) );

            // Check that this does not register the license.
            Assert.Null( this.LicenseRegistrationService.RegisteredLicense );
        }

        [Theory]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned) )]
        [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey) )]
        public void RegisterInvalidLicense( string licenseKeyName )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );

            Assert.False( this.LicenseRegistrationService.TryRegisterLicense( licenseKey, out var errorMessage ) );
            Assert.NotNull( errorMessage );
            Assert.Null( this.LicenseRegistrationService.RegisteredLicense );
        }

        [Fact]
        public void RegisterManyKeys()
        {
            Assert.Null( this.LicenseRegistrationService.RegisteredLicense );

            // First registration. 
            Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness, out _ ) );
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, this.LicenseRegistrationService.RegisteredLicense.LicenseString );

            // Second registration. 
            Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaProfessionalPersonal, out _ ) );
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalPersonal, this.LicenseRegistrationService.RegisteredLicense.LicenseString );
        }

        [Fact]
        public void RegisterCommunity()
        {
            Assert.Null( this.LicenseRegistrationService.RegisteredLicense );

            Assert.True( this.LicenseRegistrationService.TryRegisterCommunityEdition( out _ ) );
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.Equal( LicensedProduct.MetalamaCommunity, this.LicenseRegistrationService.RegisteredLicense.Product );
        }

        [Fact]
        public void Unregister()
        {
            Assert.Null( this.LicenseRegistrationService.RegisteredLicense );

            // First registration. 
            Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness, out _ ) );
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, this.LicenseRegistrationService.RegisteredLicense.LicenseString );

            // Unregistration.
            Assert.True( this.LicenseRegistrationService.TryRemoveCurrentLicense( out var removedLicense ) );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, removedLicense );
        }
    }
}