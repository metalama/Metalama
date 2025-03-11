// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Testing;
using System.Linq;
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
            Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.Equal( licenseKey, this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );
        }

        [Theory]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal) )]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate) )]
        [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), true, false )]
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

            Assert.Equal( isParsable, this.LicenseRegistrationService.ParseLicenseKey( licenseKey ).IsSuccess );
            Assert.Equal( isValid, this.LicenseRegistrationService.ValidateLicenseKey( licenseKey ).IsSuccess );

            // Check that this does not register the license.
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
        }

        [Theory]
        [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned) )]
        [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey) )]
        public void RegisterInvalidLicense( string licenseKeyName )
        {
            var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );

            Assert.False( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
        }

        [Fact]
        public void RegisterManyKeys()
        {
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

            // First registration. 
            Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness ).IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );

            // Second registration. 
            Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalPersonal ).IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalPersonal, this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );
        }

        [Fact]
        public void RegisterCommunity()
        {
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

            Assert.True( this.LicenseRegistrationService.RegisterCommunityEdition( CommunityLicenseReason.Individual ).IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.Equal( LicenseProduct.MetalamaCommunity, this.LicenseRegistrationService.RegisteredLicenses.Single().Product );
        }

        [Fact]
        public void Unregister()
        {
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

            // First registration. 
            Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness ).IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );

            // Unregistration.
            this.LicenseRegistrationService.RemoveLicenses();
            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
        }
    }
}